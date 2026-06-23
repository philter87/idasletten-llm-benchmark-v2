# Idasletten — AGENTS.md

## Project overview

Table-football tournament app with a Norse-mythology theme. Built with ASP.NET Core 10 Razor Pages, Entity Framework Core (SQLite), and MediatR CQRS.

## Tech stack

| Layer | Technology |
|---|---|
| Backend | C# / ASP.NET Core 10 Razor Pages |
| ORM | Entity Framework Core 10 + SQLite |
| CQRS | MediatR 14 |
| UI | basecoat via CDN (no Tailwind install needed) |
| Scoring | Custom (Elo, TrueSkill via Moserware.Skills, Lives, WinCount) |
| Auth | Azure AD via Microsoft.Identity.Web + Cookie Auth |
| Deployment | Fly.io (fly CLI) |

## Running locally

```bash
dotnet run --project Idasletten/Idasletten.csproj
```

The app runs at `https://localhost:7xxx` / `http://localhost:5xxx`. It uses a local SQLite file (`idasletten-dev.db`) and seeds sample data automatically.

### Test user login

Set in `appsettings.Development.json` (already set to `test@example.com` / `Test1234!`). The test-login form only appears when `TestUser:Email` and `TestUser:Password` are both set.

## Running tests

```bash
dotnet test
```

Tests use an in-memory database (separate per test class) and the `TestWebApplicationFactory`.

## Architecture

```
Idasletten/
├── Features/           # Vertical slices (one folder per feature)
│   ├── Tournaments/    # Commands, Queries, Events
│   ├── Players/
│   ├── Matches/
│   ├── Users/
│   └── Scoring/        # IScoreCalculator + 4 implementations
├── Pages/              # Razor Pages (thin — only dispatch MediatR)
│   ├── Tournaments/
│   └── Users/
└── Shared/
    ├── Entities/       # EF Core entities
    ├── Data/           # AppDbContext + DbSeeder
    └── Enums/
```

- **Pages** contain minimal logic. They call `ISender.Send()` / `IPublisher.Publish()`.
- **Feature handlers** use `AppDbContext` directly (no repositories/services).
- **Every command handler** publishes a domain event at the end (e.g., `UserCreated`, `TournamentCreated`).

## Database migrations

- **Always create migrations** with the EF CLI:
  ```bash
  dotnet ef migrations add <MigrationName> --project Idasletten
  ```
- **Migrations are applied automatically on startup** via `db.Database.EnsureCreated()` in `Program.cs`.
- Locally a file-based SQLite database (`idasletten-dev.db`) is used.
- In production (`ASPNETCORE_ENVIRONMENT=Production`) the connection string is `Data Source=/data/idasletten.db` (mounted volume on Fly.io).

## Scoring systems

| System | Description |
|---|---|
| Elo | Standard ELO with K=32. Team score = average of member scores. |
| TrueSkill | Moserware.Skills library. Team-based TrueSkill updates. |
| Lives | Each loss costs one life (min 0). Score = Lives remaining. |
| WinCount | Score = WinCount. Tie-breaker = goal difference (PointsWon − PointsLost). |

## Authentication

- Login is **optional** for browsing and recording matches.
- Login **required** for: creating tournaments, editing completed matches.
- Azure AD via `[Authorize]` attribute on pages that need it.
- Test user login enabled when `TestUser:Email` and `TestUser:Password` env vars are set (dev only).

## Deployment

```bash
fly deploy
```

Set secrets on Fly.io:
```bash
fly secrets set AzureAd__ClientId=... AzureAd__ClientSecret=... AzureAd__TenantId=...
```

GitHub Actions (`.github/workflows/deploy.yml`) deploys `main` to Fly.io automatically on push.

## Fly.io notes

`UseForwardedHeaders` is configured with `KnownNetworks` and `KnownProxies` **cleared** — this makes ASP.NET Core trust Fly's reverse proxy and generate correct `https://` redirect URIs for Azure AD.
