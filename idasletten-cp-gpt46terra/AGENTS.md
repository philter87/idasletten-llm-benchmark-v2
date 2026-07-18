# Idasletten contributor guide

## Stack and structure

- .NET 10, C#, Razor Pages, Entity Framework Core, SQLite, MediatR, and xUnit.
- `Idasletten/Pages` contains thin Razor Page models: they send commands and queries through MediatR only.
- `Idasletten/Features` contains CQRS vertical slices. Handlers access `IdaslettenDbContext` directly; do not add repositories. Each command handler must publish an event after it persists its change.
- `Idasletten/Shared` holds the EF model, database context, scoring support, and cross-feature code.
- Use the Basecoat CDN plus the app's light-theme CSS. Do not add Tailwind.

## Database and migrations

- Development defaults to SQLite in-memory when no `ConnectionStrings__Idasletten` is supplied. Production uses SQLite at the configured connection string.
- **Migrations automatically apply on application startup.** Create them only through `dotnet ef migrations add <Name> --project Idasletten`.
- The app and test factory seed useful local data. Test login is enabled only when both `TestUser__Email` and `TestUser__Password` are configured.

## Commands

```bash
dotnet run --project Idasletten
dotnet test Idasletten.slnx
dotnet ef migrations add <Name> --project Idasletten
```

For local test login, run with `TestUser__Email=test@example.com TestUser__Password=secret`. Azure AD is configured by `AzureAd__TenantId`, `AzureAd__ClientId`, and optionally `AzureAd__ClientSecret`.

## Deployment

The Fly application name is the repository folder name: `idasletten-cp-gpt46terra`. Deploy using `fly deploy`; GitHub Actions deploys `main` when `FLY_API_TOKEN` is configured as a repository secret.
