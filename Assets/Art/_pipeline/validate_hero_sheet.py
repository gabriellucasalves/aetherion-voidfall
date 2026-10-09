#!/usr/bin/env python3
"""Valida um herói contra o molde do Guerreiro. Não altera sprites.

Uso: python3 Assets/Art/_pipeline/validate_hero_sheet.py Mago
"""
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(Path(__file__).resolve().parent))
from measure_hero import measure_sheet, fmt  # noqa: E402

HEROES = {
    "Guerreiro": ROOT / "Assets/Resources/Guerreiro/sheet.png",
    "Mago": ROOT / "Assets/Resources/Mago/sheet.png",
    "Arqueiro": ROOT / "Assets/Resources/Arqueiro/sheet.png",
}


def main():
    names = sys.argv[1:] or list(HEROES)
    failed = 0
    for name in names:
        path = HEROES.get(name) or Path(name)
        if not path.exists():
            print("FALTA", path)
            failed += 1
            continue
        m = measure_sheet(path)
        print(fmt(m))
        if name != "Guerreiro" and (not m.get("height_ok") or not m.get("baseline_ok")):
            failed += 1
            print("  → fora do molde (altura 24–30 e baseline y≈60). Ver StyleGuide.")
        elif name == "Guerreiro":
            print("  → referência (molde).")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
