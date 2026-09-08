import struct
import zlib
from pathlib import Path

ROOT = Path("/Users/gabriellucas/Desktop/trabalho jogo sexta/Assets/Art/Guerreiro")
CELL = 64
COLS = 7
ROWS = 3

# Paleta magma da arte-conceito
C = {
    ".": None,
    "k": (14, 6, 8, 255),
    "d": (42, 6, 10, 255),
    "s": (72, 12, 14, 255),
    "m": (128, 16, 18, 255),
    "r": (188, 20, 20, 255),
    "l": (224, 48, 28, 255),
    "o": (255, 102, 16, 255),
    "y": (255, 220, 68, 255),
    "w": (255, 248, 210, 255),
    "p": (156, 12, 22, 255),
    "n": (24, 10, 14, 255),
}

# ---------- cavaleiro baixinho (chibi): elmo grande com chifres, corpo curto ----------

HELMET = [
    ".kwk...kkkkkkkkkk...kwk.",
    "kwwk..krrrrrrrrrrk..kwwk",
    "kwwwk.krrllrrrrrrk.kwwwk",
    ".kwwwkkrlrrrrrrrrkkwwwk.",
    "..kwwwrrrrrrrrrrrrwwwk..",
    "...kkwrrrrrrrrrrrrwkk...",
    "....krrrkkkkkkkkrrrk....",
    "....krrknyyyyyynkrrk....",
    "....krrkknnnnnnkkrrk....",
    "....krrrrkknnkkrrrrk....",
    "....kmrrrrknnkrrrrmk....",
    "....kmmrrrrkkrrrrmmk....",
    ".....kmmmmmmmmmmmmk.....",
    "......kkkkkkkkkkkk......",
]

BODY = [
    ".kkkkkkkkkkkk.",
    "klrrrloolrrrlk",
    "klrrrlyylrrrlk",
    ".krrrrllrrrrk.",
    ".ksrrorrorrsk.",
    ".ksrrrrrrrrsk.",
    "..kddkyykddk..",
    "..kkkkkkkkkk..",
]

LEG = [
    "kkkk.",
    "krrk.",
    "kddk.",
    "kdook",
    "kkkkk",
]

SHIELD = [
    ".kkkkkkkk.",
    "kwmmmmmmwk",
    "kmlwmmwlmk",
    "kmmlyylmmk",
    "kmmlyylmmk",
    "kmmmllmmmk",
    ".kmmllmmk.",
    ".ksmmmmsk.",
    "..kdmmdk..",
    "...kddk...",
    "....kk....",
]

SWORD_V = [
    "...w...",
    "..kwk..",
    "..kyk..",
    ".klwlk.",
    ".klylk.",
    ".klwlk.",
    ".klylk.",
    ".klwlk.",
    ".klylk.",
    "..kyk..",
    "kklllkk",
    "klyyylk",
    ".kkkkk.",
    "..kok..",
    "..kkk..",
]

SWORD_UP = [
    "..........kww",
    ".........kwyk",
    "........kyyk.",
    ".......kyyk..",
    "......kyok...",
    ".....kyyk....",
    "....kyok.....",
    "...klyk......",
    "..kllk.......",
    ".klylk.......",
    "kklolkk......",
    "klyyylk......",
    ".kkkkk.......",
]

SWORD_DOWN = [
    ".kkkkk.......",
    "klyyylk......",
    "kklolkk......",
    ".klylk.......",
    "..kllk.......",
    "...klyk......",
    "....kyok.....",
    ".....kyyk....",
    "......kyok...",
    ".......kyyk..",
    "........kyyk.",
    ".........kwyk",
    "..........kww",
]

SWORD_BACK = [
    "kww..........",
    "kwyk.........",
    ".kyyk........",
    "..kyok.......",
    "...kyyk......",
    "....kyok.....",
    ".....klyk....",
    "......kllk...",
    "......klylk..",
    ".....kklolkk.",
    ".....klyyylk.",
    "......kkkkk..",
]

SWORD_THRUST = [
    "kkkkkkkkkkkkkkkkkkkkk.",
    "kywwwwwwwwwwwwwwwwyyw.",
    "klooooooooooooooooook.",
    ".kkkkkkkkkkkkkkkkkkk..",
]

SMEAR = [
    ".............ooo.....",
    "...........oyyyyo....",
    ".........oyyyyyyyo...",
    ".......oyyyyoyyyyo...",
    ".....oyyyyo..oyyyo...",
    "....oyyyo.....oyyo...",
    "...oyyo........oyo...",
    "...oyo.........oo....",
    "...oo................",
]


def write_png(path, width, height, pixels):
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        raw.extend(pixels[y * width * 4 : (y + 1) * width * 4])

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
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


def blit(pixels, width, ox, oy, rows, flicker=None):
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            color = C.get(ch)
            if flicker and ch in "oy" and flicker == 1:
                color = C["o"] if ch == "y" else C["y"]
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


def draw_warrior(pixels, width, ox, oy, pose):
    lean = pose.get("lean", 0)
    bx = ox + 16 + pose.get("x", 0)
    by = oy + 35 + pose.get("y", 0)
    flicker = pose.get("flicker", 0)
    sword = pose.get("sword", "front")
    shield = pose.get("shield", "side")
    legs = pose.get("legs", "stand")
    back = pose.get("l", 0)
    front = pose.get("r", 0)

    # pernas curtinhas coladas no corpo (tuck = recolhidas no pulo)
    leg_y = by + 20 if legs != "tuck" else by + 18
    blit(pixels, width, bx + 8 + back, leg_y, LEG, flicker)
    blit(pixels, width, bx + 13 + front, leg_y, LEG, flicker)

    # corpo e elmo grandao
    blit(pixels, width, bx + 5 + lean, by + 13, BODY, flicker)
    blit(pixels, width, bx + lean, by, HELMET, flicker)

    # escudo no lado de tras quando em guarda normal
    if shield == "side":
        blit(pixels, width, bx + lean + 1, by + 13, SHIELD, flicker)
    elif shield == "tuck":
        blit(pixels, width, bx + lean + 3, by + 12, SHIELD, flicker)

    # espada
    if sword == "front":
        blit(pixels, width, bx + lean + 21, by + 2, SWORD_V, flicker)
    elif sword == "up":
        blit(pixels, width, bx + lean + 17, by - 6, SWORD_UP, flicker)
    elif sword == "down":
        blit(pixels, width, bx + lean + 17, by + 12, SWORD_DOWN, flicker)
    elif sword == "back":
        blit(pixels, width, bx - 9, by + 2, SWORD_BACK, flicker)
    elif sword == "smear":
        blit(pixels, width, bx + 19, by + 2, SMEAR, flicker)
    elif sword == "thrust":
        blit(pixels, width, bx + 17, by + 12, SWORD_THRUST, flicker)

    # bloqueio: escudo levantado na frente do corpo
    if shield == "front":
        blit(pixels, width, bx + lean + 16, by + 8, SHIELD, flicker)


def poses():
    return [
        # idle — respira, brilho pisca
        {"sword": "front", "flicker": 0},
        {"sword": "front", "flicker": 1},
        {"y": 1, "sword": "front", "flicker": 0},
        {"y": 1, "sword": "front", "flicker": 1},
        # walk — passinhos curtos, corpo balanca
        {"lean": 1, "l": -2, "r": 2, "sword": "front", "flicker": 0},
        {"lean": 1, "y": -1, "l": -1, "r": 1, "sword": "front", "flicker": 1},
        {"lean": 1, "y": -1, "l": 0, "r": 0, "sword": "front", "flicker": 0},
        {"lean": 1, "l": 2, "r": -2, "sword": "front", "flicker": 1},
        {"lean": 1, "y": -1, "l": 1, "r": -1, "sword": "front", "flicker": 0},
        {"lean": 1, "y": -1, "l": 0, "r": 0, "sword": "front", "flicker": 1},
        # jump — sobe / apice (pernas recolhidas) / cai
        {"y": -2, "l": -3, "r": 3, "sword": "up", "shield": "tuck", "flicker": 0},
        {"y": -4, "l": 0, "r": 1, "legs": "tuck", "sword": "up", "shield": "tuck", "flicker": 1},
        {"y": -1, "l": -4, "r": 4, "sword": "down", "shield": "tuck", "flicker": 0},
        # attack — preparo / rastro / estocada / recupera
        {"lean": -1, "l": -1, "r": 1, "sword": "back", "flicker": 0},
        {"lean": 1, "x": 1, "l": 2, "r": -1, "sword": "smear", "flicker": 1},
        {"lean": 2, "x": 2, "l": 3, "r": -1, "sword": "thrust", "flicker": 1},
        {"lean": 1, "x": 1, "l": 1, "r": 0, "sword": "down", "flicker": 0},
        # block — escudo cobre a frente
        {"y": 1, "sword": "back", "shield": "front", "flicker": 0},
        {"y": 1, "l": 1, "sword": "back", "shield": "front", "flicker": 1},
        # hurt — recua
        {"x": -3, "lean": -1, "l": -2, "r": 1, "sword": "front", "flicker": 0},
        {"x": -2, "lean": -1, "l": -1, "r": 0, "sword": "front", "flicker": 1},
    ]


def crop_cell(sheet, sheet_w, index):
    ox, oy = cell_origin(index)
    cell = bytearray(CELL * CELL * 4)
    for y in range(CELL):
        src = ((oy + y) * sheet_w + ox) * 4
        dest = y * CELL * 4
        cell[dest : dest + CELL * 4] = sheet[src : src + CELL * 4]
    return cell


def build_sheet():
    width, height = COLS * CELL, ROWS * CELL
    pixels = bytearray(width * height * 4)
    for i, pose in enumerate(poses()):
        ox, oy = cell_origin(i)
        draw_warrior(pixels, width, ox, oy, pose)
        outline_cell(pixels, width, ox, oy)
    write_png(ROOT / "Guerreiro_sheet.png", width, height, pixels)
    frames_dir = ROOT / "frames"
    frames_dir.mkdir(exist_ok=True)
    for i in range(21):
        write_png(frames_dir / f"g_{i:02d}.png", CELL, CELL, crop_cell(pixels, width, i))


def build_palette():
    keys = "kdsmrloypnw"
    pixels = bytearray(len(keys) * 24 * 24 * 4)
    for i, key in enumerate(keys):
        color = C[key]
        for y in range(24):
            for x in range(24):
                px = i * 24 + x
                idx = (y * len(keys) * 24 + px) * 4
                pixels[idx : idx + 4] = bytes(color)
    write_png(ROOT / "paleta.png", len(keys) * 24, 24, pixels)


if __name__ == "__main__":
    ROOT.mkdir(parents=True, exist_ok=True)
    build_sheet()
    build_palette()
    print("ok", ROOT / "Guerreiro_sheet.png")
