# Integração Cinematic V3

Branch: art/cinematic-v3-far-city. Base: codex/feixe-infernal-monstros-f1, 8276e33.

Somente os PNGs vivos far.png (512×130) e city.png (640×176) foram substituídos pelos arquivos fornecidos, sem edição adicional. Importadores originais preservados: Sprite, Point, PPU 16, sem mipmaps e sem compressão. Backups exatos dos PNGs anteriores em Assets/Resources/T1_Hybrid/_backup_pre_cinematic_v3/; hashes em manifest_integracao.json.

Esculturas e Cidadela isolada arquivadas apenas nesta pasta de documentação, sem importação para o level. C#, cenas, paralaxe, câmera, colisores, HUD, Guerreiro/Feixe/inimigos, sky, clouds, ground e plataformas permanecem iguais à base.

Unity 6000.5.9f1: 46 verificações existentes passaram em Play Mode (Ground_T1 híbrido, Feixe, inimigos, cinco gaps, plataformas e portal). Resultados em playmode-results.txt. Não substitui aprovação artística humana. O Editor registrou ArgumentOutOfRangeException durante a inicialização, já observado na base; origem não identificada.

Riscos visuais aceitos no prompt da Direção: Cidadela pequena ainda pode precisar de retoque Aseprite; rim light algorítmico pode marcar folhas e lago; montanhas lavanda podem destoar do horizonte ciano. O changelog recebido é mantido como registro anterior à integração: seu título MOCKUP descreve a origem, não a validação executada nesta branch.

WebGL será gerado pelo WebGLBuilder.Build existente, sem mudar o pipeline. Publicação no projeto pessoal aetherion-voidfall, domínio aetherion-voidfall-gules.vercel.app. A conta Hobby antiga não é destino desta publicação. Sem merge em main.
