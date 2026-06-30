# AGENTS.md

Agent/developer guide for the Idasletten table-football tournament app. See `CLAUDE.md` (or
`../prompt.md`) for the full product spec this implementation follows.

## Tech stack

- ASP.NET Core 10, Razor Pages.
- Entity Framework Core + SQLite.
- MediatR (CQRS, vertical slices).
- ASP.NET Core Identity (`User : IdentityUser<Guid>`) as the entity/store shape; actual
  sign-in is hand-rolled cookie auth (see [Authentication](#authentication)).
- `Moserware.Skills.Core` for TrueSkill.
- [basecoat](https://basecoatui.com) CSS/JS via CDN for components (`.btn`, `.card`,
  `.dialog`, `.field`, `.table`, etc). **Important correction to the spec's assumption:**
  `basecoat.cdn.min.css` only ships those component classes — it does *not* bundle
  Tailwind-style layout/spacing utilities (`.flex`, `.grid`, `.gap-*`, `.text-*`, responsive
  `sm:`/`md:` variants, ...), even though basecoat's own docs examples use them. Per spec we
  still don't install Tailwind; instead `wwwroot/css/site.css` defines a small hand-rolled
  utility layer (same class names, plain CSS) so the rest of the markup can keep using
  Tailwind-like classes. If you add a new utility class to a page, add the matching rule to
  `site.css` or it will silently no-op.

## Architecture

```
Idasletten/
  Pages/        Razor Pages — thin, only send commands/queries via MediatR (ISender)
  Features/     One folder per slice (Users, Tournaments, TournamentPlayers, Matches, Rounds).
                Each slice owns its entity/entities plus Commands/<Name>/ and Queries/<Name>/
                subfolders. Every command handler publishes an event at the end.
  Shared/
    Data/       IdaslettenDbContext, DbSeeder
    Scoring/    IScoreCalculator + one implementation per ScoreSystem, ScoreRecalculator
    Auth/       Cookie/Azure AD/test-login plumbing, Graph avatar fetch
Idasletten.Tests/
  CustomWebApplicationFactory   boots Program.cs as-is in the Development environment
  TestData/Any.cs               random test-data builders
  Features/<Slice>/...Tests.cs  xUnit, AAA, Should_X_When_Y naming
```

Handlers use `IdaslettenDbContext` directly — no repository layer. Query handlers return DTOs,
never EF entities, so Razor Pages never depend on the EF model directly.

## Running locally

```
dotnet run --project Idasletten
```

No connection string is configured for local/Development — the app opens a single
long-lived in-memory SQLite connection (`Program.cs`), so the schema and seed data
(`Shared/Data/DbSeeder.cs`) are recreated fresh every run. Production sets
`ConnectionStrings:Default` to a file path, which switches the app to a persistent
file-based database (with the same seeding skipped).

To exercise the test-only login, set both:

```
TestUser__Email=you@example.com
TestUser__Password=whatever
```

`DbSeeder` only seeds a user matching `TestUser__Email` when these are set, so the test
login on `/login` (next to the disabled Microsoft button if Azure AD isn't configured) has
someone to sign in as.

## Migrations

**Always create migrations with the `dotnet ef` CLI**, from `Idasletten/`:

```
dotnet ef migrations add <Name> -o Migrations
```

**Migrations are applied automatically on startup** (`db.Database.Migrate()` in
`Program.cs`) — never apply them manually as a deployment step.

## Testing

```
dotnet test
```

`CustomWebApplicationFactory` runs the real `Program.cs` startup (Development environment,
in-memory SQLite, auto-migrate, auto-seed) rather than re-wiring DI — each factory instance
gets its own isolated, pre-seeded database. Most feature tests resolve `ISender` +
`IdaslettenDbContext` from a `factory.Services.CreateScope()` and drive the app through
MediatR commands/queries directly; a few smoke tests go through `factory.CreateClient()` to
check routing/auth at the HTTP level.

Test data: `Idasletten.Tests/TestData/Any.cs` (e.g. `Any.User()`, `Any.Tournament()`) for
random valid values. Naming: `Should_DoSomething_When_ConditionIsFulfilled`, AAA-structured.

**Gotcha — self-posting forms and antiforgery:** Razor's `FormTagHelper` only auto-injects
the antiforgery hidden field when the `<form>` tag has an `asp-page`, `asp-page-handler`,
`asp-action`, or explicit `asp-antiforgery="true"` attribute. A plain
`<form method="post">`/`<form method="post" action="/some-path">` with none of those silently
omits the token and 400s on submit — this broke the navbar logout form and the
create-tournament/create-match forms during initial Playwright validation (forms with
`asp-page-handler` were unaffected). `PagesSmokeTests.Should_HaveAnAntiforgeryTrigger_When_APageHasASelfPostingForm`
scans every `.cshtml` for this pattern; if it fails, add `asp-antiforgery="true"` (or an
`asp-page`/`asp-page-handler`/`asp-action`) to the flagged form.

## Authentication

- Cookie scheme is the only sign-in mechanism; both login paths end by signing a
  `ClaimsPrincipal` into it directly (no ASP.NET Core Identity `SignInManager`/external-login
  linking — see `Shared/Auth/AzureAdSignInHandler.cs`). `ClaimTypes.NameIdentifier` always
  carries our own `User.Id` (Guid), regardless of which login path was used.
- **Azure AD**: registered as the `"AzureAD"` OpenID Connect scheme only when
  `AzureAd:ClientId` is configured (`appsettings.json` / `AzureAd__*` env vars) — otherwise
  `OpenIdConnectOptions` validation throws on *every* request, not just sign-in attempts. The
  "Log ind med Microsoft" button is disabled when not configured.
- **Test login**: only registered/visible when `TestUser__Email` and `TestUser__Password` are
  both set; compares the submitted form values against those two env vars and, on match,
  signs in as the seeded user with that email. Not real authentication — for local dev and
  Playwright/E2E use only.
- `[Authorize]` is on the create-tournament POST handler. Editing an already-`Done` match
  (`Pages/Tournaments/CreateMatch.cshtml.cs`) checks `User.Identity.IsAuthenticated` in code
  rather than via the attribute, since the same page/handler also serves the anonymous
  create/plan flow.
- **Graph avatar fetch**: best-effort only, wrapped so a missing/failed Graph call never
  breaks login (`Shared/Auth/GraphAvatarFetcher.cs`). Only attempted for real Azure AD
  sign-ins with an access token (`User.Read` scope) — never for the test user or for players
  auto-created from typed initials. Not exercised by any automated check in this repo (no
  real Azure tenant available here); verify manually against a real Azure AD app
  registration with Graph permissions if you need to confirm it.

## Scoring systems

`Tournament.ScoreSystem` selects an `IScoreCalculator` (`Shared/Scoring/`). Every score-
affecting write (`SaveMatchCommand`, with `RecordResult: true`) ends by calling
`ScoreRecalculator.RecalculateAsync`, which resets every `TournamentPlayer`'s aggregate
fields and replays all `Done` matches (in `Order`) through the calculator from scratch. This
applies equally to a brand-new result and to editing a previously-Done match — there's no
"undo" path for a single match, which matters because TrueSkill updates aren't reversible.

- **Elo** — start 1200, K=32. Team rating = average of member ratings. More than two teams in
  one match: every team pair plays a virtual head-to-head (ranked by net goals), each
  pairwise delta scaled by `1/(teamCount-1)`.
- **TrueSkill** — `Moserware.Skills.Core`, `GameInfo.DefaultGameInfo`. `TournamentPlayer`
  only has a single `Score` double (no mu/sigma columns), so `TrueSkillScoreCalculator` keeps
  its own working `Dictionary<PlayerId, Rating>` for the lifetime of one
  `ScoreRecalculator` pass (a fresh calculator instance per call) and writes the conservative
  rating (`mu - 3*sigma`) to `Score` after each match.
- **Lives** — `Score = Lives`, default 3, floored at 0. A loss (lowest net goals) costs one
  life; a win doesn't restore any.
- **WinCount** — `Score = WinCount`. Tie-break for any ranked display is goal difference
  (`PointsWon - PointsLost`).

Win/loss for all systems is determined by net goals (`GoalsWon - GoalsLost`) per team, not by
the raw `GoalsWon` value — relevant once more than two teams are in a match.

## Other resolved spec ambiguities

- **"Create and Plan"** (`/tournaments/create`) navigates to `/tournaments/{id}/matches`
  after creating the tournament.
- **Plan several matches**: each round forms teams from the *entire* current player pool
  (dropping any remainder that doesn't fill a full team), then pairs adjacent teams into
  matches. `FixedTeams` computes rosters once and repeats them every round; otherwise each
  round re-derives rosters (a fresh shuffle for Random, a rotated ranking for Equality/Fair so
  teammates vary round to round while still following the balancing rule). A player isn't
  guaranteed exactly `GamesPerPlayer` games if the pool size doesn't divide evenly — this is a
  deliberate simplification given the spec doesn't define partial-pool handling.
- **Create-match route**: `/tournaments/{tournamentId}/create-match/{matchId}` — the "Create
  match" button POSTs to create a blank `Planned` match first, then redirects here, so the
  same page/route serves a brand-new match, a pre-filled planned match, and a read-only
  (or, if logged in, editable) completed match.

## Known/accepted gaps

- `SQLitePCLRaw.lib.e_sqlite3` (a transitive dependency of `Microsoft.EntityFrameworkCore.Sqlite`)
  has an open, currently unfixed security advisory (GHSA-2m69-gcr7-jv3q / CVE-2025-6965) as of
  this writing — there is no patched version to move to yet.
- Graph avatar fetch (see above) isn't covered by any automated test in this repo.

## Deployment

- Fly.io app name: **`idasletten-sonnet5`** (the directory name, per the spec's build
  convention).
- `fly.toml` mounts a persistent volume for the SQLite file
  (`ConnectionStrings__Default=Data Source=/data/idasletten.db`).
- `.github/workflows/deploy.yml` runs `flyctl deploy` on push to `main`. Requires a
  `FLY_API_TOKEN` repository secret (not set up by this change — add it via
  `fly tokens create deploy` and the repo's Settings → Secrets).
- `UseForwardedHeaders` (in `Program.cs`) clears `KnownNetworks`/`KnownProxies` so ASP.NET
  Core trusts Fly's proxy and generates `https://` redirect URIs for Azure AD.
