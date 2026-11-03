import shutil, hashlib, sys
from pathlib import Path
import numpy as np
from PIL import Image
R = Path.cwd(); PF = R/"Tools/Aseprite/plataformas_frente"; T1 = R/"Assets/Resources/T1_Hybrid"
OUT = PF/"out_v7_1_plats"; V7 = R/"Tools/Aseprite/fundo_cenario_v7/out_v7_1"
sys.path.insert(0, str(R/"Tools/Aseprite/fundo_refino_v4")); import pintar_fundo_v4 as V4
PAL = np.array(list(V4.PAL.values())); NAMES = V4.NAMES
h = lambda p: hashlib.md5(p.read_bytes()).hexdigest()[:10]
PIECES = ["ground","ground_edge_l","ground_edge_r","pit","plat_s","plat_m","plat_l"]
B1 = PF/"backup_pre_v7_1_plats"; B1.mkdir(exist_ok=True)
B2 = PF/"backup_pre_v7_1_sombra"; B2.mkdir(exist_ok=True)
for p in PIECES:
    if not (B1/f"{p}.png").exists(): shutil.copy2(T1/f"{p}.png", B1/f"{p}.png")
for p in ["sky","far","city"]:
    if not (B2/f"{p}.png").exists(): shutil.copy2(T1/f"{p}.png", B2/f"{p}.png")
pre = {p: h(T1/f"{p}.png") for p in ["sky","clouds_a","clouds_b","clouds_c","clouds_d","far","city"]}
# contrato
for p in PIECES:
    old = np.array(Image.open(B1/f"{p}.png").convert("RGBA")); new = np.array(Image.open(OUT/f"{p}__FLAT_preview.png"))
    assert old.shape == new.shape, p
    if p.startswith("plat"):
        assert (new[4, 2:new.shape[1]-2, 3] == 255).all() and ((new[4,:,3]>0)==(old[4,:,3]>0)).all(), p
    elif p != "pit":
        assert ((new[8:,:,3]>0)==(old[8:,:,3]>0)).all() or ((new[8,:,3]>0)==(old[8,:,3]>0)).all(), p
    shutil.copy2(OUT/f"{p}__FLAT_preview.png", T1/f"{p}.png")
# reforço leve de sombra far/city (só GPL)
lum = PAL @ [0.3, 0.59, 0.11]
def darker(i):
    best, bd = i, 1e9
    for j in range(len(PAL)):
        if not (0.62*lum[i] <= lum[j] <= 0.9*lum[i]): continue
        d = np.abs((PAL[j]/max(1,lum[j])) - (PAL[i]/max(1,lum[i]))).sum()*100 + (0.78*lum[i]-lum[j])**2*0.01
        if d < bd: best, bd = j, d
    return best
DK = {i: darker(i) for i in range(len(PAL))}
def shade(src, dst):
    a = np.array(Image.open(src).convert("RGBA")); op = a[..., 3] > 0
    idx = np.full(op.shape, -1)
    for i, c in enumerate(PAL): idx[(a[..., :3] == c).all(-1) & op] = i
    assert (idx[op] >= 0).all()
    H, W = op.shape
    tr = lambda k: np.hstack([~op[:, k:], np.ones((H, k), bool)])  # transparent k to the right
    right = op & (tr(1) | tr(2))
    bot = np.vstack([~op[1:], np.zeros((1, W), bool)])
    lowc = op & (bot | np.vstack([~op[2:], np.zeros((2, W), bool)]))
    # contato no chão: 1px escuro acima da linha mais baixa de cada coluna opaca (sombra de base)
    m = (right | lowc) & ((np.indices(op.shape).sum(0)) % 2 == 0 | right)
    m = right | (lowc & (np.indices(op.shape).sum(0) % 2 == 0))
    out = idx.copy()
    for i in np.unique(idx[m]): out[m & (idx == i)] = DK[i]
    a[..., :3] = np.where(out[..., None] >= 0, PAL[np.clip(out, 0, None)], a[..., :3])
    Image.fromarray(a.astype(np.uint8)).save(dst); return int(m.sum())
for p in ["far","city"]:
    n = shade(B2/f"{p}.png", T1/f"{p}.png"); print(p, "px sombreados", n)
post = {p: h(T1/f"{p}.png") for p in pre}
for p in pre: print(p, pre[p], "->", post[p], "MUDOU" if pre[p]!=post[p] else "igual")
