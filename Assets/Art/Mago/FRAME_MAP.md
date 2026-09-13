# Mago spritesheet — mapa de frames

**Arquivo runtime:** `Assets/Resources/Mago/sheet.png`  
**Cópia de arte:** `Assets/Art/Mago/Mago_sheet.png`  
**Gerador:** `Assets/Art/Mago/build_sheet.py` (PIL; Aseprite opcional depois)

| | |
|--|--|
| Célula | 64×64 |
| Grade | **7 colunas × 4 linhas** = 28 frames |
| Tamanho | 448×256 |
| Leitura | índice linear L→R, cima→baixo (igual Guerreiro) |

## Animações

| Frames | Clip | Notas |
|--------|------|--------|
| 0–3 | idle | Bob + pulso do orbe |
| 4–9 | walk | 6 passos |
| 10–12 | jump | sobe / ápice / cai |
| 13–16 | cast | Orbe básico (`IsAttacking`) |
| 17–20 | special | Núcleo Arcano (`IsCastingSpecial`) |
| 21–22 | hurt | robe avermelhada |
| 23 | silhueta | referência de leitura |
| 24 | paleta | 12 swatches |
| 25–27 | spare | variantes idle |

## Paleta (dark fantasy)

Índigo robe, pele quente, cajado marrom, orbe/glow ciano (~12 cores).

## Verificar em T1

1. `GroundT1Controller.ForceMagoForTesting = true`
2. Idle/walk ao parado/andar; clique/J → cast; L/Q → special; tomar dano → hurt
