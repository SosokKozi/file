---
name: unity-mcp-workflow
description: Работа с редактором Unity через MCP (сервер MCP for Unity на localhost:8080 из .mcp.json) — открыть проект, создать сцену Boot, нажать Play, прочитать консоль, запустить EditMode-тесты, проверить сцену и объекты, снять профайл, поправить настройки проекта (URP, ввод, Always Included Shaders). Используй, когда нужно что-то сделать именно в редакторе Unity, проверить игру вживую, разобрать ошибки консоли или когда MCP Unity не отвечает. Keywords: Unity MCP, editor, play mode, console, tests, project settings, URP asset, input handling.
---

# Unity через MCP

## Подключение

- `.mcp.json` проекта: `unityMCP` → `http://localhost:8080/mcp` (пакет **MCP for Unity**, CoplayDev). В Unity: *Window → MCP for Unity → Start Server* (или Auto-Setup). Индикатор должен быть зелёным.
- Работает только когда Claude Code запущен **на том же компьютере**, что и Unity. Облачная сессия Claude до localhost пользователя не достаёт — там проверяем код через `tools/CompileCheck` и `tools/SimTests`.
- Если в проекте используется другой Unity MCP (официальный плагин Unity или IvanMurzak Unity-MCP) — поменяйте запись в `.mcp.json`; остальное в этом навыке не меняется.
- Не отвечает → проверить, что редактор открыт, сервер запущен, порт 8080 свободен; `claude mcp list` показывает статус.

## Первый запуск проекта

1. Unity Hub → *Add project from disk* → папка `ashen-canvas`. Версия — Unity 6 LTS.
2. Пакеты из `Packages/manifest.json` подтянутся сами (URP, Input System, Test Framework, glTFast). Если Package Manager ругается на версию — обновить до предложенной.
3. Меню **Ashen Canvas → Создать сцену Boot** (сцена + запись в Build Settings).
4. URP: *Project Settings → Graphics* — назначить URP Asset (создать: *Assets → Create → Rendering → URP Asset (with Universal Renderer)*). Без него игра работает на встроенном конвейере — материалы это учитывают.
5. *Player → Active Input Handling*: **Both** или **Input System Package** — `GameInput` поддерживает оба.
6. Play. Должен появиться титульный экран «Пепельный холст».

## Что просить у MCP

| Задача | Как |
|---|---|
| Ошибки компиляции | прочитать консоль (read console), исправить, повторить |
| Тесты | запустить EditMode-тесты (test runner) — те же, что `dotnet test tools/SimTests` |
| Проверить, что игра стартует | Play → подождать → консоль без ошибок → Stop |
| Иерархия в Play | найти `GameRoot`, `Zone ...`, `Hero`, посчитать врагов |
| Настройки | Graphics (URP Asset), Player (ввод), *Always Included Shaders*: `Sprites/Default`, `Universal Render Pipeline/Simple Lit` — для сборок, т.к. материалы создаются кодом через `Shader.Find` |
| Профайл | Profiler: CPU main thread, draw calls, GC Alloc (IMGUI даёт аллокации — известно) |

## Правила

- Сцены и префабы не редактировать как текст. Изменения сцены — через MCP-инструменты или код (`Editor/ProjectMenu.cs`).
- После правок кода: консоль чистая → тесты зелёные → Play-смоук. Только потом коммит.
- Не включать в коммит `Library/`, `Temp/`, `Logs/`, `UserSettings/` (есть `.gitignore`).
