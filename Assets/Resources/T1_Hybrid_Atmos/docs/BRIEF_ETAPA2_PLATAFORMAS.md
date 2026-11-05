# Aetherion: Voidfall — Etapa 2: Plataformas (playfield P5 densificado)

## Objetivo
Densificar só a ARTE do playfield P5 (ground + plats) com riqueza 16-bit (Owlboy/Chrono Trigger) sobre o fundo R1 aprovado. A geometria do level não muda: os gaps, as posições e os topos de colisão ficam iguais.

## Kit
| Peça | Legado (PPU16) | Denso 2× (PPU32) | Arquivo preview |
|---|---|---|---|
| ground | ~256×64 | 512×128 | kit_ground.png |
| plat_s | ~48×24 | 96×~52 | kit_plat_s.png |
| plat_m | ~80×30 | 160×~58 | kit_plat_m.png |
| plat_l | ~128×36 | 256×~64 | kit_plat_l.png |
| edges L/R | — | lábio de musgo de 2–3 px + cap esquerdo quente / cap direito frio (vem do recorte de plat_*) | — |

Com o RT 768×432 e OrthoSize 6.75, 1 unidade = 32 px. Em PPU 32, as peças 2× ocupam o mesmo tamanho no mundo que as antigas em PPU 16. A diferença é que a arte passa a mapear 1:1 no RT. Filtro Point, sem compressão e sem mipmaps (fica para a importação, fora deste bot).

## Materiais / paleta Passado Intacta (21 cores fixas, sem dither, alpha binário)
- **Topo:** musgo lavanda em 4–8 px, em 5 tons (m0..m4). Tem tufos de 1–3 px, gotas de musgo e a borda de contato escura sob o lábio.
- **Corpo:** pedra ardósia em blocos (r0..r4), com junta escura, luz em cima à esquerda, sombra embaixo à direita em cada pedra e veios claros e teal.
- **Underside:** gradiente para ardósia fria escura (cold2), com musgo pendente e cristal rosa pontual (c0..c2), em ~2–3% das colunas.
- **Luz:** o sol vem da esquerda. Highlight quente de 2 px na borda esquerda e no topo (warm/warm2) e sombra fria de 2 px à direita (cold/cold2), igual ao R1.
- **Silhueta:** contorno de 1 px em `#1C1628`, para que o primeiro plano tenha o maior contraste da cena e fique legível contra o mid e o céu.

## Critérios de aceite
1. Os topos de colisão batem pixel a pixel com o R1 P5: ground no y≥392, largura total; plat A x120–200 com topo em y256; plat B x520–600 com topo em y236.
2. Nenhum gap novo e nenhum gap fechado.
3. P1–P4 do R1 sem nenhuma alteração.
4. Sem dither, alpha só 0/255, no máximo 21 cores no P5.
5. A leitura do personagem e dos obstáculos se mantém: o P5 é o plano mais escuro e contrastado.
6. Em Point ×2, nenhum bloco chapado maior que ~6×6 px no topo.

## Fora de escopo
Redesenho de sky/far/mid/ruins/cidadela; mudança de gaps ou layout; Unity, git, Vercel, On-Demand, pack de importação e prompts.

## Layout Mario-like (v2, vigente)
O viewport mostra o trecho típico: 3 segmentos de chão com topo em y392 (x0–240 / x320–460 / x560–768) e 2 gaps mortais (80 px e 100 px), com abismo quase preto (#0A0B12–#12141C) até a base da tela. Acima ficam as plats de rota de subida: A x200–280 com topo em y300, B x480–580 com topo em y270, C x620–700 com topo em y240. O level completo pode ter mais gaps; o conceito ilustra só a linguagem. Este layout substitui o critério de aceite 1 (o chão contínuo era do v1).
