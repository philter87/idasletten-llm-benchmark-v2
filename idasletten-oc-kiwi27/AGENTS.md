# AGENTS.md — Idasletten

## Project overview

Idasletten is a table-football tournament web app built with ASP.NET Core Razor Pages, Entity Framework Core, SQLite and MediatR. Authentication is optional; Azure AD is supported, and a test-only local login is available when `TestUser__Email` and `TestUser__Password` are configured.

## Tech stack

- **Runtime / language**: .NET 8, C#
- **Web framework**: ASP.NET Core Razor Pages
- **Database**: SQLite (in-memory locally, file-based in production)
- **ORM**: Entity Framework Core 8
- **Architecture**: CQRS with vertical slices via MediatR
- **UI**: Basecoat CSS + JS via CDN (light/white theme, Tailwind is included in the CDN bundle)
- **Auth**: ASP.NET Core Identity, Azure AD OpenID Connect, cookie authentication
- **Rating algorithms**: Elo (custom), TrueSkill (Moserware.Skills.Core), Lives, WinCount
- **Tests**: xUnit, FluentAssertions, `WebApplicationFactory`

## Project structure

```
Idasletten/
  Features/
    Tournaments/     Commands, queries, entities
    Players/         Tournament-player commands/queries/entities
    Matches/         Match commands/queries/entities
    Users/           User commands/queries/entities
    Scoring/         IScoreCalculator implementations and recalculation logic
  Shared/
    Data/            ApplicationDbContext, migrations, DbInitializer
    Auth/            Authentication and forwarded-headers helpers
  Pages/             Razor pages (thin, send MediatR commands/queries)
Idasletten.Tests/
  Any/               Test data factory helpers
  Factories/         Custom WebApplicationFactory
  Integration/       Integration/feature tests
```

Rules for the codebase:
- Razor Pages contain minimal logic; they delegate to MediatR handlers.
- Handlers live in the corresponding `Features/<slice>/Commands` or `Queries` folder.
- Handlers may use `ApplicationDbContext` directly; avoid repository/service layers.
- Every command handler publishes a domain notification/event at the end.
- EF migrations are the only schema-management mechanism.

## Running locally

```bash
dotnet tool restore              # restores dotnet-ef
dotnet build
dotnet run --project Idasletten
```

The app runs on `http://localhost:5000` by default.

### Local database

If `ConnectionStrings__DefaultConnection` is **not** set, the app uses a shared in-memory SQLite connection (`DataSource=idasletten-dev;mode=memory;cache=shared`). This is intended for local development.

If you want a persistent local file instead, set it before running:

```bash
$env:ConnectionStrings__DefaultConnection="DataSource=idasletten-local.db"
dotnet run --project Idasletten
```

### Test login

When both `TestUser__Email` and `TestUser__Password` are set, a test user is seeded and a second login button appears on `/login`.

```bash
$env:TestUser__Email="test@idasletten.local"
$env:TestUser__Password="Test1234!"
dotnet run --project Idasletten
```

## Migrations

> **Rule**: always create migrations with the `dotnet ef` CLI and apply them automatically on app startup.

Create a new migration:

```bash
dotnet dotnet-ef migrations add <MigrationName> --project Idasletten --output-dir Shared/Data/Migrations
```

Migrations are applied automatically by `DbInitializer.SeedAsync`, which calls `db.Database.MigrateAsync()` on startup.

## Testing

Run all tests:

```bash
dotnet test
```

- Tests use a custom `WebApplicationFactory` with an in-memory SQLite database.
- A test-only `/test-login` endpoint exists only in the `Testing` environment and signs in the seeded test user.
- Test data factories are in `Idasletten.Tests/Any/Any.cs`.

## Azure AD configuration

Set these environment variables (or use `AzureAd` in configuration):

```bash
$env:AzureAd__TenantId="<tenant-id>"
$env:AzureAd__ClientId="<client-id>"
$env:AzureAd__ClientSecret="<client-secret>"
```

On first login, a local `AppUser` is created from the Azure claims and the profile photo is fetched from Microsoft Graph when possible.

## Deployment

The app is configured for Fly.io.

1. Install `flyctl` and authenticate.
2. Create the app and volume once:

```bash
fly apps create idasletten
fly volumes create data --region arn --size 1 --app idasletten
```

3. Set required secrets:

```bash
fly secrets set AzureAd__TenantId=<...> AzureAd__ClientId=<...> AzureAd__ClientSecret=<...>
```

4. Deploy:

```bash
fly deploy
```

A GitHub Action in `.github/workflows/fly-deploy.yml` builds, tests and deploys on every push to `main`. It requires a `FLY_API_TOKEN` repository secret.

## Forwarded headers

`UseForwardedHeaders` is configured with `KnownNetworks` and `KnownProxies` cleared so ASP.NET Core trusts Fly.io's proxy and generates `https://` redirect URIs for Azure AD.
