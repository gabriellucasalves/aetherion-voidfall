# Resources/T1_Hybrid — fundo híbrido da Fase I (céu V3 + layout V2)

> **PLACEHOLDER — NÃO É ARTE FINAL.**
> Os quatro PNGs desta pasta são **downscale da referência de IA**
> (`docs/fundo_hibrido/REFERENCIA_IA_fundo_fase_hibrido_v3ceu_v2layout.png`),
> quantizados para a paleta `paleta_alien_diurna_64cores.gpl`, com o mago e a
> ilha flutuante removidos. Servem só para validar composição, escala e paralaxe
> no jogo. A arte final é pintura nativa no Aseprite.

| Arquivo | Tamanho | Camada | Paralaxe X / Y | Ordem |
|---|---|---|---|---|
| `sky.png` | 480×192, opaco | Céu V3 (cerúleo + nuvens creme/lavanda) | 0,95 / 0,95 (`T1Backdrop`) | −50 |
| `far.png` | 512×130, RGBA | Montanhas frias + Cidadela Celeste | 0,90 / 0,12 | −36 |
| `city.png` | 640×176, RGBA | Guarda-sol, salgueiro, ruínas, água Aether | 0,80 / 0,10 | −30 |
| `ground.png` | 512×64, RGBA | Grama/terra do chão (tile espelhado alternado) | 1 (mundo) | −6 |

Contrato: PPU 16, filtro Point, sem compressão, sem mipmaps, pivô na base (0,5; 0).
A superfície de caminhada do `ground.png` é a linha 9 a partir do topo (alinhada ao `GroundTop`).

## Trocar pelos PNGs nativos

1. Pintar nos templates `Tools/Aseprite/fundo_hibrido_v3_v2/templates/*_template.aseprite`.
2. Exportar com `Tools/Aseprite/ExportForUnity.lua` (modo `flat`, sem camadas `GUIDE`).
3. Sobrescrever o arquivo de **mesmo nome** nesta pasta (o `.meta` já tem o import correto).
4. Mantendo o mesmo tamanho, nenhuma linha de código muda. Se mudar o tamanho ou a linha
   da grama, ajustar as constantes em `Assets/Scripts/Stages/T1HybridScenery.cs`.

Os placeholders se regeneram com
`python3 Tools/Aseprite/fundo_hibrido_v3_v2/gerar_placeholders_t1_hybrid.py`.

## Ligar / desligar

- Editor: menu **Aetherion → Fundo T1 → Usar fundo híbrido (V3+V2)** (marcado = híbrido).
- Editor, QA rápido: **Aetherion → Fundo T1 → Jogar Ground_T1 direto**.
- WebGL: o padrão é híbrido; `?fundo=classico` na URL mostra o fundo antigo para comparar.
- Código: `T1HybridScenery.DefaultEnabled`.
