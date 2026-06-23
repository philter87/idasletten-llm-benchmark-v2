# Idasletten - Table Football Tournament App

> **Idasletten (Iðavöllr)** findes i nordisk mytologi lige uden for Valhals porte. Det er her vikingerne som er døde i krig udkæmper deres drabelige slag hver dag, som øvelse til Ragnarok. Hver aften er Idasletten rød af blod.

## Overview

Idasletten is a web application for managing table football (foosball) tournaments. It provides features for:

- Creating and managing tournaments
- Tracking players, teams, and matches
- Recording match results
- Calculating scores using different scoring systems (Elo, TrueSkill, Lives, WinCount)
- Displaying leaderboards and tournament statistics
- Multi-round tournament support with seeding

## Features

### Tournament Management
- Create tournaments with customizable settings (team size, points to win, scoring system)
- Public and private tournaments
- Archive completed tournaments
- Seed tournaments from previous tournaments
- Multi-round tournaments with player progression

### Scoring Systems
- **Elo**: Standard Elo rating system with team averages
- **TrueSkill**: Microsoft's TrueSkill system for skill-based matchmaking
- **Lives**: Each player starts with 3 lives, lose a life on each loss
- **WinCount**: Simple win counting with goal difference as tie-breaker

### Match Management
- Record match results with goals for each team
- Plan multiple matches at once with different seeding algorithms:
  - Random: Random team pairings
  - Equality: Best vs worst (1+N, 2+(N-1), ...)
  - Fair: Top half vs bottom half pairing

### Authentication
- Optional authentication for most features
- Required for creating tournaments and editing completed matches
- Azure AD integration for enterprise authentication
- Test user support for development

## Tech Stack

- **Framework**: ASP.NET Core 8.0
- **Language**: C# 11+
- **Database**: SQLite (in-memory for development, file-based for production)
- **ORM**: Entity Framework Core 8.0
- **UI**: Razor Pages with Basecoat UI via CDN
- **Architecture**: CQRS + Vertical Slices with MediatR
- **Authentication**: Azure AD, ASP.NET Core Identity
- **Scoring**: Custom implementations + Moserware.Skills library for TrueSkill

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Node.js (optional, for frontend tooling)

### Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/mjolner/idasletten.git
   cd idasletten
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build the solution**:
   ```bash
   dotnet build
   ```

4. **Run the application**:
   ```bash
   dotnet run --project Idasletten
   ```

5. **Access the application**:
   - Local: `http://localhost:5000`
   - HTTPS: `https://localhost:5001`

### Environment Variables

For development, you can configure the following environment variables:

```bash
# Test user login (development only)
TestUser__Email=test@idasletten.local
TestUser__Password=Test123!

# Azure AD (for production)
AzureAd__ClientId=your-client-id
AzureAd__ClientSecret=your-client-secret
AzureAd__TenantId=your-tenant-id
```

## Database & Migrations

### Local Development (In-Memory)

By default, the application uses SQLite in-memory database for development. No setup required.

### File-Based Database

To use a file-based SQLite database:

1. Update `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=idletten.db"
     }
   }
   ```

2. Create and apply migrations:
   ```bash
   # Create migration
dotnet ef migrations add InitialCreate --project Idasletten

   # Apply migration
dotnet ef database update --project Idasletten
   ```

Migrations are automatically applied on application startup for production databases.

## Project Structure

```
Idasletten/
├── Pages/                 # Razor Pages (minimal logic)
├── Features/             # Vertical slices
│   ├── Tournaments/       # Tournament-related commands/queries
│   ├── Players/          # Player-related commands/queries
│   ├── Matches/          # Match-related commands/queries
│   ├── Users/           # User-related commands/queries
│   └── Auth/            # Authentication commands/queries
├── Shared/               # Cross-cutting concerns
│   ├── Scoring/         # Scoring system implementations
│   └── AppDbContext.cs  # Entity Framework DbContext
├── wwwroot/             # Static files
├── Migrations/           # EF Core migrations
├── Program.cs           # Application entry point
├── Dockerfile           # Docker configuration
├── fly.toml             # Fly.io configuration
└── .github/             # GitHub Actions workflows
    └── workflows/
        └── deploy.yml    # Deployment workflow

Idasletten.Tests/
├── Features/             # Integration tests
├── TestInfrastructure/   # WebApplicationFactory, test helpers
└── Any.cs               # Test data factories
```

## Deployment

### Fly.io

1. **Install Fly.io CLI**:
   ```bash
   curl -L https://fly.io/install.sh | sh
   ```

2. **Create a new app**:
   ```bash
   fly create idasletten
   ```

3. **Set secrets**:
   ```bash
   fly secrets set AzureAd__ClientId=<your-client-id> \
                 AzureAd__ClientSecret=<your-client-secret> \
                 AzureAd__TenantId=<your-tenant-id>
   ```

4. **Deploy**:
   ```bash
   fly deploy
   ```

The app will be available at the URL provided by Fly.io.

### GitHub Actions

A GitHub Actions workflow is included that:
- Runs tests on push and pull requests
- Deploys to Fly.io when pushing to the `main` branch

## Architecture

### CQRS + Vertical Slices with MediatR

- Each feature is organized in its own folder
- Commands and queries are separate classes
- Handlers contain the business logic
- Pages send commands/queries via MediatR
- Every command handler publishes a domain event

### Example Command/Handler

```csharp
// Command
public class CreateTournamentCommand : IRequest<Guid>
{
    public string Name { get; set; }
    public int TeamSize { get; set; } = 2;
    // ... other properties
}

// Handler
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
        var tournament = new Tournament { /* ... */ };
        _context.Tournaments.Add(tournament);
        await _context.SaveChangesAsync(ct);
        
        // Always publish event
        await _publisher.Publish(new TournamentCreated(tournament.Id), ct);
        
        return tournament.Id;
    }
}

// Event
public record TournamentCreated(Guid TournamentId) : INotification;
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Test Structure

- **xUnit** testing framework
- **AAA pattern** (Arrange, Act, Assert)
- **Custom WebApplicationFactory** with in-memory database
- **Test data factories** in `Any.cs`
- **Integration tests** for MediatR handlers

### Example Test

```csharp
[Fact]
public async Task Should_CreateTournament_When_UsingMediatR()
{
    // Arrange
    var command = new CreateTournamentCommand
    {
        Name = "Test Tournament",
        TeamSize = 2,
        PointsToWin = 5,
        ScoreSystem = ScoreSystem.Elo
    };

    // Act
    var tournamentId = await mediator.Send(command);

    // Assert
    Assert.NotEqual(Guid.Empty, tournamentId);
    
    var tournament = await context.Tournaments.FindAsync(tournamentId);
    Assert.NotNull(tournament);
    Assert.Equal("Test Tournament", tournament.Name);
}
```

## UI & Design

### Basecoat UI

The application uses [Basecoat UI](https://basecoatui.com/) via CDN for styling:

```html
<link rel="stylesheet" href="https://cdn.basecoatui.com/latest/basecoat.min.css" />
```

### Design Guidelines

- **Theme**: Light/white theme
- **Layout**: Flexbox
- **Colors**: Norse mythology-inspired color scheme
- **Components**: Basecoat UI components

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -m 'Add new feature'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Open a pull request

### Development Guidelines

- Follow the existing architecture patterns
- Every command handler MUST publish a domain event
- Use MediatR for all business logic
- Pages should contain minimal logic
- Use the scoring service for score calculations
- Write tests for new features

## License

This project is proprietary and intended for use by Mjølner.

## Contact

- **Project Lead**: [Your Name]
- **Email**: [Your Email]
- **Organization**: [Mjølner]

## Additional Documentation

- [AGENTS.md](AGENTS.md) - Development guide for AI agents
- [Architecture Decisions](docs/architecture.md) - (To be created)
- [API Documentation](docs/api.md) - (To be created)

---

*Built with ❤️ at Mjølner using ASP.NET Core, C#, and a passion for foosball.*
