# Integração Cinematic V3

Branch: art/cinematic-v3-far-city. Base: codex/feixe-infernal-monstros-f1, 8276e33.

Somente os PNGs vivos far.png (512×130) e city.png (640×176) foram substituídos pelos arquivos fornecidos, sem edição adicional. Importadores originais preservados: Sprite, Point, PPU 16, sem mipmaps e sem compressão. Backups exatos dos PNGs anteriores em Assets/Resources/T1_Hybrid/_backup_pre_cinematic_v3/; hashes em manifest_integracao.json.

Esculturas e Cidadela isolada arquivadas apenas nesta pasta de documentação, sem importação para o level. C#, cenas, paralaxe, câmera, colisores, HUD, Guerreiro/Feixe/inimigos, sky, clouds, ground e plataformas permanecem iguais à base.

Unity 6000.5.9f1: 46 verificações existentes passaram em Play Mode (Ground_T1 híbrido, Feixe, inimigos, cinco gaps, plataformas e portal). Resultados em playmode-results.txt. Não substitui aprovação artística humana. O Editor registrou ArgumentOutOfRangeException durante a inicialização, já observado na base; origem não identificada.

Riscos visuais aceitos no prompt da Direção: Cidadela pequena ainda pode precisar de retoque Aseprite; rim light algorítmico pode marcar folhas e lago; montanhas lavanda podem destoar do horizonte ciano. O changelog recebido é mantido como registro anterior à integração: seu título MOCKUP descreve a origem, não a validação executada nesta branch.

WebGL gerado com sucesso pelo WebGLBuilder.Build existente: 18.180.404 bytes, sem mudar o pipeline. Conferidos 256 arquivos de código, cenas, configuração e recursos preservados, idênticos à base; backups far/city também idênticos aos originais. Publicação no projeto pessoal aetherion-voidfall, domínio aetherion-voidfall-gules.vercel.app. A conta Hobby antiga não é destino desta publicação. Sem merge em main.

## Publicação e evidências

Produção READY: https://aetherion-voidfall-gules.vercel.app/ . Deployment dpl_8ckXwaeRafqXXeP7myuotWs8gLj4, projeto prj_BjRWPsfINvsxLR7ixioA1AVhrbWM, conta gabriellucassilva81-8983. Origem do build: 3779ca2; commit da arte: 06c50bb. O projeto pessoal foi conferido antes da publicação; a conta Hobby antiga não foi usada. O conector MCP não tinha acesso a esse escopo; foi utilizada a CLI com a autenticação pessoal separada já autorizada.

Build Vercel de 6 segundos, primeiro sem atribuir domínio; hashes dos quatro arquivos conferidos antes da promoção. Após promover o mesmo deployment: domínio HTTP 200, quatro hashes idênticos ao build local e cabeçalhos gzip/MIME corretos. Registro em production-hashes.json.

No navegador de produção foi executado Boot → menu → introdução → seleção do Guerreiro → Ground_T1. Cidadela menor visível no horizonte à direita; nova luz na city, terreno/plataformas e HUD preservados. Também houve movimento e salto pelo primeiro gap usando os controles normais, sem editar a câmera. Console do navegador sem erros, inclusive sem sprite missing. Duas capturas reais e console nesta pasta: cinematic-v3-producao.png, cinematic-v3-producao-percurso.png, cinematic-v3-producao-console.txt. As imagens não são mockups. A aprovação artística final continua humana; os riscos aceitos estão descritos acima e no changelog.

Nenhum merge em main foi realizado. A pasta original trabalho jogo sexta permanece com suas alterações locais anteriores; a implementação foi isolada no worktree Aetherion_Cinematic_V3.
