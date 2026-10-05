# Плейтест

Бот-автопилот, скриншоты и таблица прыжков для `index.html`.

```sh
cd tools/playtest
npm install
node bot.js                      # все миры
node bot.js 0,3                  # миры 1 и 4
node shots.js '[[0,160],[2,110]]' # скриншоты: [мир с нуля, метр] → shots/
node physics.js                  # таблица прыжков
```

Нужен Chromium для Playwright: `npx playwright install chromium`, либо путь к своему Chrome в `CHROMIUM_PATH`. Three.js берётся из `node_modules`, остальные сетевые запросы (шрифты) отключены.
