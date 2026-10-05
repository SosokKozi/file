---
name: unity-architecture
description: Архитектура проекта «Тропа бурь» на Unity 6 LTS + URP — структура папок и сборок (asmdef), отделение детерминированной симуляции на чистом C# от представления, фиксированный шаг 120 Гц, данные в ScriptableObject и JSON, Addressables, новая система ввода, UI Toolkit, сохранения, производительность на телефоне, настройки Git для Unity, сборка и тесты из командной строки. Используй, когда нужно создать проект, добавить систему, решить, куда положить код, настроить сборку, тесты, ввод, сохранения или ускорить игру. Keywords: Unity, URP, C#, asmdef, ScriptableObject, Addressables, Input System, UI Toolkit, batchmode, tests, mobile performance, git.
---

# Архитектура Unity-проекта

## Создание проекта

- Unity 6 LTS, шаблон **Universal 3D** (URP). Платформы: Android, iOS, Windows/macOS (позже WebGL по желанию).
- Project Settings: Editor → Asset Serialization **Force Text**, Version Control **Visible Meta Files**; Player → Active Input Handling **Input System Package (New)**; Scripting Backend **IL2CPP** для релиза.
- Пакеты: Input System, Addressables, Cinemachine, Animation Rigging, Test Framework, URP.
- Официальный плагин Unity для Claude Code (прописан в `.claude/settings.json`) — для пакетов, UI, аудио, физики, оптимизации.

## Структура

```
Assets/
  _Project/
    Code/
      Sim/            (asmdef: StormPath.Sim — noEngineReferences: true)
        Core/         Fixed-step loop, Rng (xorshift/PCG), Hash, FixedMath (если понадобится)
        Movement/     MovementTuning (обычный класс, заполняется из SO), PlayerSim, Collide
        Health/ Abilities/ Biomes/ Run/
        Gen/          RouteMapGen, LevelGen, ChunkBuilders
        Validate/     Reachability, Bots
      Data/           (asmdef: StormPath.Data) ScriptableObject-описания: BiomeDef, ChunkDef, AbilityDef, UpgradeDef, TalentNode, SkinDef
      Game/           (asmdef: StormPath.Game) MonoBehaviour-клей: GameLoop, LevelView, HeroView, CameraRig, Spawners, Pools
      UI/             (UI Toolkit: UXML/USS + контроллеры) карта, HUD, меню, итоги
      Audio/  Meta/  Net/  Save/
    Tests/
      EditMode/       (asmdef, ссылается на Sim) генератор, проверка, детерминизм
      PlayMode/       смоук-тесты сцены
    Art/  Audio/  Data/ (ассеты .asset)  Scenes/ (Boot, Map, Run — минимальные)
```

Правило: **`Sim` не зависит от UnityEngine** (`noEngineReferences`). Векторы — свои `struct V3` или `System.Numerics.Vector3`. Это даёт быстрые EditMode-тесты, массовые прогоны seed и проверку забегов на сервере (.NET без Unity).

## Цикл

```csharp
public sealed class GameLoop : MonoBehaviour {
  const double Step = 1.0 / 120.0; double acc;
  void Update() {
    acc += Math.Min(Time.unscaledDeltaTime, 0.1);
    while (acc >= Step) { sim.Tick(input.Sample()); acc -= Step; }
    view.Render(sim.State, (float)(acc / Step));   // интерполяция
  }
}
```

Не используйте `FixedUpdate` и встроенную физику (Rigidbody) для героя и препятствий трассы: своя кинематика с AABB, как в прототипе. Встроенная физика — только для декоративных обломков.

Детерминизм `float`: между ПК (x64) и телефонами (ARM, IL2CPP) результаты могут чуть расходиться. Для рейтинга проверяйте забег на сервере с допуском по итоговому времени ±1 тик, либо переведите `Sim` на fixed-point (`FixedMath`), если расхождения станут проблемой.

## Данные

- Числа и описания — ScriptableObject в `Assets/_Project/Data`; на старте конвертируются в обычные классы `Sim`.
- Уровни и карта — генерируются; сохраняются в JSON (`LevelData`, `RouteMap`).
- Addressables: группы по биомам (модели, текстуры, музыка) — подгружаются при выборе точки на карте.

## Ввод

Input System, карта действий `Run`: `Move` (ось), `Jump`, `Roll`, `Ability`, `Pause`. Схемы: клавиатура и мышь, геймпад, сенсор (экранные кнопки + жесты). `InputFrame` для `Sim` собирается раз в тик; нажатия буферизуются до ближайшего тика.

## Сохранения

`Application.persistentDataPath/save.json`: мета (эссенция, таланты, облики, рекорды) + текущий забег (карта, позиция, HP, дары, seed). Запись атомарно (во временный файл → переименование) после каждой точки карты.

## Производительность (телефон)

- ≤ 120 SetPass, ≤ 250 тыс. треугольников, ≤ 1.5 мс на скрипты, GC в кадре — 0 байт (пулы, без LINQ и `foreach` по интерфейсам в горячем коде).
- Статика уровня: Static Batching или SRP Batcher-совместимые материалы; повторяющиеся объекты — GPU Instancing.
- Target frame rate 60, адаптивное разрешение (`ScalableBufferManager`), если кадр > 18 мс.
- Профилирование — Unity Profiler на устройстве (Development Build + Autoconnect Profiler).

## Git

- `.gitignore` для Unity (Library, Temp, Obj, Build, Logs, UserSettings).
- Git LFS для `*.psd *.png *.tga *.fbx *.blend *.wav *.ogg *.mp3` и других бинарных форматов.
- Сцены и префабы — маленькие; контент собирается из данных кодом, чтобы изменения были читаемы в диффе и Claude мог их править.

## Командная строка (для Claude и CI)

```sh
# тесты
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile -
# сборка Android
Unity -batchmode -nographics -quit -projectPath . -executeMethod StormPath.Build.Android -logFile -
```

`Assets/_Project/Code/Editor/Build.cs` содержит методы `Android`, `iOS`, `Windows`. В облачной сессии Claude Unity не установлен: там Claude пишет код и тесты `Sim`, которые можно запускать и через обычный `dotnet test` (отдельный `.csproj`, ссылающийся на папку `Sim`), а сборку и PlayMode-тесты — на вашем компьютере или в CI (game-ci).
