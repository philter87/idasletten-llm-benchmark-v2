# Idasletten Agent Notes

## Stack
- ASP.NET Core Razor Pages, C#, EF Core, SQLite.
- CQRS/vertical slices with MediatR. Razor Page models should stay thin and send commands/queries.
- UI uses basecoat from CDN. Do not install Tailwind; the CDN bundle includes what is needed.
- Light theme only.

## Architecture
- `Pages`: Razor pages and page models.
- `Features`: vertical slices. Commands and queries live under feature folders. Handlers may use `IdaslettenDbContext` directly; avoid repositories unless a real cross-slice abstraction appears.
- `Shared`: cross-cutting data, auth, scoring, and infrastructure.
- Every command handler should publish a MediatR notification at the end.

## Data
- Local development uses SQLite in-memory mode with a shared open connection.
- Production uses file SQLite. Configure `ConnectionStrings__Default`; Fly.io should normally use a `/data/idasletten.db` volume path.
- Migrations are created with `dotnet ef` CLI.
- Migrations are automatically applied on startup. Keep this rule intact.
- Seed data is used for local runs and test factories.

## Auth
- Browsing and recording new match results do not require login.
- Creating tournaments requires login.
- Editing already completed matches requires login because scoring is recalculated.
- Azure AD uses OpenID Connect when `AzureAd__TenantId` and `AzureAd__ClientId` are configured.
- Test login appears only when both `TestUser__Email` and `TestUser__Password` are set.
- Forwarded headers are configured with known networks/proxies cleared for Fly.io HTTPS redirects.

## Run locally
```bash
dotnet restore
dotnet ef database update --project Idasletten/Idasletten.csproj
dotnet run --project Idasletten/Idasletten.csproj
```

## Test
```bash
dotnet test
```

## Deployment
- Fly app name should use the folder name unless explicitly changed.
- GitHub Actions deploys `main` to Fly.io using `FLY_API_TOKEN`.
