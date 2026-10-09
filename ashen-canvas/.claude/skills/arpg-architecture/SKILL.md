---
name: arpg-architecture
description: Архитектура «Пепельного холста» на Unity 6 + URP — разделение на чистую симуляцию Sim (без UnityEngine) и слой Game, сборки asmdef, данные и seed, где хранить состояние, жизненный цикл зоны, сохранения, производительность (пулы, меши, свет), проверка компиляции и тесты из командной строки без Unity. Используй, когда добавляешь новую систему, решаешь, куда положить код, переносишь прототип с IMGUI/болванок на продакшен-решения, ускоряешь игру или настраиваешь сборку. Keywords: architecture, Unity, URP, asmdef, Sim, determinism, seed, save, performance, pooling, compile check, dotnet test.
---

# Архитектура

## Слои

```
Assets/_Project/Code/
  Sim/   AshenCanvas.Sim   noEngineReferences — правила игры, генерация, числа. Тестируется в .NET.
    Core/ Rng (xorshift64*), Hash (SplitMix64)
    Stats/ Stat, StatBlock, Element (пигменты)        Combat/ DamageCalc
    Skills/ SkillDb   Enemies/ EnemyDb   Items/ ItemDb, ItemGen, Loot, Vendor, Item
    World/ ZoneDb, DungeonGen, HubLayout, DungeonLayout (сетка + движение кругом), FlowField
    Quests/ QuestDb, QuestOps    Progression/ HeroOps    Run/ GameState, GameOps
  Game/  AshenCanvas.Game  MonoBehaviour-клей: GameRoot, Hero, Enemy, SkillCaster, ZoneView, Fx, GameUI
    Art/ AssetProvider (модель из Resources/Models или болванка), PlaceholderFactory, Mats
  Editor/ AshenCanvas.Editor  меню «Ashen Canvas»
Tests/EditMode  — тесты Sim (те же файлы гоняет tools/SimTests)
```

Правило: **решения принимает Sim, Game только показывает и передаёт ввод.** Формула урона, шанс выпадения, генерация карты, награда за задание — в Sim. Позиции, эффекты, тайминги анимаций — в Game.

## Детерминизм

- Всё случайное в правилах — `Sim.Core.Rng` от seed. `UnityEngine.Random` — только косметика (брызги, разброс клякс, повороты декора).
- Seed мира выбирается при новой игре (`GameRoot.StartNewGame`), дальше: `GameOps.ZoneSeed(state, zone)` = hash(seed мира, зона, номер захода). Каждый заход — новая карта, но повторяемая.
- Отдельные потоки: `CombatRng` (крит, разброс урона), `LootRng` (дроп). Не смешивать: иначе лишний удар меняет лут.
- `string.GetHashCode()` случаен между запусками .NET — используйте `GetHashCodeStable()` из `GameOps`.

## Состояние и сохранения

- `GameState` — единственное, что сохраняется. Только открытые поля, `List<>`, `[Serializable]` — так требует `JsonUtility`.
- Нельзя: `Dictionary`, свойства, `null` в списках (используйте `IsEmpty`), `ulong` (храним `long`).
- Новое поле → значение по умолчанию в объявлении; ломающее изменение → `GameState.CurrentVersion++` и миграция в `SaveSystem.Load`.
- Автосохранение: вход в лагерь, победа над боссом, выход из игры, кнопка в паузе.

## Жизненный цикл зоны (`GameRoot.EnterZone`)

1. `ClearZone()` — уничтожает корень зоны, чистит списки, эффекты, корутины.
2. Seed → `DungeonGen.Generate` (с проверкой проходимости) → `ZoneView.Build` (меши пола/стен, свет, туман, декор).
3. Героиня на `Map.Start`, монстры по `Map.Packs`, босс в `Map.Boss`, в лагере — NPC.
4. Всё созданное в зоне — дети `ZoneRoot`. Не вешайте объекты зоны на `GameRoot` — они переживут смену зоны.

## Производительность (цель: 60 FPS на ПК средней руки и на телефоне 2021 года)

- Пол и стены — 4 меша на зону (`ZoneView`), а не объект на клетку.
- Капли краски — одна `ParticleSystem` с `Emit` (`FxSystem`), кляксы — кольцевой пул из 160 квадов.
- Болванки из примитивов дороги по draw calls (~10–30 на персонажа). Это нормально для прототипа; финальные модели — один меш, 1–2 материала (см. `asset-pipeline-3d`).
- Свет: точечные источники только у свечей/фонарей и фонарь героини; URP Forward+ держит десятки, но тени — только у солнца.
- `GameUI` на IMGUI выделяет память каждый кадр. Перед релизом — перенос на UI Toolkit (`ui-hud-inventory`).
- Перед оптимизацией — Profiler (через Unity MCP можно снимать данные, см. `unity-mcp-workflow`).

## Проверки без Unity

```bash
dotnet test tools/SimTests                       # все правила, генерация 300 seed на зону, лут, задания
dotnet build tools/CompileCheck                  # код Unity компилируется (справочные сборки UnityEngine 2021.3)
dotnet build tools/CompileCheck -p:NewInput=true # то же для ветки новой системы ввода
```

CompileCheck собирается против UnityEngine 2021.3 — **не используйте API, появившиеся только в Unity 6** (например, `Rigidbody.linearVelocity`), иначе проверка упадёт, а если используете — обновите пакет справочных сборок. Поведение проверяется только в редакторе.

## Чего избегать

- Правки YAML сцен и префабов вручную. Сцена одна (`Boot`), всё строится кодом.
- Логики в `OnGUI`, кроме реакции на кнопку.
- Синглтонов кроме `GameRoot.I`, `FxSystem.I`, `CameraRig.I`.
- Встроенной физики для героини и врагов: движение — `DungeonLayout.MoveCircle` по сетке (быстро и одинаково на всех устройствах).
