// Local Playwright script that takes screenshots of the running Idasletten app
// (Playwright chromium) for visual validation. Outputs PNGs under screenshots/.
//
// Run:
//   dotnet run --project Idasletten/Idasletten.csproj &   # leave on :5085 with test-user env
//   node screenshots/shot.mjs
//   ls screenshots/*.png
//
// The Playwright MCP server in this configuration cannot reach the host's
// localhost; this local Node script is the supported route for visual checks.

import playwright from '/home/philter87/.npm/_npx/e41f203b7505f1fb/node_modules/playwright/index.js';

const BASE = process.env.IDASLETTEN_URL || 'http://127.0.0.1:5085';

const shots = [
  { path: 'home.png', url: `${BASE}/` },
  { path: 'tournaments.png', url: `${BASE}/Tournaments` },
  { path: 'login.png', url: `${BASE}/Login` },
];

const browser = await playwright.chromium.launch({
  executablePath: '/home/philter87/.cache/ms-playwright/chromium_headless_shell-1223/chrome-headless-shell-linux64/chrome-headless-shell',
  args: ['--no-sandbox'],
});
const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
const page = await ctx.newPage();

// 1. Browse the public pages first.
for (const s of shots) {
  await page.goto(s.url, { waitUntil: 'networkidle' });
  await page.screenshot({ path: `screenshots/${s.path}`, fullPage: true });
  console.log(`saved screenshots/${s.path}`);
}

// 2. Sign in as the test user so we can also screenshot the create-tournament page.
await page.goto(`${BASE}/Login`, { waitUntil: 'networkidle' });
const testBtn = page.getByRole('button', { name: /test user/i });
if (await testBtn.count()) {
  await testBtn.click();
  await page.waitForURL(BASE + '/').catch(() => {});
}
await page.goto(`${BASE}/Tournaments/Create`, { waitUntil: 'networkidle' });
await page.screenshot({ path: 'screenshots/create-tournament.png', fullPage: true });
console.log('saved screenshots/create-tournament.png');
await page.goto(`${BASE}/Tournaments`, { waitUntil: 'networkidle' });
// Click the first tournament detail link.
const firstLink = page.getByRole('link', { name: /Ragnarok Series/i }).first();
let tournamentId = '';
if (await firstLink.count()) {
  await firstLink.click();
  await page.waitForLoadState('networkidle');
  const m = page.url().match(/[?&]id=([0-9a-f-]{36})/);
  tournamentId = m ? m[1] : '';
}
await page.screenshot({ path: 'screenshots/detail-scoreboard.png', fullPage: true });
console.log('saved screenshots/detail-scoreboard.png');

if (tournamentId) {
  await page.goto(`${BASE}/Tournaments/Matches?id=${tournamentId}`, { waitUntil: 'networkidle' });
  await page.screenshot({ path: 'screenshots/matches.png', fullPage: true });
  console.log('saved screenshots/matches.png');
  await page.goto(`${BASE}/Tournaments/Players?id=${tournamentId}`, { waitUntil: 'networkidle' });
  await page.screenshot({ path: 'screenshots/players.png', fullPage: true });
  console.log('saved screenshots/players.png');
  await page.goto(`${BASE}/Tournaments/CreateMatch?id=${tournamentId}`, { waitUntil: 'networkidle' });
  await page.screenshot({ path: 'screenshots/create-match.png', fullPage: true });
  console.log('saved screenshots/create-match.png');
}

await browser.close();
console.log('done');