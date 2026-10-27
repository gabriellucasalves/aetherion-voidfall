"""QA — prévia 1× da Fase I com o fundo V4.1 e o kit da frente no layout do T1FrontLevel.

Reproduz as posições/paralaxes do T1HybridScenery numa tela de 384×216 (resolução
interna do jogo) e salva também uma ampliação 3× em nearest só para visualização.
O layout abaixo espelha T1FrontLevel.Segments / Plats (manter em sincronia).

Uso: python3 mockup_frente.py <pasta T1_Hybrid> <pasta saída> [camX ...]
"""
import sys
from pathlib import Path

from PIL import Image

PPU, VW, VH = 16, 384, 216
GROUND_TOP = -2.35
ORTHO = VH / (PPU * 2)
REST_CAM_Y = GROUND_TOP + ORTHO * 0.55
GROUND_SURF, PLAT_SURF, EDGE_W, EDGE_OVERLAP = 8, 4, 24, 8
PIT_TUCK = 4 / PPU  # abismo passa por baixo da parede irregular das beiradas

SEGMENTS = [(-40, -26), (-24, -17), (-14, -7), (-4, 13), (16, 24), (27, 40)]
PLATS = [("plat_s", -15.5, 2.0), ("plat_m", -11.0, 3.75), ("plat_m", -5.5, 2.0),
         ("plat_s", 14.5, 2.0), ("plat_m", 19.0, 3.75), ("plat_s", 25.5, 2.0), ("plat_l", 31.0, 3.75)]
CLOUDS = [("clouds_a", -8, 2.8, 0.92), ("clouds_c", -4.5, 5.2, 0.90), ("clouds_d", 4, 5.4, 0.88),
          ("clouds_b", 8.5, 2.3, 0.90)]
WORLD_EDGE = 36


def main():
    src, out = Path(sys.argv[1]), Path(sys.argv[2])
    cams = [float(v) for v in sys.argv[3:]] or [-12.0, 14.0]
    out.mkdir(parents=True, exist_ok=True)
    load = lambda n: Image.open(src / f"{n}.png").convert("RGBA")
    hero = Image.open(src.parent / "Guerreiro/sheet.png").convert("RGBA").crop((0, 0, 64, 64))
    hero = hero.crop(hero.getbbox())
    for cam_x in cams:
        cam_y = REST_CAM_Y
        img = Image.new("RGBA", (VW, VH), (122, 186, 237, 255))

        def blit(sprite, x, y_bottom, flip=False):
            """x = centro (mundo), y_bottom = base do sprite (pivô 0,5;0)."""
            if flip:
                sprite = sprite.transpose(Image.FLIP_LEFT_RIGHT)
            px = round((x - cam_x) * PPU + VW / 2 - sprite.width / 2)
            py = round(VH / 2 - (y_bottom - cam_y) * PPU - sprite.height)
            img.alpha_composite(sprite, (px, py)) if -sprite.width < px < VW and -sprite.height < py < VH else None

        def tiled(sprite, x0, x1, y_bottom):
            x = x0
            while x < x1 - 1e-6:
                w = min(sprite.width, round((x1 - x) * PPU))
                part = sprite.crop((0, 0, w, sprite.height))
                blit(part, x + w / PPU / 2, y_bottom)
                x += w / PPU

        blit(load("sky"), cam_x * 0.95, cam_y * 0.95 + (-3.0 - 0.95 * REST_CAM_Y))
        for name, x, y, f in CLOUDS:
            blit(load(name), x + cam_x * f, y + (cam_y - REST_CAM_Y) * f)
        blit(load("far"), cam_x * 0.9, -1.9)
        blit(load("city"), cam_x * 0.8, -3.7)

        ground, pit = load("ground"), load("pit")
        el, er = load("ground_edge_l"), load("ground_edge_r")
        y_ground = GROUND_TOP + GROUND_SURF / PPU - ground.height / PPU
        for (a0, a1), (b0, _) in zip(SEGMENTS, SEGMENTS[1:]):
            tiled(pit, a1 - PIT_TUCK, b0 + PIT_TUCK, y_ground)
        for x0, x1 in SEGMENTS:
            open_l, open_r = x0 <= -WORLD_EDGE, x1 >= WORLD_EDGE
            m0 = x0 if open_l else x0 + 1
            m1 = x1 if open_r else x1 - 1
            tiled(ground, m0, m1, y_ground)
            if not open_l:
                blit(el, x0 + EDGE_W / PPU / 2, y_ground)
            if not open_r:
                blit(er, x1 - EDGE_W / PPU / 2, y_ground)
        for kind, x, h in PLATS:
            spr = load(kind)
            blit(spr, x, GROUND_TOP + h + PLAT_SURF / PPU - spr.height / PPU)
        blit(hero, cam_x - 4.5, GROUND_TOP)
        name = f"plataformas_frente_cam{cam_x:+.0f}"
        img.save(out / f"{name}_1x.png")
        img.resize((VW * 3, VH * 3), Image.NEAREST).save(out / f"{name}_3x_visualizacao.png")
        print(out / f"{name}_1x.png")


if __name__ == "__main__":
    main()
