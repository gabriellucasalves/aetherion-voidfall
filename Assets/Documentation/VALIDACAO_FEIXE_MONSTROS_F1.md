# Validação — Feixe Infernal e monstros F1

## Base e preservação

Branch codex/feixe-infernal-monstros-f1 a partir de art/fundo-v4-checklist-aceite (0334db5). O V7.1 local foi copiado com conferência SHA-256 e registrado em commit artístico separado por autorização expressa do usuário. Os worktrees originais permanecem intactos. A PR contra main inclui os commits anteriores de cenário ainda não mergeados.

## Editor: executado

Unity 6000.5.9f1, Play Mode em Ground_T1 com física 2D real. Entrada pelos controladores durante QA automatizado; não é playtest humano nem percurso da Boot.

Executar com a Unity fechada neste checkout:

    Unity -batchmode -projectPath <checkout> -executeMethod F1Validation.Run -logFile <log>

46 verificações passaram: consumo da barra e rejeição vazia, startup, dano frontal, exclusão de alvo atrás/acima/distante, deduplicação por inimigo, facing esquerdo, término da janela, ausência de cinematic/pausa/custo de escudo, três recursos visuais, spawn apoiado/rejeição de gap, travessia de cada um dos cinco gaps pelas três larguras, subida 0→2→3,75, projétil e recuo ranged, salto melee, telegraph/investida sem explosão e portal.

Os testes de percurso exercitam EnemyTraversal diretamente com o controlador de ataque desativado para separar navegação de combate; os testes dos papéis executam a IA completa. Resultados: docs/qa/feixe-f1/editor-playmode-results.txt. Código de QA excluído de players por UNITY_EDITOR.

Na primeira execução, o teste ranged nasceu dentro de um gap e falhou por obstrução do terreno; corrigido o arranjo para o trecho contínuo, sem contornar a colisão do projétil. A execução final passou. Houve ArgumentOutOfRangeException durante a inicialização do Editor, próxima ao início da indexação e sem stack de script do projeto. A origem não foi identificada; registrar separadamente dos resultados dos testes.

## Playtest humano ainda necessário

Boot → seleção → Ground_T1; Guerreiro em ambos os facings e no ar; mobile; luta com ondas completas; legibilidade da arte em movimento; balanceamento de carga/dano; câmera e mistura de personagens em plataformas. Não há homologação artística.

## WebGL e publicação

Build pelo WebGLBuilder.Build do projeto, com cenas habilitadas e compressão gzip/fallback existentes. Não é necessário merge para gerar e publicar o estado local desta branch. Build Unity WebGL concluído com sucesso (18.079.924 bytes na compilação final). O navegador carregou o build local e percorreu Boot → menu → introdução → seleção do Guerreiro → Ground_T1, sem mensagens de erro no console. Captura e console em docs/qa/feixe-f1. O build final incorpora também a dica de controles Feixe Infernal em PT-BR.

Deploy ainda não realizado: CLI 65.9.0 autenticada como gabriellucasalves; gabriellucassilva81 retorna "The specified scope does not exist". A única equipe listada é a Hobby antiga e o projeto acessível tem domínio aetherion-voidfall.vercel.app. Foi solicitada conexão à conta correta, sem alterar o destino autorizado.

O destino autorizado é a conta pessoal gabriellucassilva81 / aetherion-voidfall-gules. Nunca publicar na equipe Hobby antiga nem mudar a conta/projeto silenciosamente.
