# ART REVIEW

## REVIEW-001

Problema: Os protótipos de cenário gerados por script ainda leem como silhueta e bloco, não como cidade descoberta.

Impacto: Falta escala monumental e grupo arquitetônico (templo com escada, altar, estátua, ruína e vegetação no mesmo conjunto).

Correção sugerida: Parar de acrescentar PNG procedural. O próximo passo é o kit da ENV-002, desenhado em pixel, e só então montar o trecho ENV-003.

Tarefa relacionada: ENV-002

## REVIEW-002

Problema: O herói ainda é chibi (~27 px de silhueta) dentro da célula 64. O contrato novo pede 44–52 px.

Impacto: O personagem não fica pequeno diante de uma estátua colossal — os dois estão na mesma escala de ícone.

Correção sugerida: Não redesenhar o herói nesta etapa. A ENV-003 precisa de um herói legível; a troca de sheet é tarefa própria, depois do kit de cenário.

Tarefa relacionada: ENV-003

## REVIEW-003

Problema: Não existe Tilemap. Chão, plataforma e fundo nascem em código (`T1Scenery`, `build_t1.py`).

Impacto: Variação de piso (musgo, raiz, laje quebrada) não é um tile que se repete com autotile. É um PNG único.

Correção sugerida: O kit de chão da ENV-002 pode continuar em sprites soltos até existir tarefa para Tilemap. Não adicionar o pacote 2D Tilemap sem necessidade.

Tarefa relacionada: ENV-002
