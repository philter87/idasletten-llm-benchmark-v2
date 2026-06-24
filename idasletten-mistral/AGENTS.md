# Idasletten - Agent Instructions

## Overview

Idasletten is a table football (foosball) tournament management web application with a Norse mythology theme. This document provides instructions for AI agents (like Mistral Vibe) working on this codebase.

---

## Tech Stack

| Component | Technology | Version |
|-----------|------------|---------|
| Runtime | .NET | 8.0 |
| Framework | ASP.NET Core | 8.0 |
| ORM | Entity Framework Core | 8.0 |
| Database | SQLite | - |
| Authentication | Azure AD (Microsoft.Identity.Web) | 2.15.0 |
| Mediator | MediatR | 12.2.0 |
| UI Framework | Razor Pages | - |
| CSS Framework | Basecoat (via CDN) | - |
| Layout | Flexbox | - |
| Theme | Light (white) | - |
| Testing | xUnit | 2.4.2 |

---

## Architecture

### Project Structure

```
Idasletten/
├── Pages/                    # Razor Pages (minimal logic, only send commands/queries)
├── Features/                 # Vertical slices (CQRS + MediatR)
│   ├── Tournaments/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── Events/
│   ├── Players/
│   ├── Matches/
│   ├── Users/
│   └── Authentication/
├── Shared/
│   ├── Data/
│   │   ├── Entities/       # Domain entities
│   │   ├── ApplicationDbContext.cs
│   │   └── Migrations/     # EF Core migrations
│   ├── Scoring/            # Scoring system implementations
│   └── UI/                 # Shared UI components, base layout
├── Program.cs
├── appsettings.json
└── Idasletten.csproj

Idasletten.Tests/
├── Features/                # Feature tests (mirror main Features structure)
├── Infrastructure/
│   └── CustomWebApplicationFactory.cs
└── Any.cs                  # Test data factories
```

### Key Principles

1. **CQRS + Vertical Slices**: Each feature folder contains its own commands, queries, handlers, and events. Features are independent and self-contained.

2. **MediatR Pattern**: All communication between Pages and Features goes through MediatR. Pages send commands/queries and receive results.

3. **Minimal Logic in Pages**: Razor Pages contain only UI logic and MediatR calls. Business logic lives in Feature handlers.

4. **Direct DbContext Usage**: Handlers may use `ApplicationDbContext` directly. Avoid repository/service abstraction layers.

5. **Event Publishing**: Every command handler MUST publish at least one event at the end of successful execution (e.g., `CreateTournamentCommand` publishes `TournamentCreatedEvent`).

6. **Auto-Apply Migrations**: Migrations are applied automatically on application startup. See Database section below.

---

## Database

### Configuration

- **Local Development**: SQLite in-memory mode (`Data Source=:memory:`)
- **Production**: SQLite file-based database
- **Connection String**: Configured in `appsettings.json` and environment variables

### Migrations

**IMPORTANT**: Migrations are applied automatically on app startup.

```csharp
// In Program.cs
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}
```

**Rules:**
- Always create migrations with `dotnet ef migrations add <Name>` CLI
- Never manually edit migration files
- Apply migrations automatically on startup (as shown above)
- Test migrations work with in-memory SQLite before committing

### Seeding

- Database is seeded on startup with test data
- Seeding logic should handle both development and test environments
- Use the `Any` class methods for generating test data

---

## Authentication

### Azure AD Configuration

- Uses Microsoft.Identity.Web package
- App registration required in Azure Portal
- Environment variables for configuration:
  - `AzureAd__Instance`
  - `AzureAd__Domain`
  - `AzureAd__TenantId`
  - `AzureAd__ClientId`
  - `AzureAd__ClientSecret`
  - `AzureAd__CallbackPath`

### Test User Authentication

- Second login option enabled only when both `TestUser__Email` and `TestUser__Password` env vars are set
- Test user is auto-seeded into the database
- Use this for local development and testing without Azure AD

### Required Login Actions

- **Login required**: Creating tournaments, editing completed matches
- **No login required**: Browsing, recording new match results

### Forwarded Headers (Fly.io)

```csharp
// In Program.cs
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
// Note: KnownNetworks and KnownProxies are cleared to trust Fly's proxy
```

---

## Scoring Systems

Four scoring systems are supported, configured via `Tournament.ScoreSystem`:

| System | Description | Default Parameters |
|--------|-------------|-------------------|
| `TrueSkill` | Microsoft's TrueSkill algorithm | mu=30, sigma=10 (Aggressive) |
| `Elo` | Standard Elo rating system | K-factor configurable |
| `Lives` | Lose a life on each loss | Initial lives=3 |
| `WinCount` | Simple win counting | Score = WinCount |

**Default**: TrueSkill (as per user decision)

### Implementation Location

`Shared/Scoring/` directory contains:
- `IScoringSystem.cs` - Interface
- `TrueSkillScoringSystem.cs` - TrueSkill implementation using Moserware.Skills
- `EloScoringSystem.cs` - Elo implementation
- `LivesScoringSystem.cs` - Lives implementation
- `WinCountScoringSystem.cs` - WinCount implementation
- `ScoringSystemFactory.cs` - Factory to resolve correct system

---

## Testing

### Test Framework

- **Framework**: xUnit
- **Pattern**: AAA (Arrange, Act, Assert)
- **Naming Convention**: `Should_DoSomething_When_ConditionIsFulfilled`
- **Mocking**: Prefer simple stubs over mocking frameworks

### Custom WebApplicationFactory

Location: `Idasletten.Tests/Infrastructure/CustomWebApplicationFactory.cs`

Configures:
- In-memory SQLite database for tests
- Test authentication (test user when env vars are set)
- Seeded test data

### Test Data Factories

Location: `Idasletten.Tests/Any.cs`

Static class with methods for generating test entities:

```csharp
public static class Any
{
    public static User User() => new() { ... };
    public static Tournament Tournament() => new() { ... };
    public static TournamentPlayer TournamentPlayer() => new() { ... };
    // ... etc
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
cd Idasletten.Tests
dotnet test

# Run with coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

---

## Running Locally

### Prerequisites

- .NET 8.0 SDK
- Node.js (optional, for npm if needed)

### Setup

1. Clone the repository
2. Ensure you're in the `idasletten-mistral` directory
3. Restore dependencies:

```bash
cd Idasletten
dotnet restore
```

### Environment Variables

Create a `.env` file or set environment variables:

```
# Database
ConnectionStrings__Default=Data Source=idasletten.db

# Azure AD (optional for local dev without Azure)
AzureAd__Instance=https://login.microsoftonline.com/
AzureAd__Domain=your-domain.onmicrosoft.com
AzureAd__TenantId=your-tenant-id
AzureAd__ClientId=your-client-id
AzureAd__ClientSecret=your-client-secret
AzureAd__CallbackPath=/signin-oidc

# Test User (for local development without Azure AD)
TestUser__Email=test@idasletten.local
TestUser__Password=Test@1234
```

### Run the Application

```bash
cd Idasletten
dotnet run
```

Application will be available at:
- `http://localhost:5000` (HTTP)
- `https://localhost:5001` (HTTPS)

### Create Migrations

```bash
cd Idasletten
dotnet ef migrations add <MigrationName> --project Idasletten
```

### Apply Migrations

Migrations are applied automatically on startup. To manually apply:

```bash
cd Idasletten
dotnet ef database update
```

---

## Deployment

### Fly.io

1. Install flyctl: https://fly.io/docs/hands-on/install-flyctl/
2. Authenticate: `fly auth login`
3. Create app: `fly launch`
4. Configure secrets:

```bash
fly secrets set ConnectionStrings__Default=<connection-string>
fly secrets set AzureAd__ClientId=<client-id>
fly secrets set AzureAd__ClientSecret=<client-secret>
# ... other Azure AD settings
```

5. Deploy: `fly deploy`

### fly.toml Configuration

```toml
app = "idasletten"
primary_region = "ams"

[build]
  dockerfile = "Dockerfile"

[http_service]
  internal_port = 8080
  force_https = true
  auto_stop_machines = true
  auto_start_machines = true
  min_machines_running = 0

[[vm]]
  memory = "1gb"
  cpu_kind = "shared"
  cpus = 2
```

### GitHub Actions

Workflow file: `.github/workflows/deploy.yml`

Triggers on push to `main` branch, builds and deploys to Fly.io.

---

## UI Guidelines

### Basecoat CSS

- Loaded via CDN in `_Layout.cshtml`
- Documentation: https://basecoatui.com/kitchen-sink/
- Do NOT install Tailwind (already included in CDN bundle)

```html
<!-- In _Layout.cshtml -->
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/basecoat@1.0.0/dist/basecoat.min.css">
```

### Layout

- **Light theme** (white background)
- **Flexbox** for layout (not CSS Grid)
- **Responsive** design
- Follow basecoat component patterns

### Color Scheme

Use basecoat's default light theme. Custom colors if needed:
- Primary: Norse mythology inspired colors (deep blues, golds)
- Background: White
- Text: Dark gray/black
- Accents: Gold/orange

---

## Git Workflow

### Branching

- `main` - Production ready code
- `develop` - Integration branch
- `feature/*` - Feature branches
- `fix/*` - Bug fix branches

### Commit Messages

Use conventional commits:
- `feat: add new feature`
- `fix: fix bug`
- `docs: update documentation`
- `refactor: refactor code`
- `test: add tests`
- `chore: maintenance tasks`

### Pull Requests

- All PRs require approval
- All tests must pass
- Code review required for all changes

---

## Common Tasks

### Adding a New Feature

1. Create folder in `Features/`
2. Add Commands, Queries, Events subfolders
3. Implement command/query handlers
4. Publish events from command handlers
5. Register with MediatR in Program.cs
6. Add corresponding tests in `Tests/Features/`

### Adding a New Page

1. Create Razor Page in `Pages/` folder
2. Add minimal logic (only MediatR calls)
3. Use basecoat components for styling
4. Follow existing page patterns

### Adding a New Entity

1. Create entity class in `Shared/Data/Entities/`
2. Add DbSet to `ApplicationDbContext`
3. Configure relationships in `OnModelCreating`
4. Create migration: `dotnet ef migrations add Add<EntityName>`

---

## Troubleshooting

### Common Issues

**"No database provider has been configured"**
- Check `appsettings.json` connection string
- Verify environment variables are set
- Ensure SQLite package is installed

**"Migrations not found"**
- Ensure migrations are created in the correct project
- Check `DbContext` namespace matches
- Run `dotnet ef migrations add InitialCreate` if no migrations exist

**"Cannot find MediatR handlers"**
- Ensure handlers are registered: `builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`
- Check handler namespace

**"Basecoat styles not loading"**
- Verify CDN URL in `_Layout.cshtml`
- Check internet connection (CDN requires online)
- Clear browser cache

### Debugging

```bash
# Run with logging
dotnet run --verbose

# Check environment variables
set | grep AzureAd
dotnet user-secrets list

# View database (SQLite)
sqlite3 idasletten.db
```

---

## Contacts & Resources

- **Repository**: https://github.com/mjolner-code/idasletten-llm-benchmark-v2
- **Original Spec**: prompt.md
- **Basecoat Docs**: https://basecoatui.com/
- **MediatR Docs**: https://github.com/jbogard/MediatR
- **EF Core Docs**: https://docs.microsoft.com/en-us/ef/core/
- **Azure AD Docs**: https://docs.microsoft.com/en-us/azure/active-directory/

---

## Appendix: Entity Reference

### Tournament
- Name (string)
- TeamSize (int, default: 2)
- PointsToWin (int, default: 5)
- ScoreSystem (enum: Elo, TrueSkill, Lives, WinCount, default: TrueSkill)
- MaxPlayerCount (int?, optional)
- IsArchived (bool)
- IsPublic (bool)
- SeedTournamentId (Guid?, optional)
- ParentTournamentId (Guid?, optional)
- RoundNumber (int?, auto-incremented)

### TournamentPlayer
- UserId (Guid)
- TournamentId (Guid)
- Score (double)
- WinCount (int)
- MatchCount (int)
- LoseCount (int)
- Lives (int, default: 3, only for Lives scoring)
- PointsWon (int)
- PointsLost (int)
- ScoreDiff (double)

### TournamentTeam
- Name (string, auto-generated: "Team 1", "Team 2", ...)
- Number (int, auto-generated: 1, 2, ...)

### TournamentMatch
- Order (int)
- TournamentId (Guid)
- State (enum: Planned, Done, Cancelled)

### TournamentTeamMatchResult
- MatchId (Guid)
- TournamentId (Guid)
- TeamId (Guid)
- GoalsWon (int)
- GoalsLost (int)

### User
- Username (string, 3 initials, unique)
- Name (string)
- Email (string, optional)
- ImageUrl (string, fetched from Azure Graph API)

---

*Last updated: 2026-06-24*
