"""Plataformas da frente (pós V4.1) — pintura nativa 1× do kit jogável da Fase I.

Peças: ground (faixa tileável), ground_edge_l / ground_edge_r (beiradas dos gaps),
pit (abismo do gap) e plat_s / plat_m / plat_l (ilhas flutuantes de 3 / 5 / 7 tiles).
Mesma linguagem do mid V4.1: grama viva, terra em blocos arredondados, musgo,
raízes e cristal rosa pontual; pedra rúnica alienígena com glifo dourado.
Usa SOMENTE as cores da paleta_alien_diurna_64cores.gpl (via pintar_fundo_v4) e
não lê a referência de IA. Camadas: BASE, SHADOW, DETAIL, HIGHLIGHT, MOSS, FX.

Contrato com o Unity (T1FrontLevel.cs):
  * ground*/pit: 64 px de altura, superfície de caminhada na linha GROUND_SURF (8).
  * plat_*: superfície na linha PLAT_SURF (4); topo sólido de x=2 até w-3.
  * ground_edge_l: coluna 0 = beirada do gap; colunas 16..23 sobrepõem o ground.
    ground_edge_r é o espelho do contrato (gap na coluna 23).

Uso: python3 pintar_plataformas_frente.py <pasta_saida_camadas>
"""
import json
import sys
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "fundo_refino_v4"))
from pintar_fundo_v4 import IDX, LAYERS, Canvas, paint_tuft, save  # noqa: E402

GROUND_W, GROUND_H, GROUND_SURF = 256, 64, 8
EDGE_W, EDGE_OVERLAP = 24, 8
PIT_W = 32
PLAT_SURF = 4
PLATS = {"plat_s": (48, 26), "plat_m": (80, 30), "plat_l": (112, 34)}


# ---------------------------------------------------------------- utilidades
def periodic_noise(h, w, cell, rng):
    """Ruído de valor 2D com período w em x (w múltiplo de cell)."""
    gw, gh = w // cell, h // cell + 2
    g = rng.uniform(-1, 1, (gh, gw))
    ys, xs = np.mgrid[0:h, 0:w] / cell
    y0, x0 = ys.astype(int), xs.astype(int)
    fy, fx = ys - y0, xs - x0
    fy, fx = fy * fy * (3 - 2 * fy), fx * fx * (3 - 2 * fx)
    x1 = (x0 + 1) % gw
    x0 = x0 % gw
    a = g[y0, x0] * (1 - fx) + g[y0, x1] * fx
    b = g[y0 + 1, x0] * (1 - fx) + g[y0 + 1, x1] * fx
    return a * (1 - fy) + b * fy


def periodic_wave(w, rng, cells=(32, 16, 8)):
    out = np.zeros(w)
    amp = 1.0
    xs = np.arange(w)
    for c in cells:
        k = w // c
        g = rng.uniform(-1, 1, k)
        x = xs / c
        i = x.astype(int)
        f = x - i
        f = f * f * (3 - 2 * f)
        out += amp * (g[i % k] * (1 - f) + g[(i + 1) % k] * f)
        amp *= 0.5
    return out / max(1e-6, np.abs(out).max())


def shift_down(m, k=1):
    return np.vstack([np.zeros((k, m.shape[1]), bool), m[:-k]])


def shift_up(m, k=1):
    return np.vstack([m[k:], np.zeros((k, m.shape[1]), bool)])


def shift_right(m, k=1):
    return np.hstack([np.zeros((m.shape[0], k), bool), m[:, :-k]])


def shift_left(m, k=1):
    return np.hstack([m[:, k:], np.zeros((m.shape[0], k), bool)])


def cobbles(c, rng, cell_w, cell_h, periodic):
    """Blocos arredondados (Voronoi anisotrópico em fileiras desencontradas).

    Retorna (id da célula, máscara de junta, centro y de cada célula)."""
    seeds = []
    row = 0
    y = -cell_h * 0.3
    while y < c.h + cell_h:
        x = rng.uniform(0, cell_w) + (cell_w * 0.5 if row % 2 else 0)
        while x < c.w + cell_w:
            seeds.append((x + rng.uniform(-2, 2), y + rng.uniform(-1.5, 1.5),
                          cell_w * rng.uniform(0.8, 1.2), cell_h * rng.uniform(0.85, 1.15)))
            x += cell_w * rng.uniform(0.85, 1.15)
        y += cell_h * rng.uniform(0.85, 1.05)
        row += 1
    s = np.array(seeds)
    d1 = np.full((c.h, c.w), 1e9)
    d2 = np.full((c.h, c.w), 1e9)
    cid = np.zeros((c.h, c.w), int)
    for i, (sx, sy, sw, sh) in enumerate(s):
        dx = c.xx - sx
        if periodic:
            dx = (dx + c.w / 2) % c.w - c.w / 2
        d = (dx / sw) ** 2 + ((c.yy - sy) / sh) ** 2
        closer = d < d1
        d2 = np.where(closer, d1, np.minimum(d2, d))
        cid = np.where(closer, i, cid)
        d1 = np.where(closer, d, d1)
    joint = (np.sqrt(d2) - np.sqrt(d1)) < 0.16
    return cid, joint, s[:, 1]


def paint_earth(c, body, rng, top_y, deep_y, cell=(11, 8), periodic=False, runes=1, flecks=2):
    """Terra/rocha em blocos com luz de cima-esquerda, escurecendo com a profundidade."""
    cid, joint, sy = cobbles(c, rng, cell[0], cell[1], periodic)
    depth = np.clip((c.yy - top_y) / max(1, deep_y - top_y), 0, 1)
    stone = body & ~joint
    tiers = [
        (depth < 0.45, "Solo_Terra_Argila", "Solo_Areia_Media", "Solo_Terra_Sombra", "Solo_Rocha_Base"),
        ((depth >= 0.45) & (depth < 0.8), "Solo_Terra_Sombra", "Solo_Terra_Argila", "Solo_Rocha_Base",
         "Solo_Subsolo_Profundo"),
        (depth >= 0.8, "Solo_Rocha_Base", "Solo_Terra_Sombra", "Solo_Subsolo_Profundo", "Casca_Contorno_Escuro"),
    ]
    lit = stone & (~shift_down(stone) | ~shift_right(stone))
    lit &= ~(~shift_up(stone) | ~shift_left(stone))
    dark = stone & (~shift_up(stone) | ~shift_left(stone))
    for tier, base, hi, sh, mortar in tiers:
        c.put("BASE", stone & tier, base)
        c.put("HIGHLIGHT", lit & tier, hi)
        c.put("SHADOW", dark & tier, sh)
        c.put("DETAIL", body & joint & tier, mortar)
    # Textura interna: poucos pontos de terra mais escura (clusters de 2 px).
    grit = stone & (periodic_noise(c.h, c.w, 4, rng) > 0.55 if periodic else
                    (rng.random((c.h, c.w)) > 0.93)) & shift_right(stone) & ~lit
    c.put("SHADOW", grit & (depth < 0.45), "Solo_Terra_Sombra")
    c.put("SHADOW", grit & (depth >= 0.45), "Solo_Rocha_Base")
    # Pedra rúnica alienígena: um bloco em ardósia com glifo dourado.
    ids = [i for i in np.unique(cid[stone & (depth > 0.15) & (depth < 0.55)])
           if (cid == i)[stone].sum() > 30]
    for i in rng.choice(ids, size=min(runes, len(ids)), replace=False) if ids else []:
        m = (cid == i) & stone
        ys, xs = np.where(m)
        cx, cy = int(np.median(xs)), int(np.median(ys))
        c.put("BASE", m, "Pedra_Ardosia_Sombra")
        c.put("HIGHLIGHT", m & lit, "Pedra_Ardosia_Luz")
        c.put("SHADOW", m & dark, "Basalto_Sombra_Fria")
        glyph = m & (((np.abs(c.xx - cx) <= 1) & (c.yy == cy)) | ((c.xx == cx) & (np.abs(c.yy - cy) <= 1)))
        c.put("FX", glyph, "Luz_Ouro_Runico")
    # Flecos de cristal rosa presos na terra.
    cand = np.argwhere(joint & body & (depth > 0.2) & (depth < 0.7))
    for k in rng.choice(len(cand), size=min(flecks, len(cand)), replace=False) if len(cand) else []:
        y, x = cand[k]
        for dy, dx, col in ((0, 0, "Flor_Magenta_Viva"), (-1, 0, "Flor_Rosa_Luz"), (0, 1, "Fungo_Purpura_Medio")):
            yy, xx = y + dy, (x + dx) % c.w
            if body[yy, xx]:
                c.layers["FX"][yy, xx] = IDX[col]
    return stone


def paint_grass_cap(c, solid_cols, surf, under, rng, periodic):
    """Faixa de grama: topo plano na superfície, franja ondulada caindo na terra."""
    top = (c.yy >= surf) & (c.yy <= under[None, :]) & solid_cols[None, :]
    c.occlude(top)
    c.put("BASE", top, "Folha_Jade_Medio")
    n = periodic_noise(c.h, c.w, 4, rng) if periodic else rng.uniform(-1, 1, (c.h, c.w))
    c.put("HIGHLIGHT", top & (c.yy == surf) & (n > -0.35), "Folha_Verde_Luz")
    c.put("HIGHLIGHT", top & (c.yy == surf) & (n <= -0.35), "Folha_Esmeralda_Viva")
    c.put("HIGHLIGHT", top & (c.yy == surf + 1) & ((c.xx + c.yy) % 2 == 0), "Folha_Esmeralda_Viva")
    c.put("SHADOW", top & (c.yy >= under[None, :] - 1), "Folha_Verde_Sombra")
    c.put("SHADOW", top & (c.yy == under[None, :]), "Folha_Sombra_Profunda")
    lumps = top & (c.yy > surf + 1) & (c.yy < under[None, :] - 1) & (n > 0.45)
    c.put("DETAIL", lumps, "Folha_Verde_Sombra")
    return top


def paint_tufts(c, xs, surf, rng, solid_cols, flowers=0.3):
    palette = ["Luz_Ouro_Runico", "Flor_Rosa_Luz", "Marmore_Luz_Solar"]
    k = 0
    for x in xs:
        if not (2 <= x < c.w - 2 and solid_cols[x]):
            continue
        tx, ty = paint_tuft(c, int(x), surf - 1, rng, h=int(rng.integers(2, 5)))
        if rng.random() < flowers and ty >= 0:
            c.layers["FX"][max(0, ty), tx] = IDX[palette[k % len(palette)]]
            k += 1


def tuft_positions(w, rng, gap=(6, 12), start=3):
    xs, x = [], start
    while x < w - 3:
        xs.append(x)
        x += int(rng.integers(*gap))
    return xs


def contact_shadow(c, grass, body):
    """Linha escura sob a franja de grama, onde ela encosta na terra."""
    under = shift_down(grass) & ~grass & body
    c.put("DETAIL", under, "Solo_Subsolo_Profundo")


# ================================================================ GROUND
def ground_cap_profile(w, rng, periodic):
    wave = periodic_wave(w, rng) if periodic else np.interp(
        np.arange(w), np.linspace(0, w - 1, 6), rng.uniform(-1, 1, 6))
    under = GROUND_SURF + 5 + np.round(wave * 1.4).astype(int)
    # Pingos de grama descendo pela terra (2–3 px de largura).
    x = int(rng.integers(3, 10))
    while x < w - 3:
        ln = int(rng.integers(2, 4))
        under[x:x + int(rng.integers(2, 4))] += ln
        x += int(rng.integers(9, 20))
    return under


def paint_ground(seed=5101):
    rng = np.random.default_rng(seed)
    c = Canvas(GROUND_W, GROUND_H)
    cols = np.ones(c.w, bool)
    under = ground_cap_profile(c.w, rng, periodic=True)
    body = c.yy > GROUND_SURF
    c.occlude(body)
    paint_earth(c, body, rng, GROUND_SURF + 6, GROUND_H - 2, periodic=True, runes=1, flecks=3)
    grass = paint_grass_cap(c, cols, GROUND_SURF, under, rng, periodic=True)
    contact_shadow(c, grass, body)
    paint_roots_in_soil(c, body & ~grass, under, rng, periodic=True)
    paint_tufts(c, tuft_positions(c.w, rng), GROUND_SURF, rng, cols)
    return c


def paint_roots_in_soil(c, soil, under, rng, periodic):
    """Raízes finas descendo da grama pelas juntas (poucas, curtas)."""
    for _ in range(4):
        x = int(rng.integers(2, c.w - 2))
        y = int(under[x]) + 1
        for k in range(int(rng.integers(4, 9))):
            xx = (x + (k // 3) * (1 if rng.random() < 0.5 else -1)) % c.w
            if 0 <= y + k < c.h and soil[y + k, xx]:
                c.layers["DETAIL"][y + k, xx] = IDX["Casca_Fenda_Profunda" if k % 3 else "Casca_Tronco_Sombra"]


def paint_ground_edge(side, seed):
    """Beirada do gap. side='L': gap à esquerda (coluna 0); 'R': gap à direita."""
    rng = np.random.default_rng(seed)
    c = Canvas(EDGE_W, GROUND_H)
    gx = c.xx if side == "L" else (c.w - 1 - c.xx)          # distância ao gap
    inner = EDGE_W - EDGE_OVERLAP                              # 16: começa a sobreposição
    # Parede do penhasco: 1 px de recuo com pequenas saliências; canto arredondado no topo.
    wall = np.full(c.h, 1)
    wall += (rng.random(c.h) < 0.18).astype(int)
    wall[GROUND_SURF:GROUND_SURF + 2] = [2, 1]
    # Limite interno irregular (seguindo blocos) dentro da sobreposição.
    inner_edge = inner + 2 + np.round(np.interp(np.arange(c.h), np.linspace(0, c.h - 1, 7),
                                                rng.uniform(-2, 3, 7))).astype(int)
    solid = (c.yy > GROUND_SURF) & (gx >= wall[:, None]) & (gx <= inner_edge[:, None])
    c.occlude(solid)
    paint_earth(c, solid, rng, GROUND_SURF + 6, GROUND_H - 2, cell=(10, 8), runes=0, flecks=1)
    # Face do penhasco: borda vertical (luz à esquerda → L clara, R sombreada).
    face = solid & ~(shift_right(solid) if side == "L" else shift_left(solid))
    face2 = solid & ~(shift_right(solid, 2) if side == "L" else shift_left(solid, 2)) & ~face
    deep = c.yy > GROUND_SURF + 26
    if side == "L":
        c.put("HIGHLIGHT", face & ~deep, "Solo_Areia_Media")
        c.put("HIGHLIGHT", face & deep, "Solo_Terra_Argila")
        c.put("SHADOW", face2 & deep, "Solo_Rocha_Base")
    else:
        c.put("SHADOW", face, "Casca_Contorno_Escuro")
        c.put("SHADOW", face2 & ~deep, "Solo_Rocha_Base")
        c.put("SHADOW", face2 & deep, "Solo_Subsolo_Profundo")
    # Grama: topo plano até a beirada, lábio que dobra sobre a parede e pingos na face.
    cols = (np.arange(c.w) if side == "L" else (c.w - 1 - np.arange(c.w)))
    grass_cols = (cols >= 1) & (cols <= inner + 3)
    under = np.full(c.w, GROUND_SURF + 5)
    under += np.round(np.interp(np.arange(c.w), [0, c.w - 1], rng.uniform(-1, 1, 2))).astype(int)
    lip = np.where(grass_cols & (cols <= 3))[0]
    under[lip] += np.array([3, 2, 1][:len(lip)]) if side == "L" else np.array([1, 2, 3][-len(lip):])
    grass = paint_grass_cap(c, grass_cols, GROUND_SURF, under, rng, periodic=False)
    corner = grass & (c.yy == GROUND_SURF) & (gx == 1)
    for n in LAYERS:
        c.layers[n][corner] = -1
    c.put("HIGHLIGHT", grass & (c.yy == GROUND_SURF + 1) & (gx == 1), "Folha_Esmeralda_Viva")
    drips = np.zeros_like(solid)
    for k in range(int(rng.integers(3, 6))):
        drips[GROUND_SURF + 6 + k, (1 if side == "L" else c.w - 2)] = True
    drips &= solid
    c.put("MOSS", drips, "Folha_Verde_Sombra")
    contact_shadow(c, grass, solid)
    xs = [x for x in tuft_positions(c.w, rng, gap=(5, 9), start=3) if 2 <= (x if side == "L" else c.w - 1 - x) <= inner]
    paint_tufts(c, xs, GROUND_SURF, rng, grass_cols, flowers=0.4)
    return c


# ================================================================ PIT
def paint_pit(seed=6161):
    """Abismo do gap: vazio lavanda → preto, névoa fina e poucas faíscas de Aether."""
    rng = np.random.default_rng(seed)
    c = Canvas(PIT_W, GROUND_H)
    start = GROUND_SURF + 2
    m = c.yy >= start
    c.put("BASE", m & (c.yy < start + 4) & ((c.xx + c.yy) % 2 == 0), "Sombra_Ambiente_Lavanda")
    c.put("BASE", m & (c.yy >= start + 4), "Sombra_Ambiente_Lavanda")
    band = c.yy - (start + 14)
    c.put("SHADOW", m & (band >= 0) & (band < 6) & ((c.xx + c.yy) % 2 == 0), "Oclusao_Universal_Preto")
    c.put("SHADOW", m & (band >= 6), "Oclusao_Universal_Preto")
    n = periodic_noise(c.h, c.w, 8, rng)
    mist = m & (c.yy > start + 5) & (c.yy < start + 14) & (n > 0.35) & ((c.yy % 3) == 0)
    c.put("DETAIL", mist, "Meio_Tom_Sombra_Fria")
    motes = np.zeros_like(m)
    for _ in range(5):
        motes[int(rng.integers(start + 6, c.h - 6)), int(rng.integers(0, c.w))] = True
    c.put("FX", motes & (c.yy < start + 22), "Aether_Ciano_Vivo")
    c.put("FX", motes & (c.yy >= start + 22), "Aether_Turquesa_Profundo")
    return c


# ================================================================ PLATS
def paint_plat(w, h, seed, crystals):
    rng = np.random.default_rng(seed)
    c = Canvas(w, h)
    cx = (w - 1) / 2
    s = PLAT_SURF
    # Grama: topo de x=2..w-3, ombro arredondado, lábio que passa 1 px do corpo.
    span = np.zeros((h, w), bool)
    for y, (a, b) in {s: (2, w - 3), s + 1: (1, w - 2)}.items():
        span[y, a:b + 1] = True
    under_cap = s + 5 + np.round(np.interp(np.arange(w), np.linspace(0, w - 1, 5),
                                           rng.uniform(-1, 1, 5))).astype(int)
    under_cap[[0, w - 1]] = s + 3
    under_cap[[1, w - 2]] = np.minimum(under_cap[[1, w - 2]], s + 5)
    # Corpo: terra que afina em cone irregular por baixo (ilha flutuante).
    u = np.abs(np.arange(w) - cx) / (w / 2)
    depth = (h - s - 9) * (1 - u ** 1.7)
    depth += np.interp(np.arange(w), np.linspace(0, w - 1, max(4, w // 8)), rng.uniform(-2.5, 2.5, max(4, w // 8)))
    bottom = np.clip(np.round(s + 6 + depth), s + 5, h - 5).astype(int)
    body = (c.yy > s + 1) & (c.yy <= bottom[None, :]) & (c.xx >= 1) & (c.xx <= w - 2)
    c.occlude(body)
    paint_earth(c, body, rng, s + 5, int(bottom.max()), cell=(9, 7), runes=1 if w >= 80 else 0, flecks=1)
    # Silhueta inferior escura (lê contra o céu creme/lavanda).
    rim = body & ~shift_up(body)
    c.put("DETAIL", rim, "Casca_Contorno_Escuro")
    c.put("SHADOW", body & ~shift_up(body, 2) & ~rim, "Solo_Subsolo_Profundo")
    side_r = body & ~shift_left(body) & (c.yy > s + 6)
    c.put("SHADOW", side_r, "Casca_Contorno_Escuro")
    cap_cols = np.ones(w, bool)
    grass = paint_grass_cap(c, cap_cols, s + 2, under_cap, rng, periodic=False)
    c.occlude(span)
    c.put("HIGHLIGHT", span & (c.yy == s), "Folha_Verde_Luz")
    c.put("BASE", span & (c.yy == s + 1), "Folha_Esmeralda_Viva")
    grass |= span
    contact_shadow(c, grass, body)
    # Raízes pendentes e musgo sob a ilha.
    for _ in range(max(2, w // 20)):
        x = int(rng.integers(4, w - 4))
        y = int(bottom[x])
        ln = int(rng.integers(3, min(8, h - y)))
        for k in range(1, ln):
            xx = x + (1 if (k > 2 and rng.random() < 0.35) else 0)
            if y + k < h:
                c.occlude(np.zeros_like(body) | ((c.yy == y + k) & (c.xx == xx)))
                c.layers["DETAIL"][y + k, xx] = IDX["Casca_Tronco_Sombra" if k < ln - 1 else "Casca_Fenda_Profunda"]
    for _ in range(max(1, w // 32)):
        x = int(rng.integers(3, w - 3))
        y = int(bottom[x])
        for k in range(1, int(rng.integers(2, 4))):
            if y + k < h:
                c.layers["MOSS"][y + k, x] = IDX["Folha_Verde_Sombra"]
    # Cristal rosa saindo da barriga (pontual, como no mid).
    for i in range(crystals):
        x = int(np.clip(cx + (rng.uniform(-0.35, 0.35) * w), 4, w - 5))
        y = int(bottom[x])
        size = 4 if w >= 80 else 3
        for k in range(size):
            for dx in range(-(size - 1 - k) // 2, (size - k) // 2 + 1):
                yy, xx = y + k, x + dx
                if 0 <= yy < h and 0 <= xx < w:
                    for n in LAYERS:
                        c.layers[n][yy, xx] = -1
                    col = "Flor_Rosa_Luz" if dx < 0 else ("Fungo_Purpura_Medio" if dx > 0 else "Flor_Magenta_Viva")
                    c.layers["FX"][yy, xx] = IDX[col]
        if y - 1 >= 0 and body[y - 1, x]:
            c.layers["FX"][y - 1, x] = IDX["Flor_Magenta_Viva"]
    paint_tufts(c, tuft_positions(w, rng, gap=(7, 13), start=4), s, rng, np.arange(w) >= 0, flowers=0.35)
    return c


# ================================================================ saída
def main():
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    report = {}
    save(paint_ground(), "ground", out, report)
    save(paint_ground_edge("L", 5202), "ground_edge_l", out, report)
    save(paint_ground_edge("R", 5303), "ground_edge_r", out, report)
    save(paint_pit(), "pit", out, report)
    for i, (name, (w, h)) in enumerate(PLATS.items()):
        save(paint_plat(w, h, 7100 + i, crystals=0 if name == "plat_s" else 1), name, out, report)
    report["_contrato"] = {
        "ground_surf_row": GROUND_SURF, "plat_surf_row": PLAT_SURF,
        "plat_solid_cols": "2..w-3", "edge_overlap_px": EDGE_OVERLAP,
    }
    (out / "relatorio_pintura.json").write_text(json.dumps(report, indent=2, ensure_ascii=False))
    for k, v in report.items():
        if not k.startswith("_"):
            print(k, v["size"], "cores", len(v["colors"]), "opacos", v["opaque"], "/", v["total"])


if __name__ == "__main__":
    main()
