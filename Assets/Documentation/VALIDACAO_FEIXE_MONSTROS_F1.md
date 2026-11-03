# Validação — Feixe Infernal e monstros F1

## Base e preservação

Branch codex/feixe-infernal-monstros-f1 a partir de art/fundo-v4-checklist-aceite (0334db5). O V7.1 local foi copiado com conferência SHA-256 e registrado em commit artístico separado por autorização expressa do usuário. Os worktrees originais permanecem intactos. A PR contra main inclui os commits anteriores de cenário ainda não mergeados.

## Editor: executado

Unity 6000.5.9f1, Play Mode em Ground_T1 com física 2D real. Entrada pelos controladores durante QA automatizado; não é playtest humano nem percurso da Boot.

Executar com a Unity fechada neste checkout:

    Unity -batchmode -projectPath <checkout> -executeMethod F1Validation.Run -logFile <log>

46 verificações passaram: consumo da barra e rejeição vazia, startup, dano frontal, exclusão de alvo atrás/acima/distante, deduplicação por inimigo, facing esquerdo, término da janela, ausência de cinematic/pausa/custo de escudo, três recursos visuais, spawn apoiado/rejeição de gap, travessia de cada um dos cinco gaps pelas três larguras, subida 0→2→3,75, projétil e recuo ranged, salto melee, telegraph/investida sem explosão e portal.

Os testes de percurso exercitam EnemyTraversal diretamente com o controlador de ataque desativado para separar navegação de combate; os testes dos papéis executam a IA completa. Resultados: docs/qa/feixe-f1/editor-playmode-results.txt. Código de QA excluído de players por UNITY_EDITOR.

Na primeira execução, o teste ranged nasceu dentro de um gap e falhou por obstrução do terreno; corrigido o arranjo para o trecho contínuo, sem contornar a colisão do projétil. A execução final passou. Houve ArgumentOutOfRangeException no bootstrap/indexação do Editor, sem stack de script do projeto; registrar separadamente dos resultados dos testes.

## Playtest humano ainda necessário

Boot → seleção → Ground_T1; Guerreiro em ambos os facings e no ar; mobile; luta com ondas completas; legibilidade da arte em movimento; balanceamento de carga/dano; câmera e mistura de personagens em plataformas. Não há homologação artística.

## WebGL e publicação

Build pelo WebGLBuilder.Build do projeto, com cenas habilitadas e compressão gzip/fallback existentes. Não é necessário merge para gerar e publicar o estado local desta branch. Estado efetivo do build e do deploy será registrado na PR após a execução; não assumir sucesso por este documento.

O destino autorizado é a conta pessoal gabriellucassilva81 / aetherion-voidfall-gules. Nunca publicar na equipe Hobby antiga nem mudar a conta/projeto silenciosamente.
