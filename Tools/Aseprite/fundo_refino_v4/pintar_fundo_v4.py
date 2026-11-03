"""Fundo Refino V4 — pintura nativa 1× (sky, clouds_a..d, far, city).

Desenha pixel a pixel com primitivas de cluster (faixas, troncos em curva,
copas, lóbulos de nuvem, montanhas por vertente). Usa SOMENTE as cores da
paleta paleta_alien_diurna_64cores.gpl e não lê a referência de IA.
Cada arte sai separada nas camadas dos templates (BASE, SHADOW, DETAIL,
HIGHLIGHT, MOSS, FX) para o montar_fundo_v4.lua.

Uso: python3 pintar_fundo_v4.py <pasta_saida_camadas>
"""
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
GPL = HERE.parent / "fundo_hibrido_v3_v2/paleta_alien_diurna_64cores.gpl"
LAYERS = ["BASE", "SHADOW", "DETAIL", "HIGHLIGHT", "MOSS", "FX"]


def load_palette():
    pal = {}
    for line in GPL.read_text().splitlines():
        p = line.split()
        if len(p) >= 4 and all(x.isdigit() for x in p[:3]):
            pal[p[3]] = tuple(int(x) for x in p[:3])
    assert len(pal) == 58, len(pal)
    return pal


PAL = load_palette()
NAMES = list(PAL)
IDX = {n: i for i, n in enumerate(NAMES)}


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.layers = {n: np.full((h, w), -1, np.int16) for n in LAYERS}
        self.yy, self.xx = np.mgrid[0:h, 0:w]

    def occlude(self, mask):
        for arr in self.layers.values():
            arr[mask] = -1

    def put(self, layer, mask, color):
        self.layers[layer][mask] = IDX[color]

    def flat(self):
        out = np.full((self.h, self.w), -1, np.int16)
        for n in LAYERS:
            arr = self.layers[n]
            out = np.where(arr >= 0, arr, out)
        return out

    def opaque(self):
        return self.flat() >= 0


def to_rgba(idx):
    h, w = idx.shape
    img = np.zeros((h, w, 4), np.uint8)
    for i, n in enumerate(NAMES):
        m = idx == i
        if m.any():
            img[m, :3] = PAL[n]
            img[m, 3] = 255
    return Image.fromarray(img)


# ---------------------------------------------------------------- ruído
def value_noise_1d(n, cell, rng, periodic=False):
    k = n // cell + 3
    g = rng.uniform(-1, 1, k)
    if periodic:
        g[n // cell:] = g[:k - n // cell]
    x = np.arange(n) / cell
    i = x.astype(int)
    f = x - i
    f = f * f * (3 - 2 * f)
    return g[i] * (1 - f) + g[i + 1] * f


def fbm_1d(n, rng, cells=(48, 24, 12, 6), periodic=False):
    out = np.zeros(n)
    amp = 1.0
    for c in cells:
        out += amp * value_noise_1d(n, c, rng, periodic)
        amp *= 0.5
    return out / np.abs(out).max()


def noise_2d(h, w, cell, rng):
    gh, gw = h // cell + 3, w // cell + 3
    g = rng.uniform(-1, 1, (gh, gw))
    ys, xs = np.mgrid[0:h, 0:w] / cell
    y0, x0 = ys.astype(int), xs.astype(int)
    fy, fx = ys - y0, xs - x0
    fy, fx = fy * fy * (3 - 2 * fy), fx * fx * (3 - 2 * fx)
    a = g[y0, x0] * (1 - fx) + g[y0, x0 + 1] * fx
    b = g[y0 + 1, x0] * (1 - fx) + g[y0 + 1, x0 + 1] * fx
    return a * (1 - fy) + b * fy


def checker(c):
    return (c.yy + c.xx) % 2 == 0


def orphans_to_neighbors(idx, passes=2):
    """Remove pixels sem nenhum vizinho da mesma cor (clusters mínimos de 2 px)."""
    t = idx.copy()
    h, w = t.shape
    for _ in range(passes):
        p = np.pad(t, 1, constant_values=-2)
        same = np.zeros((h, w), int)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dy or dx:
                    same += p[1 + dy:h + 1 + dy, 1 + dx:w + 1 + dx] == t
        ys, xs = np.where((same == 0) & (t >= 0))
        for y, x in zip(ys, xs):
            nb = p[y:y + 3, x:x + 3].ravel()
            nb = nb[nb >= 0]
            if len(nb):
                t[y, x] = np.bincount(nb).argmax()
    return t


# ================================================================ SKY
SKY_W, SKY_H = 480, 192
SKY_BANDS = [
    ("Nuvem_Creme_Solar", 0),
    ("Nuvem_Ambar_Medio", 30),
    ("Nuvem_Sombra_Lavanda", 64),
    ("Marmore_Reflexo_Medio", 100),
    ("Nevoa_Turquesa_Distante", 124),
    ("Ceu_Aura_Luminosa", 146),
    ("Ceu_Ciano_Claro", 168),
]


def stepped_boundary(y0, amp, rng, w):
    wave = fbm_1d(w, rng, (240, 120, 60, 30), periodic=True) * amp
    raw = np.zeros(w, int)
    x = 0
    while x < w:
        run = int(rng.integers(7, 30))
        seg = slice(x, min(w, x + run))
        raw[seg] = int(round(y0 + wave[seg].mean())) + int(rng.choice([-1, 0, 0, 1]))
        x += run
    raw[-36:] = raw[0]
    res = np.zeros(w, int)
    cur, since = raw[0], 99
    for i, t in enumerate(raw):
        if t != cur and since >= 4:
            cur += 1 if t > cur else -1
            since = 0
        res[i] = cur
        since += 1
    return res


def tongue(arr, base, cx, y, direction, widths, band, only, w, h):
    for t, wd in enumerate(widths):
        yy = y + direction * t
        if not 0 <= yy < h:
            continue
        off = (widths[0] - wd) // 2 + (t % 2)
        for k in range(wd):
            px = (cx - widths[0] // 2 + off + k) % w
            if base[yy, px] == only:
                arr[yy, px] = band


def paint_sky(seed=404):
    rng = np.random.default_rng(seed)
    c = Canvas(SKY_W, SKY_H)
    w, h = SKY_W, SKY_H
    bounds = [stepped_boundary(y0, 2.5 + 0.4 * i, rng, w) for i, (_, y0) in enumerate(SKY_BANDS[1:], 1)]
    base = np.zeros((h, w), int)
    for bi, b in enumerate(bounds, 1):
        for x in range(w):
            base[b[x]:, x] = bi
    for bi, (name, _) in enumerate(SKY_BANDS):
        c.put("BASE", base == bi, name)

    trans = np.full((h, w), -1, int)
    for bi, b in enumerate(bounds, 1):
        up, lo = bi - 1, bi
        x = int(rng.integers(0, 20))
        while x < w - 6:
            w0 = int(rng.integers(14, 52))
            n = int(rng.integers(2, 4))
            tongue(trans, base, x, b[x] - 1, -1, [max(3, int(w0 * 0.6 ** t)) for t in range(n)], lo, up, w, h)
            if rng.random() < 0.6:
                sl = int(rng.integers(5, max(6, int(w0 * 0.55))))
                tongue(trans, base, x + int(rng.integers(-6, 7)), b[x] - n - int(rng.integers(2, 4)),
                       -1, [sl], lo, up, w, h)
            x += w0 + int(rng.integers(6, 26))
        x = int(rng.integers(10, 40))
        while x < w - 6:
            w0 = int(rng.integers(12, 36))
            tongue(trans, base, x, b[x], 1, [w0, max(3, w0 // 2)], up, lo, w, h)
            x += w0 + int(rng.integers(20, 50))
    for bi, (name, _) in enumerate(SKY_BANDS):
        c.put("DETAIL", trans == bi, name)

    # Brilho dourado: véus finos de ouro pálido no creme do topo (sem nuvens).
    gold = np.zeros((h, w), bool)
    for _ in range(9):
        cx, cy = int(rng.integers(0, w)), int(rng.integers(6, 26))
        ln = int(rng.integers(18, 60))
        for t, wd in enumerate([ln, max(4, ln // 2)]):
            for k in range(wd):
                px = (cx - wd // 2 + k + t * 3) % w
                if base[cy - t, px] == 0:
                    gold[cy - t, px] = True
    c.put("HIGHLIGHT", gold, "Fronde_Ouro_Palido")
    return c


# ================================================================ CLOUDS
CLOUD_TONES = ["Nuvem_Sombra_Profunda", "Nuvem_Sombra_Lavanda", "Pedra_Ardosia_Luz",
               "Nuvem_Ambar_Medio", "Nuvem_Creme_Solar", "Nuvem_Luz_Pura"]
LIGHT = np.array([-0.48, -0.78, 0.40])
LIGHT = LIGHT / np.linalg.norm(LIGHT)


def box_blur(a, r):
    k = 2 * r + 1
    p = np.pad(a, r, mode="edge")
    s = p.cumsum(0).cumsum(1)
    s = np.pad(s, ((1, 0), (1, 0)))
    return (s[k:, k:] - s[:-k, k:] - s[k:, :-k] + s[:-k, :-k]) / (k * k)


def cloud_seeds(w, base, rng, rmax, lobes):
    """Banco horizontal: envelope em sino assimétrico, lóbulos muito sobrepostos
    (passo ~0,6 r), com uma segunda fileira menor sobre a massa principal."""
    peak = rng.uniform(0.3, 0.45)
    seeds = []
    x = 8.0
    while x < w - 8:
        t = x / w
        bell = math.exp(-((t - peak) / (0.28 if t < peak else 0.36)) ** 2)
        r = max(3.0, rmax * (0.35 + 0.65 * bell) * rng.uniform(0.8, 1.15))
        seeds.append((x, base - r * 0.7 + rng.uniform(-1.5, 1.5), r))
        x += r * rng.uniform(0.42, 0.6)
    tops = []
    for x, y, r in seeds:
        if r > rmax * 0.6 and rng.random() < 0.7:
            r2 = r * rng.uniform(0.5, 0.72)
            tops.append((x + rng.uniform(-r * 0.5, r * 0.3), y - r * rng.uniform(0.45, 0.75), r2))
    return seeds + tops


def render_cloud(w, h, seeds, base_y, seed, tails=()):
    rng = np.random.default_rng(seed)
    puffs = [dict(x=x, y=y, r=r, lvl=0) for x, y, r in seeds]
    for p in list(puffs):
        for _ in range(3 if p["r"] > 7 else 1):
            ang = -math.pi / 2 + rng.uniform(-1.2, 1.2)
            r = rng.uniform(2.2, max(2.6, p["r"] * 0.45))
            d = p["r"] - r * rng.uniform(0.2, 0.5)
            puffs.append(dict(x=p["x"] + math.cos(ang) * d, y=p["y"] + math.sin(ang) * d, r=r, lvl=1))
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    wx = xx + 0.5 * noise_2d(h, w, 5, rng)
    wy = yy + 0.35 * noise_2d(h, w, 5, rng)
    H = np.full((h, w), -1e9)
    NX, NY, NZ = np.zeros((h, w)), np.zeros((h, w)), np.zeros((h, w))
    OWN, ZO = np.full((h, w), -1), np.zeros((h, w))
    ymin = min(p["y"] for p in puffs)
    for i, p in enumerate(puffs):
        z = 0.6 * (p["y"] - ymin) + 0.6 * p["lvl"]
        dx, dy = wx - p["x"], (wy - p["y"]) / 0.85
        d2 = (dx * dx + dy * dy) / p["r"] ** 2
        zc = np.sqrt(np.clip(1 - d2, 0, 1)) * p["r"] + z
        win = (d2 < 1) & (zc > H)
        H[win] = zc[win]
        NX[win], NY[win] = (dx / p["r"])[win], (dy / p["r"])[win]
        NZ[win] = np.sqrt(np.clip(1 - d2, 0, 1))[win]
        OWN[win], ZO[win] = i, z
    mask = H > -1e8
    mask[base_y + 1:] = False
    above = np.maximum.accumulate(mask, 0)
    left = np.maximum.accumulate(mask, 1)
    right = np.maximum.accumulate(mask[:, ::-1], 1)[:, ::-1]
    cave = above & left & right & ~mask
    cave[base_y + 1:] = False
    NX[cave], NY[cave], NZ[cave] = 0, 0.45, 0.89
    mask |= cave
    for cx, ln in tails:
        for t, wd in enumerate([ln, int(ln * 0.6)]):
            x0 = int(cx - wd / 2)
            row = base_y - t
            seg = slice(max(0, x0), min(w, x0 + wd))
            newm = ~mask[row, seg]
            mask[row, seg] = True
            NY[row, seg] = np.where(newm, 0.9, NY[row, seg])
            NZ[row, seg] = np.where(newm, 0.4, NZ[row, seg])
    Hm = np.where(mask, H, 0.0)
    ao = np.clip((box_blur(Hm, 3) - Hm) / 5, 0, 1) * mask
    v = np.clip((yy - yy[mask].min()) / max(1, base_y - yy[mask].min()), 0, 1)
    lam = np.clip(NX * LIGHT[0] + NY * LIGHT[1] + NZ * LIGHT[2], 0, 1)
    lam = (lam + 0.25) / 1.25
    val = 0.70 * lam + 0.22 * (1 - v) ** 1.3 + 0.10 - 0.2 * ao
    belly = mask & (yy >= base_y - 3 + np.round(noise_2d(h, w, 7, rng) * 1.2))
    val = np.where(belly, np.minimum(val, 0.36 - 0.1 * (yy - base_y + 3) / 3), val)
    tone = np.full((h, w), -1, int)
    tone[mask] = 3
    tone[mask & (val > 0.52)] = 4
    tone[mask & (val > 0.80)] = 5
    tone[mask & (val <= 0.40)] = 2
    tone[mask & (val <= 0.30)] = 1
    tone[mask & (val <= 0.20)] = 0
    # Topo de cada lóbulo da frente sobre o de trás: crista clara.
    up_own = np.vstack([np.full((1, w), -1), OWN[:-1]])
    up_z = np.vstack([np.zeros((1, w)), ZO[:-1]])
    crest = mask & (OWN >= 0) & (up_own >= 0) & (up_own != OWN) & (ZO > up_z + 2) & (lam > 0.6) & ~belly
    tone[crest & (tone == 4)] = 5
    tone[crest & (tone == 3)] = 4
    p = np.pad(mask, 1)
    edge_top = mask & ~p[:-2, 1:-1]
    tone[edge_top & (tone == 4) & (NX < 0.2)] = 5
    tone = orphans_to_neighbors(tone)
    tone[~mask] = -1
    gold = edge_top & (tone == 5) & (NX < -0.3)
    gi = np.argwhere(gold)
    pick = gi[rng.choice(len(gi), size=min(len(gi), 5), replace=False)] if len(gi) else []
    return tone, pick


def cloud_canvas(spec, seed):
    w, h = spec["size"]
    rng = np.random.default_rng(seed)
    seeds = cloud_seeds(w, spec["base"], rng, spec["rmax"], spec["lobes"])
    tone, gold = render_cloud(w, h, seeds, spec["base"], seed, spec.get("tails", ()))
    c = Canvas(w, h)
    c.put("BASE", tone >= 0, CLOUD_TONES[3])
    for t in (0, 1, 2):
        c.put("SHADOW", tone == t, CLOUD_TONES[t])
    c.put("HIGHLIGHT", tone == 4, CLOUD_TONES[4])
    c.put("HIGHLIGHT", tone == 5, CLOUD_TONES[5])
    fx = np.zeros((h, w), bool)
    for y, x in gold:
        fx[y, x] = True
        if x + 1 < w and tone[y, x + 1] >= 4:
            fx[y, x + 1] = True
    c.put("FX", fx, "Luz_Ouro_Runico")
    return c


# ---------------------------------------------------------------- V4.1
# Rampa de volume: creme no topo → pêssego no meio → sombra inferior lavanda.
CLOUD_TONES_41 = ["Meio_Tom_Sombra_Fria", "Nuvem_Sombra_Profunda", "Nuvem_Sombra_Lavanda",
                  "Nuvem_Ambar_Medio", "Nuvem_Creme_Solar", "Nuvem_Luz_Pura"]
CLOUD_THRESH_41 = [0.10, 0.30, 0.45, 0.62, 0.82]   # limites entre os tons 0..5
DITHER_BAND = 0.015

# Lóbulos dirigidos (x, y, r) — silhuetas distintas para cada nuvem:
# a = torre empilhada à esquerda com cauda longa; b = duas corcovas pesadas à
# direita com cauda para a esquerda; c = faixa baixa e fina; d = tufo curto.
CLOUDS = {
    "clouds_a": dict(size=(176, 60), base=54, seeds=[
        (26, 44, 10), (42, 38, 14), (60, 32, 16), (80, 36, 14), (98, 42, 11), (115, 46, 9),
        (131, 48, 7), (145, 50, 5.5), (157, 51, 4), (44, 22, 10), (62, 15, 12), (79, 22, 9),
        (54, 9, 6)], tails=[]),
    "clouds_b": dict(size=(200, 72), base=66, seeds=[
        (24, 60, 5), (38, 57, 7), (54, 52, 10), (74, 46, 13), (96, 42, 15), (118, 46, 14),
        (140, 40, 17), (162, 45, 14), (182, 53, 9), (94, 26, 11), (114, 26, 9), (142, 20, 13),
        (160, 28, 9), (130, 12, 7), (150, 8, 6)], tails=[]),
    "clouds_c": dict(size=(88, 26), base=22, seeds=[
        (8, 20, 3.5), (18, 18, 5), (30, 15, 7.5), (42, 17, 5.5), (52, 18, 4.5), (63, 18, 5.5),
        (75, 20, 3.5), (28, 8, 5), (37, 10, 3.5), (61, 11, 3)], tails=[]),
    "clouds_d": dict(size=(56, 20), base=17, seeds=[
        (11, 14, 4), (21, 11, 6), (33, 10, 6.5), (44, 13, 4.5), (27, 5, 4)], tails=[]),
}


def render_cloud_41(w, h, seeds, base_y, seed, tails=()):
    """Volume empilhado: normais por lóbulo (lóbulos baixos à frente), ventre
    lavanda largo e dither em xadrez só numa faixa estreita entre tons."""
    rng = np.random.default_rng(seed)
    puffs = [dict(x=x, y=y, r=r, lvl=0) for x, y, r in seeds]
    for p in list(puffs):
        for _ in range(3 if p["r"] > 8 else 1):
            ang = -math.pi / 2 + rng.uniform(-1.25, 1.1)
            r = rng.uniform(2.0, max(2.4, p["r"] * 0.42))
            d = p["r"] - r * rng.uniform(0.2, 0.45)
            puffs.append(dict(x=p["x"] + math.cos(ang) * d, y=p["y"] + math.sin(ang) * d, r=r, lvl=1))
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    wx = xx + 0.45 * noise_2d(h, w, 5, rng)
    wy = yy + 0.3 * noise_2d(h, w, 5, rng)
    H = np.full((h, w), -1e9)
    NX, NY, NZ = np.zeros((h, w)), np.zeros((h, w)), np.zeros((h, w))
    OWN, ZO = np.full((h, w), -1), np.zeros((h, w))
    ymin = min(p["y"] for p in puffs)
    for i, p in enumerate(puffs):
        z = 0.6 * (p["y"] - ymin) + 0.6 * p["lvl"]
        dx, dy = wx - p["x"], (wy - p["y"]) / 0.9
        d2 = (dx * dx + dy * dy) / p["r"] ** 2
        zc = np.sqrt(np.clip(1 - d2, 0, 1)) * p["r"] + z
        win = (d2 < 1) & (zc > H)
        H[win] = zc[win]
        NX[win], NY[win] = (dx / p["r"])[win], (dy / p["r"])[win]
        NZ[win] = np.sqrt(np.clip(1 - d2, 0, 1))[win]
        OWN[win], ZO[win] = i, z
    mask = H > -1e8
    mask[base_y + 1:] = False
    above = np.maximum.accumulate(mask, 0)
    left = np.maximum.accumulate(mask, 1)
    right = np.maximum.accumulate(mask[:, ::-1], 1)[:, ::-1]
    cave = above & left & right & ~mask
    cave[base_y + 1:] = False
    NX[cave], NY[cave], NZ[cave] = 0, 0.5, 0.86
    mask |= cave
    for cx, ln in tails:
        for t, wd in enumerate([int(ln * 0.85), int(ln * 0.6), int(ln * 0.35)]):
            x0 = int(cx - wd / 2 + rng.integers(-4, 5))
            row = base_y - t
            seg = slice(max(0, x0), min(w, x0 + wd))
            newm = ~mask[row, seg]
            mask[row, seg] = True
            NY[row, seg] = np.where(newm, 0.85, NY[row, seg])
            NZ[row, seg] = np.where(newm, 0.5, NZ[row, seg])
    Hm = np.where(mask, H, 0.0)
    ao = np.clip((box_blur(Hm, 3) - Hm) / 5, 0, 1) * mask
    top = yy[mask].min()
    v = np.clip((yy - top) / max(1, base_y - top), 0, 1)
    lam = np.clip(NX * LIGHT[0] + NY * LIGHT[1] + NZ * LIGHT[2], 0, 1)
    lam = (lam + 0.2) / 1.2
    val = 0.36 * lam + 0.56 * (1 - v) ** 1.1 + 0.10 - 0.18 * ao
    # Ventre: faixa lavanda de espessura variável (bandas não repetitivas).
    band = 5 + np.round(noise_2d(h, w, 9, rng) * 2.5 + 1)
    belly = mask & (yy >= base_y - band)
    val = np.where(belly, np.minimum(val, 0.42 - 0.10 * np.clip((yy - base_y + band) / band, 0, 1)), val)
    tone = np.zeros((h, w), int)
    for t, th in enumerate(CLOUD_THRESH_41, 1):
        tone[val > th] = t
    # Dither de transição: só na faixa estreita em volta de cada limite.
    dith = np.zeros((h, w), bool)
    chk = (yy.astype(int) + xx.astype(int)) % 2 == 0
    for t, th in enumerate(CLOUD_THRESH_41, 1):
        near = mask & (np.abs(val - th) < DITHER_BAND) & ~belly
        lower = near & (tone == t) & chk
        tone[lower] = t - 1
        dith |= lower
    up_own = np.vstack([np.full((1, w), -1), OWN[:-1]])
    up_z = np.vstack([np.zeros((1, w)), ZO[:-1]])
    crest = mask & (OWN >= 0) & (up_own >= 0) & (up_own != OWN) & (ZO > up_z + 2) & (lam > 0.62) & ~belly
    tone[crest & (tone == 4)] = 5
    tone[crest & (tone == 3)] = 4
    p = np.pad(mask, 1)
    edge_top = mask & ~p[:-2, 1:-1]
    tone[edge_top & (tone >= 3) & (NX < 0.25)] = 5
    tone = np.where(mask, tone, -1)
    tone = orphans_to_neighbors(tone)
    dith &= tone >= 0
    gold = edge_top & (tone == 5) & (NX < -0.35)
    gi = np.argwhere(gold)
    pick = gi[rng.choice(len(gi), size=min(len(gi), 4), replace=False)] if len(gi) else []
    return tone, pick, dith


def cloud_canvas_41(spec, seed):
    w, h = spec["size"]
    tone, gold, dith = render_cloud_41(w, h, spec["seeds"], spec["base"], seed, spec.get("tails", ()))
    c = Canvas(w, h)
    c.put("BASE", tone >= 0, CLOUD_TONES_41[3])
    for t in (0, 1, 2):
        c.put("SHADOW", (tone == t) & ~dith, CLOUD_TONES_41[t])
    c.put("HIGHLIGHT", (tone == 4) & ~dith, CLOUD_TONES_41[4])
    c.put("HIGHLIGHT", (tone == 5) & ~dith, CLOUD_TONES_41[5])
    for t in range(6):
        c.put("DETAIL", dith & (tone == t), CLOUD_TONES_41[t])
    fx = np.zeros((h, w), bool)
    for y, x in gold:
        fx[y, x] = True
    c.put("FX", fx, "Fronde_Ouro_Palido")
    return c


# ================================================================ FAR
FAR_W, FAR_H = 512, 130


def ridge_profile(w, peaks, base, rng, rough):
    x = np.arange(w)
    prof = np.full(w, float(base))
    owner = np.full(w, -1)
    for i, (px, ph, pw, sharp) in enumerate(peaks):
        t = np.clip(1 - np.abs(x - px) / pw, 0, 1) ** sharp
        y = base - ph * t
        better = y < prof
        prof[better] = y[better]
        owner[better] = i
    prof += fbm_1d(w, rng, (32, 16, 8, 4)) * rough * np.clip((base - prof) / 20, 0.3, 1)
    return np.round(prof).astype(int), owner


def paint_range(c, peaks, base, rng, rough, lit, shade, cap=None, gully=None, haze=None):
    w, h = c.w, c.h
    prof, owner = ridge_profile(w, peaks, base, rng, rough)
    m = c.yy >= prof[None, :]
    c.occlude(m)
    c.put("BASE", m, lit)
    jag = fbm_1d(h, rng, (16, 8, 4)) * 3
    shade_m = np.zeros_like(m)
    dither = np.zeros_like(m)
    for i, (px, ph, pw, _) in enumerate(peaks):
        top = base - ph
        # Divisor de vertente: desce do pico para a direita (luz da esquerda).
        div = px + (c.yy - top) * 0.35 + jag[np.clip(c.yy, 0, h - 1)]
        own = (owner[None, :] == i) & m
        shade_m |= own & (c.xx > div + 1)
        dither |= own & (np.abs(c.xx - div) <= 1.5)
        if gully is not None:
            # Espinhaços secundários no flanco iluminado.
            for k in range(1, 4):
                g = px - k * pw * 0.22 + (c.yy - top) * 0.55
                gm = own & (np.abs(c.xx - g) < 0.9) & (c.yy > top + 6 + k * 4) & (c.xx < div)
                c.put("DETAIL", gm, gully)
    c.put("SHADOW", shade_m, shade)
    c.put("SHADOW", dither & checker(c), shade)
    if cap is not None:
        near_peak = np.zeros_like(m)
        for (px, ph, pw, _) in peaks:
            near_peak |= (np.abs(c.xx - px) < pw * 0.22) & (c.yy < base - ph * 0.72)
        capm = m & (c.yy <= prof[None, :] + 1) & ~shade_m & near_peak
        c.put("HIGHLIGHT", capm, cap)
    if haze is not None:
        # Base dissolvendo na névoa com dither ordenado (2 faixas).
        hz0 = base - 10
        c.put("DETAIL", m & (c.yy >= hz0) & (c.yy < hz0 + 5) & checker(c), haze)
        c.put("DETAIL", m & (c.yy >= hz0 + 5), haze)
    return prof


def paint_citadel(c, cx, top, rng):
    """Cidade flutuante pálida, rim light dourado discreto à esquerda."""
    w = 58
    island_top = top + 30
    x0 = cx - w // 2
    xs = np.arange(c.w)
    # Ilha: topo quase plano, ventre em cunha irregular com pingentes.
    depth = np.clip(1 - np.abs(xs - cx) / (w / 2), 0, 1) ** 0.7 * 16
    depth += np.where(depth > 0, fbm_1d(c.w, rng, (8, 4, 2)) * 2.5, 0)
    isl = (c.yy >= island_top) & (c.yy <= island_top + depth[None, :]) & (np.abs(c.xx - cx) <= w / 2)
    c.occlude(isl)
    c.put("BASE", isl, "Pedra_Ardosia_Luz")
    c.put("SHADOW", isl & (c.yy > island_top + 3) & (c.xx > cx - 6), "Nuvem_Sombra_Profunda")
    c.put("SHADOW", isl & (c.yy > island_top + 9), "Meio_Tom_Sombra_Fria")
    c.put("MOSS", isl & (c.yy <= island_top + 1), "Montanha_Luz_Atmosfera")
    # Torres: blocos pálidos com coruchéus.
    towers = [(-22, 10, 5), (-14, 18, 6), (-5, 26, 7), (4, 20, 6), (12, 14, 5), (20, 9, 4), (-9, 12, 4), (9, 30, 4)]
    city = np.zeros_like(isl)
    for dx, ht, tw in towers:
        tx = cx + dx
        body = (c.xx >= tx - tw // 2) & (c.xx < tx - tw // 2 + tw) & (c.yy < island_top) & (c.yy >= island_top - ht)
        spire = (c.yy < island_top - ht) & (c.yy >= island_top - ht - (tw // 2 + 2)) & \
                (np.abs(c.xx - (tx - 0.5)) <= (c.yy - (island_top - ht - tw // 2 - 2)) * 0.5)
        city |= body | spire
    wall = (c.yy < island_top) & (c.yy >= island_top - 5) & (np.abs(c.xx - cx) <= w / 2 - 4)
    city |= wall
    c.occlude(city)
    c.put("BASE", city, "Marmore_Reflexo_Medio")
    p = np.pad(city, 1)
    right_edge = city & ~p[1:-1, 2:]
    left_edge = city & ~p[1:-1, :-2]
    top_edge = city & ~p[:-2, 1:-1]
    c.put("SHADOW", city & (c.xx > cx + 2) & ~left_edge, "Pedra_Ardosia_Luz")
    c.put("SHADOW", right_edge, "Pedra_Ardosia_Luz")
    win = city & (c.yy % 4 == 1) & (c.xx % 3 == 0) & ~top_edge & ~left_edge & ~right_edge & (c.yy < island_top - 6)
    c.put("DETAIL", win, "Nuvem_Sombra_Profunda")
    c.put("HIGHLIGHT", (left_edge | top_edge) & (c.xx < cx + 4), "Fronde_Ouro_Palido")
    c.put("HIGHLIGHT", top_edge & (c.xx >= cx + 4), "Marmore_Luz_Solar")


def paint_treeline(c, base, rng, lit, mid, shade, deep, rmin, rmax):
    """Duas fileiras de copas arredondadas (fundo mais escuro), copas com
    tufos de 2–3 px no topo iluminado; corpo contínuo até a base."""
    w, h = c.w, c.h
    total = np.zeros((h, w), bool)
    for row, (dy, rs, body, top_lit) in enumerate(((-6, 0.8, shade, mid), (0, 1.0, mid, lit))):
        m = np.zeros((h, w), bool)
        lit_m = np.zeros_like(m)
        x = -4 + row * rmin
        while x < w + 4:
            r = rng.uniform(rmin, rmax) * rs
            cy = base + dy - r * rng.uniform(0.2, 0.8)
            bump = noise_2d(h, w, 3, rng) * 0.18
            d2 = ((c.xx - x) ** 2 + ((c.yy - cy) / 0.9) ** 2) / r ** 2 + bump
            crown = (d2 < 1) | ((c.yy >= cy) & (np.abs(c.xx - x) < r * 0.9))
            m |= crown
            lit_m |= (d2 < 0.75) & ((c.xx - x) * 0.7 + (c.yy - cy) < -r * 0.2) & (bump < 0.05)
            x += r * rng.uniform(1.1, 1.7)
        m &= c.yy <= h
        c.occlude(m)
        c.put("BASE", m, body)
        c.put("HIGHLIGHT", lit_m & m, top_lit)
        c.put("SHADOW", m & (c.yy > base + 4), deep)
        total |= m
    return total


def paint_far(seed=909):
    rng = np.random.default_rng(seed)
    c = Canvas(FAR_W, FAR_H)
    # R1: cordilheira distante, pico central mais suave (sem vulcão).
    paint_range(c, [(70, 44, 90, 1.2), (190, 52, 110, 1.4), (330, 62, 150, 1.6), (450, 46, 100, 1.3)],
                108, rng, 3.0, "Nevoa_Turquesa_Distante", "Nuvem_Sombra_Lavanda",
                cap="Marmore_Luz_Solar")
    paint_citadel(c, 272, 14, rng)
    # R2: montanhas médias ciano-lavanda com dither nas vertentes.
    paint_range(c, [(30, 34, 70, 1.0), (140, 40, 85, 1.1), (250, 26, 70, 1.0), (400, 44, 95, 1.2),
                    (500, 32, 60, 1.0)],
                122, rng, 4.0, "Montanha_Luz_Atmosfera", "Nuvem_Sombra_Profunda",
                gully="Ceu_Aura_Luminosa", haze="Montanha_Media_Distante")
    # R3: linha de mata distante (fecha a base, coberta pela city).
    paint_treeline(c, 118, rng, "Montanha_Luz_Atmosfera", "Montanha_Media_Distante",
                   "Montanha_Sombra_Fria", "Montanha_Profunda_Contorno", 5, 10)
    return c


# ================================================================ CITY
CITY_W, CITY_H = 640, 176


def spline(pts, n=240):
    pts = np.array(pts, float)
    p = np.vstack([pts[0], pts, pts[-1]])
    out = []
    for i in range(1, len(p) - 2):
        p0, p1, p2, p3 = p[i - 1], p[i], p[i + 1], p[i + 2]
        for t in np.linspace(0, 1, n // (len(pts) - 1), endpoint=False):
            t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(p[-2])
    return np.array(out)


def paint_limb(c, pts, w0, w1, rng, twist=0.11, flare=0.0):
    """Tronco/galho torcido: luz à esquerda, sulcos em espiral."""
    s = spline(pts)
    n = len(s)
    tang = np.gradient(s, axis=0)
    tang /= np.linalg.norm(tang, axis=1, keepdims=True) + 1e-9
    widths = np.linspace(w0, w1, n)
    if flare:
        widths += flare * np.clip(1 - np.arange(n) / (n * 0.15), 0, 1) ** 2
    x0, x1 = int(s[:, 0].min() - w0 - flare - 2), int(s[:, 0].max() + w0 + flare + 2)
    y0, y1 = int(s[:, 1].min() - w0 - 2), int(s[:, 1].max() + w0 + 2)
    x0, y0 = max(0, x0), max(0, y0)
    x1, y1 = min(c.w, x1), min(c.h, y1)
    yy, xx = np.mgrid[y0:y1, x0:x1]
    pts_px = np.stack([xx.ravel(), yy.ravel()], 1).astype(float)
    d = ((pts_px[:, None, :] - s[None, :, :]) ** 2).sum(-1)
    j = d.argmin(1)
    dist = np.sqrt(d[np.arange(len(j)), j])
    wd = widths[j]
    inside = dist <= wd / 2
    nrm = np.stack([-tang[j, 1], tang[j, 0]], 1)
    nrm *= np.where(nrm[:, :1] > 0, -1, 1)          # normal apontando para a esquerda
    lat = ((pts_px - s[j]) * nrm).sum(1) / (wd / 2 + 1e-9)
    full = np.zeros((c.h, c.w), bool)
    L = np.full((c.h, c.w), 9.0)
    S = np.zeros((c.h, c.w))
    full[y0:y1, x0:x1] = inside.reshape(yy.shape)
    L[y0:y1, x0:x1] = lat.reshape(yy.shape)
    S[y0:y1, x0:x1] = (j / n).reshape(yy.shape)
    c.occlude(full)
    c.put("BASE", full, "Casca_Madeira_Media")
    c.put("HIGHLIGHT", full & (L > 0.58), "Casca_Madeira_Luz")
    c.put("SHADOW", full & (L < -0.05), "Casca_Tronco_Sombra")
    c.put("SHADOW", full & (L < -0.72), "Casca_Fenda_Profunda")
    groove = full & (np.abs(((S * n * twist + L * 0.9) % 1.0) - 0.5) < 0.09) & (np.abs(L) < 0.85)
    c.put("DETAIL", groove & (L > 0.2), "Casca_Tronco_Sombra")
    c.put("DETAIL", groove & (L <= 0.2), "Casca_Fenda_Profunda")
    return full


def paint_pad(c, cx, cy, rx, ry, rng, ramp, fringe=True):
    """Copa em guarda-chuva: platô com topo recortado em tufos."""
    lit, mid, shade, deep = ramp
    dx = (c.xx - cx) / rx
    inside_x = np.abs(dx) < 1
    dome = np.sqrt(np.clip(1 - dx ** 2, 0, 1)) ** 0.55
    scallop = np.abs(np.sin((c.xx - cx) * math.pi / rng.uniform(5.5, 7.5))) * 2.2
    top = cy - ry * dome - scallop * dome + noise_2d(c.h, c.w, 6, rng) * 1.2
    bot = cy + 2 + 2.5 * np.sqrt(np.clip(1 - dx ** 2, 0, 1))
    if fringe:
        bot = bot + (noise_2d(c.h, c.w, 2, rng) > 0.35) * 2
    m = inside_x & (c.yy >= top) & (c.yy <= bot)
    c.occlude(m)
    c.put("BASE", m, mid)
    rel = (c.yy - top) / np.maximum(1, bot - top)
    tufts = noise_2d(c.h, c.w, 3, rng)
    # Tufos verdes no terço superior: o teal da copa puxa para verde-água.
    c.put("MOSS", m & (rel < 0.6) & (tufts > 0.1) & (tufts < 0.5), "Folha_Jade_Medio")
    c.put("HIGHLIGHT", m & ((rel < 0.22) | ((rel < 0.45) & (tufts > 0.5))) & (dx < 0.55), lit)
    c.put("SHADOW", m & ((rel > 0.66) | ((dx > 0.6) & (rel > 0.35))), shade)
    c.put("SHADOW", m & (rel > 0.88), deep)
    p = np.pad(m, 1)
    c.put("HIGHLIGHT", m & ~p[:-2, 1:-1] & (dx < 0.2), lit)
    return m


def paint_drooping_clump(c, x, y, rx, ln, rng, ramp, depth):
    """Cacho pendente: ombro arredondado em cima, corpo que afina até a ponta,
    hachura vertical de folhas. depth 0 = fundo (escuro) … 2 = frente (claro)."""
    lit, mid, shade, deep = ramp
    t = (c.yy - y) / ln
    half = rx * np.where(t < 0, np.sqrt(np.clip(1 - (t * ln / (rx * 0.9)) ** 2, 0, 1)),
                         np.clip(1 - t, 0, 1) ** 0.75)
    wob = np.sin((c.yy - y) * 0.45 + x) * 0.8
    dxp = c.xx - x - wob
    m = (np.abs(dxp) <= half) & (c.yy >= y - rx * 0.9) & (t <= 1)
    if not m.any():
        return m
    c.occlude(m)
    body = [shade, mid, mid][depth]
    c.put("BASE", m, body)
    side = dxp / np.maximum(half, 1)
    hatch = (c.xx + (c.yy // 5)) % 3 == 0
    if depth >= 1:
        c.put("HIGHLIGHT", m & (side < (-0.45 if depth == 1 else -0.1)) & ~hatch & (t < 0.75), lit)
        c.put("SHADOW", m & (side > 0.45), deep if depth == 1 else shade)
    c.put("DETAIL", m & hatch & (np.abs(side) < 0.8) & (t > -0.2), [deep, shade, shade][depth])
    p = np.pad(m, 1)
    if depth == 2:
        c.put("HIGHLIGHT", m & ~p[:-2, 1:-1] & (side < 0.3), lit)
    return m


def paint_weeping(c, cx, base_y, rng):
    """Weeping Hero: tronco largo em S, coroa de cachos lavanda pendentes."""
    ramp = ("Nuvem_Sombra_Lavanda", "Nuvem_Sombra_Profunda", "Meio_Tom_Sombra_Fria",
            "Sombra_Ambiente_Lavanda")
    paint_limb(c, [(cx + 4, base_y), (cx - 8, base_y - 26), (cx + 6, base_y - 56), (cx - 2, base_y - 84)],
               24, 12, rng, twist=0.08, flare=10)
    for pts, a, b in [
        ([(cx + 6, base_y - 5), (cx + 20, base_y - 1), (cx + 30, base_y + 4)], 10, 4),
        ([(cx - 6, base_y - 4), (cx - 18, base_y), (cx - 26, base_y + 5)], 10, 4),
        ([(cx + 2, base_y - 2), (cx + 10, base_y + 3)], 8, 3),
        ([(cx - 2, base_y - 70), (cx - 30, base_y - 90), (cx - 58, base_y - 96)], 8, 3),
        ([(cx + 2, base_y - 76), (cx + 32, base_y - 96), (cx + 64, base_y - 100)], 8, 3),
    ]:
        paint_limb(c, pts, a, b, rng, twist=0.12)
    folia = np.zeros((c.h, c.w), bool)
    crown_top = base_y - 146
    span = 96
    # Três passadas (fundo → frente): cachos mais claros e mais baixos na frente.
    for depth, count, drop in ((0, 16, 4), (1, 30, 6), (2, 30, 10)):
        for _ in range(count):
            u = rng.uniform(-1, 1)
            x = cx + u * span * (0.85 + 0.15 * depth / 2)
            arch = crown_top + 10 + (u ** 2) * 46 + drop + rng.uniform(-6, 6)
            rx = rng.uniform(6, 10) - depth * 0.8
            ln = rng.uniform(26, 52) * (1.0 + 0.45 * abs(u)) - depth * 4
            folia |= paint_drooping_clump(c, x, arch, rx, ln, rng, ramp, depth)
    orbs = [(cx - 62, base_y - 82), (cx - 32, base_y - 106), (cx + 4, base_y - 124), (cx + 40, base_y - 100),
            (cx + 72, base_y - 74), (cx - 12, base_y - 72), (cx + 24, base_y - 64), (cx + 52, base_y - 126),
            (cx - 80, base_y - 108)]
    fx = np.zeros_like(folia)
    halo = np.zeros_like(folia)
    for ox, oy in orbs:
        fx |= (np.abs(c.xx - ox) <= 1) & (np.abs(c.yy - oy) <= 1) & \
            ~((np.abs(c.xx - ox) == 1) & (np.abs(c.yy - oy) == 1))
        halo |= ((np.abs(c.xx - ox) == 2) & (c.yy == oy)) | ((np.abs(c.yy - oy) == 2) & (c.xx == ox))
    c.put("FX", halo & folia, "Fronde_Ouro_Palido")
    c.put("FX", fx & folia, "Luz_Ouro_Runico")
    return folia


def paint_parasol(c, cx, base_y, height, pads, rng, lean=0):
    ramp = ("Montanha_Luz_Atmosfera", "Aether_Turquesa_Profundo", "Montanha_Sombra_Fria",
            "Montanha_Profunda_Contorno")
    top = base_y - height
    paint_limb(c, [(cx, base_y), (cx + lean * 0.3 - 4, base_y - height * 0.35),
                   (cx + lean * 0.6 + 4, base_y - height * 0.7), (cx + lean, top + 6)],
               max(8, height * 0.13), 5, rng, twist=0.13, flare=height * 0.07)
    for (dx, dy, rx, ry) in pads:
        if dy > 8:
            paint_limb(c, [(cx + lean * (1 - dy / height), top + dy + 10), (cx + dx * 0.5, top + dy + 4),
                           (cx + dx, top + dy + 2)], 4, 2, rng)
    for (dx, dy, rx, ry) in sorted(pads, key=lambda p: -p[1]):
        paint_pad(c, cx + dx, top + dy, rx, ry, rng, ramp)


def paint_stone(c, cx, base_y, w, h, rng, kind="stele"):
    """Estela de runa ou bloco de ruína em pedra arenosa, musgo no topo."""
    half = w / 2
    taper = (base_y - c.yy) / h * 1.5
    body = (np.abs(c.xx - cx) <= half - taper) & (c.yy <= base_y) & (c.yy >= base_y - h)
    cap = (c.yy < base_y - h + 4) & (((c.xx - cx) ** 2 / (half - 1) ** 2 + (c.yy - (base_y - h + 4)) ** 2 / 16) > 1)
    body &= ~cap
    c.occlude(body)
    c.put("BASE", body, "Solo_Terra_Argila")
    c.put("HIGHLIGHT", body & (c.xx < cx - half * 0.35), "Solo_Areia_Media")
    c.put("SHADOW", body & (c.xx > cx + half * 0.45), "Solo_Terra_Sombra")
    rune = body & (np.abs(c.xx - cx) <= 2) & ((c.yy - (base_y - h + 8)) % 6 < 3) & (c.yy < base_y - 6) & \
        (c.yy > base_y - h + 6) & ((c.xx + (c.yy // 6)) % 3 != 0)
    c.put("DETAIL", rune, "Solo_Terra_Sombra")
    p = np.pad(body, 1)
    topm = body & (c.yy < base_y - h + 7) & (noise_2d(c.h, c.w, 2, rng) > -0.2)
    c.put("MOSS", topm, "Folha_Verde_Sombra")
    c.put("MOSS", topm & ~p[:-2, 1:-1], "Folha_Jade_Medio")
    return body


def paint_arch(c, cx, base_y, rng):
    """Arco de ruína: dois pilares de blocos e arco semicircular, com hera."""
    outer, inner, top = 22, 12, base_y - 58
    acy = top + outer
    ring = ((c.xx - cx) ** 2 + (c.yy - acy) ** 2 <= outer ** 2) & ((c.xx - cx) ** 2 + (c.yy - acy) ** 2 >= inner ** 2) & (c.yy <= acy)
    pillars = (c.yy > acy) & (c.yy <= base_y) & (np.abs(c.xx - cx) <= outer) & (np.abs(c.xx - cx) >= inner)
    broken = (c.xx > cx + 8) & (c.yy < top + 9) & (c.xx - cx > (c.yy - top) * 1.2 + 4)
    m = (ring | pillars) & ~broken
    c.occlude(m)
    c.put("BASE", m, "Solo_Terra_Argila")
    c.put("HIGHLIGHT", m & (((c.xx - cx) < -inner - 4) | ((c.yy < acy - 8) & (c.xx < cx))), "Solo_Areia_Media")
    c.put("SHADOW", m & ((c.xx - cx) > inner + 4), "Solo_Terra_Sombra")
    c.put("SHADOW", m & (np.abs(c.xx - cx) < inner + 3) & (c.yy > acy - 4), "Solo_Terra_Sombra")
    mortar = m & ((c.yy % 7 == 0) | (((c.xx + (c.yy // 7) * 4) % 9) == 0)) & (c.yy > acy)
    c.put("DETAIL", mortar, "Solo_Rocha_Base")
    ang = np.arctan2(c.yy - acy, c.xx - cx)
    c.put("DETAIL", ring & (np.abs(np.sin(ang * 6)) < 0.12), "Solo_Rocha_Base")
    p = np.pad(m, 1)
    moss = m & ~p[:-2, 1:-1]
    c.put("MOSS", moss, "Folha_Jade_Medio")
    vine = m & (noise_2d(c.h, c.w, 3, rng) > 0.42) & ((c.xx - cx) < -inner + 2) & (c.yy > top + 10)
    c.put("MOSS", vine, "Folha_Verde_Sombra")
    return m


def paint_water(c, x0, x1, top, bottom, rng, glow_x):
    xs = np.arange(c.w)
    cx = (x0 + x1) / 2
    half = (x1 - x0) / 2
    u = np.clip(np.abs(xs - cx) / half, 0, 1)
    edge = top + (1 - np.sqrt(np.clip(1 - u ** 2.2, 0, 1))) * (bottom - top) + fbm_1d(c.w, rng, (24, 12, 6)) * 2
    lower = bottom - (u ** 4) * 3 + fbm_1d(c.w, rng, (20, 10)) * 1.2
    m = (c.yy >= edge[None, :]) & (c.yy <= lower[None, :]) & (np.abs(c.xx - cx) <= half)
    c.occlude(m)
    c.put("BASE", m, "Ceu_Ciano_Claro")
    c.put("SHADOW", m & (c.yy > bottom - 4) & checker(c), "Ceu_Azul_Vibrante")
    c.put("SHADOW", m & (c.yy > bottom - 2), "Ceu_Azul_Vibrante")
    p = np.pad(m, 1)
    c.put("HIGHLIGHT", m & ~p[:-2, 1:-1], "Ceu_Aura_Luminosa")
    refl = np.zeros_like(m)
    for _ in range(26):
        rx = int(rng.integers(x0 + 6, x1 - 6))
        ry = int(rng.integers(top + 2, bottom - 2))
        ln = int(rng.integers(4, 14))
        refl[ry, rx:rx + ln] = True
    refl &= m
    cream = refl & (noise_2d(c.h, c.w, 9, rng) > 0)
    c.put("DETAIL", cream, "Nuvem_Creme_Solar")
    c.put("DETAIL", refl & ~cream, "Nuvem_Sombra_Lavanda")
    pink = m & (np.abs(c.xx - glow_x) <= 3 - (c.yy - top) // 4) & ((c.yy - top) % 2 == 0)
    c.put("FX", pink, "Flor_Rosa_Luz")
    shore = (c.yy >= edge[None, :] - 2) & (c.yy < edge[None, :]) & (np.abs(c.xx - cx) <= half + 2)
    shore &= ~m
    c.occlude(shore)
    c.put("BASE", shore, "Solo_Areia_Media")
    c.put("MOSS", shore & (noise_2d(c.h, c.w, 3, rng) > 0.2), "Folha_Jade_Medio")
    return m, shore


def paint_crystal(c, cx, base_y):
    h, w = 14, 4
    tri = (c.yy <= base_y) & (c.yy >= base_y - h) & \
        (np.abs(c.xx - cx) <= w * (1 - np.abs((c.yy - (base_y - h * 0.45)) / (h * 0.55))))
    c.occlude(tri)
    c.put("BASE", tri, "Flor_Magenta_Viva")
    c.put("HIGHLIGHT", tri & (c.xx < cx), "Flor_Rosa_Luz")
    c.put("SHADOW", tri & (c.xx > cx + 1), "Fungo_Purpura_Medio")
    c.put("FX", (c.xx == cx - 1) & (c.yy == base_y - 9), "Marmore_Luz_Solar")
    sparks = np.zeros_like(tri)
    for dx, dy in [(-6, -10), (5, -13), (-4, -16), (7, -6), (-8, -4)]:
        sparks[base_y + dy, cx + dx] = True
    c.put("FX", sparks, "Flor_Rosa_Luz")


def paint_boulder(c, cx, base_y, rx, ry, rng):
    d2 = ((c.xx - cx) / rx) ** 2 + ((c.yy - base_y) / ry) ** 2 + noise_2d(c.h, c.w, 4, rng) * 0.1
    m = (d2 < 1) & (c.yy <= base_y)
    c.occlude(m)
    c.put("BASE", m, "Pedra_Ardosia_Sombra")
    c.put("HIGHLIGHT", m & ((c.xx - cx) / rx + (c.yy - base_y) / ry < -0.9), "Pedra_Ardosia_Luz")
    c.put("SHADOW", m & ((c.xx - cx) / rx > 0.35), "Basalto_Sombra_Fria")
    p = np.pad(m, 1)
    c.put("MOSS", m & ~p[:-2, 1:-1] & (c.xx < cx + rx * 0.3), "Folha_Jade_Medio")


def paint_bushes(c, xs, base_y, rng):
    for x in xs:
        r = rng.uniform(5, 9)
        d2 = ((c.xx - x) / r) ** 2 + ((c.yy - base_y + r * 0.4) / (r * 0.7)) ** 2 + noise_2d(c.h, c.w, 3, rng) * 0.15
        m = (d2 < 1) & (c.yy <= base_y)
        c.occlude(m)
        c.put("BASE", m, "Folha_Verde_Sombra")
        c.put("MOSS", m & ((c.xx - x) + (c.yy - base_y + r * 0.4) < -r * 0.3), "Folha_Jade_Medio")
        c.put("SHADOW", m & ((c.xx - x) > r * 0.4), "Folha_Sombra_Profunda")


def visible(c, mask, colors):
    flat = c.flat()
    return mask & np.isin(flat, [IDX[n] for n in colors])


def paint_tuft(c, x, y, rng, h=None):
    """Tufo de grama em leque (3–5 lâminas), base escura, ponta clara à esquerda."""
    blades = [(-2, 2), (-1, 4), (0, 3), (1, 5), (2, 2)][int(rng.integers(0, 2)):]
    if h is not None:
        blades = [(dx, max(1, min(bh, h))) for dx, bh in blades]
    for dx, bh in blades:
        for k in range(bh):
            px, py = x + dx + (1 if (k > 2 and dx > 0) else 0), y - k
            if 0 <= px < c.w and 0 <= py < c.h:
                col = "Folha_Verde_Sombra" if k == 0 else (
                    "Folha_Verde_Luz" if (k == bh - 1 and dx <= 0) else "Folha_Jade_Medio")
                for n in LAYERS:
                    c.layers[n][py, px] = -1
                c.layers["MOSS"][py, px] = IDX[col]
    return x, y - max(bh for _, bh in blades)


def refine_mid_41(c, masks, seed=4141):
    """Retoque V4.1: musgo nas ruínas, flora na água, borda de luz na mata."""
    rng = np.random.default_rng(seed)
    p = lambda m: np.pad(m, 1)
    # 1) Mata: borda superior iluminada só onde a mata ainda aparece.
    tl = masks["treeline"]
    rim = visible(c, tl & ~p(tl)[:-2, 1:-1], ["Folha_Verde_Sombra", "Folha_Jade_Medio",
                                                 "Folha_Sombra_Profunda"])
    c.put("HIGHLIGHT", rim, "Folha_Jade_Medio")
    sparkle = rim & (noise_2d(c.h, c.w, 2, rng) > 0.62)
    c.put("HIGHLIGHT", sparkle, "Folha_Verde_Luz")
    # 2) Musgo nas ruínas: faces de cima + manchas no lado iluminado + hera pendente.
    stone_cols = ["Solo_Terra_Argila", "Solo_Areia_Media", "Solo_Terra_Sombra", "Solo_Rocha_Base"]
    for key in ("arch", "stele_a", "stele_b"):
        m = visible(c, masks[key], stone_cols + ["Folha_Jade_Medio", "Folha_Verde_Sombra"])
        top = m & ~p(masks[key])[:-2, 1:-1]
        under = np.vstack([np.zeros((1, c.w), bool), top[:-1]]) & m
        c.put("MOSS", under, "Folha_Verde_Sombra")
        c.put("MOSS", top, "Folha_Jade_Medio")
        c.put("MOSS", top & (noise_2d(c.h, c.w, 2, rng) > 0.3), "Folha_Verde_Luz")
        patch = m & (noise_2d(c.h, c.w, 3, rng) > 0.5) & (c.xx < np.median(c.xx[m]) if m.any() else m)
        c.put("MOSS", patch, "Folha_Verde_Sombra")
        c.put("MOSS", patch & (noise_2d(c.h, c.w, 2, rng) > 0.4), "Folha_Jade_Medio")
        ys, xs = np.where(top)
        for i in rng.choice(len(xs), size=min(len(xs), 5 if key == "arch" else 2), replace=False) if len(xs) else []:
            ln = int(rng.integers(3, 9))
            for k in range(1, ln):
                yy, xx = ys[i] + k, xs[i] + (k // 4)
                if yy < c.h and m[yy, xx]:
                    c.layers["MOSS"][yy, xx] = IDX["Folha_Verde_Sombra" if k % 3 else "Folha_Jade_Medio"]
    # 3) Margem da lagoa: tufos, flores minúsculas e juncos nas pontas.
    water = masks["water"]
    shore_top = visible(c, ~water & np.vstack([water[1:], np.zeros((1, c.w), bool)]) | masks["shore"],
                        ["Solo_Areia_Media", "Folha_Jade_Medio", "Folha_Verde_Sombra"])
    shore_top &= ~p(masks["shore"] | water)[:-2, 1:-1]
    ys, xs = np.where(shore_top)
    order = np.argsort(xs)
    last = -99
    flowers = ["Flor_Rosa_Luz", "Luz_Ouro_Runico", "Marmore_Luz_Solar", "Flor_Rosa_Luz"]
    nf = 0
    for i in order:
        if xs[i] - last < int(rng.integers(5, 10)):
            continue
        last = xs[i]
        tx, ty = paint_tuft(c, int(xs[i]), int(ys[i]), rng)
        if rng.random() < 0.35:
            c.layers["FX"][max(0, ty), tx] = IDX[flowers[nf % len(flowers)]]
            nf += 1
    wx = np.where(water.any(0))[0]
    for side, xs_reed in (("L", wx.min() + np.array([2, 5, 7])), ("R", wx.max() - np.array([3, 6]))):
        for x in xs_reed:
            col = water[:, x]
            y0 = int(np.argmax(col)) + 1 if col.any() else 0
            hgt = int(rng.integers(7, 12))
            for k in range(hgt):
                c.layers["MOSS"][y0 - k, x] = IDX["Folha_Verde_Sombra" if k < hgt - 2 else "Fronde_Ocre_Sombra"]
            c.layers["MOSS"][y0 - hgt + 1, x + 1] = IDX["Fronde_Ocre_Sombra"]
    # 4) Vitórias-régias: poucas, longe do reflexo do cristal.
    for lx, ly, fl in ((wx.min() + 22, 134, True), (wx.min() + 40, 137, False), (wx.max() - 26, 135, True)):
        pad = (np.abs(c.xx - lx) <= 2) & (c.yy == ly) | (np.abs(c.xx - lx) <= 1) & (c.yy == ly + 1)
        pad &= water
        c.put("MOSS", pad & (c.yy == ly), "Folha_Jade_Medio")
        c.put("MOSS", pad & (c.yy == ly + 1), "Folha_Verde_Sombra")
        if fl:
            c.layers["FX"][ly - 1, lx - 1] = IDX["Flor_Rosa_Luz"]
    # 5) Tufos no chão da mata entre os objetos (silhueta limpa contra o fundo).
    floor_top = visible(c, masks["floor"] & ~p(masks["floor"])[:-2, 1:-1],
                        ["Folha_Sombra_Profunda", "Folha_Verde_Sombra"])
    ys, xs = np.where(floor_top)
    last = -99
    for i in np.argsort(xs):
        if xs[i] - last < int(rng.integers(9, 18)):
            continue
        last = xs[i]
        paint_tuft(c, int(xs[i]), int(ys[i]), rng, h=3)


def paint_city(seed=1717):
    rng = np.random.default_rng(seed)
    c = Canvas(CITY_W, CITY_H)
    ground = 150
    masks = {}
    # Mata intermediária contínua (fundo da camada) — verdes, separada da far azulada.
    masks["treeline"] = paint_treeline(c, 134, rng, "Folha_Jade_Medio", "Folha_Verde_Sombra",
                                       "Folha_Sombra_Profunda", "Folha_Oclusao_Extrema", 8, 15)
    floor = (c.yy >= 138)
    masks["floor"] = floor
    c.occlude(floor)
    c.put("BASE", floor, "Folha_Sombra_Profunda")
    c.put("MOSS", floor & (c.yy < 142) & (noise_2d(c.h, c.w, 3, rng) > 0), "Folha_Verde_Sombra")
    # Parasóis: grande à esquerda-centro, menor no centro, um na borda esquerda.
    paint_parasol(c, 48, ground, 104, [(-6, 0, 34, 12), (16, 30, 24, 9), (-18, 48, 20, 7)], rng, lean=6)
    paint_parasol(c, 168, ground, 138, [(4, 0, 48, 15), (-30, 36, 30, 10), (30, 50, 28, 9), (-10, 74, 22, 7)],
                  rng, lean=10)
    paint_parasol(c, 262, ground, 96, [(0, 0, 40, 13), (18, 28, 20, 8)], rng, lean=-6)
    masks["stele_a"] = paint_stone(c, 212, ground - 4, 13, 34, rng)
    masks["stele_b"] = paint_stone(c, 432, ground - 6, 11, 26, rng)
    masks["arch"] = paint_arch(c, 384, ground - 2, rng)
    masks["water"], masks["shore"] = paint_water(c, 228, 356, 126, 141, rng, glow_x=300)
    paint_crystal(c, 300, 125)
    paint_weeping(c, 520, ground, rng)
    paint_boulder(c, 600, ground - 2, 18, 13, rng)
    paint_boulder(c, 96, ground - 2, 14, 10, rng)
    paint_boulder(c, 444, ground - 4, 10, 7, rng)
    paint_bushes(c, [8, 120, 196, 248, 410, 470, 574, 632], ground - 4, rng)
    refine_mid_41(c, masks)
    return c


# ================================================================ saída
def save(c, name, out, report):
    files = []
    for n in LAYERS:
        arr = orphans_to_neighbors(c.layers[n], 1) if n in ("BASE",) else c.layers[n]
        c.layers[n] = arr
        path = out / f"{name}__{n}.png"
        to_rgba(arr).save(path)
        files.append(str(path))
    flat = c.flat()
    to_rgba(flat).save(out / f"{name}__FLAT_preview.png")
    used = sorted({NAMES[i] for i in np.unique(flat) if i >= 0})
    report[name] = dict(size=[c.w, c.h], layers=files, colors=used,
                        opaque=int((flat >= 0).sum()), total=c.w * c.h)


def main():
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    report = {}
    save(paint_sky(), "sky", out, report)
    for i, (name, spec) in enumerate(CLOUDS.items()):
        save(cloud_canvas_41(spec, 31 + i), name, out, report)
    save(paint_far(), "far", out, report)
    save(paint_city(), "city", out, report)
    (out / "relatorio_pintura.json").write_text(json.dumps(report, indent=2, ensure_ascii=False))
    for k, v in report.items():
        print(k, v["size"], "cores", len(v["colors"]), "opacos", v["opaque"], "/", v["total"])


if __name__ == "__main__":
    main()
