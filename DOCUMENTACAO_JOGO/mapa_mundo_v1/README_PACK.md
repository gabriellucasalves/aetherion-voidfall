# mapa_assets_v1 — pack de arte do overworld (sem implementação Unity)

**Custo:** só processamento offline no box. Sem GenerateImage novo, sem CloudAgent, sem deploy, sem código no repositório.

**Isto NÃO é o jogo rodando.** Fundo estático + overlays transparentes + guia de rota + JSON de coordenadas **aproximadas da arte**.

## Conteúdo

| Arquivo | O que é |
|---|---|
| `source/aetherion_worldmap_conceito_ilha_3fases.jpg` | Conceito aprovado (intacto; herói assado). |
| `map_island_bg_clean.png` | Placa limpa 1280×720 — herói removido por patch local de grama. |
| `compare/knight_removal_compare.png` | Antes/depois do recorte editado. |
| `compare/clean_area_zoom.png` | Zoom da área limpa. |
| `overlays/node_{locked,unlocked,cleared}_{24,32}.png` | Status **por cima** dos nós (não substituem o círculo+número da base). |
| `overlays/select_arrow_16.png` | Indicador de seleção. |
| `preview/overlays_preview_4x_pt.png` | Folha 4× com rótulos PT (fora dos assets). |
| `preview/map_with_overlays_demo.png` | Demo visual fundo+overlays (não runtime). |
| `guides/route_guide_annotated.png` | Cópia anotada: nós 1–2–3 + waypoints da trilha. |
| `map_art_coords.json` | Metadados de arte (px + norm top-left). **Não** são world units Unity. |
| `docs/PROMPT_IMPLEMENTAR_MAPA.md` | Spec de implementação local (outra IA / Cursor no Mac). |

## Limpeza do herói assado — status honesto

- **Feito:** cavaleiro vermelho/preto removido; nó **1**, numerais, castelo/fortaleza e resto do mapa **não** redesenhados de propósito.
- **Qualidade:** usável como placa de trabalho; o patch de grama ainda mostra **leve mismatch de textura/ruído** na área editada (ver `compare/`). **Não** é pintura Aseprite nativa perfeita.
- Se a outra IA exigir pixel-perfect: retoque manual no Aseprite em cima de `map_island_bg_clean.png`.

## Herói overworld

**Não incluso.** Sprites atuais de Guerreiro/Arqueiro/Mago/inimigos **não** estavam disponíveis neste box. A implementação deve usar os sprites **atuais do projeto** (`Aetherion_Atmos_V1`) e animar overworld à parte. Não há garantia de sheets direcionais prontos.

## Como usar os overlays

- Fundo = `map_island_bg_clean.png` (números 1/2/3 já na arte).
- Overlays = anéis/ícones **transparentes** centrados nos nós (locked cadeado / unlocked ciano / cleared check).
- Não apagar o círculo base da arte; overlays são status, não “novo nó”.
- Import: Sprite, **Point**, sem mipmaps, Compression **None**. **Não** forçar PPU/resolução global do projeto.

## Limitações do fundo

- PNG único, **sem** camadas Aseprite / parallax.
- Fase 2 (ruínas) e Fase 3 (fortaleza/boss) = **proposta visual** no conceito; não são fases/boss implementados.
- Coordenadas do JSON = derivadas da arte, aproximadas; validar alinhamento olhando `guides/` e `preview/map_with_overlays_demo.png`.

## Checklist rápido (arte)

- [x] Fonte preservada em `source/`
- [x] Placa limpa + comparativo
- [x] Overlays 24/32 + seta 16 com alpha
- [x] Preview 4× PT
- [x] Guia de rota + JSON
- [ ] Retoque Aseprite da mancha de grama (opcional, recomendado)
- [ ] Sheets overworld do herói (fica com o projeto / outra IA)
- [ ] Integração Unity / testes (fora deste pack)

Gerado: 2026-11-06 America/Sao_Paulo.
