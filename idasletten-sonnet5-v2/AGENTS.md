# AGENTS.md — Idasletten

Table-football tournament tracker with a Norse-mythology theme. This file documents the tech
stack, architecture, and operational rules for anyone (human or agent) working in this repo.

## Tech stack

- **C# / ASP.NET Core 10** (net10.0), Razor Pages.
- **Entity Framework Core** with **SQLite**.
- **MediatR 12.x** (deliberately pinned below v13 — v13+ requires a commercial license for
  production use; 12.x is the last MIT-licensed version).
- **Microsoft.Identity.Web** for Azure AD sign-in.
- **basecoat** UI via CDN (no npm/build step) + plain flexbox for layout. Light theme only.
- **Moserware.Skills.Core** for TrueSkill.
- **xUnit** for tests.

## Architecture — CQRS + vertical slices

- `Pages/` — Razor Pages. Thin: a page's code-behind only sends MediatR commands/queries and
  shapes the response. No business logic here.
- `Features/<Feature>/Commands|Queries/<Name>/` — one folder per command/query, containing its
  request record, handler, and (for commands) the notification it publishes. Handlers use
  `IdaslettenDbContext` directly — no repository layer. **Every command handler publishes an
  event at the end** (e.g. `CreateUserHandler` → `UserCreated`).
- `Shared/` — cross-cutting code that isn't specific to one feature: `Entities/` (the EF
  entities), `Auth/` (sign-in helpers, username generation), `Scoring/` (the four
  `IScoreSystemStrategy` implementations and `TournamentScoreRecalculator`).
- `Data/` — `IdaslettenDbContext`, EF migrations, and `DataSeeder`.

## Database & migrations

- **Local development**: SQLite **in-memory** (`DataSource=:memory:` behind a single kept-alive
  `SqliteConnection`), so restarting the app gives you a clean, freshly-migrated,
  freshly-seeded database every time. This is the default whenever `ASPNETCORE_ENVIRONMENT`
  is `Development` and no `ConnectionStrings:Default` is configured.
- **Production**: a real SQLite file, path set via `appsettings.Production.json`
  (`/data/idasletten.db`) backed by a Fly.io volume mount.
- **Tests**: the same in-memory-SQLite approach, via `IdaslettenWebApplicationFactory` (see
  below) — deliberately *not* EF Core's `InMemory` provider, so tests exercise the same
  relational engine, constraints, and LINQ translation as production.
- **Migrations are always created with the `dotnet ef` CLI** (`dotnet ef migrations add <Name>
  -o Data/Migrations`, run from the `Idasletten/` folder), never hand-written.
- **Migrations are applied automatically on startup** (`db.Database.Migrate()` in
  `Program.cs`), for every environment including production. There is no manual migration
  step in deployment.

## Authentication

- Anonymous users can browse everything and record match results. Login is required only to
  **create a tournament** and to **edit an already-`Done` match**.
- Two sign-in options on `/login`:
  - **Microsoft (Azure AD)** via `Microsoft.Identity.Web`. `AzureAd` config needs real
    `TenantId`/`ClientId` values (via user secrets or environment variables) to work against a
    real tenant — the checked-in `appsettings.json` only has placeholder GUIDs.
  - **Test user** — only shown/enabled when both `TestUser__Email` and `TestUser__Password`
    are set (env vars, `TestUser:Email`/`TestUser:Password` in config). This user is also
    seeded into the database on startup (`DataSeeder`).
- Both flows land on the **same cookie scheme** (`CookieAuthenticationDefaults`); the OIDC
  handler is only used to *challenge* (redirect to Azure AD) — `[Authorize]` failures redirect
  to our own `/Login` page (`CookieAuthenticationOptions.LoginPath`), not straight to Microsoft.
- `Shared/Auth/AzureAdUserProvisioning` maps an Azure AD sign-in to a domain `User` (matched by
  email, created via `CreateUserCommand` if new) and best-effort fetches the profile photo from
  Microsoft Graph (`GraphProfilePhotoFetcher` — silently no-ops if Graph access isn't available,
  which it won't be without real Azure AD credentials).
- **Fly.io forwarded headers**: `ForwardedHeadersOptions.KnownIPNetworks`/`KnownProxies` are
  cleared so `https://` redirect URIs are generated correctly behind Fly's proxy.

## Scoring systems

`Shared/Scoring/IScoreSystemStrategy` + `TournamentScoreRecalculator`. On every match
create/edit, **all of a tournament's player stats are recomputed from scratch** by replaying
every `Done` match in `Order`, rather than incrementally patching — this keeps every system
correct even when an older match's result is edited (which requires login; see above).

- **Elo**: K=32. Team rating = average of its players' `Score`. Generalizes to N teams via
  pairwise round-robin comparison, normalized by `teamCount - 1`.
- **TrueSkill**: `Moserware.Skills.Core`, default `GameInfo`. Multi-player teams use the
  library's native per-player team support (not an average). Ratings (mu/sigma) are kept in a
  strategy-instance-local dictionary that only lives for one recompute pass.
- **Lives**: starts at 3, `-1` on any loss (floors at 0); `Score` mirrors `Lives`.
- **WinCount**: `Score` mirrors `WinCount`; tie-breaking by goal difference
  (`PointsWon - PointsLost`) happens at query/sort time, not in `Score` itself.

## "Plan several matches" seeding

`Features/Matches/Commands/PlanSeveralMatches` + `TeamSeeder`. `Equality` (best-vs-worst) and
`Fair` (top-half-vs-bottom-half) are defined by the spec via 2-player-team examples; for team
sizes other than 2 they fall back to simple ranked chunking rather than guessing a
generalization. `FixedTeams` forms teams once and rotates the opponent pairing across rounds;
otherwise teams are re-formed every round.

## Known simplifications / open items

- **Azure AD credentials**: `appsettings.json` has placeholder `AzureAd` GUIDs. Set real
  `TenantId`/`ClientId` (and consent the Graph `User.Read` scope, if the photo-fetch feature
  matters) via user secrets locally or Fly secrets in production before relying on the
  Microsoft login button.
- **Logout** only signs out of the local cookie; it doesn't end the federated Azure AD session,
  so a user could get silently signed back in via SSO. Acceptable for now given the app's scale.
- **"Create and Plan"** on `/tournaments/create` (spec flagged this as needing clarification)
  navigates to `/tournaments/{id}/matches` — plan matches immediately after creating.
- **Removing a player** from the "add from previous tournament" picker isn't implemented (the
  UI shows a struck-through, disabled "Added" state instead of a working "−" button) — removing
  a player who might already be in recorded matches is a bigger operation than this flow needs.
- **"Select from list" checkbox dialogs** (create-match, per spec) are implemented; the
  "previous tournament" picker on `/players` uses a plain `<select>` + button instead of a
  basecoat dropdown-menu component, for simplicity.
- `SQLitePCLRaw.lib.e_sqlite3` has an open NU1903 advisory at the pinned EF Core version; no
  fixed version was available at build time.

## Running locally

```bash
cd Idasletten
dotnet run
```

Opens on the URL(s) in `Properties/launchSettings.json`. To enable the test-user login:

```bash
TestUser__Email=test@example.com TestUser__Password=Test1234! dotnet run
```

## Testing

```bash
cd Idasletten.Tests
dotnet test
```

- `TestSupport/IdaslettenWebApplicationFactory` — custom `WebApplicationFactory<Program>`
  backed by an in-memory SQLite connection; Program.cs's own migrate+seed startup code runs
  against it unmodified.
- `TestSupport/Any` — test data factory (`Any.User()`, `Any.Tournament()`, ...): every method
  fills in every field with a random value so tests only need to override what they care about.
- Naming: `Should_DoSomething_When_ConditionIsFulfilled`. AAA (Arrange/Act/Assert) comments in
  every test body. Plain xUnit `Assert`s — no mocking framework; tests either call MediatR
  handlers directly through a DI scope (most of them) or hit pages over HTTP via
  `factory.CreateClient()` (a few page-access/authorization smoke tests).

## Deployment

- **Fly.io**, app name `idasletten-sonnet5-v2` (folder name), region `arn`. Config: `fly.toml`
  (repo root) + `Idasletten/Dockerfile`. A persistent volume (`idasletten_data`, mounted at
  `/data`) holds the production SQLite file.
- First-time setup (not yet done in this environment — `fly` CLI wasn't authenticated when
  this was built):
  ```bash
  fly auth login
  fly volumes create idasletten_data --region arn --size 1
  fly secrets set AzureAd__TenantId=... AzureAd__ClientId=... AzureAd__ClientCredentials__0__ClientSecret=...
  fly deploy
  ```
- **GitHub Actions** (`.github/workflows/deploy.yml`): runs `dotnet test`, then deploys to
  Fly.io on every push to `main`. Requires a `FLY_API_TOKEN` repo secret
  (`fly tokens create deploy` → add as a GitHub Actions secret).
