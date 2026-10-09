# Regras de asset

Todo PNG do ambiente obedece ao mesmo pixel, à mesma luz (fonte alta, sombra fria), à paleta de `ArtBible/PALETTE.md` e ao PPU 16.

## Importação

- Filtro **Point**. Sem mipmap. Sem compressão que borre.
- O jogo monta vários sprites com `Sprite.Create` em runtime (`T1Scenery.LoadSprite`, visuais dos heróis). O `.meta` com PPU 28 é ignorado nesses caminhos. Asset novo documenta o PPU 16 mesmo assim.
- Pivot do fundo largo: base, centro horizontal, como as camadas atuais.

## Kit mínimo antes da fase longa

Arquitetura: 3 paredes, 3 colunas, 2 arcos, 2 portas, 2 pisos, 2 ruínas.  
Divindade: 1 principal, 1 quebrada, 1 cabeça, 1 braço, fragmentos.  
Vegetação: 3 musgos, 3 raízes, 3 plantas, 2 fungos.  
Objetos: 5 pedras, 3 cristais, 3 símbolos, 3 objetos de culto.  
Gameplay: chão, borda, degrau, variação quebrada e com musgo.

Fonte do desenho: Aseprite. O `build_t1.py` é protótipo de leitura, não a arte final do kit.

## Teste de aceite (seção 29 do documento mestre)

Não marcar a fase como pronta enquanto o herói competir com o fundo, o chão for uma linha só, ou a estátua distante não for reconhecível como corpo quebrado.
