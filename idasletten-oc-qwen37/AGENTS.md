# Idasletten - Agent Instructions

## Overview
Idasletten is a table football (foosball) tournament management web application built with ASP.NET Core Razor Pages, Entity Framework Core, SQLite, and MediatR (CQRS pattern).

## Tech Stack
- **Framework**: ASP.NET Core 10.0 with Razor Pages
- **Database**: SQLite (file-based in production, in-memory for tests)
- **ORM**: Entity Framework Core 10.0
- **Architecture**: CQRS with MediatR (vertical slices)
- **UI**: Basecoat UI via CDN (https://basecoatui.com)
- **Authentication**: Azure AD (Microsoft Identity Web) + test user login
- **Testing**: xUnit, WebApplicationFactory, Bogus for test data
- **Deployment**: Fly.io with GitHub Actions

## Project Structure
```
Idasletten/
├── Data/                    # DbContext and migrations
├── Models/                  # Entity models
├── Features/                # CQRS vertical slices
│   ├── Tournaments/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── Events/
│   ├── Matches/
│   ├── Players/
│   └── Users/
├── Pages/                   # Razor Pages
│   ├── Shared/
│   ├── Tournaments/
│   ├── Matches/
│   ├── Players/
│   └── Users/
├── Shared/                  # Shared utilities (scoring calculators, etc.)
└── wwwroot/                 # Static files

Idasletten.Tests/
├── Infrastructure/          # Test infrastructure (CustomWebApplicationFactory)
├── Features/                # Feature tests
└── Any.cs                   # Test data factory
```

## Architecture Rules

### CQRS Pattern
- **Commands**: Write operations that change state. Always return a result (Guid for created entities).
- **Queries**: Read operations. Return DTOs or entities.
- **Events**: Published after command handlers complete (e.g., `UserCreated`, `TournamentCreated`).
- Handlers can use `DbContext` directly (no repository pattern).
- Each feature has its own folder with Commands/, Queries/, and Events/ subfolders.

### Database
- **Migrations**: Always create migrations with `dotnet ef migrations add <Name> -p Idasletten/Idasletten.csproj -o Data/Migrations`
- **Auto-apply**: Migrations are automatically applied on app startup in `Program.cs`
- **SQLite**: File-based in production (`idasetten.db`), in-memory for tests

### Scoring Systems
Four scoring systems are implemented in `Shared/Scoring/`:
1. **Elo**: Rating-based system (K-factor=32, initial=1500)
2. **TrueSkill**: Uses Moserware.Skills library
3. **Lives**: Players lose lives when they lose matches (default 3 lives)
4. **WinCount**: Simple win counter

## Running the Application

### Local Development
```bash
# Run the application
dotnet run --project Idasletten

# The app will be available at http://localhost:5000
# Database file: idasetten.db (auto-created)
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

### Database Migrations
```bash
# Create a new migration
dotnet ef migrations add <MigrationName> -p Idasletten/Idasletten.csproj -o Data/Migrations

# Apply migrations (automatic on startup, but can be done manually)
dotnet ef database update -p Idasletten/Idasletten.csproj

# Remove last migration
dotnet ef migrations remove -p Idasletten/Idasletten.csproj
```

## Testing Conventions
- **Framework**: xUnit
- **Pattern**: AAA (Arrange/Act/Assert)
- **Naming**: `Should_DoSomething_When_ConditionIsFulfilled`
- **Test Data**: Use `Any` class for random test data generation
- **Database**: In-memory database via `CustomWebApplicationFactory`
- **Seeding**: Factory automatically seeds test data

Example test:
```csharp
[Fact]
public async Task Should_CreateUser_When_UsernameIsUnique()
{
    // Arrange
    var factory = new CustomWebApplicationFactory();
    var mediator = factory.Services.GetRequiredService<IMediator>();
    var username = Any.Username();

    // Act
    var userId = await mediator.Send(new CreateUserCommand(username, Any.Name(), Any.Email(), null));

    // Assert
    Assert.NotEqual(Guid.Empty, userId);
}
```

## Key Features

### Tournaments
- Create tournaments with configurable scoring systems, team sizes, and points to win
- Public/private tournaments
- Tournament seeding from previous tournaments
- Multi-round tournaments (parent/child relationships)

### Matches
- Create matches manually or plan multiple matches at once
- Three seeding types: Random, Equality (best vs worst), Fair (top vs bottom half)
- Fixed or rotating teams
- Match results automatically update player scores

### Players
- Add players by initials (auto-creates users if needed)
- Cross-tournament player statistics
- Score tracking with delta display

### Authentication
- Azure AD for production
- Test user login (enabled when `TestUser__Email` and `TestUser__Password` env vars are set)
- Login required for: creating tournaments, editing completed matches
- No login required for: recording new match results

## Deployment

### Fly.io
```bash
# Deploy to Fly.io
fly deploy

# The app uses the folder name as the app name
```

### Environment Variables
- `ConnectionStrings__DefaultConnection`: SQLite connection string (optional, defaults to `idasetten.db`)
- `AzureAd__Instance`: Azure AD instance (e.g., `https://login.microsoftonline.com/`)
- `AzureAd__Domain`: Azure AD domain
- `AzureAd__TenantId`: Azure AD tenant ID
- `AzureAd__ClientId`: Azure AD client ID
- `TestUser__Email`: Test user email (optional)
- `TestUser__Password`: Test user password (optional)

### GitHub Actions
Automatic deployment on push to `main` branch via `.github/workflows/deploy.yml`

## Important Notes

1. **Migrations Auto-Apply**: Migrations are automatically applied on startup. Do not manually run `dotnet ef database update` in production.

2. **Basecoat UI**: Use Basecoat UI via CDN. Do not install Tailwind separately.

3. **Forwarded Headers**: Configured for Fly.io proxy. `KnownNetworks` and `KnownProxies` are cleared to trust all proxies.

4. **Test User Login**: Only enabled when both `TestUser__Email` and `TestUser__Password` environment variables are set.

5. **Scoring Calculators**: Each scoring system has its own calculator in `Shared/Scoring/`. The factory pattern is used to select the appropriate calculator.

6. **Event Publishing**: Every command handler publishes an event after successful completion (e.g., `CreateUserHandler` publishes `UserCreated`).

## Common Tasks

### Adding a New Feature
1. Create folder in `Features/<FeatureName>/`
2. Add `Commands/`, `Queries/`, and `Events/` subfolders
3. Create command/query records and handlers
4. Create Razor Page in `Pages/<FeatureName>/`
5. Add tests in `Idasletten.Tests/Features/<FeatureName>/`

### Adding a New Scoring System
1. Create calculator in `Shared/Scoring/<SystemName>ScoringCalculator.cs`
2. Implement `IScoringCalculator` interface
3. Add to `ScoreSystem` enum in `Models/ScoreSystem.cs`
4. Update `ScoringCalculatorFactory.GetCalculator()` to return new calculator

### Creating a Migration
```bash
dotnet ef migrations add <Name> -p Idasletten/Idasletten.csproj -o Data/Migrations
```

## Troubleshooting

### Build Errors
- Ensure all NuGet packages are restored: `dotnet restore`
- Check for missing using statements
- Verify Moserware.Skills compatibility (it's a .NET Framework package but works with .NET 10)

### Database Errors
- Delete `idasetten.db` and restart the app to recreate
- Check migration history: `dotnet ef migrations list -p Idasletten/Idasletten.csproj`

### Test Failures
- Tests use in-memory database, so no file cleanup needed
- Each test gets a fresh database via `CustomWebApplicationFactory`
- Check seed data in `CustomWebApplicationFactory.SeedData()`
