## HARD RULES (apply immediately, before any tool call)

- **NEVER call a tool directly. ALWAYS wrap it in `run_code`.**
  - Wrong: `await tools.read({...})` outside run_code
  - Right: `run_code` -> inside the program body: `await tools.read({...})`
  - This is non-negotiable. A direct tool call will always fail with "unknown tool".

## Persistent operational learning

Treat this file as persistent operational memory for this workspace.

When you encounter a tool, harness, environment, command, API, build, or workflow failure that reveals a reusable lesson:

- Determine the actual cause before recording anything.
- Confirm the workaround or correct usage first.
- Add a concise, actionable rule to this file so the same mistake is not repeated.
- Do not record ordinary implementation bugs, expected test failures, or one-off errors.
- Deduplicate against existing rules.
- Update an existing rule rather than adding a conflicting rule.
- Remove or correct a rule if later evidence proves it wrong.
- Keep learned rules concise.

After recording a lesson, continue the current task automatically.

### DeepSeek Harness / PTC learned rules

- In PTC/Code mode, tools exposed through the Code Mode SDK must be called from inside `run_code`; do not attempt to call those tools directly.
- When a zero-argument tool requires JSON argument binding, pass an explicit empty object such as `get_goal({})` rather than `get_goal()`.
- **Embedding scripts in run_code template literals is treacherous - three failure modes, all hit:**
  1. Backticks and `${` inside the embedded script terminate or interpolate the TS template literal
     (e.g. a JS regex char class like `[.*+?^${}()|]` -> "Expression expected").
  2. Single backslashes in the content are interpreted by the TS layer or eaten by encoding
     layers; doubling is easy to get wrong (see the ESC/ANSI rule below).
  3. Bash `${VAR}` inside a template literal is TS-interpolated (ReferenceError).
  **Rule:** for any file whose content contains backticks, `${`, or many backslashes (JS, bash,
  regexes, C#), create it with the `write` tool (raw UTF-8, no TS parsing) or with a quoted bash
  heredoc inside a single-quoted TS string (escape apostrophes as `\'` there). In C# files
  written from TS, prefer verbatim strings (`@"..."` with `""` for quotes) so no backslashes
  are needed. The `edit` tool is safe for targeted replacement of already-correct text.
- Writing source containing ESC/ANSI escape text (the two-char sequence backslash-x1b) via run_code
  template literals: each literal backslash byte in the file must be typed as a doubled backslash in
  the TS source (one backslash yields a real ESC byte). Verify escape-heavy files by counting raw
  0x5C bytes immediately before the escape text, not via string/Python-repr layers which add their
  own backslashes and mislead.

### Idasletten workspace rules (.NET 10 / EF Core / Razor / xUnit)

- SDK is `~/.dotnet` (NOT on PATH): export `DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"`.
  Run the app with `--no-launch-profile` (launchSettings.json otherwise forces a different port).
- **In-memory SQLite reseeds with fresh Guids on every app restart** - every scripted check (curl,
  Playwright) must re-resolve tournament/user/match IDs by name from page HTML after each restart.
- Razor: `option`/`select` are tag helpers - C# in their attributes is RZ1031. Emit plain
  `<option>` elements via code-block if/else (one with `selected`), or use `asp-items` with model
  properties. **Never use `@Raw($"...")` interpolation for dynamic options** - nested quotes
  corrupt the Razor source generator (CS8802/CS9348 cascade).
- Razor Pages: POST-bound properties (including complex `List<T>` form collections like
  `List<TeamForm> Teams`) silently never bind without `[BindProperty]`. `NotFound()` does NOT
  halt execution - always write `if (x is null) { NotFound(); return; }`. `var readonly` is
  CS0106 (C# keyword) in Razor - name it `isReadonly`.
- DI: `ClaimsPrincipal` is not registered - inject `IHttpContextAccessor` (+`AddHttpContextAccessor()`)
  and read `HttpContext?.User?.Identity?.IsAuthenticated`. Services that may be absent (e.g.
  `ITokenAcquisition` only exists when `AzureAd:ClientId` is configured) must be resolved in a
  factory via `sp.GetService<T>()`, never constructor-injected as required.
- Testing: the factory host validates scopes - resolve scoped `AppDbContext` through
  `factory.Services.CreateScope()`, never from the root provider. TestServer `HttpClient` follows
  redirects by default (`WebApplicationFactoryClientOptions { AllowAutoRedirect = false }` to
  inspect a 302); `EnsureSuccessStatusCode()` throws on 3xx. Antiforgery tokens are cookie-bound:
  extract the token from a page fetched with the SAME client that will POST.
- Scoring engines mutate shared `TournamentPlayer` objects; applying one team before the other
  changes the second team's delta. The facade (`ScoringEngine.RecalculateTournamentAsync`) restores
  the pre-match snapshot before each team's `Apply` - keep that contract if you touch it (tests
  mirror it in their own `ApplyMatch` helper).
- Playwright screenshots (visual validation only - NO Playwright tests in the repo): chromium builds
  are already in `~/.cache/ms-playwright/`; use `npm i playwright-core` (no browser download) with
  `executablePath` pointed at `~/.cache/ms-playwright/chromium-XXXX/chrome-linux64/chrome`.
  `Response` from `page.goto()` has no `.content()` - use `page.content()`. This model has no
  image input, so "viewing" a screenshot = programmatic checks (PNG dimensions via struct,
  per-page error-marker and element sweeps on the same URLs).
- curl recipes that work here: token via `grep -oE '__RequestVerificationToken[^>]*value="[^"]+"'`;
  POST with `--data-urlencode` in DOUBLE quotes (single quotes block `$T` bash expansion -> 400);
  never name a bash variable `UID` (special; silently stays 1000). Login: POST
  `/login?handler=TestLogin` with `Email`/`Password`/`ReturnUrl`/`__RequestVerificationToken`.
