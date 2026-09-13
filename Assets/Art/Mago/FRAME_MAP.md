# Mago spritesheet — mapa de frames

**Arquivo runtime:** `Assets/Resources/Mago/sheet.png`  
**Cópia de arte:** `Assets/Art/Mago/Mago_sheet.png`  
**Aseprite:** `Assets/Art/Mago/Mago.aseprite`  
**Gerador:** `Assets/Art/Mago/build_sheet.py` (ASCII parts → PNG, espelha Guerreiro)  
**Import:** `import_frames.lua` + `tag_anims.lua`

| | |
|--|--|
| Célula | 64×64 |
| Grade | **7 colunas × 4 linhas** = 28 frames |
| Tamanho | 448×256 |
| Leitura | índice linear L→R, cima→baixo (igual Guerreiro / `MagoVisual`) |

## Animações

| Frames | Clip | Tag Aseprite | Notas |
|--------|------|--------------|--------|
| 0–3 | idle | idle | Bob + pulso do cristal |
| 4–9 | walk | walk | 6 passos, staff tilt |
| 10–12 | jump | jump | sobe / ápice / cai |
| 13–16 | cast | cast | Staff thrust + orbe (`IsAttacking`) |
| 17–20 | special | special | Arcane nova / crystal flare (`IsCastingSpecial`) |
| 21–22 | hurt | hurt | robe vermelha |
| 23 | silhueta | — | referência de leitura |
| 24 | paleta | — | 16 swatches |
| 25–27 | spare | — | variantes idle |

## Paleta (dark fantasy, ~12–16 cores)

| Key | Uso | RGB |
|-----|-----|-----|
| k | outline | 10,8,18 |
| n | hood void / face shadow | 18,14,32 |
| d/s/m/r/l/h | robe indigo→lilac | 32–178 purple ramp |
| a/i | silver staff | 150–230 grey |
| p/c/w | purple crystal glow | 160,60,220 → white |
| b | boots | 28,18,40 |
| t | belt/pouch | 92,58,42 |
| u/v | hurt tint | reds |

Sem face bege — capuz com interior em sombra.

## Pipeline (Mac)

```bash
cd "Assets/Art/Mago"
python3 build_sheet.py
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script import_frames.lua
# opcional retag:
# aseprite -b Mago.aseprite --script tag_anims.lua
```

## Verificar em T1

1. `GroundT1Controller.ForceMagoForTesting = true`
2. Idle/walk ao parado/andar; clique/J → cast; L/Q → special; tomar dano → hurt

`MagoVisual.cs` frame ranges **unchanged** (7×4 / 28).
