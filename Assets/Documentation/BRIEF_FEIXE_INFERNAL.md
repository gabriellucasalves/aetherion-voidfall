# Feixe Infernal — Guerreiro F1

Direção aprovada: poder ativo em gameplay, sem cutscene MvC, sem pausa global, sem redesenhar a sheet 16-bit do Guerreiro.

## Contrato implementado

- L / Q / E ou botão mobile de especial: mesmos disparo, recurso e cooldown.
- Exige barra ESPECIAL cheia (100/100 de Ressonância); consome a barra inteira. Não consome ESCUDO.
- Preparação: 0,15 s. Ativo: 0,40 s. Cooldown: 6 s.
- Caixa horizontal de 7,0 × 0,45 unidades, no centro do colisor do Guerreiro + 0,10 u vertical.
- Bocal 0,35 u à frente; direção capturada no início. A origem acompanha o corpo durante o disparo.
- Um hit por EnemyController por disparo, inclusive com trigger e sólido sobrepostos.
- Dano inicial: Strength × 1,2, submetido à resistência de cada inimigo.
- Sem rastreamento de alvo; inimigos acima/atrás/fora da caixa não recebem dano.
- Sem corte cinematográfico, time stop, invulnerabilidade adicional ou uso de WarriorBarrage.
- VFX de fogo nativo 112×8 por frame; caixa independente do brilho. Animação usa os frames existentes do Guerreiro.

## Conceito de referência — não é sprite final aprovado

Núcleo creme quente, corpo âmbar/laranja e línguas de fogo vermelho, leitura horizontal estreita. Não transformar o feixe numa explosão circular nem numa faixa de tela inteira. A textura entregue é um sprite de integração nativo; a aprovação final de arte continua humana.

Código: InfernalBeam, PlayerCombat, SpecialController. Fonte do VFX: Assets/Art/Inimigos/build_monsters_f1.py. A sheet do Guerreiro permanece byte a byte igual à base.
