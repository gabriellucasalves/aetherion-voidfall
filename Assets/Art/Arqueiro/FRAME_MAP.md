# Arqueiro Arcano spritesheet — mapa de frames

**Arquivo runtime:** `Assets/Resources/Arqueiro/sheet.png`  
**Cópia de arte:** `Assets/Art/Arqueiro/Arqueiro_sheet.png`  
**Tiras de ação (override em runtime):** `Assets/Resources/TrioAnimations/arqueiro/<acao>.png`  
**Runtime:** `ArqueiroVisual.cs`

| Propriedade | Valor |
|---|---|
| Célula | `64×64` Point (sem AA, sem blur, sem mipmaps) |
| Altura do herói | `27 px` (pés em `y = 61`, pico do capuz azul em `y = 34`, `FootPivot = 3/64`) |
| Grade (`sheet.png`) | **7 colunas × 3 linhas** = 21 frames (`448×192`) |
| PPU | `16` (`PixelArt.Ppu`) |

## Identidade Visual (Arqueiro Arcano de Aetherion)

- **Cabeça / Capuz:** capuz de pano azul-noturno caído (`#1E3052` / `#2C4A76` / `#446C9E`), interior em sombra (`#121020`), dois olhos ciano brilhantes (`#6BF7FF`), gola de tecido azul-noturno e tiara/diadema fino de `1 px` em ouro (`#FFF49C` / `#F8CE5C`) com joia ciano na testa e pequena asinha lateral.
- **Corpo e Equipamento:** peitoral leve de mármore (`#F0ECF8`) com gema ciano no peito, ombreira e cinturão de ouro imperial, capa/manto azul-petróleo com padrões de runa ciano, aljava dourada com plumas de energia ciano, arco recurvo de cristal/mármore com pontas de ouro e corda de luz ciano.

## Mapa do `sheet.png` (21 frames) e Overrides `TrioAnimations/arqueiro/`

| Frames (`sheet.png`) | Override (`TrioAnimations/arqueiro/`) | Contagem | Notas |
|---|---|---|---|
| `0–3` | `idle.png` (`128×64`) | 2 (esp. em 4) | Respiração com arco de cristal em guarda |
| `4–9` | `andar.png` (`384×64`) | 6 | Passada aberta (usada também ao caminhar mirando a 40% da velocidade) |
| `10–12` | `pulo.png` (`256×64`) | 3 | Subida (`10`), ápice (`11`), queda (`12`) |
| `13–15` | `ataque.png` (`320×64`) | **5** | **Tiro Carregado:** `0` Puxar (`<0.25s`), `1` Carga Média (`0.25–0.80s`), `2` Carga Máxima (`>=0.80s`), `3` Soltar (spawn imediato de 1 flecha), `4` Recuo |
| `16–17` | *(do `sheet.png`)* | 2 | Dash (Shift) com pós-imagem (`DashGhost`) |
| `18` | `especial_perfurante.png` (`384×64`) | 6 | **Flecha Perfurante (L/Q):** 1 única flecha que atravessa até 3 alvos e para no terreno |
| `19–20` | `dano.png` (`128×64`) | 2 | Recuo de dano (cancela carga ativa sem disparar) |
| *(slots 27–31)* | `morte.png` (`320×64`) | 5 | Queda progressiva até o chão |
| *(slots 32–37)* | `correr.png` (`384×64`) | 6 | Corrida rápida |

## Mecânica de Tiro Carregado (1 única flecha por disparo)

- **Puxar (`Press`):** inicia somente na borda de descida (`GetMouseButtonDown(0)`, `GetKeyDown(J)` ou transição `false -> true` de `MobileControls.AttackHeld`).
- **Carregar (`Hold`):** acumula `ChargeTime`, reduz velocidade de movimento para `40%`, exibe a pose correspondente (`Puxar` / `Carga Média` / `Carga Máxima`) e o indicador de 3 segmentos acima do capuz.
- **Soltar (`Release`):** dispara **exatamente 1 flecha** (`muzzleDelay = 0f`) no nível atingido:
  - **Nível 1 (`< 0.25 s`):** `40%` do dano base, menor velocidade/alcance, leve queda no fim (`TrioVfx/flecha_n1` + `flecha_impacto_n1`).
  - **Nível 2 (`0.25–0.80 s`):** dano interpolado de `40%` a `100%`, haste dourada/cristal (`TrioVfx/flecha_n2` + `flecha_impacto_n2`).
  - **Nível 3 (`>= 0.80 s`):** `100%` do dano base, alta velocidade e alcance, núcleo branco-ciano com aletas douradas, rastro mágico e impacto estelar (`TrioVfx/flecha_n3` + `flecha_impacto_n3`).
- **Recuo (`Cooldown`):** cooldown fixo de `0.30 s` após soltar; tomar dano, usar dash, especial ou morrer cancela a carga sem disparar.
