# Protocolo dos agentes

Documento mestre: `AI/AI_COMMAND_CENTER_PIXEL_ART.md`.  
Fonte da verdade do dia a dia: esta pasta e `ArtBible/`.

## Quem faz o quê

| Agente | Muda | Não muda |
|--------|------|----------|
| Diretor de arte | `ArtBible/`, `AI/ART_REVIEW.md`, prioridade em `AI/TASKS.md` | C# de gameplay, combate, kits |
| Engenharia (Cursor) | Unity, parallax, import, integração de PNG já aprovado | Arte definitiva “inventada” no lugar de um asset pedido |
| Pixel / Aseprite | Sheets, tiles, limpeza, export Point | Números de combate, câmera de jogo |

## Antes de editar

1. Ler o documento mestre.
2. Ler `AI/TASKS.md`, `AI/ART_REVIEW.md` e `ArtBible/VISUAL_STYLE.md`.
3. Não trocar sistema que a tarefa não cita.

## Depois de editar

1. Atualizar o status da tarefa.
2. Registrar em `AI/CHANGELOG.md`.
3. Se a arte falhou um critério da seção 29 do documento mestre, abrir item em `AI/ART_REVIEW.md`.

## Parallax deste projeto

O fator em `ParallaxLayer` **não** usa a escala 0,02–1,15 do documento mestre.

```text
posição = origem + câmera * fator
fator 1    cola na câmera (céu)
fator 0    preso ao mundo (chão jogável)
fator < 0  passa na frente, sentido oposto
```

Valores atuais em `T1Scenery`: céu 0,95 · longe 0,9 · templos 0,8 · véu 0,25 · props 0,4 · primeiro plano −0,12.

Não “corrigir” esses números para os do documento sem uma tarefa própria. Os dois sistemas são escalas invertidas.

## Regra de ouro

Se o objeto só existe para preencher espaço, não entra.
