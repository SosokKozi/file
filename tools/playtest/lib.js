// Общие функции плейтеста: запуск браузера, загрузка игры, бот-автопилот.
const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright-core');

const ROOT = path.resolve(__dirname, '..', '..');
const THREE_FILE = path.join(__dirname, 'node_modules', 'three', 'build', 'three.min.js');

// Браузер: CHROMIUM_PATH, иначе браузер Playwright; если его версия не та — любой chromium-* из PLAYWRIGHT_BROWSERS_PATH.
async function launch() {
  if (process.env.CHROMIUM_PATH) return chromium.launch({ executablePath: process.env.CHROMIUM_PATH });
  try {
    return await chromium.launch();
  } catch (e) {
    const dir = process.env.PLAYWRIGHT_BROWSERS_PATH;
    const found = dir && fs.existsSync(dir) && fs.readdirSync(dir)
      .filter((d) => /^chromium-\d+$/.test(d))
      .map((d) => path.join(dir, d, 'chrome-linux', 'chrome'))
      .find((f) => fs.existsSync(f));
    if (!found) throw new Error('Не найден Chromium. Установите его: npx playwright install chromium, или укажите CHROMIUM_PATH.');
    return chromium.launch({ executablePath: found });
  }
}

// Открывает index.html; Three.js отдаётся из node_modules, остальная сеть отключена (шрифты подменяются системными).
async function openGame(browser, viewport) {
  const page = await browser.newPage({ viewport: viewport || { width: 1280, height: 720 } });
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error' && !/ERR_FAILED|net::/.test(m.text())) errors.push(m.text()); });
  await page.route('**/*', (route) => {
    const url = route.request().url();
    if (url.includes('cdn.jsdelivr.net/npm/three')) return route.fulfill({ body: fs.readFileSync(THREE_FILE), contentType: 'application/javascript' });
    if (url.startsWith('file:')) return route.continue();
    return route.abort();
  });
  await page.goto('file://' + path.join(ROOT, 'index.html'));
  await page.waitForFunction(() => window.__game && window.__game.LV, null, { timeout: 15000 });
  return { page, errors };
}

// Бот работает внутри страницы через window.__game.simFrame (60 кадров/с, без рендера).
// Он нарочно простой: смерть бота — повод посмотреть место глазами, а не обязательно ошибка уровня.
function botRun([lvl, stopAtM]) {
      const g = window.__game; g.startLevel(lvl);
      const P = g.P, LV = g.LV, K = g.keys;
      const solids = LV.solids;
      function topAt(x, z, maxY) { let best = -Infinity, bs = null; for (const s of solids) { if (s.disabled) continue; if (x < s.minX + 0.2 || x > s.maxX - 0.2 || z < s.minZ || z > s.maxZ) continue; if (s.maxY <= maxY && s.maxY > best) { best = s.maxY; bs = s; } } return [best, bs]; }
      function platsAt(z, y) { const out = []; for (const s of solids) { if (s.disabled) continue; if (z < s.minZ || z > s.maxZ) continue; if (s.hy > 0.55 && s.hy < 0.65 && s.maxY <= y + 2.6 && s.maxY > y - 25) out.push(s); } return out; }
      let jumpHeld = 0, t = 0, lastJump = 0;
      const dt = 1 / 60;
      for (let f = 0; f < 60 * 240; f++) {
        if (stopAtM && -P.z >= stopAtM) return { lvl, stoppedAtM: Math.round(-P.z) };
        t += dt;
        // lateral target
        const look = P.z - Math.max(6, P.spd * 0.45);
        let tx = P.x;
        const ps = platsAt(look, P.y);
        if (ps.length) { ps.sort((a, b) => Math.abs(a.cx - P.x) - Math.abs(b.cx - P.x)); const s = ps[0]; tx = clamp(P.x, s.minX + 0.9, s.maxX - 0.9); if (s.hx < 1.5) tx = s.cx; }
        // avoid pillars / partial walls / spikes / movers ahead
        for (const s of solids) {
          if (s.disabled || s.hy > 0.55 && s.hy < 0.65) continue;
          const dz = P.z - s.maxZ; if (dz < 0 || dz > 9) continue;
          if (s.minY > P.y + 0.6 || s.maxY < P.y + 0.3) continue;
          if (s.maxY - P.y > 2.0 && P.x + 0.6 > s.minX && P.x - 0.6 < s.maxX) { tx = (P.x < s.cx ? s.minX - 1.0 : s.maxX + 1.0); }
        }
        for (const hz of LV.hazards) {
          const dz = P.z - (hz.cz + hz.hz); if (dz < 0 || dz > 10) continue;
          const left = hz.cx - hz.hx, right = hz.cx + hz.hx;
          const [gt, gs] = topAt(P.x, hz.cz, P.y + 0.5);
          if (gs && (left > gs.minX + 1.2 || right < gs.maxX - 1.2)) { tx = (left - gs.minX > gs.maxX - right) ? (left + gs.minX) / 2 : (right + gs.maxX) / 2; }
        }
        K.left = tx < P.x - 0.25; K.right = tx > P.x + 0.25;
        // jump decisions
        let wantJump = false, wantRoll = false;
        if (P.grounded) {
          const ahead = P.z - (P.spd * 0.1 + 0.9);
          const [gt] = topAt(P.x, ahead, P.y + 0.45);
          if (gt < P.y - 0.3) wantJump = true;
          for (const s of solids) {
            if (s.disabled) continue;
            const dz = P.z - s.maxZ; if (dz < 0 || dz > P.spd * 0.16 + 1) continue;
            if (P.x + 0.4 <= s.minX || P.x - 0.4 >= s.maxX) continue;
            if (s.minY < P.y + 0.4 && s.maxY > P.y + 0.45 && s.maxY - P.y < 2.3) wantJump = true;
            if (s.minY >= P.y + 0.85 && s.minY < P.y + 1.8) wantRoll = true;
          }
          for (const hz of LV.hazards) { const dz = P.z - (hz.cz + hz.hz); if (dz > 0 && dz < P.spd * 0.14 + 1 && P.x + 0.5 > hz.cx - hz.hx && P.x - 0.5 < hz.cx + hz.hx) wantJump = true; }
          for (const sw of LV.sweepers) {
            const dz = P.z - sw.cz; if (dz < 0 || dz > 12) continue;
            // predict bar angle when we reach dz-ish
            const tt = (dz - 1) / P.spd, a = sw.a + sw.w * tt;
            const along = Math.abs(Math.sin(a)); // bar pointing toward path in z
            if (dz < P.spd * 0.2 + 2) wantJump = true;
          }
          for (const lz of LV.lasers) {
            const dz = P.z - lz.cz; if (dz < 0 || dz > P.spd * 0.4) continue;
            const tt = dz / P.spd;
            const fut = lz.base + 0.28 + (1 + Math.sin((t) * 0 + 0)) * 0; // unknown simT; use current height approx
            const rel = lz.cy - P.y;
            if (dz < P.spd * 0.2 + 1) { if (rel < 1.0) wantJump = true; else if (rel < 1.9) wantRoll = true; }
          }
          // next platform much higher (stairs)
          const [hi] = topAt(P.x, P.z - (P.spd * 0.12 + 1), P.y + 1.5);
          if (hi > P.y + 0.45) wantJump = true;
        } else {
          // double jump if no ground below trajectory
          const fz = P.z - P.spd * 0.35;
          const [g1] = topAt(P.x, fz, P.y + 0.2), [g2] = topAt(P.x, P.z - P.spd * 0.6, P.y + 0.5), [g0] = topAt(P.x, P.z, P.y + 0.1);
          if (P.airJumps > 0 && P.vy < 1 && g0 < P.y - 6 && g1 < P.y - 6 && g2 < P.y - 6 && !P.padBoost) wantJump = true;
          // perfect landing
          if (P.vy < -1) { const [gb] = topAt(P.x, P.z - P.spd * 0.05, P.y + 0.1); const fallT = gb > -Infinity ? (P.y - gb) / Math.max(1, -P.vy * 1.3) : 9; if (fallT < 0.12 && !P.fastFall) wantRoll = true; }
        }
        if (wantJump && t - lastJump > 0.18) { g.press('jump'); K.jump = true; jumpHeld = 0.3; lastJump = t; }
        if (jumpHeld > 0) { jumpHeld -= dt; if (jumpHeld <= 0) K.jump = false; }
        if (wantRoll) g.press('roll');
        g.simFrame(dt);
        if (g.state === 'complete') return { lvl, done: true, time: +g.RUN.time.toFixed(2), par: +LV.par.toFixed(2), ratio: +(g.RUN.time / LV.par).toFixed(2), deaths: g.RUN.deaths, deathsAtM: g.RUN.log, perfects: g.RUN.perfects, sparks: g.RUN.sparks + '/' + LV.sparkCount, chunks: LV.chunkLog.join(' ') };
      }
      return { lvl, done: false, reachedM: Math.round(-P.z), finishM: Math.round(LV.finishD), deaths: g.RUN.deaths, deathsAtM: g.RUN.log, chunks: LV.chunkLog.join(' ') };
      function clamp(v, a, b) { return Math.max(a, Math.min(b, v)); }
}

async function runBot(page, lvl, stopAtM) {
  return page.evaluate(botRun, [lvl, stopAtM || 0]);
}

module.exports = { launch, openGame, runBot, ROOT };
