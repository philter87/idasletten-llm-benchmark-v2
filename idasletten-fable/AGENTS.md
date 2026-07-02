# Idasletten — agent guide

Table-football (foosball) tournament web app with a Norse-mythology theme.
Players, teams, matches, results and a configurable scoreboard.

## Tech stack

- **.NET 10**, C#, ASP.NET Core **Razor Pages** (rendering only, no logic).
- **Entity Framework Core + SQLite**. In-memory SQLite locally (shared-cache DB
  kept alive by a singleton open connection), file-based SQLite in production
  (`ConnectionStrings__Default`, on Fly.io a volume at `/data`).
- **MediatR** for CQRS.
- **Moserware.Skills** for TrueSkill (old .NET Framework package, runs fine on
  .NET 10 — expect the NU1701 restore warning, it is harmless).
- **basecoat** UI via CDN (`basecoat.cdn.min.css` + `all.min.js`). Do **not**
  install Tailwind — the CDN bundle is standalone. Arbitrary Tailwind utility
  classes are *not* available; page layout lives in `wwwroot/css/site.css`
  (flexbox helpers). Light/white theme.
- UI text is Danish, matching the mythology theme.

## Architecture — CQRS + vertical slices

```
Idasletten/
  Pages/       Razor Pages; page models only send MediatR commands/queries.
  Features/    One folder per slice (Tournaments, Matches, Users, Scoring),
               each with Commands/ and Queries/ subfolders plus its entities.
               Handlers use AppDbContext directly — no repositories/services.
  Shared/      AppDbContext, seeding, cross-slice helpers.
```

**Rules**

- Every command handler publishes a MediatR event (INotification) at the end,
  e.g. `CreateUserHandler` → `UserCreated`.
- Handlers may call other slices through `IMediator` (e.g. resolving initials
  goes through `AddPlayerToTournamentCommand` → `CreateUserCommand`), so the
  events always fire.
- Editing a `Done` match must trigger `RecalculateTournamentCommand`, which
  resets all players and replays every done match in `Order`.

## Migrations

- **Always create migrations with the `dotnet ef` CLI** (`cd Idasletten &&
  dotnet ef migrations add <Name>`).
- **Migrations are applied automatically on app startup** (`Database.Migrate()`
  in `Program.cs`). Never apply them manually in deployment steps.

## Run locally

```bash
cd Idasletten
dotnet run    # http://localhost:5095 (see Properties/launchSettings.json)
```

Local runs use the in-memory database and seed demo data (see
`Shared/SeedData.cs`): tournaments "Ragnarok Forår 2026" (Elo, public),
"Valhal Høst 2025" (WinCount, archived) and "Einherjernes Kamp" (Lives, private).

**Test-user login** (second login option next to Microsoft) is enabled only
when both env vars are set; the user is then also seeded into the database:

```bash
TestUser__Email=test@idasletten.local TestUser__Password=secret dotnet run
```

Azure AD login activates when `AzureAd__ClientId` / `AzureAd__TenantId` /
`AzureAd__ClientSecret` are configured. Profile images come from the Microsoft
Graph API when `Graph__TenantId`/`Graph__ClientId`/`Graph__ClientSecret` are set.

## Tests

```bash
dotnet test
```

- xUnit, AAA (Arrange/Act/Assert), names like `Should_DoSomething_When_ConditionIsFulfilled`.
- `TestWebApplicationFactory` boots the real app with a uniquely named shared
  in-memory SQLite DB per factory — migrated and seeded exactly like a local run.
- `Any` (Idasletten.Tests/Any.cs) creates test data with random values.
- Prefer simple stubs over mocking frameworks.
- No Playwright unit/integration tests — Playwright (MCP) is used for visual
  screenshot validation only.

## Deployment (Fly.io)

- App name **idasletten-fable** (= folder name), `fly.toml` + `Dockerfile` in
  this folder. Deploy manually with `fly deploy`.
- GitHub Actions workflow `.github/workflows/fly-deploy.yml` deploys `main`
  on push (needs the `FLY_API_TOKEN` secret). Note: workflows only trigger when
  the `.github` folder sits at the repository root.
- Fly's proxy terminates TLS; `Program.cs` configures `UseForwardedHeaders`
  with `KnownNetworks`/`KnownProxies` cleared so Azure AD redirect URIs are
  generated as `https://`.
- SQLite file lives on the `idasletten_data` volume mounted at `/data`.
