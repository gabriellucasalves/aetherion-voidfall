"""Placeholders do fundo híbrido (céu V3 + layout V2) no contrato T1.

ATENÇÃO: estes PNGs são DOWNSCALE da referência de IA, quantizados para a
paleta oficial. NÃO são sprites finais. Servem só para validar composição,
escala e paralaxe no jogo até as pinturas nativas do Aseprite existirem.
As pinturas finais devem ser exportadas com o MESMO nome de arquivo em
Assets/Resources/T1_Hybrid/ (ExportForUnity.lua, modo flat).

Uso (a partir da raiz do repositório):
    python3 Tools/Aseprite/fundo_hibrido_v3_v2/gerar_placeholders_t1_hybrid.py
"""
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[3]
REF = REPO / "docs/fundo_hibrido/REFERENCIA_IA_fundo_fase_hibrido_v3ceu_v2layout.png"
GPL = Path(__file__).with_name("paleta_alien_diurna_64cores.gpl")
OUT = REPO / "Assets/Resources/T1_Hybrid"

# Recortes na referência 1280x720 -> tamanho nativo T1 (1 px = 1/16 unidade).
SKY = dict(size=(480, 192), box=(0, 0, 1280, 512))
FAR = dict(size=(512, 130), box=(0, 230, 1280, 555))
CITY = dict(size=(640, 176), box=(0, 280, 1280, 632))
GROUND = dict(size=(512, 64), box=(0, 622, 768, 718))

WATER_TOP = 556          # abaixo disso o azul da referência é água, não céu
# Topo das montanhas / floresta enevoada (x, y) na referência 1280x720.
MOUNTAIN_LINE = np.array([
    (0, 410), (60, 400), (140, 395), (220, 402), (300, 392), (380, 385), (450, 378),
    (520, 372), (587, 365), (620, 380), (655, 398), (690, 388), (732, 345), (752, 336),
    (824, 332), (845, 352), (862, 370), (912, 398), (960, 395), (1012, 412), (1060, 400),
    (1120, 375), (1170, 362), (1231, 350), (1280, 360),
])
CITADEL_BOX = (740, 280, 830, 340)
HOLE = (555, 440, 742, 606)  # mago + brilho + ilha flutuante: removidos do fundo


def load_palette():
    cols = {}
    for line in GPL.read_text().splitlines():
        p = line.split()
        if len(p) >= 4 and all(x.isdigit() for x in p[:3]):
            cols[p[3]] = tuple(int(x) for x in p[:3])
    assert len(cols) == 58, len(cols)
    return cols


PAL_BY_NAME = load_palette()
ALL = list(PAL_BY_NAME)
# Céu e planos distantes usam só tons frios/atmosféricos (sem os cianos Aether).
SKY_TONES = [
    "Ceu_Azul_Ceruleo", "Ceu_Azul_Vibrante", "Ceu_Ciano_Claro", "Ceu_Aura_Luminosa",
    "Nevoa_Turquesa_Distante", "Nuvem_Luz_Pura", "Nuvem_Creme_Solar", "Nuvem_Ambar_Medio",
    "Nuvem_Sombra_Lavanda", "Nuvem_Sombra_Profunda", "Fronde_Marfim_Luz", "Fronde_Ouro_Palido",
    "Marmore_Luz_Solar", "Marmore_Reflexo_Medio", "Pedra_Ardosia_Luz", "Montanha_Luz_Atmosfera",
    "Meio_Tom_Sombra_Fria",
]
FAR_TONES = [
    "Ceu_Azul_Vibrante", "Ceu_Ciano_Claro", "Ceu_Aura_Luminosa", "Nevoa_Turquesa_Distante",
    "Montanha_Luz_Atmosfera", "Montanha_Media_Distante", "Montanha_Sombra_Fria",
    "Montanha_Profunda_Contorno", "Pedra_Ardosia_Luz", "Pedra_Ardosia_Sombra",
    "Marmore_Reflexo_Medio", "Nuvem_Sombra_Lavanda", "Nuvem_Sombra_Profunda",
    "Meio_Tom_Sombra_Fria", "Aether_Turquesa_Profundo", "Folha_Verde_Sombra",
]


def classify(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    lum = r * 0.3 + g * 0.59 + b * 0.11
    sky = (b - r > 70) & (b > 170) & (b >= g)
    cloud = (lum > 150) & (r >= g - 12) & ~((g > b + 5) & (g > r)) & ((r > 150) | (b - g > 20))
    # Sombra lavanda das nuvens: R≈G com azul acima (montanha tem G bem acima de R).
    cloud |= ~sky & (r >= g - 15) & (b > g + 10) & (lum > 110)
    # Montanha/névoa: azul domina o verde, verde domina o vermelho.
    # Copas turquesa (G≈B) ficam na vegetação.
    mount = ~sky & ~cloud & (b > g + 14) & (g > r + 15)
    veg = ~sky & ~cloud & ~mount
    return sky, cloud, mount, veg


def remove_hole(a):
    """Remove mago/ilha: a água e a margem abaixo da linha d'água são
    espelhadas das bordas limpas; acima dela o buraco fica desconhecido e é
    coberto pela névoa da camada far."""
    x0, y0, x1, y1 = HOLE
    out = a.copy()
    half = (x0 + x1) // 2
    for x in range(x0, x1):
        src = x0 - (x - x0) - 1 if x < half else x1 + (x1 - x) - 1
        out[WATER_TOP:y1, x] = a[WATER_TOP:y1, src]
    unknown = np.zeros(a.shape[:2], bool)
    unknown[y0:WATER_TOP, x0:x1] = True
    return out, unknown


def largest_bottom_component(mask):
    """Mantém só o que se conecta à base (copas ligadas aos troncos)."""
    h, w = mask.shape
    keep = np.zeros_like(mask)
    stack = [(h - 1, x) for x in range(w) if mask[h - 1, x]]
    for y, x in stack:
        keep[y, x] = True
    while stack:
        y, x = stack.pop()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not keep[ny, nx]:
                keep[ny, nx] = True
                stack.append((ny, nx))
    return keep


def box_blur(img, r):
    k = 2 * r + 1
    pad = [(r, r), (r, r)] + [(0, 0)] * (img.ndim - 2)
    p = np.pad(img, pad, mode="edge")
    c = p.cumsum(0).cumsum(1)
    c = np.pad(c, [(1, 0), (1, 0)] + [(0, 0)] * (img.ndim - 2))
    return (c[k:, k:] - c[:-k, k:] - c[k:, :-k] + c[:-k, :-k]) / (k * k)


def inpaint(a, known):
    """Convolução normalizada em várias escalas: névoa suave, sem estrias."""
    out = a.astype(float).copy()
    filled = known.copy()
    for r in (6, 14, 30, 60):
        w = box_blur(filled.astype(float), r)
        acc = box_blur(out * filled[..., None], r)
        ok = ~filled & (w > 0.02)
        out[ok] = acc[ok] / w[ok][:, None]
        filled |= ok
    return out


def to_palette(rgb, names):
    pal = np.array([PAL_BY_NAME[n] for n in names], dtype=np.int32)
    flat = rgb.reshape(-1, 3).astype(np.int32)
    # distância ponderada (luminância pesa mais) para preservar valores
    w = np.array([0.30, 0.59, 0.11]) * 3
    d = (((flat[:, None, :] - pal[None, :, :]) ** 2) * w).sum(-1)
    return pal[d.argmin(1)].reshape(rgb.shape).astype(np.uint8)


def drop_orphans(rgb, alpha):
    """Pixels isolados (nenhum vizinho igual) recebem a cor mais comum ao redor."""
    h, w, _ = rgb.shape
    key = (rgb[..., 0].astype(np.int64) << 16) | (rgb[..., 1].astype(np.int64) << 8) | rgb[..., 2]
    key = np.where(alpha, key, -1)
    out = rgb.copy()
    p = np.pad(key, 1, constant_values=-2)
    for y in range(h):
        for x in range(w):
            if key[y, x] < 0:
                continue
            nb = p[y:y + 3, x:x + 3].ravel()
            nb = np.delete(nb, 4)
            if (nb == key[y, x]).sum() == 0:
                vals, counts = np.unique(nb[nb >= 0], return_counts=True)
                if len(vals):
                    k = vals[counts.argmax()]
                    out[y, x] = ((k >> 16) & 255, (k >> 8) & 255, k & 255)
    return out


def downscale(a, mask, spec):
    x0, y0, x1, y1 = spec["box"]
    img = Image.fromarray(a[y0:y1, x0:x1].astype(np.uint8))
    small = np.array(img.resize(spec["size"], Image.BOX))
    m = Image.fromarray((mask[y0:y1, x0:x1] * 255).astype(np.uint8)).resize(spec["size"], Image.BOX)
    return small, np.array(m) >= 128


def save(name, rgb, alpha, names=ALL):
    rgb = to_palette(rgb, names)
    rgb = drop_orphans(rgb, alpha)
    rgba = np.dstack([rgb, np.where(alpha, 255, 0).astype(np.uint8)])
    rgba[~alpha, :3] = 0
    Image.fromarray(rgba, "RGBA").save(OUT / f"{name}.png")
    print(name, rgba.shape[1], "x", rgba.shape[0], "opacos", int(alpha.sum()))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    a = np.array(Image.open(REF).convert("RGB")).astype(np.int32)
    assert a.shape[:2] == (720, 1280), a.shape
    a, unknown = remove_hole(a)
    sky, cloud, mount, veg = classify(a)
    for m in (sky, cloud, mount, veg):
        m[unknown] = False
    h, w = sky.shape
    yy = np.arange(h)[:, None].repeat(w, 1)

    # --- city: vegetação/ruínas ligadas ao chão + tudo abaixo da linha d'água
    city_m = veg & (yy > 250)
    city_m |= (yy >= WATER_TOP)
    city_m = largest_bottom_component(city_m)

    # --- far: silhueta das montanhas traçada sobre a referência (a cor sozinha
    # confunde montanha azul-clara com céu). Cidadela recortada por pixel.
    far_top = np.round(np.interp(np.arange(w), MOUNTAIN_LINE[:, 0], MOUNTAIN_LINE[:, 1])).astype(int)
    far_m = yy >= far_top[None, :]
    cx0, cy0, cx1, cy1 = CITADEL_BOX
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    stone = ~(sky | (r > 190))
    win = np.zeros_like(far_m)
    win[cy0:cy1, cx0:cx1] = True
    far_m |= win & stone
    near = box_blur(city_m.astype(float), 2) > 0
    far_known = far_m & ~near & ~unknown & ((sky | mount) | (win & stone))
    far_rgb = inpaint(a, far_known).astype(np.int32)
    far_rgb = np.where(far_known[..., None], a, far_rgb)
    # Buraco do mago: névoa texturizada copiada do trecho vizinho à esquerda.
    hx0, hy0, hx1, _ = HOLE
    span = hx1 - hx0
    far_rgb[hy0:WATER_TOP, hx0:hx1] = far_rgb[hy0:WATER_TOP, hx0 - span:hx0]
    far_top = np.where(far_m.any(0), far_m.argmax(0), h)

    # --- sky: tudo acima do primeiro elemento de terra; abaixo, horizonte estendido
    city_top = np.where(city_m.any(0), city_m.argmax(0), h)
    cut = np.minimum(far_top, city_top) - 2
    sky_known = (yy < cut[None, :]) & (sky | cloud)
    # Gradiente vertical do céu limpo, suavizado; abaixo dos recortes ele
    # continua até o tom de névoa do horizonte.
    row_sky = np.zeros((h, 3))
    last = a[0, 0].astype(float)
    for y in range(h):
        s = sky[y] & (y < cut) & ~cloud[y]
        if s.sum() >= 40:
            last = a[y, s].mean(0)
        row_sky[y] = last
    k = 31
    pad = np.pad(row_sky, ((k // 2, k // 2), (0, 0)), mode="edge")
    row_sky = np.array([pad[i:i + k].mean(0) for i in range(h)])
    horizon = np.array(PAL_BY_NAME["Nevoa_Turquesa_Distante"], float)
    for y in range(330, h):
        t = min(1.0, (y - 330) / 140)
        row_sky[y] = row_sky[y] * (1 - t) + horizon * t
    sky_rgb = np.where(sky_known[..., None], a, row_sky[:, None, :].repeat(w, 1)).astype(np.int32)

    # --- ground: grama/terra ligada à base
    gx0, gy0, gx1, gy1 = GROUND["box"]
    ground_m = np.zeros_like(veg)
    ground_m[gy0:gy1] = largest_bottom_component((veg | (yy >= 640))[gy0:gy1])

    s_rgb, _ = downscale(sky_rgb, np.ones_like(sky), SKY)
    save("sky", s_rgb, np.ones(SKY["size"][::-1], bool), SKY_TONES)
    f_rgb, f_a = downscale(far_rgb, far_m, FAR)
    save("far", f_rgb, f_a, FAR_TONES)
    c_rgb, c_a = downscale(a, city_m, CITY)
    save("city", c_rgb, c_a)
    g_rgb, g_a = downscale(a, ground_m, GROUND)
    save("ground", g_rgb, g_a)


if __name__ == "__main__":
    main()
