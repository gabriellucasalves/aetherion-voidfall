# Gera as camadas do cenário T1 de Aetherion (16-bit dark fantasy).
# Saída: Assets/Resources/T1/*.png  (céu, fundo distante, cidade, calçada, props, plataformas)
import math
import uuid
from pathlib import Path

from PIL import Image

OUT = Path("/Users/gabriellucas/Desktop/trabalho jogo sexta/Assets/Resources/T1")
OUT.mkdir(parents=True, exist_ok=True)

# ---------------- paleta 16-bit dark fantasy ----------------
INK = (5, 5, 13)
SKY_TOP = (8, 9, 23)
SKY_MID = (20, 15, 38)
SKY_HOR = (36, 23, 51)
BLOOD = (77, 18, 26)
MOON = (224, 230, 247)
MOON_SH = (158, 171, 209)
CLOUD = (14, 13, 29)
FAR_A = (19, 19, 36)
FAR_B = (26, 24, 43)
ST_BASE = (36, 33, 54)
ST_LIT = (56, 54, 82)
ST_DARK = (22, 20, 36)
ST_EDGE = (77, 77, 112)
WIN_LIT = (255, 158, 56)
WIN_DARK = (13, 13, 26)
FIRE1 = (255, 115, 26)
FIRE2 = (255, 209, 89)
MOSS = (26, 43, 31)
MOSS_LIT = (41, 66, 41)
BAN_R = (107, 23, 31)
BAN_D = (71, 15, 23)
TRUNK = (23, 18, 26)
TRUNK_L = (36, 28, 38)

# paleta do PLANO JOGÁVEL — mais clara e quente que o fundo, pra destacar
GP_BASE = (69, 64, 87)
GP_LIT = (104, 99, 128)
GP_DARK = (40, 37, 56)
GP_EDGE = (150, 148, 181)
GP_MOSS = (48, 87, 51)
GP_MOSS_L = (79, 128, 66)
GOLD = (199, 154, 66)
GOLD_D = (130, 96, 38)
ORB = (156, 94, 224)
ORB_D = (97, 54, 148)


def hash2(x, y):
    h = (x * 374761393 + y * 668265263) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return (h ^ (h >> 16)) & 0x7FFFFFFF


def chance(x, y, one_in):
    return hash2(x, y) % one_in == 0


class Canvas:
    """y cresce pra CIMA (0 = base), como no Unity."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.px = self.img.load()

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[x, self.h - 1 - y] = c if len(c) == 4 else c + (255,)

    def fill(self, x, y, w, h, c):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                self.put(xx, yy, c)

    def outline(self, color=INK):
        """Contorno 1px em volta da silhueta (leitura 16-bit no plano jogável)."""
        src = self.img.load()
        mask = [[src[x, y][3] > 0 for x in range(self.w)] for y in range(self.h)]
        for y in range(self.h):
            for x in range(self.w):
                if mask[y][x]:
                    continue
                near = (
                    (x > 0 and mask[y][x - 1])
                    or (x < self.w - 1 and mask[y][x + 1])
                    or (y > 0 and mask[y - 1][x])
                    or (y < self.h - 1 and mask[y + 1][x])
                )
                if near:
                    src[x, y] = color + (255,)

    def save(self, name):
        self.img.save(OUT / f"{name}.png")


def stone_shade(x, y):
    r = hash2(x // 3, y // 3) % 10
    if r == 0:
        return ST_DARK
    if r == 1:
        return ST_LIT
    if r == 2 and y < 16:
        return MOSS
    return ST_BASE


def gp_shade(x, y):
    """Pedra do plano jogável: mais clara, com musgo mais vivo."""
    r = hash2(x // 3, y // 3) % 10
    if r == 0:
        return GP_DARK
    if r == 1:
        return GP_LIT
    if r == 2 and y < 16:
        return GP_MOSS
    return GP_BASE


# ============================ CÉU ============================
def build_sky():
    c = Canvas(480, 192)
    for y in range(c.h):
        g = 1 - y / (c.h - 1)  # 1 no horizonte
        for x in range(c.w):
            band = g * 5
            i0 = int(band)
            f = band - i0
            step = i0 + (1 if (f > 0.5 and (x + y) % 2 == 0) else 0)
            q = min(1.0, step / 5)
            if q < 0.5:
                t = q * 2
                col = tuple(int(SKY_TOP[i] + (SKY_MID[i] - SKY_TOP[i]) * t) for i in range(3))
            else:
                t = (q - 0.5) * 2
                col = tuple(int(SKY_MID[i] + (SKY_HOR[i] - SKY_MID[i]) * t) for i in range(3))
            # brasas do apocalipse no horizonte (dois focos)
            if y < 30:
                e = (30 - y) / 30
                lobe = max(0.0, math.sin((x - 60) * 0.011)) + max(0.0, math.sin((x - 300) * 0.014))
                if (x + y * 3) % 4 != 0:
                    m = min(1.0, e * 0.6 * lobe)
                    col = tuple(int(col[i] + (BLOOD[i] - col[i]) * m) for i in range(3))
            c.put(x, y, col)
    # estrelas discretas
    for y in range(70, c.h):
        for x in range(c.w):
            if chance(x, y, 2100):
                c.put(x, y, (191, 199, 230))
    # lua
    cx, cy, r = 350, 142, 14
    for dy in range(-r - 3, r + 4):
        for dx in range(-r - 3, r + 4):
            d = math.hypot(dx, dy)
            if d <= r:
                col = MOON
                if dx - dy > r * 0.62:
                    col = MOON_SH
                if chance(cx + dx, cy + dy, 19):
                    col = MOON_SH
                c.put(cx + dx, cy + dy, col)
            elif d <= r + 2 and (dx + dy) % 2 == 0:
                c.put(cx + dx, cy + dy, MOON_SH + (79,))
    # nuvens compridas
    for (nx, ny, ln) in ((60, 150, 96), (250, 166, 72), (392, 118, 84), (140, 102, 64)):
        for x in range(ln):
            half = int(3 + 2.5 * math.sin(x / ln * math.pi) + hash2(nx + x, ny) % 2)
            for y in range(-half, half // 2 + 1):
                col = CLOUD if y < half // 2 else SKY_MID
                c.put(nx + x, ny + y, col)
    # nebulosa do Vazio: poeira roxa em faixa diagonal
    for x in range(c.w):
        base = 118 + int(26 * math.sin(x * 0.021)) - x // 16
        for k in range(3):
            y = base + hash2(x, 900 + k) % 24
            if 62 < y < c.h - 4 and chance(x, y, 3):
                c.put(x, y, (60, 30, 90) if k % 2 == 0 else (88, 48, 128))
    # estrelas brilhantes com cruz
    for (sx, sy) in ((40, 168), (118, 150), (206, 176), (298, 162), (452, 170), (388, 96), (26, 120)):
        c.put(sx, sy, MOON)
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            c.put(sx + dx, sy + dy, MOON_SH)
    # bando de corvos do Vazio em silhueta
    for (bx, by) in ((288, 152), (295, 148), (303, 151), (312, 146), (321, 150)):
        c.put(bx, by, INK)
        c.put(bx - 1, by + 1, INK)
        c.put(bx + 1, by + 1, INK)
    c.save("sky")


# ====================== FUNDO DISTANTE ======================
def smoke(c, x0, base_y, height=58):
    for y in range(height):
        sx = x0 + int(3.5 * math.sin(y * 0.25)) + hash2(x0, y) % 2
        a = int(255 * (0.5 - 0.45 * y / height))
        c.put(sx, base_y + y, CLOUD + (a,))
        if y % 2 == 0:
            c.put(sx + 1, base_y + y, CLOUD + (int(a * 0.7),))


def far_tower(c, x, base_y, tw, th):
    c.fill(x, base_y, tw, th, FAR_B)
    for i in range(0, tw, 3):
        if not chance(x + i, base_y, 3):
            c.fill(x + i, base_y + th, 2, 3, FAR_B)


def build_far():
    c = Canvas(512, 130)
    for x in range(c.w):
        hh = 46 + int(26 * (0.5 + 0.5 * math.sin(x * 0.017 + 1.4)) ) + hash2(x, 1) % 4
        for y in range(hh):
            c.put(x, y, FAR_A)
    for x in range(c.w):
        hh = 26 + int(20 * (0.5 + 0.5 * math.sin(x * 0.031 + 4.2))) + hash2(x, 2) % 3
        for y in range(hh):
            c.put(x, y, FAR_B)
    # castelo distante em silhueta
    far_tower(c, 138, 40, 8, 56)
    far_tower(c, 150, 40, 14, 88)
    far_tower(c, 170, 40, 10, 70)
    far_tower(c, 378, 34, 12, 66)
    # janelas acesas minúsculas
    for (wx, wy) in ((156, 96), (159, 84), (175, 88), (383, 78)):
        c.put(wx, wy, WIN_LIT)
    # arvores mortas na crista da colina
    for x in range(6, c.w - 6):
        if chance(x, 700, 15):
            hh = 46 + int(26 * (0.5 + 0.5 * math.sin(x * 0.017 + 1.4))) + hash2(x, 1) % 4
            th = 3 + hash2(x, 701) % 5
            for i in range(th):
                c.put(x, hh + i, FAR_B)
            c.put(x - 1, hh + th - 2, FAR_B)
            c.put(x + 1, hh + th - 1, FAR_B)
    # aqueduto arruinado na encosta direita
    for i, ax in enumerate(range(300, 372, 12)):
        if i == 3:
            continue  # vao desabado
        ah = 26 + hash2(ax, 5) % 5
        c.fill(ax, 14, 3, ah, FAR_B)
        c.fill(ax - 3, 14 + ah - 3, 9, 2, FAR_B)
    # fenda do Vazio brilhando na montanha esquerda
    fx = 66
    for fy in range(12, 42):
        fx += (hash2(fy, 11) % 3) - 1
        c.put(fx, fy, ORB if fy % 3 else (206, 150, 255))
        if fy % 5 == 0:
            c.put(fx + 1, fy, ORB_D)
    # fogueiras e janelas extras
    for (wx, wy) in ((92, 70), (238, 74), (330, 58), (472, 66)):
        c.put(wx, wy, WIN_LIT)
    smoke(c, 200, 62)
    smoke(c, 420, 54)
    smoke(c, 96, 70, 40)
    c.save("far")


# ========================== CIDADE ==========================
def arch_hole(c, x0, y0, ww, hh, fill=None):
    half = ww // 2
    cap = hh - half
    for y in range(hh):
        inset = 0
        if y > cap:
            t = (y - cap) / half
            inset = int(half * (1 - math.sqrt(max(0.0, 1 - t * t))))
        for x in range(inset, ww - inset):
            if fill and (x + y) % 2 == 0:
                c.put(x0 + x, y0 + y, fill)
            else:
                c.put(x0 + x, y0 + y, WIN_DARK)


def window(c, x0, y0, lit):
    for y in range(6):
        inset = 1 if y == 5 else 0
        for x in range(inset, 4 - inset):
            col = (WIN_LIT if (x + y) % 2 == 0 else FIRE1) if lit else WIN_DARK
            c.put(x0 + x, y0 + y, col)


def wall_arches(c, x0, y0, ww, hh):
    for x in range(ww):
        top = hh - hash2(x0 + x, 55) % 7
        for y in range(top):
            c.put(x0 + x, y0 + y, stone_shade(x0 + x, y0 + y))
        if top > hh - 4:
            c.put(x0 + x, y0 + top, ST_EDGE)
    ax = x0 + 6
    while ax + 14 < x0 + ww:
        arch_hole(c, ax, y0, 12, 26)
        ax += 24


def house(c, x0, y0, ww, hh, ruined):
    for x in range(ww):
        top = hh
        if ruined and x > ww // 2:
            top = hh - (x - ww // 2) * 2 // 3 - hash2(x0 + x, 8) % 3
        for y in range(top):
            c.put(x0 + x, y0 + y, stone_shade(x0 + x, y0 + y + 31))
        if top < hh:
            c.put(x0 + x, y0 + top, ST_EDGE)
    if not ruined:
        roof = (31, 23, 33)
        for y in range(ww // 2):
            for x in range(y, ww - y):
                col = ST_DARK if (x == y or (x + y) % 5 == 0) else roof
                c.put(x0 + x, y0 + hh + y, col)
        c.put(x0 + ww // 2, y0 + hh + ww // 2, ST_EDGE)
    arch_hole(c, x0 + ww // 2 - 2, y0, 5, 9)
    window(c, x0 + 4, y0 + hh - 12, chance(x0, y0, 2))
    if ww > 24:
        window(c, x0 + ww - 8, y0 + hh - 12, False)


def tower(c, x0, y0, ww, hh, lit_window_y=None):
    for x in range(ww):
        top = hh - ((x - (ww - 6)) * 4 if x > ww - 6 else 0) - hash2(x0 + x, 13) % 3
        for y in range(top):
            col = stone_shade(x0 + x, y0 + y + 17)
            if x == 0:
                col = ST_DARK
            elif x == 1:
                col = ST_LIT
            c.put(x0 + x, y0 + y, col)
        if top > 4:
            c.put(x0 + x, y0 + top, ST_EDGE)
    for i in range(0, ww - 6, 4):
        if not chance(x0 + i, 3, 4):
            c.fill(x0 + i, y0 + hh, 2, 4, ST_BASE)
    window(c, x0 + ww // 2 - 2, y0 + hh - 22, lit_window_y is not None)
    window(c, x0 + ww // 2 - 2, y0 + hh - 44, False)
    cx = x0 + 4
    for y in range(6, hh - 10, 2):
        cx += hash2(x0, y) % 3 - 1
        c.put(cx, y0 + y, ST_DARK)
        c.put(cx, y0 + y + 1, ST_DARK)


def banner(c, x0, y0):
    c.fill(x0, y0 + 22, 10, 2, ST_DARK)
    for y in range(22):
        width = 8 - (y - 14 if y > 14 else 0)
        if y > 16 and y % 2 == 0:
            width -= 1
        for x in range(max(0, width)):
            col = BAN_D if ((x + y) % 6 == 0 or y < 3) else BAN_R
            c.put(x0 + 1 + x, y0 + 22 - y, col)


def fire(c, x0, y0):
    widths = (8, 7, 6, 5, 4, 3, 2, 1)
    for y, ww in enumerate(widths):
        start = x0 + (8 - ww) // 2 + hash2(x0, y) % 2
        for x in range(ww):
            c.put(start + x, y0 + y, FIRE1 if (y < 3 or x % 2) else FIRE2)
    c.fill(x0 + 1, y0 - 2, 6, 2, ST_DARK)


def rose(c, cx, cy, r):
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            d = math.hypot(dx, dy)
            if d > r:
                continue
            col = WIN_DARK
            if d > r - 1.6:
                col = ST_EDGE
            elif abs(dx) < 1 or abs(dy) < 1 or abs(dx - dy) < 1 or abs(dx + dy) < 1:
                col = ST_BASE
            elif (dx * dx + dy * dy) % 7 < 2:
                col = BLOOD
            c.put(cx + dx, cy + dy, col)


def spire(c, x0, y0, ww, hh, broken):
    def top_at(x):
        return hh - 8 - abs((x * 7) % 11 - 5) if broken else hh

    for x in range(ww):
        t = top_at(x)
        for y in range(t):
            col = stone_shade(x0 + x, y0 + y + 71)
            if x == 0:
                col = ST_DARK
            elif x == 1:
                col = ST_LIT
            c.put(x0 + x, y0 + y, col)
        c.put(x0 + x, y0 + t, ST_EDGE)
    if not broken:
        for y in range(ww):
            for x in range(y // 2, ww - y // 2):
                c.put(x0 + x, y0 + hh + y, ST_LIT if x == y // 2 else ST_DARK)
    window(c, x0 + ww // 2 - 2, y0 + hh - 30, not broken)


def cathedral(c, x0, y0):
    bw, bh = 100, 92
    for x in range(bw):
        gable = max(0, 26 - abs(x - bw // 2))
        top = bh - hash2(x0 + x, 21) % 3 + gable
        for y in range(top):
            c.put(x0 + x, y0 + y, stone_shade(x0 + x, y0 + y + 53))
        c.put(x0 + x, y0 + top, ST_EDGE)
    rose(c, x0 + bw // 2, y0 + 78, 12)
    for i in range(4):
        arch_hole(c, x0 + 12 + i * 22, y0 + 18, 8, 26, fill=FIRE1 if i % 2 == 0 else None)
    spire(c, x0 - 4, y0, 16, 118, False)
    spire(c, x0 + bw - 12, y0, 16, 96, True)
    fire(c, x0 + 78, y0 + 12)


def wall_breach(c, x0, y0, ww):
    for x in range(ww):
        t = x / ww
        hh = int(34 * (1 - math.sin(t * math.pi))) + 8 + hash2(x0 + x, 31) % 4
        for y in range(hh):
            c.put(x0 + x, y0 + y, stone_shade(x0 + x, y0 + y + 97))
        c.put(x0 + x, y0 + hh, ST_EDGE)
        if chance(x0 + x, 6, 5):
            c.put(x0 + x, y0 + hh + 1, MOSS_LIT)


def bridge_frag(c, x0, y0, ww):
    c.fill(x0 + 4, y0, 8, 30, ST_BASE)
    c.fill(x0 + ww - 14, y0, 8, 30, ST_BASE)
    arch_hole(c, x0 + 16, y0, 14, 24)
    for x in range(ww):
        gap = ww // 2 - 6 < x < ww // 2 + 9
        if gap:
            continue
        for y in range(28, 34 + hash2(x0 + x, 41) % 2):
            c.put(x0 + x, y0 + y, stone_shade(x0 + x, y0 + y + 11))
        c.put(x0 + x, y0 + 34, ST_EDGE)


def vine(c, x0, y0, ln):
    for y in range(ln):
        sx = x0 + hash2(x0, y) % 2
        c.put(sx, y0 - y, MOSS if y % 2 == 0 else MOSS_LIT)
        if y % 4 == 0:
            c.put(sx + 1, y0 - y, MOSS_LIT)


# Mármore das divindades — mais claro que a muralha, ainda na mesma rampa curta.
MARBLE = (148, 140, 170)
MARBLE_L = (196, 190, 214)
MARBLE_D = (86, 80, 108)
MARBLE_E = (220, 214, 232)


def marble_at(x, y):
    r = hash2(x // 2, y // 2) % 8
    if r == 0:
        return MARBLE_D
    if r == 1:
        return MARBLE_L
    return MARBLE


def moss_clump(c, x0, y0, w, h):
    for y in range(h):
        for x in range(w):
            if hash2(x0 + x, y0 + y) % 5 == 0:
                continue
            c.put(x0 + x, y0 + y, MOSS_LIT if (x + y) % 3 == 0 else MOSS)


def deity_head(c, x0, y0, w=18, h=14):
    """Cabeça de estátua caída: órbitas vazias, rachadura, musgo na têmpora."""
    for y in range(h):
        inset = 0
        if y < 2:
            inset = 2 - y
        if y > h - 4:
            inset = (y - (h - 4)) * 2
        for x in range(inset, w - inset):
            col = marble_at(x0 + x, y0 + y)
            if x <= 2:
                col = MARBLE_D
            elif x >= w - 3:
                col = MARBLE_L
            c.put(x0 + x, y0 + y, col)
        c.put(x0 + inset, y0 + y, MARBLE_E)
    eye_y = y0 + h // 2
    c.fill(x0 + 4, eye_y, 3, 3, WIN_DARK)
    c.fill(x0 + w - 8, eye_y, 3, 3, WIN_DARK)
    c.put(x0 + 5, eye_y + 1, MARBLE_D)
    for i in range(h - 2):
        c.put(x0 + w // 2 - 1 + i // 3, y0 + h - 2 - i, INK)
    c.fill(x0 + w // 2 - 1, y0 + 2, 2, 4, MARBLE_D)
    moss_clump(c, x0 + 1, y0 + h - 3, 6, 3)
    # fragmento da coroa quebrada ao lado
    c.fill(x0 + w + 2, y0 + 1, 4, 3, MARBLE_D)
    c.put(x0 + w + 3, y0 + 4, MARBLE)


def fallen_torso(c, x0, y0):
    """Corpo de divindade derrubado, manto rachado, braço separado."""
    w, h = 52, 16
    for y in range(h):
        for x in range(w):
            col = marble_at(x0 + x, y0 + y)
            if y > h - 3:
                col = MARBLE_L
            if y < 2:
                col = MARBLE_D
            c.put(x0 + x, y0 + y, col)
        c.put(x0, y0 + y, MARBLE_E)
    for x in range(3, w - 2, 6):
        c.put(x0 + x, y0 + 4, MARBLE_D)
        c.put(x0 + x + 1, y0 + 5, INK)
    # ombro partido
    c.fill(x0 + w - 2, y0 + 3, 7, 5, MARBLE)
    c.fill(x0 + w + 4, y0 + 1, 5, 3, MARBLE_D)
    # braço solto na frente
    c.fill(x0 - 8, y0, 7, 3, MARBLE)
    c.put(x0 - 8, y0 + 1, MARBLE_L)
    c.fill(x0 - 4, y0 + 3, 2, 2, MARBLE_D)
    moss_clump(c, x0 + 4, y0, 8, 2)
    moss_clump(c, x0 + 18, y0 + h - 2, 5, 2)


def broken_pedestal(c, x0, y0):
    """Pedestal com só os pés: a estátua foi levada pelo desabamento."""
    c.fill(x0, y0, 16, 4, MARBLE_D)
    c.fill(x0 + 2, y0 + 4, 12, 3, MARBLE)
    c.fill(x0 + 3, y0 + 7, 4, 3, MARBLE_L)
    c.fill(x0 + 9, y0 + 7, 4, 3, MARBLE_L)
    c.put(x0 + 5, y0 + 9, MARBLE_E)
    c.put(x0 + 11, y0 + 9, MARBLE_E)
    for i in range(6):
        c.put(x0 + 7, y0 + 4 + i, INK)
    moss_clump(c, x0 + 1, y0 + 5, 4, 2)
    # lasca de rosto no chão
    c.fill(x0 + 18, y0, 6, 5, MARBLE)
    c.fill(x0 + 20, y0 + 2, 2, 2, WIN_DARK)
    c.put(x0 + 23, y0 + 4, MOSS)


def relief_figure(c, x0, y0):
    """Baixo-relevo de divindade na muralha: poucos pixels, leitura de longe."""
    c.fill(x0 + 2, y0, 3, 2, MARBLE_D)
    c.fill(x0 + 1, y0 + 2, 5, 6, MARBLE)
    c.fill(x0, y0 + 4, 2, 3, MARBLE_L)
    c.fill(x0 + 5, y0 + 4, 2, 3, MARBLE_L)
    c.put(x0 + 3, y0 + 8, MARBLE_D)
    c.put(x0 + 4, y0 + 9, MARBLE_D)
    c.put(x0 + 2, y0 + 3, WIN_DARK)


def rubble_blocks(c, x0, y0, n):
    x = x0
    for i in range(n):
        bw = 3 + hash2(x0 + i, 2) % 4
        bh = 2 + hash2(x0 + i, 3) % 3
        by = y0 + hash2(x0 + i, 4) % 3
        for yy in range(bh):
            for xx in range(bw):
                c.put(x + xx, by + yy, marble_at(x + xx, by + yy) if i % 2 == 0 else stone_shade(x + xx, by + yy))
        if hash2(x, 8) % 2 == 0:
            c.put(x, by + bh - 1, MOSS)
            c.put(x + 1, by + bh - 1, MOSS_LIT)
        x += bw + 1


def settle_moss(c):
    """Musgo em manchas nas quinas, não um verde contínuo na silhueta."""
    src = c.img.load()
    for iy in range(1, c.h - 1):
        for ix in range(c.w):
            if src[ix, iy][3] == 0 or src[ix, iy - 1][3] != 0:
                continue
            r, g, b, _ = src[ix, iy]
            if r > 170 or g > 140:
                continue
            if hash2(ix // 5, iy // 3) % 4 != 0:
                continue
            src[ix, iy] = (MOSS_LIT if hash2(ix, iy) % 2 == 0 else MOSS) + (255,)
            if hash2(ix, iy) % 3 == 0 and src[ix, iy + 1][3] > 0 and src[ix, iy + 1][0] < 170:
                src[ix, iy + 1] = MOSS + (255,)


def crack_wall(c, x0, y0, ln):
    x, y = x0, y0
    for i in range(ln):
        c.put(x, y, INK)
        x += 1 if hash2(x0, i) % 3 else 0
        y += 1 if i % 2 == 0 else 0


def build_city():
    c = Canvas(640, 176)
    for x in range(c.w):
        hh = 10 + hash2(x // 7, 77) % 5
        for y in range(hh):
            c.put(x, y, stone_shade(x, y))
    wall_arches(c, 0, 0, 96, 44)
    house(c, 100, 10, 30, 26, True)
    house(c, 134, 10, 26, 30, False)
    tower(c, 168, 10, 22, 74, lit_window_y=1)
    banner(c, 176, 60)
    cathedral(c, 218, 10)
    wall_breach(c, 330, 0, 70)
    house(c, 404, 10, 28, 24, False)
    house(c, 436, 10, 24, 28, True)
    fire(c, 442, 34)
    smoke(c, 448, 62, 46)
    house(c, 464, 10, 26, 22, False)
    bridge_frag(c, 496, 8, 70)
    tower(c, 574, 10, 24, 88, lit_window_y=1)
    banner(c, 584, 72)
    wall_arches(c, 600, 0, 40, 40)
    # relevos das divindades na face da catedral e da torre
    for rx in (232, 252, 292, 308):
        relief_figure(c, rx, 28)
    relief_figure(c, 182, 36)
    relief_figure(c, 588, 40)
    crack_wall(c, 246, 40, 18)
    crack_wall(c, 360, 16, 14)
    crack_wall(c, 120, 18, 10)
    # escombros: corpo caído, cabeça partida, pedestal só com os pés
    fallen_torso(c, 6, 2)
    deity_head(c, 62, 8, 22, 18)
    broken_pedestal(c, 188, 4)
    rubble_blocks(c, 328, 2, 5)
    deity_head(c, 336, 6, 36, 30)
    rubble_blocks(c, 376, 2, 4)
    deity_head(c, 492, 6, 24, 20)
    rubble_blocks(c, 522, 2, 5)
    for i in range(0, c.w, 4):
        if chance(i, 5, 6):
            vine(c, i, 12 + hash2(i, 9) % 30, 4 + hash2(i, 4) % 12)
    settle_moss(c)
    c.save("city")


# ==================== CALÇADA (plano jogável) ====================
def build_pavement():
    W, H = 512, 44
    c = Canvas(W, H)
    deep = (10, 13, 20)
    for y in range(H):
        base_t = y / 40
        for x in range(W):
            xm = x % W  # padrão fecha nas bordas -> cópias emendam sem costura
            col = tuple(int(deep[i] + (ST_BASE[i] - deep[i]) * min(1.0, base_t)) for i in range(3))
            if y > 30:
                # lajes grandes claras com juntas (plano jogável destacado)
                base_gp = tuple(int(deep[i] + (GP_BASE[i] - deep[i]) * min(1.0, base_t)) for i in range(3))
                col = base_gp
                sx = (xm + (y // 7) * 8) % 16
                if sx == 15 or y % 7 == 6:
                    col = GP_DARK
                elif hash2(xm // 16, y // 7) % 5 == 0:
                    col = GP_LIT
                if y >= 42 and chance(xm, 3, 6):
                    col = GP_MOSS_L  # grama viva na borda
                if chance(xm, y, 37):
                    col = GP_DARK  # rachaduras
            else:
                if chance(xm, y, 61):
                    col = ST_DARK
            c.put(x, y, col)
    # pedrinhas e tufos em cima
    for x in range(W):
        if chance(x, 91, 14):
            c.put(x, 43, GP_MOSS_L)
            c.put(x, 42, GP_MOSS)
        if chance(x, 92, 23):
            c.fill(x, 41, 2, 2, GP_LIT)
    # símbolos, raiz e cristal — detalhes que o jogador encontra andando
    for gx in (72, 248, 400):
        disc(c, gx, 34, 5, GLYPH, hole=3)
        c.put(gx, 34, CRYS)
    for x in range(6, W - 6):
        if hash2(x, 17) % 53 == 0:
            for i in range(7):
                c.put((x + i) % W, 33 + (i % 2), (46, 32, 24))
                if i % 3 == 0:
                    c.put((x + i) % W, 34, MOSS)
    for cx in (150, 320, 470):
        c.fill(cx, 36, 2, 5, CRYS_D)
        c.put(cx, 41, CRYS_H)
        c.put(cx - 1, 37, GP_MOSS)
    c.save("pavement")


# ==================== PROPS (camada próxima) ====================
def build_tree():
    c = Canvas(72, 110)
    for y in range(72):
        half = max(1, 5 - y // 16)
        cx = 36 + int(2 * math.sin(y * 0.08))
        for x in range(-half, half + 1):
            c.put(cx + x, y, TRUNK_L if x == -half else TRUNK)
    def branch(x0, y0, direction, ln):
        x, y = x0, y0
        for i in range(ln):
            x += direction
            if i % 2 == 0:
                y += 1
            c.put(x, y, TRUNK)
            c.put(x, y + 1, TRUNK if i < ln // 2 else TRUNK_L)
    branch(36, 58, -1, 24)
    branch(36, 66, 1, 20)
    branch(36, 72, -1, 14)
    branch(37, 70, 1, 26)
    c.fill(28, 0, 4, 2, TRUNK)
    c.fill(41, 0, 4, 2, TRUNK)
    c.outline()
    c.save("tree")


def build_statue():
    c = Canvas(40, 78)
    c.fill(4, 0, 32, 6, ST_DARK)
    c.fill(8, 6, 24, 5, ST_BASE)
    for x in range(8, 32):
        c.put(x, 10, ST_LIT)
    for y in range(11, 54):
        half = 7 - y // 12
        for x in range(-half, half + 1):
            col = ST_BASE
            if x == -half:
                col = ST_LIT
            elif x == half:
                col = ST_DARK
            if chance(x + 20, y, 17):
                col = MOSS
            c.put(20 + x, y, col)
    # capuz
    for dy in range(-5, 6):
        for dx in range(-5, 6):
            if dx * dx + dy * dy <= 25:
                col = ST_BASE if dy > -2 else ST_DARK
                if dx <= -3:
                    col = ST_LIT
                if -2 <= dx <= 2 and -3 <= dy <= 0:
                    col = WIN_DARK  # rosto na sombra
                c.put(20 + dx, 58 + dy, col)
    # braço quebrado (toco à direita)
    c.fill(27, 40, 5, 4, ST_BASE)
    c.put(31, 42, ST_DARK)
    c.outline()
    c.save("statue")


def build_lamp():
    c = Canvas(22, 64)
    c.fill(6, 0, 10, 3, ST_DARK)
    c.fill(10, 3, 3, 44, TRUNK)
    for y in range(3, 47, 6):
        c.put(10, y, TRUNK_L)
    # lanterna
    c.fill(6, 46, 10, 12, ST_DARK)
    c.fill(8, 48, 6, 8, FIRE1)
    c.fill(9, 50, 4, 4, FIRE2)
    c.fill(8, 58, 6, 2, ST_DARK)
    c.outline()
    c.save("lamp")


def build_rubble():
    c = Canvas(56, 22)
    for x in range(56):
        hh = int(9 * math.sin(x / 56 * math.pi)) + hash2(x, 71) % 4
        for y in range(hh):
            c.put(x, y, stone_shade(x, y + 201))
        if hh > 2:
            c.put(x, hh, ST_LIT if x % 3 else ST_EDGE)
        if chance(x, 72, 7):
            c.put(x, hh + 1, MOSS_LIT)
    # bloco de coluna caído
    c.fill(34, 0, 12, 7, ST_BASE)
    for x in range(34, 46):
        c.put(x, 7, ST_LIT)
    c.outline()
    c.save("rubble")


def build_column():
    c = Canvas(24, 60)
    for y in range(60):
        top_jag = hash2(3, y) % 4
        if y > 52 - top_jag:
            continue
        for x in range(4, 20):
            col = ST_LIT if x % 4 == 0 else (ST_DARK if x % 4 == 2 else ST_BASE)
            if chance(x, y, 31):
                col = MOSS
            c.put(x, y, col)
    c.fill(2, 0, 20, 4, ST_BASE)
    for x in range(2, 22):
        c.put(x, 4, ST_LIT)
    c.outline()
    c.save("column")


# ============ DECORAÇÃO DO PLANO JOGÁVEL (estilo SNES) ============
def build_statue_warrior():
    """Estátua de guerreiro em pedra clara, espada com guarda dourada."""
    c = Canvas(48, 96)
    # pedestal em degraus
    c.fill(4, 0, 40, 7, GP_DARK)
    for x in range(4, 44):
        c.put(x, 7, GP_LIT)
    c.fill(9, 8, 30, 6, GP_BASE)
    for x in range(9, 39):
        c.put(x, 14, GP_LIT)
    c.fill(13, 15, 22, 4, GP_DARK)
    # pernas
    for y in range(19, 40):
        for x in range(20, 25):
            c.put(x, y, GP_LIT if x == 20 else GP_BASE)
        for x in range(27, 32):
            c.put(x, y, GP_DARK if x == 31 else GP_BASE)
    # torso com saiote
    for y in range(40, 62):
        half = 9 if y < 46 else 8
        for x in range(26 - half, 26 + half):
            col = GP_BASE
            if x == 26 - half:
                col = GP_LIT
            elif x >= 26 + half - 2:
                col = GP_DARK
            if chance(x, y, 19):
                col = GP_DARK
            c.put(x, y, col)
    # ombreira esquerda + toco do braço direito quebrado
    c.fill(15, 56, 7, 6, GP_LIT)
    c.fill(33, 56, 5, 4, GP_DARK)
    c.put(37, 58, GP_EDGE)
    # braços à frente segurando o punho
    c.fill(18, 46, 5, 10, GP_BASE)
    # cabeça com elmo e visor na sombra
    for y in range(62, 76):
        half = 5 if y < 72 else 4
        for x in range(26 - half, 26 + half):
            col = GP_LIT if x == 26 - half else GP_BASE
            if 24 <= x <= 28 and 64 <= y <= 67:
                col = WIN_DARK
            c.put(x, y, col)
    c.fill(24, 76, 4, 3, GP_DARK)  # crista quebrada
    # espada fincada — lâmina clara, guarda e pomo DOURADOS
    for y in range(16, 52):
        c.put(14, y, GP_EDGE)
        c.put(15, y, (208, 210, 232))
    c.fill(10, 52, 10, 2, GOLD)    # guarda
    c.put(10, 52, GOLD_D)
    c.put(19, 52, GOLD_D)
    c.fill(13, 54, 4, 5, GOLD_D)   # punho
    c.fill(13, 59, 4, 2, GOLD)     # pomo
    # rachadura no torso
    cx = 29
    for y in range(42, 58, 2):
        cx += hash2(5, y) % 3 - 1
        c.put(cx, y, GP_DARK)
    # musgo vivo subindo do pedestal
    for x in range(4, 44):
        if chance(x, 301, 4):
            c.put(x, 1 + hash2(x, 302) % 8, GP_MOSS)
        if chance(x, 303, 7):
            c.put(x, 8 + hash2(x, 304) % 6, GP_MOSS_L)
    c.outline()
    c.save("statue_warrior")


def build_statue_mage():
    """Estátua de mago em pedra clara, orbe roxo ainda pulsando."""
    c = Canvas(44, 88)
    # pedestal
    c.fill(6, 0, 32, 6, GP_DARK)
    for x in range(6, 38):
        c.put(x, 6, GP_LIT)
    c.fill(10, 7, 24, 5, GP_BASE)
    c.fill(13, 12, 18, 3, GP_DARK)
    # manto afunilando até os ombros
    for y in range(15, 60):
        half = 10 - (y - 15) * 5 // 45
        for x in range(22 - half, 22 + half):
            col = GP_BASE
            if x == 22 - half:
                col = GP_LIT
            elif x >= 22 + half - 2:
                col = GP_DARK
            if (x + y) % 9 == 0:
                col = GP_DARK  # dobras do manto
            if chance(x, y, 23):
                col = GP_MOSS
            c.put(x, y, col)
    # capuz com rosto na sombra
    for dy in range(-6, 7):
        for dx in range(-6, 7):
            if dx * dx + dy * dy <= 36:
                col = GP_BASE if dy > -3 else GP_DARK
                if dx <= -4:
                    col = GP_LIT
                if -3 <= dx <= 3 and -4 <= dy <= 0:
                    col = WIN_DARK
                c.put(22 + dx, 66 + dy, col)
    # cajado partido com ponteira dourada
    for y in range(15, 68):
        c.put(34, y, TRUNK if y % 6 else TRUNK_L)
    c.fill(33, 66, 3, 3, GOLD_D)   # ponteira lascada
    c.put(34, 68, GOLD)
    # orbe roxo VIVO caído no pedestal (a magia ainda não morreu)
    for dy in range(-3, 4):
        for dx in range(-3, 4):
            if dx * dx + dy * dy <= 9:
                col = ORB if (dx + dy) % 2 else ORB_D
                if dx * dx + dy * dy <= 2:
                    col = (212, 168, 255)  # núcleo brilhante
                c.put(9 + dx, 15 + dy, col)
    c.put(6, 15, ORB_D)  # lasca solta
    c.outline()
    c.save("statue_mage")


def build_ruin_wall():
    """Pedaço de muralha clara com arcos, farrapo de estandarte e vinhas vivas."""
    c = Canvas(80, 64)
    for x in range(80):
        top = 58 - hash2(x, 401) % 10
        if 60 <= x < 80:
            top = min(top, 58 - (x - 60) * 2)  # canto desabado
        for y in range(top):
            c.put(x, y, gp_shade(x, y + 151))
        c.put(x, top, GP_EDGE)
    arch_hole(c, 14, 14, 14, 30)
    arch_hole(c, 44, 14, 14, 30)
    # farrapo de estandarte vermelho pendurado no topo
    for y in range(14):
        width = 6 - (y - 9 if y > 9 else 0)
        if y > 10 and y % 2 == 0:
            width -= 1
        for x in range(max(0, width)):
            col = BAN_D if ((x + y) % 5 == 0 or y < 2) else (140, 32, 40)
            c.put(33 + x, 52 - y, col)
    # vinhas verdes vivas
    for i in range(0, 80, 5):
        if chance(i, 402, 3):
            ln = 4 + hash2(i, 404) % 10
            for y in range(ln):
                sx = i + hash2(i, y) % 2
                c.put(sx, 30 + hash2(i, 403) % 22 - y, GP_MOSS if y % 2 == 0 else GP_MOSS_L)
    for x in range(80):
        if chance(x, 405, 4):
            c.put(x, hash2(x, 406) % 7, GP_MOSS)
    c.outline()
    c.save("ruin_wall")


def build_bush():
    """Moita de musgo verde vivo com florzinhas pálidas."""
    c = Canvas(26, 14)
    for (bx, by, r) in ((7, 4, 6), (14, 5, 7), (20, 3, 5)):
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if dx * dx + dy * dy * 3 <= r * r and by + dy >= 0:
                    col = GP_MOSS_L if dy >= r // 2 or chance(bx + dx, by + dy, 7) else GP_MOSS
                    c.put(bx + dx, by + dy, col)
    # florzinhas discretas
    for (fx, fy) in ((6, 8), (15, 10), (21, 6)):
        c.put(fx, fy, (196, 199, 232))
    c.outline()
    c.save("bush")


def build_stones():
    """Trio de pedras claras com musgo vivo."""
    c = Canvas(24, 12)
    for (sx, sy, r) in ((5, 0, 4), (13, 0, 6), (20, 0, 3)):
        for dy in range(0, r + 1):
            for dx in range(-r + dy // 2, r - dy // 2 + 1):
                col = GP_BASE
                if dy == r or dy == r - 1:
                    col = GP_LIT
                if dx <= -r + dy // 2 + 1:
                    col = GP_DARK
                c.put(sx + dx, sy + dy, col)
        if chance(sx, 501, 2):
            c.put(sx, sy + r + 1, GP_MOSS_L)
    c.outline()
    c.save("stones")


def build_fallen():
    """Segmento de coluna claro caído na horizontal."""
    c = Canvas(52, 18)
    for y in range(2, 14):
        # ponta direita quebrada
        end = 50 - abs((y * 5) % 7 - 3)
        for x in range(2, end):
            col = GP_BASE
            if y >= 12:
                col = GP_LIT
            elif y <= 3:
                col = GP_DARK
            if x % 9 == 0:
                col = GP_DARK  # caneluras
            if chance(x, y, 29):
                col = GP_MOSS
            c.put(x, y, col)
    # base da coluna (anel maior na esquerda) com friso dourado
    c.fill(0, 0, 6, 16, GP_BASE)
    for y in range(16):
        c.put(0, y, GP_LIT)
        c.put(5, y, GOLD_D if y % 3 == 0 else GP_DARK)
    for x in range(4, 48):
        if chance(x, 502, 6):
            c.put(x, 14 + hash2(x, 503) % 2, GP_MOSS_L)
    c.outline()
    c.save("fallen")


# ============ PRIMEIRO PLANO (silhuetas com parallax invertido) ============
def build_fore_thorns():
    """Espinheiro retorcido para a camada frontal (usado por T1Scenery.ForeProp)."""
    w, h = 96, 26
    c = Canvas(w, h)
    for x in range(w):
        hh = 6 + int(5 * math.sin(x * 0.16) + 3 * math.sin(x * 0.045 + 2.0)) + hash2(x, 40) % 3
        for y in range(max(2, hh)):
            c.put(x, y, TRUNK)
    for x in range(2, w - 2):
        if chance(x, 41, 7):
            ln = 5 + hash2(x, 42) % 9
            dx = -1 if hash2(x, 43) % 2 else 1
            px = x
            for i in range(ln):
                if i % 3 == 0:
                    px += dx
                c.put(px, 8 + i, TRUNK_L if i % 4 else TRUNK)
    c.outline()
    c.save("fore_thorns")


# ==================== PLATAFORMAS DE RUÍNA ====================
VINE_ROWS = 10
TUFT_ROWS = 3


def build_platform(width_units):
    body_w = round(width_units * 16)
    body_h = 6
    w = body_w + 4
    h = TUFT_ROWS + body_h + VINE_ROWS
    c = Canvas(w, h)
    body_bottom = VINE_ROWS
    for y in range(body_h):
        inset_l = hash2(y, 31) % 3
        inset_r = hash2(y, 47) % 3
        for x in range(inset_l, w - inset_r):
            col = gp_shade(x, y + 500)
            if (x + (y // 3) * 5) % 9 == 8 or y % 3 == 2:
                col = GP_DARK
            if y == body_h - 1:
                col = GP_EDGE if x % 7 < 5 else GP_LIT
            c.put(x, body_bottom + y, col)
    body_top = body_bottom + body_h
    for x in range(2, w - 2):
        if chance(x, 77, 4):
            c.put(x, body_top, GP_MOSS_L)
            if chance(x, 78, 2):
                c.put(x, body_top + 1, GP_MOSS)
    for x in range(1, w - 1):
        if chance(x, 55, 5):
            ln = 2 + hash2(x, 56) % (VINE_ROWS - 3)
            for i in range(ln):
                c.put(x, body_bottom - 1 - i, GP_MOSS if i % 2 == 0 else GP_MOSS_L)
    # juntas de tijolo no corpo de pedra
    for y in range(body_h - 1):
        off = 0 if (y // 3) % 2 == 0 else 4
        for x in range(2, w - 2):
            if (x + off) % 8 == 0 and y % 3 != 2:
                c.put(x, body_bottom + y, GP_DARK)
    # rachaduras diagonais
    for cx0 in (w // 4, (w * 2) // 3):
        for i in range(body_h - 1):
            c.put(cx0 + i // 2, body_bottom + i, GP_DARK)
    # tufos de grama no topo
    for x in range(3, w - 3):
        if chance(x, 91, 6):
            c.put(x, body_top + 1, GP_MOSS_L)
            c.put(x, body_top + 2, GP_MOSS_L if chance(x, 92, 2) else GP_MOSS)
            if chance(x, 93, 3):
                c.put(x + 1, body_top + 1, GP_MOSS)
    # folhas nas vinhas penduradas
    for x in range(1, w - 1):
        if chance(x, 55, 5) and chance(x, 57, 2):
            ln = 2 + hash2(x, 56) % (VINE_ROWS - 3)
            c.put(x + 1, body_bottom - ln + 1, GP_MOSS_L)
    # runa antiga com brilho arcano
    rx = max(6, w - 10)
    c.put(rx, body_bottom + 2, ORB)
    c.put(rx + 1, body_bottom + 3, ORB_D)
    c.put(rx, body_bottom + 4, ORB_D)
    c.fill(1, body_bottom - 3, 3, 3, GP_DARK)
    c.fill(w - 5, body_bottom - 4, 4, 4, GP_DARK)
    c.outline()
    c.save(f"plat{round(width_units * 100)}")


# ==================== METAS DO UNITY ====================
META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0}}
  spritePixelsToUnits: 16
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write_metas():
    for png in sorted(OUT.glob("*.png")):
        meta = png.with_suffix(".png.meta")
        if meta.exists():
            continue
        meta.write_text(META.format(guid=uuid.uuid4().hex))


# ===================== CIDADE-TEMPLO ALIENÍGENA =====================
# Bíblia: templo de uma raça que parecia divina. Pedra + círculo + cristal.
# Não é castelo. Camadas nativas baixas (SNES), parallax no Unity.

CRYS = (64, 214, 188)
CRYS_D = (18, 78, 74)
CRYS_H = (186, 255, 236)
RUST = (128, 58, 32)
RUST_D = (72, 32, 22)
GLYPH = (176, 132, 64)


def disc(c, cx, cy, r, col, hole=0):
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            d = math.hypot(dx, dy)
            if hole < d <= r:
                c.put(cx + dx, cy + dy, col)


def stepped(c, x0, y0, w, h, steps):
    """Pirâmide em degraus, topo quebrado."""
    for s in range(steps):
        inset = s * (w // (steps * 2))
        sh = h // steps
        for x in range(inset, w - inset):
            top = sh - (hash2(x0 + x, s) % 2)
            if s == steps - 1 and x > w * 0.6:
                top = max(2, top // 2)
            for y in range(top):
                col = stone_shade(x0 + x, y0 + s * sh + y)
                if x == inset:
                    col = ST_DARK
                elif x == inset + 1:
                    col = ST_LIT
                c.put(x0 + x, y0 + s * sh + y, col)
            c.put(x0 + x, y0 + s * sh + top, ST_EDGE)
    # círculo religioso no centro do corpo
    disc(c, x0 + w // 2, y0 + h // 2, 7, ST_EDGE, hole=5)
    disc(c, x0 + w // 2, y0 + h // 2, 3, GLYPH, hole=1)
    c.put(x0 + w // 2, y0 + h // 2, CRYS)


def god_face(c, x0, y0, s=1):
    """Rosto não humano entalhado: olhos demais, boca em fenda."""
    w, h = 14 * s, 16 * s
    for y in range(h):
        inset = abs(y - h // 2) // 3
        for x in range(inset, w - inset):
            c.put(x0 + x, y0 + y, MARBLE if y > 2 else MARBLE_D)
    # três olhos
    for ex in (3, 7, 11):
        c.fill(x0 + ex * s, y0 + 9 * s, 2 * s, 2 * s, WIN_DARK)
        c.put(x0 + ex * s, y0 + 10 * s, CRYS_D)
    c.fill(x0 + 5 * s, y0 + 4 * s, 4 * s, 1, INK)
    moss_clump(c, x0 + 1, y0 + h - 3, 5, 2)


def multi_arm_colossus(c, x0, y0):
    """Estátua colossal quebrada: tronco, vários braços, cabeça caída ao lado."""
    # pedestal
    c.fill(x0 + 6, y0, 22, 6, MARBLE_D)
    moss_clump(c, x0 + 4, y0 + 4, 8, 3)
    # torso
    for y in range(28):
        half = 8 - y // 10
        for x in range(-half, half + 1):
            col = MARBLE_L if x > half - 2 else MARBLE
            if x < -half + 2:
                col = MARBLE_D
            c.put(x0 + 16 + x, y0 + 6 + y, col)
    # braços extras, um quebrado no ar, um no chão
    c.fill(x0 + 4, y0 + 22, 10, 3, MARBLE)
    c.fill(x0 + 24, y0 + 24, 12, 3, MARBLE_L)
    c.fill(x0 + 34, y0 + 8, 8, 3, MARBLE_D)  # braço caído
    c.fill(x0, y0 + 16, 6, 2, MARBLE)
    # cristal no peito — âncora de luz (x≈ x0+16, y≈ y0+20)
    disc(c, x0 + 16, y0 + 20, 3, CRYS, hole=1)
    c.put(x0 + 16, y0 + 20, CRYS_H)
    # cabeça tombada
    god_face(c, x0 + 30, y0 + 1)
    # raiz atravessando o pedestal
    for i in range(14):
        c.put(x0 + 8 + i, y0 + 2 + (i % 3), MOSS if i % 2 == 0 else (46, 32, 24))


def ring_gate(c, x0, y0):
    """Portão circular partido. O vão escuro é a boca do templo."""
    disc(c, x0, y0 + 28, 26, ST_BASE, hole=18)
    disc(c, x0, y0 + 28, 18, WIN_DARK, hole=0)
    # anel externo rachado: falta um quarto
    for dy in range(-26, 27):
        for dx in range(-26, 27):
            d = math.hypot(dx, dy)
            if 20 <= d <= 26 and not (dx > 8 and dy > 6):
                col = ST_EDGE if int(d) == 26 else ST_LIT
                if hash2(x0 + dx, y0 + dy) % 9 == 0:
                    col = MOSS
                c.put(x0 + dx, y0 + 28 + dy, col)
    # símbolo no arco
    disc(c, x0, y0 + 48, 4, GLYPH, hole=2)
    # cristal no limiar — segunda âncora
    c.fill(x0 - 2, y0 + 8, 4, 6, CRYS_D)
    c.fill(x0 - 1, y0 + 10, 2, 3, CRYS_H)
    # ferragem enferrujada na ombreira
    c.fill(x0 - 22, y0 + 10, 3, 16, RUST_D)
    c.fill(x0 - 22, y0 + 18, 8, 2, RUST)


def buried_machine(c, x0, y0):
    """Mecanismo enterrado na pedra: não é ficção científica limpa, é ruína."""
    c.fill(x0, y0, 28, 8, ST_DARK)
    for x in range(0, 28, 4):
        c.fill(x0 + x, y0 + 2, 2, 5, RUST if x % 8 else RUST_D)
    disc(c, x0 + 14, y0 + 6, 5, ST_EDGE, hole=3)
    c.put(x0 + 14, y0 + 6, CRYS)
    moss_clump(c, x0 + 2, y0 + 7, 6, 2)


def alien_growth(c, x0, y0):
    """Fungo baixo e cipó. Poucos pixels, leitura de longe."""
    c.fill(x0, y0, 2, 7, (40, 28, 22))
    for i in range(5):
        c.put(x0 + 1 + (i % 2), y0 + 7 + i, MOSS_LIT if i % 2 == 0 else MOSS)
    c.put(x0 + 3, y0 + 10, CRYS_D)
    c.put(x0 - 1, y0 + 4, MOSS)


def creature_hint(c, x0, y0):
    """Bicho pequeno entre pedras — dois olhos, não um personagem."""
    c.fill(x0, y0, 5, 2, (28, 36, 32))
    c.put(x0 + 1, y0 + 2, CRYS_H)
    c.put(x0 + 3, y0 + 2, CRYS_H)


def build_sky_temple():
    c = Canvas(480, 192)
    for y in range(c.h):
        g = 1 - y / (c.h - 1)
        for x in range(c.w):
            band = g * 5
            i0 = int(band)
            f = band - i0
            step = i0 + (1 if (f > 0.5 and (x // 2 + y) % 2 == 0) else 0)
            q = min(1.0, step / 5)
            if q < 0.55:
                t = q / 0.55
                col = tuple(int(SKY_TOP[i] + (SKY_MID[i] - SKY_TOP[i]) * t) for i in range(3))
            else:
                t = (q - 0.55) / 0.45
                col = tuple(int(SKY_MID[i] + (SKY_HOR[i] - SKY_MID[i]) * t) for i in range(3))
            if y < 22:
                e = (22 - y) / 22
                if (x // 2) % 3 != 0:
                    col = tuple(int(col[i] + (BLOOD[i] - col[i]) * e * 0.45) for i in range(3))
            c.put(x, y, col)
    # planeta baixo, à esquerda — disco com uma faixa, não um degradê
    disc(c, 78, 36, 28, (24, 18, 40), hole=0)
    for y in range(22, 40):
        for x in range(52, 106):
            if math.hypot(x - 78, y - 36) <= 28 and y % 4 < 2:
                c.put(x, y, (46, 28, 58))
    # lua partida no alto direito
    disc(c, 350, 150, 14, MOON, hole=0)
    disc(c, 358, 156, 7, SKY_MID, hole=0)
    for y in range(130, c.h):
        for x in range(c.w):
            if chance(x, y, 1400):
                c.put(x, y, MOON)
    c.save("sky")


def build_far_temple():
    """Horizonte: metrópole em silhueta e uma estátua gigante quebrada."""
    c = Canvas(640, 210)
    SIL = (18, 16, 34)
    SIL_L = (28, 24, 46)
    for x in range(c.w):
        hh = 18 + int(8 * math.sin(x * 0.02)) + hash2(x, 1) % 3
        for y in range(hh):
            c.put(x, y, FAR_A if y < hh - 4 else FAR_B)
        if x % 14 == 0 and hh > 14:
            c.put(x, 10, CRYS_D)
            c.put(x, 11, CRYS_D)

    def tower(x, w, h, broken):
        c.fill(x, 16, w, h, SIL)
        c.fill(x, 16, 2, h, SIL_L)
        top = 16 + h
        if broken:
            for i in range(w):
                c.put(x + i, top - (i % 4), SIL)
        else:
            disc(c, x + w // 2, top, max(4, w // 2), SIL, hole=0)

    tower(24, 16, 100, True)
    tower(52, 22, 150, False)
    tower(88, 12, 80, True)
    tower(520, 18, 130, True)
    tower(556, 14, 96, False)

    # estátua colossal — o corpo sobe da cidade e a cabeça está partida
    bx, by = 250, 14
    c.fill(bx, by, 78, 96, SIL)
    c.fill(bx, by, 4, 96, SIL_L)
    for y in range(50, 96):
        cut = (y - 50) // 2
        for x in range(78 - cut, 78):
            c.put(bx + x, by + y, (0, 0, 0, 0))
    disc(c, bx + 36, by + 118, 32, SIL, hole=0)
    disc(c, bx + 58, by + 136, 14, (0, 0, 0, 0), hole=0)
    c.fill(bx + 16, by + 112, 6, 5, (6, 6, 14))
    c.fill(bx + 32, by + 118, 6, 5, (6, 6, 14))
    c.fill(bx + 46, by + 112, 5, 5, (6, 6, 14))
    # braço caído na encosta, separado do corpo
    c.fill(150, 18, 90, 10, SIL)
    c.fill(150, 26, 90, 2, SIL_L)
    c.put(bx + 40, by + 70, CRYS)
    c.save("far")


def build_city_temple():
    """Meio: cabeça enterrada, templo em degraus, portão circular."""
    c = Canvas(640, 168)
    for x in range(c.w):
        hh = 7 + hash2(x // 5, 3) % 3
        for y in range(hh):
            c.put(x, y, stone_shade(x, y + 4))
    # corpo enterrado — só o dorso aparece
    for x in range(150):
        hh = int(16 * math.sin(x / 150 * math.pi))
        for y in range(hh):
            col = MARBLE_D if y < 3 else MARBLE
            if y == hh - 1:
                col = MOSS if x % 4 == 0 else MARBLE_L
            c.put(20 + x, y, col)
    deity_head(c, 48, 8, 62, 50)
    moss_clump(c, 46, 10, 14, 4)
    # templo
    stepped(c, 210, 6, 90, 64, 4)
    god_face(c, 242, 28, 1)
    crack_wall(c, 230, 20, 22)
    # portão — cristal na base é a âncora (430, 22)
    ring_gate(c, 470, 6)
    alien_growth(c, 190, 8)
    alien_growth(c, 330, 8)
    alien_growth(c, 560, 10)
    vine(c, 200, 28, 16)
    vine(c, 520, 36, 18)
    creature_hint(c, 160, 8)
    creature_hint(c, 400, 7)
    rubble_blocks(c, 360, 4, 4)
    settle_moss(c)
    c.save("city")


def build_near_veil():
    """Camada da frente: cipó, pedra e caco, com alpha. Passa na frente do herói."""
    c = Canvas(640, 80)
    for i, x in enumerate((12, 80, 150, 230, 310, 390, 470, 560)):
        vine(c, x, 70, 18 + hash2(x, 1) % 16)
        if i % 2 == 0:
            rubble_blocks(c, x - 6, 2, 3)
            moss_clump(c, x, 2, 6, 3)
        else:
            c.fill(x, 4, 3, 8, CRYS_D)
            c.put(x + 1, 10, CRYS)
    c.save("near")


if __name__ == "__main__":
    build_sky_temple()
    build_far_temple()
    build_city_temple()
    build_near_veil()
    build_pavement()
    build_tree()
    build_statue()
    build_lamp()
    build_rubble()
    build_column()
    build_statue_warrior()
    build_statue_mage()
    build_ruin_wall()
    build_bush()
    build_stones()
    build_fallen()
    build_fore_thorns()
    for wu in (5.4, 4.2, 1.15):
        build_platform(wu)
    write_metas()
    print("camadas geradas em", OUT)
