# Tools de pixel art — como usar (Etapas 3–7)

Idioma: PT-BR. Valores = contrato medido no audit (PPU **15**, pivot **3/64**, Point).

---

## Aseprite — template (Etapa 3)

**Arquivo:** `Tools/Aseprite/CreateCharacterTemplate.lua`

1. Abra o Aseprite.  
2. **File → Scripts → Open Scripts Folder** e copie o `.lua` para lá (ou rode via **File → Scripts** se já estiver no path do projeto).  
3. Execute **CreateCharacterTemplate**.  
4. Surge um sprite **64×64** com camadas `GUIDE - *` (ocultáveis) + `BODY`, `CLOTHING`, `EQUIPMENT`, `OUTLINE`, `SHADOW`, `HIGHLIGHT`, `FX`.  
5. As linhas horizontais seguem o Guerreiro (crown 34 … feet 60 / ground 61).  
6. Esconda todas as `GUIDE - *` antes de exportar.

---

## Unity — Import Tool (Etapa 4)

**Menu:** `Tools > Pixel Art > Import Tool`  
**Script:** `Assets/Editor/PixelCharacterImportTool.cs`

1. Selecione um ou mais textures/sprites no Project.  
2. A janela lista o que **vai mudar** vs o padrão do projeto.  
3. Confira o preview e clique **Aplicar** (suporta Undo).  
4. Aplica: Sprite, Point, Compression None, mipmaps off, Read/Write on, **PPU 15**, pivot `(0.5, 3/64)`.

Não inventa settings — espelha o que os `*Visual.cs` usam de fato.

---

## Unity — Character Validator (Etapa 5)

**Menu:** `Tools > Pixel Art > Character Validator`  
**Script:** `Assets/Editor/CharacterSpriteValidator.cs`

Compara Guerreiro | Arqueiro | Mago:

- resolução da célula/sheet  
- PPU (meta vs runtime 15)  
- pivot  
- Filter / Compression / mipmaps  
- altura visual / baseline / bbox (frame 0)  
- escala do GO (se houver na cena) / posição dos pés  

Legenda: ✅ ok · ⚠ divergência conhecida · ❌ fora do contrato.  
**Nunca** altera PNGs sozinho.

---

## Unity — Proportion Debug (Etapa 6)

**Script:** `Assets/Scripts/Player/CharacterProportionDebug.cs`

1. Adicione o componente a um herói (root ou filho `*Pixel`).  
2. No Inspector: toggle **Show Guides**.  
3. No Scene View aparecem linhas: topo, cabeça, ombros, mãos, cintura, joelhos, pés, chão.  
4. `Assume Visual Offset` (default on) aplica o `y = -0.5` dos Visuals.

Não mexe em combate — só gizmos.

---

## Unity — Character Comparison (Etapa 7)

**Menu:** `Tools > Pixel Art > Character Comparison`  
**Script:** `Assets/Editor/CharacterComparisonWindow.cs`

Coloca os três sheets lado a lado (mesmo chão, mesmo PPU, espaçamento uniforme) com as linhas de proporção.  
Ferramenta de editor apenas — não altera prefabs de produção.

---

## Docs relacionados

| Doc | Conteúdo |
|-----|----------|
| `CharacterPixelArtAudit.md` | Medidas Etapa 1 |
| `CharacterStyleGuide.md` | Spec de construção + paletas |
| `MagoRedesignSpec.md` | Redesign do Mago (spec only) |
