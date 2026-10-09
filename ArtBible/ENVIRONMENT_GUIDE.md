# Guia de ambiente

## Planos

A câmera precisa separar, de trás para a frente:

céu → cidade distante → templos → estátuas → ruínas → plataforma → vegetação próxima.

| Plano | O que conta | Parallax no jogo hoje |
|--------|-------------|------------------------|
| Céu | Lua partida, planeta baixo, estrela rara | 0,95 (`T1Backdrop`) |
| Longe | Metrópole em silhueta, torres, estátua colossal quebrada | 0,9 |
| Templos | Cabeça enterrada, degraus, portão circular, cristal | 0,8 |
| Chão | Laje, rachadura, símbolo, raiz. Parallax 1 no documento = preso ao mundo | colisor e sprites de gameplay, fator 0 |
| Frente | Cipó e caco, pouco, sem cobrir o herói | −0,12 a 0,25 |

A fórmula real está em `AI/AGENT_PROTOCOL.md`. Não usar 0,02 para o céu neste código: aqui 0,02 quase trava a camada no mundo.

## Resolução

Framebuffer do jogo: **384×216**, filtro Point, escala inteira (`PixelPresentation`).  
PPU único: **16** (`PixelArt.Ppu`).

O documento mestre cita 320×180 ou 426×240 como composição de referência. Isso ainda não substitui o framebuffer. Mudança de resolução é tarefa separada, depois de medir o herói na tela.

## O que não fazer

- Um PNG gigante no lugar das camadas.
- Castelo gótico como linguagem dos prédios.
- Objeto quebrado espalhado sem o conjunto (pedestal, fragmento, símbolo, planta).
- Musgo contínuo em toda a borda da silhueta.
- Tilemap novo só porque o documento menciona a palavra. Hoje o chão é sprite gerado e colisor em código.
