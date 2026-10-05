# Бэкенд соревновательных режимов

## Схема данных (Postgres / D1)

```sql
create table players (
  id          uuid primary key,
  nick        text not null,
  nick_lower  text not null unique,
  skin        text not null default 'default',
  hidden      boolean not null default false,   -- скрыт из публичных рейтингов
  created_at  timestamptz not null default now()
);

create table builds (                            -- версии симуляции
  build       text primary key,                  -- хеш sim/*
  active      boolean not null default true,
  created_at  timestamptz not null default now()
);

create table runs (                              -- каждый проверенный финиш
  id          bigserial primary key,
  player_id   uuid not null references players(id),
  world       text not null,                     -- 'w1'..'w5', 'daily-2026-10-05', 'tour-2026-41'
  build       text not null references builds(build),
  ticks       integer not null,                  -- время = ticks / 120
  deaths      smallint not null,
  sparks      smallint not null,
  perfects    smallint not null,
  splits      integer[] not null,
  replay_key  text not null,                     -- путь в Storage / R2
  status      text not null default 'verified',  -- 'pending' | 'verified' | 'rejected' | 'flagged'
  created_at  timestamptz not null default now()
);
create index runs_board on runs (world, build, status, ticks);

create table bests (                             -- лучший забег игрока на мир — быстрый рейтинг
  player_id   uuid references players(id),
  world       text,
  build       text,
  run_id      bigint not null references runs(id),
  ticks       integer not null,
  primary key (player_id, world, build)
);
create index bests_board on bests (world, build, ticks);

create table friends (
  player_id   uuid references players(id),
  friend_id   uuid references players(id),
  primary key (player_id, friend_id)
);

create table challenges (                        -- вызовы по ссылке
  code        text primary key,                  -- 6 символов
  run_id      bigint not null references runs(id),
  created_at  timestamptz not null default now()
);
```

Недельный рейтинг — тот же запрос по `runs` с фильтром `created_at >= начало недели`, либо отдельная таблица `weekly_bests`, которую чистит cron.

## API

| Метод | Путь | Тело / ответ |
|---|---|---|
| POST | `/auth/anon` | → `{ token, player }` |
| PATCH | `/me` | `{ nick?, skin?, hidden? }` |
| POST | `/runs` | `{ world, build, replay (base64, gzip) }` → `{ run_id, status: 'pending' }` |
| GET | `/runs/:id` | статус проверки, время, место |
| GET | `/boards/:world?scope=global\|friends\|week&around=me` | `{ top: [...100], me: { rank, ticks } }` |
| GET | `/replays/:run_id` | бинарный реплей (кешируется CDN навсегда — неизменяем) |
| GET | `/daily` | `{ world: 'daily-YYYY-MM-DD', seed, chunks, ends_at, players }` |
| POST | `/challenges` | `{ run_id }` → `{ code }` |
| GET | `/challenges/:code` | `{ run, player }` |
| WS | `/race/:room` | сообщения ниже |

## Проверка реплея (воркер)

```ts
import { simulate } from '../sim/run';          // тот же код, что в клиенте
export async function verify(r: Replay): Promise<Verdict> {
  if (!activeBuilds.has(r.build)) return reject('unknown build');
  if (r.input.byteLength > 65536) return reject('too large');
  const world = buildWorld(r.world, r.seed);    // level-gen
  const res = simulate(world, decodeRLE(r.input), { maxTicks: 120 * 300 });
  if (!res.finished) return reject('did not finish');
  if (res.ticks !== r.ticks) return reject('time mismatch');
  const flagged = res.ticks < 0.6 * world.parTicks || res.maxInputChangesPerSec > 30;
  return { status: flagged ? 'flagged' : 'verified', ...res };
}
```

После `verified`: вставить в `runs`, обновить `bests`, если время лучше, и разослать уведомления тем, кого обогнали.

## Сообщения онлайн-гонки

```ts
// клиент → сервер
{ t: 'join', nick, skin }
{ t: 'ready' }
{ t: 'pos', tick, x, y, z, s }           // 10 раз в секунду; s — битовая маска состояния (бег/воздух/кувырок/сальто/смерть)
{ t: 'finish', replay }
// сервер → клиент
{ t: 'room', players: [...], world, seed }
{ t: 'start', at }                        // серверное время старта
{ t: 'pos', id, tick, x, y, z, s }
{ t: 'result', places: [{ id, ticks | 'DNF' }] }
```

Синхронизация часов: при входе 5 пингов `{ t: 'ping', c }` → `{ t: 'pong', c, s }`, смещение = медиана `s - (c + rtt/2)`.

## Стоимость (порядок величин)

- Реплей 2 КБ × 100 тыс. забегов в месяц = 200 МБ — копейки в Storage/R2.
- Проверка 50 мс CPU на забег — укладывается в бесплатные лимиты Workers/Edge Functions на старте.
- Рейтинги кешируйте на 10–30 с на CDN: это самый частый запрос.
