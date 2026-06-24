# AGENTS.md — Idasletten Development Guide

## Project Overview
Idasletten is a web application for running table-football (foosball) tournaments with a Norse mythology theme. It tracks players, teams, matches, results, and a configurable scoreboard.

## Tech Stack
- **Runtime**: C# / .NET 9.0
- **Framework**: ASP.NET Core Razor Pages
- **ORM**: Entity Framework Core with SQLite
- **CQRS**: MediatR (vertical slices)
- **UI**: [basecoat](https://basecoatui.com/) via CDN (includes Tailwind CSS — do NOT install Tailwind separately)
- **Authentication**: Azure AD via OpenIdConnect + Cookie auth
- **Testing**: xUnit, WebApplicationFactory, InMemory database

## Project Structure
```
idasletten-cp-opus48/
├── Idasletten/                  # Main web project
│   ├── Features/                # CQRS vertical slices
│   │   ├── Matches/
│   │   │   ├── Commands/        # RecordMatchResult, CreatePlannedMatch, PlanSeveralMatches
│   │   │   └── Queries/         # GetMatches, GetMatchById
│   │   ├── Players/
│   │   │   └── Commands/        # AddPlayerToTournament
│   │   ├── Scoring/             # EloScoringService, TrueSkillScoringService, LivesScoringService, WinCountScoringService
│   │   ├── Tournaments/
│   │   │   ├── Commands/        # CreateTournament
│   │   │   └── Queries/         # GetTournaments, GetTournamentById, GetPublicTournaments
│   │   └── Users/
│   │       ├── Commands/        # CreateUser
│   │       └── Queries/         # GetUser
│   ├── Pages/                   # Razor Pages (minimal logic — use MediatR)
│   │   ├── Account/             # Logout
│   │   ├── Tournaments/         # Index, Detail, Create, CreateMatch, Matches, MatchDetail, Players
│   │   └── Users/               # Profile
│   └── Shared/
│       ├── Entities/            # EF Core entities (User, Tournament, TournamentPlayer, ...)
│       └── Infrastructure/      # AppDbContext, DatabaseSeeder
└── Idasletten.Tests/            # Test project
    ├── Features/Scoring/        # Unit tests for scoring systems
    ├── Integration/             # WebApplicationFactory integration tests
    ├── Any.cs                   # Test data factory
    └── IdaslettenWebApplicationFactory.cs
```

## Architecture — CQRS + Vertical Slices (MediatR)

- **Pages** contain minimal logic — they only send commands/queries via MediatR.
- **Features** contain one folder per feature/slice with commands, queries, and handlers.
- **Handlers** use `AppDbContext` directly (no repositories/services).
- **Every command handler publishes an event** (e.g., `CreateUserHandler` → `UserCreated`).
- **Events** are `INotification` records published via `IMediator.Publish()`.

## Database Rules (CRITICAL)

- **Development/Production**: SQLite file-based (`idasletten.db`)
- **Migrations auto-apply on startup** in non-Testing environments — `db.Database.Migrate()` runs in `Program.cs`
- **Creating migrations**: Always use the `dotnet ef` CLI:
  ```bash
  cd Idasletten
  dotnet ef migrations add <MigrationName>
  ```
- **Never** hand-edit migration files
- **Tests**: Use InMemory database (configured in `IdaslettenWebApplicationFactory`)

## Running Locally

```bash
# Restore & build
cd Idasletten
dotnet build

# Run (migrations apply automatically)
dotnet run

# App starts at https://localhost:5001 / http://localhost:5000
```

### Local test user login
Set these environment variables (or in `appsettings.Development.json`):
```json
{
  "TestUser": {
    "Email": "test@example.com",
    "Password": "testpassword"
  }
}
```
This enables a test login button on the `/Login` page and seeds a TEST user in the database.

## Running Tests

```bash
cd idasletten-cp-opus48
dotnet test
```

Tests use an InMemory database and don't require any external services.

## Authentication

- **No login required**: browsing tournaments, recording match results (`/create-match`)
- **Login required**: creating tournaments (`/Tournaments/Create`), editing completed matches
- **Azure AD**: Configure in `appsettings.json`:
  ```json
  {
    "AzureAd": {
      "TenantId": "<your-tenant-id>",
      "ClientId": "<your-client-id>",
      "ClientSecret": "<your-client-secret>"
    }
  }
  ```
- **Fly.io proxy**: Forwarded headers are configured with `KnownNetworks` and `KnownProxies` cleared so Azure AD redirect URIs use `https://`

## Scoring Systems

| System | Description |
|---|---|
| **Elo** | Standard Elo rating. For multi-player teams, averages team scores. K=32. |
| **TrueSkill** | Bayesian system via `Moserware.Skills` library. Score = (Mean - 3×StdDev) × 100 |
| **Lives** | Players start with 3 lives. Losing a match costs one life. Score = remaining lives. |
| **WinCount** | Score = number of wins. Tie-breaker: goal difference. |

## Deployment — Fly.io

```bash
# Install fly CLI, then:
fly launch          # First time setup
fly deploy          # Deploy
fly secrets set ASPNETCORE_ENVIRONMENT=Production
fly secrets set ConnectionStrings__DefaultConnection="DataSource=/data/idasletten.db"
```

The app name is `idasletten-cp-opus48` (matches folder name).

## UI Framework — basecoat

- **CDN**: `https://cdn.jsdelivr.net/npm/basecoat-css@latest/dist/basecoat.min.css`
- **Do NOT** install Tailwind CSS separately (included in basecoat CDN bundle)
- **Theme**: Light/white — use `class="light"` on `<html>`
- **Components**: https://basecoatui.com/kitchen-sink/
- **Layout**: Flexbox

## Key Rules for AI Agents

1. **Migrations must be created with `dotnet ef migrations add <Name>`** — never hand-edit
2. **Migrations auto-apply on app startup** (in Program.cs, non-Testing environment)
3. **No login required for match recording** — `POST /tournaments/{id}/create-match` is public
4. **Login required for tournament creation** — pages decorated with `[Authorize]`
5. **Handler pattern**: `Command/Query → Handler → DbContext directly` (no repositories)
6. **Publish events**: Every command handler must publish a notification event after saving
7. **Test naming**: `Should_DoSomething_When_ConditionIsFulfilled`
8. **Test data**: Use `Any.User()`, `Any.Tournament()`, etc. — never hardcode entity data
9. **Pages path**: Custom routes use `@page "/path/{param}"` — follow existing patterns
10. **Team auto-naming**: Teams are named "Team 1", "Team 2" etc. — not user input

## Page Routes

| Page | Route |
|---|---|
| Home | `/` |
| All Tournaments | `/Tournaments` |
| Tournament Detail | `/tournaments/{tournamentId}` |
| Create Tournament | `/Tournaments/Create` |
| Create/Record Match | `/tournaments/{tournamentId}/create-match` |
| All Matches | `/tournaments/{tournamentId}/matches` |
| Match Detail | `/tournaments/{tournamentId}/matches/{matchId}` |
| Players | `/tournaments/{tournamentId}/players` |
| User Profile | `/users/{userId}` |
| Login | `/Login` |
