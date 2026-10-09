#!/usr/bin/env python3
"""Mago spritesheet — ASCII pixel parts → PNG frames → Aseprite (Guerreiro pipeline).

Layout (KEEP for MagoVisual):
  Cell 64×64, 7 cols × 4 rows = 28 frames (448×256)
  0–3 idle | 4–9 walk | 10–12 jump | 13–16 cast | 17–20 special
  21–22 hurt | 23 silhouette | 24 palette | 25–27 spare idle

Design: hooded face-in-shadow, deep purple robes, silver staff + purple crystal.
"""
from __future__ import annotations

import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parent
# Absolute Mac project roots (used when script runs on the user's machine)
MAC_ART = Path("/Users/gabriellucas/Desktop/trabalho jogo sexta/Assets/Art/Mago")
MAC_RES = Path("/Users/gabriellucas/Desktop/trabalho jogo sexta/Assets/Resources/Mago")

CELL, COLS, ROWS = 64, 7, 4

# Deep purple / indigo + silver staff + purple crystal (~16 colors) — alinhado a mago_conceito.png
C = {
    ".": None,
    "k": (8, 6, 14, 255),        # outline
    "n": (12, 8, 22, 255),       # hood void (rosto vazio)
    "d": (28, 18, 48, 255),      # robe deepest
    "s": (42, 28, 72, 255),      # robe shadow
    "m": (62, 40, 108, 255),     # robe mid
    "r": (88, 58, 148, 255),     # robe
    "l": (118, 82, 188, 255),    # robe light
    "h": (158, 122, 220, 255),   # mantle highlight / quilt
    "q": (138, 98, 198, 255),    # mantle quilt mid
    "a": (148, 152, 172, 255),   # silver staff
    "i": (210, 214, 232, 255),   # silver highlight
    "p": (148, 52, 210, 255),    # crystal purple
    "c": (210, 130, 255, 255),   # crystal bright
    "w": (248, 236, 255, 255),   # crystal core
    "b": (22, 14, 32, 255),      # boot
    "t": (98, 62, 40, 255),      # belt / pouch
    "e": (186, 148, 118, 255),   # mão
    "u": (160, 48, 72, 255),     # hurt tint
    "v": (210, 80, 100, 255),    # hurt bright
    "z": (20, 12, 36, 255),      # capa / fold deep
}

# ---------- M1 idle: silhueta contínua (conceito) — não peças flutuando ----------

# Capuz pontudo do conceito: pico sólido + abertura escura + ombros.
HOOD = [
    "..........kk..........",
    "........kkhhkk........",
    ".......khhqqhhk.......",
    "......khqlrrlqhk......",
    ".....khqlrrrrlqhk.....",
    "....khqlrrrrrrlqhk....",
    "...khqlrrrrrrrrlqhk...",
    "..khqlrrrnnnnrrrlqhk..",
    ".khqlrrrnnnnnnrrrlqhk.",
    ".kqlrrrnnnnnnnnrrrlqk.",
    ".kqlrrrnnnnnnnnrrrlqk.",
    ".kmqlrrrnnnnnnrrrlqmk.",
    ".kmqqlrrrrnnrrrrlqqmk.",
    "..ksqqlrrrrrrrrlqqsk..",
    "..kkssqqlrrrrlqqsskk..",
    "...kkmmssqllqssmmkk...",
    "....kkkkkkkkkkkkkk....",
]

HOOD_HURT = [
    "..........kk..........",
    "........kkvvkk........",
    ".......kvvuuuvk.......",
    "......kvuuuuuuvk......",
    ".....kvuuuuuuuuvk.....",
    "....kvuuuuuuuuuuvk....",
    "...kvuuuuuuuuuuuvk....",
    "..kvuuuunnnnnuuuuvk...",
    ".kvuuuunnnnnnnuuuuvk..",
    ".kuuuuunnnnnnnnuuuuk..",
    ".kuuuuunnnnnnnnuuuuk..",
    ".kuuuuunnnnnnnuuuuuk..",
    ".kuuuuuuunnnnuuuuuuk..",
    "..kuuuuuuuuuuuuuuuk...",
    "..kkuuuuuuuuuuuuukk...",
    "...kkuuuuuuuuuuukk....",
    "....kkkkkkkkkkkkkk....",
]

# Robe + manto acolchoado + cinto/bolsa + barra até as botas (uma peça).
BODY = [
    "kkkkkkkkkkkkkkkk",
    "hqhqhqhhqhqhqhhk",
    "hqlrrlhhllrrlqhk",
    "hlrrrrllllrrrrhk",
    "kmrrrrrllrrrrrmk",
    "ksrrrrrllrrrrrsk",
    "ksrrrrmttmrrrrsk",
    "kdddkteetktddddk",
    "kzdddktttktdddzk",
    "kzddddssssddddzk",
    "kzddddddddddddzk",
    "kkzddddddddddzkk",
    ".kzddddddddddzk.",
    ".kzddbbbbbbddzk.",
    "..kzbbbbbbbbzk..",
    "...kkkkkkkkkk...",
]

BODY_HURT = [
    "kkkkkkkkkkkkkkkk",
    "vuvuvuvvvuvuvuvk",
    "vuuuuuvvuuuuuuvk",
    "vuuuuuvvvuuuuuvk",
    "kuuuuuuvvuuuuuuk",
    "kuuuuuuvvuuuuuuk",
    "kuuuuutttuuuuuuk",
    "kuuukteetkuuuuuk",
    "kuuuuktttkuuuuuk",
    "kuuuuuuuuuuuuuuk",
    "kuuuuuuuuuuuuuuk",
    "kkuuuuuuuuuuuukk",
    ".kuuuuuuuuuuuuk.",
    ".kuuubbbbbbuuuk.",
    "..kzbbbbbbbbzk..",
    "...kkkkkkkkkk...",
]

# Manga larga + mão visível (conceito)
SLEEVE = [
    ".kkkk.",
    "krrrlk",
    "krmrlk",
    "ksmslk",
    ".kddek",
    "..kek.",
    "...k..",
]

# Botas só quando robe levanta no walk/jump — idle a robe cobre quase tudo
LEG = [
    "kkk.",
    "kbk.",
    "kbbk",
    "kkkk",
]

# Cajado prata + cristal losango + nube dither (conceito)
STAFF = [
    "...c.p.w.p.c...",
    "..p..cpwpc..p..",
    "...c.pwpwp.c...",
    "....kcpipck....",
    ".....kaiak.....",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kak......",
    "......kkk......",
]

STAFF_TILT = [
    "....c.p.w.p.c.",
    "...p..cpwpc..p",
    "....c.pwpwp.c.",
    ".....kcpipck..",
    "......kaiak...",
    ".......kak....",
    ".......kak....",
    "......kak.....",
    "......kak.....",
    ".....kak......",
    ".....kak......",
    "....kak.......",
    "....kak.......",
    "...kak........",
    "...kak........",
    "..kkk.........",
]

STAFF_THRUST = [
    "..........c.p.w.p.c.",
    ".........p..cpwpc..p",
    "..........c.pwpwp.c.",
    "...........kcpipck..",
    "............kaiak...",
    ".............kak....",
    ".............kak....",
    ".............kak....",
    ".............kak....",
    ".............kak....",
    ".............kak....",
    ".............kak....",
    ".............kak....",
    ".............kkk....",
]

ORB = [
    "..cwc..",
    ".cpwpc.",
    "cwpwpwc",
    ".cpwpc.",
    "..cwc..",
]

NOVA = [
    "....c...c....",
    "..c..wpw..c..",
    ".c.cpwwwpc.c.",
    "...pwwwwwp...",
    "c.pwwwwwwwp.c",
    "...pwwwwwp...",
    ".c.cpwwwpc.c.",
    "..c..wpw..c..",
    "....c...c....",
]

SPARKS = [
    "c...w...c",
    ".p.....p.",
    "...c.w...",
    ".w.....c.",
    "c...p...w",
]


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
        b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")
    )


def put(pixels, width, x, y, color):
    if color is None or x < 0 or y < 0 or x >= width:
        return
    i = (y * width + x) * 4
    if i + 3 >= len(pixels):
        return
    pixels[i : i + 4] = bytes(color)


def blit(pixels, width, ox, oy, rows, remap=None):
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            color = C.get(ch)
            if remap and ch in remap:
                color = C.get(remap[ch], color)
            put(pixels, width, ox + x, oy + y, color)


def cell_origin(index):
    return (index % COLS) * CELL, (index // COLS) * CELL


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


def draw_mage(pixels, width, ox, oy, pose):
    lean = pose.get("lean", 0)
    # Encaixa capuz 22px + corpo 16px numa célula 64 com pivô nos pés.
    bx = ox + 8 + pose.get("x", 0)
    by = oy + 6 + pose.get("y", 0)
    hurt = pose.get("hurt", False)
    mode = pose.get("mode", "idle")
    staff = pose.get("staff", "side")
    legs = pose.get("legs", "stand")
    back = pose.get("l", 0)
    front = pose.get("r", 0)
    arm = pose.get("arm", 0)
    glow = pose.get("glow", 0)

    hood = HOOD_HURT if hurt else HOOD
    body = BODY_HURT if hurt else BODY

    show_legs = legs != "hide" and mode in ("walk", "jump", "cast", "special")
    leg_y = by + 44 if legs != "tuck" else by + 40
    if show_legs:
        blit(pixels, width, bx + 14 + back, leg_y, LEG)
        blit(pixels, width, bx + 22 + front, leg_y, LEG)

    # Cajado atrás (esquerda do mago)
    staff_x = bx + lean - 2
    staff_y = by + 2
    if staff == "side":
        blit(pixels, width, staff_x, staff_y, STAFF)
    elif staff == "tilt":
        blit(pixels, width, staff_x - 1, staff_y - 1, STAFF_TILT)
    elif staff == "raise":
        blit(pixels, width, staff_x + 2, staff_y - 6 - glow, STAFF)
    elif staff == "thrust":
        blit(pixels, width, staff_x + 4 + arm, staff_y - 2, STAFF_THRUST)
    elif staff == "front":
        blit(pixels, width, bx + lean + 14, by, STAFF)

    # Corpo sob o capuz — overlap de ~6px pra silhueta contínua
    blit(pixels, width, bx + 6 + lean, by + 14, body)

    if mode == "special":
        blit(pixels, width, bx + lean + 1, by + 22, SLEEVE)
        blit(pixels, width, bx + lean + 24, by + 22, SLEEVE)
    elif mode == "cast":
        blit(pixels, width, bx + lean + 22 + arm, by + 18 - arm, SLEEVE)
        blit(pixels, width, bx + lean + 4, by + 24, SLEEVE)
    else:
        blit(pixels, width, bx + lean + 22, by + 24, SLEEVE)
        blit(pixels, width, bx + lean + 3, by + 24, SLEEVE)

    blit(pixels, width, bx + lean, by, hood)

    if pose.get("orb"):
        ox_o = bx + lean + 28 + arm
        oy_o = by + 12 - arm * 2
        blit(pixels, width, ox_o, oy_o, ORB)
        if glow:
            blit(pixels, width, ox_o - 1, oy_o - 4, SPARKS)

    if pose.get("nova"):
        nx = bx + lean + 6
        ny = by - 4 - glow
        blit(pixels, width, nx, ny, NOVA)
        if glow >= 2:
            blit(pixels, width, nx - 2, ny - 2, SPARKS)
            blit(pixels, width, nx + 8, ny + 4, SPARKS)


def poses():
    # 28 poses matching FRAME_MAP
    p = []
    # 0–3 idle — breathe + crystal pulse
    p += [
        {"mode": "idle", "staff": "side", "glow": 0},
        {"mode": "idle", "y": 1, "staff": "side", "glow": 1},
        {"mode": "idle", "staff": "side", "glow": 0},
        {"mode": "idle", "y": 1, "staff": "side", "glow": 1},
    ]
    # 4–9 walk
    p += [
        {"mode": "walk", "lean": 1, "l": -2, "r": 2, "staff": "tilt"},
        {"mode": "walk", "lean": 1, "y": -1, "l": -1, "r": 1, "staff": "tilt", "glow": 1},
        {"mode": "walk", "lean": 1, "y": -1, "l": 0, "r": 0, "staff": "side"},
        {"mode": "walk", "lean": 1, "l": 2, "r": -2, "staff": "tilt", "glow": 1},
        {"mode": "walk", "lean": 1, "y": -1, "l": 1, "r": -1, "staff": "tilt"},
        {"mode": "walk", "lean": 1, "y": -1, "l": 0, "r": 0, "staff": "side", "glow": 1},
    ]
    # 10–12 jump
    p += [
        {"mode": "jump", "y": -3, "l": -3, "r": 3, "staff": "raise", "glow": 1},
        {"mode": "jump", "y": -6, "legs": "tuck", "staff": "raise", "glow": 2},
        {"mode": "jump", "y": -1, "l": -4, "r": 4, "staff": "tilt"},
    ]
    # 13–16 cast — staff thrust / orb launch
    p += [
        {"mode": "cast", "lean": -1, "staff": "raise", "arm": 0, "glow": 1},
        {"mode": "cast", "lean": 0, "staff": "raise", "arm": 1, "orb": True, "glow": 1},
        {"mode": "cast", "lean": 2, "x": 1, "staff": "thrust", "arm": 3, "orb": True, "glow": 2},
        {"mode": "cast", "lean": 1, "staff": "thrust", "arm": 2, "orb": True, "glow": 1},
    ]
    # 17–20 special — arcane nova
    p += [
        {"mode": "special", "staff": "front", "nova": True, "glow": 0},
        {"mode": "special", "y": -1, "staff": "front", "nova": True, "glow": 1},
        {"mode": "special", "y": -2, "staff": "front", "nova": True, "glow": 2},
        {"mode": "special", "y": -1, "staff": "front", "nova": True, "glow": 3},
    ]
    # 21–22 hurt
    p += [
        {"mode": "idle", "hurt": True, "x": -3, "lean": -1, "l": -2, "r": 1, "staff": "tilt"},
        {"mode": "idle", "hurt": True, "x": -2, "lean": -1, "staff": "tilt"},
    ]
    # 23 silhouette placeholder (drawn specially)
    p.append({"mode": "sil", "staff": "side"})
    # 24 palette placeholder
    p.append({"mode": "pal"})
    # 25–27 spare idle variants
    p += [
        {"mode": "idle", "staff": "tilt", "glow": 1},
        {"mode": "idle", "y": 1, "staff": "side", "glow": 0},
        {"mode": "idle", "lean": 1, "staff": "raise", "glow": 2},
    ]
    assert len(p) == 28, len(p)
    return p


def crop_cell(sheet, sheet_w, index):
    ox, oy = cell_origin(index)
    cell = bytearray(CELL * CELL * 4)
    for y in range(CELL):
        src = ((oy + y) * sheet_w + ox) * 4
        dest = y * CELL * 4
        cell[dest : dest + CELL * 4] = sheet[src : src + CELL * 4]
    return cell


def to_silhouette(pixels, width, ox, oy):
    for y in range(CELL):
        for x in range(CELL):
            i = ((oy + y) * width + (ox + x)) * 4
            if i + 3 < len(pixels) and pixels[i + 3] > 10:
                pixels[i : i + 4] = bytes(C["k"])


def draw_palette_cell(pixels, width, ox, oy):
    keys = "kndsmrlhqzaipcwbtu"
    for i, key in enumerate(keys):
        color = C[key]
        x0 = ox + 4 + (i % 8) * 7
        y0 = oy + 12 + (i // 8) * 20
        for yy in range(16):
            for xx in range(6):
                put(pixels, width, x0 + xx, y0 + yy, color)
        for xx in range(6):
            put(pixels, width, x0 + xx, y0, C["k"])
            put(pixels, width, x0 + xx, y0 + 15, C["k"])
        for yy in range(16):
            put(pixels, width, x0, y0 + yy, C["k"])
            put(pixels, width, x0 + 5, y0 + yy, C["k"])


def build_sheet(out_root: Path | None = None):
    root = out_root or ROOT
    width, height = COLS * CELL, ROWS * CELL
    pixels = bytearray(width * height * 4)
    for i, pose in enumerate(poses()):
        ox, oy = cell_origin(i)
        if pose.get("mode") == "pal":
            draw_palette_cell(pixels, width, ox, oy)
            continue
        draw_mage(pixels, width, ox, oy, pose)
        outline_cell(pixels, width, ox, oy)
        if pose.get("mode") == "sil":
            to_silhouette(pixels, width, ox, oy)

    sheet_path = root / "Mago_sheet.png"
    write_png(sheet_path, width, height, pixels)

    frames_dir = root / "frames"
    frames_dir.mkdir(parents=True, exist_ok=True)
    for i in range(28):
        write_png(frames_dir / f"m_{i:02d}.png", CELL, CELL, crop_cell(pixels, width, i))

    # Resources copy when running inside Unity project tree
    res = root.parents[1] / "Resources" / "Mago" / "sheet.png"
    try:
        if root.parents[1].name == "Assets" or (root.parents[1] / "Resources").exists() or True:
            write_png(res, width, height, pixels)
    except Exception:
        pass

    # Explicit Mac resources path if present
    if MAC_RES.parent.exists() or False:
        pass
    if (root.parents[1] / "Resources").exists():
        write_png(root.parents[1] / "Resources" / "Mago" / "sheet.png", width, height, pixels)

    return sheet_path, width, height


def build_palette(root: Path | None = None):
    root = root or ROOT
    keys = "kndsmrlhqzaipcwbtu"
    pixels = bytearray(len(keys) * 24 * 24 * 4)
    for i, key in enumerate(keys):
        color = C[key]
        for y in range(24):
            for x in range(24):
                px = i * 24 + x
                idx = (y * len(keys) * 24 + px) * 4
                pixels[idx : idx + 4] = bytes(color)
    write_png(root / "paleta.png", len(keys) * 24, 24, pixels)


if __name__ == "__main__":
    # Prefer Mac project path when present; else script directory
    out = MAC_ART if MAC_ART.parent.exists() else ROOT
    out.mkdir(parents=True, exist_ok=True)
    sheet, w, h = build_sheet(out)
    build_palette(out)
    # Always also write Resources sibling if Assets tree
    assets = out.parents[1] if out.name == "Mago" else None
    if assets and assets.name == "Assets":
        res_dir = assets / "Resources" / "Mago"
        res_dir.mkdir(parents=True, exist_ok=True)
        # re-read sheet bytes
        (res_dir / "sheet.png").write_bytes(sheet.read_bytes())
        print("resources", res_dir / "sheet.png")
    print("ok", sheet, f"{w}x{h}")
