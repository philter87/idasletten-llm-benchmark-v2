# AGENTS.md — Idasletten

Idasletten is a table football (foosball) tournament tracking web app with a Norse mythology theme, built with ASP.NET Core 9 + Razor Pages + EF Core + SQLite.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9 |
| Web framework | ASP.NET Core Razor Pages |
| ORM | Entity Framework Core 9 with SQLite |
| CQRS | MediatR 12 |
| UI library | Basecoat CSS (CDN, light theme) |
| Authentication | Azure AD (Microsoft.Identity.Web) + optional test user login |
| Scoring: TrueSkill | Moserware.Skills NuGet package |
| Tests | xUnit, FluentAssertions, WebApplicationFactory |
| Deployment | Fly.io |

---

## Project Structure

```
Idasletten/            Main web project
  Features/            Vertical slices (CQRS)
    Users/
      Entities/        User entity
      Commands/        CreateUser
      Queries/         GetUser, GetUsers
      Events/          UserCreated
    Tournaments/
      Entities/        Tournament, TournamentPlayer, TournamentTeam, TournamentTeamPlayer, ScoreSystem enum
      Commands/        CreateTournament, AddPlayerToTournament
      Queries/         GetTournament, GetTournaments
      Events/          TournamentCreated, PlayerAdded
    Matches/
      Entities/        TournamentMatch, TournamentTeamMatchResult, MatchState enum
      Commands/        RecordMatchResult, PlanMatch, PlanSeveralMatches
      Queries/         GetMatches, GetMatch
      Events/          MatchResultRecorded, MatchPlanned
    Scoring/           IScoreCalculator, Elo/TrueSkill/Lives/WinCount calculators, ScoreCalculatorFactory
  Pages/               Razor Pages (thin — only send commands/queries via MediatR)
    Index              Home page
    Tournaments/       Index, Details, Create, CreateMatch, Matches, Players
    Users/             Details
    Login
  Shared/
    Data/              AppDbContext
    Extensions/        TestUserAuthExtensions
    Seeding/           DatabaseSeeder

Idasletten.Tests/      Integration/feature tests
  Factories/           CustomWebApplicationFactory
  Tests/               Feature tests
  Helpers/             Any (test data factories)
```

---

## Architecture Rules

1. **CQRS + MediatR vertical slices**: every feature has its own folder. Handlers use `AppDbContext` directly — no repositories or service layers.
2. **Every command handler publishes a domain event** at the end. E.g. `CreateUserHandler` → `UserCreated`.
3. **Pages are thin**: Razor Pages only call MediatR and pass DTOs to the view. No business logic in pages.
4. **Migrations auto-apply on startup**: `db.Database.Migrate()` is called in `Program.cs` during startup. **Always create migrations with `dotnet ef`.**

---

## Commands

### Run locally
```bash
cd Idasletten
dotnet run
# App starts at https://localhost:5001
```

### Run tests
```bash
dotnet test
```

### Build
```bash
dotnet build
```

### Create a migration
```bash
cd Idasletten
dotnet ef migrations add <MigrationName>
```

### Apply migrations manually (not needed in normal flow — auto-applied on startup)
```bash
dotnet ef database update
```

---

## Database

- **Development**: file-based SQLite (`idasletten-dev.db`) in the project folder
- **Production (Fly.io)**: file-based SQLite at `$DATABASE_PATH` (mounted volume)
- Migrations are **automatically applied on startup**

---

## Authentication

- Azure AD login via `Microsoft.Identity.Web`
- Configure in `appsettings.json`:
  ```json
  {
    "AzureAd": {
      "Instance": "https://login.microsoftonline.com/",
      "Domain": "yourtenant.onmicrosoft.com",
      "TenantId": "...",
      "ClientId": "...",
      "CallbackPath": "/signin-oidc"
    }
  }
  ```
- AzureAD config is **optional** for local development. If `ClientId` is empty, only cookie auth is used.
- **Test user login**: enabled by setting `TestUser__Email` and `TestUser__Password` environment variables (or in appsettings). Shows a second login button on `/login`.

### What requires login
- Creating a tournament (`/tournaments/create`)
- Editing a completed match (requires auth check in page)

### What does NOT require login
- Browsing tournaments/scoreboard
- Recording a new match result (`/tournaments/{id}/create-match`)

---

## Fly.io Deployment

The app name matches the folder: `idasletten-cp-sonnet`.

```bash
# First time deploy
fly launch --name idasletten-cp-sonnet

# Deploy
fly deploy

# SSH into app
fly ssh console
```

### Environment variables (set in Fly.io secrets)
```bash
fly secrets set AzureAd__TenantId=xxx
fly secrets set AzureAd__ClientId=xxx
fly secrets set AzureAd__ClientSecret=xxx
fly secrets set DATABASE_PATH=/data/idasletten.db
```

### GitHub Actions
Push to `main` triggers automatic deploy via `.github/workflows/fly-deploy.yml`.
Set `FLY_API_TOKEN` as a GitHub Actions secret.

---

## Scoring Systems

| System | How Score works |
|---|---|
| **Elo** | Standard Elo (K=32). For multi-player teams, uses team average Elo. Default starting Elo: 1000 |
| **TrueSkill** | Moserware.Skills library. Score = (mean − 3σ) × 100. Default: mean=25, σ=8.333 |
| **Lives** | Losers lose 1 life (default 3). Score = remaining lives |
| **WinCount** | Score = number of wins. Tie-breaker: goal difference |

---

## Testing

- Tests use a custom `WebApplicationFactory` with EF Core **in-memory** database
- The factory also seeds test data (same as development seeding)
- Test data factory: `Any.User()`, `Any.Tournament()`, etc. — creates randomized entities
- Pattern: xUnit + AAA (Arrange / Act / Assert)
- Method naming: `Should_DoSomething_When_ConditionIsFulfilled`
- Prefer simple stubs over mocking frameworks

---

## Notes for AI Agents

- **Never use repositories or service layers** — handlers call `AppDbContext` directly
- **Every command must publish a domain event** using `IMediator.Publish()`
- Keep Razor Pages thin — no business logic there
- The `public partial class Program {}` declaration in `Program.cs` is intentional and required for `WebApplicationFactory` in tests
- Basecoat CSS is loaded from CDN — do NOT install Tailwind
- Use Danish language for user-facing text (this is a Danish app)
