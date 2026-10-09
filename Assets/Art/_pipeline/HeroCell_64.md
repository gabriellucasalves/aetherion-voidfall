# Template Aseprite — célula de herói 64×64 (Etapa 3)

Arquivo de trabalho: um sprite **64×64**, 1 layer de arte + 1 layer `GUIDE` (não exportar).

## Layers

| Layer | Uso |
|-------|-----|
| `GUIDE` | linha y=60 (baseline / pés), caixa 24–30 px de altura, eixo x=32 |
| `ART` | pixel art Point, paleta do herói |

## Guias (coordenadas 0 = topo)

- **Baseline / pés:** y = **60** (pixels 61–63 vazios — FootPivot 3/64)
- **Topo máximo da silhueta idle:** y ≥ **31** (altura ≤ 30 px até y=60)
- **Centro horizontal:** x = 32 (pivot 0.5)
- **Facing:** direita (arma / cajado / arco no lado +x)

Overlay PNG: `guia_baseline.png` (abrir como referência ou colar na layer GUIDE).

## Tags (copiar do FRAME_MAP do herói)

Guerreiro: idle 1–4, walk 5–10, jump 11–13, attack 14–17, block 18–19, hurt 20–21  
Mago: idle 1–4, walk 5–10, jump 11–13, cast 14–17, special 18–21, hurt 22–23  
Arqueiro: idle 1–4, walk 5–10, jump 11–13, shoot 14–16, dash 17–18, special 19, hurt 20–21  

(Aseprite é 1-indexed; os Visuals são 0-indexed.)

## Export

```bash
python3 Assets/Art/_pipeline/import_hero_sheet.py Mago
python3 Assets/Art/_pipeline/validate_hero_sheet.py Mago
```

Isso monta a grade 7×N e copia para `Resources/<Hero>/sheet.png` **só quando você rodar o import** — não é automático.
