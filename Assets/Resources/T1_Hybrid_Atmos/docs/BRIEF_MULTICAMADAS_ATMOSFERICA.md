# BRIEF — Perspectiva atmosférica + paralaxe multicamadas (+ Cidadela menor)

**Projeto:** Aetherion: Voidfall · Fase I  
**Data:** 2026-11-03 (BRT)  
**Status:** aguardando aceite Direção (não pintar / não integrar até OK)  
**Ref Direção:** diagrama “perspectiva atmosférica + paralaxe multicamadas” + pedido: detalhes Dense V2 na Cidadela, **mas menor**, com luz/sombra/profundidade de espaço.

## 1. O que foi rejeitado / o que fica
| Item | Decisão |
|------|---------|
| Dense V2 Cidadela (tamanho + posição baixa) | **Rejeitado** — tapa mid, silhueta grande demais |
| Detalhe da Cidadela V2 (torres, janelas, base, veios) | **Manter linguagem** — densificar, não “tijolo” |
| Dense V1 (script sutil) | Insuficiente sozinho |
| Fundo chapado V7.1 live | Problema de raiz: **poucos planos + mesmo contraste** |
| RT 768×432 / Ortho 6.75 | Mantém (Etapa 1 já aplicada) |
| Ground / gaps / colisão | **Intocados** neste sprint |

## 2. Efeito alvo (palavras da Direção)
Não é “uma imagem chapada com Cidadela grande”. É **várias camadas** que se juntam: quanto mais longe, **menos contraste e movimento**; quanto mais perto, **mais contraste, sombra e leitura**. Luz e sombra separam planos (playfield “pra frente”, fundo “pra trás”).

## 3. Cinco planos (mapa arte ↔ Unity)

| Plano | Conteúdo | Tratamento visual | Paralaxe (sugestão) | Asset / camada |
|-------|----------|-------------------|---------------------|----------------|
| **1** | Céu + nuvens distantes | Gradientes suaves, pouca definição, creme→lavanda | ~0.05–0.12 | `sky` + `clouds_*` lentas |
| **2** | Montanhas + **Cidadela pequena** (+ templo distante se couber) | Silhueta legível, contraste ↓, névoa/frio, dessaturado | ~0.18–0.28 | `far` (+ citadel crop) |
| **3** | Floresta / flora intermediária (Parasol, Weeping mid) | Volumes definidos mas suavizados; véu leve | ~0.35–0.45 | `mid` / parte de `city` “recuada” |
| **4** | Estruturas próximas (ruínas, estátuas, arco) | Detalhe nítido, sombra de contato, contraste médio-alto | ~0.55–0.7 | `city` frente / props mid-front |
| **5** | Terreno jogável (ground + plats) | Máxima definição, arestas claras, sombra sob plats | 1.0 (câmera) | `ground` / `plat_*` **sem redesenhar level** |

**Cidadela:** só no **Plano 2**, ~**35–45%** da largura útil do far (terço direito, ideia Cinematic V3), **acima** da linha de mid — não compete com estátua-coração / árvore do Plano 4. Detalhe estilo V2 (agulhas, janelas âmbar, base lavanda + cristal), escala menor.

## 4. Luz e sombra (obrigatório)
1. **Sol** (topo-esquerdo, V7.1): highlight nos planos 4–5; planos 2–3 recebem luz mais fraca/fria.
2. **Hierarquia de valor:** far nunca compartilha o mesmo bloco de luminância do playfield (erro do chapado).
3. **Sombra de contato** sob plats/ground e pé de estátuas/árvores (Plano 4–5).
4. **Névoa atmosférica** no Plano 2 (lavanda/ardósia), 1–2 valores da paleta — sem blur bilinear.
5. Continua **Point / pixel art 16-bit** (Owlboy/Chrono/Iconoclasts): profundidade por **valor + saturação + camadas**, não por anti-alias.

## 5. Densidade (768)
- Far/city (planos 2–4) em densidade nativa adequada ao RT 768 (PPU 2× na importação Unity quando a arte for 2×).
- Cidadela pintada **nativa pequena e densa**, não upscale do V2 grande.
- Guerreiro/inimigos: **fora** deste sprint (depois).

## 6. Escopo de entrega (após aceite)
**Etapa A — conceito** (este bot): composite 5 planos + Cidadela pequena (preview x2/x3) para ok visual.  
**Etapa B — pack** `T1_Hybrid_Atmos_V1/`: sky, far(+citadel), mid/city split se preciso, clouds; CHANGELOG; sem Unity.  
**Etapa C — prompt outra IA:** import Point, PPU, factors de paralaxe no `T1HybridScenery` / `T1HybridLayer`; WebGL + Vercel personal.  
Sem On-Demand neste bot; sem merge `main` sem ok.

## 7. Fora de escopo
Pivot selva/pirâmide; Dense V2 tamanho atual; Bilinear; redesenhar gaps; HUD (já tem prompt separado); morte/câmera.

## 8. Critérios de aceite
- [ ] Em 1 segundo dá pra apontar os 5 planos.
- [ ] Cidadela **menor**, no fundo, detalhada, sem tapar mid importante.
- [ ] Playfield mais contraste que o far; far “longe” (frio/névoa).
- [ ] Sem chapado; sem blur; paleta Passado Intacta preservada.
- [ ] Ground/plats jogáveis intactos.

## 9. Decisão pedida
- [ ] Aceitar brief e autorizar **Etapa A** (conceito 5 planos + Cidadela pequena)
- [ ] Ajustar (ex.: Cidadela ainda menor / Plano 3 mais floresta / far mais escuro)
