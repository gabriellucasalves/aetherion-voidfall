#!/usr/bin/env python3
"""Gera a spritesheet do Mago (placeholder procedural jogável).

Layout (igual convenção do Guerreiro, com 1 linha extra p/ especial):
  - Célula 64×64, 7 colunas × 4 linhas = 28 frames (448×256)
  - Recursos: Assets/Resources/Mago/sheet.png
  - Arte:    Assets/Art/Mago/Mago_sheet.png

Mapa de frames (índice linear, esquerda→direita, cima→baixo):
  0–3   idle
  4–9   walk
 10–12  jump
 13–16  cast  (Orbe básico)
 17–20  special (Núcleo Arcano)
 21–22  hurt
 23     silhueta
 24     paleta
 25–27  spare / idle variants

Paleta dark-fantasy (~12 cores): robe índigo, pele, cajado marrom, orbe ciano.
Polir depois no Aseprite local — este script garante sheet jogável na VM.
"""
from __future__ import annotations

import math
from pathlib import Path

from PIL import Image

CELL, COLS, ROWS = 64, 7, 4
ROOT = Path(__file__).resolve().parent
OUT_ART = ROOT / "Mago_sheet.png"
OUT_RES = ROOT.parents[1] / "Resources" / "Mago" / "sheet.png"

PAL = {
    "bg": (0, 0, 0, 0),
    "out": (12, 10, 22, 255),
    "robe_d": (28, 24, 58, 255),
    "robe": (48, 42, 98, 255),
    "robe_l": (78, 70, 140, 255),
    "trim": (90, 70, 160, 255),
    "skin": (210, 185, 155, 255),
    "skin_s": (170, 140, 115, 255),
    "staff": (62, 42, 28, 255),
    "staff_l": (110, 78, 42, 255),
    "orb": (90, 200, 255, 255),
    "orb_c": (200, 240, 255, 255),
    "glow": (120, 160, 255, 180),
    "hurt": (160, 50, 80, 255),
    "boot": (22, 18, 36, 255),
}


def new_frame() -> Image.Image:
    return Image.new("RGBA", (CELL, CELL), PAL["bg"])


def px(img: Image.Image, x: int, y: int, c) -> None:
    if 0 <= x < CELL and 0 <= y < CELL and c[3] > 0:
        img.putpixel((x, y), c)


def fill_rect(img, x0, y0, x1, y1, c) -> None:
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px(img, x, y, c)


def outline_rect(img, x0, y0, x1, y1, c) -> None:
    for x in range(x0, x1 + 1):
        px(img, x, y0, c)
        px(img, x, y1, c)
    for y in range(y0, y1 + 1):
        px(img, x0, y, c)
        px(img, x1, y, c)


def fill_ellipse(img, x0, y0, x1, y1, c) -> None:
    cx = (x0 + x1) / 2.0
    cy = (y0 + y1) / 2.0
    rx = max(1.0, (x1 - x0) / 2.0)
    ry = max(1.0, (y1 - y0) / 2.0)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                px(img, x, y, c)


def draw_mage(
    img: Image.Image,
    *,
    bob: int = 0,
    leg_phase: int = 0,
    arm: str = "idle",
    staff_raise: int = 0,
    orb_scale: float = 1.0,
    hurt: bool = False,
    jump: int = 0,
    special: int = 0,
) -> None:
    cx = 32
    base_y = 40 + bob + jump

    robe = PAL["hurt"] if hurt else PAL["robe"]
    robe_d = PAL["hurt"] if hurt else PAL["robe_d"]
    robe_l = (200, 90, 110, 255) if hurt else PAL["robe_l"]
    skin = PAL["skin_s"] if hurt else PAL["skin"]

    # boots / legs
    if leg_phase == 0:
        fill_rect(img, cx - 7, base_y + 16, cx - 2, base_y + 22, PAL["boot"])
        fill_rect(img, cx + 2, base_y + 16, cx + 7, base_y + 22, PAL["boot"])
    elif leg_phase == 1:
        fill_rect(img, cx - 9, base_y + 15, cx - 3, base_y + 22, PAL["boot"])
        fill_rect(img, cx + 1, base_y + 17, cx + 6, base_y + 21, PAL["boot"])
    elif leg_phase == 2:
        fill_rect(img, cx - 6, base_y + 17, cx - 1, base_y + 21, PAL["boot"])
        fill_rect(img, cx + 3, base_y + 15, cx + 9, base_y + 22, PAL["boot"])
    else:  # tuck
        fill_rect(img, cx - 6, base_y + 14, cx - 1, base_y + 19, PAL["boot"])
        fill_rect(img, cx + 1, base_y + 14, cx + 6, base_y + 19, PAL["boot"])

    # robe
    fill_rect(img, cx - 9, base_y - 4, cx + 9, base_y + 16, robe)
    fill_rect(img, cx - 10, base_y + 12, cx + 10, base_y + 17, robe_d)
    fill_rect(img, cx - 1, base_y - 2, cx + 1, base_y + 14, robe_l)

    # hood / head
    fill_ellipse(img, cx - 8, base_y - 22, cx + 8, base_y - 6, robe)
    fill_ellipse(img, cx - 6, base_y - 18, cx + 6, base_y - 7, skin)
    for dy in range(12):
        half = max(1, 8 - dy // 2)
        col = PAL["trim"] if dy < 3 else robe
        fill_rect(img, cx - half, base_y - 22 - dy, cx + half, base_y - 22 - dy, col)
    eye = PAL["hurt"] if hurt else PAL["orb"]
    px(img, cx - 3, base_y - 14, eye)
    px(img, cx + 2, base_y - 14, eye)

    # staff
    sx = cx - 16
    staff_top = base_y - 20 - staff_raise
    for y in range(staff_top, base_y + 21):
        px(img, sx, y, PAL["staff"])
        px(img, sx + 1, y, PAL["staff_l"])
    r = max(2, int(4 * orb_scale))
    fill_ellipse(img, sx - r, staff_top - 2 - r, sx + r, staff_top - 2 + r, PAL["orb"])
    fill_ellipse(
        img,
        sx - max(1, r // 2),
        staff_top - 2 - max(1, r // 2),
        sx + max(1, r // 2) - 1,
        staff_top - 2,
        PAL["orb_c"],
    )

    if arm == "idle":
        fill_rect(img, cx + 7, base_y - 2, cx + 11, base_y + 8, robe_l)
    elif arm == "cast":
        fill_rect(img, cx + 6, base_y - 6, cx + 18, base_y - 1, robe_l)
        fill_ellipse(img, cx + 16, base_y - 12, cx + 24, base_y - 4, PAL["orb"])
        fill_ellipse(img, cx + 18, base_y - 10, cx + 22, base_y - 7, PAL["orb_c"])
    elif arm == "cast2":
        fill_rect(img, cx + 7, base_y - 8, cx + 20, base_y - 2, robe_l)
        fill_ellipse(img, cx + 18, base_y - 16, cx + 28, base_y - 6, PAL["orb"])
        for dx, dy in ((30, -14), (26, -18), (32, -8)):
            px(img, cx + dx, base_y + dy, PAL["orb_c"])
    elif arm == "special":
        fill_rect(img, cx - 14, base_y - 10, cx - 8, base_y - 4, robe_l)
        fill_rect(img, cx + 8, base_y - 10, cx + 14, base_y - 4, robe_l)
        R = 6 + special * 2
        fill_ellipse(
            img,
            cx - R,
            base_y - 28 - special,
            cx + R,
            base_y - 16 - special,
            PAL["glow"],
        )
        fill_ellipse(
            img,
            cx - R + 2,
            base_y - 26 - special,
            cx + R - 2,
            base_y - 18 - special,
            PAL["orb"],
        )
        fill_ellipse(
            img,
            cx - 2,
            base_y - 24 - special,
            cx + 2,
            base_y - 20 - special,
            PAL["orb_c"],
        )
        for a in range(0, 360, 45):
            rad = math.radians(a + special * 20)
            px(
                img,
                int(cx + math.cos(rad) * (R + 4)),
                int(base_y - 22 - special + math.sin(rad) * (R + 2)),
                PAL["orb_c"],
            )

    for y in range(base_y - 4, base_y + 16):
        px(img, cx - 10, y, PAL["out"])
        px(img, cx + 10, y, PAL["out"])


def build_sheet() -> Image.Image:
    sheet = Image.new("RGBA", (COLS * CELL, ROWS * CELL), (0, 0, 0, 0))
    frames: list[Image.Image] = []

    for i, bob in enumerate([0, -1, 0, 1]):
        f = new_frame()
        draw_mage(f, bob=bob, orb_scale=1.0 + i * 0.05)
        frames.append(f)

    walk_legs = [1, 0, 2, 0, 1, 2]
    walk_bob = [0, -1, 0, -1, 0, -1]
    for i in range(6):
        f = new_frame()
        draw_mage(f, bob=walk_bob[i], leg_phase=walk_legs[i])
        frames.append(f)

    for jump, leg in [(-4, 3), (-2, 3), (2, 0)]:
        f = new_frame()
        draw_mage(f, jump=jump, leg_phase=leg, staff_raise=2)
        frames.append(f)

    for arm, raise_, scale in [
        ("idle", 0, 1.0),
        ("cast", 2, 1.2),
        ("cast2", 4, 1.5),
        ("cast", 2, 1.1),
    ]:
        f = new_frame()
        draw_mage(f, arm=arm, staff_raise=raise_, orb_scale=scale)
        frames.append(f)

    for i in range(4):
        f = new_frame()
        draw_mage(f, arm="special", staff_raise=4 + i, orb_scale=0.6, special=i, bob=-i)
        frames.append(f)

    for i in range(2):
        f = new_frame()
        draw_mage(f, hurt=True, bob=1 + i, orb_scale=0.7)
        frames.append(f)

    # 23 silhouette
    f = new_frame()
    draw_mage(f)
    for y in range(CELL):
        for x in range(CELL):
            p = f.getpixel((x, y))
            if p[3] > 0:
                f.putpixel((x, y), PAL["out"])
    frames.append(f)

    # 24 palette
    f = new_frame()
    colors = [
        PAL["robe_d"],
        PAL["robe"],
        PAL["robe_l"],
        PAL["trim"],
        PAL["skin"],
        PAL["staff"],
        PAL["orb"],
        PAL["orb_c"],
        PAL["glow"],
        PAL["boot"],
        PAL["out"],
        PAL["hurt"],
    ]
    for ci, col in enumerate(colors):
        x0 = 4 + (ci % 6) * 9
        y0 = 10 + (ci // 6) * 22
        fill_rect(f, x0, y0, x0 + 7, y0 + 16, col)
        outline_rect(f, x0, y0, x0 + 7, y0 + 16, PAL["out"])
    frames.append(f)

    for i in range(3):
        f = new_frame()
        draw_mage(f, bob=i - 1, orb_scale=1.0 + i * 0.08)
        frames.append(f)

    assert len(frames) == COLS * ROWS
    for i, fr in enumerate(frames):
        sheet.paste(fr, ((i % COLS) * CELL, (i // COLS) * CELL), fr)
    return sheet


def main() -> None:
    sheet = build_sheet()
    OUT_ART.parent.mkdir(parents=True, exist_ok=True)
    OUT_RES.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT_ART)
    sheet.save(OUT_RES)
    print(f"wrote {OUT_ART} and {OUT_RES} ({sheet.size[0]}x{sheet.size[1]})")


if __name__ == "__main__":
    main()
