# FASE 1 — Aetherion intacta (estilo da Fase 3) — MANIFEST de integração

Destino: `Assets/Resources/<pasta de Resources existente da Fase 1>/v3/` (**confirme no projeto** o nome da pasta atual da Fase 1, p.ex. a que o `T1AtmosScenery.cs` carrega; criar a subpasta `v3/` ao lado do que existe, sem apagar os antigos).
Subpastas: `v3/camadas/`, `v3/plataformas/`, `v3/tiles/`, `v3/props/`, `v3/fx/`. `previews/` é só referência — **não importar**.

## Import (todas as texturas deste pacote)
- Texture Type **Sprite (2D and UI)**, **Pixels Per Unit 32**, **Filter Mode Point (no filter)**, **Compression None**, **Generate Mip Maps OFF**, Alpha Is Transparency ON, sRGB ON.
- **Wrap Mode = Clamp** em tudo, **exceto** o que repete (marcado **Repeat** nas tabelas abaixo).
- FX: Sprite Mode **Multiple**, fatiar Grid By Cell Size com a célula indicada (frames = largura/célula).
- Escala em jogo 1:1 (1 px da arte = 1/32 u). **Linha do chão = y=372** nas camadas de 768×432 → pivô das camadas (0,5; 0,1389) colocado em **GroundTop**.

## Camadas de paralaxe (768×432, tileáveis em X → **Repeat** X)
Fator de paralaxe = fração do movimento da câmera; **confirme no projeto** que os fatores atuais da Fase 1 batem (se o builder já tiver valores próprios, mantenha os dele).
| arquivo | px | cores | paralaxe | sorting order | wrap | obs |
|---|---|---|---|---|---|---|
| camadas/P1_ceu_dia_native.png | 768×432 | 14 | 0.08 | -50 | Repeat X | longe, blocos 2×2 |
| camadas/P2_castelo_montanhas_longe_native.png | 768×432 | 21 | 0.22 | -40 | Repeat X | castelo branco + ilhas, blocos 2×2 |
| camadas/P3_montanhas_bosque_native.png | 768×432 | 16 | 0.4 | -30 | Repeat X |  |
| camadas/P4_arvores_ruinas_native.png | 768×432 | 16 | 0.62 | -20 | Repeat X |  |
| camadas/P4b_nevoa_clara_native.png | 768×432 | 1 | 0.62 | -15 | Repeat X | névoa (1 cor, semitransparente por dither) |
| camadas/P6_folhagem_frente_native.png | 768×432 | 2 | 1.25 | 20 | Repeat X | **na frente dos heróis** |

## Plataformas (one-way: BoxCollider2D + PlatformEffector2D, só o topo colide)
Grama/flores ficam **acima** da linha do colisor (decorativas). Pivô sugerido: **topo-centro na linha do colisor**, ou use o y indicado.
| arquivo | px | topo do colisor (linha y a partir do topo do PNG) | largura colisor | altura colisor | wrap |
|---|---|---|---|---|---|
| plataformas/plataforma_musgo_128_a.png | 128×84 | y=11 | 122 px (centralizado) | 6 px | Clamp |
| plataformas/plataforma_musgo_128_b.png | 128×86 | y=12 | 122 px (centralizado) | 6 px | Clamp |
| plataformas/plataforma_musgo_160_a.png | 160×105 | y=12 | 154 px (centralizado) | 6 px | Clamp |
| plataformas/plataforma_musgo_160_b.png | 160×109 | y=14 | 154 px (centralizado) | 6 px | Clamp |
| plataformas/plataforma_musgo_96_a.png | 96×62 | y=8 | 90 px (centralizado) | 6 px | Clamp |
| plataformas/plataforma_musgo_96_b.png | 96×63 | y=8 | 90 px (centralizado) | 6 px | Clamp |

## Tiles
| arquivo | px | uso / linha caminhável | wrap |
|---|---|---|---|
| tiles/chao_grama_128.png | 128×83 | **linha caminhável y=6** (tufos acima). Topo do PNG em GroundTop + 6 px (para cima); colisor do chão continua em GroundTop | Repeat X (128 px) |
| tiles/chao_subsolo_raizes_128x64.png | 128×64 | abaixo do chao_grama (começa 77 px abaixo de GroundTop), sem colisor | Repeat X/Y |
| tiles/chao_borda_abismo_dir.png | 48×90 | chão termina, vão à **direita**. Linha caminhável **y=7**; sobrepõe 4 px no vão | Clamp |
| tiles/chao_borda_abismo_esq.png | 55×90 | vão à **esquerda**. Linha caminhável **y=8**; sobrepõe 4 px no vão | Clamp |
| tiles/abismo_ceu_64x96.png | 64×96 | dentro dos vãos, topo em GroundTop − 0,125 u (4 px abaixo); sem colisor (hazard/kill continuam os atuais). Sorting -10 | Repeat X |
| tiles/pedra_caindo_0.png | 14×15 | pedrinhas decorativas nos vãos (opcional, caindo devagar) | Clamp |
| tiles/pedra_caindo_1.png | 13×13 | pedrinhas decorativas nos vãos (opcional, caindo devagar) | Clamp |
| tiles/pedra_caindo_2.png | 29×35 | pedrinhas decorativas nos vãos (opcional, caindo devagar) | Clamp |
| tiles/pedra_caindo_3.png | 14×14 | pedrinhas decorativas nos vãos (opcional, caindo devagar) | Clamp |

Os colisores do chão **não mudam de posição** (GroundTop continua o mesmo); só o visual troca. Larguras de vãos e posições: as atuais.

## Props (sem colisor, pivô base-centro (0,5; 0), Sorting -5, base 3 px abaixo de GroundTop)
| arquivo | px | cores |
|---|---|---|
| props/arbusto_flores.png | 41×36 | 27 |
| props/cogumelos.png | 34×36 | 33 |
| props/cristais_ciano.png | 41×44 | 31 |
| props/flores.png | 30×30 | 31 |
| props/lanterna.png | 31×44 | 28 |
| props/pedra_runica.png | 42×44 | 26 |
| props/pilar_vinhas.png | 40×52 | 30 |
| props/placa.png | 29×40 | 31 |
| props/rocha_samambaia.png | 38×34 | 30 |
| props/santuario.png | 43×54 | 28 |

## FX ambiente (só visual, sem colisão, sem dano)
| arquivo | px | frames | célula | fps | pivô | uso |
|---|---|---|---|---|---|---|
| fx/fx_petalas_4f_32x32.png | 128×32 | 4 (loop) | 32×32 | 8 | centro (0,5; 0,5) | pétalas caindo na diagonal, 6–10 instâncias espalhadas pela tela, sorting 15 |
| fx/fx_motes_aether_4f_32x32.png | 128×32 | 4 (loop) | 32×32 | 6 | centro (0,5; 0,5) | partículas de Aether ciano flutuando, 4–8 instâncias, sorting -12 (atrás das plataformas) |

## Previews (referência, não importar)
- previews/mock_fase1_v1.png, mock_fase1_v1_b.png — mock aprovado (canvas ×3; heróis ×5 = escala 1,67× do jogo, só para ver).
- previews/comparativo_f1_atual_vs_novo.png, trio_f1_f2_f3.png, plataformas_4x.png.

## Notas
- Castelo do P2 clareado (mármore branco) nesta versão final; P2 usa 21 cores (montanhas 5 + paleta do castelo 14 + cachoeira).
- **Contorno (rim) de 1 px do Mago: NÃO incluído** — sprites dos heróis estão fora do escopo deste pacote.
