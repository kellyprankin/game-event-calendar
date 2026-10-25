# Plan

Timebox: 3 hours. Each step ends in a working state and gets its own commit, so the project can stop after any step and still run.

**Stack:** .NET 10 minimal APIs, EF Core + Npgsql, Postgres (docker compose), Ical.Net, QRCoder, xUnit + Testcontainers, Vue 3 + Vite + TypeScript.

## Phase 0: Setup (~15 min)
1. Keep the take-home instructions out of the repo (`.gitignore`).
2. Rename the branch to `main`, create the public GitHub repo, and push.
3. Add a starter `CLAUDE.md` with the stack, conventions, and the rule "seat check must be one atomic conditional update."

## Phase 1: Data and storage (~30 min)
4. Add EF Core with Npgsql, plus a `docker-compose.yml` with Postgres and the API.
5. Create the models:
   - **GameTemplate:** name, formats, default duration, default and max capacity, minimum players.
   - **Event:** copies duration and capacity from its template at creation; tracks the registered count.
   - **Registration:** one row per player.
6. Seed three games (Magic: The Gathering, Pokémon TCG, Lorcana), add a unique index on event + normalized name, and create the first migration. Apply migrations at startup so `docker compose up` is enough.

## Phase 2: API (~60 min)
7. List templates; create event with server-side checks (format must belong to the template, capacity can't exceed its max).
8. List events by date range; get one event with seats left.
9. Registration via `RegistrationService` with an atomic seat claim. 409 for a full event, 409 for a duplicate name.
10. Concurrency test: 50 simultaneous registrations against a 30-seat event, exactly 30 succeed.
11. `.ics` download with Ical.Net: correct start/end, location, timezone.
12. QR code image with QRCoder, encoding `PUBLIC_BASE_URL/events/{id}/register`.

## Phase 3: Frontend (~50 min)
13. Vue 3 + Vite + TypeScript. The build goes into the API's `wwwroot` so everything is served from one address.
14. Calendar page with events grouped by day.
15. Create-event form whose formats and capacity limits follow the selected game.
16. Event page: details, seats left, minimum players, QR code, `.ics` link.
17. Registration page with clear errors (full, duplicate), proper labels, and `aria-live` error announcements.

## Phase 4: Write-up (~25 min)
18. `README.md`: run instructions (`docker compose up`, setting `PUBLIC_BASE_URL` for phone scanning), design write-up (capacity and concurrency, template system, cuts and next steps), and AI usage note drawn from `AI_NOTES.md`.
19. Final end-to-end run, including scanning the QR code with a phone.

## Planned cuts
- No auth.
- Name-only identity, so duplicate detection is best effort.
- Minimum players is shown but not enforced.
- No editing or cancelling events (out of scope per the brief).
- REST instead of GraphQL.
- Agenda list instead of a month grid.

## Risks
- **Docker or migration setup eating time.** If Phase 1 runs past ~45 minutes, switch to SQLite and adjust the seat claim accordingly.
- **Frontend sprawl.** Plain functional pages are enough; CSS polish isn't graded.
