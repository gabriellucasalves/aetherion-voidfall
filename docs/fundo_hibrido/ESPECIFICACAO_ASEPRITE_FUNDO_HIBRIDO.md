# Especificação Aseprite — Fundo híbrido (céu V3 + layout V2)

**Status:** aprovado pela Direção Criativa (2026-10-26)  
**Referência visual:** `fundo_fase_hibrido_v3ceu_v2layout.png` (NÃO é sprite nativo)  
**Paleta:** `floresta_16bit/paleta/paleta_alien_diurna_64cores.gpl`  
**Contrato T1 (Guia):** PPU **16**, filtro **Point**, pivô props bottom-center `(0.5, 0)`.

## 1. Objetivo

Substituir o visual atual da Fase 1 (templo branco dominante + escala desalinhada) por um fundo em camadas alinhado ao híbrido: céu Gate B.1 V3 + layout de plataforma V2 (Parasol/Weeping, ruínas, rio Aether, Cidadela distante, plataformas legíveis).

## 2. Camadas nativas a pintar (1×)

| Ordem | Arquivo sugerido | Tamanho nativo (T1) | Conteúdo | Paralaxe tip. |
|---|---|---|---|---|
| 0 | `sky.png` | **480×192** | Gradiente cerúleo opaco (faixas pintadas, sem banding duro); **sem** montanhas/árvores | 0,95 · order −50 |
| 1 | `clouds_a..e.png` (opcional) | ver Gate B.1 | Cinco sprites RGBA com alfa verdadeiro; luz alto-esq.; D = faixa ~160×28 | 0,92–0,85 |
| 2 | `far.png` | **512×130** | Montanhas frias + silhueta Cidadela Celeste (baixo contraste) | 0,9 · −36 |
| 3 | `mid_forest.png` / `city.png` | **640×176** | Floresta intermediária, Parasol, Weeping, ruínas/estátua — **evitar templo branco dominante** | 0,8 · −30 |
| 4 | props | livre | `statue_*`, `tree_parasol`, `tree_weeping`, colunas | 0,4…0 |
| 5 | plataformas / pavement | contrato plat* / 512×44 | Chão grama/terra com silhueta escura legível vs céu claro | 0 · −4 / order 2 |

Viewport de referência do jogo: ~384×216 (ou guia 304×171 no template). Preview 3×/4× nearest-neighbor.

## 3. Fluxo Aseprite

1. Copiar `Tools/Aseprite/CreateSceneryTemplate.lua` → pasta Scripts do Aseprite → Rescan.
2. Gerar templates: `kind=sky`, `kind=far`, `kind=city`, `kind=prop`, `kind=platform`.
3. Indexar à paleta GPL. Pintar em **1×**; **não** downscale da referência de IA.
4. Céu: 5–6 faixas irregulares (`#0E2B4B` → `#9CE0F5` / `#C6E7EB`); clusters, sem Bayer.
5. Nuvens: creme `#FFF3D6`, âmbar `#F0D7B2`, lavanda `#C9B6D3`/`#887A9E`, base fria; ouro `#FFD043` pontual.
6. Export: `ExportForUnity.lua` (1 PNG por camada, sem `GUIDE`).
7. Destino sugerido: `Assets/Resources/...` no mesmo padrão do T1 atual (confirmar pasta `T1/` no repo).

## 4. Critérios de aceite arte

- [ ] Céu independente das nuvens  
- [ ] Cinco nuvens com identidades distintas (D alongada)  
- [ ] Sem retângulo azul opaco nas nuvens  
- [ ] Sem templo branco competindo com HUD/plataformas  
- [ ] Escala de props coerente com herói (~32 px altura visual)  
- [ ] Indexado à paleta GPL  

## 5. Integração Unity (próximo)

Atualizar o gerador de cenário da fase (ex.: `T1Scenery` / `GroundT1Controller`) para carregar os novos sprites; manter PPU 16 Point; branch `art/fundo-hibrido-v3-v2`; PR sem rebuild WebGL obrigatório nesta etapa (rebuild sob pedido).

