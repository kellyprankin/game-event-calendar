# Design notes

Decisions and trade-offs behind this project; the README's design write-up summarizes these.

## Capacity
- Capacity is copied onto each event from its template at creation, then enforced in two places: an atomic seat claim in `RegistrationService` (`RegisteredCount + 1 WHERE RegisteredCount < Capacity`) and a database check constraint (`RegisteredCount <= Capacity`) as a backstop.
- Proof: `RegistrationTests` fires 50 simultaneous registrations at a 30-seat event against real Postgres (Testcontainers) and asserts exactly 30 succeed. To check the test has teeth, the seat claim was temporarily swapped for a naive count-then-insert: all 50 registrations succeeded and the test failed (expected 30, actual 50).
- Duplicates: unique index on (event, normalized name). A duplicate rolls back the seat claim so no seat is wasted. A real system would key on an account or email, since names aren't unique.

## Storage: Postgres instead of in-memory
- The brief allows any storage. EF Core's in-memory provider doesn't support `ExecuteUpdate`, doesn't enforce unique indexes or check constraints, and has no real transactions, so the capacity and duplicate guarantees would live only in app code.
- Postgres in Docker lets the database enforce them, and `docker compose up` is still one command. Cost: graders need Docker.

## Templates
- Game behavior (formats, duration, default/max capacity, min players) is data on `GameTemplate`; no code branches on a game's name or id.
- Templates are seeded at startup with EF's `UseSeeding`, inserting only missing rows so edits to existing rows survive.
- Adding a 4th game is one seed entry: see commit `209ff0c` ("Add D&D template (no core changes)"), which touches only `SeedData.cs`. D&D also shows the model isn't card-specific.

### Formats are strings, not enums
- An enum is fixed at compile time, so every new game would mean code changes, which is what the template requirement rules out. A shared enum would also conflate games (Magic's "Standard" isn't Netrunner's) and allow invalid pairings.
- Each template owns its list; the API checks the chosen format is in that game's list and rejects others with a 400.
- Trade-offs: the database doesn't enforce `Event.Format` (API-only validation), and a renamed format leaves the old name on past events (arguably correct: an event records what it was scheduled as).
- **Next step:** a `GameFormat` table (one row per game + format) with `Event` referencing it by foreign key. That gives database-level integrity and a place for per-format settings, e.g. Magic Draft needing a different duration or seat limit than Standard.

## Game choices
- Three card games with Wizards history: Magic, Vampire: The Eternal Struggle (Garfield, 1994), and Netrunner (Garfield, 1996; later Fantasy Flight's *Android: Netrunner*, now maintained by Null Signal Games). Netrunner is a personal favorite.
- D&D as the non-card example.

## Other choices
- REST over GraphQL for the timebox; `.ics` and QR images are natural REST resources. In GraphQL the schema would be `Event`, `GameTemplate`, and a `registerForEvent` mutation returning `Registration | EventFullError | DuplicateRegistrationError`.
- Minimal APIs organized into route-group extension classes, matching the "small focused services" direction in the job description.
- Native `date`/`time` inputs instead of a calendar library: accessible by default and no extra dependency. The trade-off is that each browser draws its own picker (Edge, for example, shades today and colors weekends red), so the look varies by browser. Past days are greyed out via `min`; the form and the API still reject start times that have passed.

## Security
Reviewed once the app was feature-complete.
- **Checked and fine:** no vulnerable NuGet or npm packages; EF Core parameterizes all queries; Ical.Net escapes event text (a name containing line breaks and a fake `ATTENDEE:` line stayed inside `SUMMARY`); Vue escapes rendered text and nothing uses `v-html`; `.ics` download names are reduced to letters and digits; no endpoint returns registrant names; production returns generic ProblemDetails with no stack traces; the container runs as a non-root user; input lengths are capped in the API and the database.
- **Fixed:** Postgres was published on all interfaces with the public dev password; it now binds to `127.0.0.1` only.
- **Known limits, not fixed:**
  - **No abuse protection.** With no auth, anyone can create events or fill one with fake names. Next step: ASP.NET's built-in rate limiter on create and register (partitioned so the concurrency test still runs), then real player identity.
  - **Registration links trust the Host header** when `PUBLIC_BASE_URL` is unset. A spoofed Host only changes the requester's own response, but behind a shared cache it could poison the QR code for others. Set `PUBLIC_BASE_URL` (and `AllowedHosts`) in any real deployment.
  - **Duplicate check reveals names.** "Already registered" confirms a given name signed up for an event. Minor with name-only registration; goes away with accounts.
  - **No HTTPS.** Fine locally; a deployment would terminate TLS in front of the app.
