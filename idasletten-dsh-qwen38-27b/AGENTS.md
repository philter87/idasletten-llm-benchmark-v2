# AGENTS.md — Idasletten

Table-football (foosball) tournament web app with a Norse-mythology theme. Spec: `plan.md`.

## Tech stack

- **C# / .NET 10**, ASP.NET Core **Razor Pages** (server-rendered, no SPA).
- **EF Core + SQLite**: in-memory database locally (Development), file-based in production.
- **basecoat** UI via CDN (`basecoat.cdn.min.css` + `dist/js/all.min.js`). Do **not** install
  Tailwind — it is included in the CDN bundle. Layout is **flexbox**; the theme is **light** ("white").
- **CQRS + vertical slices** with **MediatR** (14.x).
- **xUnit** for tests (`Idasletten.Tests`).
- **Fly.io** deployment via the `fly` CLI + a GitHub Action that deploys `main` on push
  (app name = folder name: `idasletten`, region `osl`).

## Project layout

    Idasletten.sln
    Idasletten/
      Pages/      Razor pages — minimal logic; they only send commands/queries via MediatR.
      Features/   One folder per vertical slice (Users, Tournaments, Players, Matches), each
                  with Commands/ and Queries/ sub-folders. Handlers may use DbContext directly
                  (no repositories/services). Every command handler PUBLISHES A DOMAIN EVENT AT
                  THE END (e.g. FindOrCreateUser -> UserCreated; UserCreated also triggers a
                  best-effort Azure Graph avatar fetch).
      Shared/     Everything not specific to one feature:
                  Models/ (EF entities + enums), Data/ (AppDbContext, migrations, seed data),
                  Scoring/ (the four score systems + the replay facade), Auth/ (cookie scheme,
                  password hasher, test-user options), ThirdParty/ (vendored Moserware.Skills).
    Idasletten.Tests/
                  Any.cs (test data factory), TestWebApplicationFactory.cs, TestDb.cs,
                  Scoring/, CQRS tests, Pages/ (TestServer page tests).

## Rules (from the spec — keep them)

- **Migrations are always created with the `dotnet ef` CLI and applied automatically on app
  startup** (`db.Database.MigrateAsync()` in `Program.cs`, before the app starts). Never edit
  applied migrations by hand; add a new migration when the model changes.
- **Authentication is optional.** Anyone can browse and record NEW match results without logging
  in. Login (Azure AD when configured, test login when configured) is required ONLY for:
  (1) creating tournaments (`/tournaments/create`), (2) editing already-Done matches.
  The gate exists in two layers: the page (redirect to `/login?returnUrl=...`) and the
  `RecordMatchResult` command handler (throws `FeatureException` for unauthenticated Done-edits).
- **Forwarded headers (Fly.io):** `UseForwardedHeaders` with `KnownNetworks` and `KnownProxies`
  **cleared**, so ASP.NET Core trusts Flys proxy and generates `https://` redirect URIs for
  Azure AD.
- **The in-memory DB reseeds with fresh Guids on every local restart.** Any scripted check
  (curl, Playwright) must re-resolve tournament/user IDs by name from page HTML after each restart.
- **Editing a finished match = rewrite the result and fully replay the tournament matches**
  (no undo bookkeeping). The authoritative scoreboard is always the replay.
- **A tournament may be seeded only if it has no parent.** Child rounds (ParentTournamentId)
  cannot be seeded/planned in bulk; their matches are planned from the carried-over players.
- **Multi-round flow:** a child tournament RoundNumber = parent + 1 (root = 1); parent players
  can be carried over (scores reset through the scoring engine); children are hidden from
  `/tournaments` by default (`?includeChildren=true` shows them).
- **Zero-lives players** (Lives system) are excluded from auto-planning and blocked from manual
  matches. **Archived tournaments block all mutations.**
- **Unknown initials auto-create** a `User` (username = trimmed, upper-cased initials, 2-20
  chars, unique) and a `TournamentPlayer` everywhere players are entered.

## Scoring systems

`Shared/Scoring` — four engines behind `IScoringEngine`, orchestrated by the `ScoringEngine`
facade. Initial state: Elo 1500, TrueSkill mu 25 / sigma 25/3 (displayed with 2 decimals),
Lives 3, WinCount 0.

- **Elo** — normal Elo, K=32; a multi-player team rating = the average of its members.
- **TrueSkill** — the vendored **Moserware.Skills** library (`ThirdParty/Skills`); teams are
  ranked by goals (ties repeat the rank); mu is stored in `Score`, sigma in `TrueSkillSigma`.
- **Lives** — lose a match, lose a life (floor 0); the score mirrors the remaining lives.
- **WinCount** — score = number of wins; scoreboard ties broken by goal difference, then by
  fewer goals lost.
- **Per-match snapshot:** inside one match every team delta is computed against the
  **pre-match** score state (restored before each team Apply), so the result is independent of
  team order.
- `ScoreDiff` = change in Score since the players last match (display delta, e.g. +16 / -16).
- `Lives` is only set for Lives tournaments (0 for the other systems).

## Auth

- Cookie scheme `AppCookie` (login path `/login`); authorization policy `IdentityRequired`.
- Azure AD (OIDC) is wired **only when** `AzureAd:ClientId` is configured; the Microsoft button
  on `/login` appears in that case, and sign-out includes the Azure AD end-session.
- **Test login** (next to the Microsoft button) is shown **only when both** `TestUser__Email`
  and `TestUser__Password` env vars are set; the test user is seeded (PBKDF2-SHA256, 100k
  iterations) and can be used for local runs and the Playwright screenshot sessions.
- Optional Azure **Graph** avatar fetch on user creation (best-effort; skipped when no
  `ITokenAcquisition` is available).

## Running locally

The .NET SDK lives in `~/.dotnet` (not on PATH):

    export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
    ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://127.0.0.1:5199 \
    TestUser__Email=test@idasletten.dk TestUser__Password=Correct-Horse-42 \
    dotnet run --project Idasletten --no-launch-profile   # --no-launch-profile is REQUIRED

The app migrates + seeds on startup (in-memory mode). Seeded data: 10 Norse users and four
tournaments (Elo 1v1 public, TrueSkill 2v2 public, Lives 1v1 private, archived WinCount) plus a
seeded child round ("Valkyrior Open — Round 2").

## Testing

    dotnet test    # from the solution root

- `TestWebApplicationFactory` boots the real `Program` with an in-memory DB (Development
  environment, so the seed data is present); page tests drive it through TestServer (antiforgery
  tokens are extracted from rendered forms; the login POST goes to `/login?handler=TestLogin`).
- `TestDb` creates a fresh migrated in-memory DB plus a MediatR-wired service provider for
  CQRS-level tests.
- Conventions: AAA pattern, `Should_DoSomething_When_ConditionIsFulfilled` naming, a static
  `Any` factory for domain objects, simple stubs over mocking frameworks.

## Deployment

- `fly.toml` — app `idasletten` (folder name), region `osl`, internal port **8080**, a volume
  for the SQLite file, Production environment (file DB, no seed).
- `.github/workflows/deploy-fly.yml` — on push to `main`: restore/build/test, `dotnet publish`,
  then `flyctl deploy --remote-only` using the `FLY_API_TOKEN` repo secret.
- Fly injects proxy headers; the app clears forwarded-header networks/proxies so the OIDC
  redirect URIs come out `https://`.

## Deliberate spec decisions (the items the spec asked to confirm)

- **"Create and Plan"** (`/tournaments/create`) creates the tournament, plans one round of
  matches, then navigates to **`/tournaments/{id}/matches`**.
- The match UI renders exactly **two** teams (the handler accepts 2-4 for flexibility).
- Match detail = the create-match page in read-only mode (`?match={id}`); editing a Done match
  adds `&edit=true` and requires login.
- The "historical tournaments" link on the home page (`/tournaments`) is deliberately less
  prominent (muted, small) and shows archived and private tournaments.
- `User` uses standard identity-style fields (unique username, name, optional email, image URL,
  password hash for the test user, claims for the cookie) without pulling in the ASP.NET Identity
  framework, which the mandated stack does not include.
