# TASKS

## ENV-000
### Auditoria do projeto e fonte da verdade

Responsável: CURSOR UNITY ENGINEER

Status: DONE

Descrição: Mapear Unity, câmera, PPU, parallax e arte existente. Criar `AI/` e `ArtBible/`. Não desenhar cenário novo nesta tarefa.

Dependências: nenhuma.

## ENV-001
### Linguagem visual das divindades

Responsável: GROK ART DIRECTOR

Status: DONE

Descrição: Anatomia, símbolo do anel e peças reutilizáveis fechados em `ArtBible/DEITY_LANGUAGE.md`. Folha de referência: `ArtBible/divindade-pecas.png`.

Dependências: ENV-000.

## ENV-002
### Kit de identidade visual (não a fase inteira)

Responsável: PIXEL / ASEPRITE, com revisão do diretor

Status: DONE

Descrição: Kit em pixel nativo, PPU 16, em `Assets/Art/Kit/png/` (52 peças). Folha: `Assets/Art/Kit/folha.png`. Ainda não montado na fase.

Dependências: ENV-001.

## ENV-003
### Trecho de teste de 30–60 segundos

Responsável: CURSOR UNITY ENGINEER + PIXEL

Status: DONE

Descrição: Sequência montada em T1, do spawn para a direita: ruína, fragmentos, cabeça no pedestal, coluna e parede com símbolo, arco, divindade quebrada com o braço ao lado, portal com cristal. Peças em `Resources/Kit`, colocadas por `T1Scenery.BuildKitWalk`.

Dependências: ENV-002.

## ENV-004
### Alinhar parallax do documento com o do código

Responsável: CURSOR UNITY ENGINEER

Status: BLOCKED

Descrição: O documento mestre pede fatores 0,02–1,15. O jogo usa a escala invertida descrita em `AI/AGENT_PROTOCOL.md`. Só mudar se o diretor pedir a troca de convenção.

Dependências: decisão explícita na revisão de arte.
