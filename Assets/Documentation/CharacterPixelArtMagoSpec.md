# Spec de arte — Mago (Etapa 10)

Desenhar o Mago **dentro do molde do Guerreiro**, com a identidade do conceito `Assets/Art/Mago/mago_conceito.png`.

**Etapa 11 autorizada e aplicada** (2026-09-14): sheet regenerado por `Assets/Art/Mago/draw_mago_etapa11.py`. Validator do Mago passou (h=26, y=35–60).

## Problema (audit)

| | Guerreiro | Mago (main, audit) |
|--|-----------|-------------------|
| Altura idle | ~27 px | ~38 px |
| Baseline | y=60 (ok) | y=48 (**+12 px flutuando**) |
| Leitura | membros + gear | blob capa/capuz |
| Ação | espada/escudo | só o cajado carrega |

## Alvo do redraw

| Item | Spec |
|------|------|
| Célula | 64×64, 7 colunas |
| Sheet | 7×4 = 28 (manter special 17–20 + meta 23–27) |
| Altura silhueta idle | **26–30 px** |
| Baseline | **y = 60** (mesmo FootPivot) |
| Largura corpo (sem cajado) | **16–22 px** |
| Cajado idle | visível à direita, **haste ≥ 2 px**, cristal + 4–8 partículas |
| Cajado idle altura | **não** passar de ~22 px acima da cabeça (cabe na caixa 30 px) |
| Capa | 1–2 painéis atrás; trim lilás fino; anima no walk |
| Robe | **escura** (indigo/preto-roxo); trim claro só nas bordas |
| Capuz | void preto, sem face; pico legível |
| Botas | 2 blocos no chão no walk; idle pode cobrir quase tudo mas a **barra da robe** toca y=60 |
| Paleta | 10–14 cores (incl. prata + cristal) |

## Foco de animação (ordem)

1. **Idle (0–3)** — bob 1 px + pulso do cristal + capa ±1 px  
2. **Walk (4–9)** — capa sway ±3 px; robe sobe e mostra botas  
3. **Jump (10–12)** — capa abre; cristal mais brilhante no ápice  
4. **Cast (13–16)** — cajado avança; partículas saem pra frente  
5. **Special (17–20)** — flare no cristal + capa aberta  
6. **Hurt (21–22)** — recuo, glow apaga  

Campo de magia (MagicWard) **não** entra no sheet: é VFX runtime ao redor do corpo (Etapa M4 de gameplay). O sprite só precisa de tint/capa estável.

## Referências

- Estrutura: `Resources/Guerreiro/sheet.png` frame 0  
- Identidade: `Assets/Art/Mago/mago_conceito.png`  
- Guia de chão: `Assets/Art/_pipeline/guia_baseline.png`  
- Template: `Assets/Art/_pipeline/HeroCell_64.md`

## Aceite do próximo redraw

- [x] Validator: idle h=26, y=35–60, pad=3
- [x] Não flutua (baseline y=60)
- [x] Cajado visível + pulso de cristal no idle
- [ ] Conferir na T1 lado a lado com o Guerreiro (Play Mode)
- [ ] Polir capa/capuz no Aseprite se ainda ler como blob de perto
