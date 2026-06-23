# AGENTS.md — Idasletten

Table-football (foosball) tournament app with a Norse-mythology theme. Track players, teams,
matches and a configurable scoreboard. Auth is optional: anyone can browse and record results;
logging in is only required to create tournaments and edit completed matches.

## Tech stack

- **C# / .NET 10**, ASP.NET Core **Razor Pages**.
- **Entity Framework Core** + **SQLite**.
- **MediatR** for CQRS.
- **Microsoft.Identity.Web** for Azure AD; cookie auth for the test user.
- **Moserware.Skills** for TrueSkill; custom calculators for Elo / Lives / WinCount.
- **Basecoat** UI via CDN (+ Tailwind browser CDN for utilities). Light theme, flexbox layout.
  Do **not** install/build Tailwind — it comes from the CDN.

## Projects

- `Idasletten` — the web app.
- `Idasletten.Tests` — xUnit tests.

## Architecture — CQRS + vertical slices

- **`Pages/`** — Razor pages with minimal logic; they only send commands/queries via MediatR.
- **`Features/<Slice>/`** — one folder per feature. `Commands/` and `Queries/` sub-folders hold the
  request records and their handlers. Handlers use `AppDbContext` directly (no repositories).
  **Every command handler publishes a domain event at the end** (e.g. `CreateTournamentHandler`
  → `TournamentCreated`). Events implement `IDomainEvent`; `LoggingEventHandler<T>` is the
  catch-all sink so every event has a handler.
- **`Shared/`** — cross-cutting code: `Domain/` entities, `Scoring/` (calculators + `ScoreService`
  + `SeedingPlanner`), `Events/`, `Graph/`, `Provisioning`, `CurrentUser`.
- **`Data/`** — `AppDbContext` (extends `IdentityDbContext`) and `DataSeeder`.

### Scoring

`ScoreService.RecalculateAsync` recomputes a tournament from scratch by **replaying its completed
matches in order**. Both recording a new result and editing an existing one call it, so the two
paths always agree. Each `IScoreCalculator` handles one `ScoreSystem`:

- **Elo** — team rating = average of players; the resulting delta is applied to each player. Baseline 1000.
- **TrueSkill** — Moserware.Skills; displayed score is the conservative rating (mean − 3·σ).
- **Lives** — start with 3 lives, lose one per lost match; score mirrors remaining lives.
- **WinCount** — score = wins; goal difference breaks ties (applied in the scoreboard query).

## Database

- **Local & tests:** SQLite **in-memory**, kept alive by a single open connection
  (`Program.cs`). Works with migrations and seeding.
- **Production:** file-based SQLite at `Data Source=/data/idasletten.db` (Fly volume).
- The choice is driven by `IHostEnvironment.IsProduction()`.

### Migrations — **auto-apply on startup**

`db.Database.Migrate()` runs on startup in `Program.cs`, then `DataSeeder.SeedAsync` seeds demo
data (idempotent) and ensures the test user exists. **Always create migrations with the CLI:**

```bash
dotnet ef migrations add <Name> --project Idasletten
```

Never hand-edit the model snapshot.

## Running locally

```bash
# In-memory DB, demo data, test login enabled:
ASPNETCORE_ENVIRONMENT=Development \
TestUser__Email=test@idasletten.local \
TestUser__Password=ragnarok \
dotnet run --project Idasletten --no-launch-profile
# → http://localhost:5005 (set ASPNETCORE_URLS to change)
```

## Testing

```bash
dotnet test
```

- Integration/feature tests use a custom `WebApplicationFactory` (`IdaslettenFactory`) with the
  in-memory database, sending real MediatR commands/queries.
- `Any` is the test-data factory (`Any.User()`, `Any.Tournament()`, …) — all fields randomised.
- xUnit, **AAA** (Arrange/Act/Assert), method names `Should_X_When_Y`. Prefer simple stubs over
  mocking frameworks.
- **UI validation is done with Playwright MCP screenshots only — do not write Playwright tests.**
  Authenticate via the test login to view authenticated pages.

## Authentication

- Login required **only** for: creating a tournament; editing a `Done` match.
- No login for: browsing, recording a new result (`/create-match`).
- **Azure AD** via `AddMicrosoftIdentityWebApp`, enabled when `AzureAd:ClientId` + `AzureAd:Instance`
  are configured. Otherwise a plain cookie scheme is used so the **test login** still works.
- **Test login** (second button on `/login`) is shown only when both `TestUser__Email` and
  `TestUser__Password` are set; that user is also seeded.
- New users have their photo fetched from the **Graph API** on creation (no-op when unconfigured).
- **Fly.io:** `UseForwardedHeaders` with `KnownNetworks`/`KnownProxies` cleared so redirect URIs
  are generated as `https://`.

## Configuration keys

| Key | Purpose |
|---|---|
| `AzureAd:Instance` / `TenantId` / `ClientId` / `ClientSecret` | Azure AD sign-in + Graph photos |
| `TestUser__Email` / `TestUser__Password` | Enable + seed the test-login user |
| `ConnectionStrings:Default` | Override the production SQLite path |

## Deployment

- **Fly.io** (`fly deploy`). App name = the folder name (`idasletten-opus48-v2`). A persistent
  volume is mounted at `/data` for the SQLite file.
- A **GitHub Action** (`.github/workflows/deploy.yml`) deploys `main` on push using `FLY_API_TOKEN`.
