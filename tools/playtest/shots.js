// Снимает скриншоты: бот добегает до нужного метра, затем кадр сохраняется в shots/.
// node shots.js '[[0,160],[2,110]]'   — [мир (с нуля), метр]
const fs = require('fs');
const path = require('path');
const { launch, openGame, runBot } = require('./lib');

(async () => {
  const list = JSON.parse(process.argv[2] || '[[0,40]]');
  const out = path.join(__dirname, 'shots');
  fs.mkdirSync(out, { recursive: true });
  const browser = await launch();
  const { page, errors } = await openGame(browser);
  await page.addStyleTag({ content: '.pop,.countdown{display:none!important}' });
  for (const [w, m] of list) {
    await runBot(page, w, m);
    await page.waitForTimeout(150);
    const file = path.join(out, `world${w + 1}_${m}m.png`);
    await page.screenshot({ path: file });
    console.log(file);
  }
  if (errors.length) console.log('Ошибки страницы:\n' + errors.join('\n'));
  await browser.close();
})();
