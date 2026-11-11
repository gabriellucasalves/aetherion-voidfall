# FASE 3 — Frente nova (plataformas, chão, abismo, props) — MANIFEST de troca drop-in

**v2.** Mesmos nomes de arquivo, mesmos topos de colisor (mundo) e mesma linha caminhável → **sobrescrever** os PNGs nos caminhos abaixo (manter os .meta / GUIDs).
**Única mudança de dimensão: as 3 plataformas flutuantes ficaram mais altas** (corpo grosso e ornamentado crescendo só PARA BAIXO; o topo caminhável continua na mesma linha y=0 e com a mesma largura). Os nomes `..._x48` foram mantidos de propósito para não quebrar referências. Todo o resto mantém o tamanho exato em px.

### ⚠ Ajuste obrigatório de pivô/colisor nas plataformas (Flash)
- Pivô do sprite: **Custom (0,5; 1,0) = topo-centro** (em px: (W/2, 0) a partir do topo). Assim o topo fica exatamente na mesma posição do mundo, independente da nova altura.
- Se o projeto usava pivô **Center (0,5; 0,5)** nas plataformas de 48 px, o topo ficava 24 px acima do transform. Para manter o transform onde está, use em vez disso pivô Y = **1 − 24/H**: 96×60 → **0,600**; 128×79 → **0,6962**; 160×100 → **0,760**.
- Colisor (BoxCollider2D + PlatformEffector2D): **tamanho 92/124/156 × 8 px (0,25 u)**, borda de cima no topo do sprite → offset Y = topo − 0,125 u em relação ao transform (com pivô topo-centro: **offset (0; −0,125)**, size (2,875 / 3,875 / 4,875; 0,25)).
- Se algum código calcula o colisor a partir de `sprite.bounds` (altura/centro), troque para usar o **topo** (`bounds.max.y`) + altura fixa 0,25 u — senão o colisor desce junto com o corpo novo.
- Sorting: as plataformas continuam no mesmo sorting; o corpo novo (raízes/estalactites) pode cobrir parte do fundo — é esperado.

Estilo: pixel art 16-bit, paleta ≤16 cores por asset, rim light, rachaduras, veias de corrupção carmesim com brilho. Plataformas grossas com friso em baixo-relevo, gema carmesim central, filetes de ouro e parte de baixo pendurada (estalactites + raízes/pontas de tentáculo). Chão com friso de grega, cravos de ouro, rubi carmesim limpo e lajotas quebradas em obsidiana/roxo. Subsolo com tijolos, rachaduras, fissuras carmesim brilhantes, crânios e entulho (tileável X/Y). Props nítidos (base nos sprites feitos à mão originais + camada de corrupção).

**NÃO mudam:** camadas de fundo da Fase 3 (`T3_Ruins_Atmos/v3/` P1…P4b), boss Devorador do Vazio, inimigos, `portao_arena_2f_164x100.png` (portão), FX da Fase 3, lógica, colisores, spawns, hazards.

## Import (todas as texturas deste pacote)
- Texture Type **Sprite (2D and UI)**, **Pixels Per Unit 32**, **Filter Mode Point (no filter)**, **Compression None**, **Generate Mip Maps OFF**, Alpha Is Transparency ON, sRGB ON.
- **Wrap Mode = Clamp** em tudo, **exceto** o que repete (marcado **Repeat** nas tabelas abaixo).
- FX: Sprite Mode **Multiple**, fatiar Grid By Cell Size com a célula indicada (frames = largura/célula).
- Escala em jogo 1:1 (1 px da arte = 1/32 u). **Linha do chão = y=372** nas camadas de 768×432 → pivô das camadas (0,5; 0,1389) colocado em **GroundTop**.

## 1. Plataformas e chão → sobrescrever em `Assets/Resources/T3_Ruins_Atmos/v2/` (caminho do fase3_final_v1; **confirme no projeto**)
| arquivo (pacote) | px | cores | colisor / linha caminhável (igual ao atual) |
|---|---|---|---|
| plataformas/plataforma_flutuante_96x48.png | 96×60 | 16 | one-way, topo **y=0**, altura colisor 8 px (y=0..8), largura **92 px** centralizada. **PNG agora 96×60 (antes 96×48)** — corpo cresceu só para baixo. Pivô **topo-centro (0,5; 1,0)** |
| plataformas/plataforma_flutuante_128x48.png | 128×79 | 16 | one-way, topo **y=0**, altura 8 px, largura **124 px**. **PNG agora 128×79 (antes 128×48)**. Pivô **topo-centro (0,5; 1,0)** |
| plataformas/plataforma_flutuante_160x48.png | 160×100 | 16 | one-way, topo **y=0**, altura 8 px, largura **156 px**. **PNG agora 160×100 (antes 160×48)**. Pivô **topo-centro (0,5; 1,0)** |
| plataformas/chao_tile_64x32.png | 64×32 | 16 | chão, **topo y=0** = GroundTop, **Repeat X**, pivô (0,0) — linha caminhável y=0 |
| plataformas/chao_ponta_esq_16x32.png | 16×32 | 16 | ponta do chão, topo y=0 |
| plataformas/chao_ponta_dir_16x32.png | 16×32 | 15 | ponta do chão, topo y=0 |
| plataformas/chao_borda_abismo_dir_24x48.png | 24×48 | 16 | borda (vão à direita), topo y=0; colisor só nos 32 px de cima; 16 px de baixo = rocha/veias penduradas |
| plataformas/chao_borda_abismo_esq_24x48.png | 24×48 | 16 | borda (vão à esquerda), topo y=0; colisor só nos 32 px de cima |
| plataformas/capitel_coluna.png | 80×54 | 16 | sólido, topo **y=1** de x=4 a x=76, pivô base-centro (40,54) — mesma silhueta |
| plataformas/estatua_rei_tombada.png | 104×33 | 16 | degrau: topo **y=2** de x=16 a x=80; pedestal topo y=12 — mesma silhueta |

## 2. Props do chão → sobrescrever em `Assets/Resources/T3_Ruins_Atmos/v3/props/` (**confirme no projeto**)
Sem colisor, pivô base-centro (0,5; 0), Sorting -5 — iguais aos atuais.
| arquivo | px | cores |
|---|---|---|
| props/caixotes_barril.png | 50×35 | 16 |
| props/cristal_a.png | 30×31 | 15 |
| props/cristal_b.png | 38×35 | 15 |
| props/cristal_c.png | 29×29 | 16 |
| props/entulho_a.png | 52×41 | 16 |
| props/entulho_b.png | 41×31 | 16 |
| props/ossos_detritos.png | 24×13 | 15 |
| props/pilar_quebrado.png | 28×47 | 16 |
| props/tambor_coluna.png | 36×25 | 16 |
| props/urna_rachada.png | 25×31 | 16 |

## 3. Tiles → sobrescrever em `Assets/Resources/T3_Ruins_Atmos/v3/tiles/` (**confirme no projeto**)
| arquivo | px | uso | wrap |
|---|---|---|---|
| tiles/chao_subsolo_64x48.png | 64×48 | abaixo do chao_tile, sem colisor | **Repeat X/Y** |
| tiles/abismo_vazio_64x128.png | 64×128 | dentro dos vãos, topo em GroundTop − 0,25 u, Sorting -10, sem colisor | **Repeat X** |

## Previews (não importar)
- previews/mock_fase3_frente_v2_arena.png e mock_fase3_frente_v2_caminho.png — mock com fundo, boss, portão e inimigos atuais + frente v2 (×3).
- previews/comparativo_f3_antiga_v1_v2_vs_f2.png — 4 faixas: frente antiga / v1 / v2 / referência Fase 2 v3 (a Fase 2 está em escala de mock 1,67×).
- previews/frente_antiga_vs_v2_3x.png, plataformas_4x.png — close-ups.
