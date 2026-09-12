#!/usr/bin/env python3
"""Spritesheet do Arqueiro — mesmo grid do Guerreiro (448×192, célula 64, 7×3).

Paleta floresta/agilidade (capa verde, couro, arco de madeira). Sem asas/pena.

Mapa de índices (linha-major):
  Idle      0  1  2  3
  Walk      4  5  6  7  8  9
  Jump     10 11 12
  Shoot    13 14 15      # puxa → solta → recolhe
  Dash     16 17
  Special  18            # soltura em leque
  Hurt     19 20
"""

from __future__ import annotations

import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parent
OUT_ART = ROOT / "Arqueiro_sheet.png"
OUT_RES = ROOT.parents[1] / "Resources" / "Arqueiro" / "sheet.png"

CELL = 64
COLS = 7
ROWS = 3

C = {
    ".": None,
    "k": (10, 8, 6, 255),
    "n": (26, 18, 12, 255),
    "d": (58, 36, 20, 255),
    "b": (98, 64, 32, 255),
    "m": (148, 102, 48, 255),
    "w": (196, 148, 72, 255),
    "g": (18, 52, 40, 255),
    "e": (28, 102, 64, 255),
    "l": (48, 158, 92, 255),
    "t": (70, 210, 130, 255),
    "s": (214, 164, 118, 255),
    "p": (168, 112, 78, 255),
    "h": (42, 24, 18, 255),
    "y": (255, 214, 64, 255),
    "c": (110, 240, 180, 255),
    "a": (236, 240, 220, 255),
    "u": (255, 250, 230, 255),
    "o": (255, 140, 40, 255),
}


def write_png(path: Path, width: int, height: int, pixels: bytearray) -> None:
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        raw.extend(pixels[y * width * 4 : (y + 1) * width * 4])

    def chunk(tag: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", ihdr)
        + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
        + chunk(b"IEND", b"")
    )


def put(pixels, width, x, y, color):
    if color is None or x < 0 or y < 0 or x >= width:
        return
    i = (y * width + x) * 4
    if i + 3 >= len(pixels):
        return
    pixels[i : i + 4] = bytes(color)


def blit(pixels, width, ox, oy, rows):
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            put(pixels, width, ox + x, oy + y, C.get(ch))


def outline_cell(pixels, width, ox, oy):
    opaque = set()
    for y in range(CELL):
        for x in range(CELL):
            i = ((oy + y) * width + (ox + x)) * 4
            if i + 3 < len(pixels) and pixels[i + 3] > 10:
                opaque.add((x, y))
    for x, y in opaque:
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < CELL and 0 <= ny < CELL and (nx, ny) not in opaque:
                put(pixels, width, ox + nx, oy + ny, C["k"])


def cell_origin(index):
    return (index % COLS) * CELL, (index // COLS) * CELL


# Peças maiores / mais legíveis (chibi como o Guerreiro)
HEAD = [
    "....khhhhhhk....",
    "...khhhhhhhhk...",
    "..khhsssssshhk..",
    "..khspuupspshk..",
    "..ksssuussussk..",
    "..kssssssssssk..",
    "...kpsssssspk...",
    "....kkpkkkpk....",
    ".....kkkkkk.....",
]

CAP = [
    "...hhhhhh...",
    "..hhhhhhhh..",
    ".hh......hh.",
]

BODY = [
    ".kkeeeeeeeekk.",
    "keellllllleeek",
    "kellttttlleek.",
    ".kebbbbbbbek..",
    ".kbbbbbbbbk...",
    "..knbdddbnk...",
    "...kkkkkkkk...",
]

LEG = [
    "kkkkk",
    "kbbbk",
    "kbdbk",
    "kdddk",
    "kdmmk",
    "kkkkk",
]

# Arco em C aberto para a direita (silhueta clara)
BOW = [
    "......kwk...",
    ".....kw.wk..",
    "....kw...wk.",
    "...kw.....wk",
    "...kw.....wk",
    "...kw.....wk",
    "....kw...wk.",
    ".....kw.wk..",
    "......kwk...",
]

BOW_DRAWN = [
    "......kwk...",
    ".....kw.wk..",
    "....kw...wk.",
    "..kw.......wk",
    "..kw.......wk",
    "..kw.......wk",
    "....kw...wk.",
    ".....kw.wk..",
    "......kwk...",
]

STRING = [
    "k",
    "u",
    "k",
    "u",
    "k",
    "u",
    "k",
]

ARROW_H = ["ycwwwwa"]
ARROW_SHORT = ["ycwa"]

QUIVER = [
    ".kddk.",
    "kbbbdk",
    "kbaybk",
    "kbaybk",
    "kbbbdk",
    ".kkkk.",
]

STREAK = [
    "t.......",
    ".tt.t...",
    "..ttt...",
    "...tt...",
    "....t...",
]

FAN = [
    "c........",
    ".cy......",
    "..cyw....",
    "...cywa..",
    "....cya..",
    ".....ca..",
]


def draw_archer(pixels, width, ox, oy, pose):
    lean = pose.get("lean", 0)
    bx = ox + 14 + pose.get("x", 0)
    by = oy + 30 + pose.get("y", 0)
    legs = pose.get("legs", "stand")
    bow = pose.get("bow", "idle")
    arrow = pose.get("arrow", None)
    fan = pose.get("fan", False)
    streak = pose.get("streak", False)
    back = pose.get("l", 0)
    front = pose.get("r", 0)

    leg_y = by + 22 if legs != "tuck" else by + 20
    blit(pixels, width, bx + 8 + back, leg_y, LEG)
    blit(pixels, width, bx + 14 + front, leg_y, LEG)

    # aljava nas costas
    blit(pixels, width, bx + lean - 2, by + 14, QUIVER)

    blit(pixels, width, bx + 5 + lean, by + 14, BODY)
    blit(pixels, width, bx + lean + 2, by + 1, CAP)
    blit(pixels, width, bx + lean, by + 3, HEAD)

    bow_x = bx + lean + 22
    bow_y = by + 10
    if bow == "draw":
        blit(pixels, width, bow_x - 1, bow_y, BOW_DRAWN)
        blit(pixels, width, bow_x + 2, bow_y + 1, STRING)
    else:
        blit(pixels, width, bow_x, bow_y, BOW)

    if arrow == "nocked":
        blit(pixels, width, bow_x - 2, bow_y + 4, ARROW_H)
    elif arrow == "fly":
        blit(pixels, width, bow_x + 8, bow_y + 4, ARROW_H)
    elif arrow == "held":
        blit(pixels, width, bow_x + 1, bow_y + 4, ARROW_SHORT)

    if fan:
        blit(pixels, width, bow_x + 7, bow_y + 1, FAN)
        blit(pixels, width, bow_x + 10, bow_y + 4, ARROW_H)
        blit(pixels, width, bow_x + 8, bow_y + 7, ["cya"])

    if streak:
        blit(pixels, width, bx + lean - 10, by + 16, STREAK)


def poses():
    return [
        # Idle 0-3
        {"bow": "idle", "arrow": "held"},
        {"bow": "idle", "arrow": "held", "y": 1},
        {"bow": "idle", "arrow": "held"},
        {"bow": "idle", "arrow": "held", "y": 1},
        # Walk 4-9
        {"lean": 1, "l": -2, "r": 2, "bow": "idle", "arrow": "held"},
        {"lean": 1, "y": -1, "l": -1, "r": 1, "bow": "idle", "arrow": "held"},
        {"lean": 1, "y": -1, "bow": "idle", "arrow": "held"},
        {"lean": 1, "l": 2, "r": -2, "bow": "idle", "arrow": "held"},
        {"lean": 1, "y": -1, "l": 1, "r": -1, "bow": "idle", "arrow": "held"},
        {"lean": 1, "y": -1, "bow": "idle", "arrow": "held"},
        # Jump 10-12
        {"y": -2, "l": -3, "r": 3, "bow": "idle", "arrow": "held"},
        {"y": -4, "legs": "tuck", "bow": "idle", "arrow": "held"},
        {"y": -1, "l": -4, "r": 4, "bow": "idle", "arrow": "held"},
        # Shoot 13-15
        {"lean": -1, "bow": "draw", "arrow": "nocked"},
        {"lean": 1, "x": 1, "bow": "idle", "arrow": "fly"},
        {"bow": "idle", "arrow": "held"},
        # Dash 16-17
        {"lean": 2, "x": 2, "l": 2, "r": -1, "bow": "idle", "streak": True},
        {"lean": 2, "x": 3, "l": 3, "r": -2, "bow": "idle", "streak": True},
        # Special 18
        {"lean": 1, "x": 1, "bow": "idle", "fan": True},
        # Hurt 19-20
        {"x": -3, "lean": -1, "l": -2, "r": 1, "bow": "idle"},
        {"x": -2, "lean": -1, "l": -1, "bow": "idle"},
    ]


def build_sheet():
    width, height = COLS * CELL, ROWS * CELL
    pixels = bytearray(width * height * 4)
    for i, pose in enumerate(poses()):
        ox, oy = cell_origin(i)
        draw_archer(pixels, width, ox, oy, pose)
        outline_cell(pixels, width, ox, oy)
    write_png(OUT_ART, width, height, pixels)
    write_png(OUT_RES, width, height, pixels)
    print("ok", OUT_ART)
    print("ok", OUT_RES)


if __name__ == "__main__":
    build_sheet()
