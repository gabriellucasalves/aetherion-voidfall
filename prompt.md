# AETHERION: VOIDFALL

## Documento de conceito e instruções para desenvolvimento do protótipo

Você está trabalhando em um projeto acadêmico de Desenvolvimento de Games utilizando **Unity**.

O objetivo é desenvolver um jogo 2D em **pixel art**, inicialmente para execução em navegador através de uma build **WebGL**.

Este documento representa o conceito inicial do jogo e deve ser utilizado como referência durante toda a implementação.

O projeto deve possuir uma arquitetura organizada, modular e simples de expandir.

Não implemente todos os sistemas de uma única vez.

Sempre priorize primeiro um protótipo funcional e depois adicione novos recursos incrementalmente.

---

# 1. CONCEITO DO JOGO

Nome provisório:

**Aetherion: Voidfall**

Gênero:

* Action Roguelite
* Survival
* Shoot'em Up
* Pixel Art
* Fantasia
* Ficção científica

Referências conceituais:

* Seraph's Last Stand
* Space Invaders
* jogos roguelite baseados em builds e upgrades

IMPORTANTE:

As referências devem servir apenas para compreender conceitos de gameplay.

O projeto deve possuir:

* personagens próprios;
* história própria;
* sprites próprios ou temporários;
* habilidades próprias;
* interface própria;
* mapas próprios;
* inimigos próprios.

---

# 2. HISTÓRIA

Durante milhares de anos, o universo de **Aetherion** permaneceu protegido por uma energia conhecida como **Véu Celestial**.

Essa barreira separava Aetherion de uma dimensão desconhecida chamada **O Vazio**.

Entretanto, algo destruiu o Véu.

A corrupção começou lentamente.

Primeiro surgiram criaturas em regiões isoladas.

Depois cidades inteiras começaram a desaparecer.

Por fim, a corrupção alcançou não apenas o planeta principal de Aetherion, mas também luas, estações espaciais e outros mundos.

A invasão acontece agora em duas frentes.

Na superfície, criaturas do Vazio avançam contra os últimos reinos.

No espaço, frotas corrompidas bloqueiam qualquer tentativa de fuga ou resistência.

Restam apenas alguns guerreiros capazes de enfrentar a invasão.

O jogador deverá escolher um deles.

Cada herói possui:

* características próprias;
* atributos diferentes;
* habilidades diferentes;
* estilo diferente de combate;
* uma nave própria.

O objetivo inicial é impedir o avanço do Vazio na superfície.

Depois disso, o herói deverá entrar em sua nave e continuar a batalha fora do planeta.

A guerra de Aetherion acontece tanto **na terra quanto no espaço**.

---

# 3. ESTRUTURA DA PRIMEIRA VERSÃO

O protótipo deverá inicialmente possuir:

## MENU

Tela inicial.

↓

## INTRODUÇÃO

Breve apresentação da história.

↓

## ESCOLHA DE PERSONAGEM

Escolher entre:

* Guerreiro
* Mago
* Anjo

↓

## FASE 1

Combate terrestre.

↓

## CHEFE / FINAL DA FASE

↓

## TRANSIÇÃO

Personagem entra em sua nave.

↓

## FASE 2

Combate espacial.

↓

## CHEFE ESPACIAL

↓

## TELA DE VITÓRIA

Essa estrutura representa o primeiro MVP do projeto.

---

# 4. TELA INICIAL

Ao iniciar o jogo apresentar uma tela em pixel art.

Elementos:

**AETHERION: VOIDFALL**

Botões:

* JOGAR
* CONFIGURAÇÕES
* CRÉDITOS

Na primeira versão, Configurações e Créditos podem ser simples.

No fundo pode existir uma animação pixel art mostrando:

* planeta Aetherion;
* estrelas;
* partículas;
* pequenas naves;
* uma fissura ou energia representando O Vazio.

O botão JOGAR deve iniciar a apresentação da história.

---

# 5. INTRODUÇÃO

Apresentar uma pequena sequência narrativa antes da seleção do personagem.

Não utilizar textos muito grandes.

Utilizar pequenas telas.

Exemplo:

### Tela 1

> Por milhares de anos, o Véu protegegeu Aetherion.

### Tela 2

> Até que algo despertou além das estrelas.

### Tela 3

> O Véu foi destruído.

### Tela 4

> E o Vazio atravessou.

### Tela 5

Mostrar criaturas atacando o planeta.

Texto:

> Aetherion está caindo.

### Tela 6

Mostrar os três heróis.

Texto:

> Mas ainda existem aqueles dispostos a lutar.

Em seguida:

**ESCOLHA SEU HERÓI**

---

# 6. PERSONAGENS

Existirão inicialmente três personagens.

---

## GUERREIRO

Função:

**Tank / combate físico**

Características:

* muita vida;
* alta defesa;
* ataques fortes;
* menor velocidade;
* menor alcance.

Atributos iniciais sugeridos:

Vida: 150

Força: 90

Poder: 20

Defesa: 80

Agilidade: 30

Velocidade de ataque: 40

Habilidade inicial:

**Corte de Energia**

Realiza um ataque com espada que lança uma pequena onda de energia.

Características visuais:

* armadura;
* espada;
* aparência pesada;
* cores relacionadas a metal e energia.

---

# 7. MAGO

Função:

**Dano mágico / ataques em área**

Características:

* dano elevado;
* ataques à distância;
* pouca resistência;
* habilidades elementais.

Atributos iniciais sugeridos:

Vida: 80

Força: 20

Poder: 100

Defesa: 20

Agilidade: 50

Velocidade de ataque: 60

Habilidade inicial:

**Orbe Arcano**

Dispara automaticamente energia contra inimigos próximos.

Características visuais:

* manto;
* cajado;
* energia mágica;
* partículas.

---

# 8. ANJO

Função:

**Mobilidade / equilíbrio**

Características:

* extremamente móvel;
* boa velocidade;
* capacidade de esquiva;
* ataques de energia;
* estatísticas equilibradas.

Atributos iniciais sugeridos:

Vida: 100

Força: 50

Poder: 65

Defesa: 45

Agilidade: 100

Velocidade de ataque: 80

Habilidade inicial:

**Pena Celestial**

Dispara penas de energia contra inimigos.

Características visuais:

* asas;
* armadura leve;
* energia celestial;
* alta mobilidade.

---

# 9. TELA DE SELEÇÃO

Mostrar os três personagens simultaneamente.

Estrutura aproximada:

[Guerreiro]

[Mago]

[Anjo]

Quando o mouse estiver sobre um personagem, mostrar:

* nome;
* pequena descrição;
* Vida;
* Força;
* Poder;
* Defesa;
* Agilidade;
* habilidade inicial.

Utilizar barras visuais para facilitar a comparação.

Exemplo:

VIDA
█████████░

PODER
████░░░░░░

DEFESA
████████░░

Deve existir um botão:

**ESCOLHER**

Após selecionar:

**Tem certeza que deseja escolher o Mago?**

CONFIRMAR

VOLTAR

---

# 10. GAMEPLAY — FASE 1

A primeira fase acontece na superfície de Aetherion.

O cenário deverá transmitir que aquele território está sendo atacado.

Elementos visuais:

* ruínas;
* estruturas destruídas;
* céu corrompido;
* partículas;
* criaturas surgindo;
* elementos de fantasia.

O jogador poderá movimentar o personagem.

Controles sugeridos:

WASD — movimentação

ESC — pausa

As habilidades principais devem inicialmente possuir **ataque automático**.

O personagem identifica inimigos dentro de determinado alcance e realiza ataques automaticamente.

Isso permite que o foco do jogador seja:

* movimentação;
* posicionamento;
* esquiva;
* desenvolvimento da build.

---

# 11. INIMIGOS TERRESTRES

Criar inicialmente poucos tipos de inimigos.

## Criatura básica

Anda diretamente em direção ao jogador.

Pouca vida.

Pouco dano.

---

## Criatura veloz

Pouca vida.

Alta velocidade.

---

## Criatura resistente

Movimento lento.

Muita vida.

---

## Criatura voadora

Move-se pelo cenário ignorando determinados obstáculos.

---

## Inimigo de longo alcance

Mantém distância do jogador.

Realiza ataques à distância.

---

# 12. SISTEMA DE ONDAS

Os inimigos devem aparecer progressivamente.

Exemplo:

Onda 1

Criaturas básicas.

Onda 2

Maior quantidade.

Onda 3

Básicas + velozes.

Onda 4

Adicionar criaturas voadoras.

Onda 5

Mini-chefe.

Depois continuar aumentando a combinação de inimigos.

O sistema deve permitir facilmente configurar:

* quantidade;
* tipo;
* vida;
* velocidade;
* intervalo;
* dificuldade.

Evitar programar cada onda diretamente no código.

Utilizar estruturas configuráveis, preferencialmente ScriptableObjects.

---

# 13. EXPERIÊNCIA

Cada inimigo derrotado fornece experiência.

Criar uma barra de XP na interface.

Exemplo:

LV. 3

████████████░░░

Ao completar a barra:

**LEVEL UP**

O jogo deve pausar temporariamente.

Mostrar três cartas aleatórias.

O jogador escolhe uma.

Depois o jogo continua.

---

# 14. SISTEMA DE CARTAS

Inicialmente criar cartas genéricas.

Exemplo:

## Força Superior

+15% dano.

---

## Essência Vital

+20 vida máxima.

---

## Passos do Vento

+10% velocidade.

---

## Ataque Rápido

+12% velocidade de ataque.

---

## Armadura

+15 defesa.

Posteriormente poderão existir cartas exclusivas por classe.

---

# 15. RARIDADE

Preparar o sistema para possuir raridades.

Tipos:

COMUM

RARO

ÉPICO

LENDÁRIO

A raridade poderá modificar:

* bônus;
* aparência da carta;
* probabilidade de aparecer.

Para o primeiro protótipo, todas podem inicialmente ser comuns.

A arquitetura, entretanto, deve permitir implementar raridades posteriormente.

---

# 16. EVOLUÇÃO DE HABILIDADES

Uma habilidade poderá possuir níveis.

Exemplo:

ORBE ARCANO I

↓

ORBE ARCANO II

↓

ORBE ARCANO III

↓

ORBE ARCANO IV

Cada nível pode aumentar:

* dano;
* velocidade;
* quantidade de projéteis;
* área;
* efeitos.

Posteriormente poderá existir uma evolução final.

Exemplo:

ORBE ARCANO IV

↓

**SUPERNOVA ARCANA**

---

# 17. FINAL DA FASE 1

Depois de sobreviver determinado tempo ou número de ondas, aparecerá um chefe.

Depois de derrotá-lo:

* inimigos param de nascer;
* música diminui;
* portal/base/nave aparece.

Mostrar mensagem:

**A superfície está temporariamente segura.**

Depois:

**Mas o Vazio já alcançou as estrelas.**

Mostrar o personagem caminhando até sua nave.

Realizar transição para a Fase 2.

---

# 18. NAVES

Cada personagem possui uma nave diferente.

Isso deve refletir seu estilo.

---

## NAVE DO GUERREIRO

Características:

* maior resistência;
* mais escudo;
* tiros mais pesados;
* menor velocidade.

---

## NAVE DO MAGO

Características:

* alto dano;
* ataques de energia;
* menor resistência.

Visualmente poderá parecer parcialmente tecnológica e parcialmente mágica.

---

## NAVE DO ANJO

Características:

* alta velocidade;
* disparos rápidos;
* maior capacidade de esquiva.

Pode possuir aparência semelhante a asas.

---

# 19. FASE 2 — COMBATE ESPACIAL

Na segunda fase, alterar completamente o cenário.

Agora estamos no espaço próximo a Aetherion.

O jogador controla a nave.

Gameplay inspirado em shoot'em ups clássicos.

A câmera permanece 2D.

O jogador poderá mover a nave utilizando:

WASD.

Inicialmente os disparos poderão ser automáticos.

Inimigos poderão surgir:

* de cima;
* das laterais;
* em formações;
* em grupos.

O fundo deverá possuir movimento contínuo para transmitir sensação de viagem.

---

# 20. INIMIGOS ESPACIAIS

Criar inicialmente:

### Drone corrompido

Nave simples.

---

### Caça do Vazio

Nave rápida.

---

### Nave pesada

Muita resistência.

---

### Inimigo atirador

Mantém distância.

---

### Formação inimiga

Grupo de pequenas naves organizadas.

---

# 21. PROGRESSÃO ENTRE FASES

IMPORTANTE:

As melhorias adquiridas durante a Fase 1 devem continuar na Fase 2.

Porém alguns atributos podem ser convertidos.

Exemplo:

Força do Guerreiro

→ aumenta dano da nave.

Defesa

→ aumenta escudo.

Agilidade

→ aumenta velocidade.

Poder

→ aumenta habilidade especial.

Isso cria uma ligação entre as duas formas de gameplay.

---

# 22. INTERFACE DURANTE A PARTIDA

Mostrar:

Vida

Nível

XP

Tempo ou onda

Habilidade

Opcionalmente:

Quantidade de inimigos derrotados.

Exemplo:

❤️ 124 / 150

LV 4

XP ███████░░░

ONDA 7

☠ 126

A interface deve ser simples e legível.

---

# 23. DIREÇÃO ARTÍSTICA

O jogo deve utilizar estética:

**2D Pixel Art Fantasy + Sci-Fi**

Resolução visual inspirada em jogos retrô modernos.

Não utilizar elementos realistas.

Priorizar:

* pixel art;
* animações curtas;
* partículas;
* iluminação;
* efeitos de impacto.

Misturar:

fantasia medieval

*

magia

*

tecnologia espacial.

Aetherion deve parecer um universo onde tecnologia e magia coexistem.

---

# 24. ARQUITETURA UNITY

Organizar o projeto aproximadamente desta forma:

Assets/

Art/

Animations/

Audio/

Materials/

Prefabs/

Scenes/

Scripts/

ScriptableObjects/

UI/

Dentro de Scripts:

Core/

Player/

Enemies/

Combat/

Abilities/

Upgrades/

Waves/

UI/

Ships/

Managers/

---

# 25. CENAS

Criar inicialmente cenas separadas:

Boot

MainMenu

Intro

CharacterSelection

GroundLevel

SpaceLevel

Victory

Não colocar todo o jogo em uma única Scene.

---

# 26. SISTEMAS PRINCIPAIS

Arquitetar sistemas independentes.

Exemplo:

GameManager

PlayerController

PlayerStats

CharacterData

EnemyController

EnemySpawner

WaveManager

ExperienceSystem

LevelSystem

UpgradeSystem

UpgradeCard

Projectile

HealthSystem

DamageSystem

ShipController

SceneTransitionManager

UIManager

Evitar classes gigantes que controlam múltiplos sistemas.

---

# 27. SCRIPTABLEOBJECTS

Utilizar ScriptableObjects sempre que fizer sentido para dados configuráveis.

Exemplo:

CharacterData

EnemyData

AbilityData

UpgradeData

WaveData

ShipData

Isso deve permitir alterar estatísticas pelo Inspector sem modificar código.

---

# 28. SISTEMA DE ATRIBUTOS

Criar estrutura reutilizável para:

MaxHealth

CurrentHealth

Strength

Power

Defense

Agility

AttackSpeed

MovementSpeed

CriticalChance

Esses atributos deverão aceitar modificadores provenientes das cartas.

Não espalhar valores fixos diretamente pelos scripts.

---

# 29. WEBGL

O projeto deve ser desenvolvido considerando desde o início compatibilidade com **Unity Web/WebGL**.

Evitar dependências incompatíveis com WebGL.

Considerar:

* desempenho;
* tamanho dos assets;
* quantidade de partículas;
* quantidade de inimigos;
* memória;
* carregamento;
* resolução.

Criar uma primeira build Web funcional o quanto antes durante o desenvolvimento.

---

# 30. RESOLUÇÃO

Priorizar desktop em navegador.

Resolução de referência:

1920 × 1080

A UI deverá utilizar Canvas Scaler configurado para adaptação a diferentes resoluções.

O gameplay deverá continuar utilizável em telas menores.

---

# 31. VERSIONAMENTO

O projeto será versionado utilizando Git e GitHub.

Utilizar um `.gitignore` apropriado para Unity.

Não versionar diretórios desnecessários como:

Library/

Temp/

Logs/

Obj/

Build/

quando apropriado.

Manter Assets, Packages e ProjectSettings versionados.

---

# 32. DEPLOY

O projeto deverá posteriormente gerar uma build Unity Web/WebGL.

Estrutura:

Unity

↓

Web Build

↓

GitHub

↓

serviço de hospedagem

↓

URL pública

Exemplo:

aetherion-voidfall.netlify.app

Cada nova versão publicada poderá ser disponibilizada através dessa URL.

---

# 33. MVP

IMPORTANTE:

Não tentar construir o jogo completo imediatamente.

O primeiro MVP deverá possuir apenas:

* menu;
* introdução simples;
* seleção dos três personagens;
* movimentação;
* um ataque básico por personagem;
* três tipos de inimigos;
* sistema de vida;
* sistema de dano;
* XP;
* Level Up;
* três cartas;
* algumas ondas;
* transição;
* uma nave funcional;
* dois tipos de inimigos espaciais;
* tela de vitória.

Depois de tudo isso funcionar, expandir o jogo.

---

# 34. ORDEM DE IMPLEMENTAÇÃO

Implementar obrigatoriamente de forma incremental.

## ETAPA 1

Estrutura do projeto.

Criar:

* pastas;
* scenes;
* GameManager;
* sistema de transição.

Testar.

---

## ETAPA 2

Menu principal.

Implementar:

* título;
* botão Jogar;
* navegação.

Testar.

---

## ETAPA 3

Seleção de personagens.

Criar dados dos:

* Guerreiro;
* Mago;
* Anjo.

Implementar CharacterData utilizando ScriptableObjects.

Testar seleção.

---

## ETAPA 4

Gameplay terrestre básico.

Criar:

* movimentação;
* personagem;
* câmera;
* cenário de teste.

Testar.

---

## ETAPA 5

Combate.

Criar:

* inimigo básico;
* vida;
* dano;
* ataque automático;
* projéteis.

Testar.

---

## ETAPA 6

Ondas.

Criar:

* EnemySpawner;
* WaveManager;
* progressão de dificuldade.

Testar.

---

## ETAPA 7

Progressão.

Criar:

* XP;
* Level Up;
* escolha entre três upgrades.

Testar.

---

## ETAPA 8

Fase espacial.

Criar:

* nave;
* movimentação;
* tiro;
* inimigos espaciais;
* background.

Testar.

---

## ETAPA 9

Transição completa.

Conectar:

GroundLevel

↓

SpaceLevel

Preservar:

* personagem;
* atributos;
* upgrades.

---

## ETAPA 10

WebGL.

Gerar build.

Executar no navegador.

Corrigir:

* resolução;
* performance;
* input;
* carregamento.

---

# 35. REGRA PARA O CURSOR

Antes de realizar alterações grandes:

1. Analise a estrutura atual do projeto Unity.
2. Informe quais arquivos precisam ser criados ou alterados.
3. Preserve sistemas existentes que estejam funcionando.
4. Não recrie sistemas desnecessariamente.
5. Não adicione bibliotecas externas sem necessidade.
6. Utilize recursos nativos da Unity sempre que possível.
7. Escreva código C# legível e modular.
8. Evite números mágicos.
9. Exponha configurações importantes através do Inspector ou ScriptableObjects.
10. Documente decisões importantes através de comentários curtos.
11. Garanta compatibilidade com WebGL.
12. Não implemente várias etapas futuras antes que a etapa atual esteja funcional.

---

# 36. PRIMEIRA TAREFA DO CURSOR

Utilizando toda a documentação acima como referência, analise o projeto Unity atualmente aberto.

Não implemente ainda todo o jogo.

Comece exclusivamente pela **ETAPA 1 — Fundação do Projeto**.

Primeiro:

1. Examine a estrutura atual do projeto.
2. Identifique a versão da Unity.
3. Identifique se o projeto utiliza Built-in, URP ou outro pipeline.
4. Liste os arquivos e Scenes existentes.
5. Verifique Input System e configurações relevantes.
6. Proponha a estrutura de pastas.
7. Proponha as Scenes iniciais.
8. Proponha a arquitetura dos sistemas essenciais.
9. Explique quais arquivos serão criados.
10. Somente depois implemente a estrutura inicial necessária.

Crie inicialmente:

* Boot Scene;
* MainMenu Scene;
* GameManager persistente;
* SceneTransitionManager;
* estrutura organizada de pastas.

Depois teste se:

Boot

→ MainMenu

funciona corretamente.

Não avance para CharacterSelection enquanto essa primeira estrutura não estiver funcional.

Ao terminar, informe:

* arquivos criados;
* arquivos alterados;
* funcionamento implementado;
* como testar dentro da Unity;
* próximos passos recomendados.
