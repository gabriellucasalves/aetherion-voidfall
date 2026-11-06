# PROMPT ÚNICO — Astra / Codex · Polish Aetherion: Voidfall (1 tiro)

Você tem **uma** sessão. Faça tudo abaixo sem pedir confirmação intermediária, sem redesenhar o GDD e sem inventar features novas. Ao final: branch, Play Mode screenshots, WebGL na Vercel personal, URL + SHA.

## Projeto
- Pasta Unity: `/Users/gabriellucas/Desktop/Aetherion_Atmos_V1` (pack Atmos **já ativo**, `DefaultEnabled=true`, `AllowLegacyFallback=false`).
- Repo: `github.com/gabriellucasalves/aetherion-voidfall`
- Branch nova: `polish/astra-hud-res-cenario` (não merge em `main`).
- Deploy: Vercel **personal** `gabriellucassilva81` → projeto `aetherion-voidfall-gules` → `https://aetherion-voidfall-gules.vercel.app`
- Refs estilo: Owlboy / Chrono Trigger / Iconoclasts — 16-bit denso, Point, **sem** Bilinear/blur/MSAA no mundo.
- Mock HUD alvo (copiar se faltar): `~/Desktop/trabalho jogo sexta/` ou anexos `mockup_hud_ptbr.png` / `mockup_guerreiro_leve_hud_portal.png`. Pack arte: `Assets/Resources/T1_Hybrid_Atmos/`.

## Problema (Direção)
O WebGL live ainda parece **chapado/pixelado grosso**: barras HUD finas no canto, sem ícone do herói, cenário Atmos sem “capricho” de profundidade e o RT 768×432 ainda estoura texel demais em Full HD. Precisa **polir + densificar apresentação + HUD novo** num único passo.

---

## A — Apresentação (subir resolução interna)
Arquivo típico: `Assets/Scripts/Stages/PixelPresentation.cs` (e qualquer hardcode 768/432).

| Constante | Agora | Novo |
|---|---|---|
| InternalWidth | 768 | **1536** |
| InternalHeight | 432 | **864** |
| OrthoSize | 6.75 | **6.75 fixo** (não acoplar à altura) |
| Filter | Point | Point |
| PPU sprites Atmos | 32 | **32** (não mudar) |

Efeito: framing do level idêntico; px/unidade no RT dobra de novo; Point + escala inteira no letterbox. **Proibido** Bilinear no RT/sprites do mundo.

Validar: Guerreiro e plats com o **mesmo tamanho aparente** na tela que hoje; letterbox limpo em 1920×1080 / 2560×1440.

---

## B — HUD (centro superior, 3 barras grandes + ícone)
Arquivos típicos: `GroundT1Controller.BuildHud`, `UiKit.cs` (ou equivalente UI Overlay 1920×1080).

**Layout alvo (substitui o HUD atual):**
1. **Faixa/painel** no **topo-centro** do canvas (não canto esquerdo solto): fundo escuro opaco ~`Color(0.05, 0.04, 0.08, 0.92)`, borda preta 2 px, altura ~90–120 px, largura suficiente para ícone + 3 barras.
2. **Ícone circular do herói** à **esquerda do painel** (~64–80 px), crop idle da sheet / `HeroAppearance` / `CharacterData`, borda escura 2–3 px. **Troca com herói ativo** (Guerreiro / Mago / Arqueiro).
3. **Três barras empilhadas** à direita do ícone, **centradas no eixo horizontal do topo** (bloco HUD no meio da tela):
   - Altura **36–44** cada · largura **320–400**
   - Track opaco escuro `alpha ≥ 0.95` + outline preto 2 px
   - **VIDA** fill `rgb(0.85, 0.15, 0.18)`
   - **ESCUDO** fill `rgb(0.35, 0.65, 0.95)` — Guerreiro; Mago = label **CAMPO**; Arqueiro = **ocultar** 2ª barra
   - **ESPECIAL** fill laranja/dourado atual; cheia → texto legível `PRONTO (L/Q/E)`
4. Labels PT-BR `VIDA` / `ESCUDO|CAMPO` / `ESPECIAL`: font ≥ 18–22, branco + outline/sombra preta.
5. Remover labels/fills antigos soltos no Overlay (as de 16–18 px).
6. Overlay morte/pausa pode cobrir o jogo; **tracks das barras permanecem legíveis**.
7. Título `Fase I - Antes da Queda` pode ficar abaixo do painel ou deslocado; não pode competir com as barras. Controles no rodapé: manter, tipografia legível.

Aceite HUD: em céu creme/nuvem clara as 3 barras + ícone são óbvios a 1 metro do monitor.

---

## C — Polir cenário Atmos (sem redesenhar level geometry)
Pack já aprovado: `T1_Hybrid_Atmos` (P1…P5, fog, veil, plats densas, gaps Mario). **Não** fechar/criar gaps; **não** trocar layout 72u; **não** voltar fallback GoldenAge.

Polimento visual (código + ajustes de material/cor nos SpriteRenderers — sem Aseprite novo nesta sessão):

1. **Hierarquia de planos** (valor, não blur):
   - Far (P1/P2): tint mais **frio/escuro/dessaturado** (~−15–25% lum, saturação ↓); Cidadela pequena permanece.
   - Mid (P3/P4): contraste médio; garantir `overlay_p2b_fog` e `overlay_p3b_veil` **visíveis** (alpha útil, sorting certo).
   - Playfield (P5): contraste alto nas arestas; plats ardósia + musgo lavanda nítidas; rim quente esquerda / sombra fria direita preservados.
2. Parallax factors intactos: **0.08 / 0.22 / 0.40 / 0.62 / 1.0**; snap 1/32 u.
3. Sorting orders conforme `PARALLAX.md` do pack.
4. Abismos dos gaps: fundo **escuro** (não céu vazando); trigger de queda → `"VOCÊ CAIU"` intacto.
5. Sem filtros, sem reescalar bilinear, sem redesenhar props/árvores/estátuas pixel a pixel nesta sessão. Se um sprite do pack não estiver wired, **wire** — não reinventar.

Meta: far “pra trás”, playfield “pra frente”, névoa discreta entre montanhas e mid, **sem lavar** a cena.

---

## D — Restrições
- Não On-Demand / serviços externos além do deploy Vercel já usado.
- Não merge `main`. Não alterar combate, Feixe Infernal, inimigos, portal (só se HUD overlap exigir z-order).
- Não Bilinear no mundo. Não mudar PPU 32 do pack.
- Guerreiro sheet: sem redesign; só portrait HUD.

---

## E — Checklist de aceite (obrigatório antes do report)
- [ ] Play Mode 1536×864 Point, Ortho 6.75, framing igual
- [ ] HUD topo-centro: ícone + 3 barras grossas legíveis no céu claro
- [ ] Retrato muda com herói; Escudo/Campo/ocultar corretos
- [ ] Atmos ativo; far mais profundo; fog/veil ligados; plats densas; gaps mortais
- [ ] Sem shimmer/blur no parallax
- [ ] WebGL build sem erros de Console; deploy Vercel personal
- [ ] Screenshots: (1) início sem HUD se útil, (2) início com Guerreiro+HUD, (3) gap, (4) browser live

## F — Entrega
1. Branch + SHA + diff resumido (arquivos).
2. URL `https://aetherion-voidfall-gules.vercel.app` atualizada.
3. Prints anexos.
4. Uma linha: o que **não** coube neste tiro (se houver).

Comece inspecionando `PixelPresentation`, `BuildHud` e o loader Atmos; implemente A→B→C; valide Play; só então WebGL+deploy.
