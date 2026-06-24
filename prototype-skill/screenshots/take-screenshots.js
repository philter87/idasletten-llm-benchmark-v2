const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const screenshotDir = __dirname;
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.setViewportSize({ width: 1280, height: 900 });
  const BASE = 'http://localhost:3000';

  async function shot(name, url) {
    try {
      await page.goto(BASE + url, { waitUntil: 'networkidle' });
      await page.screenshot({ path: path.join(screenshotDir, name + '.png'), fullPage: true });
      console.log('OK  ' + name);
    } catch (e) {
      console.log('ERR ' + name + ': ' + e.message.split('\n')[0]);
    }
  }

  // Get a real tournament ID from the tournaments list table
  await page.goto(BASE + '/tournaments');
  const href = await page.locator('tbody a[href^="/tournaments/"]').first().getAttribute('href').catch(() => null);
  const tid = href ? href.split('/')[2] : null;
  console.log('TID:', tid);

  await shot('01-home', '/');
  await shot('02-tournaments', '/tournaments');
  await shot('03-create-tournament', '/tournaments/create');
  await shot('04-login', '/login');

  if (tid) {
    await shot('05-tournament-detail', '/tournaments/' + tid);
    await shot('06-create-match', '/tournaments/' + tid + '/create-match');
    await shot('07-matches', '/tournaments/' + tid + '/matches');
    await shot('08-players', '/tournaments/' + tid + '/players');

    await page.goto(BASE + '/tournaments/' + tid);
    const userHref = await page.locator('a[href^="/users/"]').first().getAttribute('href').catch(() => null);
    if (userHref) await shot('09-user-profile', userHref);

    // login as PCH then screenshot
    await page.goto(BASE + '/login');
    await page.fill('input[name="initials"]', 'PCH');
    await page.click('button[type="submit"]');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: path.join(screenshotDir, '10-after-login-home.png'), fullPage: true });
    console.log('OK  10-after-login-home');

    await shot('11-tournament-loggedin', '/tournaments/' + tid);
  }

  await shot('12-tournaments-archived', '/tournaments?all=1');

  await browser.close();
  console.log('Done!');
})();
