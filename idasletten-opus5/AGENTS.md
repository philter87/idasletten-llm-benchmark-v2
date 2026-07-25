# Idasletten - agent guide

Web app for running table football (foosball) tournaments, with a Norse mythology theme.
Anyone may browse and record results. A login is only needed to **create a tournament** and to
**edit a match that has already been played**.

## Tech stack

| Thing | Choice |
|---|---|
| Language / framework | C#, .NET 10, ASP.NET Core **Razor Pages** |
| Data | **EF Core 10 + SQLite**. In-memory locally and in tests, a file on the volume in production |
| Messaging | **MediatR 12** - CQRS commands, queries and domain events |
| Identity | ASP.NET Identity entities (`User : IdentityUser<Guid>`), cookie auth + Azure AD (OpenID Connect) |
| Pictures | Microsoft **Graph API** (`Microsoft.Graph` + `Azure.Identity`, application permissions) |
| Rating | **Moserware.Skills.Core** (the net8.0 repackaging of moserware/Skills) for TrueSkill |
| UI | **basecoat** via CDN (Tailwind is inside that bundle - do **not** install Tailwind), flexbox layout, light theme |
| Tests | xUnit, `WebApplicationFactory`, in-memory SQLite |
| Hosting | Fly.io (`fly.toml`, `Dockerfile`), GitHub Actions deploy on push to `main` |

## Projects

```
idasletten-opus5/
├── Idasletten/               # the web app
│   ├── Features/             # one folder per vertical slice
│   ├── Pages/                # Razor Pages - no logic, only MediatR calls
│   ├── Shared/               # everything that is not specific to one slice
│   └── wwwroot/              # site.css, hero.svg, valknut.svg
├── Idasletten.Tests/         # xUnit tests
├── screenshots/              # Playwright MCP screenshots used for design review
├── Dockerfile, fly.toml, .github/workflows/fly-deploy.yml
└── AGENTS.md
```

## Architecture - CQRS and vertical slices

* **Pages** hold no business logic. They send a command or a query with `ISender` and render the result.
* **Features** is one folder per slice with `Commands/`, `Queries/`, `Events/` and the entities of the
  slice: `Users`, `Tournaments`, `Players`, `Matches`, `Scoring`.
* Handlers use `AppDbContext` **directly** - there are no repositories or service layers.
* **Every command handler publishes a domain event when it is done** (`CreateTournamentHandler` →
  `TournamentCreated`, `GetOrCreateUserHandler` → `UserCreated`, ...). All events implement
  `IDomainEvent`, and the open generic `DomainEventLogger<T>` writes an audit line for every one of
  them. `FetchUserPhotoOnUserCreated` is a real subscriber: it fetches the Graph picture.
* **Shared** holds `Data` (DbContext, migrations, seeder), `Auth`, `Messaging`, `Startup` and `Ui`.

### Rules that are easy to get wrong

1. **Migrations are always made with the `dotnet ef` CLI** and are applied **automatically on startup**
   (`Shared/Startup/DatabaseInitializer.cs`, an `IHostedService`). Never call `EnsureCreated`.
2. **Never read configuration while registering services** if a test host has to override it. The
   connection string is resolved from `IConfiguration` when the `DbContext` is *resolved*
   (`InMemoryDatabase.ResolveConnectionString`) - reading it eagerly made every test share one
   database.
3. Scores are **never** updated incrementally. Any change to a match calls
   `TournamentScoring.RecalculateAsync`, which resets every player and replays all done matches in
   order. This is what makes editing an old result correct.
4. Teams are reused: the same set of players in a tournament is always the same `TournamentTeam`
   (`MatchTeams.GetOrCreateTeamAsync`), which is what makes "fixed teams" work.
5. Delete child rows through the `DbSet`, not by clearing a tracked navigation collection - EF
   otherwise tries to orphan-delete them a second time and fails with a concurrency exception.
6. basecoat's `.input` class only styles an `<input>` that has an explicit `type` attribute.
7. The CDN bundle has the basecoat **components**, not the Tailwind **utility classes**. Layout is
   plain flexbox in `wwwroot/css/site.css`.

## Running locally

```bash
cd idasletten-opus5
dotnet run --project Idasletten                  # http://localhost:5xxx
```

* No connection string configured → the app runs on a **SQLite in-memory** database
  (`Data Source=Idasletten;Mode=Memory;Cache=Shared`, kept alive by `InMemoryDatabaseKeepAlive`).
  Migrations are applied and the database is seeded with vikings and four tournaments, so the app is
  never empty.
* Production sets `ConnectionStrings__Idasletten=Data Source=/data/idasletten.db`. Seeding follows the
  database: on for the in-memory one, off for a file - so a deployment never gets demo tournaments.
  Override with `Seed__Enabled=true|false`, and turn the whole automatic setup off with
  `Database__AutoInitialize=false`.

### Logging in locally

```bash
TestUser__Email=test@idasletten.dk TestUser__Password=Valhal123 dotnet run --project Idasletten
```

The test login on `/login` only appears when **both** `TestUser__Email` and `TestUser__Password` are
set, and the user (initials `TST`) is also seeded into the database. Microsoft login appears when
`AzureAd__TenantId` and `AzureAd__ClientId` are set; add `AzureAd__ClientSecret` to also fetch profile
pictures from the Graph API with application permissions.

### Migrations

```bash
dotnet ef migrations add <Name> --project Idasletten --output-dir Shared/Data/Migrations
```

`AppDbContextFactory` is the design-time factory, so the CLI does not boot the web host.

## Testing

```bash
dotnet test                     # from idasletten-opus5
```

* `IdaslettenFactory` (a `WebApplicationFactory<Program>`) boots the real app on its **own** named
  in-memory database and migrates plus seeds it once from `InitialiseAsync`. Test classes call it
  through `IAsyncLifetime`. Automatic initialisation is off in tests
  (`Database:AutoInitialize=false`) because `WebApplicationFactory` boots more than one host.
* `Any` is the test data factory: `Any.User()`, `Any.Tournament()`, `Any.Player()`, ... every field
  gets a random value.
* Naming is `Should_DoSomething_When_ConditionIsFulfilled`, bodies are Arrange / Act / Assert.
* Prefer a simple stub (`NoUserPhotoProvider`) over a mocking framework.
* Playwright is **only** used for screenshots through the MCP server - there are no Playwright tests.

## Scoring systems

| System | Rule |
|---|---|
| `Elo` | Start 1200, K = 32. A team is rated by the **average** rating of its players; the whole delta is given to every player on the team. More than two teams are compared pairwise and averaged. |
| `TrueSkill` | `Moserware.Skills` with the default `GameInfo`. `SkillMean`/`SkillDeviation` are stored per player, `Score` is the conservative rating (mu - 3 sigma). |
| `Lives` | Everybody starts with 3 lives, a lost match costs one, `Score` = lives left. 0 lives = knocked out (struck through in the scoreboard). |
| `WinCount` | `Score` = number of won matches, goal difference breaks ties. |

`ScoreDiff` is the change the player's **last** match made and is shown as `+16` / `-16`.
Ranking (`ScoreEngine.Rank`): score, then goal difference, then wins, then fewest matches.

## Match planning

`MatchPlanner` is pure and deterministic for a given `Random`. One round gives every player one game,
so *games per player* decides how many matches are created:
`matches = floor(players / teamSize) / 2 * gamesPerPlayer`.

* **Random** - the list is shuffled for every round.
* **Equality** - best with worst: 1+N, 2+(N-1), ...
* **Fair** - the ranking is split in `teamSize` equal slices and the n'th player of each slice plays
  together: with 10 players that is 1+6, 2+7, 3+8, 4+9, 5+10 (the example from the specification).

The ranking comes from the seed tournament when the tournament has one, otherwise from the standings
of the tournament itself. Between rounds the ranked list is rotated, so players get new team mates and
a different player sits over when the numbers do not add up. With **fixed teams** the teams are built
once and only the pairing rotates.

## Decisions taken where the specification was open

* **"Create and Plan"** on `/tournaments/create` creates the tournament and goes to
  `/tournaments/{id}/matches?plan=true`, which opens the "plan several matches" dialog. If there are
  not enough players yet, the dialog links to the players page.
* **Seeding a round**: `SeedTournamentId` is ignored when `ParentTournamentId` is set, and
  `SetSeedTournament` throws for a round - "a tournament may be seeded only if it has no parent".
* **Rounds** are hidden in tournament lists by default (`GetTournaments(IncludeRounds: false)`), and
  the parent tournament shows its rounds in a card. "Opret næste runde" is on the tournament page and
  requires a login (it creates a tournament).
* **Editing a played match** is allowed for anybody who is logged in - the page shows a read-only
  view with a login link to everybody else, and the command recalculates the tournament.
* **Goals conceded with more than two teams**: `GoalsLost` is the sum of the other teams' goals,
  which is the usual opponent score in a normal two-team match.
* **`Lives`** is only set (to 3) when the tournament uses that score system, otherwise it stays 0.
* The UI language is **Danish**, like the quote on the front page and the company the users come from.

## Deployment

```bash
fly deploy                      # from idasletten-opus5, app name = folder name
fly secrets set AzureAd__TenantId=... AzureAd__ClientId=... AzureAd__ClientSecret=...
fly secrets set TestUser__Email=... TestUser__Password=...   # only if the test login is wanted
```

* `.github/workflows/fly-deploy.yml` builds, tests and deploys `main` with `FLY_API_TOKEN`.
* SQLite lives on the `idasletten_data` volume, so keep the app on **one** machine.
* `UseForwardedHeaders` runs first in the pipeline with `KnownIPNetworks` and `KnownProxies` cleared,
  so ASP.NET Core trusts Fly's proxy and builds `https://` redirect URIs for Azure AD.
