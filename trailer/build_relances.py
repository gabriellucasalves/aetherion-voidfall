#!/usr/bin/env python3
"""Gera relances do mago, arqueiro e naves no espaço para o trailer."""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageOps

ROOT = Path(__file__).resolve().parent
MEDIA = ROOT / "media"
WEB = ROOT.parent / "webgl" / "trailer" / "media"
ASSETS = Path.home() / ".cursor/projects/Users-gabriellucas-Desktop-trabalho-jogo-sexta/assets"
W, H = 1280, 720


def cutout(path: Path, bg: str = "black", tol: int = 28) -> Image.Image:
    im = Image.open(path).convert("RGBA")
    arr = np.array(im)
    r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]
    if bg == "black":
        mask = (r.astype(np.int16) + g.astype(np.int16) + b.astype(np.int16)) <= tol * 3
    else:
        mask = (
            (np.abs(r.astype(np.int16) - 180) < 50)
            & (np.abs(g.astype(np.int16) - 180) < 50)
            & (np.abs(b.astype(np.int16) - 180) < 50)
        )
        mask |= (
            (r > 150)
            & (g > 150)
            & (b > 150)
            & (np.abs(r.astype(int) - g.astype(int)) < 12)
            & (np.abs(g.astype(int) - b.astype(int)) < 12)
        )
    arr[:, :, 3] = np.where(mask, 0, a)
    out = Image.fromarray(arr)
    bbox = out.getbbox()
    return out.crop(bbox) if bbox else out


def cover(im: Image.Image, size=(W, H), zoom: float = 1.0) -> Image.Image:
    tw, th = int(size[0] * zoom), int(size[1] * zoom)
    src_w, src_h = im.size
    scale = max(tw / src_w, th / src_h)
    nw, nh = max(1, int(src_w * scale)), max(1, int(src_h * scale))
    resized = im.resize((nw, nh), Image.Resampling.LANCZOS)
    left = (nw - tw) // 2
    top = (nh - th) // 2
    cropped = resized.crop((left, top, left + tw, top + th))
    return cropped.resize(size, Image.Resampling.LANCZOS)


def place(base, sprite, xy, height=None, shadow=True):
    sp = sprite.copy()
    if height:
        ratio = height / sp.size[1]
        sp = sp.resize((max(1, int(sp.size[0] * ratio)), height), Image.Resampling.NEAREST)
    x, y = xy
    if shadow:
        sh = Image.new("RGBA", sp.size, (0, 0, 0, 0))
        alpha = sp.split()[-1]
        sh.putalpha(alpha.point(lambda v: int(v * 0.45)))
        base.alpha_composite(sh, (x + 6, y + 8))
    base.alpha_composite(sp, (x, y))
    return base


def vignette(im: Image.Image) -> Image.Image:
    overlay = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(overlay)
    for i in range(90):
        d.rectangle([i, i, W - 1 - i, H - 1 - i], outline=(0, 0, 0, int(i * 1.8)))
    return Image.alpha_composite(im, overlay)


def orbs(draw, cx, cy, n=5):
    for i in range(n):
        ang = i * 1.1
        radius = 40 + i * 18
        x = int(cx + radius * np.cos(ang))
        y = int(cy + radius * np.sin(ang) * 0.55)
        col = (180, 90, 255, 210) if i % 2 == 0 else (120, 40, 220, 180)
        draw.ellipse([x - 6, y - 6, x + 6, y + 6], fill=col)
        draw.ellipse([x - 2, y - 2, x + 2, y + 2], fill=(255, 220, 255, 255))


def arrows(draw, x0, y0, n=4):
    for i in range(n):
        x = x0 + 30 + i * 55
        y = y0 - 10 - i * 8
        draw.line([(x0 + 20, y0), (x, y)], fill=(120, 220, 255, 200), width=2)
        draw.polygon([(x, y), (x - 10, y - 4), (x - 10, y + 4)], fill=(255, 220, 80, 240))


def lasers(draw, pts, color=(80, 255, 255, 220)):
    for x1, y1, x2, y2 in pts:
        draw.line([(x1, y1), (x2, y2)], fill=color, width=3)
        draw.ellipse([x2 - 3, y2 - 3, x2 + 3, y2 + 3], fill=(255, 255, 255, 230))


def draw_pixel_enemies(draw: ImageDraw.ImageDraw, enemies: list[tuple[int, int, str, float]]) -> None:
    """Silhuetas inimigas simples (pixel art) no lado direito da cena."""
    for x, y, eye, scale in enemies:
        s = max(1, int(scale))
        dark = (22, 16, 24, 255)
        shade = (14, 10, 18, 255)
        body = (38, 22, 28, 255) if eye == "red" else (32, 18, 52, 255)
        eye_col = (230, 45, 55, 255) if eye == "red" else (190, 70, 255, 255)
        # cabeça
        draw.rectangle([x, y, x + 18 * s, y + 14 * s], fill=body, outline=dark)
        # corpo
        draw.polygon(
            [
                (x + 2 * s, y + 12 * s),
                (x + 22 * s, y + 12 * s),
                (x + 26 * s, y + 38 * s),
                (x + 20 * s, y + 50 * s),
                (x + 4 * s, y + 50 * s),
                (x - 2 * s, y + 38 * s),
            ],
            fill=shade,
            outline=dark,
        )
        # pernas
        draw.rectangle([x + 4 * s, y + 46 * s, x + 10 * s, y + 58 * s], fill=body)
        draw.rectangle([x + 14 * s, y + 46 * s, x + 20 * s, y + 58 * s], fill=body)
        # olhos
        draw.rectangle([x + 3 * s, y + 4 * s, x + 7 * s, y + 8 * s], fill=eye_col)
        draw.rectangle([x + 11 * s, y + 4 * s, x + 15 * s, y + 8 * s], fill=eye_col)


def enemy_blob(size=48, hue="red"):
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    body, eye = ((90, 20, 30, 255), (255, 80, 80, 255)) if hue == "red" else ((40, 20, 70, 255), (200, 80, 255, 255))
    d.polygon(
        [(size * 0.15, size * 0.5), (size * 0.85, size * 0.2), (size * 0.95, size * 0.5), (size * 0.85, size * 0.8)],
        fill=body,
    )
    d.ellipse([size * 0.45, size * 0.35, size * 0.7, size * 0.6], fill=eye)
    d.rectangle([0, size * 0.4, size * 0.2, size * 0.6], fill=(255, 60, 40, 200))
    return im


def save_both(im: Image.Image, name: str):
    rgb = im.convert("RGB")
    rgb.save(MEDIA / name, quality=95)
    WEB.mkdir(parents=True, exist_ok=True)
    rgb.save(WEB / name, quality=95)


def main():
    mago = cutout(ASSETS / "mago-04ac8ac0-c0f3-4495-9e29-33f9f10b02cf.png", "black")
    arqueiro = cutout(ASSETS / "arqueiro-406d6f30-affa-4172-95f4-a148036d3670.png", "gray")
    nave_g = cutout(ASSETS / "nave_2-e3707c5c-952c-4c84-bd33-3d1956cbadce.png", "black")
    nave_a = cutout(ASSETS / "nave1-19e1c654-f616-4506-856a-3d627b65202a.png", "black")
    nave_m = cutout(ASSETS / "nave_3-7333f040-ef67-4b5c-9e81-1146f3a0b421.png", "black")

    for name, sp in [
        ("sprite-mago.png", mago),
        ("sprite-arqueiro.png", arqueiro),
        ("sprite-nave-guerreiro.png", nave_g),
        ("sprite-nave-anjo.png", nave_a),
        ("sprite-nave-mago.png", nave_m),
    ]:
        sp.save(MEDIA / name)
        sp.save(WEB / name)

    ruinas = cover(Image.open(MEDIA / "trailer-ruinas.png").convert("RGBA"), zoom=1.05)
    gameplay = cover(Image.open(MEDIA / "trailer-gameplay.png").convert("RGBA"), zoom=1.08)

    # Mago
    frame = Image.blend(ruinas, gameplay, 0.35)
    frame = ImageEnhance.Color(frame).enhance(0.85)
    frame = ImageEnhance.Contrast(frame).enhance(1.1)
    frame = Image.alpha_composite(frame, Image.new("RGBA", (W, H), (10, 0, 20, 110)))
    place(frame, mago, (90, 140), height=480)
    d = ImageDraw.Draw(frame, "RGBA")
    draw_pixel_enemies(
        d,
        [
            (820, 380, "purple", 2),
            (960, 410, "red", 2),
            (890, 490, "purple", 1),
        ],
    )
    orbs(d, 420, 260, 6)
    save_both(vignette(frame), "trailer-mago.png")

    # Arqueiro
    frame = cover(Image.open(MEDIA / "trailer-ruinas.png").convert("RGBA"), zoom=1.12)
    arr = np.array(frame).astype(np.int16)
    arr[:, :, 0] = (arr[:, :, 0] * 0.85).clip(0, 255)
    arr[:, :, 2] = np.minimum(255, arr[:, :, 2] + 25)
    frame = Image.fromarray(arr.astype(np.uint8))
    frame = Image.alpha_composite(frame, Image.new("RGBA", (W, H), (0, 10, 30, 90)))
    place(frame, arqueiro, (160, 120), height=500)
    d = ImageDraw.Draw(frame, "RGBA")
    for fx, fy in [(520, 180), (560, 240), (600, 160), (540, 300), (620, 280)]:
        d.ellipse([fx, fy, fx + 8, fy + 14], fill=(240, 240, 255, 180))
    arrows(d, 520, 320, 5)
    draw_pixel_enemies(
        d,
        [
            (900, 420, "red", 2),
            (1040, 440, "purple", 2),
            (980, 520, "red", 1),
        ],
    )
    d.ellipse([880, 300, 940, 360], fill=(255, 230, 120, 70))
    save_both(vignette(frame), "trailer-arqueiro.png")

    # Trio
    frame = cover(Image.open(MEDIA / "trailer-guerreiro.png").convert("RGBA"), zoom=1.05)
    frame = Image.alpha_composite(frame, Image.new("RGBA", (W, H), (0, 0, 0, 100)))
    place(frame, mago, (40, 180), height=420)
    place(frame, arqueiro, (880, 150), height=440)
    save_both(vignette(frame), "trailer-herois.png")

    # Naves formação
    base = cover(Image.open(MEDIA / "trailer-espaco.png").convert("RGBA"), zoom=1.15)
    sky_src = MEDIA / "sky.png" if (MEDIA / "sky.png").exists() else MEDIA / "trailer-ceu.png"
    sky = cover(Image.open(sky_src).convert("RGBA"), zoom=1.2)
    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    for y in range(H):
        md.line([(0, y), (W, y)], fill=int(255 * max(0, 1 - y / (H * 0.75))))
    frame = Image.blend(base, Image.composite(sky, base, mask), 0.55)
    frame = Image.alpha_composite(frame, Image.new("RGBA", (W, H), (5, 0, 20, 70)))
    place(frame, nave_a, (80, 160), height=150)
    place(frame, nave_m, (140, 300), height=160)
    place(frame, nave_g, (60, 450), height=170)

    ha = np.array(nave_g)
    rgb = ha[:, :, :3].astype(np.int16)
    ha[:, :, 0] = np.clip(rgb[:, :, 0] * 0.2 + 20, 0, 255)
    ha[:, :, 1] = np.clip(rgb[:, :, 1] * 0.6 + 80, 0, 255)
    ha[:, :, 2] = np.clip(rgb[:, :, 2] * 0.3 + 40, 0, 255)
    hostile = ImageOps.mirror(Image.fromarray(ha.astype(np.uint8)))

    for pos, hgt in [((920, 120), 110), ((1000, 280), 130), ((880, 420), 120), ((1080, 500), 100)]:
        place(frame, hostile, pos, height=hgt, shadow=False)

    e1, e2 = enemy_blob(40, "red"), enemy_blob(36, "purple")
    for pos in [(760, 200), (820, 340), (780, 520), (1140, 180), (1180, 380)]:
        frame.alpha_composite(e1 if pos[1] % 100 < 50 else e2, pos)

    d = ImageDraw.Draw(frame, "RGBA")
    lasers(d, [(280, 220, 900, 160), (340, 360, 980, 300), (280, 520, 860, 450)])
    lasers(d, [(920, 180, 400, 200), (1000, 320, 420, 340)], (255, 80, 80, 180))
    for ex, ey in [(860, 150), (980, 300), (840, 440)]:
        d.ellipse([ex - 18, ey - 18, ex + 18, ey + 18], fill=(255, 180, 60, 160))
        d.ellipse([ex - 8, ey - 8, ex + 8, ey + 8], fill=(255, 255, 200, 220))
    save_both(vignette(frame), "trailer-naves.png")

    # Combate próximo
    frame = cover(Image.open(MEDIA / "trailer-espaco.png").convert("RGBA"), zoom=1.25)
    frame = Image.alpha_composite(frame, Image.new("RGBA", (W, H), (0, 0, 15, 100)))
    place(frame, nave_m, (200, 250), height=220)
    place(frame, hostile, (820, 200), height=180, shadow=False)
    place(frame, hostile, (900, 400), height=160, shadow=False)
    d = ImageDraw.Draw(frame, "RGBA")
    for i in range(7):
        ang = -0.5 + i * 0.16
        x2 = int(480 + 500 * np.cos(ang))
        y2 = int(340 + 500 * np.sin(ang))
        d.line([(480, 340), (x2, y2)], fill=(180, 100, 255, 200), width=2)
    for ex, ey in [(780, 260), (860, 360), (920, 220)]:
        d.ellipse([ex - 14, ey - 14, ex + 14, ey + 14], fill=(200, 120, 255, 140))
    save_both(vignette(frame), "trailer-naves-combate.png")

    print("relances gerados em trailer/media e webgl/trailer/media")


if __name__ == "__main__":
    main()
