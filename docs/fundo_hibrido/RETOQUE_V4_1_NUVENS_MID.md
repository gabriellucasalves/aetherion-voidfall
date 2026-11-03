# Retoque V4.1 — nuvens + mid (Owlboy / Chrono Trigger)

**Status:** **aprovado** pela Direção Criativa (2026-10-26) — uma só Cidadela  
**Base:** pintura V4 já em `T1_Hybrid/` + PR #12  
**Referência de direção:** `fundo_fase_refino_v4_1.jpg` (NÃO downscale)  
**Inspirações (composição/iluminação, nunca copiar assets):** Owlboy (nuvens volumosas, profundidade atmosférica), Chrono Trigger (harmonia de cor, vegetação, riqueza de mid)

## Escopo (só fundo)
- Polir `clouds_a`…`clouds_d` (e sky se precisar de faixa sob as nuvens)
- Refinar detalhes em `city.png` / mid (vegetação, musgo em ruínas, flora perto da água)
- **Não** mexer em ground / plat / pilares da frente

## Nuvens (Owlboy-like)
1. Massas empilhadas com volume claro: highlight creme topo → peach mid → undershadow lavanda
2. Clusters / dither suave nas transições; silhuetas distintas entre as 4 camadas
3. Paralaxe 0,88–0,92 mantido; alfa só 0/255; sem retângulo opaco
4. Evitar “almofada” repetitiva: variar comprimento e altura das bandas

## Mid / cenário (Chrono-like)
1. Tufts de grama e flora pequena perto da água (poucos pixels, paleta GPL)
2. Musgo / verde suave em arco e estelas
3. Separação de silhueta mid vs far (evitar lama de cor)
4. Orbs / cristal Aether continuam pontuais
5. Manter Parasol (platô teal) vs Weeping (cascata lavanda)

## Aceite
- [ ] Nuvens com volume legível e identidades distintas
- [ ] Mid mais rico sem competir com herói/HUD
- [ ] 0 cores fora da GPL; Point; PPU 16
- [ ] ground/plataformas intocados

## Correção 2026-10-26
- **Uma única** estrutura: Cidadela Celeste flutuante. Montanhas = só natureza (sem segundo castelo/torre).
- Plataformas flutuantes na ref de IA são ruído — **não** pintar nesta etapa.
