# GameEvent

Take-home exercise: a small app for a game store to schedule tabletop game events and let players register. See `docs/PLAN.md` for the step-by-step plan and current scope.

## Stack
- API: ASP.NET Core (.NET 10) minimal APIs in `GameEventApi/GameEventApi`
- Data: EF Core 10 + Npgsql, Postgres via `docker-compose.yml`
- Libraries: Ical.Net (`.ics`), QRCoder (QR codes). Never hand-roll either.
- Tests: xUnit + WebApplicationFactory + Testcontainers (real Postgres)
- Frontend: Vue 3 + Vite + TypeScript, built into the API's `wwwroot`

## Commands
- Start Postgres only: `docker compose up -d db`
- Run API: `dotnet run --project GameEventApi/GameEventApi`
- Add migration: `dotnet ef migrations add <Name> --project GameEventApi/GameEventApi` (uses the local tool in `dotnet-tools.json`; run `dotnet tool restore` first)
- Whole app: `docker compose up --build`

## Conventions
- Endpoints live in `Endpoints/*Endpoints.cs` as `MapXEndpoints(this RouteGroupBuilder)` extensions with route groups. Keep `Program.cs` small.
- Use named static handler methods, `TypedResults`, and `Results<...>` return types so every outcome shows in the signature and OpenAPI.
- Errors are `ProblemDetails`.
- Game behavior comes from `GameTemplate` data. Never branch on a game's name or id in code; adding a game must only mean adding a template row.
- Events copy duration, capacity, and min players from their template at creation. Validate format and capacity against the template on the server.

## Capacity rules (do not break)
- Seats are claimed with a single atomic conditional update: `RegisteredCount = RegisteredCount + 1 WHERE Id = @id AND RegisteredCount < Capacity`. Zero rows affected means full.
- Never count registrations and then insert as separate steps.
- Claim the seat and insert the registration in one transaction; on a duplicate name (unique violation) roll back so the seat is released.
- Registration logic lives in `RegistrationService`, not in the endpoint handler.
- The concurrency test (many parallel registrations, exactly `Capacity` succeed) must stay green.

## Working agreement
- One bounded task at a time; build and run tests before calling it done.
- Don't commit unless asked. The take-home instructions file is gitignored on purpose; never commit it.
