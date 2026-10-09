# PROJECT AI COMMAND CENTER
## Jogo 2D de Plataforma — Pixel Art 16-bit / Unity

> **Documento mestre para uso no Cursor + agentes Grok + Aseprite + Unity.**
>
> Este arquivo é a referência operacional inicial do projeto. O Cursor deve ler este documento antes de executar qualquer tarefa relacionada à arte, cenário, arquitetura técnica ou organização do projeto.

---

# 1. OBJETIVO DO PROJETO

Estamos desenvolvendo um jogo **2D de plataforma e ação**, construído em **Unity**, com identidade visual inspirada na era **16-bit**, especialmente na linguagem visual de jogos de Super Nintendo e Mega Drive.

O objetivo não é simplesmente produzir "pixel art bonita".

O objetivo é criar um mundo que pareça ter:

- história;
- arquitetura própria;
- civilização;
- religião;
- tecnologia;
- ruínas;
- vegetação;
- escala;
- profundidade;
- narrativa ambiental.

O jogador deve conseguir atravessar uma fase e entender parte da história **apenas observando o cenário**, sem depender de texto.

A experiência desejada é:

> **"Estou explorando as ruínas de uma civilização alienígena que realmente existiu."**

---

# 2. PREMISSA DO MUNDO

Milhares de anos atrás, uma civilização extremamente avançada existia em um planeta alienígena.

Essa civilização construiu cidades, templos e monumentos dedicados a entidades alienígenas que eram consideradas **divindades**.

Essas entidades desapareceram.

A civilização entrou em colapso.

As cidades foram abandonadas.

Com o passar de milhares de anos:

- estruturas desabaram;
- estátuas foram destruídas;
- templos foram parcialmente soterrados;
- a vegetação tomou conta das construções;
- tecnologias antigas perderam sua função;
- símbolos religiosos permaneceram;
- partes das antigas divindades continuam presentes nas ruínas.

O jogador controla um guerreiro que atravessa esse planeta abandonado.

---

# 3. DIREÇÃO ARTÍSTICA

## Referências de linguagem visual

Usar como referência de estudo:

- Super Metroid
- Terranigma
- Chrono Trigger
- ActRaiser
- Castlevania: Symphony of the Night

**IMPORTANTE:** não copiar elementos específicos dessas obras.

Usar apenas conceitos de:

- composição;
- leitura de cenário;
- pixel art;
- contraste;
- profundidade;
- arquitetura;
- narrativa ambiental;
- uso de paleta;
- parallax;
- detalhamento.

O projeto precisa desenvolver uma identidade própria.

---

# 4. PRINCÍPIO FUNDAMENTAL DO CENÁRIO

O cenário NÃO deve parecer:

> "Uma plataforma com algumas pedras e prédios atrás."

O cenário deve parecer:

> "Uma cidade antiga construída por uma civilização inteira, que foi destruída e posteriormente tomada pela natureza."

Cada objeto precisa parecer pertencer ao mesmo mundo.

Os elementos não devem ser distribuídos aleatoriamente apenas para preencher espaço.

Devem existir **grupos arquitetônicos e narrativos**.

Exemplo:

```text
TEMPLO
├── escadaria
├── entrada
├── colunas
├── símbolos
├── altar
├── estátuas
├── ruínas
└── vegetação
```

Outro:

```text
MONUMENTO
├── pedestal
├── estátua
├── fragmentos
├── símbolos
└── vegetação
```

Outro:

```text
TORRE
├── fundação
├── corpo
├── janelas
├── ornamentos
├── rachaduras
└── partes destruídas
```

---

# 5. REQUISITOS DO JOGO

O jogo é um **platformer 2D**.

O personagem se movimenta principalmente na horizontal.

O jogador precisa contemplar o cenário enquanto percorre a plataforma.

Portanto, o cenário precisa funcionar simultaneamente como:

1. espaço jogável;
2. decoração;
3. narrativa;
4. ambientação;
5. construção de mundo.

O chão não deve ser uma linha visualmente vazia.

---

# 6. ESTRUTURA DE CAMADAS

O cenário deve ser construído em múltiplas camadas.

## Layer 0 — Sky

Elementos:

- céu alienígena;
- estrelas;
- lua/planeta;
- nebulosas;
- atmosfera;
- partículas extremamente distantes.

Movimento:

```text
Parallax aproximado: 0.02 – 0.05
```

---

## Layer 1 — Far Background

Elementos:

- montanhas;
- silhuetas de cidades;
- torres gigantes;
- templos distantes;
- estruturas colossais;
- ruínas quase ocultas.

Movimento:

```text
Parallax aproximado: 0.08 – 0.15
```

---

## Layer 2 — Background Architecture

Elementos:

- templos;
- monumentos;
- torres;
- estátuas;
- portais;
- grandes estruturas religiosas.

Movimento:

```text
Parallax aproximado: 0.20 – 0.35
```

---

## Layer 3 — Gameplay

Esta é a camada principal do jogador.

Elementos:

- chão;
- plataformas;
- paredes;
- escadas;
- ruínas;
- pedras;
- objetos interativos;
- estruturas que fazem parte da gameplay.

Movimento:

```text
Parallax: 1.0
```

---

## Layer 4 — Foreground

Elementos:

- raízes;
- vegetação;
- pedras;
- galhos;
- estruturas parcialmente destruídas;
- elementos próximos da câmera;
- partículas.

Movimento:

```text
Parallax: 1.05 – 1.15
```

O foreground deve ser usado com moderação para criar profundidade sem prejudicar a leitura do personagem.

---

# 7. FILOSOFIA DE PROFUNDIDADE

A câmera deve transmitir sensação de espaço.

O jogador deve conseguir distinguir:

```text
CÉU
↓
CIDADE DISTANTE
↓
TEMPLOS
↓
ESTÁTUAS
↓
RUÍNAS
↓
PLATAFORMA
↓
VEGETAÇÃO PRÓXIMA
```

Não colocar todos os elementos na mesma profundidade.

---

# 8. ESCALA

Um dos problemas identificados nos protótipos atuais é a falta de escala monumental.

O jogador deve parecer pequeno diante das estruturas antigas.

Criar elementos como:

- estátuas gigantes;
- cabeças de divindades enterradas;
- mãos colossais;
- templos enormes;
- torres que desaparecem no alto;
- portões gigantes;
- monumentos parcialmente destruídos.

Exemplo narrativo:

O personagem pode caminhar por uma área aparentemente simples e, depois de alguns segundos, descobrir que uma grande estrutura no fundo é na verdade parte de uma estátua colossal.

---

# 9. DIVINDADES ALIENÍGENAS

As divindades são um dos elementos centrais da identidade do mundo.

Elas não devem parecer:

- humanos;
- cavaleiros;
- reis;
- demônios genéricos;
- monstros medievais.

Devem parecer entidades alienígenas antigas.

Possíveis características:

- múltiplos braços;
- membros alongados;
- anatomia assimétrica;
- olhos incomuns;
- cabeças não humanas;
- estruturas circulares;
- halos;
- formas geométricas;
- elementos orgânicos;
- símbolos religiosos.

Criar versões reutilizáveis:

```text
DIVINDADE INTACTA
DIVINDADE QUEBRADA
CABEÇA
TORSO
BRAÇO
MÃO
PEDESTAL
FRAGMENTOS
```

As peças devem poder ser combinadas no Aseprite e na Unity.

---

# 10. ARQUITETURA DA CIVILIZAÇÃO

A arquitetura precisa ter uma linguagem própria.

Criar formas recorrentes.

Exemplos:

- círculos;
- anéis;
- olhos;
- triângulos;
- linhas verticais;
- símbolos;
- formas orgânicas;
- padrões repetitivos.

Esses elementos devem aparecer em:

- paredes;
- templos;
- estátuas;
- portas;
- pisos;
- monumentos;
- altares;
- objetos.

Isso cria coerência visual.

---

# 11. RUÍNAS

A destruição precisa parecer consequência do tempo e de acontecimentos do mundo.

Evitar colocar objetos quebrados aleatoriamente.

Preferir:

- paredes parcialmente desabadas;
- torres inclinadas;
- colunas caídas;
- templos parcialmente soterrados;
- estátuas enterradas;
- escadarias interrompidas;
- portões destruídos;
- blocos acumulados;
- raízes atravessando paredes;
- vegetação crescendo nas rachaduras.

---

# 12. VEGETAÇÃO

A natureza está recuperando a cidade.

Criar assets independentes:

- musgo;
- raízes;
- cipós;
- plantas alienígenas;
- fungos;
- ervas;
- flores;
- plantas penduradas;
- vegetação de rachadura;
- pequenos organismos.

A vegetação deve parecer antiga e integrada às ruínas.

Não deve parecer simplesmente uma camada verde colocada por cima.

---

# 13. GAMEPLAY E PLATAFORMAS

O chão deve possuir identidade.

Criar variações:

- chão de pedra;
- chão rachado;
- chão com musgo;
- chão com raízes;
- chão parcialmente destruído;
- chão elevado;
- degraus;
- plataformas pequenas;
- colunas caídas;
- ruínas formando plataformas.

O cenário jogável deve parecer parte da cidade.

---

# 14. MICRODETALHES

Adicionar pequenos elementos para incentivar o jogador a observar.

Exemplos:

- pedras;
- fragmentos de estátuas;
- ossos de criaturas;
- placas antigas;
- símbolos;
- cristais;
- fontes de energia;
- mecanismos;
- metal antigo;
- pequenos animais alienígenas;
- fungos;
- partículas;
- objetos religiosos.

Não colocar todos os detalhes em todos os lugares.

A densidade deve variar.

---

# 15. PONTOS DE INTERESSE

Durante a caminhada, o cenário deve apresentar elementos progressivamente mais importantes.

Exemplo de sequência:

```text
0–3 segundos
Pequenas ruínas.

3–6 segundos
Fragmentos de uma estátua.

6–10 segundos
Cabeça de uma divindade parcialmente enterrada.

10–15 segundos
Templo ao fundo.

15–20 segundos
Grande estátua quebrada.

20–25 segundos
Torre monumental.

25–30 segundos
Portal antigo emitindo energia.
```

A intenção é criar uma sensação de descoberta.

---

# 16. PIXEL ART

A arte deve ser desenvolvida pensando em pixel art verdadeira.

Características:

- pixels visíveis;
- formas legíveis;
- silhuetas fortes;
- paleta limitada;
- clusters de pixels;
- sombreamento controlado;
- sem aparência de pintura digital;
- sem suavização;
- sem antialiasing.

Evitar gerar um desenho moderno e simplesmente aplicar filtro pixelado.

O resultado deve parecer construído em pixels desde o início.

---

# 17. RESOLUÇÃO DE TRABALHO

Preferência inicial:

```text
320 × 180
```

ou

```text
426 × 240
```

para composição de referência.

A escala final será feita posteriormente na Unity.

A decisão final de resolução pode ser revisada depois de validar:

- tamanho do personagem;
- tamanho dos tiles;
- câmera;
- densidade dos pixels;
- leitura dos detalhes.

Não aumentar a resolução apenas para adicionar mais detalhes.

---

# 18. PALETA INICIAL

Paleta base sugerida:

- azul extremamente escuro;
- roxo escuro;
- cinza frio;
- cinza violeta;
- verde musgo;
- verde alienígena;
- cinza claro;
- ciano pontual;
- dourado/laranja pontual.

Cores luminosas devem ser raras.

Ciano/dourado podem representar:

- tecnologia antiga;
- energia;
- símbolos religiosos;
- mecanismos;
- portais;
- objetos importantes.

---

# 19. PIPELINE DE PRODUÇÃO

O pipeline oficial inicial será:

```text
GROK BOT
Direção artística
        ↓
CONCEITO
        ↓
ASSETS
        ↓
ASEPRITE
Ajuste / limpeza / organização
        ↓
UNITY
Tilemap / Sprite / Parallax
        ↓
TESTE
        ↓
REVISÃO ARTÍSTICA
        ↓
GROK BOT
        ↓
NOVAS TAREFAS
```

---

# 20. RESPONSABILIDADES DOS AGENTES

## AGENTE 1 — DIRETOR DE ARTE

Nome sugerido:

```text
GROK ART DIRECTOR
```

Responsabilidades:

- identidade visual;
- arquitetura;
- divindades;
- paleta;
- composição;
- narrativa ambiental;
- revisão dos assets;
- definição de prioridades artísticas.

Não deve alterar código da Unity.

---

## AGENTE 2 — ENGINEERING AGENT

Nome:

```text
CURSOR UNITY ENGINEER
```

Responsabilidades:

- Unity;
- C#;
- Tilemap;
- câmera;
- parallax;
- prefabs;
- importação;
- organização;
- ferramentas;
- integração dos assets;
- testes.

Não deve inventar arte definitiva quando o projeto exigir um asset artístico.

---

## AGENTE 3 — PIXEL ART / ASEPRITE

Responsabilidades:

- limpeza de sprites;
- criação de tiles;
- organização dos spritesheets;
- correção de pixels;
- preparação para Unity;
- exportação.

---

# 21. FONTE DA VERDADE

Criar no projeto:

```text
AI/
├── TASKS.md
├── ART_REVIEW.md
├── CHANGELOG.md
└── AGENT_PROTOCOL.md

ArtBible/
├── VISUAL_STYLE.md
├── PALETTE.md
├── WORLD_LORE.md
├── ENVIRONMENT_GUIDE.md
└── ASSET_RULES.md
```

Nenhum agente deve considerar uma decisão importante como permanente sem registrar no projeto.

---

# 22. TASKS.md

Formato:

```markdown
# TASKS

## ENV-001
### Criar linguagem visual das divindades

Responsável:
GROK ART DIRECTOR

Status:
TODO

Descrição:
Definir anatomia, símbolos, proporções e linguagem visual das divindades.

Dependências:
Nenhuma.
```

Status possíveis:

```text
TODO
IN PROGRESS
REVIEW
DONE
BLOCKED
```

---

# 23. ART_REVIEW.md

O diretor de arte deve registrar problemas assim:

```markdown
# ART REVIEW

## REVIEW-001

Problema:
A camada de gameplay está visualmente vazia.

Impacto:
Baixa densidade visual durante a movimentação.

Correção sugerida:
Adicionar variações de chão, raízes, fragmentos e pequenos objetos.

Tarefa relacionada:
ENV-XXX
```

---

# 24. CHANGELOG.md

Após cada alteração relevante:

```markdown
# CHANGELOG

## 2026-09-24

### Art
- Criada estrutura inicial de direção artística.

### Unity
- Estrutura inicial do pipeline de ambiente definida.

### AI
- Protocolo de comunicação entre agentes criado.
```

---

# 25. REGRAS DO CURSOR

Antes de modificar o projeto:

1. Ler este documento.
2. Ler `AI/TASKS.md`.
3. Ler `AI/ART_REVIEW.md`.
4. Ler `ArtBible/VISUAL_STYLE.md`.
5. Inspecionar a estrutura atual do projeto.
6. Identificar tarefas relacionadas.
7. Não alterar sistemas não relacionados sem necessidade.

Depois de modificar:

1. Testar.
2. Verificar erros.
3. Registrar alterações.
4. Atualizar tarefas.
5. Atualizar changelog.

---

# 26. REGRAS PARA ASSETS

Todo asset precisa respeitar:

- mesma escala de pixel;
- mesma perspectiva;
- mesma iluminação;
- mesma paleta;
- mesmo nível de detalhamento;
- mesma linguagem arquitetônica.

Não aceitar assets visualmente incompatíveis apenas porque são bonitos individualmente.

---

# 27. PRIMEIRA GRANDE META

A primeira meta não é criar a fase inteira.

A primeira meta é construir um **KIT DE IDENTIDADE VISUAL**.

Esse kit deve conter:

### Arquitetura
- 3 paredes;
- 3 colunas;
- 2 arcos;
- 2 portas;
- 2 pisos;
- 2 ruínas.

### Divindades
- 1 divindade principal;
- 1 versão quebrada;
- 1 cabeça;
- 1 braço;
- fragmentos.

### Vegetação
- 3 tipos de musgo;
- 3 raízes;
- 3 plantas alienígenas;
- 2 fungos.

### Objetos
- 5 pedras;
- 3 cristais;
- 3 símbolos;
- 3 objetos religiosos.

### Gameplay
- tiles de chão;
- plataformas;
- degraus;
- bordas;
- variações quebradas.

Depois disso, montar uma pequena seção de teste.

---

# 28. PRIMEIRA FASE DE TESTE

Criar uma área curta:

```text
aproximadamente 30–60 segundos de caminhada
```

Ela deve conter:

```text
INÍCIO
↓
ruínas pequenas
↓
fragmentos de estátuas
↓
primeiro monumento
↓
templo distante
↓
grande divindade destruída
↓
portal/estrutura misteriosa
FIM
```

O objetivo é validar a linguagem visual antes de produzir dezenas de assets.

---

# 29. CRITÉRIOS DE ACEITAÇÃO

O cenário só será considerado aprovado quando:

### Visual

- [ ] Parece pixel art verdadeira.
- [ ] Possui identidade própria.
- [ ] Não parece cenário procedural genérico.
- [ ] Possui profundidade.
- [ ] Possui escala monumental.
- [ ] Possui vegetação integrada.
- [ ] Possui arquitetura consistente.
- [ ] Possui detalhes suficientes.

### Narrativa

- [ ] É possível perceber que uma civilização existiu.
- [ ] É possível perceber que havia uma religião.
- [ ] As divindades são reconhecíveis como parte daquela civilização.
- [ ] A destruição parece antiga.
- [ ] A natureza está recuperando o local.

### Gameplay

- [ ] O personagem é facilmente legível.
- [ ] O chão possui variações.
- [ ] O fundo não compete com o personagem.
- [ ] O cenário funciona com câmera 2D.
- [ ] O parallax cria profundidade.

### Técnico

- [ ] Sprites importados corretamente.
- [ ] Pixels sem blur.
- [ ] Filtro Point.
- [ ] Compressão adequada.
- [ ] Pixels Per Unit consistente.
- [ ] Tilemap funcional.
- [ ] Parallax funcionando.
- [ ] Sem erros no console.

---

# 30. REGRA DE OURO

Antes de adicionar qualquer objeto ao cenário, perguntar:

> **"Por que esse objeto existe neste mundo?"**

Se a resposta for apenas:

> "Para preencher espaço."

Não adicionar.

O cenário deve parecer descoberto, não preenchido.

---

# 31. PRIMEIRA TAREFA DO CURSOR

Ao receber este documento, NÃO começar imediatamente a criar o cenário.

Primeiro:

1. analisar o projeto Unity existente;
2. mapear a estrutura de pastas;
3. identificar sistema atual de personagem;
4. identificar câmera;
5. identificar Tilemap;
6. identificar resolução atual;
7. identificar pixels por unidade;
8. identificar assets existentes;
9. verificar se existe sistema de parallax;
10. verificar pipeline de importação de sprites.

Depois criar:

```text
AI/TASKS.md
AI/ART_REVIEW.md
AI/CHANGELOG.md
AI/AGENT_PROTOCOL.md

ArtBible/VISUAL_STYLE.md
ArtBible/PALETTE.md
ArtBible/WORLD_LORE.md
ArtBible/ENVIRONMENT_GUIDE.md
ArtBible/ASSET_RULES.md
```

Não destruir ou substituir sistemas existentes sem verificar primeiro.

---

# 32. PRIMEIRA RESPOSTA ESPERADA DO CURSOR

Depois de analisar o projeto, responder com:

```text
PROJECT AUDIT

Unity:
[versão]

Resolução:
[resolução]

Pixels Per Unit:
[valor]

Câmera:
[descrição]

Tilemap:
[sim/não + descrição]

Parallax:
[sim/não]

Estrutura de arte:
[descrição]

Assets existentes:
[resumo]

Problemas encontrados:
[lista]

Tarefas propostas:
[lista]

Próxima tarefa recomendada:
[uma tarefa]
```

Somente depois dessa auditoria começar a implementação.

---

# 33. PRINCÍPIO FINAL

Este projeto não deve ser desenvolvido como:

```text
IA gera imagem
↓
imagem vai para Unity
↓
fim
```

O processo deve ser:

```text
LORE
↓
DIREÇÃO ARTÍSTICA
↓
DESIGN DE ASSETS
↓
PIXEL ART
↓
ASEPRITE
↓
UNITY
↓
COMPOSIÇÃO
↓
PARALLAX
↓
TESTE
↓
REVISÃO
↓
ITERAR
```

O objetivo é construir um **mundo**, não apenas um background.

---

# INÍCIO DO PROJETO

**Data inicial:** 24/09/2026

**Engine:** Unity

**Gênero:** 2D Platformer / Action

**Estética:** Pixel Art 16-bit

**Ferramenta de pixel art:** Aseprite

**IDE/Agente técnico:** Cursor + Grok 4.7

**Diretor artístico:** Grok Bot

**Objetivo atual:** construir a identidade visual e o primeiro ambiente jogável da cidade alienígena em ruínas.
