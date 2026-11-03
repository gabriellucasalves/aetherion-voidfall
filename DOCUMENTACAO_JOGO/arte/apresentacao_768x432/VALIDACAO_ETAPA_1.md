# Etapa 1 — PR #15: apresentação 768×432

PR #15 (cursor/apresentacao-768x432-f5e6, 2d06d67) integrada somente na base art/cinematic-v3-far-city. Merge bc16dfbbfef5089390d0b4c123109ca7644cdf3d. Nenhum merge em main.

## Play Mode executado

Unity 6000.5.9f1. Validação direta do RenderTexture ativo confirmou 768×432, FilterMode.Point, mipmaps desligados, antiAliasing=1 (sem MSAA), PPU 16, câmera ortográfica 6,75 e aspect 16:9. A sonda temporária de QA foi removida antes do build; nenhum código de instrumentação foi incluído na entrega. Resultado em playmode-presentation.json.

Os 46 checks existentes de gameplay passaram: Feixe, inimigos, spawn, gaps, plataformas e portal. Não houve erro de compilação C#. Isso não substitui playtest humano de balanceamento. A exceção de inicialização do Editor já observada na base foi registrada novamente, sem atribuir sua origem ao projeto.

## Enquadramento e limite da Etapa 1

OrthoSize usa LegacyDesignHeight=216: 216/(16×2)=6,75. A câmera continua vendo 13,5 unidades de altura e 24 de largura. O Guerreiro mantém suas dimensões no mundo e sua proporção em relação às plataformas.

O Fit inteiro existente foi preservado. Em um canvas 1920×1080: scale=2, imagem de mundo 1536×864, margens de 192 px nas laterais e 108 px em cima/baixo. A imagem ocupa 80% da largura e altura anteriores, portanto o Guerreiro também fica 20% menor em pixels de tela nessa situação. Não afirmar mesmo tamanho absoluto na tela em Full HD. Em 3840×2160, scale=5 preenche a tela exatamente. Em viewports menores que 768×432, o clamp mínimo 1 pode recortar a imagem, comportamento do Fit preservado pela PR.

Cada texel dos assets de PPU 16 ocupa 2×2 pixels no RT novo. A Etapa 1 não adiciona detalhes aos sprites existentes. A densificação artística nativa pertence à Etapa 2 e continua necessária para aumentar detalhe sem apenas ampliar a grade atual; preservar medidas de mundo nesse trabalho futuro. Sem Bilinear e sem simplificação gráfica procedural como substituição de arte.

WebGLBuilder.Build do Feixe concluiu com sucesso: 18.133.607 bytes. Destino autorizado: projeto pessoal aetherion-voidfall, conta gabriellucassilva81-8983, https://aetherion-voidfall-gules.vercel.app . Estado e evidências da publicação serão registrados após a verificação.
