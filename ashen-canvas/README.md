# Пепельный холст

Мультяшная ARPG на Unity 6 в духе Path of Exile и Diablo. Художница Мирра возвращает краски выцветшему миру — кистью, брызгами и взрывами.

## Запуск

1. Unity Hub → **Add project from disk** → эта папка. Версия: Unity 6 LTS (6000.0 или новее).
2. Дождаться импорта пакетов (URP, Input System, Test Framework, glTFast).
3. Меню **Ashen Canvas → Создать сцену Boot**.
4. *Project Settings → Graphics* — назначить URP Asset (если его нет, игра работает и без него, на встроенном конвейере).
5. **Play** → «Новая игра».

Игра стартует и в любой другой сцене: `GameRoot` создаётся сам при нажатии Play.

## Управление

WASD — бег · мышь — прицел · ЛКМ, ПКМ, Q, E, R, F — навыки · 1 — зелье · Пробел — говорить и подбирать · I — сумка · C — героиня · K — навыки · Esc — меню.

## Что уже играбельно

- Пролог «Пепельная часовня»: темнота, свечи, фонарь героини, 3–15 врагов, босс Серый Глашатай с ударом по площади, залпом и призывом.
- Лагерь «Радуга»: Тётушка Гуашь (задания), Мастер Сангина (кисти и палитры), Лис Индиго (одежда, украшения, зелья), Мольберт путей.
- Три процедурные зоны с разными врагами и боссами; каждый заход — новая карта.
- 7 навыков кисти, ранги, 40 уровней.
- Лут: 24 основы, 23 свойства с ярусами, 4 редкости, 3 уникальных предмета; торговля; сохранения.

## Проверки без Unity

```bash
dotnet test tools/SimTests
dotnet build tools/CompileCheck
dotnet build tools/CompileCheck -p:NewInput=true
```

## MCP: Blender и Unity

`.mcp.json` подключает `blender` (`uvx blender-mcp`, аддон Blender MCP, порт 9876) и `unityMCP` (MCP for Unity, `http://localhost:8080/mcp`). Claude Code нужно запускать **в этой папке на том же компьютере**, где открыты Blender и Unity. Подробности — навыки `unity-mcp-workflow` и `asset-pipeline-3d`.

## Модели

Сейчас все модели — болванки из примитивов (`PlaceholderFactory`). Положите `Assets/_Project/Resources/Models/<id>.glb` — игра возьмёт модель вместо болванки. Список id и промпты — `Assets/_Project/Art/asset_catalog.json`; генерация через Tripo — `tools/tripo/tripo_generate.py` (без ключа — сухой прогон).

Пайплайн разработки — [`docs/PIPELINE.md`](docs/PIPELINE.md), дизайн — [`docs/GDD.md`](docs/GDD.md).
