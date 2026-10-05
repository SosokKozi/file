// Таблица дальности и высоты прыжков. Константы должны совпадать с index.html.
const G = 38, JUMP = 13.4, DJUMP = 12.2, PAD = 24, RELEASE_MUL = 1.9;

function sim(spd, vy0, hold, doubleAtVy, pad) {
  let y = 0, vy = vy0, t = 0, apex = 0, used = false;
  const dt = 1 / 240;
  for (;;) {
    let g = G;
    if (vy > 0 && !hold && !pad) g *= RELEASE_MUL;
    vy -= g * dt; y += vy * dt; t += dt; apex = Math.max(apex, y);
    if (doubleAtVy !== undefined && !used && vy <= doubleAtVy) { vy = DJUMP; used = true; pad = false; }
    if (y <= 0 && t > 0.05) break;
  }
  return `${(t * spd).toFixed(1)} м / ${t.toFixed(2)} с / высота ${apex.toFixed(2)}`;
}

console.log('скорость | полный | тап | полный+двойной | батут');
for (const s of [21, 30, 40, 48]) {
  console.log([s, sim(s, JUMP, true), sim(s, JUMP, false), sim(s, JUMP, true, 0), sim(s, PAD, true, undefined, true)].join(' | '));
}
