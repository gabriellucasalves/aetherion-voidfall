#!/usr/bin/env python3
"""Render the Aetherion Apocalipse trailer to a shareable MP4."""
from __future__ import annotations

import math
import struct
import subprocess
import wave
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent
MEDIA = ROOT / "media"
OUT = ROOT / "Aetherion-Apocalipse-Trailer.mp4"
AUDIO = ROOT / "_trailer_audio.wav"
FONT_PATH = ROOT / "fonts/PressStart2P-Regular.ttf"

W, H = 1280, 720
FPS = 24

SCENES = [
    dict(img="trailer-ceu.png", caption="Por milhares de anos, o Véu protegeu Aetherion.", hold=5.8, flash=False, title=False),
    dict(img="trailer-veu.png", caption="Até que algo despertou além das estrelas.", hold=5.6, flash=False, title=False),
    dict(img="trailer-veu.png", caption="O Véu foi destruído.", hold=3.8, flash=True, title=False),
    dict(img="trailer-ruinas.png", caption="Aetherion está caindo. Suas torres viraram ruínas.", hold=5.2, flash=False, title=False),
    dict(img="trailer-guerreiro.png", caption="Na chuva e no silêncio, um guerreiro se levanta.", hold=6.0, flash=False, title=False),
    dict(img="trailer-inimigos.png", caption="Seus mortos não descansam.", hold=5.8, flash=False, title=False),
    dict(img="trailer-gameplay.png", caption="Espada contra ossos. Escudo contra o Vazio.", hold=6.4, flash=False, title=False),
    dict(img="trailer-mago.png", caption="Orbes arcanos rasgam a noite.", hold=6.0, flash=False, title=False),
    dict(img="trailer-arqueiro.png", caption="Penas de luz atravessam o Véu.", hold=5.8, flash=False, title=False),
    dict(img="trailer-herois.png", caption="Três destinos. Uma última linha.", hold=6.2, flash=False, title=False),
    dict(img="trailer-boss-terra.png", caption="Nas ruínas desperta Vorthak.", hold=6.2, flash=False, title=False),
    dict(img="trailer-boss-terra-relance.png", caption="O Olhar Eterno rasga a pedra.", hold=5.4, flash=True, title=False),
    dict(img="trailer-espaco.png", caption="A guerra não termina na terra.", hold=6.2, flash=False, title=False),
    dict(img="trailer-naves.png", caption="Depois da terra, o céu vira campo de guerra.", hold=6.2, flash=False, title=False),
    dict(img="trailer-naves-combate.png", caption="Três naves. Uma frota do Vazio.", hold=6.0, flash=False, title=False),
    dict(img="trailer-boss-espaco.png", caption="Nas estrelas espera Morveth.", hold=5.6, flash=False, title=False),
    dict(img="trailer-boss-espaco-relance.png", caption="O Encapuzado abre as garras.", hold=6.0, flash=False, title=False),
    dict(img="trailer-titulo.png", caption="Dois senhores. Uma última resistência.", hold=7.4, flash=False, title=True),
]

INTRO = 2.0
OUTRO = 3.2


def load_font(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT_PATH), size)


def wrap_text(text: str, font: ImageFont.ImageFont, max_width: int, draw: ImageDraw.ImageDraw) -> list[str]:
    words = text.split()
    lines, cur = [], ""
    for word in words:
        trial = word if not cur else cur + " " + word
        if draw.textlength(trial, font=font) <= max_width:
            cur = trial
        else:
            if cur:
                lines.append(cur)
            cur = word
    if cur:
        lines.append(cur)
    return lines or [text]


def cover(im: Image.Image, zoom: float) -> Image.Image:
    tw, th = int(W * zoom), int(H * zoom)
    src_w, src_h = im.size
    scale = max(tw / src_w, th / src_h)
    nw, nh = max(1, int(src_w * scale)), max(1, int(src_h * scale))
    resized = im.resize((nw, nh), Image.Resampling.NEAREST)
    left = (nw - tw) // 2
    top = (nh - th) // 2
    cropped = resized.crop((left, top, left + tw, top + th))
    return cropped.resize((W, H), Image.Resampling.NEAREST)


def scanlines(im: Image.Image) -> Image.Image:
    arr = np.array(im)
    arr[1::3] = (arr[1::3].astype(np.uint16) * 72 // 100).clip(0, 255).astype(np.uint8)
    return Image.fromarray(arr)


def vignette(im: Image.Image) -> Image.Image:
    y, x = np.ogrid[:H, :W]
    cx, cy = W / 2, H / 2
    d = np.sqrt(((x - cx) / (W * 0.72)) ** 2 + ((y - cy) / (H * 0.78)) ** 2)
    mask = np.clip(1.15 - d, 0.35, 1.0)
    arr = np.array(im).astype(np.float32)
    arr *= mask[..., None]
    return Image.fromarray(arr.clip(0, 255).astype(np.uint8))


def draw_caption(base: Image.Image, text: str, font: ImageFont.ImageFont) -> None:
    if not text:
        return
    draw = ImageDraw.Draw(base)
    lines = wrap_text(text, font, W - 140, draw)
    line_h = 28
    y = H - 92 - line_h * (len(lines) - 1)
    for line in lines:
        tw = draw.textlength(line, font=font)
        x = (W - tw) / 2
        draw.text((x + 3, y + 3), line, font=font, fill=(0, 0, 0))
        draw.text((x, y), line, font=font, fill=(239, 199, 95))
        y += line_h


def draw_title(base: Image.Image, big: ImageFont.ImageFont, small: ImageFont.ImageFont) -> None:
    overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    g = ImageDraw.Draw(shade)
    g.rectangle((0, int(H * 0.42), W, H), fill=(0, 0, 0, 170))
    overlay = Image.alpha_composite(overlay, shade)
    d = ImageDraw.Draw(overlay)
    lines = ["AETHERION", "APOCALIPSE"]
    y = 250
    for line in lines:
        tw = d.textlength(line, font=big)
        x = (W - tw) / 2
        d.text((x + 4, y + 4), line, font=big, fill=(58, 34, 8, 255))
        d.text((x, y), line, font=big, fill=(239, 199, 95, 255))
        y += 58
    sub = "O Véu caiu. O Vazio avança."
    tw = d.textlength(sub, font=small)
    d.text(((W - tw) / 2, y + 18), sub, font=small, fill=(243, 234, 208, 230))
    base.paste(Image.alpha_composite(base.convert("RGBA"), overlay).convert("RGB"))


def hud(base: Image.Image, t: float, font: ImageFont.ImageFont) -> None:
    draw = ImageDraw.Draw(base)
    s = int(t)
    clock = f"00:{s:02d}"
    play = "PLAY" if int(t * 2) % 2 == 0 else "    "
    draw.text((24, 18), f"> {play}", font=font, fill=(239, 199, 95))
    draw.text((W / 2 - 36, 18), clock, font=font, fill=(239, 199, 95))
    draw.text((W - 170, 18), "VHS  198X", font=font, fill=(239, 199, 95))


def card(text_lines: list[str], fonts: tuple) -> Image.Image:
    im = Image.new("RGB", (W, H), (5, 3, 8))
    draw = ImageDraw.Draw(im)
    big, small = fonts
    y = 280
    for i, line in enumerate(text_lines):
        font = big if i == 0 else small
        fill = (239, 199, 95) if i == 0 else (200, 190, 168)
        tw = draw.textlength(line, font=font)
        draw.text(((W - tw) / 2, y), line, font=font, fill=fill)
        y += 48 if i == 0 else 28
    return scanlines(vignette(im))


def generate_audio(total: float) -> None:
    sr = 44100
    n = int(sr * total)
    t = np.arange(n) / sr
    melody = np.array([146.83, 174.61, 164.81, 130.81, 155.56, 146.83, 110.00, 130.81])
    step = 0.72
    idx = (t / step).astype(int)
    freq = melody[idx % len(melody)]
    bass_f = np.where((idx % 4) < 2, 36.71, 43.65)
    lead = np.sign(np.sin(2 * np.pi * freq * t)) * 0.06
    bass = (2 * np.abs(2 * ((t * bass_f) % 1) - 1) - 1) * 0.16
    pad = np.sin(2 * np.pi * (freq * 0.5) * t) * 0.05
    fade = np.minimum(t / 0.5, 1.0) * np.minimum((total - t) / 1.2, 1.0)
    fade = np.clip(fade, 0, 1)
    mix = np.clip((lead + bass + pad) * fade * 0.9, -1, 1)
    pcm = (mix * 32767).astype(np.int16)
    with wave.open(str(AUDIO), "w") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(sr)
        wf.writeframes(pcm.tobytes())


def typed(caption: str, local_t: float) -> str:
    chars = int(local_t / 0.026)
    return caption[: max(0, chars)]


def render() -> None:
    font_cap = load_font(16)
    font_hud = load_font(12)
    font_title = load_font(32)
    font_sub = load_font(14)
    font_intro = load_font(28)

    cache: dict[str, Image.Image] = {}
    for scene in SCENES:
        p = MEDIA / scene["img"]
        cache[scene["img"]] = Image.open(p).convert("RGB")

    duration = INTRO + sum(s["hold"] for s in SCENES) + OUTRO
    generate_audio(duration)

    import imageio_ffmpeg

    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    cmd = [
        ffmpeg, "-y",
        "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS),
        "-i", "-",
        "-i", str(AUDIO),
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-preset", "medium", "-crf", "19",
        "-c:a", "aac", "-b:a", "160k",
        "-shortest", "-movflags", "+faststart",
        str(OUT),
    ]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stderr=subprocess.PIPE)

    nframes = int(duration * FPS)
    intro_frames = int(INTRO * FPS)
    outro_start = nframes - int(OUTRO * FPS)

    intro_img = card(
        ["AETHERION: APOCALIPSE", "TRAILER OFICIAL", "DARK FANTASY 16-BIT"],
        (font_intro, font_sub),
    )
    outro_img = card(
        ["AETHERION: APOCALIPSE", "Ainda existem aqueles", "dispostos a lutar."],
        (font_intro, font_sub),
    )

    scene_starts = []
    acc = INTRO
    for scene in SCENES:
        scene_starts.append(acc)
        acc += scene["hold"]

    try:
        for i in range(nframes):
            t = i / FPS
            if i < intro_frames:
                frame = intro_img.copy()
            elif i >= outro_start:
                frame = outro_img.copy()
            else:
                idx = 0
                for s_i, start in enumerate(scene_starts):
                    if t >= start:
                        idx = s_i
                scene = SCENES[idx]
                local = t - scene_starts[idx]
                zoom = 1.12 - 0.12 * min(1.0, local / scene["hold"])
                frame = cover(cache[scene["img"]], zoom)
                if scene["flash"] and local < 0.45:
                    pulse = 0.85 if local < 0.12 or 0.22 < local < 0.32 else 0.15
                    flash_im = Image.new("RGB", (W, H), (255, 255, 255))
                    frame = Image.blend(frame, flash_im, pulse)
                frame = scanlines(vignette(frame))
                draw_caption(frame, typed(scene["caption"], local), font_cap)
                if scene["title"] and local > 0.6:
                    draw_title(frame, font_title, font_sub)
                hud(frame, t, font_hud)
            proc.stdin.write(np.asarray(frame, dtype=np.uint8).tobytes())
            if i % 48 == 0:
                print(f"frame {i}/{nframes} ({100 * i / nframes:.0f}%)", flush=True)
        proc.stdin.close()
        err = proc.stderr.read().decode("utf-8", "ignore")
        code = proc.wait()
        if code != 0:
            raise RuntimeError(err[-2000:])
    finally:
        if proc.stdin:
            try:
                proc.stdin.close()
            except Exception:
                pass

    AUDIO.unlink(missing_ok=True)
    print("wrote", OUT, "size", OUT.stat().st_size)


if __name__ == "__main__":
    render()
