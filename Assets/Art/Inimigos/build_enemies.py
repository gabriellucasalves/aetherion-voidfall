# Inimigos T1 de Aetherion — mesma base chibi do Guerreiro (célula 64px,
# peças em string-art, contorno automático, brilho piscando).
# Sprint atual: SÓ o visual (idle + walk) para validação.
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path("/Users/gabriellucas/Desktop/trabalho jogo sexta/Assets/Art/Inimigos")
CELL = 64

C = {
    ".": None,
    "k": (10, 8, 14, 255),      # contorno
    # osso encardido (cinza-terra, bem sujo)
    "w": (148, 138, 116, 255),
    "b": (100, 91, 76, 255),
    "B": (60, 54, 45, 255),
    # brilho maligno (pisca e/E, v/V) — único ponto vivo
    "e": (255, 60, 52, 255),
    "E": (150, 18, 26, 255),
    "v": (176, 94, 224, 255),
    "V": (108, 50, 150, 255),
    # metal enferrujado e opaco
    "g": (94, 88, 78, 255),
    "u": (84, 64, 44, 255),
    "U": (54, 40, 28, 255),
    # zumbi (verde lodo apagado)
    "z": (60, 72, 50, 255),
    "Z": (40, 48, 34, 255),
    "x": (24, 30, 21, 255),
    # ghoul (roxo cinzento fúnebre)
    "h": (76, 64, 86, 255),
    "H": (52, 43, 62, 255),
    "n": (32, 26, 40, 255),
    # pano rasgado
    "c": (48, 20, 25, 255),
    "C": (28, 12, 16, 255),
}

# cores que NÃO recebem sombra (brilhos malignos)
GLOW = {C["e"][:3], C["E"][:3], C["v"][:3], C["V"][:3]}

# ------------------- ESQUELETO GUERREIRO -------------------

S_SKULL = [
    "..kkkkkkkkkkkk..",
    ".kwwwwwwwwwwwwk.",
    "kwwwwwwwwwwwwwwk",
    "kwwwwwwwwwwwwwwk",
    "kwwbwwwkkkkwwwwk",
    "kbwwwwwkeekwwwwk",
    "kbwbwwwkeekwwwwk",
    "kbbwwwwkkkkwkkwk",
    ".kbbwwwwwwwwkkbk",
    ".kkbbbbbbbbbbkk.",
    "..kwkwkwkwkwkk..",
    "..kbbbbbbbbbbk..",
    "...kkkkkkkkkk...",
]

S_RIBS = [
    ".kkkkkkkkkk.",
    "kwwwwwwwwwwk",
    "kkBkBkBkBkkk",
    "kwwwwwwwwwwk",
    "kkBkBkBkBkkk",
    "kwwwwwwwwwwk",
    ".kcCcCcCcCk.",
    ".kCcCcCcCCk.",
    "..kkkkkkkk..",
]

S_LEG = [
    "kbk..",
    "kwk..",
    "kwk..",
    "kwbk.",
    "kkkk.",
]

S_SWORD = [
    "...g...",
    "..kgk..",
    ".kggwk.",
    ".kgugk.",
    ".kggwk.",
    ".kgggk.",
    ".kuggk.",
    ".kggwk.",
    ".kgugk.",
    ".kgggk.",
    ".kugwk.",
    ".kgggk.",
    "..kgk..",
    "kkuuukk",
    "kuUUUuk",
    ".kkkkk.",
    "..kBk..",
    "..kBk..",
    "..kkk..",
]

# ------------------- ZUMBI CORROMPIDO -------------------

Z_HEAD = [
    ".kkkkkkkkk..",
    "kzzzzzzzzzk.",
    "kzzzzzzzzzzk",
    "kzZzzkkkzzzk",
    "kzzzzkvkzzzk",
    "kZzzzkkkzzzk",
    "kzzzzzzzzzk.",
    "kZZzkkkkzzk.",
    ".kkzxxxxzk..",
    "..kkkkkkk...",
]

Z_BODY = [
    "..kkkkkkkkkkkkk..",
    ".kzzzzzzzzzzzzzk.",
    "kzvvzzzzzzzzzzzzk",
    "kvvvzzzzzzzzwwzzk",
    "kzvzzzzzzzzkwkzzk",
    "kzzzzzzzzzzzwwzzk",
    "kZzzzzzzzzzzzzzZk",
    "kZzzzzzzzzzzzzZk.",
    ".kZZzzzzzzzzzZk..",
    ".kxZZzzzzzzZZxk..",
    "..kkxxZZZZxxkk...",
    "...kkkkkkkkkk....",
]

Z_ARM = [
    "kzzk",
    "kzzk",
    "kZzk",
    "kZzk",
    "kwbk",
    "kwbk",
    "kbbk",
    "kzzk",
    ".kk.",
]

Z_LEG = [
    "kzzk.",
    "kZZk.",
    "kZZk.",
    "kZxxk",
    "kkkkk",
]

# ------------------- GHOUL VELOZ -------------------

G_HEAD = [
    "..kkkkkkkk...",
    ".khhhhhhhhk..",
    "khhhhkkkhhhk.",
    "khhhhkekhhhk.",
    "khHhhkkkhhhhk",
    "khhhhhhhhhhhk",
    "kHhkwkwkwkhhk",
    "khhkkkkkkkhhk",
    ".kHHhhhhhHHk.",
    "..kkkkkkkkk..",
]

G_BODY = [
    "....kvk..kvk....",
    "...khvhkkhvhk...",
    "..khhhhhhhhhhk..",
    ".khhhhhhhhhhhhk.",
    "khhhhhhhhhhhhhhk",
    "khHhhhhhhhhhhhhk",
    "kHhhhhhhhhhhhhHk",
    "kHhhhhhhhhhhhHk.",
    ".kHHhhhhhhhHHk..",
    "..kkHHHHHHHkk...",
    "....kkkkkkk.....",
]

G_ARM = [
    "khhhk.",
    "khhhk.",
    "kHhhk.",
    ".kHhk.",
    ".kHhk.",
    ".khhhk",
    ".kwkwk",
    ".kwkwk",
    "..w.w.",
]

G_LEG = [
    "khhk..",
    "kHhhk.",
    ".kHhk.",
    ".khhk.",
    ".kwwk.",
    ".kkkk.",
]


def blit(img, ox, oy, rows, flicker=0):
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            color = C.get(ch)
            if flicker == 1:
                if ch == "e":
                    color = C["E"]
                elif ch == "E":
                    color = C["e"]
                elif ch == "v":
                    color = C["V"]
                elif ch == "V":
                    color = C["v"]
            if color is not None and 0 <= ox + x < img.width and 0 <= oy + y < img.height:
                px[ox + x, oy + y] = color


def shade(img):
    """Sombra pixelada 16-bit: escurece o lado de trás (direita) e a base
    da silhueta com dithering. Luz vem da esquerda/alto, como no cenário."""
    px = img.load()
    opaque = {
        (x, y)
        for y in range(img.height)
        for x in range(img.width)
        if px[x, y][3] > 10 and px[x, y][:3] not in GLOW and px[x, y][:3] != C["k"][:3]
    }

    def darken(x, y, f):
        r, g, b, a = px[x, y]
        px[x, y] = (int(r * f), int(g * f), int(b * f), a)

    # borda direita de cada linha (lado nas costas da luz) — faixa larga
    rows = {}
    for (x, y) in opaque:
        rows.setdefault(y, []).append(x)
    for y, xs in rows.items():
        mx = max(xs)
        for x in xs:
            if x >= mx - 2:
                darken(x, y, 0.5)
            elif x == mx - 3 and (x + y) % 2 == 0:
                darken(x, y, 0.5)  # dither na transição

    # base de cada coluna (sombra do chão subindo)
    cols = {}
    for (x, y) in opaque:
        cols.setdefault(x, []).append(y)
    for x, ys in cols.items():
        my = max(ys)
        for y in ys:
            if y >= my - 1:
                darken(x, y, 0.55)
            elif y == my - 2 and (x + y) % 2 == 0:
                darken(x, y, 0.55)


def outline(img):
    px = img.load()
    opaque = {(x, y) for y in range(img.height) for x in range(img.width) if px[x, y][3] > 10}
    for (x, y) in opaque:
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < img.width and 0 <= ny < img.height and (nx, ny) not in opaque:
                px[nx, ny] = C["k"]


def draw_skeleton(pose):
    img = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    bx = 18 + pose.get("x", 0)
    by = 31 + pose.get("y", 0)
    lean = pose.get("lean", 0)
    f = pose.get("flicker", 0)
    # pernas encaixadas embaixo das costelas
    blit(img, bx + 4 + pose.get("l", 0), by + 21, S_LEG, f)
    blit(img, bx + 10 + pose.get("r", 0), by + 21, S_LEG, f)
    blit(img, bx + 2 + lean, by + 13, S_RIBS, f)
    blit(img, bx + lean * 2, by, S_SKULL, f)
    # espada enferrujada empunhada ao lado do corpo
    blit(img, bx + lean + 15, by + 5, S_SWORD, f)
    shade(img)
    outline(img)
    return img


def draw_zombie(pose):
    img = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    bx = 18 + pose.get("x", 0)
    by = 31 + pose.get("y", 0)
    lean = pose.get("lean", 0)
    f = pose.get("flicker", 0)
    blit(img, bx + 3 + pose.get("l", 0), by + 21, Z_LEG, f)
    blit(img, bx + 10 + pose.get("r", 0), by + 21, Z_LEG, f)
    # braço de trás pendurado
    blit(img, bx + lean, by + 14, Z_ARM, f)
    blit(img, bx + lean, by + 9, Z_BODY, f)
    # braço da frente pendurado (osso exposto)
    blit(img, bx + 13 + lean, by + 14, Z_ARM, f)
    blit(img, bx + 3 + lean * 2, by, Z_HEAD, f)
    shade(img)
    outline(img)
    return img


def draw_ghoul(pose):
    img = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    bx = 14 + pose.get("x", 0)
    by = 35 + pose.get("y", 0)
    lean = pose.get("lean", 0)
    f = pose.get("flicker", 0)
    # pernas dobradas atrás, embaixo do corpo
    blit(img, bx + 1 + pose.get("l", 0), by + 16, G_LEG, f)
    blit(img, bx + 7 + pose.get("r", 0), by + 16, G_LEG, f)
    # corpo arqueado com espinhos
    blit(img, bx + lean, by + 6, G_BODY, f)
    # braços de garra apoiados no chão, alternam no passo
    blit(img, bx + 13 + pose.get("al", 0), by + 13, G_ARM, f)
    blit(img, bx + 18 + pose.get("ar", 0), by + 12, G_ARM, f)
    # cabeça baixa e projetada pra frente (corcunda)
    blit(img, bx + 14 + lean * 2, by + 12 + pose.get("hy", 0), G_HEAD, f)
    shade(img)
    outline(img)
    return img


# idle A/B + walk A/B (brilho dos olhos pisca entre frames)
POSES = {
    "esqueleto": (
        draw_skeleton,
        [
            {"flicker": 0},
            {"y": 1, "flicker": 1},
            {"lean": 1, "l": -2, "r": 2, "flicker": 0},
            {"lean": 1, "y": 1, "l": 2, "r": -2, "flicker": 1},
        ],
    ),
    "zumbi": (
        draw_zombie,
        [
            {"flicker": 0},
            {"y": 1, "flicker": 1},
            {"lean": -1, "l": -2, "r": 2, "y": 1, "flicker": 0},
            {"lean": 1, "l": 2, "r": -2, "flicker": 1},
        ],
    ),
    "ghoul": (
        draw_ghoul,
        [
            {"flicker": 0},
            {"y": 1, "hy": 1, "flicker": 1},
            {"lean": 1, "l": -3, "r": 3, "al": 2, "ar": -2, "flicker": 0},
            {"lean": 1, "y": 1, "l": 3, "r": -3, "al": -2, "ar": 2, "flicker": 1},
        ],
    ),
}


def build():
    ROOT.mkdir(parents=True, exist_ok=True)
    frames_dir = ROOT / "frames"
    frames_dir.mkdir(exist_ok=True)
    all_frames = {}
    for name, (draw, poses) in POSES.items():
        frames = [draw(p) for p in poses]
        all_frames[name] = frames
        sheet = Image.new("RGBA", (CELL * len(frames), CELL), (0, 0, 0, 0))
        for i, fr in enumerate(frames):
            sheet.alpha_composite(fr, (i * CELL, 0))
        sheet.save(ROOT / f"{name}_sheet.png")
        for i, fr in enumerate(frames):
            fr.save(frames_dir / f"{name}_{i:02d}.png")
        # GIF do ciclo de andar pra validar movimento
        walk = [frames[2], frames[0], frames[3], frames[1]]
        big = [f.resize((CELL * 6, CELL * 6), Image.NEAREST).convert("RGB") for f in walk]
        big[0].save(ROOT / f"{name}_walk.gif", save_all=True, append_images=big[1:], duration=170, loop=0)
    return all_frames


def presentation(all_frames):
    """Lâmina de validação: os 3 inimigos lado a lado, idle + walk."""
    S = 5
    pad = 26
    cw = CELL * S
    names = [("esqueleto", "ESQUELETO GUERREIRO"), ("zumbi", "ZUMBI CORROMPIDO"), ("ghoul", "GHOUL VELOZ")]
    W = pad + len(names) * (cw * 2 + pad)
    H = cw + 90
    out = Image.new("RGB", (W, H), (16, 13, 26))
    d = ImageDraw.Draw(out)
    # faixa de chão na paleta do plano jogável
    d.rectangle([0, H - 58, W, H - 50], fill=(69, 64, 87))
    d.rectangle([0, H - 50, W, H], fill=(40, 37, 56))
    x = pad
    for key, label in names:
        for j, idx in enumerate((0, 2)):  # idle e passo
            fr = all_frames[key][idx].resize((cw, cw), Image.NEAREST)
            out.paste(fr, (x + j * cw, H - 58 - cw + 8), fr)
        d.text((x + cw - len(label) * 3, H - 34), label, fill=(226, 220, 240))
        x += cw * 2 + pad
    out.save(ROOT / "apresentacao.png")
    out.save("/tmp/inimigos_t1.png")


if __name__ == "__main__":
    presentation(build())
    print("ok", ROOT)
