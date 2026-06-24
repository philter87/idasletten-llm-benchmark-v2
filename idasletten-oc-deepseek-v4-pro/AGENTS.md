# AGENTS.md — Idasletten

## Tech Stack
- **.NET 10.0**, Razor Pages, C#
- **Entity Framework Core** + **SQLite** (in-memory for dev/testing, file-based for production)
- **MediatR** for CQRS + vertical slice architecture
- **basecoat UI** via CDN (`https://cdn.jsdelivr.net/npm/basecoat@1/dist/basecoat.min.css`), includes Tailwind
- **Azure AD** (OpenID Connect) for optional authentication
- **Moserware.Skills** for TrueSkill scoring
- **xUnit** for testing
- **Fly.io** for deployment

## Architecture: CQRS + Vertical Slices

```
Idasletten/
  Features/
    Matches/Commands/    — CreatePlannedMatch, RecordMatchResult, PlanSeveralMatches
    Matches/Queries/     — GetMatchesForTournament, GetMatchById
    Players/Commands/    — AddPlayerToTournament
    Scoring/             — IScoringService, Elo, TrueSkill, Lives, WinCount
    Tournaments/Commands/— CreateTournament
    Tournaments/Queries/ — GetTournaments, GetPublicTournaments, GetTournamentById
    Users/Commands/      — CreateUser
    Users/Queries/       — GetUserByUsername, GetUserById
  Pages/                 — Razor pages (thin controllers, only send MediatR commands/queries)
  Shared/
    Entities/            — EF Core entity classes + enums
    Infrastructure/      — AppDbContext, DatabaseSeeder, DbContextExtensions
```

## Rules

### Migrations
- Use `dotnet ef migrations add <Name>` in the `Idasletten` project directory
- Migrations auto-apply on startup (unless environment is "Testing")

### Auth
- Login is optional; recording match results works without login
- `[Authorize]` required only for: creating tournaments, editing completed matches

### Event Publishing
- Every command handler MUST publish an `INotification` event at the end
- Events: TournamentCreated, UserCreated, PlayerAddedToTournament, PlannedMatchCreated, SeveralMatchesPlanned, MatchResultRecorded

### Handlers
- Handlers use `AppDbContext` directly (no repository layer)
- Handlers use `IMediator` for publishing events and sending nested commands

### UI
- basecoat UI via CDN (no local Tailwind config)
- Light theme, Inter font, Flexbox for layout
- Nordic/Norse mythology theme (dark red accent `#8b1a1a`)

## Running Locally

```bash
cd Idasletten
dotnet run
```

The app runs at `http://localhost:5180` (http) or `https://localhost:7234` (https).

### Test User
Set environment variables to enable test login:
- `TestUser__Email` — test user email
- `TestUser__Password` — test user password

## Running Tests

```bash
dotnet test
```

Tests use `WebApplicationFactory<Program>` with an in-memory database (unique per test run).

## Deployment

```bash
fly deploy
```

Or push to `main` to trigger the GitHub Actions workflow.

## Test Conventions
- Method naming: `Should_DoSomething_When_ConditionIsFulfilled`
- AAA pattern (Arrange / Act / Assert)
- `Any.*` static factory for test data
- Prefer simple stubs over mocking frameworks
