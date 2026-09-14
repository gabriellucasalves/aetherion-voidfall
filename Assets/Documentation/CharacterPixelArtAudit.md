# Auditoria de pixel art — heróis jogáveis (Etapa 1)

**Projeto:** Aetherion: Voidfall  
**Escopo:** só leitura/medição dos assets reais — sem reescrever sprites, tools ou gameplay.  
**Data da medição:** 2026-09-14  

## Fontes medidas

| Herói | Runtime (usado pelos Visuals) | Cópia Art | FRAME_MAP | Script Visual |
|-------|-------------------------------|-----------|-----------|---------------|
| Guerreiro | `Assets/Resources/Guerreiro/sheet.png` | `Assets/Art/Guerreiro/Guerreiro_sheet.png` (MD5 idêntico) | *(não existe)* | `GuerreiroVisual.cs` |
| Mago | `Assets/Resources/Mago/sheet.png` | `Assets/Art/Mago/Mago_sheet.png` (MD5 idêntico) | `Assets/Art/Mago/FRAME_MAP.md` | `MagoVisual.cs` |
| Arqueiro | `Assets/Resources/Arqueiro/sheet.png` | `Assets/Art/Arqueiro/Arqueiro_sheet.png` (MD5 idêntico) | `Assets/Art/Arqueiro/FRAME_MAP.md` | `ArqueiroVisual.cs` |

Também existe `Assets/StreamingAssets/Guerreiro/Guerreiro_sheet.png` (mesmo PNG do Guerreiro; importer = `DefaultImporter`, não é o path de runtime).

**Método:** dimensões via PNG; import via `.meta`; PPU/pivot efetivos via `Sprite.Create` nos Visuals; silhueta/cores/arma no frame idle (índice 0) com alpha > 10; peças de arma cruzadas com ASCII em `build_sheet.py`.

---

## Tabela comparativa (valores medidos)

| Critério | Guerreiro | Mago | Arqueiro |
|----------|-----------|------|---------|
| **Célula (W×H)** | 64×64 | 64×64 | 64×64 |
| **Sheet (W×H)** | 448×192 | 448×256 | 448×192 |
| **Grade / frames** | 7×3 = 21 | 7×4 = 28 (23–27 meta) | 7×3 = 21 |
| **PPU no `.meta` Resources** | 28 | 28 | 28 |
| **PPU efetivo no Visual** | **15** (`const float Ppu = 15f`) | **15** | **15** |
| **Pivot no `.meta` Resources** | `{x: 0.5, y: 0}` | `{x: 0.5, y: 0}` | `{x: 0.5, y: 0}` |
| **Pivot efetivo no Visual** | `(0.5, 3/64)` ≈ `(0.5, 0.046875)` | idem | idem |
| **Filter mode (`.meta`)** | Point (`filterMode: 0`) | Point | Point |
| **Filter mode (runtime)** | `FilterMode.Point` forçado no load | idem | idem |
| **Compression (Default)** | None (`textureCompression: 0`) | None | None |
| **Mipmaps** | Off (`enableMipMap: 0`) | Off | Off |
| **Readable** | `isReadable: 1` | `isReadable: 1` | `isReadable: 1` |
| **Altura de silhueta (idle)** | **~27 px** (y 34–60) | **~38 px** (y 11–48) | **~29 px** (y 30–58) |
| **Largura bbox idle** | ~30 px | ~28 px | ~38 px (arco+aljava) |
| **Largura de corpo (aprox.)** | ~19 px torso / **~28 px** c/ escudo | **~21 px** robe no meio | **~14–18 px** tronco / ~38 c/ arco |
| **Proporção cabeça/corpo** | Cabeça/elmo ~13 px (~**48%** da silhueta); ~2,1 “cabeças” | Capuz ~18 px (~**47%**); ~2,1 “cabeças” | Cabeça ~9 px (~**31%**); ~3,2 “cabeças” |
| **Pés / baseline na célula** | Última opacidade y=60; **3 px** vazios abaixo → **alinha** com FootPivot 3/64 | Última opacidade y=48; **15 px** vazios abaixo → **~12 px acima** do pivot | Última opacidade y=58; **5 px** vazios → **~2 px** acima do pivot |
| **Tamanho da arma (idle)** | Espada ~**7×15** (ASCII) / lâmina ~**8×16** px | Cajado ~**13×20** (ASCII) / metal+cristal ~**7×19** px | Arco ~**12×9** (ASCII) / região do arco ~**11×14** px |
| **Arma em ataque** | Swing amplifica bbox ~34–38 px de largura | Cast f13: staff ~**11×59** (quase a célula) | Draw/release: arco+flecha até ~40–46 px de largura |
| **Cores distintas (idle / frames anim.)** | **10 / 10** | **15 / 17** (frames 0–22) | **16 / 17** |
| **Chaves na paleta do build** | 11 | 18 (incl. hurt) | 18 |
| **GO pixel — localPosition** | `(0, -0.5, 0)` | `(0, -0.5, 0)` | `(0, -0.5, 0)` |
| **GO pixel — localScale** | `(1,1,1)` (flip só no parent) | idem | idem |

### Diferenças relevantes (resumo)

1. **Mesma pipeline de célula/grade/pivot nos três Visuals**, mas o Mago tem sheet mais alto (4 linhas) e silhueta ~40% mais alta em px.
2. **Divergência PPU:** `.meta` diz 28; runtime usa 15. Altura em world units (silhueta @ PPU 15): Guerreiro ≈ 1,80; Mago ≈ 2,53; Arqueiro ≈ 1,93.
3. **Baseline do Mago não casa com o FootPivot** compartilhado (flutua ~12 px / ~0,8 u @ PPU 15). Guerreiro é a referência correta de pé-no-chão.
4. **Art/Guerreiro/Guerreiro_sheet.png.meta** (e frames `g_*.png.meta`) **não** espelham o runtime: PPU 100, pivot centro, `filterMode: 1` (Bilinear), `spriteMode: 2` (Multiple), compression 1. O jogo **não** usa esse importer — só `Resources/*/sheet` + `Sprite.Create`.
5. **FRAME_MAP** documenta PPU 15 / pivot no pé para Arqueiro; o `.meta` Resources ainda lista PPU 28 / pivot y=0 (valores sobrescritos no código).

---

## Pixel Perfect Camera e escala

| Item | Achado |
|------|--------|
| Pacote `com.unity.2d.pixel-perfect` | **Ausente** em `Packages/manifest.json` |
| Componente Pixel Perfect Camera | **Não encontrado** em cenas/scripts |
| Câmera T1 | Ortográfica; `orthographicSize = 5.35f` (`GroundT1Controller`) |
| Collider do herói (todos) | `BoxCollider2D` size `(0.58, 1.15)`, offset `(0, 0.08)` — independente do sprite |
| Escala do root do herói | Spawn sem scale custom; `FaceVisual` só espelha `localScale.x` |

---

## Referência estrutural e legibilidade

**Melhor referência estrutural: Guerreiro.**

Motivos medidos:

- Pés exatamente no FootPivot (3 px de padding).
- Layout idle compacto e estável (bbox y 34–60 em todos os frames de combate medidos).
- Silhueta com **membros e equipamento legíveis**: elmo+chifres, escudo à esquerda, espada à direita, pernas distintas na base.
- Sheet 7×3 alinhado ao Arqueiro; Mago espelha a mesma API visual (célula 64, 7 cols, PPU 15, FootPivot).

### Por que a legibilidade difere

| | Guerreiro | Mago | Arqueiro |
|---|-----------|------|----------|
| Leitura em silhueta | Alta — limbos/gear externos | Baixa — **blob** de capuz+robe | Média-alta — tronco fino, arco/aljava abrem a forma |
| Ação animada | Espada/escudo mudam o contorno | Depende do **cajado/cristal** e tilt; corpo quase sólido | Arco draw→release muda bem o lado esquerdo |
| Identidade | Magma/vermelho, elmo | Capuz void (sem face bege), roxo, cristal — alinhado ao conceito | Capa verde, arco de madeira |

O Mago (como no conceito hooded purple) prioriza massa de tecido: capuz grande (~18 px), robe contínua (~21 px no meio), mãos/pés embutidos. Sem o staff e o trim claro, frames de walk/idle confundem-se mais que no Guerreiro.

---

## Mapas de frame (docs existentes)

### Guerreiro (`GuerreiroVisual` — sem FRAME_MAP.md)

| Frames | Clip |
|--------|------|
| 0–3 | idle |
| 4–9 | walk |
| 10–12 | jump |
| 13–16 | attack (também placeholder de special) |
| 17–18 | block |
| 19–20 | hurt |

### Mago (`FRAME_MAP.md` + `MagoVisual`)

| Frames | Clip |
|--------|------|
| 0–3 | idle |
| 4–9 | walk |
| 10–12 | jump |
| 13–16 | cast |
| 17–20 | special |
| 21–22 | hurt |
| 23–27 | silhueta / paleta / spare (não animados) |

### Arqueiro (`FRAME_MAP.md` + `ArqueiroVisual`)

| Frames | Clip |
|--------|------|
| 0–3 | idle |
| 4–9 | walk |
| 10–12 | jump |
| 13–15 | shoot |
| 16–17 | dash |
| 18 | special |
| 19–20 | hurt |

---

## Conclusões (Etapa 1)

1. Os três heróis compartilham a **mesma convenção de runtime** (célula 64, 7 colunas, PPU 15, pivot no pé `3/64`, Point filter, GO em y=−0,5). Isso é o contrato a preservar em etapas futuras.
2. O **Guerreiro** é a referência de baseline, proporção chibi compacta e silhueta legível; qualquer normalização de arte deve partir dele.
3. O **Mago** é o outlier visual: mais alto em px, pés 12 px acima do pivot comum, corpo “blob” de capa/capuz — a arma e o glow carregam a leitura de ação.
4. O **Arqueiro** está perto do Guerreiro em altura/baseline (±2 px), com silhueta mais larga por arco/aljava e proporção um pouco menos “cabeçuda”.
5. **Não há Pixel Perfect Camera**; nitidez depende de Point filter + escala inteira da câmera ortográfica.
6. Próximas etapas (fora deste PR) devem alinhar baseline do Mago ao FootPivot e/ou unificar documentação `.meta` (28) vs código (15), **sem** alterar combate/movimento nesta etapa.
