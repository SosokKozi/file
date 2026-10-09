#!/usr/bin/env python3
"""
Генерация 3D-моделей через Tripo API по каталогу ассетов.

Пока ключа нет, скрипт работает в режиме «сухого прогона»: показывает, какие запросы
отправил бы, и ничего не скачивает. Игра в это время использует болванки из кода
(PlaceholderFactory), так что ничего не ломается.

    python3 tools/tripo/tripo_generate.py                 # сухой прогон по всем болванкам
    python3 tools/tripo/tripo_generate.py --only hero     # один ассет
    TRIPO_API_KEY=... python3 tools/tripo/tripo_generate.py --only hero --run

Результат: Assets/_Project/Art/Generated/<id>.glb (сырой) — затем пройти шаг «доводка в Blender»
из docs/PIPELINE.md и положить итог в Assets/_Project/Resources/Models/<id>.glb.

ВНИМАНИЕ: адреса и поля API записаны по документации Tripo на момент написания.
Перед первым настоящим запуском сверьте их с https://platform.tripo3d.ai/docs — API меняется.
Ключ — только из переменной окружения, никогда не коммитьте его.
"""
import argparse
import json
import os
import sys
import time
import urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CATALOG = os.path.join(ROOT, "Assets", "_Project", "Art", "asset_catalog.json")
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Generated")
API = os.environ.get("TRIPO_API_BASE", "https://api.tripo3d.ai/v2/openapi")


def request(method, url, key, body=None):
    data = json.dumps(body).encode("utf-8") if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", "Bearer " + key)
    req.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.loads(r.read().decode("utf-8"))


def build_prompt(style, asset):
    return asset["prompt"] + ". " + style


def generate(asset, style, key):
    prompt = build_prompt(style, asset)
    task = request("POST", API + "/task", key, {"type": "text_to_model", "prompt": prompt})
    task_id = task["data"]["task_id"]
    print("  задача", task_id)
    while True:
        time.sleep(5)
        info = request("GET", API + "/task/" + task_id, key)["data"]
        status = info.get("status")
        print("  статус:", status, info.get("progress", ""))
        if status == "success":
            out = info.get("output", {})
            url = out.get("pbr_model") or out.get("model")
            if not url:
                raise RuntimeError("в ответе нет ссылки на модель: " + json.dumps(out))
            os.makedirs(OUT_DIR, exist_ok=True)
            path = os.path.join(OUT_DIR, asset["id"] + ".glb")
            urllib.request.urlretrieve(url, path)
            return path
        if status in ("failed", "cancelled", "banned", "expired", "unknown"):
            raise RuntimeError("задача не удалась: " + status)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--only", nargs="*", help="id ассетов из каталога")
    ap.add_argument("--run", action="store_true", help="на самом деле отправить запросы (нужен TRIPO_API_KEY)")
    args = ap.parse_args()

    with open(CATALOG, encoding="utf-8") as f:
        catalog = json.load(f)
    style = catalog.get("style", "")
    assets = [a for a in catalog["assets"] if a.get("source") == "placeholder"]
    if args.only:
        assets = [a for a in catalog["assets"] if a["id"] in args.only]

    key = os.environ.get("TRIPO_API_KEY", "")
    live = args.run and bool(key)
    if args.run and not key:
        print("TRIPO_API_KEY не задан — работаю как сухой прогон.", file=sys.stderr)

    for a in assets:
        print(f"[{a['id']}] {a['name']}")
        print("  промпт:", build_prompt(style, a))
        if live:
            path = generate(a, style, key)
            print("  сохранено:", os.path.relpath(path, ROOT))
    if not live:
        print(f"\nСухой прогон: {len(assets)} ассет(ов). Добавьте --run и TRIPO_API_KEY, чтобы сгенерировать.")


if __name__ == "__main__":
    main()
