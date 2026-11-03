"""Plataformas da frente — V7.1 Passado Intacta.

Reaproveita a geometria/contrato de pintar_plataformas_frente.py (mesmos tamanhos,
GROUND_SURF=8, PLAT_SURF=4, topo sólido x=2..w-3, EDGE_OVERLAP=8) e troca a cromia:
grama verde -> musgo lavanda-teal; terra marrom -> argila lavanda + ardósia.
Depois adiciona detalhes únicos: runas ouro extras, veios de Aether nas ilhas,
cristais/fungos nas juntas, musgo lavanda pingando, face direita mais escura.
Só cores da GPL. Uso: python3 pintar_plataformas_v7_1.py <saida>
"""
import json, sys
from pathlib import Path
import numpy as np

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "fundo_refino_v4"))
import pintar_fundo_v4 as V4  # noqa
import pintar_plataformas_frente as P  # noqa

ORIG = dict(V4.IDX)
REMAP = {
    # topo caminhável: musgo lavanda-teal
    "Folha_Jade_Medio": "Montanha_Sombra_Fria",
    "Folha_Verde_Luz": "Nuvem_Sombra_Lavanda",
    "Folha_Esmeralda_Viva": "Montanha_Luz_Atmosfera",
    "Folha_Verde_Sombra": "Meio_Tom_Sombra_Fria",
    "Folha_Sombra_Profunda": "Sombra_Ambiente_Lavanda",
    # corpo: argila lavanda + ardósia
    "Solo_Terra_Argila": "Nuvem_Sombra_Profunda",
    "Solo_Areia_Media": "Pedra_Ardosia_Luz",
    "Solo_Terra_Sombra": "Meio_Tom_Sombra_Fria",
    "Solo_Rocha_Base": "Sombra_Ambiente_Lavanda",
    "Solo_Subsolo_Profundo": "Basalto_Oclusao_Contorno",
    "Casca_Contorno_Escuro": "Oclusao_Universal_Preto",
    "Casca_Tronco_Sombra": "Fungo_Purpura_Sombra",
    "Casca_Fenda_Profunda": "Sombra_Ambiente_Lavanda",
    "Pedra_Ardosia_Sombra": "Pedra_Ardosia_Sombra",
}


def remapped(fn, *a, **k):
    for o, n in REMAP.items():
        V4.IDX[o] = ORIG[n]
    try:
        return fn(*a, **k)
    finally:
        V4.IDX.update(ORIG)


I = ORIG
DARKER = {I["Nuvem_Sombra_Profunda"]: I["Meio_Tom_Sombra_Fria"], I["Pedra_Ardosia_Luz"]: I["Pedra_Ardosia_Sombra"],
          I["Pedra_Ardosia_Sombra"]: I["Basalto_Sombra_Fria"], I["Meio_Tom_Sombra_Fria"]: I["Sombra_Ambiente_Lavanda"],
          I["Nuvem_Sombra_Lavanda"]: I["Nuvem_Sombra_Profunda"], I["Montanha_Luz_Atmosfera"]: I["Montanha_Sombra_Fria"],
          I["Montanha_Sombra_Fria"]: I["Meio_Tom_Sombra_Fria"], I["Basalto_Sombra_Fria"]: I["Sombra_Ambiente_Lavanda"]}


def flat_top(c):
    f = c.flat()
    return f


def darken_right(c, rows_from, width=3, periodic=False):
    """Face direita mais escura (sol à esquerda)."""
    if periodic:
        return
    op = c.opaque()
    for n in ("BASE", "HIGHLIGHT"):
        arr = c.layers[n]
        for k in range(1, width + 1):
            edge = op & ~np.hstack([op[:, k:], np.zeros((c.h, k), bool)]) & (c.yy >= rows_from)
            for a, b in DARKER.items():
                arr[edge & (arr == a)] = b
    hl = c.layers["HIGHLIGHT"]
    hl[(c.xx > c.w * 0.75) & (hl == I["Nuvem_Sombra_Lavanda"])] = I["Montanha_Luz_Atmosfera"]


def extras(c, rng, surf, kind):
    op = c.opaque()
    f = c.flat()
    body = op & (c.yy > surf + 6)
    joint = body & np.isin(f, [I["Sombra_Ambiente_Lavanda"], I["Basalto_Oclusao_Contorno"]])
    # runas extras (glifo ouro em L / ponto) em blocos claros
    stone = body & np.isin(f, [I["Nuvem_Sombra_Profunda"], I["Meio_Tom_Sombra_Fria"]])
    n_runes = {"ground": 3, "edge": 1, "plat_l": 2, "plat_m": 1, "plat_s": 1}[kind]
    cand = np.argwhere(stone & (c.xx > 3) & (c.xx < c.w - 4) & (c.yy < c.h - 4))
    for k in rng.choice(len(cand), size=min(n_runes, len(cand)), replace=False) if len(cand) else []:
        y, x = cand[k]
        for dy, dx in ((0, 0), (-1, 0), (0, 1)):
            if stone[y + dy, x + dx]:
                c.layers["FX"][y + dy, x + dx] = I["Luz_Ouro_Runico"]
    # cristais magenta / fungos nas juntas
    cand = np.argwhere(joint & (c.xx > 1) & (c.xx < c.w - 2))
    nfx = {"ground": 6, "edge": 2, "plat_l": 3, "plat_m": 2, "plat_s": 1}[kind]
    for j, k in enumerate(rng.choice(len(cand), size=min(nfx, len(cand)), replace=False) if len(cand) else []):
        y, x = cand[k]
        if j % 2 == 0:
            c.layers["FX"][y, x] = I["Flor_Magenta_Viva"]
            if y > 0 and op[y - 1, x]:
                c.layers["FX"][y - 1, x] = I["Flor_Rosa_Luz"]
        else:
            c.layers["FX"][y, x] = I["Fungo_Purpura_Sombra"]
            if x + 1 < c.w and op[y, x + 1]:
                c.layers["FX"][y, x + 1] = I["Flor_Magenta_Viva"]
    if kind.startswith("plat"):
        # veio de Aether teal fino descendo pela ilha
        for _ in range({"plat_s": 1, "plat_m": 1, "plat_l": 2}[kind]):
            x = int(rng.integers(c.w // 4, 3 * c.w // 4)); y = surf + 7
            for _s in range(c.h):
                if y >= c.h or not body[y, x]:
                    break
                c.layers["FX"][y, x] = I["Aether_Turquesa_Profundo"] if _s % 3 else I["Aether_Ciano_Vivo"]
                y += 1
                x += int(rng.choice([-1, 0, 0, 1]))
                x = int(np.clip(x, 2, c.w - 3))
        # musgo lavanda pingando sob a ilha
        op = c.opaque()
        bottom = np.array([np.where(op[:, x])[0].max() if op[:, x].any() else -1 for x in range(c.w)])
        for x in rng.choice(np.arange(3, c.w - 3), size=max(3, c.w // 12), replace=False):
            y = bottom[x]
            if y < surf + 6:
                continue
            ln = int(rng.integers(2, 5))
            for k in range(1, ln + 1):
                if y + k < c.h:
                    c.layers["MOSS"][y + k, x] = I["Nuvem_Sombra_Profunda"] if k < ln else I["Meio_Tom_Sombra_Fria"]


def build(name, fn, *a, kind, surf, periodic=False, **k):
    c = remapped(fn, *a, **k)
    rng = np.random.default_rng(abs(hash(name)) % 9999 + 71)
    rng = np.random.default_rng(sum(map(ord, name)) * 13)
    darken_right(c, surf + 2, periodic=periodic)
    extras(c, rng, surf, kind)
    return c


def pit_v71():
    c = P.paint_pit()
    # brilho Aether difuso no fundo do abismo
    m = (c.yy >= c.h - 6) & ((c.xx + c.yy) % 2 == 0)
    c.layers["FX"][m & (c.yy >= c.h - 3)] = I["Aether_Turquesa_Profundo"]
    c.layers["FX"][(c.yy >= c.h - 6) & (c.yy < c.h - 3) & ((c.xx + c.yy) % 4 == 0)] = I["Montanha_Sombra_Fria"]
    return c


def main():
    out = Path(sys.argv[1]); out.mkdir(parents=True, exist_ok=True)
    rep = {}
    V4.save(build("ground", P.paint_ground, kind="ground", surf=P.GROUND_SURF, periodic=True), "ground", out, rep)
    V4.save(build("ground_edge_l", P.paint_ground_edge, "L", 5202, kind="edge", surf=P.GROUND_SURF), "ground_edge_l", out, rep)
    V4.save(build("ground_edge_r", P.paint_ground_edge, "R", 5303, kind="edge", surf=P.GROUND_SURF), "ground_edge_r", out, rep)
    V4.save(pit_v71(), "pit", out, rep)
    for i, (name, (w, h)) in enumerate(P.PLATS.items()):
        V4.save(build(name, P.paint_plat, w, h, 7100 + i, crystals=0 if name == "plat_s" else 1,
                      kind=name, surf=P.PLAT_SURF), name, out, rep)
    rep["_contrato"] = {"ground_surf_row": P.GROUND_SURF, "plat_surf_row": P.PLAT_SURF,
                        "plat_solid_cols": "2..w-3", "edge_overlap_px": P.EDGE_OVERLAP, "remap": REMAP}
    (out / "relatorio_pintura.json").write_text(json.dumps(rep, indent=2, ensure_ascii=False))
    for k, v in rep.items():
        if not k.startswith("_"):
            print(k, v["size"], len(v["colors"]), "cores")


if __name__ == "__main__":
    main()
