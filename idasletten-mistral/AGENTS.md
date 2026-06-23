# Idasletten - Development Guide for AI Agents

## Overview
Idasletten is a table football (foosball) tournament management web application with a Norse mythology theme. It tracks players, teams, matches, results, and provides a configurable scoreboard.

## Tech Stack
- **Language**: C# 11+
- **Framework**: ASP.NET Core 8.0 (Web)
- **Database**: SQLite (in-memory locally, file-based in production)
- **ORM**: Entity Framework Core 8.0
- **UI**: Razor Pages with Basecoat UI via CDN
- **Architecture**: CQRS + Vertical Slices with MediatR
- **Authentication**: Azure AD (primary), Test User (development)
- **Scoring**: Elo, TrueSkill (Moserware.Skills), Lives, WinCount

## Project Structure

```
Idasletten/
├── Pages/                 # Razor Pages (minimal logic)
├── Features/             # Vertical slices
│   ├── Tournaments/       # Tournament-related commands/queries
│   ├── Players/          # Player-related commands/queries
│   ├── Matches/           # Match-related commands/queries
│   ├── Users/            # User-related commands/queries
│   └── Auth/             # Authentication commands/queries
├── Shared/               # Cross-cutting concerns
├── Migrations/           # EF Core migrations
├── wwwroot/              # Static files
└── Program.cs            # Application entry point

Idasletten.Tests/
├── Features/             # Integration tests mirroring main features
├── TestInfrastructure/   # WebApplicationFactory, test helpers
└── Any.cs               # Test data factories
```

## Architecture Rules

### 1. Vertical Slices with CQRS
- Each feature folder contains:
  - `Commands/` - Command classes
  - `Queries/` - Query classes  
  - `Handlers/` - Command/query handlers
  - `Events/` - Domain events
- Pages send commands/queries via MediatR only
- Handlers use DbContext directly (no repositories)

### 2. Event Publishing
**CRITICAL**: Every command handler MUST publish a domain event at the end of successful execution.

```csharp
// Example pattern
public class CreateTournamentHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;
    
    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken ct)
    {
        var tournament = new Tournament { /* ... */ };
        _context.Tournaments.Add(tournament);
        await _context.SaveChangesAsync(ct);
        
        // ALWAYS publish event
        await _publisher.Publish(new TournamentCreated(tournament.Id), ct);
        
        return tournament.Id;
    }
}
```

### 3. Database & Migrations
- **Local**: SQLite in-memory mode
- **Production**: SQLite file-based
- **Migrations**: Always create with `dotnet ef migrations add <Name>`
- **Auto-apply on startup**: Migrations are automatically applied when the application starts (see Program.cs)

### 4. Authentication
- Azure AD via app registration (primary)
- Test user login enabled when `TestUser__Email` and `TestUser__Password` env vars are set
- Configure `UseForwardedHeaders` with cleared `KnownNetworks` and `KnownProxies` for Fly.io
- Login required for: creating tournaments, editing completed matches
- No login required for: browsing, recording new match results

### 5. Testing
- **Framework**: xUnit with AAA pattern
- **Test naming**: `Should_DoSomething_When_ConditionIsFulfilled`
- **Test data**: Use static `Any` class with methods like `Any.User()`, `Any.Tournament()`
- **Factory**: Custom `WebApplicationFactory` with in-memory database
- **Seeding**: Factory seeds data for both tests and local development
- **Preferences**: Simple stubs over mocking frameworks

## Running Locally

### Prerequisites
- .NET 8.0 SDK
- Node.js (for potential frontend tooling)

### Setup
```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run --project Idasletten

# Or with watch
dotnet watch run --project Idasletten
```

The app will be available at:
- Local: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### Environment Variables
For test user login:
```bash
# Required for test user to appear
TestUser__Email=test@example.com
TestUser__Password=TestPassword123!
```

For Azure AD:
```bash
AzureAd__ClientId=your-client-id
AzureAd__ClientSecret=your-client-secret
AzureAd__TenantId=your-tenant-id
```

## Creating Migrations

```bash
# Add migration
dotnet ef migrations add AddTournamentModel --project Idasletten

# Apply migration (automatic on startup, but can be manual)
dotnet ef database update --project Idasletten
```

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Deployment

### Fly.io
```bash
# Login
fly auth login

# Create app
fly create idasletten

# Set secrets
fly secrets set AzureAd__ClientId=<value> AzureAd__ClientSecret=<value> AzureAd__TenantId=<value>

# Deploy
fly deploy
```

### GitHub Actions
A workflow file should be created at `.github/workflows/deploy.yml` to deploy `main` branch to Fly.io on push.

## UI Guidelines
- Use Basecoat UI via CDN: `https://cdn.basecoatui.com/latest/basecoat.min.css`
- Do NOT install Tailwind (included in Basecoat CDN bundle)
- Light/white theme only
- Flexbox for layout
- Components reference: https://basecoatui.com/kitchen-sink/

## Scoring Systems

| System | Implementation | Notes |
|--------|---------------|-------|
| Elo | Custom implementation | Average team score, standard ELO calculations |
| TrueSkill | Moserware.Skills library | Use for skill-based matchmaking |
| Lives | Custom implementation | Lose a life on game loss, default 3 lives |
| WinCount | Simple calculation | Score = WinCount, tie-break by goal difference |

## Important Patterns

### 1. Tournament Creation Flow
```
User creates tournament -> Seed from previous tournament (optional) -> 
Add players -> Plan matches -> Start tournament
```

### 2. Multi-round Tournaments
- Each round is a separate tournament
- `ParentTournamentId` links to previous round
- `RoundNumber` auto-incremented
- Scores reset between rounds
- Players carry over from parent

### 3. Seeding
- `SeedTournamentId` references source tournament for initial player ordering
- Used in match planning algorithms
- Cannot seed from tournament with a parent

### 4. Match Planning Types
- **Random**: Random team assignments
- **Equality**: Best vs worst pairing
- **Fair**: Top half vs bottom half pairing

## File Structure Conventions

### Commands/Queries
```csharp
// File: Features/Tournaments/Commands/CreateTournament.cs
public record CreateTournamentCommand(
    string Name,
    int TeamSize = 2,
    int PointsToWin = 5,
    ScoreSystem ScoreSystem = ScoreSystem.Elo,
    int? MaxPlayerCount = null,
    bool IsPublic = true
) : IRequest<Guid>;
```

### Handlers
```csharp
// File: Features/Tournaments/Handlers/CreateTournamentHandler.cs
public class CreateTournamentHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;
    
    public CreateTournamentHandler(AppDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }
    
    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken ct)
    {
        // Implementation
        await _publisher.Publish(new TournamentCreated(tournament.Id), ct);
        return tournament.Id;
    }
}
```

### Events
```csharp
// File: Features/Tournaments/Events/TournamentCreated.cs
public record TournamentCreated(Guid TournamentId) : INotification;
```

## Helpful Commands

```bash
# Build and run
dotnet run --project Idasletten

# Create migration
dotnet ef migrations add <Name> --project Idasletten

# Run tests
dotnet test

# Clean and rebuild
dotnet clean && dotnet build

# Check EF Core CLI version
dotnet ef --version
```

## Known Issues & Workarounds

1. **SQLite in-memory with migrations**: Use connection string `Data Source=:memory:` and apply migrations on startup
2. **Azure AD redirect URIs**: Configure `UseForwardedHeaders` for Fly.io proxy
3. **Test user seeding**: Ensure test user exists before test runs

## Useful Links
- [Basecoat UI](https://basecoatui.com/installation/#install-cdn)
- [Moserware Skills (TrueSkill)](https://github.com/moserware/Skills)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Fly.io .NET Deployment](https://fly.io/docs/dotnet/)
