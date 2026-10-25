# Game Events

A small web app for a game store organizer to schedule in-store events and let players register for them. Players scan a QR code on the event page, enter their name, and get a seat until the event is full.

**Stack:** ASP.NET Core (.NET 10) minimal APIs, EF Core + PostgreSQL, Vue 3 + Vite + TypeScript. Ical.Net builds the calendar invites and QRCoder the QR codes.

## Running it

You need Docker Desktop (or another Docker engine).

```sh
docker compose up --build
```

Then open **http://localhost:8080**. The database schema and the game templates are created on startup; there's no separate seed step.

**Scanning the QR code with a phone:** phones can't reach `localhost`, so open the app using your computer's network address instead, e.g. `http://192.168.1.50:8080`. QR codes encode whatever address the page was opened from. To pin a specific address instead, put it in a `.env` file next to `docker-compose.yml`:

```sh
PUBLIC_BASE_URL=http://192.168.1.50:8080
```

(Windows Firewall may ask to allow port 8080 the first time.)

### Development setup

Requires the .NET 10 SDK and Node 24.

```sh
docker compose up -d db                       # Postgres only
dotnet run --project api/GameEventApi         # API on http://localhost:5198
cd web && npm install && npm run dev          # Vue dev server; proxies /api to the API
```

Tests run the real API against a throwaway Postgres container, so Docker must be running:

```sh
dotnet test api/GameEventApi.slnx
```

### Project layout

```
api/GameEventApi/          API: Data/ (entities, seed), Endpoints/, Services/, Contracts/
api/GameEventApi.Tests/    Integration tests (xUnit + WebApplicationFactory + Testcontainers)
web/                       Vue app; builds into the API's wwwroot
docs/                      Plan, design notes, AI notes
```

## Design write-up

### How many people can attend, and what happens when two people race for the last seat?

Capacity lives on the **event**. When an event is created, its capacity defaults to the game template's value; the organizer may change it within the template's range (e.g. 8–30 for Magic, 3–7 for D&D). The server validates that range, then copies the capacity onto the event, so later template changes don't alter events already on the calendar. Each event also keeps a `RegisteredCount`.

Registration claims a seat with a single conditional update inside a transaction:

```sql
UPDATE "Events" SET "RegisteredCount" = "RegisteredCount" + 1
WHERE "Id" = @id AND "RegisteredCount" < "Capacity"
```

Postgres locks the event row for the rest of the transaction, so concurrent requests for the last seat queue behind each other: the first one gets the seat, and the rest see `RegisteredCount = Capacity`, update zero rows, and get **409 "This event is full."** The app never counts registrations and then inserts as two separate steps, which is the classic race where two requests both see 29 of 30 and both get in. As a backstop, a database check constraint rejects any write that would leave `RegisteredCount > Capacity`.

**Duplicates:** registration is name-only, so a duplicate means the same name (ignoring case and extra spaces) on the same event, enforced by a unique index. A duplicate rolls back the transaction, which gives the claimed seat back, and returns **409 "You're already registered."** A real system would identify players by account or email, since two different people can share a name.

**Evidence:** `RegistrationTests` fires 50 simultaneous registrations at a 30-seat event and asserts exactly 30 succeed and 20 are told the event is full. It also sends 10 simultaneous registrations under one name and asserts one succeeds without leaking seats. To make sure the test can actually fail, I temporarily replaced the seat claim with a naive count-then-insert: all 50 registrations got in and the test failed.

### How does the template system work, and what would a 4th game require?

Each game is a `GameTemplate` row: its formats, default duration, default and max capacity, minimum players, and display order. Event logic only reads those fields. It checks that the chosen format is in the template's list and the capacity is in its range, and copies the duration and minimum players onto the event. Nothing in the code checks which game it's dealing with; the create form likewise rebuilds its format list and capacity limits from whichever template is selected.

The seeded games are Magic: The Gathering plus two other card games with Wizards history, **Vampire: The Eternal Struggle** and **Netrunner** (both Richard Garfield designs that Wizards published in the '90s; Netrunner is a personal favorite).

**Adding a 4th game is one new entry in `SeedData.cs`** (or one row in the table). Commit `209ff0c` shows this: it adds **Dungeons & Dragons**, a non-card game with very different numbers (tables of 3–7, a "One-shot" format), and touches no other file. A game that needed genuinely new behavior, such as team registration or per-format durations, would need new template fields. The natural next step is a `GameFormat` table so each format can carry its own duration and seat limit and events reference formats by foreign key instead of by name.

### What I cut or faked, and what I'd build next

- **No auth or accounts.** One organizer, anonymous players. Anyone can create events.
- **Name-only identity.** Duplicate detection is best effort (see above).
- **Minimum players is shown, not enforced.** The event page shows it; nothing cancels an under-filled event.
- **No editing, cancelling, or unregistering**, per the brief. There's also no organizer view of who registered.
- **Agenda list instead of a month grid.** Upcoming events grouped by day.
- **Store details are placeholders** in `appsettings.json`. Times are entered and displayed in the browser's time zone, stored in UTC, and written to `.ics` files in UTC.
- **REST, not GraphQL.** The QR image and `.ics` download are natural REST resources. In GraphQL this would be `Event` and `GameTemplate` types and a `registerForEvent` mutation returning `Registration | EventFullError | DuplicateRegistrationError`.
- **Frontend tests.** Only the API is tested; the UI was checked by hand.
- **No abuse protection or HTTPS.** Without auth, anyone can create events or fill one with fake names; rate limiting is the next step. Set `PUBLIC_BASE_URL` in any real deployment so registration links don't trust the request's Host header. Security review notes are in [`docs/DESIGN_NOTES.md`](docs/DESIGN_NOTES.md#security).

**Next:** player accounts or email-based registration (which also enables confirmation and unregistering), a waitlist that promotes people when seats open, an organizer view of registrants, the `GameFormat` table, a configured store time zone, and a month grid.

## AI usage

I used Claude (desktop app) to read the brief with me, plan the work, and choose the stack, and Claude Code in a terminal next to Visual Studio to implement it. I gave the agent one bounded step at a time from [`docs/PLAN.md`](docs/PLAN.md), with standing rules in [`CLAUDE.md`](CLAUDE.md) (for example, "seat check must be one atomic conditional update; never count-then-insert"), and reviewed each diff before committing.

**One fix:** Claude Code first seeded the game templates with EF Core's `HasData`. The project built and the migration looked right, but the app crashed on startup because EF compares the seeded `Formats` lists by reference and reported a pending model change on every build. Running `docker compose up` instead of trusting the green build caught it; the fix was EF's `UseSeeding` hook. I also had the agent prove the concurrency test could fail (see above) rather than accepting a passing test at face value. More in [`docs/AI_NOTES.md`](docs/AI_NOTES.md).
