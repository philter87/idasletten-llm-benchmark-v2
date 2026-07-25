# Screenshots

Design validation of the running app, captured with the Playwright MCP server (screenshots only -
there are no Playwright tests). The app ran on the seeded in-memory database with the test login
enabled, and the authenticated shots use the seeded test user (`TST`).

| File | Page | Shows |
|---|---|---|
| `01-home.png` | `/` | Hero with the Norse quote and the public tournaments |
| `02-tournaments.png` | `/tournaments` | All tournaments, including archived and private ones |
| `03-tournament-elo.png` | `/tournaments/{id}` | Elo scoreboard, next matches, recent results, rounds |
| `04-tournament-lives.png` | `/tournaments/{id}` | Lives scoring - hearts and a knocked out player |
| `05-tournament-trueskill.png` | `/tournaments/{id}` | TrueSkill scoreboard |
| `06-matches.png` | `/tournaments/{id}/matches` | Planned matches and results |
| `07-plan-several-dialog.png` | `/tournaments/{id}/matches?plan=true` | "Plan several matches" with the live match count |
| `08-players.png` | `/tournaments/{id}/players` | Player table |
| `09-players-from-previous-tournament.png` | `/tournaments/{id}/players` | Add players from a previous tournament, + / - and strike-through |
| `10-create-match.png` | `/tournaments/{id}/create-match` | Recording a result without logging in |
| `11-select-players-dialog.png` | `/tournaments/{id}/create-match` | "Select from list" dialog |
| `12-match-readonly-anonymous.png` | `/tournaments/{id}/create-match?matchId=…` | A played match is read-only without a login |
| `13-login.png` | `/login` | Microsoft login plus the test-only login |
| `14-create-tournament.png` | `/tournaments/create` | Tournament settings (login required) |
| `15-user-stats.png` | `/users/{id}` | Cross-tournament statistics |
| `16-edit-played-match-logged-in.png` | `/tournaments/{id}/create-match?matchId=…` | Editing a played result when logged in |
| `17-add-player-dialog.png` | `/tournaments/{id}` | Add player dialog |
| `18-tournament-logged-in.png` | `/tournaments/{id}` | Extra actions for a logged in user (next round, archive) |
| `19-mobile-tournament.png` | `/tournaments/{id}` | The same page at 414 px width |
