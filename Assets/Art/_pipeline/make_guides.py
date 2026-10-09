#!/usr/bin/env python3
"""Gera o overlay de baseline 64×64 (não é sprite de gameplay)."""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent
CELL = 64
BASELINE = 60
TOP_MAX = 31  # silhueta idle não sobe acima disto


def main():
    im = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    px = im.load()
    # caixa de altura-alvo (Guerreiro)
    for x in range(CELL):
        px[x, BASELINE] = (255, 70, 70, 200)
        px[x, TOP_MAX] = (80, 180, 255, 90)
    for y in range(TOP_MAX, BASELINE + 1):
        px[32, y] = (255, 220, 80, 70)
    # padding dos pés (61–63)
    for y in range(61, 64):
        for x in range(CELL):
            if x % 2 == y % 2:
                px[x, y] = (255, 70, 70, 40)
    out = ROOT / "guia_baseline.png"
    im.save(out)
    print("ok", out)


if __name__ == "__main__":
    main()
