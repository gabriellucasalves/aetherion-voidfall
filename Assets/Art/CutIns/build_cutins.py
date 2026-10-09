#!/usr/bin/env python3
"""Cut-ins de especial â€” estilo fighting game 8/16-bit (MvC / KOF / anime anos 90).

Gera, para cada herÃ³i jogÃ¡vel, uma tira de 8 frames 240x135 (16:9) exibida
como overlay rÃ¡pido (~0.45s) quando o especial dispara (SpecialCutIn.cs):

  frame 0      flash branco (super flash) com silhueta do herÃ³i
  frames 1-2   busto entra deslizando da direita, faixa diagonal + speed lines
  frames 3-6   busto assentado com shake de 1px; speed lines correndo
  frame 7      outro â€” faixa esmaece

O busto Ã© recortado do frame 0 (idle) do sheet de gameplay do prÃ³prio herÃ³i
(`Assets/Resources/<Pasta>/sheet.png`) e ampliado 5x em nearest neighbor â€”
mesma identidade visual, zero redesenho. ReferÃªncias de estilo documentadas
em FRAME_MAP.md.

SaÃ­da: Assets/Resources/CutIns/<id>.png (tira 1920x135, 8 cÃ©lulas de 240px).
Rodar Ã  mÃ£o: `python3 Assets/Art/CutIns/build_cutins.py` (nÃ£o roda em build).
"""
from __future__ import annotations

import random
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent
RES = ROOT.parents[1] / "Resources"
OUT = RES / "CutIns"

CELL_W, CELL_H = 240, 135
FRAMES = 10
GRID_COLS = 5  # 5x2 = 1200x270 (cabe no maxTextureSize 2048)
CELL = 64  # cÃ©lula dos sheets de gameplay
SCALE = 5  # ampliaÃ§Ã£o nearest do busto

HEROES = {
    # id: (pasta em Resources, cor escura da faixa, cor mÃ©dia, cor clara/linhas)
    "guerreiro": ("Guerreiro", (24, 26, 38), (52, 58, 86), (255, 176, 64)),
    "mago": ("Mago", (16, 8, 30), (52, 26, 90), (196, 120, 255)),
    "arqueiro": ("Arqueiro", (28, 24, 12), (74, 60, 26), (255, 224, 120)),
}


def load_idle(folder: str) -> Image.Image:
    sheet = Image.open(RES / folder / "sheet.png").convert("RGBA")
    return sheet.crop((0, 0, CELL, CELL))


def bust(idle: Image.Image) -> Image.Image:
    """Recorta cabeÃ§a+torso da silhueta (topo do bbox opaco) e amplia 5x."""
    bbox = idle.getbbox()
    if bbox is None:
        return idle.resize((CELL * SCALE, CELL * SCALE), Image.NEAREST)
    left, top, right, bottom = bbox
    depth = min(22, bottom - top)  # cabeÃ§a + torso
    crop = idle.crop((max(0, left - 1), top, min(CELL, right + 1), top + depth))
    return crop.resize((crop.width * SCALE, crop.height * SCALE), Image.NEAREST)


def silhouette(img: Image.Image, color: tuple[int, int, int]) -> Image.Image:
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    px_in, px_out = img.load(), out.load()
    for y in range(img.height):
        for x in range(img.width):
            if px_in[x, y][3] > 8:
                px_out[x, y] = (*color, 255)
    return out


def in_band(x: int, y: int) -> bool:
    """Faixa diagonal (estilo cut-in): sobe da esquerda-baixa Ã  direita-alta."""
    center = 92 - (x * 40) // CELL_W  # y do meio da faixa desloca com x
    return abs(y - center) <= 46


def draw_band(frame: Image.Image, dark, mid, light, alpha: int) -> None:
    px = frame.load()
    for y in range(CELL_H):
        for x in range(CELL_W):
            if not in_band(x, y):
                continue
            center = 92 - (x * 40) // CELL_W
            d = abs(y - center)
            if d >= 45:
                px[x, y] = (*light, alpha)  # borda clara de 2px
            elif d >= 42:
                px[x, y] = (*mid, alpha)
            else:
                px[x, y] = (*dark, alpha)


def draw_speed_lines(frame: Image.Image, light, mid, offset: int, rng: random.Random) -> None:
    px = frame.load()
    for _ in range(26):
        y = rng.randrange(8, CELL_H - 8)
        length = rng.randrange(30, 130)
        x0 = (rng.randrange(0, CELL_W) - offset) % CELL_W
        thick = 2 if rng.random() < 0.3 else 1
        color = light if rng.random() < 0.55 else mid
        for dx in range(length):
            x = x0 + dx
            if x >= CELL_W:
                break
            for dy in range(thick):
                if in_band(x, y + dy):
                    px[x, y + dy] = (*color, 255)


def build_hero(hero_id: str, folder: str, dark, mid, light) -> Image.Image:
    art = bust(load_idle(folder))
    flash_art = silhouette(art, (255, 255, 255))
    rows = (FRAMES + GRID_COLS - 1) // GRID_COLS
    strip = Image.new("RGBA", (CELL_W * GRID_COLS, CELL_H * rows), (0, 0, 0, 0))

    base_x = CELL_W - art.width - 24
    base_y = (CELL_H - art.height) // 2

    for i in range(FRAMES):
        rng = random.Random(hash(hero_id) % 9973 + i * 131)
        frame = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))

        if i == 0:
            # Super flash clássico: tela escurece e o herói vira silhueta branca
            draw_band(frame, dark, mid, (255, 255, 255), 250)
            frame.alpha_composite(flash_art, (base_x, base_y))
        else:
            alpha = 235 if i < FRAMES - 1 else 150
            draw_band(frame, dark, mid, light, alpha)
            draw_speed_lines(frame, light, mid, offset=i * 41, rng=rng)
            slide = max(0, 3 - i) * 18  # entra da direita nos frames 1-2
            shake = (i % 2) if 3 <= i <= FRAMES - 2 else 0
            frame.alpha_composite(art, (base_x + slide + shake, base_y))

        strip.alpha_composite(frame, ((i % GRID_COLS) * CELL_W, (i // GRID_COLS) * CELL_H))

    return strip


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for hero_id, (folder, dark, mid, light) in HEROES.items():
        strip = build_hero(hero_id, folder, dark, mid, light)
        path = OUT / f"{hero_id}.png"
        strip.save(path)
        print(f"{hero_id}: {strip.width}x{strip.height} -> {path.relative_to(RES.parents[0])}")


if __name__ == "__main__":
    main()
