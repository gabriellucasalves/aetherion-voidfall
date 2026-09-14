# Arqueiro spritesheet — mapa de frames

**Arquivo runtime:** `Assets/Resources/Arqueiro/sheet.png`  
**Cópia de arte:** `Assets/Art/Arqueiro/Arqueiro_sheet.png`  
**Gerador:** `Assets/Art/Arqueiro/build_sheet.py`  
**Runtime:** `ArqueiroVisual.cs`

| | |
|---|---|
| Célula | 64×64 Point |
| Grade | **7 colunas × 3 linhas** = 21 frames |
| Tamanho | 448×192 |
| Leitura | índice linear L→R, cima→baixo (igual Guerreiro) |
| PPU | 15 · pivot no pé |

## Animações

| Frames | Clip | Notas |
|--------|------|--------|
| 0–3 | idle | Respiração + arco em guarda |
| 4–9 | walk | 6 passos |
| 10–12 | jump | sobe / ápice / cai |
| 13–15 | shoot | **draw → release → recover** — flecha (`Arrow`) spawna no release |
| 16–17 | dash | impulso (Shift) |
| 18 | special | soltura em leque (wind-up reusa 13–14) |
| 19–20 | hurt | recuo |

### Sync tiro básico (FASE 4)

- `ArqueiroVisual` toca `{13, 13, 14, 15}` @ ~12 fps enquanto `IsAttacking`.
- `Arrow.MuzzleDelay` ≈ **0.16s** — flecha aparece no frame de soltar (14).
- `PlayerCombat.FireArcher` define `_attackLeft ≈ 0.42s` para cobrir o clip.

Índices do sheet **não mudaram** nesta fase — só o conteúdo visual dos frames 13–15 ficou mais legível.

## Paleta (dark fantasy floresta)

| Key | Uso |
|-----|-----|
| k/n/h | outline / sombra |
| d/b/m/w | madeira / couro |
| g/e/l/t | capa verde |
| s/p | pele |
| y/c/a/u/o | ponta / brilho / pena curta no sheet |
