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

A PR #14 disparou automaticamente um preview pela integração GitHub/Vercel no projeto smashhhh/aetherion-voidfall, com status Ready. O endereço de preview exige login Vercel (302 para SSO): https://aetherion-voidfall-git-codex-feixe-infernal-mon-41df03-smashhhh.vercel.app . Isso não confirma publicação no domínio pessoal solicitado.

Produção concluída em 2026-11-03: https://aetherion-voidfall-gules.vercel.app . Conta verificada gabriellucassilva81-8983; projeto aetherion-voidfall (prj_BjRWPsfINvsxLR7ixioA1AVhrbWM). Deployment dpl_J9nrN68CVxPU9RnXTJYWkTsCyv4m, origem local 23effa4, alvo production, build Vercel de 6 s. Publicação inicialmente sem atribuir domínio, verificação dos quatro SHA-256, depois promoção do mesmo deployment. Não houve merge da PR.

Verificação após promoção: domínio HTTP 200; quatro arquivos do build idênticos ao WebGL local, gzip e MIME de WASM corretos. Navegador percorreu Boot → menu → introdução → seleção do Guerreiro → Ground_T1 no domínio de produção, com zero erros de console. Evidências: docs/qa/feixe-f1/production-hashes.json, webgl-f1-producao.png e webgl-f1-producao-console.txt. A autenticação da conta pessoal foi feita separadamente, sem substituir a sessão antiga.

O destino autorizado é a conta pessoal gabriellucassilva81 / aetherion-voidfall-gules. Nunca publicar na equipe Hobby antiga nem mudar a conta/projeto silenciosamente.
