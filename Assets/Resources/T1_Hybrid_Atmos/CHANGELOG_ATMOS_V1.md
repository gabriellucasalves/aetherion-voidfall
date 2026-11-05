# T1_Hybrid_Atmos_V1 — 03/11/2026
Fundo R1 (Etapa A, aprovado) + playfield E2 v2 (plats densas + gaps Mario-like, aprovado).

| Arquivo | Tam. | Conteúdo |
|---|---|---|
| layer_p1.png / sky.png | 768×432 | P1: céu, sol e nuvens. Opaco e full-frame, é o fundo base. |
| layer_p2.png / far.png | 768×432 | P2: montanhas distantes e cidadela flutuante. |
| overlay_p2b_fog.png | 768×432 | Névoa entre as montanhas e o mid. Alpha **parcial** de propósito (overlay atmosférico). Ordem: entre P2 e P3, com o mesmo factor de P2. |
| layer_p3.png / mid.png | 768×432 | P3: flora média (árvores rosas, rochas). |
| overlay_p3b_veil.png | 768×432 | Véu suave sobre o mid. Alpha **parcial**, usa o factor de P3. |
| layer_p4.png / ruins.png | 768×432 | P4: ruínas, pilares, fonte e a faixa verde. |
| layer_p5.png / playfield_p5.png | 768×432 | P5: 3 segmentos de chão, 2 abismos e plats A/B/C. É a referência de composição e de posição dos colliders. |
| ground.png | 512×128 | Strip tileável do chão denso. |
| plat_s/m/l.png | 96×52 / 160×58 / 256×64 | Plats flutuantes densas. |
| preview.png / preview_x2.png | 768 / 1536 | Composite final aprovado (conceito_plats_v2). |
| docs/ | — | Briefs da Direção. |

- Arte densa 1:1 no RT 768×432. **PPU 32** na importação (32 px = 1 u; a tela tem 24×13.5 u, OrthoSize 6.75).
- Filter Mode **Point**, Compression **None**, mipmaps off, sem dither. P1 a P5 com alpha binário (0/255); só os dois overlays têm alpha parcial.
- Para remontar o preview, compor nesta ordem: p1 → p2 → fog → p3 → veil → p4 → p5 (bate com o preview em 99.999%).
