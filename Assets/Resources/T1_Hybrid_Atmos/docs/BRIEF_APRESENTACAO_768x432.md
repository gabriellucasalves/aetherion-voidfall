# BRIEF — Apresentação 768×432 (Point, densificar 16-bit)
Data: 03/11/2026 (BRT) · Refs: Owlboy, Chrono Trigger, Iconoclasts  
Status: AGUARDA AUTORIZAÇÃO antes de alterar código/arte.

## 1. Problema
O mundo é renderizado em **384×216** (`PixelPresentation`) e ampliado com Point. Em Full HD/4K cada texel vira um “tijolo”. A arte Point/PPU 16 está correta — o gargalo é a **resolução interna**, não Bilinear faltando.

Refs (Owlboy etc.) também são pixel art Point; a sensação “caprichada” vem de **mais pixels na tela** (framebuffer maior e/ou arte mais densa), não de suavizar.

## 2. Decisão da Direção
Rota: **768×432** (2× linear em cada eixo vs 384×216), manter Point + escala inteira, depois densificar arte.

## 3. Como fazer certo (crítico)
Hoje: `OrthoSize = InternalHeight / (Ppu * 2)` → com H=216 e Ppu=16 → **OrthoSize = 6.75**.

Se só dobrar H e manter a fórmula, OrthoSize vira **13.5**: a câmera mostra o dobro do mundo e o Guerreiro fica **metade do tamanho na tela** (parece “mais longe”, não “mais caprichado”).

**Proposta correta:**
| Constante | Hoje | Novo |
|---|---|---|
| `InternalWidth` | 384 | **768** |
| `InternalHeight` | 216 | **432** |
| `OrthoSize` | 6.75 (acoplado) | **6.75 fixo** (desacoplar de InternalHeight) |
| `PixelArt.Ppu` | 16 | **16** (inalterado nesta etapa) |
| `filterMode` RT + sprites | Point | Point |

Efeito: px por unidade no RT = `432 / (2×6.75) = 32` → cada pixel de arte atual vira **2×2** no framebuffer (upscale nativo nearest). Framing do level igual. Visual imediatamente menos “bloco grosso”, ainda 16-bit.

## 4. Escopo Etapa 1 (código — após ok)
Arquivos:
- `Assets/Scripts/Stages/PixelPresentation.cs` — 768×432; OrthoSize constante 6.75f (ou `LegacyInternalHeight=216` na fórmula).
- Comentário/header atualizar.
- `GroundT1Controller` / `T1HybridScenery` já usam `PixelPresentation.OrthoSize` → continuam ok se OrthoSize permanecer 6.75.
- **Não** mudar paralaxe factors, HUD Canvas, colisores, plats, combate.
- **Não** Bilinear, **não** MSAA no mundo pixel.

Validar: Play Mode + WebGL; letterbox em 1920×1080 (scale inteiro máximo = 2 → 1536×864); em 2560×1440 scale 3; em 3840×2160 scale 4.

## 5. Escopo Etapa 2 (arte — sprint seguinte)
Upscale 2×2 automático já “enche” o RT, mas **não cria detalhe Owlboy**. Densificar nativo (Aseprite):
1. Cidadela pequena (V3) — agulhas, ventre, silhueta.
2. `far` / `city` — volumes, luz, flora.
3. Guerreiro (e depois Mago/Arqueiro) — sheet mais densa.
4. Inimigos F1.
5. HUD mockup (barras) — aproveitar pixels extras.

## 6. Fora de escopo
Passado Selva pivot; Bilinear; mudar PPU global nesta etapa; merge main sem ok; On-Demand.

## 7. Riscos
- WebGL/GPU: RT 4× pixels — custo baixo.
- Game View Editor pequeno: scale=1 → letterbox; testar maximizado.
- Arte atual “esticada” 2×2 fica aceitável curto prazo; microdetalhe vem na Etapa 2.
- Grep por 384/216 hardcoded em tools/docs.

## 8. Critérios de aceite Etapa 1
- [ ] RT 768×432 Point
- [ ] OrthoSize = 6.75 (framing igual)
- [ ] Guerreiro/plats mesma escala aparente
- [ ] Sem blur
- [ ] Screenshot Editor + URL WebGL (Vercel personal) antes/depois
- [ ] Branch `tech/apresentacao-768x432`

## 9. Parada
Aguardando **autorização humana** para aplicar só a Etapa 1 (código). Etapa 2 arte = sprint separado.
