# Auditoria visual — sensação 16-bit

**Projeto:** Aetherion: Voidfall  
**Escopo:** aparência. Não altera sprites, combate nem kits.  
**Direção aceita:** side-scroller de ação no vocabulário do Super Nintendo, não NES esticado e não Neo Geo.

Fontes: [limitações do NES](https://www.reddit.com/r/nes/comments/8ksr6c/the_nes_limitations/), [NES vs SNES no r/PixelArt](https://www.reddit.com/r/PixelArt/comments/n3zkd1/classic_nes_vs_snes_palette_which_is_the_best/), [parallax 16-bit](https://www.reddit.com/r/PixelArt/comments/wjoolj/16bit_super_nintendo_style_parallax_forest/), [PPU do SNES](https://www.sneslab.net/wiki/SNES_PPU_for_NES_developers), [sprites do SNES](https://forums.nesdev.org/viewtopic.php?t=15953), [orçamento de sprite](https://undisbeliever.net/blog/20160127-managing-sprite-resources.html). Medição anterior dos heróis: `CharacterPixelArtAudit.md`.

## Veredito

Mudar o rótulo de 8-bit para 16-bit não cria a sensação. O cenário de T1 já está escrito como dark fantasy em camadas (`T1Scenery`: céu, fundo, cidade, props, plano jogável, primeiro plano). O que quebra a leitura de cartucho é o herói chibi (~27 px, cabeça ~48% do corpo) desenhado numa câmera moderna com dois tamanhos de pixel (herói a PPU 15, cenário a PPU 16).

Referência de desenho: Mega Man X, Super Castlevania IV, Demon’s Crest, Contra III. Metal Slug fica de fora: sprite de arcade e dezenas de frames são outro orçamento.

## O que a geração fazia

- **NES.** Cerca de 3 cores visíveis por sprite, poucas camadas, contorno preto, forma grande em poucos pixels.
- **SNES.** Até 15 cores por sprite, tiles de fundo com 16 cores, 2 ou 3 camadas e parallax. A rampa (sombra, base, luz, brilho) substitui o dither. O contorno preto não cerca cada forma.
- **Tela.** Cerca de 256×224, o mesmo pixel no herói e no fundo. Filtro de CRT ajuda dither antigo; a sensação moderna (The Messenger, Eastward) vem da grade e da paleta.

## O que o jogo já tem

- Filtro Point nos heróis.
- Célula 64×64, grande o bastante para um sprite de ação do SNES se o desenho preencher a célula.
- Paleta: Guerreiro ~10 cores (faixa NES); Mago ~15 (faixa SNES).
- T1 em camadas com parallax, bruma, glow e props à frente do herói (`ForeProp`, sorting 30).
- Walk de 6 frames, na faixa comum do SNES.

## O que quebra a sensação

1. **Herói 8-bit dentro da célula 16-bit.** O contrato antigo pedia silhueta de 24–30 px e cabeça de 40–50%. Num SNES de ação o corpo ocupa cerca de 40–56 px, com 3,5 a 5 cabeças, e a arma muda o contorno.
2. **Dois pixels na mesma imagem.** Câmera em `orthographicSize = 5.35`, sem framebuffer fixo. Herói PPU 15, cenário PPU 16.
3. **Sombreamento plano.** 8–12 cores chapadas com outline escuro. O metal do Guerreiro precisa de rampa de 4 tons e contorno só onde a silhueta se perde.
4. **Ação curta.** Corte do Guerreiro em 4 frames; o especial reusa o ataque. O Mago já tem clip `special` (17–20).
5. **Cidade lavada por tint.** `SpriteRenderer.color` em `(0.8, 0.8, 0.9)` achata o detalhe da camada. Cada camada do SNES guarda a paleta no pixel, não num multiply.
6. **Heróis fora do mesmo baseline.** O Mago flutua ~12 px e é ~40% mais alto. Os três precisam da mesma faixa e do pé em y=60.

## O que não fazer

- Não dobrar a célula para 128. A ação do SNES cabe em 64 se o desenho ocupar a célula.
- Não mirar Metal Slug.
- Não resolver com filtro de CRT em cima do chibi atual.
- Não reescrever combate, follow da câmera nem kits.

## Contrato aplicado no código (sem redesenhar sprites)

- Framebuffer interno **384×216**, escala inteira, filtro Point (`PixelPresentation`).
- Herói, inimigo e T1 no mesmo PPU: `PixelArt.Ppu` (**16**).
- O tamanho desenhado hoje (silhueta ~27 px) continua até o redesenho. O contrato de desenho novo está no style guide.

## Contrato de desenho (próxima arte, ainda não desenhada)

- Silhueta idle **44–52 px** na célula 64, baseline y=60, cabeça ~25–32% da altura.
- 12–15 cores, rampa de 4 tons na matéria principal. Contorno seletivo.
- Ataque com antecipação, golpe e recuperação (6+ frames). Especial do Guerreiro com clip próprio.
- Cenário: paleta no pixel; tiles com variação; primeiro plano já existe e deve continuar na frente do herói (sorting acima de 12).
- Prova: um frame de Mega Man X ou Super Castlevania ao lado do Guerreiro, no mesmo zoom. Se a densidade de sombra não conversar, o frame ainda está em 8-bit.
