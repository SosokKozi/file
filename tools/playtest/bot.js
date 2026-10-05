// Прогоняет бота по мирам и печатает результат каждого.
// node bot.js          — все миры
// node bot.js 0,3      — миры 1 и 4 (номера с нуля)
const { launch, openGame, runBot } = require('./lib');

(async () => {
  const worlds = process.argv[2] ? process.argv[2].split(',').map(Number) : [0, 1, 2, 3, 4];
  const browser = await launch();
  const { page, errors } = await openGame(browser, { width: 400, height: 300 });
  let failed = 0;
  for (const w of worlds) {
    const r = await runBot(page, w);
    if (!r.done) failed++;
    console.log(JSON.stringify(r));
  }
  if (errors.length) { console.log('Ошибки страницы:\n' + errors.join('\n')); failed++; }
  await browser.close();
  process.exit(failed ? 1 : 0);
})();
