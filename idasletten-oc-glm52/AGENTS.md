# AGENTS.md — Idasletten

Idasletten is a web app for running table-football (foosball) tournaments: tracking players,
teams, matches, results, and a configurable scoreboard. Norse-mythology themed.

## Tech stack

- **.NET 10** ASP.NET Core Razor Pages
- **Entity Framework Core** with **SQLite** (in-memory locally, file-based in production)
- **MediatR** (CQRS + vertical slices)
- **Moserware.Skills** NuGet for TrueSkill scoring (works on net10.0 despite the .NET Framework 4.x target; the package loads via the compat shim — ignore the NU1701 warning)
- **basecoat** UI via CDN (https://basecoatui.com) — Tailwind is included in the CDN bundle; do **not** add Tailwind separately.
- **xUnit** for tests, **Microsoft.AspNetCore.Mvc.Testing** `WebApplicationFactory` for integration/feature tests.
- Deploy target: **Fly.io** (folder name `idasletten-oc-glm52` is the app name).

## Solution layout

```
idasletten-oc-glm52/
  Idasletten.sln
  Idasletten/                   # main web project
    Program.cs                  # composition root
    SeedData.cs                 # dev/test seeder (runs on every startup if empty)
    Features/                   # one folder per vertical slice
      Tournaments/{Commands,Queries}
      Players/{Commands,Queries}
      Matches/{Commands,Queries}
      Teams/, Users/
    Shared/                     # cross-slice infra (DbContext, MatchRecorder, Scoring/)
    Migrations/                 # EF Core migrations (dotnet ef CLI)
    Pages/                      # Razor Pages (thin; only send MediatR commands/queries)
  Idasletten.Tests/             # xUnit integration tests
```

### Architecture rules

- **CQRS + vertical slices**: each feature folder owns its entities, commands, queries, handlers,
  and notifications. Commands live in `Commands/`, queries in `Queries/`. Handlers may use
  `DbContext` directly — no repositories or services.
- **Every command handler publishes a notification** at the end
  (e.g. `CreateUserHandler` → `UserCreated`, `CreateMatchHandler` → `MatchRecorded`,
  `PlanSeveralMatchesHandler` → `MatchesPlanned`). Notifications live alongside their command.
- Pages are thin: they only dispatch MediatR requests and render the result.

## Database

- **Local (Development):** SQLite **in-memory** (`DataSource=:memory:`). A single
  `SqliteConnectionHolder` keeps the connection open for the app's lifetime (in-memory SQLite
  is wiped when the connection closes). Configured in `Program.cs` under `IsDevelopment()`.
- **Production:** file-based SQLite at `/data/idasletten.db` (volume-mounted on Fly.io).
- Switch the connection string via `ConnectionStrings:Sqlite` in `appsettings.json`.

### Migrations rule (IMPORTANT)

- **Always create migrations with the `dotnet ef` CLI**, never by hand-editing the model snapshot.
  Example:
  ```bash
  dotnet ef migrations add <Name> --project Idasletten/Idasletten.csproj --output-dir Migrations
  dotnet ef migrations remove --project Idasletten/Idasletten.csproj   # undo
  dotnet ef database update --project Idasletten/Idasletten.csproj      # apply manually if needed
  ```
- **Migrations are applied automatically on app startup** (`db.Database.Migrate()` in `Program.cs`).
  You do **not** need to run `database update` manually in any environment.
- In-memory SQLite + migrations works locally because the seeded connection holder keeps the
  schema alive for the process.

## Scoring systems

`ScoreSystem` enum on `Tournament` selects how `TournamentPlayer.Score` is computed. Implementations
live in `Shared/Scoring/` and all implement `IScoringSystem`:

| System   | Behaviour                                                                    |
|----------|------------------------------------------------------------------------------|
| Elo      | Standard ELO. Teams use the **average** of member scores. Initial mean 1200. |
| TrueSkill| Moserware.Skills `TwoTeamTrueSkillCalculator`. Score stored = μ − 3σ.        |
| Lives    | Lose a match → lose a life (default 3). Score = remaining lives.            |
| WinCount | Score = wins. Tie-break by goal difference (PointsWon − PointsLost).         |

`MatchRecorder.RecordAsync` is the single entry point for recording match results: it writes
`TournamentTeamMatchResult` rows, updates W/L/played/points, applies the scoring system, and
marks the match `Done`. Editing a `Done` match requires login (enforced in the create-match
handler via `HttpContext.User`).

## Run locally

```bash
dotnet build
dotnet run --project Idasletten/Idasletten.csproj        # http://localhost:5085
```

Seed data runs on first startup (10 players + a "Ragnarok Series — Round 1" tournament with
4 sample matches) as long as the DB is empty.

### Test-user login

Set environment variables `TestUser__Email` and `TestUser__Password` to enable a second, test-only
login button on `/login` (the test user is also seeded into the DB). Set `TestUser__Username`
to override the default `TST`.

```bash
TestUser__Email=test@example.com TestUser__Password=test123 dotnet run
```

### Azure AD (optional)

Provide `AzureAd:ClientId`, `AzureAd:ClientSecret` (and optionally tenant) via secrets/env to enable
the "Sign in with Microsoft" button. `ForwardedHeaders` is configured with `KnownNetworks` and
`KnownProxies` cleared so ASP.NET Core trusts Fly's proxy and generates `https://` redirect URIs.

## Tests

```bash
dotnet test
```

- Integration tests use a custom `WebApplicationFactory` with an **in-memory SQLite** database.
- A static `Any` factory in `Idasletten.Tests` (e.g. `Any.User()`) fills all fields with random
  values via Bogus.
- AAA pattern (Arrange / Act / Assert). Method names follow `Should_DoSomething_When_Condition`.
- Prefer simple stubs over mocking frameworks when a dependency is needed.
- Do **not** write Playwright integration tests — Playwright is for visual validation only
  (see "UI validation" below).

## Deployment

### Fly.io

- Folder name is used as the Fly app name: `idasletten-oc-glm52`.
- `fly.toml` and a `Dockerfile` (multi-stage .NET build) are included.
- Deploy:
  ```bash
  fly deploy --app idasletten-oc-glm52 --remote-only
  fly secrets set TestUser__Email=... TestUser__Password=...
  fly secrets set AzureAd__ClientId=... AzureAd__ClientSecret=...
  ```
- Persistent SQLite volume mounted at `/data`.

### GitHub Action

`.github/workflows/fly-deploy.yml` deploys `main` to Fly.io on push using the `fly` CLI.

## UI validation

Visual design validation uses Playwright (chromium) on the host to navigate the running app and
save PNG screenshots under `screenshots/`. The Playwright MCP server in this environment cannot
reach `localhost` (the sandbox blocks 127.0.0.1), so a small Node Playwright script
(`screenshots/shot.mjs`) drives a local headless chromium instead. See
[`screenshots/README.md`](./screenshots/README.md) for repro.

To run screenshots:
```bash
dotnet run --project Idasletten/Idasletten.csproj &     # leave running on :5085
node screenshots/shot.mjs
ls screenshots/*.png
```

## Useful commands

```bash
dotnet build
dotnet test
dotnet ef migrations add <Name> --project Idasletten/Idasletten.csproj --output-dir Migrations
dotnet run --project Idasletten/Idasletten.csproj
fly deploy --app idasletten-oc-glm52 --remote-only
```