#!/usr/bin/env python3
"""Etapa 11 — redraw do Mago no molde do Guerreiro.

Célula 64×64, baseline y=60, silhueta ~27 px.
Identidade: capuz void, robe escura, capa, cajado + luz.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent
RES = ROOT.parents[1] / "Resources" / "Mago" / "sheet.png"
CELL, COLS, ROWS = 64, 7, 4
FLOOR = 60

C = {
    "k": (6, 4, 10, 255),
    "n": (8, 5, 14, 255),
    "z": (14, 8, 26, 255),
    "d": (20, 12, 36, 255),
    "s": (32, 18, 56, 255),
    "m": (46, 26, 78, 255),
    "r": (62, 36, 104, 255),
    "l": (86, 54, 132, 255),
    "h": (118, 92, 158, 255),
    "a": (132, 136, 152, 255),
    "i": (198, 202, 218, 255),
    "p": (118, 42, 176, 255),
    "c": (186, 108, 230, 255),
    "w": (240, 228, 252, 255),
    "b": (24, 14, 18, 255),
    "t": (82, 50, 32, 255),
    "e": (168, 128, 98, 255),
}


def new_cell() -> Image.Image:
    return Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))


def put(im: Image.Image, x: int, y: int, col: tuple[int, int, int, int] | None) -> None:
    if col is None or x < 0 or y < 0 or x >= CELL or y >= CELL:
        return
    im.putpixel((x, y), col)


def blit(im: Image.Image, ox: int, oy: int, rows: list[str], remap: dict | None = None) -> None:
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch == ".":
                continue
            key = remap.get(ch, ch) if remap else ch
            put(im, ox + x, oy + y, C.get(key))


def outline(im: Image.Image) -> None:
    opaque = {
        (x, y)
        for y in range(CELL)
        for x in range(CELL)
        if im.getpixel((x, y))[3] > 10
    }
    for x, y in list(opaque):
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < CELL and 0 <= ny < CELL and (nx, ny) not in opaque:
                put(im, nx, ny, C["k"])


# Silhueta ~27 linhas. Cajado à direita, capa à esquerda.
# O blit assenta o último pixel em y=60.

# 23 linhas de corpo+capuz → + botas/outline ≈ 26–28 px (molde Guerreiro)
BODY = [
    "....kkkk....",
    "...kllllk...",
    "..klrrrrlk..",
    ".klrrnnrrlk.",
    ".krrnnnnrrk.",
    ".krrnnnnrrk.",
    ".kmrrnnrrmk.",
    "..ksrrrrsk..",
    ".kdsrrrrsdk.",
    ".kdssrrssdk.",
    ".kdsrtttsdk.",
    ".kdddtdtddk.",
    ".kzddddddzk.",
    ".kzddddddzk.",
    ".kzddddddzk.",
    ".kzddddddzk.",
    ".kzddddddzk.",
    "..kddddddk..",
    "..kbddbdk...",
    "...kbbbk....",
    "....kkk.....",
]

CAPE = [
    ".kk....",
    "kzdk...",
    "kzddk..",
    "kzddk..",
    "kzdddk.",
    "kzdddk.",
    "kzsddk.",
    ".zssdk.",
    ".zsssk.",
    "..zssk.",
    "..zzkk.",
]

STAFF = [
    ".cwc.",
    "cpwpc",
    ".pap.",
    ".aia.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kak.",
    ".kkk.",
]

# capa varrida para cima (apice/queda do pulo)
CAPE_UP = CAPE[::-1]

HAND = [
    "eek",
    "eek",
]

BOOT = [
    "bbk",
    "kkk",
]


def draw(
    cape_dx: int = 0,
    bob: int = 0,
    staff_dx: int = 0,
    glow: int = 0,
    boots: bool = False,
    hurt: bool = False,
    pose: str = "",
) -> Image.Image:
    im = new_cell()
    # Âncora: corpo termina em FLOOR. BODY tem 15 linhas → oy = 60-14 = 46? 
    # Wait BODY 15 rows: oy + 14 = 60 → oy = 46. Top of body = 46. That's only 15px.
    # Need body starting higher. BODY should fill 27px: start y=34.
    # 15-row body from y=46 is too short vs warrior 27px... 
    # Warrior is 27 including horns. 15px body is TOO SMALL on screen?
    # Spec: 26-30. So BODY must be ~27 rows OR we add hood above.
    #
    # Recalc: BODY listed is 15 rows. Add 4 hood rows already IN body (rows 0-5 are hood).
    # 15 rows from y=46 → y=46..60 = 15px. TOO SHORT vs 27 target.
    #
    # Stretch: start BODY at y = 60 - 26 = 34 if we have 27 rows.
    # I'll use oy = 36 for a 15-row body → height 15, PLUS cape/staff extending.
    # Staff is 15 rows too. If staff starts 4px above body, total h ~19. Still short.
    #
    # Better: oy_body = 38 (body y 38-52? 15 rows → 38-52). Then boots at 58-60.
    # That's still short.
    #
    # 15 rows with oy=46: y=46-60. Height 15. Spec wants 26-30.
    # I need a TALLER sprite ~27 rows.

    # Rebuild positions with a taller composed figure:
    # hood 8 + torso 12 + hem/boots 7 = 27, oy = 34.

    # BODY tem 21 linhas: oy=40 → y=40–60. pack_to_floor acerta o baseline.
    oy = 38 + bob
    ox = 21

    # pose do pulo: capa, cajado e bainha da robe mudam de verdade
    cape_rows, cape_ox, cape_oy = CAPE, ox - 5 + cape_dx, oy + 7
    staff_dy = 0
    body_rows = BODY
    if pose == "rise":
        cape_rows, cape_ox, cape_oy = CAPE, ox - 7 + cape_dx, oy + 10
        staff_dy = -2
        body_rows = BODY[:-4]
    elif pose == "apex":
        cape_rows, cape_ox, cape_oy = CAPE_UP, ox - 7 + cape_dx, oy + 4
        staff_dy = -3
        body_rows = BODY[:-3]
    elif pose == "fall":
        cape_rows, cape_ox, cape_oy = CAPE_UP, ox - 6 + cape_dx, oy + 2
        staff_dy = 1
        body_rows = BODY[:-2]
    elif pose.startswith("land"):
        cape_ox, cape_oy = ox - 4 + cape_dx, oy + 8

    blit(im, cape_ox, cape_oy, cape_rows)

    staff_rows = list(STAFF)
    if glow:
        staff_rows[0] = ".wcw."
        staff_rows[1] = "cwwwc"
    blit(im, ox + 11 + staff_dx, oy + staff_dy, staff_rows)

    remap = {"l": "r", "r": "s", "m": "d"} if hurt else None
    blit(im, ox, oy + 2, body_rows, remap)

    blit(im, ox + 11 + staff_dx, oy + 11 + staff_dy, HAND)

    # botas (walk) ou barra da robe no chão
    if pose == "rise":
        # pernas recolhidas assimetricas + rastro de vento abaixo
        blit(im, ox + 2, FLOOR - 4, BOOT)
        blit(im, ox + 7, FLOOR - 6, BOOT)
        for sx, sy, key in ((ox + 3, FLOOR - 1, "s"), (ox + 4, FLOOR, "m"), (ox + 9, FLOOR - 2, "s"), (ox - 1, FLOOR - 1, "m")):
            put(im, sx, sy, C[key])
    elif pose == "apex":
        blit(im, ox + 3, FLOOR - 4, BOOT)
        blit(im, ox + 7, FLOOR - 4, BOOT)
    elif pose == "fall":
        blit(im, ox + 2, FLOOR - 2, BOOT)
        blit(im, ox + 7, FLOOR - 1, BOOT)
        put(im, ox - 1, oy + 3, C["s"])
        put(im, ox + 12, oy + 4, C["s"])
    elif pose == "land":
        # squash: botas abertas + poeira nas laterais
        blit(im, ox + 1, FLOOR - 1, BOOT)
        blit(im, ox + 8, FLOOR - 1, BOOT)
        for sx, sy in ((ox - 2, FLOOR - 1), (ox - 3, FLOOR), (ox + 13, FLOOR - 1), (ox + 14, FLOOR)):
            put(im, sx, sy, C["h"])
    elif pose == "land2":
        blit(im, ox + 2, FLOOR - 1, BOOT)
        blit(im, ox + 7, FLOOR - 1, BOOT)
        put(im, ox - 2, FLOOR, C["s"])
        put(im, ox + 13, FLOOR, C["s"])
    elif boots:
        blit(im, ox + 2, FLOOR - 1, BOOT)
        blit(im, ox + 6, FLOOR - 1, BOOT)
    else:
        # garante baseline na robe
        blit(im, ox + 3, FLOOR - 1, ["kkk"])

    # partículas do cristal
    cx, cy = ox + 12 + staff_dx, oy + 1 + staff_dy
    sparks = [(-3, 0, "p"), (3, 1, "c"), (-2, -2, "c"), (2, -2, "p")]
    if glow:
        sparks += [(-4, 1, "c"), (4, 0, "w"), (0, -3, "c")]
    for dx, dy, key in sparks:
        put(im, cx + dx, cy + dy, C[key])

    outline(im)
    # void do capuz por cima do outline
    put(im, ox + 5, oy + 7, C["n"])
    put(im, ox + 6, oy + 7, C["n"])
    put(im, ox + 5, oy + 8, C["n"])
    put(im, ox + 6, oy + 8, C["n"])
    put(im, cx, cy, C["w"] if glow else C["c"])
    return im


def pack_to_floor(im: Image.Image) -> Image.Image:
    """Garante última linha opaca em y=60 sem esticar."""
    ys = [y for y in range(CELL) if any(im.getpixel((x, y))[3] > 10 for x in range(CELL))]
    if not ys:
        return im
    dy = FLOOR - max(ys)
    if dy == 0:
        return im
    out = new_cell()
    out.paste(im, (0, dy), im)
    return out


def place(sheet: Image.Image, i: int, im: Image.Image, frames: Path) -> None:
    im = pack_to_floor(im)
    sheet.paste(im, ((i % COLS) * CELL, (i // COLS) * CELL), im)
    im.save(frames / f"m_{i:02d}.png")


def main() -> None:
    sheet = Image.new("RGBA", (COLS * CELL, ROWS * CELL), (0, 0, 0, 0))
    frames = ROOT / "frames"
    frames.mkdir(exist_ok=True)

    # 0–3 idle
    idle = [(0, 0, 0), (1, 1, 1), (0, 0, 0), (-1, 1, 1)]
    for i, (cape, bob, glow) in enumerate(idle):
        place(sheet, i, draw(cape, bob, 0, glow, False), frames)

    # 4–9 walk
    walk = [(2, 0, 0), (3, 1, 1), (2, 0, 0), (-2, 1, 1), (-3, 0, 0), (-2, 1, 1)]
    for i, (cape, bob, glow) in enumerate(walk):
        place(sheet, 4 + i, draw(cape, bob, 0, glow, True), frames)

    # 10–12 jump
    jump = [("rise", 2, 1), ("apex", 3, 2), ("fall", 1, 1)]
    for i, (pose, cape, glow) in enumerate(jump):
        place(sheet, 10 + i, draw(cape, -1, 0, glow, False, pose=pose), frames)

    # 13–16 cast — cajado avança
    for i, glow in enumerate([1, 2, 2, 1]):
        place(sheet, 13 + i, draw(-1, 0, 1 + i // 2, glow, False), frames)

    # 17–20 special
    for i, (cape, glow) in enumerate([(2, 2), (3, 2), (3, 2), (2, 1)]):
        place(sheet, 17 + i, draw(cape, 0, 1, glow, False), frames)

    # 21–22 hurt
    for i, cape in enumerate([-2, -1]):
        place(sheet, 21 + i, draw(cape, 0, -1, 0, False, True), frames)

    # 23 silhueta / 24 paleta / 25–27 spare
    sil = draw()
    sil2 = new_cell()
    for y in range(CELL):
        for x in range(CELL):
            if sil.getpixel((x, y))[3] > 10:
                sil2.putpixel((x, y), C["k"])
    place(sheet, 23, sil2, frames)

    pal = new_cell()
    keys = "kndzsmrlhaipcwbt"
    for i, key in enumerate(keys):
        x0, y0 = 4 + (i % 8) * 7, 16 + (i // 8) * 18
        for yy in range(12):
            for xx in range(5):
                put(pal, x0 + xx, y0 + yy, C[key])
    place(sheet, 24, pal, frames)

    # 25-26 aterrissagem (squash + recover, usados pelo MagoVisual), 27 spare
    place(sheet, 25, draw(1, 2, 0, 0, False, pose="land"), frames)
    place(sheet, 26, draw(0, 1, 0, 1, False, pose="land2"), frames)
    place(sheet, 27, draw(-1, 0, 0, 0), frames)

    sheet.save(ROOT / "Mago_sheet.png")
    RES.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(RES)
    draw(0, 0, 0, 1).resize((320, 320), Image.NEAREST).save("/tmp/mago_e11.png")
    print("ok", RES)


if __name__ == "__main__":
    main()
