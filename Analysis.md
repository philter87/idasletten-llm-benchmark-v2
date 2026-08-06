# Analysis
Analysis of all Idasletten LLM benchmark implementations. Each project builds a table-football tournament management app (Idasletten) from the same prompt, using different AI tools and models.

---

## Tool & Model Key

| Prefix/Name | Tool | Model |
|---|---|---|
| `cp-sonnet` | Copilot CLI | Claude Sonnet |
| `cp-sonnet-v2` | Copilot CLI | Claude Sonnet (v2 session) |
| `gpt55` | Codex (Copilot CLI) | GPT-5.5 |
| `mistral` | OpenCode | Mistral |
| `oc-deepseek-v4-flash` | OpenCode | DeepSeek V4 Flash |
| `oc-deepseek-v4-pro` | OpenCode | DeepSeek V4 Pro |
| `oc-glm52` | OpenCode | GLM 5.2 |
| `oc-kiwi27` | OpenCode | Kimi k1.5 (kiwi2.7) |
| `oc-qwen37` | OpenCode | Qwen 3.7 |
| `opus48-v2` | Claude Code | Claude Opus 4.8 |
| `sonnet` | Claude Code | Claude Sonnet 4.6 |

---

## Scorecard Summary

### Weights

"Does it work?" weighting — the primary question is whether the app actually functions correctly end-to-end:

| Category | Weight | Rationale |
|---|---|---|
| Flows & Features | **30%** | Most holistic validation — does the full app work? |
| Business Logic | **25%** | Spec correctness: events, rules, scoring |
| Code Quality | **20%** | Tests, security, architecture |
| Time & Money | **15%** | Cost efficiency matters, but secondary to correctness |
| UI Design | **10%** | All use the same basecoat CDN; style is secondary |

### Ranked by weighted score

| Rank | Project | BL /10 | Flows /10 | UI /10 | CQ /10 | T&M /10 | **Weighted /10** | Equal /10 |
|---|---|---|---|---|---|---|---|---|
| 🥇 1 | **cp-sonnet** | **10** | **10** | 7 | 7 | 8 | **8.80** | 8.40 |
| 🥈 2 | **cp-sonnet-v2** | 9 | **10** | 8 | 7 | 7 | **8.50** | 8.20 |
| 🥉 3 | **opus48-v2** | **10** | **10** | 8 | **9** | 2 | **8.40** | 7.80 ↑ |
| 4 | **oc-deepseek-pro** | **10** | 8 | 7 | 7 | 9 | **8.35** | 8.20 |
| 5 | **sonnet** | 9 | 9 | 7 | 7 | 8 | **8.25** | 8.00 |
| 6 | **oc-glm52** | **10** | 9 | 6 | 5 | 7 | **7.85** | 7.40 |
| 7 | **gpt55 (Codex)** | 8 | 9 | **9** | 5 | 5 | **7.35** | 7.20 |
| 8 | **oc-deepseek-flash** | 8 | 7 | 4 | 4 | **10** | **6.80** | 6.60 |
| 9 | **oc-qwen37** | 8 | 8 | 5 | 4 | 5 | **6.45** | 6.00 |
| 10 | **oc-kiwi27** | 9 | 6 | 1 | 5 | 7 | **6.20** | 5.60 |
| 11 | **mistral** | 8 | 1 | 1 | 4 | 3 | **3.65** | 3.40 |

> **Notable rank changes vs. equal weighting:**
> - `opus48-v2` **+2** (5th → 3rd): its expensive cost is outweighed by perfect business logic + flows + best code quality
> - `gpt55` **−1** (6th → 7th): great UI (9/10) is now only worth 10% — strong flows keeps it competitive
> - `oc-deepseek-flash` **−1** (8th → 8th, but gap widens): perfect cost score (10) is now only 15% of total

---

## Detailed Scores

### Business Logic (0–10)

| Project | Commands→Events (0-2) | Business Rules (0-2) | Scoring Systems (0-6) | **Total** |
|---|---|---|---|---|
| cp-sonnet | 2 — all 6 handlers publish events | 2 — 34 validations | 6 — all 4 correct (Elo K=32, Moserware TrueSkill, Lives, WinCount) | **10** |
| cp-sonnet-v2 | 2 — all 6 handlers publish events | 1 — 6 validations | 6 — all 4 correct | **9** |
| gpt55 | 2 — all 6 handlers publish events | 1 — 4 validations | 5 — TrueSkill uses ELO-like formula (K=24) instead of Moserware library | **8** |
| mistral | 0 — events defined but NOT published (placeholders only) | 2 — 43 validations (many in page handlers) | 6 — all 4 correct | **8** |
| oc-deepseek-flash | 1 — 4/6 handlers publish events | 1 — 7 validations | 6 — all 4 correct with Moserware | **8** |
| oc-deepseek-pro | 2 — all 8 handlers publish 6 distinct events | 2 — 9 validations | 6 — all 4 correct | **10** |
| oc-glm52 | 2 — all handlers publish 6 events | 2 — 23 validations | 6 — all 4 correct with Moserware | **10** |
| oc-kiwi27 | 1.5 — 5/7 handlers (PlanMatch, PlanSeveralMatches missing) | 1.5 — 8 validations | 6 — all 4 correct with Moserware | **9** |
| oc-qwen37 | 1 — 4/12 handlers (CompleteMatch, PlanMatches missing) | 1 — 5 validations | 6 — all 4 correct | **8** |
| opus48-v2 | 2 — all 5 handlers publish events | 2 — 13 validations | 6 — all 4 correct with Moserware | **10** |
| sonnet | 2 — all 6 handlers publish events | 1.5 — 8 validations | 6 — all 4 correct | **9.5 → 9** |

---

### Complicated Flows & Pages (0–10)

Validated via existing Playwright screenshots from each project's own validation session.

| Project | Login+Create+Players+Match (0-5) | Plan from prev. tournament (0-2) | Players from prev. tournament (0-1) | Select from list in match (0-1) | Score recalculation on edit (0-1) | **Total** |
|---|---|---|---|---|---|---|
| cp-sonnet | 5 — full flow, login, seeded data | 2 — matches page with plan | 1 — players page | 1 — "Vælg fra liste" button ✅ | 1 — yes | **10** |
| cp-sonnet-v2 | 5 — full flow documented | 2 — plan several matches shown | 1 — players page | 1 — "Vælg fra liste" shown ✅ | 1 — yes | **10** |
| gpt55 | 5 — full flow, authenticated screenshots | 2 — plan several matches button | 1 — players/manage link | 0 — no select from list visible | 1 — full recalc from scratch | **9** |
| mistral | 1 — homepage crashes with SQLite error ("no such table: Tournaments") | 0 | 0 | 0 | 0 — buggy (no rollback) | **1** |
| oc-deepseek-flash | 4 — needed correction during session, eventual fix | 1 — basic plan | 1 — players page | 0 — not visible | 1 — clears old results | **7** |
| oc-deepseek-pro | 5 — full flow with 10+ screenshots | 1 — matches page shown | 1 — players with seed section | 0 — not visible | 1 — removes old results, recalculates | **8** |
| oc-glm52 | 5 — full flow shown | 1 — plan matches page | 1 — players page | 1 — "Select from list..." link ✅ | 1 — idempotent recalculation | **9** |
| oc-kiwi27 | 3 — functional but no CSS styling | 1 — basic plan | 1 — players page | 0 — not visible | 1 — full replay recalculation | **6** |
| oc-qwen37 | 5 — most detailed screenshots (11 images) | 2 — matches with plan options | 1 — players with tournament seed | 0 — not visible | 0 — additive only (no rollback of old score) | **8** |
| opus48-v2 | 5 — full flow documented | 2 — full dialog: seed tournament selector, games per player, Fixed teams, seeding type (Random/Equality/Fair) ✅ | 1 — seedlist.png shows strikethrough for added players ✅ | 1 — "Select from list" visible ✅ | 1 — dedicated test: `Should_RecalculateScores_When_CompletedMatchEdited` | **10** |
| sonnet | 5 — full flow shown | 2 — "Planlæg flere kampe" shown | 1 — players page | 0 — not visible | 1 — yes | **9** |

---

### UI Design (0–10)

| Project | Score | Notes |
|---|---|---|
| cp-sonnet | 7 | Clean navy hero with Norse branding (ᚷ symbol), white theme, proper Danish text, basecoat buttons, structured scoreboard. Missing sidebar layout on detail page. |
| cp-sonnet-v2 | 8 | Orange/flame accents, trophy icon in hero, **proper 2-column layout** on tournament detail (scoreboard left, create match + recent results right sidebar). Score delta shown with colored highlights. |
| gpt55 | **9** | Stunning full-width gradient hero (reddish-brown "blood" effect, "IÐAVÖLLR" label), large typography, tournament cards in 2-column grid. Tournament detail shows proper sidebar with planned/recent matches. Most visually impressive. |
| mistral | 1 | Homepage crashes with "SQLite Error 1: no such table: Tournaments". Other pages show minimal unstyled HTML. |
| oc-deepseek-flash | 4 | Small hero text, minimal spacing. Originally had broken basecoat UI — was corrected in session. Functional but minimal polish. |
| oc-deepseek-pro | 7 | Clean maroon hero with sword icon, colored score delta values (green/red), proper sidebar layout on detail, login page included. |
| oc-glm52 | 6 | Dark maroon gradient hero, WinCount badge with green pill, proper card layout, delta badges ("+1" circles). Decent but layout slightly rough. |
| oc-kiwi27 | 1 | No CSS rendered in screenshots — raw unstyled HTML with plain text links. |
| oc-qwen37 | 5 | White background, dark navy hero card, badge tags for scoring types. Clean but very minimal. |
| opus48-v2 | 8 | Dark maroon nav bar, clean minimal design, **avatar circles with initials** in scoreboard, full "Plan several matches" dialog with all options. Polished and feature-complete. |
| sonnet | 7 | Lightning bolt icon, Danish text throughout ("Turneringer", "Log ind"), orange score highlights on match scores, user profile with avatar circle. Clean and professional. |

---

### Code Quality & Security (0–10)

| Project | Tests | Auth & Security | Structure | **Score** |
|---|---|---|---|---|
| cp-sonnet | 9 tests — scoring, tournament, integration | [Authorize] on Create; Azure AD + Cookie; public create-match per spec | CQRS + events + 34 business rules; 2,237 LOC | **7** |
| cp-sonnet-v2 | 9 tests — scoring + integration | [Authorize] on Create; Azure AD + Cookie | CQRS + 6 events; 1,803 LOC | **7** |
| gpt55 | 5 tests — basic integration | Conventions-based auth on Create; Azure AD + Cookie | Clean CQRS; **945 LOC** (smallest!); full recalculation service | **5** |
| mistral | **0 tests** — only test infrastructure (Any.cs) | [Authorize] on 3 pages (Create, Matches, Players); Azure AD | 2,789 LOC; events designed but not wired | **4** |
| oc-deepseek-flash | 1 test | [Authorize] on Create; ASP.NET Identity | 1,886 LOC; built on pro as base | **4** |
| oc-deepseek-pro | 9 tests — scoring + integration | [Authorize] on Create; Azure AD + Cookie | CQRS + 9 validations; 1,917 LOC | **7** |
| oc-glm52 | 7 tests | Command-level auth checks only; no [Authorize] attributes | CQRS + 23 validations + MatchRecorder service; 1,744 LOC | **5** |
| oc-kiwi27 | 5 tests | [Authorize] on Create; ASP.NET Identity | CQRS + TournamentRecalculator service; 2,058 LOC | **5** |
| oc-qwen37 | 3 tests | No [Authorize] attributes (global config only) | CQRS; 1,611 LOC | **4** |
| opus48-v2 | **14 tests** — match, tournament, scoring, planning, smoke tests | [Authorize] on Create; Azure AD + Cookie; smoke test validates redirect | CQRS + ScoreService.RecalculateAsync with dedicated test; 2,171 LOC / **442 test LOC** | **9** |
| sonnet | 10 tests — scoring (4 Elo tests), tournament, players | [Authorize] on Create; Azure AD + Cookie | CQRS + 4 scoring calculators; 1,424 LOC (compact!) | **7** |

---

### Project Size

| Project | Main LOC (excl. migrations) | Test LOC | Tests | Ratio |
|---|---|---|---|---|
| cp-sonnet | 2,237 | 351 | 9 | 6.4:1 |
| cp-sonnet-v2 | 1,803 | 283 | 9 | 6.4:1 |
| gpt55 | **945** | 165 | 5 | 5.7:1 |
| mistral | **2,789** | 88 | 0 | — |
| oc-deepseek-flash | 1,886 | 163 | 1 | 11.6:1 |
| oc-deepseek-pro | 1,917 | 329 | 9 | 5.8:1 |
| oc-glm52 | 1,744 | 267 | 7 | 6.5:1 |
| oc-kiwi27 | 2,058 | 241 | 5 | 8.5:1 |
| oc-qwen37 | 1,611 | 165 | 3 | 9.8:1 |
| opus48-v2 | 2,171 | **442** | **14** | **4.9:1** |
| sonnet | 1,424 | 311 | 10 | 4.6:1 |

---

### Time & Money (0–10)

Token/cost: 0-7 pts (lower cost = higher score). Time: 0-3 pts (less time = higher score).

| Project | Cost | Time | Cost Score (0-7) | Time Score (0-3) | **Total** |
|---|---|---|---|---|---|
| cp-sonnet | 569 AIC | 20 min | 5 | **3** | **8** |
| cp-sonnet-v2 | 690 AIC, 14.6M tokens | 36 min | 4 | 2.5 | **7** |
| gpt55 | 6.78M in / 277K out tokens (no $ amt) | unknown | 3 | 2 | **5** |
| mistral | $12.43 | 2 hours | 3 | 0.5 | **3** |
| oc-deepseek-flash | ~$0.13 delta | 35 min | **7** | 2.5 | **10** |
| oc-deepseek-pro | $0.96 | 1 hour | **7** | 2 | **9** |
| oc-glm52 | $7.52 | 38 min | 4 | 2.5 | **7** |
| oc-kiwi27 | ~$6.51 delta | ~1 hour | 5 | 2 | **7** |
| oc-qwen37 | unknown (before $10.36, no after) | unknown | 3 | 2 | **5** |
| opus48-v2 | $19.89 | 1h 14m | 0 | 1.5 | **2** |
| sonnet | $4.78 | 23m 49s | 5 | **3** | **8** |

> **Notes on token usage:**
> - oc-deepseek-flash was built on top of oc-deepseek-pro (cost delta: $1.09 − $0.96 = $0.13)
> - oc-kiwi27 cost delta: $7.60 − $1.09 = $6.51 (OpenCode cumulative stats)
> - oc-qwen37 only has "before session" stats ($10.36), no "after" — cost unknown
> - cp-sonnet/v2 use AI Credits (AIC), not directly comparable to USD
> - opus48 original ($25.87, 1h 14m 54s) from Tokens.txt — folder contains v2 version ($19.89)

---

## Key Findings

### 🥇 Top Performers
1. **cp-sonnet** (42/50) — Best combined score: all events, 34 business rules, full flows, "Vælg fra liste" select-from-list, clean Danish UI
2. **cp-sonnet-v2** (41/50) — Proper 2-column detail layout per spec, full features, good test coverage
3. **oc-deepseek-pro** (41/50) — Excellent value at $0.96: all events, 9 validations, full flows, good quality
4. **sonnet** (40/50) — $4.78 for excellent result: compact 1,424 LOC, 10 tests, clean design

### 💰 Best Value
- **oc-deepseek-pro**: $0.96, 1 hour → 41/50 total. Best dollar-per-quality ratio.
- **sonnet (Claude Code)**: $4.78, 23 min → 40/50 total. Fastest Claude Code session.
- **oc-deepseek-flash**: $0.13 additional cost but lower overall quality (33/50)

### 🎨 Best UI
- **gpt55**: Full-width gradient hero with "IÐAVÖLLR" branding, reddish "blood" tone, large typography — most visually impressive
- **opus48-v2** & **cp-sonnet-v2**: Tied second with proper 2-column tournament detail layout

### 📋 Most Complete Feature Set
- **opus48-v2**: Only project with explicit test for score recalculation (`Should_RecalculateScores_When_CompletedMatchEdited`), full "Plan several matches" dialog with all 3 seeding types (Random/Equality/Fair), and "Add players from previous tournament" with visual strikethrough feedback
- **cp-sonnet**: Most business rules validated (34), full event coverage

### 🚫 Notable Failures
- **mistral**: Application crashes on startup with SQLite error ("no such table: Tournaments"). Zero tests. Events are defined but never published (placeholder comments only). 17/50.
- **oc-kiwi27**: No CSS rendered — bare unstyled HTML despite functional code. 28/50.
- **oc-qwen37**: Score recalculation is additive (no rollback when editing match). 30/50.

### 📊 Architecture Compliance
All projects implement the required:
- ✅ CQRS with MediatR
- ✅ Razor Pages
- ✅ SQLite + EF Core
- ✅ Vertical feature slices
- ✅ 4 scoring systems
- ✅ Azure AD authentication
- ✅ Public create-match (no login required)
- ✅ Protected tournament creation ([Authorize])

---

## Questions

- Business logic (Validate from 0 to 10)
  - Does commands always result in Events. Ex: CreateTournament creates TournamentCreated? (0 to 2 points)
  - How many business rules are validated in the command handlers? (0 to 2 points)
  - Is the scoring system implemented correctly: ELO, TrueSkill, Lives, WinCount? (0 - 6 points)
- Validate complicated flows and pages using MCP server (take pictures along the way)? (0 to 10 points)
  - Is it possible to login with the test user locally, create a tournament, add players and create a match? (0 to 5 points)
  - Is it possible to plan matches based on a previous tournament? (0 to 2 points)
  - Is it possible to create players based on a previous tournament? (0 to 1 points)
  - Is it possible to select players from a list when creating a match (0 to 1 points)
  - Are scores recalculated when an old match is edited, for instance if the game have a different winner (0 to 1 points)
- How great is the design of the UI? Validate by looking at screenshots if any (Validate from 0 to 10)?
- Code Quality and security (0 to 10 points)
  - Code duplication - Measure code duplication
  - How many tests are there in the solution?
  - Does authentication work?
  - Are the relevant endpoints secured properly with authentication and authorization?
- Project Size
  - How many lines of code are there in the solution (excluding tests and migrations folder)?
  - How many lines of code are there in the test project?
- Time and money (0 to 10 points) (Either this information is present in the Tokens.txt or within the different folders in a file called token-usage.txt - notice that the token-usage may require you to calculate a difference)
  - How many tokens/dollars were used (0 to 7)
  - How long did it take (0 to 3) (If this information is not available then the token usage becomes the only metric)
