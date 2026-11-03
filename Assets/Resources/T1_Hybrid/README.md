# Resources/T1_Hybrid — fundo da Fase I (Refino V4 + retoque V4.1)

| Arquivo | Tamanho | Origem | Paralaxe X / Y | Ordem |
|---|---|---|---|---|
| `sky.png` | 480×192, opaco | **Nativo V4** (`sky_v4.aseprite`): creme/dourado → lavanda → cerúleo, sem nuvens | 0,95 / 0,95 (`T1Backdrop`) | −50 |
| `clouds_a..d.png` | 176×60, 200×72, 88×26, 56×20 | **Retoque V4.1**: volume (topo creme → peach → ventre lavanda), silhuetas distintas, ouro pontual | 0,88–0,92 | −48 |
| `far.png` | 512×130, RGBA | **Nativo V4**: montanhas ciano-lavanda com dither + Cidadela Celeste | 0,90 / 0,12 | −36 |
| `city.png` | 640×176, RGBA | **Retoque V4.1**: Parasóis, Weeping Hero com orbs, arco e estelas com musgo, mata verde, água Aether com tufos/flores/juncos/vitórias-régias + cristal | 0,80 / 0,10 | −30 |
| `ground.png` | 256×64, RGBA, tileável | **Plataformas da frente**: grama viva, terra em blocos arredondados, pedra rúnica com glifo dourado, flecos de cristal | mundo | −6 |
| `ground_edge_l/r.png` | 24×64 | Beiradas dos gaps (lábio de grama, parede do penhasco); 8 px sobrepõem o `ground` | mundo | −5 |
| `pit.png` | 32×64, tileável | Abismo do gap: vazio lavanda → preto, névoa, faíscas de Aether | mundo | −7 |
| `plat_s/m/l.png` | 48×26, 80×30, 112×34 | Ilhas flutuantes (3/5/7 tiles): grama, terra, raízes, musgo, cristal rosa | mundo | −4 |

Contrato: PPU 16, filtro Point, sem compressão, sem mipmaps, pivô na base (0,5; 0).
Superfície de caminhada: linha 8 em `ground*`/`pit` (alinhada ao `GroundTop`), linha 4 em `plat_*`.
Colisores (`T1FrontLevel`): topo das ilhas de x=2 até w−3 (one-way); chão sólido até 2 px do gap;
zona de queda 4,5 u abaixo do `GroundTop` (morte → "VOCÊ CAIU").

## Fontes nativas

- `Tools/Aseprite/fundo_refino_v4/aseprite/*_v4.aseprite` (cópia versionada; originais em
  `floresta_16bit/aseprite/fundo_refino_v4/`). Camadas `BASE, SHADOW, DETAIL, HIGHLIGHT, MOSS, FX`
  + guias `GUIDE` dos templates. Paleta embutida: `paleta_alien_diurna_64cores.gpl` (58 cores).
- Pintura gerada por `Tools/Aseprite/fundo_refino_v4/pintar_fundo_v4.py` (primitivas de cluster,
  sem ler a referência) e montada por `montar_fundo_v4.lua`.
- Export: `ExportForUnity.lua` modo `flat` (sem `GUIDE`) sobre o PNG de mesmo nome.
- Frente jogável: `Tools/Aseprite/plataformas_frente/` — `pintar_plataformas_frente.py` (camadas),
  `montar_plataformas_frente.lua` (.aseprite com `GUIDE - Grid 16` e `GUIDE - Superficie`),
  `mockup_frente.py` (prévia 1×) e `qa/` (capturas do Unity com colisores). Originais em
  `floresta_16bit/aseprite/plataformas_frente/`. Layout em `T1FrontLevel.Segments` / `Plats`.

## Ligar / desligar

- Editor: **Aetherion → Fundo T1 → Usar fundo híbrido (V3+V2)** (marcado = este fundo).
- Editor, QA rápido: **Aetherion → Fundo T1 → Jogar Ground_T1 direto**.
- WebGL: padrão ligado; `?fundo=classico` mostra o fundo antigo (chão contínuo, plataformas antigas).
- Código: `T1HybridScenery.DefaultEnabled`; posições das nuvens em `T1HybridScenery.Clouds`.
