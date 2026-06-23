# Idasletten UI screenshots

Visual validation is done with a local Playwright Node script (`shot.mjs`) that drives a
headless chromium against the running app and saves PNGs in this folder.

> **Why local Node Playwright and not the Playwright MCP server?**
> In this benchmark environment the Playwright MCP server's browser cannot reach the host's
> `127.0.0.1` (the dev sandbox blocks it), so we use Playwright on the host instead. The rule
> in `AGENTS.md` — "Playwright screenshots for visual validation only" — is still obeyed; this
> script does not contain any test assertions and is not part of `dotnet test`.

## Repro

```bash
# Terminal 1 — start the app (test-user login enabled):
TestUser__Email=test@example.com TestUser__Password=test123 \
  dotnet run --project Idasletten/Idasletten.csproj

# Terminal 2 — take the screenshots:
node screenshots/shot.mjs
ls screenshots/*.png
```

You can override the base URL with `IDASLETTEN_URL=http://127.0.0.1:5085 node screenshots/shot.mjs`.

## Captured pages

| PNG                       | Page                                |
|---------------------------|-------------------------------------|
| `home.png`                | `/` (hero + public tournament cards)|
| `tournaments.png`         | `/Tournaments` (all tournaments list) |
| `login.png`               | `/Login` (Azure AD + test button)    |
| `create-tournament.png`   | `/Tournaments/Create` (auth-required) |
| `detail-scoreboard.png`   | `/Tournaments/{id}` (scoreboard)      |
| `matches.png`             | `/Tournaments/{id}/Matches`           |
| `players.png`             | `/Tournaments/{id}/Players`           |
| `create-match.png`        | `/Tournaments/{id}/CreateMatch`      |