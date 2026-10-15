#!/usr/bin/env python3
"""
Gera os sprites de alta resolução em pixel art 16-bit para a Fase I:
Aetherion — Antes da Queda em um Universo Alienígena / Extraterrestre Lúdico.
Combina a arquitetura de templos clássicos com montanhas espirais, lua anelada diurna,
monólitos de cristal flutuantes e vegetação exótica de outro planeta.
Saída: Assets/Resources/GoldenAge/*.png (com .meta para Unity).
"""
import math
import uuid
from pathlib import Path
from PIL import Image

ROOT = Path("/Users/gabriellucas/Desktop/trabalho jogo sexta")
OUT = ROOT / "Assets/Resources/GoldenAge"
OUT.mkdir(parents=True, exist_ok=True)

# ----------------- Paleta 16-bit Alienígena de Aetherion -----------------
# Céu & Planetas
SUN_CORE = (255, 255, 245)
SUN_GOLD = (255, 230, 145)
MOON_SURF = (195, 218, 238)
MOON_SHADE = (130, 158, 192)
MOON_RING = (235, 245, 255, 110)
SKY_DEEP = (45, 88, 165)
SKY_MID = (78, 145, 218)
SKY_LGT = (125, 190, 240)
SKY_HORIZON = (185, 228, 222)

# Nuvens
CLOUD_WHITE = (255, 255, 255)
CLOUD_PEARL = (235, 240, 250)
CLOUD_SHADE = (150, 165, 195)
CLOUD_DARK = (110, 125, 155)

# Cristais & Monólitos Flutuantes
CRYSTAL_L = (235, 250, 255)
CRYSTAL_M = (145, 215, 248)
CRYSTAL_D = (85, 155, 210)
CRYSTAL_GLOW = (165, 235, 255, 140)

# Montanhas Alienígenas (Espirais & Picos de Rocha Púrpura/Teal)
MTN_CREST = (155, 190, 198)
MTN_LIGHT = (105, 148, 158)
MTN_MID = (70, 105, 118)
MTN_DARK = (46, 70, 82)
MTN_VIOLET = (82, 68, 102)
MTN_MIST = (175, 215, 220, 180)

# Floresta Alienígena Lúdica
F_SPORE_CAP = (215, 85, 148)
F_SPORE_LGT = (248, 145, 195)
F_TEAL_L = (68, 195, 172)
F_TEAL_M = (38, 142, 128)
F_TEAL_D = (18, 82, 74)
F_FRONT_L = (95, 185, 58)
F_FRONT_M = (48, 122, 38)
F_FRONT_D = (22, 65, 24)
F_PURPLE_M = (118, 62, 138)
F_PURPLE_D = (74, 34, 92)
F_TRUNK = (78, 48, 36)
F_TRUNK_L = (108, 68, 48)

# Mármore dos Templos & Colunas
M_WHITE = (252, 250, 245)
M_LIGHT = (238, 234, 224)
M_BASE = (212, 206, 194)
M_SHADE = (156, 150, 142)
M_DARK = (108, 102, 96)
M_CREVICE = (68, 62, 58)
DOOR_GLOW = (255, 205, 85)
DOOR_DARK = (28, 22, 26)

# Vegetação & Hera Bioluminescente
VINE_L = (92, 188, 65)
VINE_M = (52, 134, 44)
VINE_D = (26, 80, 28)
BIO_CYAN = (95, 235, 255)
BIO_PINK = (255, 110, 195)

# Flores & Esporos
FLOWER_PINK = (255, 115, 175)
FLOWER_YELLOW = (255, 220, 65)
FLOWER_BLUE = (95, 195, 255)
FLOWER_VIOLET = (195, 125, 255)

# Chão (Muro de Cantaria + Terra Alienígena + Grama)
GRASS_WIRE = (198, 252, 95)
GRASS_TOP = (95, 188, 48)
GRASS_MID = (56, 132, 34)
GRASS_SHADE = (28, 78, 22)
SOIL_L = (122, 75, 52)
SOIL_D = (78, 42, 32)
STONE_BEVEL = (238, 232, 220)
STONE_FACE = (210, 202, 188)
STONE_SHADE = (162, 154, 140)
STONE_JOINT = (105, 96, 86)

# Ouro Rúnico
RUNE_GOLD_L = (255, 238, 145)
RUNE_GOLD = (248, 198, 48)
RUNE_GOLD_D = (188, 138, 26)


def hash2(x, y):
    h = (x * 374761393 + y * 668265263) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return (h ^ (h >> 16)) & 0x7FFFFFFF


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.px = self.img.load()

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            color = c if len(c) == 4 else c + (255,)
            if color[3] < 255:
                prev = self.px[x, self.h - 1 - y]
                if prev[3] == 0:
                    self.px[x, self.h - 1 - y] = color
                else:
                    a = color[3] / 255.0
                    inv = 1.0 - a
                    out_r = int(color[0] * a + prev[0] * inv)
                    out_g = int(color[1] * a + prev[1] * inv)
                    out_b = int(color[2] * a + prev[2] * inv)
                    out_a = min(255, int(color[3] + prev[3] * inv))
                    self.px[x, self.h - 1 - y] = (out_r, out_g, out_b, out_a)
            else:
                self.px[x, self.h - 1 - y] = color

    def fill(self, x, y, w, h, c):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                self.put(xx, yy, c)

    def save(self, name):
        out_png = OUT / f"{name}.png"
        self.img.save(out_png)
        self.create_meta(out_png)

    def create_meta(self, png_path):
        meta_path = png_path.with_suffix(".png.meta")
        if meta_path.exists():
            return
        g = uuid.uuid4().hex
        meta_content = f"""fileFormatVersion: 2
guid: {g}
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
"""
        meta_path.write_text(meta_content, encoding="utf-8")


# =====================================================================
# 1. CÉU ALIENÍGENA: SOL DOURADO + LUA GIGANTE COM ANÉIS DIURNOS
# =====================================================================
def build_sky():
    c = Canvas(480, 200)
    # Gradiente do céu diurno alienígena
    for y in range(c.h):
        t = y / (c.h - 1)
        if t > 0.45:
            st = (t - 0.45) / 0.55
            col = tuple(int(SKY_MID[i] + (SKY_DEEP[i] - SKY_MID[i]) * st) for i in range(3))
        else:
            st = t / 0.45
            col = tuple(int(SKY_HORIZON[i] + (SKY_MID[i] - SKY_HORIZON[i]) * st) for i in range(3))

        for x in range(c.w):
            d = (x + y * 2) % 4 == 0
            dither = (hash2(x, y) % 5) - 2
            adj_col = tuple(max(0, min(255, col[i] + (dither if d else 0))) for i in range(3))
            c.put(x, y, adj_col)

    # 1. Gigantesca Lua Alienígena Translúcida com Anéis Planetários (à esquerda/meio)
    mx, my, mr = 220, 135, 38
    # Anel planetário elíptico inclinado
    for ring_r in range(mr + 8, mr + 24):
        for deg in range(0, 360, 2):
            rad = math.radians(deg)
            rx = int(mx + math.cos(rad) * (ring_r * 1.5))
            ry = int(my + math.sin(rad) * (ring_r * 0.45) - (math.cos(rad) * 6))
            alpha = int(45 + math.sin(rad) * 20)
            c.put(rx, ry, (230, 245, 255, alpha))

    # Corpo da Lua Alienígena
    for dy in range(-mr, mr + 1):
        for dx in range(-mr, mr + 1):
            d = math.hypot(dx, dy)
            if d <= mr:
                fact = (dx + dy * 0.4) / mr
                col = MOON_SURF if fact > 0.1 else MOON_SHADE
                # Crateras / mares lunares alienígenas
                if ((dx + 12) ** 2 + (dy - 8) ** 2 < 64) or ((dx - 16) ** 2 + (dy + 10) ** 2 < 48):
                    col = MOON_SHADE
                # Translucidez no céu diurno
                c.put(mx + dx, my + dy, col + (145,))

    # 2. Sol Dourado Radiante (à direita)
    sx, sy = 390, 155
    r_core = 14
    r_halo = 48
    for dy in range(-r_halo, r_halo + 1):
        for dx in range(-r_halo, r_halo + 1):
            d = math.hypot(dx, dy)
            if d <= r_core:
                c.put(sx + dx, sy + dy, SUN_CORE)
            elif d <= r_halo:
                fact = 1.0 - (d - r_core) / (r_halo - r_core)
                alpha = int(fact * fact * 115)
                c.put(sx + dx, sy + dy, SUN_GOLD + (alpha,))

    # Raios solares dourados
    for angle in (-0.52, -0.66, -0.80, -0.94, -1.08):
        for dist in range(10, 360):
            rx = int(sx + math.cos(angle) * dist)
            ry = int(sy + math.sin(angle) * dist)
            beam_w = int(10 + dist * 0.08)
            alpha = int(max(0, 32 * (1.0 - dist / 360.0)))
            for w in range(-beam_w // 2, beam_w // 2):
                c.put(rx + w, ry, SUN_GOLD + (alpha,))

    c.save("sky")


def build_clouds():
    c = Canvas(480, 96)
    cloud_defs = [
        (60, 48, 80, 24),
        (190, 62, 110, 28),
        (340, 50, 95, 26),
        (450, 68, 70, 22),
    ]
    for cx, cy, cw, ch in cloud_defs:
        for dy in range(-ch, ch):
            for dx in range(-cw // 2, cw // 2):
                nx = dx / (cw * 0.5)
                ny = dy / ch
                if nx * nx + ny * ny <= 1.0:
                    y_ratio = (dy + ch) / (2.0 * ch)
                    if y_ratio > 0.65:
                        col = CLOUD_WHITE
                    elif y_ratio > 0.35:
                        col = CLOUD_PEARL
                    elif y_ratio > 0.15:
                        col = CLOUD_SHADE
                    else:
                        col = CLOUD_DARK
                    c.put(cx + dx, cy + dy, col)
    c.save("clouds")


# =====================================================================
# 2. MONTANHAS ALIENÍGENAS: ESPIRAIS LÚDICAS + TEMPLO CELESTIAL
# =====================================================================
def build_mountains():
    c = Canvas(640, 180)
    peak_x = 450
    peak_y = 120

    # Spire espirais alienígenas em x=180 e x=310
    spires = [(180, 140, 26), (300, 150, 30)]

    for x in range(c.w):
        h = int(
            45
            + 24 * math.sin(x * 0.015)
            + 18 * math.cos(x * 0.038)
            + 12 * math.sin(x * 0.065)
        )
        # Pico rochoso do templo à direita
        d_peak = abs(x - peak_x)
        if d_peak < 60:
            elev = int((1.0 - d_peak / 60.0) * 58)
            h = max(h, peak_y - 20 + elev)

        # Formações rochosas alienígenas em espiral
        for sp_x, sp_h, sp_w in spires:
            d_sp = abs(x - sp_x)
            if d_sp < sp_w:
                twist = math.sin((x - sp_x) * 0.2) * 8
                elev = int((1.0 - d_sp / float(sp_w)) * sp_h + twist)
                h = max(h, elev)

        for y in range(h):
            light = math.sin((x + y * 0.6) * 0.12)
            # Montanhas em tons verde-azulados e veios violeta alienígenas
            if y >= h - 3:
                col = MTN_CREST
            elif (x + y * 2) % 18 == 0:
                col = MTN_VIOLET
            elif light > 0.25:
                col = MTN_LIGHT
            elif light > -0.2:
                col = MTN_MID
            else:
                col = MTN_DARK

            # Névoa mística na base
            if y < 22:
                mist_a = int((1.0 - y / 22.0) * 160)
                c.put(x, y, col)
                c.put(x, y, MTN_MIST[:3] + (mist_a,))
            else:
                c.put(x, y, col)

    # Templo Celestial Iluminado no Pico
    tx, ty = peak_x - 22, peak_y + 12
    tw, th = 44, 24
    c.fill(tx - 2, ty, tw + 4, 3, M_LIGHT)
    c.fill(tx + 2, ty + 3, tw - 4, 12, DOOR_DARK)
    for col_i in range(7):
        cx = tx + 3 + col_i * 6
        c.fill(cx, ty + 3, 2, 12, M_WHITE)
    c.fill(tx - 2, ty + 15, tw + 4, 3, M_WHITE)
    for ph in range(7):
        pw = (tw + 4) - ph * 6
        if pw <= 2:
            break
        c.fill(tx + tw // 2 - pw // 2, ty + 18 + ph, pw, 1, M_WHITE)

    # Feixe celestial místico subindo aos céus do templo
    for fy in range(ty + 20, c.h):
        alpha = int(45 * (1.0 - (fy - ty) / (c.h - ty)))
        c.fill(peak_x - 6, fy, 12, 1, (180, 240, 255, alpha))

    # Monólitos de cristal flutuando no horizonte alienígena
    for mx, my, mw, mh in ((120, 110, 6, 22), (250, 125, 8, 26), (360, 118, 5, 18), (530, 130, 7, 24)):
        # Cristal com pontas afiadas
        for cy in range(mh):
            prog = cy / float(mh)
            curr_w = int(mw * math.sin(prog * math.pi))
            c.fill(mx - curr_w // 2, my + cy, curr_w, 1, CRYSTAL_M)
            c.put(mx + curr_w // 2, my + cy, CRYSTAL_L)
            c.put(mx - curr_w // 2, my + cy, CRYSTAL_D)
        # Brilho do monólito
        for dy in range(-2, mh + 3):
            for dx in range(-mw, mw + 1):
                c.put(mx + dx, my + dy, CRYSTAL_GLOW)

    c.save("mountains")


# =====================================================================
# 3. FLORESTA ALIENÍGENA (ÁRVORES ESPIRAIS, COGUMELOS GIGANTES & TEAL)
# =====================================================================
def build_forest():
    c = Canvas(640, 130)
    # Fileira de trás (azulada/turquesa alienígena)
    for x in range(0, c.w, 16):
        r = 16 + (x % 5) * 2
        cy = 44 + (x % 4) * 4
        is_spore = (x % 48 == 0)
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if dx * dx + dy * dy <= r * r:
                    if is_spore:
                        col = F_SPORE_LGT if dy > 0 else F_SPORE_CAP
                    else:
                        col = F_TEAL_L if dy > 0 else F_TEAL_D
                    c.put(x + dx, cy + dy, col)

    # Fileira da frente (copas esmeralda, árvores espirais e cogumelos exóticos)
    for x in range(0, c.w, 24):
        r = 24 + (x % 7) * 2
        cy = 68 + (x % 5) * 4
        # Tronco retorcido alienígena
        c.fill(x - 2, 0, 5, cy - 8, F_TRUNK)
        c.fill(x - 2, 0, 2, cy - 8, F_TRUNK_L)

        # Cogumelo alienígena gigante intercalado
        if x % 72 == 0:
            cap_w, cap_h = 32, 16
            for dy in range(cap_h):
                cw = int(cap_w * math.sin((dy / float(cap_h)) * math.pi * 0.8 + 0.3))
                for dx in range(-cw // 2, cw // 2 + 1):
                    col = F_SPORE_LGT if (dx * 3 + dy * 2) % 5 == 0 else F_SPORE_CAP
                    c.put(x + dx, cy - 8 + dy, col)
        else:
            # Copa arredondada com mescla de verde e púrpura extraterrestre
            for dy in range(-r, r + 1):
                for dx in range(-r, r + 1):
                    d = math.hypot(dx, dy)
                    if d <= r:
                        nx = (dx + dy * 0.5) / r
                        if dy > r * 0.35 and dx > -r * 0.2:
                            col = F_FRONT_L
                        elif nx > -0.25:
                            col = F_FRONT_M
                        elif (x + dx) % 11 == 0:
                            col = F_PURPLE_M
                        else:
                            col = F_FRONT_D
                        c.put(x + dx, cy + dy, col)

    c.save("forest")


def build_colonnade():
    c = Canvas(640, 90)
    col_xs = [30, 80, 140, 210, 280, 350, 420, 490, 560, 615]
    for i, cx in enumerate(col_xs):
        ch = 42 + (i % 3) * 8
        broken = (i % 4 == 2)
        if broken:
            ch = 24
        cw = 8
        c.fill(cx - 2, 0, cw + 4, 4, M_BASE)
        c.fill(cx - 2, 3, cw + 4, 1, M_LIGHT)
        c.fill(cx, 4, cw, ch, M_BASE)
        c.fill(cx + cw - 3, 4, 3, ch, M_LIGHT)
        c.fill(cx, 4, 2, ch, M_SHADE)
        if not broken:
            c.fill(cx - 3, 4 + ch, cw + 6, 5, M_LIGHT)
            c.fill(cx - 3, 4 + ch, cw + 6, 2, M_SHADE)
        # Gavinhas bioluminescentes
        c.put(cx - 1, 10, BIO_CYAN)
        c.put(cx, 11, VINE_L)
        c.put(cx - 1, 12, BIO_PINK)

    c.save("colonnade")


# =====================================================================
# 4. CHÃO (PAVEMENT), TEMPLOS COM HERA EXÓTICA E PROPS
# =====================================================================
def build_pavement():
    c = Canvas(64, 64)
    # Fio verde-claro de destaque
    c.fill(0, 62, 64, 2, GRASS_WIRE)
    # Grama
    for y in range(54, 62):
        for x in range(64):
            c.put(x, y, GRASS_TOP if y > 58 else (GRASS_MID if y > 55 else GRASS_SHADE))
    # Tufos pendentes
    for x in range(64):
        if x % 3 == 0:
            c.put(x, 53, GRASS_SHADE)
            if x % 6 == 0:
                c.put(x, 52, GRASS_SHADE)

    # Faixa de terra fértil rica
    for y in range(46, 54):
        for x in range(64):
            c.put(x, y, SOIL_L if (x + y * 2) % 3 == 0 else SOIL_D)

    # Muro de pedra de cantaria (ashlar blocks) com chanfros de relevo 3D
    block_h = 9
    block_w = 16
    for row in range(5):
        by = 46 - (row + 1) * block_h
        x_shift = 0 if row % 2 == 0 else block_w // 2
        for col in range(-1, 5):
            bx = col * block_w + x_shift
            c.fill(bx + 1, by + 1, block_w - 2, block_h - 2, STONE_FACE)
            c.fill(bx + 1, by + block_h - 2, block_w - 2, 1, STONE_BEVEL)
            c.fill(bx + block_w - 2, by + 1, 1, block_h - 2, STONE_BEVEL)
            c.fill(bx + 1, by + 1, block_w - 2, 1, STONE_SHADE)
            c.fill(bx + 1, by + 1, 1, block_h - 2, STONE_SHADE)
            c.fill(bx, by, block_w, 1, STONE_JOINT)
            c.fill(bx, by, 1, block_h, STONE_JOINT)

    c.save("pavement")


def build_west_temple():
    c = Canvas(240, 150)
    bx, bw = 15, 210
    for s in range(3):
        sw = bw + 24 - s * 6
        sx = bx - 12 + s * 3
        c.fill(sx, s * 4, sw, 4, M_BASE)
        c.fill(sx, s * 4 + 3, sw, 1, M_LIGHT)
        c.fill(sx, s * 4, sw, 1, M_DARK)

    col_y = 12
    col_h = 72

    c.fill(bx + 16, col_y, bw - 32, col_h, M_SHADE)
    dw, dh = 32, 54
    dx = bx + bw // 2 - dw // 2
    c.fill(dx, col_y, dw, dh, DOOR_DARK)
    c.fill(dx + 3, col_y, dw - 6, dh - 3, DOOR_GLOW)
    c.fill(dx + 6, col_y, dw - 12, dh - 6, SUN_CORE)

    col_count = 6
    col_w = 12
    spacing = (bw - 20 - col_count * col_w) // (col_count - 1)
    for i in range(col_count):
        cx = bx + 10 + i * (col_w + spacing)
        c.fill(cx - 2, col_y, col_w + 4, 5, M_LIGHT)
        c.fill(cx - 2, col_y, col_w + 4, 1, M_DARK)
        c.fill(cx, col_y + 5, col_w, col_h - 10, M_BASE)
        c.fill(cx, col_y + 5, 3, col_h - 10, M_DARK)
        c.fill(cx + 3, col_y + 5, 2, col_h - 10, M_SHADE)
        c.fill(cx + col_w - 4, col_y + 5, 4, col_h - 10, M_LIGHT)
        c.fill(cx + col_w - 2, col_y + 5, 2, col_h - 10, M_WHITE)
        c.fill(cx - 3, col_y + col_h - 5, col_w + 6, 5, M_LIGHT)
        c.fill(cx - 3, col_y + col_h - 5, col_w + 6, 1, M_DARK)

    entab_y = col_y + col_h
    c.fill(bx - 6, entab_y, bw + 12, 16, M_BASE)
    c.fill(bx - 6, entab_y + 14, bw + 12, 2, M_LIGHT)
    c.fill(bx - 6, entab_y, bw + 12, 2, M_DARK)
    for tx in range(bx, bx + bw, 18):
        c.fill(tx, entab_y + 3, 4, 10, M_SHADE)

    ped_y = entab_y + 16
    ped_h = 36
    for ph in range(ped_h):
        pw = (bw + 12) - ph * 6
        if pw <= 2:
            break
        px = (bx + bw // 2) - pw // 2
        c.fill(px, ped_y + ph, pw, 1, M_LIGHT if ph >= ped_h - 2 else M_BASE)
        c.put(px, ped_y + ph, M_DARK)
        c.put(px + pw - 1, ped_y + ph, M_WHITE)

    # Hera densa com brotos bioluminescentes azul-ciano e rosa
    for vx in range(bx + 10, bx + bw - 10, 5):
        vy = entab_y - (vx % 6) * 4
        c.fill(vx, vy, 4, 5, VINE_M)
        c.put(vx + 1, vy + 2, VINE_L)
        if vx % 10 == 0:
            c.put(vx + 2, vy + 3, BIO_CYAN)
        elif vx % 15 == 0:
            c.put(vx + 2, vy + 3, BIO_PINK)

    c.save("west_temple")


def build_east_temple():
    c = Canvas(200, 150)
    bx, bw = 10, 180
    c.fill(bx, 0, bw, 8, M_BASE)
    c.fill(bx, 7, bw, 1, M_LIGHT)

    col_y = 8
    col_h = 72
    col_defs = [(bx + 15, 26, True), (bx + 52, 48, True), (bx + 90, col_h, False), (bx + 128, col_h, False), (bx + 160, col_h, False)]
    for cx, ch, broken in col_defs:
        cw = 12
        c.fill(cx, col_y, cw, ch, M_BASE)
        c.fill(cx, col_y, 3, ch, M_DARK)
        c.fill(cx + cw - 3, col_y, 3, ch, M_WHITE)
        if not broken:
            c.fill(cx - 2, col_y + ch - 4, cw + 4, 4, M_LIGHT)
        else:
            for x in range(cw):
                c.fill(cx + x, col_y + ch, 1, (x * 7) % 5, M_DARK)

    entab_y = col_y + col_h
    start_x = bx + 70
    entab_w = bw - 70
    c.fill(start_x, entab_y, entab_w, 14, M_BASE)
    c.fill(start_x, entab_y + 12, entab_w, 2, M_WHITE)

    ped_y = entab_y + 14
    for ph in range(28):
        pw = entab_w - ph * 5
        if pw <= 2:
            break
        c.fill((start_x + entab_w) - pw, ped_y + ph, pw, 1, M_BASE)

    for i in range(35):
        vx = bx + 20 + (i * 7) % 150
        vy = col_y + (i * 9) % 65
        c.fill(vx, vy, 4, 5, VINE_M)
        c.put(vx + 1, vy + 2, VINE_L)
        if i % 3 == 0:
            c.put(vx + 2, vy + 1, BIO_CYAN)

    c.save("east_temple")


def build_statue():
    c = Canvas(42, 80)
    cx = 21
    ped_w, ped_h = 28, 22
    c.fill(cx - ped_w // 2, 0, ped_w, ped_h, M_BASE)
    c.fill(cx - ped_w // 2 - 2, 0, ped_w + 4, 3, M_DARK)
    c.fill(cx - ped_w // 2 - 2, ped_h - 3, ped_w + 4, 3, M_WHITE)
    c.fill(cx - 8, 8, 16, 2, M_CREVICE)
    c.fill(cx - 6, 12, 12, 2, M_CREVICE)

    sy = ped_h
    c.fill(cx - 5, sy, 11, 24, M_LIGHT)
    c.fill(cx - 5, sy, 3, 24, M_SHADE)
    c.fill(cx + 3, sy, 3, 24, M_WHITE)

    ty = sy + 22
    c.fill(cx - 6, ty, 12, 18, M_LIGHT)
    c.fill(cx - 6, ty, 3, 18, M_SHADE)
    c.fill(cx + 3, ty, 3, 18, M_WHITE)

    hy = ty + 18
    c.fill(cx - 4, hy, 8, 10, M_WHITE)

    c.fill(cx - 9, ty + 6, 4, 14, M_LIGHT)
    c.fill(cx - 7, ty + 18, 5, 4, M_WHITE)
    c.fill(cx + 6, ty + 2, 4, 14, M_LIGHT)

    c.save("statue")


def build_fountain():
    c = Canvas(56, 44)
    cx = 28
    c.fill(cx - 24, 0, 48, 5, M_BASE)
    c.fill(cx - 24, 4, 48, 1, M_WHITE)
    c.fill(cx - 26, 5, 52, 10, M_BASE)
    c.fill(cx - 24, 7, 48, 8, (65, 155, 225))
    c.fill(cx - 28, 13, 56, 3, M_WHITE)

    c.fill(cx - 4, 16, 8, 10, M_BASE)
    c.fill(cx - 14, 26, 28, 6, M_BASE)
    c.fill(cx - 12, 28, 24, 4, (85, 175, 245))
    c.fill(cx - 15, 31, 30, 2, M_WHITE)
    c.save("fountain_basin")

    for frame in range(3):
        w_c = Canvas(56, 44)
        jet_h = 14 + (frame % 2) * 2
        w_c.fill(cx - 2, 32, 4, jet_h, (225, 250, 255))
        for y in range(8, 32 + jet_h):
            arc = int((32 + jet_h - y) * 0.7)
            w_c.put(cx - arc, y, (120, 215, 255))
            w_c.put(cx - arc - 1, y, (225, 250, 255))
            w_c.put(cx + arc, y, (120, 215, 255))
            w_c.put(cx + arc + 1, y, (225, 250, 255))
        w_c.save(f"fountain_water_{frame}")


def build_cypress():
    c = Canvas(36, 104)
    cx = 18
    c.fill(cx - 2, 0, 4, 10, F_TRUNK)
    fol_h = 94
    for y in range(8, 104):
        t = (y - 8) / fol_h
        r = max(1, int(math.sin(t * math.pi * 0.96) * 15))
        # Curvatura espiral lúdica no topo
        twist = int(math.sin(y * 0.15) * 2.5) if y > 70 else 0
        for x in range(cx + twist - r, cx + twist + r + 1):
            nx = (x - (cx + twist - r)) / (2.0 * r)
            if nx > 0.65:
                col = (68, 155, 62)
            elif nx > 0.3:
                col = (38, 108, 48)
            else:
                col = (18, 65, 30)
            if (x + y * 2) % 5 == 0:
                col = (92, 185, 75)
            c.put(x, y, col)
    c.save("cypress")


def build_platforms():
    # 1. Bloco de Runa de Aetherion (16x16)
    c = Canvas(16, 16)
    c.fill(0, 2, 16, 14, M_BASE)
    c.fill(0, 15, 16, 1, M_WHITE)
    c.fill(15, 2, 1, 14, M_WHITE)
    c.fill(0, 2, 16, 1, M_DARK)
    c.fill(0, 2, 1, 14, M_DARK)

    # Selo em losango dourado em alto relevo
    cx, cy, rad = 8, 9, 5
    for dy in range(-rad, rad + 1):
        for dx in range(-rad, rad + 1):
            dist = abs(dx) + abs(dy)
            if dist == rad:
                c.put(cx + dx, cy + dy, RUNE_GOLD)
            elif dist < rad:
                col = RUNE_GOLD_L if dx > 0 or dy > 0 else RUNE_GOLD_D
                if dist == 0:
                    col = SUN_CORE
                c.put(cx + dx, cy + dy, col)

    # Hera alienígena pendente
    c.put(2, 1, VINE_M)
    c.put(2, 0, VINE_D)
    c.put(6, 1, BIO_CYAN)
    c.put(11, 1, VINE_M)
    c.put(12, 0, BIO_PINK)
    c.save("rune_block")

    # 2. Degrau de escada (22x14)
    c_step = Canvas(22, 14)
    c_step.fill(0, 12, 22, 2, GRASS_WIRE)
    c_step.fill(0, 10, 22, 2, GRASS_TOP)
    c_step.fill(0, 0, 22, 10, M_BASE)
    c_step.fill(0, 9, 22, 1, M_WHITE)
    c_step.fill(21, 0, 1, 10, M_WHITE)
    c_step.fill(0, 0, 1, 10, M_DARK)
    c_step.save("stair_step")

    # 3. Laje (54x14)
    c_laje = Canvas(54, 14)
    c_laje.fill(0, 12, 54, 2, GRASS_WIRE)
    c_laje.fill(0, 10, 54, 2, GRASS_TOP)
    c_laje.fill(0, 0, 54, 10, M_BASE)
    c_laje.fill(0, 9, 54, 1, M_WHITE)
    c_laje.fill(53, 0, 1, 10, M_WHITE)
    c_laje.fill(0, 0, 1, 10, M_DARK)
    c_laje.save("laje")

    # 4. Ilha Flutuante Alta (72x30)
    c_ia = Canvas(72, 30)
    cx, sy = 36, 26
    for x in range(3, 69):
        nx = (x - cx) / 34.0
        taper = 1.0 - nx * nx
        bot = int((1.0 - taper) * 22) + ((x * 7) % 3)
        bot = max(1, min(sy - 4, bot))
        for y in range(bot, sy - 3):
            c_ia.put(x, y, SOIL_L if y > sy - 8 else SOIL_D)
    c_ia.fill(0, sy + 2, 72, 2, GRASS_WIRE)
    c_ia.fill(0, sy, 72, 2, GRASS_TOP)
    c_ia.fill(0, sy - 2, 72, 2, GRASS_SHADE)
    # Flores exóticas e cogumelos
    for fx, fcol in ((16, FLOWER_PINK), (24, FLOWER_YELLOW), (44, BIO_CYAN), (56, FLOWER_VIOLET)):
        c_ia.fill(fx, sy + 3, 2, 2, fcol)
    c_ia.save("island_tall")

    # 5. Ilha Flutuante Baixa (56x26)
    c_ib = Canvas(56, 26)
    cx, sy = 28, 22
    for x in range(2, 54):
        nx = (x - cx) / 26.0
        taper = 1.0 - nx * nx
        bot = int((1.0 - taper) * 18) + ((x * 5) % 3)
        bot = max(1, min(sy - 4, bot))
        for y in range(bot, sy - 3):
            c_ib.put(x, y, SOIL_L if y > sy - 7 else SOIL_D)
    c_ib.fill(0, sy + 2, 56, 2, GRASS_WIRE)
    c_ib.fill(0, sy, 56, 2, GRASS_TOP)
    c_ib.fill(0, sy - 2, 56, 2, GRASS_SHADE)
    for fx, fcol in ((14, FLOWER_YELLOW), (38, BIO_PINK)):
        c_ib.fill(fx, sy + 3, 2, 2, fcol)
    c_ib.save("island_low")

    # 6. Sombra Oval de Ilha
    c_sh = Canvas(72, 12)
    for y in range(12):
        for x in range(72):
            nx = (x - 36) / 36.0
            ny = (y - 6) / 6.0
            if nx * nx + ny * ny <= 1.0:
                fact = 1.0 - (nx * nx + ny * ny)
                c_sh.put(x, y, (15, 25, 40, int(fact * 95)))
    c_sh.save("shadow_island")

    # 7. Sombra de Contato do Personagem
    c_cs = Canvas(24, 8)
    for y in range(8):
        for x in range(24):
            nx = (x - 12) / 12.0
            ny = (y - 4) / 4.0
            if nx * nx + ny * ny <= 1.0:
                fact = 1.0 - (nx * nx + ny * ny)
                c_cs.put(x, y, (8, 12, 18, int(fact * 155)))
    c_cs.save("hero_shadow")


def build_props():
    c = Canvas(44, 24)
    cx, cy = 22, 12
    for y in range(24):
        for x in range(44):
            nx = (x - cx) / 20.0
            ny = (y - cy) / 11.0
            if nx * nx + ny * ny <= 1.0:
                c.put(x, y, F_FRONT_L if ny > 0.3 else (F_FRONT_M if ny > -0.3 else F_FRONT_D))
    for fx, fy, fcol in ((12, 14, FLOWER_PINK), (22, 16, FLOWER_YELLOW), (32, 13, BIO_CYAN)):
        c.fill(fx, fy, 2, 2, fcol)
    c.save("bush")

    c_col = Canvas(20, 84)
    c_col.fill(4, 0, 12, 4, M_BASE)
    c_col.fill(4, 3, 12, 1, M_WHITE)
    c_col.fill(6, 4, 8, 74, M_BASE)
    c_col.fill(6, 4, 2, 74, M_DARK)
    c_col.fill(12, 4, 2, 74, M_WHITE)
    c_col.fill(3, 78, 14, 6, M_WHITE)
    c_col.save("column_intact")

    c_bcol = Canvas(20, 52)
    c_bcol.fill(4, 0, 12, 4, M_BASE)
    c_bcol.fill(6, 4, 8, 44, M_BASE)
    c_bcol.fill(6, 4, 2, 44, M_DARK)
    c_bcol.fill(12, 4, 2, 44, M_WHITE)
    c_bcol.save("column_broken")


def main():
    print("Gerando assets alienígenas da Fase I (Aetherion Alien World)...")
    build_sky()
    build_clouds()
    build_mountains()
    build_forest()
    build_colonnade()
    build_pavement()
    build_west_temple()
    build_east_temple()
    build_statue()
    build_fountain()
    build_cypress()
    build_platforms()
    build_props()
    print("Assets alienígenas gerados em Assets/Resources/GoldenAge com sucesso!")


if __name__ == "__main__":
    main()
