# Especificação Aseprite — Fundo Refino V4

**Status:** aprovado pela Direção Criativa (2026-10-26)  
**Referência visual:** `fundo_fase_refino_v4.jpg` (NÃO é sprite nativo — só direção)  
**Anterior:** híbrido V3+V2; V4 refina o build atual (Weeping + montanhas)  
**Paleta:** `floresta_16bit/paleta/paleta_alien_diurna_64cores.gpl`  
**Contrato T1:** PPU **16**, filtro **Point**, pivô props bottom-center `(0.5, 0)`.

## 1. Objetivo

Manter o que já funciona no playtest (Weeping à direita, montanhas com dither, parallax legível) e corrigir o que ainda puxa pra genérico: céu ciano plano → creme/dourado + lavanda (Gate B.1 V3); Cidadela Celeste no horizonte; Parasol; ruínas/estátua + água Aether. **Plataformas/chão da frente ficam pra sprint seguinte** — nesta spec, só camadas de fundo até mid.

## 2. Camadas nativas a pintar (1×)

| Ordem | Arquivo sugerido | Tamanho nativo (T1) | Conteúdo V4 | Paralaxe tip. |
|---|---|---|---|---|
| 0 | `sky.png` | **480×192** | Gradiente: topo creme/dourado suave → lavanda médio → cerúleo baixo; faixas pintadas irregulares, **sem** montanhas/árvores | 0,95 · −50 |
| 1 | `clouds_*.png` | Gate B.1 | Nuvens horizontais creme `#FFF3D6` / âmbar `#F0D7B2` / lavanda `#C9B6D3`; base fria; ouro `#FFD043` pontual; alfa verdadeiro | 0,92–0,85 |
| 2 | `far.png` | **512×130** | Montanhas frias cyan-lavanda com dither; pico distante **mais suave** (menos vulcão dominante); silhueta **Cidadela Celeste** (cidade flutuante pálida, rim light dourado baixo contraste) | 0,9 · −36 |
| 3 | `city.png` / `mid.png` | **640×176** | Parasol(ões) esquerda-centro (copa guarda-chuva teal); Weeping Hero direita (folhagem lavanda cascata + orbs amarelos); arco de ruína + pilares; **sem** templo branco enorme | 0,8 · −30 |
| 4 | props (opcional) | livre | `tree_weeping`, `tree_parasol`, `ruin_arch`, `statue_*`, cristal Aether rosa | 0,4…0 |
| 5 | água Aether | faixa / prop | Filete ou poça luminosa refletindo creme/lavanda; brilho rosa local | mid |

**Fora do escopo agora:** `pavement` / `plat*` / chão grama detalhado — sprint de plataformas da frente.

Viewport: ~384×216. Preview 3×/4× nearest-neighbor.

## 3. Fluxo Aseprite

1. Templates já em `floresta_16bit/aseprite/fundo_hibrido_v3_v2/` (`sky`, `far`, `city`, `prop`, `platform`) — reutilizar ou renomear pasta para `fundo_refino_v4/`.
2. Abrir referência `fundo_fase_refino_v4.jpg` numa janela; pintar **só** nos templates 1×. **Não** downscale da IA.
3. Indexar à GPL. Céu primeiro → far → mid → props.
4. Export: `ExportForUnity.lua` (1 PNG/camada, sem `GUIDE`).
5. Destino: `Assets/Resources/T1/` (substituir sky/far/city) **ou** `Assets/Resources/T1_Hybrid/` + flag, conforme o switch já no projeto.

## 4. Critérios de aceite arte (fundo)

- [ ] Céu independente das nuvens; topo creme/dourado + mid lavanda (não ciano chapado)
- [ ] Cidadela legível no horizonte sem competir com mid
- [ ] Weeping + Parasol com silhuetas distintas
- [ ] Sem templo branco dominante
- [ ] Orbs amarelos / cristal Aether pontuais (sem flood de glow)
- [ ] Indexado à paleta GPL; Point; PPU 16

## 5. Próximo após pintura

Sprint **plataformas da frente**: silhueta escura legível vs céu claro, escala vs herói (~32 px), pavimento/plat* sem misturar com HUD.
