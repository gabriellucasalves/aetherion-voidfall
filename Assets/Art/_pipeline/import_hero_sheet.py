#!/usr/bin/env python3
"""Monta sheet 7×N a partir de frames PNG e copia para Resources.

Não redesenha arte. Só empacota o que já existe.

Uso:
  python3 Assets/Art/_pipeline/import_hero_sheet.py Mago
  python3 Assets/Art/_pipeline/import_hero_sheet.py Mago --from-frames

Espera:
  Art/<Hero>/<Hero>_sheet.png   OU   Art/<Hero>/frames/*_00.png …
  Destino: Resources/<Hero>/sheet.png
"""
from __future__ import annotations

import argparse
import shutil
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
CELL, COLS = 64, 7


def pack_frames(frames: list[Path], dest: Path) -> None:
    n = len(frames)
    rows = (n + COLS - 1) // COLS
    sheet = Image.new("RGBA", (COLS * CELL, rows * CELL), (0, 0, 0, 0))
    for i, fp in enumerate(frames):
        im = Image.open(fp).convert("RGBA")
        if im.size != (CELL, CELL):
            raise SystemExit(f"{fp} não é 64×64: {im.size}")
        sheet.paste(im, ((i % COLS) * CELL, (i // COLS) * CELL))
    dest.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(dest)
    print("sheet", dest, sheet.size)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("hero", choices=["Guerreiro", "Mago", "Arqueiro"])
    p.add_argument("--from-frames", action="store_true")
    args = p.parse_args()

    art = ROOT / "Assets" / "Art" / args.hero
    res = ROOT / "Assets" / "Resources" / args.hero / "sheet.png"
    art_sheet = art / f"{args.hero}_sheet.png"

    if args.from_frames:
        frames_dir = art / "frames"
        frames = sorted(frames_dir.glob("*.png"))
        if not frames:
            raise SystemExit(f"sem frames em {frames_dir}")
        pack_frames(frames, art_sheet)
        shutil.copy2(art_sheet, res)
    else:
        if not art_sheet.exists():
            raise SystemExit(f"falta {art_sheet} — use --from-frames")
        im = Image.open(art_sheet)
        if im.width != COLS * CELL or im.height % CELL:
            raise SystemExit(f"sheet inválido {im.size} (esperado 448×N*64)")
        shutil.copy2(art_sheet, res)
        print("copied", art_sheet, "→", res)

    sys.path.insert(0, str(Path(__file__).resolve().parent))
    from measure_hero import fmt, measure_sheet

    print(fmt(measure_sheet(res)))


if __name__ == "__main__":
    main()
