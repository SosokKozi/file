---
name: asset-pipeline-3d
description: Конвейер 3D-ассетов «Пепельного холста» — каталог ассетов (id, промпт, источник), болванки из кода, Blender через MCP (сборка, доводка, экспорт glb), генерация через Tripo API (text/image → 3D, сухой прогон без ключа), импорт glb через glTFast в Resources/Models, замена болванки без правок кода, бюджеты полигонов и материалов, проверка. Используй, когда нужна новая модель, подключаешь Tripo, работаешь в Blender, импортируешь glb или модель выглядит/стоит не так. Keywords: asset pipeline, 3D models, Blender, Blender MCP, Tripo, Tripo API, text to 3D, glTF, glb, glTFast, placeholder, catalog, polycount, LOD.
---

# Конвейер 3D-ассетов

```
asset_catalog.json ──► болванка в коде (сразу играбельно)
        │
        ├─► Tripo API (tools/tripo) ──► Art/Generated/<id>.glb (сырой)
        │                                       │
        └─► Blender MCP: построить/довести ◄────┘
                    │  (масштаб, ось, узлы Body/BrushPivot/BrushTip, ретопо, цвета)
                    ▼
        Resources/Models/<id>.glb ──► AssetProvider подхватывает вместо болванки
```

## 1. Каталог — `Assets/_Project/Art/asset_catalog.json`

Одна запись на модель: `id` (тот же, что в коде), `name`, `source` (`placeholder` | `blender` | `tripo` | `final`), `height_m`, `prompt`. Общий стиль — поле `style`. Тест `AssetCatalogTests` следит, чтобы каждая модель из EnemyDb/ItemDb была в каталоге.

## 2. Болванки — `Game/Art/PlaceholderFactory.cs`

Примитивы Unity, узнаваемый силуэт + цвета из арт-дирекшна. Обязательны для каждого id: игра никогда не ждёт ассетов.

## 3. Blender через MCP

- `.mcp.json`: сервер `blender` = `uvx blender-mcp`. В Blender: аддон *Blender MCP* включён, в боковой панели (N) вкладка BlenderMCP → *Connect to Claude* (порт 9876).
- Работает только локально (Claude Code на том же компьютере, что и Blender).
- Скрипт-образец `tools/blender/make_placeholders.py`: героиня и Выцветший с правильными осями и узлами. Через MCP — «выполни содержимое файла в Blender», без интерфейса — `blender -b -P tools/blender/make_placeholders.py -- --only hero`.
- Доводка сгенерированной модели: импорт glb → масштаб до `height_m` → начало координат к ногам → лицом к −Y → переименовать узлы (`Body`, `BrushPivot`, `BrushTip`) → Decimate до бюджета → материалы плоского цвета → экспорт glb (+Y up).

## 4. Tripo — `tools/tripo/tripo_generate.py`

- Без ключа — сухой прогон: печатает промпты, ничего не отправляет.
- С ключом: `TRIPO_API_KEY=... python3 tools/tripo/tripo_generate.py --only hero --run` → задача text_to_model → ожидание → скачивание `Art/Generated/<id>.glb`.
- Ключ только в переменной окружения (или секретах окружения Claude), **никогда в репозитории**.
- Адреса и поля API сверить с документацией Tripo перед первым запуском. Позже: image_to_model по концепту, анимация/риг средствами Tripo, если тариф позволяет.
- После генерации: `source` → `tripo`, затем доводка в Blender (шаг 3).

## 5. Импорт в Unity

- Пакет glTFast в `Packages/manifest.json` — `.glb` импортируется как модель-префаб.
- Готовый файл: `Assets/_Project/Resources/Models/<id>.glb`. `AssetProvider.Spawn` ищет `Resources.Load<GameObject>("Models/<id>")` — нашёл → модель, нет → болванка. Коллайдеры из модели удаляются.
- Меню **Ashen Canvas → Каталог ассетов: что уже заменено** — отчёт в консоль.
- Позже, когда моделей станет много: Addressables вместо Resources (группы по зонам).

## Бюджеты

| Тип | Треугольники | Материалы | Текстуры |
|---|---|---|---|
| Героиня | ≤ 15k | 2 (тело + кисть) | 1024², или плоские цвета |
| NPC | ≤ 8k | 1–2 | 512² |
| Обычный враг | ≤ 4k | 1 | 512² |
| Босс | ≤ 20k | 2 | 1024² |
| Декор | ≤ 1.5k | 1 | атлас зоны |
| Лут на земле | ≤ 500 | 1 | — |

Tripo выдаёт больше — обязательно Decimate/ретопология в Blender.

## Проверка модели

1. Скриншот из игры с высоты камеры: силуэт читается, не сливается с полом зоны.
2. Рост и ориентация верные (стоит лицом к камере в лагере, ноги на полу).
3. Узлы на месте: кисть машет, кончик меняет цвет.
4. Профайлер: draw calls на модель ≤ 2.
