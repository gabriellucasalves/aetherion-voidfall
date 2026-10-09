# Cut-ins de especial — mapa de frames

Overlay curto (~0.45s) exibido quando o especial dispara, estilo fighting
game 8/16-bit e anime anos 90.

**Gerador:** `build_cutins.py` (roda so a mao — nao redesenha sheets de gameplay)
**Runtime:** `Assets/Scripts/UI/SpecialCutIn.cs` (anexado por `HeroAppearance.Build`)
**Saida:** `Assets/Resources/CutIns/<id>.png`

| | |
|--|--|
| Celula | 240x135 (16:9 — vira 1920x1080 no canvas) |
| Grade | 5x2 = 1200x270 (10 frames) |
| Playback | 14 fps, uma passada (~0.71s), canvas sortingOrder 140 |
| Gatilho | borda de subida de `PlayerCombat.IsCastingSpecial` |

## Frames

| Frame | Conteudo |
|-------|----------|
| 0 | Super flash: faixa diagonal escura + silhueta branca do heroi |
| 1-2 | Busto entra deslizando da direita; speed lines correndo |
| 3-8 | Busto assentado com shake de 1px; linhas continuam |
| 9 | Outro: faixa esmaece (alpha 150) |

## Herois e especiais

| Id | Especial (GDD/scripts) | Cores da faixa |
|----|------------------------|----------------|
| guerreiro | Corte/Onda de Energia (`SwordWave`, anim 0.45s) | aco escuro + laranja energia |
| mago | Nucleo Arcano (`ArcaneNova`, anim 0.70s) | indigo/void + lilas |
| arqueiro | Rajada em leque (flechas, anim 0.55s) | verde/terra + dourado |

O busto e recortado do frame 0 (idle) do sheet de gameplay do proprio heroi
e ampliado 5x nearest neighbor — mesma identidade, zero redesenho. Se o idle
mudar, rodar o gerador de novo.

## Referencias de estilo

- **Marvel vs. Capcom / MvC2 (CPS-2, 1998-2000):** freeze + flash de tela no
  hyper combo, personagem em silhueta clara, fundo em faixa de cor chapada.
- **KOF '94-'98 (Neo Geo):** escurecimento subito da tela + personagem
  piscando em branco no startup do DM/SDM.
- **Animes anos 80/90 (Saint Seiya, Yu Yu Hakusho):** cut-in rapido com
  speed lines horizontais e busto estatico em zoom antes do golpe.

## Pipeline (Mac)

```bash
cd "Assets/Art/CutIns"
python3 build_cutins.py
```

Depende so de Pillow (PIL) — mesmo ferramental dos sheets dos herois.
Polimento opcional frame a frame pode ser feito no Aseprite abrindo o PNG
(File > Import Sprite Sheet, 240x135, grade 5x2), mas o PNG gerado ja e final.

## Verificar em T1

1. Entrar na T1 com cada heroi
2. L/Q (especial) -> cut-in deve segurar (~0.7s) e sumir sozinho
3. Especial em cooldown nao dispara cut-in (gatilho e o IsCastingSpecial)
