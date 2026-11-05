# Parallax — Atmos V1
| Plano | Arquivo | Factor | Sorting Order |
|---|---|---|---|
| P1 céu | layer_p1 | 0.08 | -50 |
| P2 far + cidadela | layer_p2 | 0.22 | -40 |
| P2b névoa | overlay_p2b_fog | 0.22 | -35 |
| P3 mid | layer_p3 | 0.40 | -30 |
| P3b véu | overlay_p3b_veil | 0.40 | -25 |
| P4 ruínas | layer_p4 | 0.62 | -20 |
| P5 playfield | layer_p5 / tiles | 1.00 | 0 (player 10, inimigos 9, HUD em Canvas) |

- Factor = fração do deslocamento da câmera aplicada à camada (1.0 acompanha o mundo).
- Para P1–P4 em loop horizontal, usar 2–3 cópias lado a lado com largura de 24 u (768 px a PPU 32).
- PixelPresentation: RT 768×432 com Point e OrthoSize 6.75 (já existe desde a Etapa 1; não alterar).
- Fazer snap da posição da camada para 1/32 u para evitar shimmer.
