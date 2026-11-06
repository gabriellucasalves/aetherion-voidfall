# Polish Astra — apresentação, HUD e profundidade Atmos

Base: codex/atmos-v1, 65f7087. Branch: polish/astra-hud-res-cenario. Sem merge em main.

## Alterações

- PixelPresentation: RT 1536×864, Point, sem mipmaps/MSAA, OrthoSize 6.75, enquadramento 24×13.5 unidades. Fit inteiro; telas menores que o RT usam redução Point para não cortar o mundo.
- StageHud e BuildHud: painel topo-centro, retrato circular recortado do idle original, barras 380×40 e labels 20 px. VIDA, ESCUDO/CAMPO, ESPECIAL com PRONTO; Arqueiro sem segunda barra. Supremo continua indicado em texto para Mago/Arqueiro, sem mudar sua lógica. Rodapé legível e título da fase abaixo do painel.
- AtmosDepth + loader: dessaturação 0.65 e tint fria no far (~20% menos luminância); contraste médio 1.03. Materiais pertencem à instância do cenário, com textura explícita por plano e liberação ao sair da cena. Isso corrige o fundo preto observado durante recargas com materiais globais.
- P5 e todos os 24 arquivos do pack permanecem byte a byte iguais. Parallax, snap 1/32, sorting, PPU 32, 72 unidades e dois gaps mantidos. Fundo visual escuro sob os gaps, sem colisor novo.
- Combate, inimigos, Feixe e portal não alterados.

## Validação local

PolishValidation.Run: 42 checks PASS, captura real de Game View no Editor, Guerreiro/Mago/Arqueiro, RT e framing, barras opacas, retratos, especial pronto, Full HD/QHD, queda mortal, cenário renderizado após recarga. Screenshots em qa/. Nos testes locais os inimigos são parados e a barra é preenchida para verificar PRONTO; esses auxiliares são UNITY_EDITOR e não entram no WebGL. Módulos internos da Unity screencapture/imageconversion habilitados para capturas, sem serviço externo de arte.

## Limites físicos do pedido

Três barras de 40 px, espaçamento, cabeçalho e margens pedem painel de 198 px; 90–120 px não comportariam o layout pedido. O mundo aparece com 1536×864 pixels nos dois monitores por causa da escala inteira: margens (192,108) em Full HD e (512,288) em QHD. Em QHD fica menor que a antiga apresentação 768×432 ampliada 3×; manter aquele tamanho exigiria escala fracionária 1.5× do novo RT. Ortho/framing e proporções entre personagens/plataformas não mudaram. Dobrar o RT não cria detalhe nos PNGs: a densidade da arte fonte foi preservada conforme a restrição de não redesenhar.

Publicação e teste de navegador serão registrados após o build.
