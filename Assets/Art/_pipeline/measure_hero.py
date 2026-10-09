#!/usr/bin/env python3
"""Mede silhueta/baseline de um sheet 64×N (alpha > 10). Não altera arquivos."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

CELL = 64
ALPHA = 10


def measure_frame(im: Image.Image, index: int, cols: int = 7) -> dict:
    col, row = index % cols, index // cols
    cell = im.crop((col * CELL, row * CELL, (col + 1) * CELL, (row + 1) * CELL))
    xs, ys = [], []
    for y in range(CELL):
        for x in range(CELL):
            if cell.getpixel((x, y))[3] > ALPHA:
                xs.append(x)
                ys.append(y)
    if not ys:
        return {"empty": True}
    return {
        "empty": False,
        "y0": min(ys),
        "y1": max(ys),
        "h": max(ys) - min(ys) + 1,
        "w": max(xs) - min(xs) + 1,
        "pad_below": CELL - 1 - max(ys),
        "baseline_ok": max(ys) in (59, 60, 61),
        "height_ok": 24 <= (max(ys) - min(ys) + 1) <= 30,
    }


def measure_sheet(path: Path, idle: int = 0) -> dict:
    im = Image.open(path).convert("RGBA")
    cols = im.width // CELL
    rows = im.height // CELL
    m = measure_frame(im, idle, cols)
    m["sheet"] = str(path)
    m["grid"] = f"{cols}x{rows}"
    m["size"] = f"{im.width}x{im.height}"
    return m


def fmt(m: dict) -> str:
    if m.get("empty"):
        return f"{m.get('sheet')}: frame vazio"
    flag_h = "OK" if m["height_ok"] else "FORA"
    flag_b = "OK" if m["baseline_ok"] else "FLUTUA"
    return (
        f"{m['sheet']}\n"
        f"  grade {m['grid']}  idle y={m['y0']}-{m['y1']}  h={m['h']} [{flag_h}]  "
        f"pad_below={m['pad_below']} [{flag_b}]  w={m['w']}"
    )
