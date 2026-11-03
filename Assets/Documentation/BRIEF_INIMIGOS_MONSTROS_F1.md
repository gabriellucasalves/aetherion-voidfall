# Inimigos monstros — Fase I

A direção dos três papéis está aprovada. As descrições abaixo são conceitos de referência, não aprovação de sprites finais. Os PNGs de integração são nativos, 16 PPU, quatro poses de 64×64, com pivô nos pés; não são imagens conceituais usadas diretamente como sprite.

## Esqueleto Alien

Crânio alongado, apêndices ósseos, costelas abertas e espinha Aether ciano. Sem machado ou espada do esqueleto antigo. Perfil esguio e leitura fria.

Ranged: avança fora de alcance, recua abaixo de 3,2 u e atira até 7 u se há linha de visão. Dispara osso Aether direcionado após 0,45 s, recupera por 1,2 s. Projétil respeita terreno, escudo e campo do Mago. Atravessa gaps/subidas quando precisa reposicionar.

## Bicho

Criatura arqueada de placas violetas, focinho projetado, espinhos e garras claras. Corpo baixo, agressivo e distinto do ranged. Sem bola ácida.

Melee: persegue a 3,8 u/s. Dentro de 2,4 u prepara 0,30 s e salta com garras; janela de ataque 0,45 s, um acerto por salto. Usa navegação para alcançar plataformas e atravessar gaps.

## Golem de Ardósia

Silhueta larga, camadas de pedra azul-cinza e fissuras Aether. Altura de desenho de aproximadamente 27 px, equivalente ao idle atual do Guerreiro; ganha massa na largura, não na altura. Sem associação visual ou funcional a kamikaze.

Tanque: 100 PV, recebe 65% do dano normal. Aproxima-se lentamente; prepara a investida parado por 0,70 s, com telegraph âmbar. Avança a 8 u/s por até 0,60 s, um hit por investida e recuperação de 1,1 s. Freia antes de uma borda sem apoio. Não explode nem se autodestrói.

## Chão, plataformas e spawn

EnemyTraversal consulta colisores físicos dos modos híbrido e clássico. Saltos procuram chegada com apoio para a largura inteira do inimigo, considerando alcance balístico e altura. As plataformas one-way continuam sendo atravessáveis por baixo. O spawn só consome uma vaga após localizar superfície e espaço livre para todo o corpo; não há mais sorteio de nascimento no ar.

Ondas: Esqueleto Alien → Bicho → Golem de Ardósia, sete de cada. As factories antigas são aliases de compatibilidade para os novos papéis. AcidBall/AxeProjectile e arte antiga permanecem no histórico/pastas legadas, sem chamadas na IA ou no spawner F1.

## Portal

Após as três ondas, um portal se abre no trecho final seguro, X=32,5. A fase só exibe sua conclusão quando o jogador o atravessa. O portal encerra a demo F1; não representa uma Fase II já implementada.

## Arte e fonte

Resources/Inimigos/{esqueleto_alien,bicho,golem_ardosia}.png. Fonte reproduzível: Assets/Art/Inimigos/build_monsters_f1.py. Conceitos e sprites de integração precisam de revisão artística humana. Não foi alterado nenhum pixel da sheet do Guerreiro, nem redesenhado o cenário V7.1 nesta entrega.
