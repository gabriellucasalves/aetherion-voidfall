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

Regressão AtmosV1Validation.Run: 46 checks PASS, incluindo ambos os gaps, saltos A/B/C, colisores, navegação dos três inimigos, parallax/snap e importação. Build WebGL concluído: 21.575.830 bytes, sem erros C# ou de shader. Pipeline existente, stripEngineCode=false conservado. Publicação verificada abaixo.

## Publicação verificada — 06/11/2026

URL: https://aetherion-voidfall-gules.vercel.app/ . Conta pessoal gabriellucassilva81-8983, projeto aetherion-voidfall (prj_BjRWPsfINvsxLR7ixioA1AVhrbWM). Build do commit 8730814; deployment dpl_EugzJQbwiHsconsUc2D3GSCjRhr2 Ready, promovido após validação do staging. URL imutável: https://aetherion-voidfall-9bpmgy9ez-gabriellucassilva81-8983.vercel.app/ .

SHA256 dos quatro arquivos WebGL idêntico ao local, com HTTP 200 no staging e na produção. Menu → introdução → seleção → Ground_T1 → salto para A usando controles normais no navegador. Testado retorno ao menu e nova seleção do Mago na mesma sessão: cenário e retrato corretos, label CAMPO. Console: zero erros, um aviso da Unity sobre sincronização futura de persistentDataPath. Logs Vercel de erro nos últimos cinco minutos: zero registros.

Print público em qa/browser-live.png; Play Mode em qa/guerreiro-1920.png, guerreiro-2560.png, mago-1920.png, arqueiro-1920.png, inicio-sem-hud.png, gap.png e voce-caiu.png. Diff de arquivos em diff-arquivos.txt. Nenhum merge em main.
