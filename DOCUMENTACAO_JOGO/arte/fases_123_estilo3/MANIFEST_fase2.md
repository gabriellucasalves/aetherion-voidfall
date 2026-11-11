# FASE 2 — Aetherion em destruição e colapso (v3) — MANIFEST de integração

Destino: `Assets/Resources/T2_Palace_Atmos/v3/` (**confirme no projeto** que a Fase 2 carrega de `T2_Palace_Atmos`; criar `v3/` ao lado do que existe, sem apagar).
Subpastas: `v3/camadas/`, `v3/plataformas/`, `v3/tiles/`, `v3/props/`, `v3/fx/`. `previews/` não importar.

## Import (todas as texturas deste pacote)
- Texture Type **Sprite (2D and UI)**, **Pixels Per Unit 32**, **Filter Mode Point (no filter)**, **Compression None**, **Generate Mip Maps OFF**, Alpha Is Transparency ON, sRGB ON.
- **Wrap Mode = Clamp** em tudo, **exceto** o que repete (marcado **Repeat** nas tabelas abaixo).
- FX: Sprite Mode **Multiple**, fatiar Grid By Cell Size com a célula indicada (frames = largura/célula).
- Escala em jogo 1:1 (1 px da arte = 1/32 u). **Linha do chão = y=372** nas camadas de 768×432 → pivô das camadas (0,5; 0,1389) colocado em **GroundTop**.

## Camadas de paralaxe (768×432, **Repeat** X)
Confirme que os fatores atuais da Fase 2 batem; se o builder já tiver valores, mantenha os dele.
| arquivo | px | cores | paralaxe | sorting order | wrap | obs |
|---|---|---|---|---|---|---|
| camadas/P1_ceu_caos_native.png | 768×432 | 16 | 0.08 | -50 | Repeat X | céu em chamas, sem sol, blocos 2×2 |
| camadas/P2_palacio_desabando_native.png | 768×432 | 16 | 0.22 | -40 | Repeat X | palácio desabando, blocos 2×2 |
| camadas/P3_salao_ruinas_native.png | 768×432 | 16 | 0.4 | -30 | Repeat X |  |
| camadas/P4_estatuas_destruidas_native.png | 768×432 | 14 | 0.62 | -20 | Repeat X |  |
| camadas/P4b_nevoa_fumaca_native.png | 768×432 | 1 | 0.62 | -15 | Repeat X | fumaça (1 cor) |
| camadas/P6_silhuetas_frente_native.png | 768×432 | 2 | 1.25 | 20 | Repeat X | **na frente dos heróis** |

## Plataformas (one-way: BoxCollider2D + PlatformEffector2D)
| arquivo | px | topo do colisor (y a partir do topo do PNG) | largura colisor | altura colisor | wrap |
|---|---|---|---|---|---|
| plataformas/plataforma_palacio_128_a.png | 128×73 | y=2 | 124 px (centralizado) | 6 px | Clamp |
| plataformas/plataforma_palacio_128_b.png | 128×75 | y=2 | 124 px (centralizado) | 6 px | Clamp |
| plataformas/plataforma_palacio_160_a.png | 160×95 | y=3 | 156 px (centralizado) | 6 px | Clamp |
| plataformas/plataforma_palacio_160_b.png | 160×95 | y=3 | 156 px (centralizado) | 6 px | Clamp |
| plataformas/plataforma_palacio_96_a.png | 96×54 | y=1 | 92 px (centralizado) | 6 px | Clamp |
| plataformas/plataforma_palacio_96_b.png | 96×56 | y=2 | 92 px (centralizado) | 6 px | Clamp |

## Tiles
| arquivo | px | uso / linha caminhável | wrap |
|---|---|---|---|
| tiles/chao_palacio_128.png | 128×75 | **linha caminhável y=0** (topo do PNG = GroundTop) | Repeat X (128 px) |
| tiles/chao_subsolo_128x64.png | 128×64 | abaixo do chão (começa 75 px abaixo de GroundTop), sem colisor | Repeat X/Y |
| tiles/chao_borda_abismo_dir.png | 48×81 | vão à **direita**; topo do PNG em GroundTop (lip y=0..2, caminhável y=0); sobrepõe 4 px no vão | Clamp |
| tiles/chao_borda_abismo_esq.png | 55×81 | vão à **esquerda**; topo do PNG em GroundTop (caminhável y=0); sobrepõe 4 px no vão | Clamp |
| tiles/abismo_profundo_64x96.png | 64×96 | dentro dos vãos, topo em GroundTop − 0,125 u; sem colisor; Sorting -10 | Repeat X |
| tiles/pedra_caindo_0.png | 14×15 | pedras decorativas caindo nos vãos | Clamp |
| tiles/pedra_caindo_1.png | 13×14 | pedras decorativas caindo nos vãos | Clamp |
| tiles/pedra_caindo_2.png | 16×17 | pedras decorativas caindo nos vãos | Clamp |
| tiles/pedra_caindo_3.png | 29×35 | pedras decorativas caindo nos vãos | Clamp |

Colisores do chão e posições de vãos/plataformas: **os atuais** (só troca visual + topo do colisor das plataformas conforme a tabela).

## Props (sem colisor, pivô base-centro (0,5; 0), Sorting -5, base 3 px abaixo de GroundTop)
| arquivo | px | cores |
|---|---|---|
| props/balaustrada.png | 57×46 | 33 |
| props/braseiro.png | 22×34 | 30 |
| props/candelabro.png | 33×62 | 33 |
| props/cristais_ciano.png | 42×46 | 29 |
| props/entulho.png | 52×40 | 32 |
| props/estandarte_caido.png | 40×36 | 32 |
| props/estatua_cabeca_coroada.png | 45×46 | 33 |
| props/tesouro.png | 45×30 | 30 |
| props/trono_tombado.png | 60×54 | 29 |
| props/urna_quebrada.png | 34×42 | 30 |

## FX ambiente (só visual; **sem dano**, a não ser que já exista hazard naquele ponto)
| arquivo | px | frames | célula | fps | pivô | uso |
|---|---|---|---|---|---|---|
| fx/fx_meteoro_queda_8f_48x48.png | 384×48 | 8 (loop) | 48×48 | 14 | cabeça (13,35) px → **(0,27; 0,27)** | meteoros no FUNDO: nascem fora da tela em cima-direita, caem para baixo-esquerda (espelhar X p/ cair à direita). 1 a cada 1,5–3 s, sorting -25 (entre P3 e P4) |
| fx/fx_impacto_explosao_6f_64x64.png | 384×64 | 6 (uma vez) | 64×64 | 14 | base-centro (32,60) → **(0,5; 0,06)** em GroundTop | às vezes (≈1 em 3 meteoros) o meteoro chega ao chão no meio da tela → explosão puramente visual + leve camera shake opcional; sorting -8 (**atrás das plataformas**) |
| fx/fx_brasas_4f_32x32.png | 128×32 | 4 (loop) | 32×32 | 8 | centro (0,5; 0,5) | brasas subindo, 6–10 instâncias, sorting 15 |
| fx/fx_fumaca_6f_32x32.png | 192×32 | 6 (uma vez) | 32×32 | 10 | base-centro (0,5; 0) | fumaça após cada explosão e em braseiros; sorting -8 (atrás das plataformas) |

## Previews (referência, não importar)
- previews/mock_fase2_v3.png, mock_fase2_v3_b.png — mock aprovado (heróis 1,67× só para ver).
- previews/comparativo_f2v3_f3.png, fx_4x.png.

## Notas
- Correção final: o "xadrez" na plataforma do meio era a fumaça desenhada por cima no mock; agora FX ficam **atrás** das plataformas (asset plataforma_palacio_160_a já é limpo).
