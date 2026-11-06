# Mapa de mundo V1 — integração e validação

Branch de trabalho: `codex/mapa-mundo-v1`, criada do HEAD `4e5b528` de `polish/astra-hud-res-cenario`. Nenhum merge em `main`.

## Implementado

- Cena `WorldMap` e placa `map_island_bg_clean.png` do pacote, importada como Sprite Point, sem mipmaps e sem compressão. Os três indicadores transparentes foram importados do pack. O JPG conceito ficou somente em documentação.
- Avatar separado da placa, com idle e caminhada dos sheets atuais de Guerreiro, Mago ou Arqueiro, conforme o herói salvo. A rota percorre pontos ajustados visualmente sobre a trilha de pedra, em ambas as direções.
- `A/D` ou setas caminham; Enter/Espaço confirmam no nó; Escape retorna ao menu. Nós concluídos podem ser revisitados. Fases bloqueadas não carregam. Fases sem cena mostram “Em desenvolvimento”.
- Fluxo real existente: `SimpleEnemySpawner` exige três ondas; `F1ExitPortal` só aparece após elas; atravessar o portal chama `GroundT1Controller.OnDemoComplete`; uma única transição marca a Fase 1 concluída, libera a Fase 2 e abre o mapa no nó 1.
- Save versionado em `PlayerPrefs` (`aetherion.worldmap.v1.*`): maior fase concluída, nó de entrada, herói e reserva de estado final. As chaves antigas não são limpas. Seleção de herói persiste após reiniciar. O menu mostra MAPA depois da primeira conclusão.
- Replay da Fase 1 permite voltar ao mapa pela pausa ou derrota. A morte não altera progresso. A lógica de combate, inimigos, cenário, PPU global, câmera e resolução não foi alterada.

## Evidências

- `WorldMapValidation.Run` em Play Mode: 15 verificações PASS em `qa/results.txt`. A automação destruiu os inimigos de cada onda para acelerar o teste, esperou o spawner abrir o portal e atravessou o trigger físico. Verificou mapa, status, fase inexistente, recarga, bloqueio, replay, pausa, derrota, retorno, save de Mago e sheet correspondente.
- Capturas reais da Game View: `qa/01-fase1-antes.png` a `qa/10-mago-salvo.png`.
- WebGL final: `WebGLBuilder.Build` concluiu sem erros C#; build de 23.184.543 bytes. No navegador local, `A/D` moveu o avatar pelo caminho, Enter mostrou “Fase 2 · Em desenvolvimento”, e um reload mostrou o botão MAPA e restaurou o herói no nó 2. Capturas `qa/browser-*.png`; console sem erros. Para chegar ao mapa rapidamente no navegador, um teste local expôs a instância Unity apenas na resposta HTML do browser e chamou `OnDemoComplete` diretamente; a prova do gate ondas→portal é do Play Mode nativo.
- O ZIP original permanece fora do commit, no diretório do projeto. A referência de arte, guia e comparativo foram arquivados aqui.

## Pendente / limites

- Fases 2 e 3 e o boss não possuem cenas no projeto. Os biomas do JPG são conceitos. Quando as fases forem criadas, seus fluxos de vitória deverão chamar `WorldMapProgress.Complete(2)` e, após vitória real do boss, `Complete(3)` + `ConfirmFinalBossDefeated()`.
- O patch de grama onde o cavaleiro foi removido ainda tem pequena repetição de textura visível junto ao nó 1 (`clean_area_zoom.png`). Uma tentativa de edição automática alterou a composição inteira e foi descartada; a placa aprovada foi mantida. Retoque manual em arte nativa continua recomendado.
- O combate integral das 21 eliminações por um jogador humano e o uso em dispositivos móveis não foram testados nesta rodada. A automação validou o gate de ondas e a física do portal; o navegador validou caminhada, confirmação e persistência.

## Como conferir no Editor

Abrir `Assets/Scenes/Boot.unity`, entrar em `Ground_T1`, vencer as três ondas e atravessar o portal. No mapa, caminhar com `A/D` ou setas e confirmar com Enter/Espaço. Após a conclusão, o menu oferece acesso direto ao mapa. Para repetir o teste automatizado, executar `WorldMapValidation.Run` no Editor.
