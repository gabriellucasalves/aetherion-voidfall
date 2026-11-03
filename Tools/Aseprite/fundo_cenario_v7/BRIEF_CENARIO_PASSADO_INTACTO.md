# Brief — Cenário Fase I: Passado Intacta (V7)

**Status:** aguardando aprovação da Direção Criativa  
**Base visual que o usuário gostou:** composit V6 (céu creme/lavanda, detalhe 16-bit, Cidadela clara)  
**Problema:** V4–V6 leem como “fase de floresta”; a Fase I é o **Passado / Era Dourada**, com civilização ainda viva e Cidadela **intacta**.

## Premissa
O universo (depois) foi destruído. A **primeira fase** acontece **antes**: cidade no céu intacta, culto ativo, floresta alienígena como jardim/santuário ao redor — não mato selvagem fechado.

Lore alinhado: GDD Bioma 01 “Floresta Alienígena do Passado” + Cidadela Celeste flutuante.  
WORLD_LORE (ruína/estátua quebrada) fica para biomas **posteriores**; aqui as estátuas ainda estão **em pé**.

## O que muda vs V6
| Camada | V6 (rejeitado como direção) | V7 Passado Intacta |
|--------|-----------------------------|--------------------|
| Leitura | Floresta densa + alguns heróis | **Santuário civilizado** sob a Cidadela |
| Far | Mata + montanha + cidadela | **Cidadela intacta = marco #1**; montanhas secundárias; **menos** treeline contínua |
| Mid | Parasol + Weeping + mata barra | **Árvores novas** + **estátuas/colunas** + clareiras |
| Densidade | Mata fechada | Espaços abertos entre monumentos (vê-se o céu/cidade) |

## Hierarquia de silhueta (Aladdin / MQ / SF2)
1. **Cidadela Celeste intacta** (far) — torres completas, janelas acesas, quilha limpa, sem ruína.
2. **Estátuas e arquitetura** (mid) — 2–3 estátuas de divindade em pé (corpo + pedestal), colunata/pórtico, arco cerimonial, estelas com glifo ouro.
3. **Árvores novas** (mid) — silhuetas distintas das atuais:
   - **Ardósia/frondes Aether** (copa em leque ardósia + frondes teal) — 1 exemplar herói.
   - **Espiral / organismo-coluna** (tronco helicoidal, sem Parasol).
   - **Parasol** — no máximo **1**, menor, lateral (não dominar).
   - **Weeping** — no máximo **1**, ou substituir por frondes douradas curtas se a Direção preferir zero Weeping.
4. Vegetação de apoio: arbustos baixos, fetos, musgo — **sem** barra de floresta cobrindo 60% da largura.

## Inventário proposto (mid 640×176)
- Estátua A (esquerda): divindade em pé ~48–56 px alt, pedestal, glifo.
- Colunata (2–3 colunas) ou pórtico baixo.
- Árvore Ardósia (centro-esquerda).
- Água Aether + cristal/portal (mantém gameplay/lore).
- Arco cerimonial (pedra clara, hera leve — não “ruína caída”).
- Estátua B / cabeça monumental menor (direita).
- Árvore Espiral ou 1 Parasol curto.
- Opcional: 1 Weeping ou frondes douradas.

## Far 512×130
- Cidadela **maior/mais legível** (centro-esquerda ou centro), 12–16 torres, ameias, janelas, luz dourada à esquerda.
- Cordilheira atrás/abaixo, estratos 16-bit (como V6).
- Treeline no máximo **fragmentada** (manchas), não faixa contínua.

## Sky / clouds
Manter direção V4/V6 (creme → lavanda, cúmulos volumosos). Sem céu ciano “genérico”.

## Intocado
`ground.png`, `plat_*`, `pit`, edges — **não mexer**.

## Técnica
- Nativo 1×, só `paleta_alien_diurna_64cores.gpl`.
- Detalhe 16-bit do V6 (lóbulos, argamassa, estratos) **mantido** na linguagem de pintura.
- Entrega: camadas + FLAT → `T1_Hybrid` + preview ×3 + Aseprite; **só após “sim” da Direção**.

## Fora de escopo neste sprint
Rebuild Unity / Vercel / inimigos — depois do aceite visual.
