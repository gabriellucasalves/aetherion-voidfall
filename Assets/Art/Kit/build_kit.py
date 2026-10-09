# Kit de identidade visual (ENV-002). Pixel desenhado, PPU 16, paleta do ArtBible.
# Saída: Assets/Art/Kit/png/*.png e Assets/Art/Kit/folha.png
import math
from pathlib import Path

from PIL import Image

OUT = Path(__file__).resolve().parent / "png"
OUT.mkdir(parents=True, exist_ok=True)

# Rampas de 4 tons. O 8-bit usa uma cor chapada; o 16-bit separa sombra, base, luz e brilho.
INK = (10, 8, 18)
ST0 = (22, 20, 36)
ST1 = (40, 36, 58)
ST2 = (68, 62, 92)
ST3 = (108, 102, 136)
ST4 = (156, 150, 182)
MB0 = (62, 56, 84)
MB1 = (104, 96, 128)
MB2 = (158, 150, 180)
MB3 = (214, 208, 228)
MOSS0 = (22, 36, 28)
MOSS1 = (40, 68, 42)
MOSS2 = (78, 118, 64)
PLANT0 = (24, 52, 40)
PLANT1 = (46, 96, 68)
PLANT2 = (92, 150, 86)
FUNG0 = (72, 36, 96)
FUNG1 = (132, 72, 160)
CRYS0 = (16, 64, 68)
CRYS1 = (48, 150, 140)
CRYS2 = (120, 230, 206)
CRYS3 = (210, 255, 244)
GLYPH0 = (110, 72, 28)
GLYPH1 = (186, 140, 62)
VOID = (6, 5, 12)
GP0 = (32, 30, 48)
GP1 = (58, 54, 78)
GP2 = (96, 92, 122)
RUST0 = (62, 32, 28)
RUST1 = (118, 62, 40)
ST_BASE, ST_LIT, ST_DARK, ST_EDGE = ST2, ST3, ST0, ST4
MARBLE, MARBLE_L, MARBLE_D = MB2, MB3, MB1
MOSS, MOSS_L = MOSS1, MOSS2
PLANT, FUNGUS = PLANT1, FUNG1
CRYS, CRYS_D, CRYS_H = CRYS1, CRYS0, CRYS3
GLYPH = GLYPH1
GP, GP_D, GP_L = GP1, GP0, GP2


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.px = self.img.load()

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h and c is not None:
            self.px[x, self.h - 1 - y] = c if len(c) == 4 else c + (255,)

    def fill(self, x, y, w, h, c):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                self.put(xx, yy, c)

    def save(self, name):
        self.img.save(OUT / f"{name}.png")
        return self.img


def disc(c, cx, cy, r, col, hole=0):
    rr, hh = r * r, hole * hole
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            d = dx * dx + dy * dy
            if hh < d <= rr:
                c.put(cx + dx, cy + dy, col)


def brick(c, x, y, bw, bh, chip=False):
    """Um tijolo com sombra, base, luz e brilho. A junta é o tom mais escuro."""
    for yy in range(bh):
        for xx in range(bw):
            if xx == 0 or yy == 0:
                col = ST0
            elif xx >= bw - 1 or yy >= bh - 1:
                col = ST1
            elif xx <= 2 and yy >= bh - 3:
                col = ST4
            elif xx <= 2 or yy >= bh - 3:
                col = ST3
            else:
                col = ST2
            if chip and xx > bw // 2 and yy < 2:
                col = ST0
            c.put(x + xx, y + yy, col)


def blocks(c, w, h, ox=0, oy=0):
    bw, bh = 12, 8
    row = 0
    y = 0
    while y < h:
        shift = bw // 2 if row % 2 else 0
        x = -shift
        while x < w:
            if x + bw > 0 and x < w:
                brick(c, ox + x, oy + y, bw, bh, chip=(row + x) % 17 == 0)
            x += bw
        y += bh
        row += 1


def wall_plain():
    c = Canvas(48, 64)
    blocks(c, 48, 64)
    return c.save("parede_lisa")


def wall_crack():
    c = Canvas(48, 64)
    blocks(c, 48, 64)
    x, y = 18, 8
    for i in range(28):
        c.put(x, y, INK)
        x += 1 if i % 3 == 0 else 0
        y += 1
        if i % 5 == 0:
            c.put(x + 1, y, ST_DARK)
    return c.save("parede_rachada")


def wall_glyph():
    c = Canvas(48, 64)
    blocks(c, 48, 64)
    disc(c, 24, 36, 8, ST3, 6)
    disc(c, 24, 36, 5, GLYPH0, 3)
    disc(c, 24, 36, 2, GLYPH1, 0)
    c.put(24, 36, CRYS2)
    return c.save("parede_simbolo")


def column(name, broken=False, ring=False):
    c = Canvas(20, 80)
    body_top = 70 if not broken else 48
    for y in range(6, body_top):
        for x, col in ((3, ST0), (4, ST1), (5, ST3), (6, ST4), (7, ST2), (8, ST2), (9, ST1), (10, ST0)):
            c.put(x, y, col)
        if y % 12 == 0:
            c.fill(2, y, 12, 2, ST3)
            c.fill(2, y + 1, 12, 1, ST0)
    c.fill(1, 0, 14, 5, ST1)
    c.fill(2, 4, 12, 1, ST4)
    if broken:
        for i in range(10):
            c.put(3 + i, 40 + (i % 3), ST_EDGE)
        c.fill(2, 36, 4, 3, MOSS)
    else:
        c.fill(2, 58, 12, 4, ST_EDGE)
    if ring:
        disc(c, 8, 52, 5, ST_EDGE, 3)
    return c.save(name)


def arch(name, broken=False):
    c = Canvas(48, 40)
    blocks(c, 48, 40)
    disc(c, 24, 8, 16, (0, 0, 0, 0), 0)
    for dy in range(-16, 17):
        for dx in range(-16, 17):
            d = dx * dx + dy * dy
            if 12 * 12 < d <= 16 * 16 and dy > -2:
                if broken and dx > 6 and dy > 4:
                    continue
                c.put(24 + dx, 8 + dy, ST_EDGE if d > 15 * 15 else ST_LIT)
    return c.save(name)


def door(name, broken=False):
    c = Canvas(24, 40)
    blocks(c, 24, 40)
    c.fill(6, 2, 12, 28, VOID)
    disc(c, 12, 30, 8, VOID, 0)
    disc(c, 12, 30, 8, ST_EDGE, 6)
    if not broken:
        disc(c, 12, 22, 3, GLYPH, 1)
        c.put(12, 22, CRYS_D)
    else:
        c.fill(8, 6, 8, 10, ST_DARK)
        c.put(14, 18, MOSS)
        c.put(15, 19, MOSS_L)
    return c.save(name)


def floor_tile(name, mode):
    c = Canvas(32, 16)
    for y in range(16):
        for x in range(32):
            col = GP_D if x % 16 == 15 or y % 8 == 7 else (GP_L if (x // 16 + y // 8) % 2 == 0 else GP)
            c.put(x, y, col)
    if mode == "crack":
        for i in range(20):
            c.put(6 + i, 4 + i // 3, INK)
    elif mode == "moss":
        for x in range(0, 32, 3):
            c.put(x, 14, MOSS_L if x % 2 == 0 else MOSS)
            c.put(x, 13, MOSS)
    elif mode == "glyph":
        disc(c, 16, 8, 4, GLYPH, 2)
        c.put(16, 8, CRYS_D)
    return c.save(name)


def ruin(name, tall=False):
    c = Canvas(40, 28 if not tall else 36)
    h = c.h
    for x in range(40):
        top = int((h - 6) * (1 - abs(x - 20) / 28)) + (x % 3)
        for y in range(max(0, top)):
            c.put(x, y, ST_DARK if y == top - 1 else ST_BASE)
    c.fill(4, 2, 8, 5, MARBLE_D)
    c.fill(22, 1, 6, 3, MARBLE)
    if tall:
        c.fill(16, 10, 6, 12, ST_LIT)
        c.put(18, 20, MOSS)
    return c.save(name)


def eyes(c, x, y, lit=False):
    c.fill(x, y + 6, 3, 3, VOID)
    c.fill(x + 10, y + 6, 3, 3, VOID)
    c.fill(x + 5, y + 2, 3, 3, VOID)
    if lit:
        c.put(x + 6, y + 3, CRYS_H)


def head(c, x, y, w=28, h=22, crack=False, lit=False):
    for yy in range(h):
        inset = abs(yy - h // 2) // 4
        for xx in range(inset, w - inset):
            col = MARBLE_D if xx < 3 else (MARBLE_L if xx > w - 4 else MARBLE)
            c.put(x + xx, y + yy, col)
    eyes(c, x + 6, y + 4, lit)
    c.fill(x + w // 2 - 1, y + 4, 1, 4, VOID)
    if crack:
        for i in range(10):
            c.put(x + w // 2 + i // 3, y + 8 + i, INK)
            if i % 3 == 0:
                c.put(x + w // 2 + i // 3, y + 9 + i, MOSS)


def ring(c, cx, cy, r, broken=False):
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            d = math.hypot(dx, dy)
            if r - 2 <= d <= r:
                if broken and dx > 2:
                    continue
                c.put(cx + dx, cy + dy, ST_EDGE)


def arm(c, x, y, direction=1, length=16):
    for i in range(length):
        c.put(x + i * direction, y, MARBLE_D)
        c.put(x + i * direction, y + 1, MARBLE)
        c.put(x + i * direction, y + 2, MARBLE_L)
    hx = x + length * direction
    for f, dy in ((0, 3), (2, 1), (4, 0)):
        c.fill(hx + f * direction, y + dy, 2, 4, MARBLE)


def deity_intact():
    c = Canvas(48, 80)
    ring(c, 24, 62, 12, False)
    c.fill(20, 8, 8, 28, MARBLE_D)
    for y in range(36, 58, 4):
        c.fill(16, y, 16, 2, MARBLE)
        c.fill(16, y, 16, 1, MARBLE_L)
    head(c, 10, 54, 28, 20, False, True)
    arm(c, 8, 50, -1, 10)
    arm(c, 36, 50, 1, 10)
    arm(c, 12, 40, -1, 12)
    arm(c, 34, 40, 1, 12)
    c.fill(14, 0, 20, 6, ST_DARK)
    disc(c, 24, 4, 3, GLYPH, 1)
    return c.save("divindade_intacta")


def deity_broken():
    c = Canvas(56, 72)
    ring(c, 22, 54, 12, True)
    c.fill(16, 6, 10, 26, MARBLE_D)
    for y in range(32, 50, 4):
        c.fill(12, y, 16, 2, MARBLE)
    head(c, 36, 4, 18, 16, True, False)
    arm(c, 6, 44, -1, 8)
    arm(c, 10, 34, -1, 14)
    c.fill(28, 28, 12, 3, MARBLE_D)
    c.fill(14, 0, 18, 5, ST_DARK)
    return c.save("divindade_quebrada")


def deity_head():
    c = Canvas(40, 32)
    head(c, 4, 4, 32, 24, True, False)
    return c.save("divindade_cabeca")


def deity_arm():
    c = Canvas(36, 14)
    arm(c, 2, 4, 1, 22)
    return c.save("divindade_braco")


def deity_hand():
    c = Canvas(16, 12)
    c.fill(2, 4, 8, 4, MARBLE)
    c.fill(1, 2, 2, 5, MARBLE_L)
    c.fill(4, 1, 2, 6, MARBLE)
    c.fill(7, 2, 2, 5, MARBLE_D)
    c.fill(10, 3, 2, 4, MARBLE)
    return c.save("divindade_mao")


def pedestal():
    c = Canvas(32, 16)
    c.fill(2, 0, 28, 6, ST_DARK)
    c.fill(6, 6, 20, 6, MARBLE_D)
    disc(c, 16, 9, 3, GLYPH, 1)
    c.fill(4, 12, 6, 3, MARBLE)
    c.fill(22, 12, 6, 3, MARBLE)
    return c.save("divindade_pedestal")


def fragment_ring():
    c = Canvas(16, 12)
    for a in range(8):
        ang = math.radians(200 + a * 12)
        c.put(4 + int(math.cos(ang) * 5), 2 + int(math.sin(ang) * 5), ST_EDGE)
    return c.save("fragmento_anel")


def fragment_eye():
    c = Canvas(10, 8)
    c.fill(1, 1, 8, 6, MARBLE)
    c.fill(3, 2, 4, 4, VOID)
    return c.save("fragmento_olho")


def moss(name, kind):
    c = Canvas(16, 8)
    if kind == 0:
        for x in range(16):
            if x % 2 == 0:
                c.put(x, 1, MOSS)
                c.put(x, 2, MOSS_L)
    elif kind == 1:
        for x in range(0, 16, 3):
            c.fill(x, 0, 2, 3 + (x % 2), MOSS)
            c.put(x, 4, MOSS_L)
    else:
        for x in range(16):
            c.put(x, 0, MOSS_L if x % 3 else MOSS)
            if x % 4 == 0:
                c.put(x, 2, PLANT)
    return c.save(name)


def root(name, kind):
    c = Canvas(24, 16)
    x, y = 2, 2
    for i in range(18):
        c.put(x, y, (46, 32, 24))
        c.put(x, y + 1, MOSS if i % 4 == 0 else (32, 24, 20))
        x += 1
        if kind == 0 and i % 4 == 0:
            y += 1
        elif kind == 1 and i % 3 == 0:
            y -= 1
        elif kind == 2 and i % 5 == 0:
            c.put(x, y - 2, MOSS_L)
    return c.save(name)


def plant(name, kind):
    c = Canvas(12, 16)
    c.fill(5, 0, 2, 8, (40, 28, 22))
    if kind == 0:
        c.fill(2, 8, 8, 3, PLANT)
        c.put(3, 12, CRYS_D)
    elif kind == 1:
        for i in range(4):
            c.put(5 - i, 8 + i, PLANT)
            c.put(6 + i, 8 + i, MOSS_L)
    else:
        c.fill(3, 9, 6, 4, PLANT)
        c.put(5, 14, CRYS)
        c.put(6, 13, PLANT)
    return c.save(name)


def fungus(name, glow):
    c = Canvas(10, 8)
    c.fill(4, 0, 2, 3, (50, 32, 40))
    disc(c, 5, 5, 3, FUNGUS, 0)
    if glow:
        c.put(5, 6, CRYS_H)
    return c.save(name)


def stone(name, w, h):
    c = Canvas(w + 2, h + 2)
    c.fill(1, 1, w, h, ST_BASE)
    c.fill(1, h - 1, w, 1, ST_LIT)
    c.put(1, 1, ST_DARK)
    return c.save(name)


def crystal(name, h, lit):
    c = Canvas(8, h + 2)
    for y in range(h):
        half = 1 + (1 if y > 2 and y < h - 2 else 0)
        col = CRYS_H if lit and y == h - 2 else (CRYS if y > 1 else CRYS_D)
        c.fill(4 - half, y, half * 2, 1, col)
    return c.save(name)


def symbol(name, kind):
    c = Canvas(16, 16)
    if kind == 0:
        disc(c, 8, 8, 6, GLYPH, 4)
        c.put(8, 8, CRYS_D)
    elif kind == 1:
        disc(c, 8, 8, 6, ST_EDGE, 4)
        c.fill(7, 4, 2, 8, GLYPH)
        c.fill(4, 7, 8, 2, GLYPH)
    else:
        for i in range(5):
            c.put(3 + i, 3 + i, GLYPH)
            c.put(11 - i, 3 + i, GLYPH)
        c.put(8, 8, CRYS)
    return c.save(name)


def relic(name, kind):
    c = Canvas(16, 12)
    if kind == 0:
        c.fill(2, 2, 12, 6, ST_DARK)
        disc(c, 8, 5, 2, CRYS_D, 0)
    elif kind == 1:
        c.fill(4, 0, 8, 3, MARBLE_D)
        c.fill(2, 3, 12, 4, ST_BASE)
        c.put(8, 5, GLYPH)
    else:
        c.fill(1, 4, 14, 2, RUST if False else (90, 48, 32))
        c.fill(3, 2, 2, 6, CRYS_D)
        c.fill(11, 2, 2, 6, CRYS_D)
    return c.save(name)


def step_tile():
    c = Canvas(32, 16)
    c.fill(0, 0, 32, 6, ST_DARK)
    c.fill(4, 6, 24, 6, ST_BASE)
    c.fill(4, 11, 24, 1, ST_LIT)
    c.fill(0, 12, 32, 4, GP_D)
    return c.save("degrau")


def edge_tile():
    c = Canvas(16, 16)
    for y in range(10):
        for x in range(16):
            c.put(x, y, GP_D if x == 15 or y == 0 else GP)
    for x in range(16):
        c.put(x, 10, ST_EDGE)
        if x % 2 == 0:
            c.put(x, 12, MOSS)
    return c.save("borda_chao")


def platform_tile():
    c = Canvas(32, 12)
    c.fill(0, 0, 32, 4, ST_DARK)
    c.fill(0, 4, 32, 6, ST_BASE)
    c.fill(0, 10, 32, 2, ST_LIT)
    for x in (4, 18):
        c.put(x, 8, MOSS)
    return c.save("plataforma")


def sheet():
    names = sorted(p.name for p in OUT.glob("*.png"))
    thumbs = [Image.open(OUT / n).convert("RGBA") for n in names]
    cell = 96
    cols = 8
    rows = math.ceil(len(thumbs) / cols)
    img = Image.new("RGBA", (cols * cell, rows * cell), (12, 10, 22, 255))
    for i, im in enumerate(thumbs):
        scale = max(1, min(4, (cell - 16) // max(im.width, im.height)))
        big = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        x = (i % cols) * cell + (cell - big.width) // 2
        y = (i // cols) * cell + (cell - big.height) // 2
        img.alpha_composite(big, (x, y))
    dest = Path(__file__).resolve().parent / "folha.png"
    img.save(dest)
    print(f"{len(names)} peças em {OUT}")
    print(dest)


def main():
    wall_plain()
    wall_crack()
    wall_glyph()
    column("coluna_inteira", False, True)
    column("coluna_lisa", False, False)
    column("coluna_quebrada", True, False)
    arch("arco_inteiro", False)
    arch("arco_quebrado", True)
    door("porta_selo", False)
    door("porta_rompida", True)
    floor_tile("piso_laje", "clean")
    floor_tile("piso_rachado", "crack")
    floor_tile("piso_musgo", "moss")
    floor_tile("piso_simbolo", "glyph")
    ruin("ruina_baixa", False)
    ruin("ruina_alta", True)
    deity_intact()
    deity_broken()
    deity_head()
    deity_arm()
    deity_hand()
    pedestal()
    fragment_ring()
    fragment_eye()
    moss("musgo_faixa", 0)
    moss("musgo_tufo", 1)
    moss("musgo_borda", 2)
    root("raiz_a", 0)
    root("raiz_b", 1)
    root("raiz_c", 2)
    plant("planta_cristal", 0)
    plant("planta_folha", 1)
    plant("planta_haste", 2)
    fungus("fungo", False)
    fungus("fungo_luz", True)
    for i, (w, h) in enumerate(((6, 4), (8, 5), (5, 7), (10, 4), (4, 4)), 1):
        stone(f"pedra_{i}", w, h)
    crystal("cristal_apagado", 8, False)
    crystal("cristal_baixo", 10, True)
    crystal("cristal_alto", 14, True)
    symbol("simbolo_anel", 0)
    symbol("simbolo_cruz", 1)
    symbol("simbolo_olho", 2)
    relic("relicario", 0)
    relic("altar_pequeno", 1)
    relic("mecanismo", 2)
    step_tile()
    edge_tile()
    platform_tile()
    sheet()


if __name__ == "__main__":
    main()
