# Idasletten - Development Guide

## Tech Stack
- C# / .NET 8.0
- Entity Framework Core + SQLite (in-memory locally, file-based in production)
- Razor Pages
- MediatR (CQRS + vertical slices)
- basecoat UI (CDN: https://basecoatui.com)
- xUnit for testing
- Azure AD authentication (optional)
- Moserware.Skills (TrueSkill scoring)

## Architecture
- **Pages/** — Razor Pages with minimal logic; send commands/queries via MediatR
- **Features/** — One folder per feature/slice, each containing handlers, commands, queries
- **Shared/** — Entities, enums, DbContext, events, seed data

### CQRS Rules
- Every command handler publishes an event (e.g., `CreateUserHandler` → `UserCreated`)
- Handlers use DbContext directly (no repositories/services layer)

## Running Locally
```bash
cd Idasletten
dotnet run
```

The app runs on `https://localhost:5001` and `http://localhost:5000`.
In Development mode, SQLite runs in-memory (`:memory:`) with auto-seeding.

## Migration Rules
- Always create migrations with `dotnet ef` CLI
- Migrations are applied automatically on app startup (`Program.cs`)

### Creating a Migration
```bash
dotnet ef migrations add MigrationName --project Idasletten
```

## Testing
```bash
cd Idasletten.Tests
dotnet test
```

- Integration/feature tests use custom `WebApplicationFactory` with in-memory database
- Test data factories: static `Any` class in the test project
- xUnit, AAA pattern
- Method naming: `Should_DoSomething_When_ConditionIsFulfilled`
- Prefer simple stubs over mocking frameworks

## Authentication
- Login is optional for general use
- Required for: creating tournaments, editing completed matches
- Azure AD via Microsoft.AspNetCore.Authentication.MicrosoftAccount
- Test user login enabled when `TestUser__Email` and `TestUser__Password` env vars are set

## Environment Variables
| Variable | Description |
|---|---|
| `TestUser__Email` | Email for test user login |
| `TestUser__Password` | Password for test user login |
| `AzureAd__TenantId` | Azure AD tenant ID |
| `AzureAd__ClientId` | Azure AD app client ID |
| `AzureAd__ClientSecret` | Azure AD app client secret |

## Deployment
- Deploy to Fly.io using `fly` CLI
- GitHub Action deploys `main` branch on push

## Database
- Local (Development): In-memory SQLite (`:memory:`)
- Production: File-based SQLite (`idasletten.db`)
