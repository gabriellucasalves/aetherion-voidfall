# Aetherion: Voidfall — Plano de sprints

Documento mestre da construção do jogo.

Tudo que for implementado no Cursor, na Unity ou no Blender deve caber em uma sprint deste arquivo.  
Não avançar duas sprints de gameplay ao mesmo tempo. Arte pode andar em paralelo quando o conceito em imagem já tiver sido entregue.

**Onde se desenvolve:** Unity.  
**Onde se desenha e anima o pixel art:** Aseprite (versão completa) ou Pixelorama.  
**Onde se modela 3D em blocos:** Blender, só se a gente ainda quiser o recorte de cubos.  
**Onde os colegas jogam:** build WebGL publicada (Vercel ou equivalente).

---

## 1. Visão do produto

O jogador escolhe um de três heróis e enfrenta **seis fases**:

| Frente | Fase | Nome provisório | Objetivo | Chefe |
|---|---|---|---|---|
| Terra | T1 | Ruínas da Borda | Aprender movimento, ataque e o primeiro bioma | Não |
| Terra | T2 | Cidades Sumidas | Aumentar densidade, novos inimigos, progressão mais forte | Não |
| Terra | T3 | Coração do Véu | Confrontar o avanço terrestre | **Sim — Boss terrestre** |
| Espaço | E1 | Órbita Quebrada | Trocar o corpo pela nave, aprender o shoot'em up | Não |
| Espaço | E2 | Bloqueio da Frota | Formações, atiradores, build convertida da terra | Não |
| Espaço | E3 | Fenda do Vazio | Confrontar a frota final | **Sim — Boss espacial** |

Depois de T3 o herói entra na nave.  
Os atributos e as cartas da terra **continuam** no espaço, convertidos para dano, escudo, velocidade e especial da nave.

Vitória só depois de derrotar o boss de E3.

---

## 2. O que já está feito (Sprint 0 — encerrada)

- Projeto Unity 6 compilando
- Boot → Menu → Introdução → Seleção
- Três heróis com atributos e habilidade inicial no `CharacterCatalog`
- Guerreiro 8-bit modelado no Blender (`Assets/Art/Guerreiro8bit.blend` / `.glb`)
- Documento de estudo e este plano de sprints

A seleção **ainda não entra em combate**. Isso começa na Sprint 1.

---

## 3. Regras para todas as sprints

1. Uma sprint de sistemas só fecha se o Play da Unity demonstrar o critério de aceite.
2. Conceito em imagem **antes** de modelo 3D. Sem imagem, usa placeholder de cubo/sprite.
3. Cada herói precisa se sentir diferente no movimento e no ataque. Se os três jogarem igual, a sprint de kit não fecha.
4. Progressão nunca nasce pronta. Cada sprint libera **uma camada** do sistema. Ver seção 6.
5. Fundo de cenário é parte da sprint da fase, não um extra.
6. Boss só nas sprints T3 e E3.
7. WebGL / Vercel só na sprint de publicação. Antes disso, testa no Editor.
8. Se atrasar: corta conteúdo da fase do meio (T2 ou E2), não corta seleção, não corta um kit jogável, não corta um boss, não corta o link.
9. Na terra a câmera é lateral, tipo **Metal Slug**: só frente e trás, pulo e plataformas. Sem andar livre nos quatro eixos. O fundo em camadas é parte do jogo. No espaço a nave continua shoot'em up.

---

## 4. Os três heróis — contrato de jogabilidade

Estes contratos não mudam no meio do caminho. Sprints de kit e de progressão só **expandem** o que está aqui.

### Guerreiro — tank / corpo a corpo

- Vida alta, defesa alta, anda mais devagar
- Ataque curto: **Corte de Energia** (golpe de espada na frente, manual)
- Bloqueio com escudo (consome a barra de defesa)
- No espaço: nave pesada, tiro lento e forte, mais escudo
- Evolução favorece vida, defesa, área do corte e stun
- Conceito 3D: **já existe**

### Mago — dano / área

- Vida baixa, poder alto, alcance longo
- Ataque: **Orbe Arcano** automático em inimigo próximo
- No espaço: nave frágil, projéteis rápidos e em leque
- Evolução favorece orbes extras, área, elemento e crítico
- Conceito 3D: **aguardando imagem**

### Anjo — mobilidade / equilíbrio

- Stats no meio, agilidade máxima
- Ataque: **Pena Celestial** em linha ou leque curto
- Extra de movimento: dash / esquiva no Shift (Sprint 3)
- No espaço: nave mais rápida, tiro frequente, maior esquiva
- Evolução favorece velocidade, dash, penas múltiplas
- Conceito 3D: **aguardando imagem**

Controles comuns na terra (fonte: Metal Slug):

- A/D ou setas — andar só no eixo X
- Espaço / W — pular
- Clique / J — atacar (Guerreiro corta na frente; não atira)
- S / K / botão direito — bloquear com o escudo
- ESC — pausa
- Shift — dash do Anjo (Sprint 3; depois carta pode liberar para outros)
- Na terra o Guerreiro não tem tiro automático. Orbe automático do Mago fica na Sprint 3.

Não volta arena top-down. O herói atravessa um corredor de ruínas, sobe pedra e muro, e o fundo (céu, serras, torres) fica atrás em parallax. Pixel art detalhado entra quando a imagem-conceito do cenário chegar; até lá o placeholder tem que ler como fase lateral, não como chão visto de cima.

---

## 5. Pipeline de arte

A demo T1 é **pixel art 2D**. O Blender de blocos fica parado neste recorte.

```
1. Conceito em imagem (já temos o Guerreiro)
2. Aseprite / Pixelorama — tela 64×64, paleta de 8–12 cores
3. Animar: idle, walk, jump, corte, block
4. Exportar spritesheet PNG (filtro Point, sem suavizar)
5. Unity toca a animação no pulo / andar / ataque / escudo
```

**Aseprite trial não salva.** Precisa da versão completa (Steam/site) ou usar Pixelorama para gravar.

### Contrato da demo — só Guerreiro em T1

Canvas: **64×64**. Pé no chão da célula (linha y = 60). Personagem olha para a **direita**. Pivot no centro-baixo.

| Tag | Quadros | Quando toca |
|---|---|---|
| idle | 4 | parado no chão |
| walk | 6 | A/D no chão |
| jump | 3 | subindo, ápice, caindo |
| attack | 4 | clique / J (Corte) |
| block | 2 | S / K / direito |
| hurt | 2 | tomou hit |

Arquivo de trabalho: `Assets/Art/Guerreiro/Guerreiro_sheet.png`  
Paleta: `Assets/Art/Guerreiro/paleta.png`

Mago, Anjo, T2 e cartas **não** entram nesta demo.

### Fila de conceitos que você vai entregar

Marcar quando a imagem chegar. Sem o visto, a sprint usa cubo colorido.

**Heróis**

- [x] Guerreiro (corpo)
- [x] Mago (corpo)
- [x] Anjo (corpo)
- [ ] Nave do Guerreiro
- [ ] Nave do Mago
- [ ] Nave do Anjo

**Cenários de fundo**

- [x] T1 — cidade de Aetherion destruída, **16-bit dark fantasy modular** (nada de imagem única):
  céu noturno com lua/estrelas/brasas (parallax 0.95) → serras e castelo distante (0.90) →
  cidade em ruínas com catedral, torres, casas, ponte partida, bandeiras e fogos (0.80) →
  props próximos: árvore morta, estátua, lampião, entulho, coluna (0.40) →
  plano jogável: calçada de lajes + plataformas de ruína com musgo e vinhas (colisores intactos) +
  decoração fixa estilo SNES no plano do personagem: estátuas destruídas de guerreiro (espada
  fincada, braço quebrado) e de mago (cajado partido, orbe caído), muralhas com arcos, colunas
  caídas, moitas de musgo e pedras — com contorno 1px pra separar do fundo — e silhuetas escuras
  em primeiro plano (na frente do herói, parallax invertido leve) fechando a sensação de 3D.
  Camadas geradas por `Assets/Art/T1/build_t1.py` → PNGs em `Assets/Resources/T1/` (PPU 16, Point).
  Luzes pulsantes (`T1Glow`), 3 faixas de neblina (`T1Mist`) e chuva sutil por cima.
- [ ] T2 — cidade desaparecendo
- [ ] T3 — fissura do Véu (arena do boss)
- [ ] E1 — órbita de Aetherion
- [ ] E2 — bloqueio de naves
- [ ] E3 — fenda no espaço (arena do boss)

**Inimigos terra** (arte em `Assets/Art/Inimigos/build_enemies.py`, sheets em `Resources/Inimigos/`, animados por `InimigoVisual` e sorteados pelo `SimpleEnemySpawner`)

- [x] Criatura básica → **Esqueleto Guerreiro** (HP 36 · vel 2.35 · arremessa machado giratório reto, dano 11, alcance 6.5 · resistência normal)
- [x] Criatura veloz → **Ghoul Veloz** (HP 22 · vel 3.7 · cospe bola ácida em arco, dano 9, alcance 5.5 · frágil, toma +40% de dano)
- [x] Criatura resistente → **Zumbi Corrompido** (HP 72 · vel 1.4 · kamikaze: pisca vermelho 0.5s e explode em área, dano 24, raio 2.1 · tanque, toma −35% de dano)
- [x] IA por fases: perseguir → armar (pisca laranja; zumbi pisca vermelho rápido) → golpe → recuperar; toque continua causando dano leve
- [x] Projéteis: machado enferrujado girando (`AxeProjectile`, voo reto) e bola ácida em arco com respingos (`AcidBall`)
- [x] Resistências por classe via `EnemyData.DamageTaken` — espada, projétil e orbe do jogador passam por `EnemyController.ReceiveDamage`
- [x] Ondas em ordem de classe: onda 1 esqueletos → 2 ghouls → 3 zumbis → repete crescendo; banner "ONDA N" + contador no HUD
- [x] Plataformas viraram one-way (`PlatformEffector2D`): dá pra subir atravessando por baixo e pousar em cima
- [ ] Criatura voadora
- [ ] Atirador terrestre
- [ ] Boss terrestre

**Inimigos espaço**

- [ ] Drone
- [ ] Caça
- [ ] Nave pesada
- [ ] Atirador espacial
- [ ] Formação pequena
- [ ] Boss espacial

**UI / extra**

- [ ] Cartas de upgrade (moldura comum / rara / épica / lendária)
- [ ] Portal / nave de transição T3 → E1
- [ ] Ícones de HUD

Enquanto a imagem não chega, a sprint de gameplay **não para**. Usa primitive 8-bit (quadrado + cor da classe).

---

## 6. Sistema de progressão — cresce por sprint

A progressão é um único sistema. Ele nasce magro e engorda.  
Nunca implementar raridade, evolução IV e carta exclusiva de classe no mesmo dia.

| Camada | Nome | Sprint que nasce | O que o jogador ganha |
|---|---|---|---|
| P0 | Atributos base | 0 (já existe) | Vida, força, poder, defesa, agilidade, velocidade de ataque |
| P1 | XP + nível | 5 | Barra de XP, level up pausa o jogo |
| P2 | Cartas comuns | 5 | +dano, +vida, +velocidade |
| P3 | Kit cresce | 6 | Carta de upgrade da habilidade I → II |
| P4 | Raridade | 6 | Comum / raro (peso na rolagem) |
| P5 | Cartas de classe | 7 | 1 carta exclusiva por herói |
| P6 | Conversão terra→espaço | 8 | Força→dano da nave, defesa→escudo, agilidade→velocidade, poder→especial |
| P7 | Habilidade III / IV | 10 | Mais projéteis, área, efeito |
| P8 | Evolução final | 11 | Corte/Orbe/Pena vira forma final só se chegou no nível IV |

Regras da progressão:

- Toda carta é um `UpgradeData` (ScriptableObject)
- Todo atributo aceita modificador. Proibido número solto no inimigo ou no projétil
- Level up sempre oferece **3 cartas**
- A build da terra atravessa as 6 fases da mesma run
- Game over volta ao menu; a run não é persistente entre execuções nesta versão

---

## 7. Arquitetura que as sprints devem respeitar

Cenas (criar quando a sprint pedir, não antes):

```
Boot
MainMenu
Intro
CharacterSelection
Ground_T1
Ground_T2
Ground_T3
Space_E1
Space_E2
Space_E3
Victory
Defeat
```

Pastas de script já existentes: `Core`, `Player`, `Enemies`, `Combat`, `Abilities`, `Upgrades`, `Waves`, `UI`, `Ships`, `Managers`.

Dados em ScriptableObject:

- `CharacterData`
- `EnemyData`
- `AbilityData`
- `UpgradeData`
- `WaveData`
- `StageData` (fundo, lista de ondas, se tem boss, próxima cena)
- `ShipData`
- `BossData`

Um `RunState` no `GameManager` guarda: herói, atributos atuais, cartas, fase atual, kills, nível.  
Esse estado sobrevive à troca T3 → E1.

---

## 8. Mapa das sprints

Cada sprint abaixo é uma fatia jogável. Duração sugerida: **3 a 7 dias**.  
Ajuste o calendário; não ajuste a ordem.

Arte em paralelo está marcada como **[ARTE]**. Ela não bloqueia o fechamento se o placeholder estiver visível.

---

### Sprint 0 — Fundação

**Status:** concluída.

**Aceite já cumprido:** menu, intro, seleção dos 3 heróis, Guerreiro no Blender.

**Polimento posterior (telas):** menu com guerreiro animado em vitrine, título com sombra e controles reais nas configurações; fundo compartilhado com lua pixel art, fissura dentada pulsando e silhueta da cidade em ruínas; intro com efeito máquina de escrever + indicador de páginas; seleção com retrato animado do guerreiro (sheet real), barras de atributos gráficas e cards "EM BREVE" para mago/anjo.

---

### Sprint 1 — Entrar na Terra 1 e andar

**Objetivo:** o herói escolhido aparece em T1 e anda.

**Jogador sente:** escolhe o Guerreiro (ou outro), a cena muda, o personagem anda para os lados, pula em ruínas, a câmera acompanha na horizontal.

**Construir**

- Cena `Ground_T1`
- `PlayerController` (A/D + pulo, velocidade lida de `Agility`)
- `CameraFollow` lateral com look-ahead
- `StageData` mínimo de T1
- HUD crua: nome do herói + vida cheia
- Spawn do prefab/placeholder certo conforme `GameManager.SelectedHero`

**Progressão nesta sprint:** só P0 (atributos base influenciam velocidade).

**Arte**

- [ARTE] aplicar `.glb` do Guerreiro se já estiver importável; senão cubo vermelho
- Fundo T1: cor + tiles temporários até a imagem-conceito chegar

**Aceite**

- [x] JOGAR → intro → escolher herói → T1
- [x] Os 3 heróis andam, cada um com velocidade diferente
- [x] ESC pausa e volta ao menu
- [x] Não tem ataque ainda, e isso é consciente

**Proibido nesta sprint:** inimigo, XP, carta, dash, outra fase.

---

### Sprint 2 — Combate do Guerreiro

**Objetivo:** um herói completo de verdade, mesmo que os outros ainda atirem igual.

**Jogador sente:** criaturas andam até ele, ele corta na frente, segura o escudo, a vida e a defesa baixam em barras, ele pode morrer.

**Construir**

- `HealthSystem`, `DamageSystem`
- `EnemyData` + `EnemyController` (persegue o jogador)
- `AbilityData` do Corte + `MeleeSlash`
- Ataque manual corpo a corpo + bloqueio com `ShieldSystem`
- HUD com barra de vida e barra de escudo
- 1 tipo de inimigo básico, spawn contínuo simples
- Morte do player → tela temporária de derrota → menu

**Progressão:** dano do corte usa `Força`. Vida usa `MaxHealth`. Escudo usa `Defesa`.

**Arte**

- Placeholder do básico
- Aguardar conceito do básico para modelar no Blender

**Aceite**

- [x] Guerreiro mata pelo menos 10 básicos sem travar
- [x] Tomar dano reduz a HUD
- [x] Morrer volta ao fluxo de menu
- [x] Mago e Anjo ainda podem usar o mesmo ataque temporário

**Proibido:** ondas formais, cartas, T2.

---

### Sprint 3 — Kit dos três heróis + movimentação exclusiva

**Objetivo:** os três deixam de jogar igual.

**Jogador sente:** Mago segura distância, Anjo dasha, Guerreiro tanka no corpo a corpo.

**Construir**

- Orbe Arcano (busca alvo, dispara sozinho)
- Pena Celestial (projétil reto / leque curto)
- Dash do Anjo no Shift (invulnerabilidade curta)
- Ajuste de alcance, cadência e dano lidos do `CharacterData`
- Feedback visual por classe (cor do projétil)

**Progressão:** `Poder` no Mago, `Agilidade` no dash e na cadência do Anjo.

**Arte**

- [ARTE] Mago e Anjo: só modelar se a imagem já tiver chegado
- Efeito temporário de corte / orbe / pena em partículas 8-bit

**Aceite**

- [x] Trocar o herói na seleção muda ataque, velocidade e “feel”
- [x] Anjo dasha com Shift; os outros não
- [x] Mago acerta de mais longe que o Guerreiro
- [x] Guerreiro tem mais vida mensurável na prática

**Proibido:** cartas exclusivas ainda. Só o kit base.

---

### Sprint Demo T1 — Guerreiro pixel (atual)

**Objetivo:** uma fase de teste só com o Guerreiro desenhado e animado.

**Jogador sente:** o Guerreiro de pixel anda, pula, corta e bloqueia; T1 é a única fase da demo.

**Ordem**

1. Paleta + idle no Aseprite/Pixelorama
2. walk e jump
3. corte e block
4. Ligar a spritesheet na Unity
5. Só então as ondas da Sprint 4 em cima dessa arte

**Aceite da arte (antes de ondas)**

- [x] Idle e walk jogáveis no Play
- [x] Jump troca de quadro no ar
- [x] Corte e block têm pose própria
- [x] Filtro Point (pixel nítido, sem blur)

**Proibido:** pixelar Mago/Anjo, T2, cartas, boss.

---

### Sprint 4 — Terra 1 de verdade (ondas + fundo)

**Objetivo:** T1 vira fase, não arena infinita.

**Jogador sente:** onda 1, 2, 3… o fundo de ruínas existe, a dificuldade sobe, ao fim aparece o portal para T2 (ainda sem T2 jogável se a sprint 5 não tiver começado — o portal pode só dizer “próxima sprint” até fechar a 5).

**Construir**

- `WaveManager` + `WaveData` (quantidade, tipo, intervalo)
- 3 inimigos: básico, veloz, resistente
- Contador de onda no HUD
- Limite de fase: N ondas ou tempo
- `StageData` de T1 com fundo e música placeholder

**Ondas mínimas de T1**

1. Básicos
2. Mais básicos
3. Básicos + velozes
4. Resistentes entram
5. Mix e encerramento da fase

**Arte**

- [ARTE] conceitos de T1, básico, veloz, resistente → Blender
- Parallax simples (2–3 camadas) mesmo com sprites retangulares

**Aceite**

- [ ] Dá para terminar T1 sem console error
- [ ] Veloz é visivelmente mais rápido; resistente aguenta mais hit
- [ ] Fundo não é tela preta
- [ ] Ao terminar T1, o estado da run é preservado no `GameManager`

**Proibido:** boss, T3, espaço.

---

### Sprint 5 — Progressão P1 + P2 (XP, nível, 3 cartas)

**Objetivo:** o sistema de evolução nasce.

**Jogador sente:** mata, a barra de XP sobe, level up pausa, escolhe 1 de 3 cartas, o personagem fica mais forte na hora.

**Construir**

- `ExperienceSystem`, `LevelSystem`
- `UpgradeData` + `UpgradeSystem`
- Cartas iniciais (todas comuns):
  - Força Superior — +15% dano
  - Essência Vital — +20 vida máxima e cura essa vida
  - Passos do Vento — +10% velocidade
- UI de 3 cartas
- HUD: LV, XP, vida

**Progressão liberada:** P1 e P2.

**Arte**

- [ARTE] moldura de carta comum
- Sem raridade visual ainda

**Aceite**

- [ ] Subir 2 níveis numa run de T1
- [ ] Cada carta altera o atributo certo (medir no Inspector)
- [ ] O jogo despausa só depois da escolha
- [ ] Morrer descarta a run; não salva build em disco

**Proibido:** cartas épicas, evolução IV, loja persistente.

---

### Sprint 6 — Terra 2 + progressão P3 e P4

**Objetivo:** segundo bioma e a habilidade começa a evoluir.

**Jogador sente:** cenário muda (cidade sumindo), inimigos voadores ou atiradores entram, no level up pode vir upgrade de habilidade ou carta rara.

**Construir**

- Cena `Ground_T2`
- Novo fundo + nova paleta
- Inimigo voador e/ou atirador
- Carta de habilidade I → II por classe:
  - Corte II — onda maior
  - Orbe II — dois orbes ou cadência maior
  - Pena II — 2 penas
- Raridade comum / raro na rolagem
- Transição T1 → T2 automática ao fechar T1

**Progressão liberada:** P3 e P4.

**Arte**

- [ARTE] conceito T2, voador, atirador, carta rara

**Aceite**

- [ ] T1 termina e cai em T2 com a mesma build
- [ ] O fundo de T2 é distinto de T1
- [ ] Dá para ver a habilidade II diferente da I
- [ ] Carta rara aparece com cor/borda diferente

**Proibido:** boss.

---

### Sprint 7 — Terra 3 + Boss terrestre + cartas de classe

**Objetivo:** fechar a frente terrestre.

**Jogador sente:** o céu corrompe, o boss terrestre entra depois das ondas, mata-lo abre a nave. Cada classe tem 1 carta que só ela puxa.

**Construir**

- Cena `Ground_T3`
- `BossData` + padrão de ataque em 2 fases
- Portal / nave ao morrer o boss
- Textos: “A superfície está temporariamente segura.” / “Mas o Vazio já alcançou as estrelas.”
- Cartas exclusivas (P5):
  - Guerreiro — Armadura do Véu (+defesa e reflect curto)
  - Mago — Núcleo Arcano (orbe explode em área)
  - Anjo — Graça Alada (dash recarrega mais rápido + pena extra)

**Boss terrestre (mínimo)**

- Vida alta, movimento lento
- Ataque 1: investida
- Ataque 2: projéteis em leque
- Abaixo de 50% vida, fica mais rápido
- Sem invocação infinita que quebre WebGL

**Progressão liberada:** P5.

**Arte**

- [ARTE] conceito do boss terrestre, arena T3, portal/nave
- Modelar boss no Blender só com a imagem na mão

**Aceite**

- [ ] T1 → T2 → T3 numa run só
- [ ] Boss só nasce no fim de T3
- [ ] Derrotar o boss para as ondas e mostra a nave
- [ ] Carta de classe não aparece para outro herói
- [ ] Morrer no boss permite tentar de novo do menu (run nova)

**Proibido:** já controlar a nave nesta sprint. Só a transição cinematográfica / trigger.

---

### Sprint 8 — Naves, conversão de atributos, entrar no espaço

**Objetivo:** o herói vira nave sem perder a build.

**Jogador sente:** depois do boss terrestre, controla a nave com WASD, atira sozinho, o fundo espacial desliza. A build da terra pesa: Guerreiro tanka, Mago estoura, Anjo foge.

**Construir**

- `ShipData` por classe
- `ShipController`
- Tabela de conversão P6
- Cena `Space_E1` (pode nascer vazia de inimigos nesta sprint)
- Parallax de estrelas
- Escudo separado da vida (Guerreiro começa com mais)

**Conversão**

| Atributo na terra | Vira no espaço |
|---|---|
| Força | Dano do tiro |
| Defesa | Escudo máximo |
| Agilidade | Velocidade da nave |
| Poder | Cadência / especial |
| Vida | Casco |
| Cartas de habilidade | Modificam o padrão de tiro |

**Progressão liberada:** P6.

**Arte**

- [ARTE] conceitos das 3 naves → Blender
- Placeholder: bloco com a cor da classe

**Aceite**

- [ ] A mesma run chega em E1 com as cartas da terra
- [ ] As 3 naves se sentem diferentes sem inimigo (só no movimento/tiro contra um dummy)
- [ ] Fundo espacial tem scroll contínuo

**Proibido:** boss espacial.

---

### Sprint 9 — Espaço 1 (ondas orbitais)

**Objetivo:** E1 jogável de verdade.

**Jogador sente:** drones e caças vêm de cima e das laterais, a nave desvia e atira, termina a órbita e segue.

**Construir**

- `WaveData` espacial
- Drone e caça
- Formação simples em linha
- Limite de E1 e portal para E2

**Arte**

- [ARTE] drones, caças, fundo E1

**Aceite**

- [ ] Completar E1 com os 3 heróis
- [ ] Inimigos não nascem todos num frame só (performance WebGL)
- [ ] HUD espacial mostra casco, escudo, onda

---

### Sprint 10 — Espaço 2 + habilidade III/IV

**Objetivo:** o meio do espaço e o teto da build.

**Jogador sente:** naves pesadas e atiradores, cartas podem evoluir a habilidade para III ou IV.

**Construir**

- Cena `Space_E2`
- Nave pesada + atirador espacial
- Evolução III e IV da habilidade de cada classe (P7)
- Fundo de bloqueio de frota

**Progressão liberada:** P7.

**Aceite**

- [ ] Tiro III/IV é visualmente outro (mais projéteis ou área)
- [ ] E2 é mais denso que E1 sem cair abaixo de 30 FPS no Editor
- [ ] Build atravessa E1 → E2

---

### Sprint 11 — Espaço 3 + Boss espacial + evolução final + vitória

**Objetivo:** fechar o jogo.

**Jogador sente:** a fenda, o boss da frota, a forma final da habilidade se a run chegou em IV, tela de vitória com o herói escolhido.

**Construir**

- Cena `Space_E3`
- Boss espacial em 2 fases
- Evolução final P8 (opcional por run):
  - Guerreiro — Corte vira muralha de energia
  - Mago — Supernova Arcana
  - Anjo — Chuva de Penas
- Tela `Victory` (herói, kills, nível, tempo)
- Tela `Defeat` padronizada
- Créditos acessíveis pelo menu de verdade

**Boss espacial (mínimo)**

- Entra de cima, ocupa faixa larga
- Fase 1: rajadas + drones
- Fase 2 (50%): laser / coluna + movimento lateral
- Derrota encerra spawns e abre Victory

**Progressão liberada:** P8.

**Arte**

- [ARTE] boss espacial, fenda E3, tela de vitória

**Aceite**

- [ ] Run completa: seleção → T1 T2 T3 boss → naves E1 E2 E3 boss → vitória
- [ ] Os 3 heróis conseguem zerar em dificuldade normal
- [ ] Game over em qualquer fase volta ao menu sem corromper estado
- [ ] Evolução final só aparece se a habilidade chegou em IV

---

### Sprint 12 — Publicação para os colegas

**Objetivo:** link jogável.

**Construir**

- Módulo WebGL no Unity Hub
- Build WebGL 1920×1080, Canvas Scaler já usado
- Ajustes de input, resolução, memória, partículas
- Deploy Vercel / Netlify
- Teste em Chrome (Mac e Windows), um celular se der
- README curto de controles no próprio menu

**Aceite**

- [ ] Link abre sem Unity instalada
- [ ] Dá para escolher herói e jogar T1 pelo menos no link
- [ ] O ideal: zerar no navegador
- [ ] Outra pessoa testou e mandou print

---

## 9. Sprints de arte em paralelo

Estas sprints não empurram gameplay. Elas substituem placeholder.

| Sprint arte | Quando pode começar | Entrega |
|---|---|---|
| A1 Guerreiro | Já | `.glb` no select e em T1 |
| A2 Mago | Imagem do mago | Modelo + material glow |
| A3 Anjo | Imagem do anjo | Modelo + asas em blocos |
| A4 Inimigos terra | Imagens dos 3 primeiros | 3 `.glb` ou sprites 8-bit |
| A5 Boss terra | Imagem do boss T3 | Modelo da Sprint 7 |
| A6 Naves | Imagens das 3 naves | Antes ou durante Sprint 8 |
| A7 Inimigos espaço | Imagens | Durante 9–10 |
| A8 Boss espaço | Imagem | Sprint 11 |
| A9 Fundos | Imagens T1–E3 | Parallax por fase |

Como usar uma imagem nova:

1. Colocar em `Assets/Art/conceitos/<nome>.png`
2. Abrir o Blender pelo mesmo fluxo do Guerreiro
3. Blocos, paleta curta, emissão só no glow
4. Exportar `.glb` com o mesmo nome
5. Trocar o placeholder na Unity, sem mexer em gameplay

---

## 10. Critérios de aceite por personagem (visão final)

Uma run só é “completa por herói” se:

**Guerreiro**

- Anda mais lento, aguenta mais
- Corte cresce até muralha se a run for boa
- Nave pesada, escudo alto
- Carta de classe (Armadura do Véu) só nele

**Mago**

- Frágil, orbe automático
- Orbe escala até supernova
- Nave frágil e agressiva
- Carta Núcleo Arcano só nele

**Anjo**

- Dash desde a Sprint 3
- Penas escalam até chuva
- Nave mais rápida
- Carta Graça Alada só nele

Se no fim os três ainda “só atiram bolinha diferente”, as sprints 3, 6, 7 e 10 não estão fechadas.

---

## 11. Critérios de cenário

Cada fase precisa de identidade, mesmo com arte temporária.

| Fase | Chão / espaço | Paleta | Elementos mínimos |
|---|---|---|---|
| T1 | Ruínas laterais | Terra + roxo baixo | chão longo, plataformas de muro, céu e serras em camadas |
| T2 | Urbano morto | cinza + magenta | casas sem janela, partículas de cinza |
| T3 | Fissura | vermelho + ouro do Véu | rachadura central, arena mais fechada |
| E1 | Órbita | azul escuro + planeta | Aetherion ao fundo, estrelas lentas |
| E2 | Frota | verde-doente + ferro | silhuetas de naves grandes |
| E3 | Fenda | preto + amarelo vazio | rasgo vertical, menos estrelas |

Trocar de fase **tem** que mudar o fundo. Se T2 parecer T1, a sprint de fase não fecha.

---

## 12. Ordem do Cursor / Unity neste documento

Quando for implementar, dizer a sprint pelo número.

Exemplo: *“vamos fazer a Sprint 1”*.

O Cursor deve:

1. Ler esta sprint e só ela
2. Listar arquivos que vai criar
3. Não implementar a sprint seguinte
4. Fechar com o checklist de aceite
5. Atualizar o quadro da seção 13

---

## 13. Quadro vivo

Atualizar no fim de cada sprint.

| Sprint | Nome | Status | Data | Nota |
|---|---|---|---|---|
| 0 | Fundação | Feita | — | Menu, intro, select, Guerreiro 3D |
| 1 | Andar em T1 | Feita | | Virada para side-scroller Metal Slug: A/D, pulo, plataformas. |
| 2 | Combate Guerreiro | Feita | 2026-09-04 | Corte manual, bloqueio, barras de vida/escudo, fase lateral. |
| 3 | Kits dos 3 | Implementada — testar no Play | | Orbe auto, pena em leque, dash Shift, kits distintos. |
| Demo | T1 Guerreiro pixel | Sprite + anim no Play | 2026-09-07 | Cubo trocado pelo sheet 64×64. Idle/walk/jump/corte/block/hurt. |
| 4 | Ondas T1 | Aberta — depois da demo | | |
| 5 | XP e 3 cartas | Aberta | | |
| 6 | T2 + habilidade II | Aberta | | |
| 7 | T3 + boss terra | Aberta | | |
| 8 | Naves + conversão | Aberta | | |
| 9 | E1 | Aberta | | |
| 10 | E2 + habilidade III/IV | Aberta | | |
| 11 | E3 + boss espaço + vitória | Aberta | | |
| 12 | WebGL + Vercel | Aberta | | |

---

## 14. Dependências (não pular)

```
0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12
                 ↘ arte A2/A3 pode entrar quando a imagem chegar
```

Sprint 5 (cartas) precisa da 4 (ondas) para ter XP suficiente.  
Sprint 7 precisa da 6 para T2 existir.  
Sprint 8 precisa da 7 para a run chegar na nave com build.  
Sprint 12 pode publicar um recorte (só até T1) se 11 atrasar, mas o alvo é a run toda.

---

## 15. Definição de pronto de uma sprint

Uma sprint só é “pronta” quando:

1. Play na Unity demonstra todos os itens de aceite
2. Console sem erro vermelho no fluxo feliz
3. Os 3 heróis foram clicados pelo menos uma vez se a sprint mexe em personagem
4. Placeholder de arte está identificado (não parece asset final sem ser)
5. Este quadro (seção 13) foi atualizado
6. Nada da sprint seguinte foi misturado “porque era fácil”

---

## 16. Próximo passo imediato

Foco atual: **testar o Guerreiro pixel no Play** (modelo + animações).  
Modelo v4 — cavaleiro baixinho (2026-09-07): proporção chibi aprovada por referência (elmo grande ~metade do corpo, chifres de osso, visor em T brilhante, corpo curto com emblema V, pernas curtinhas). Escudo em vermelho escuro pra destacar do corpo; espada grande em proporção. 27px de altura, PPU 15. `Guerreiro.aseprite` remontado com tags.  
Polimento (2026-09-07 noite): corte do guerreiro trocado de quad amarelo para arco crescente em pixel art (mesma hitbox); bloqueio cancela o ataque na hora (fim da transição bugada) e ganhou aura azul pulsante + faíscas azuis ao absorver golpe.  
Fundo T1 (2026-09-08 v2): arte gótica em `Resources/T1/fundo.jpg` com parallax 0.88 (`T1Backdrop`) + tinta fria/escura (0.58, 0.63, 0.78) pra afastar o fundo e destacar o cavaleiro. Silhuetas procedurais removidas (poluíam); profundidade vem de 3 camadas de neblina em deriva senoidal (`T1Mist`: longe -32, meio -12, frente 25 na ordem de render). Plataformas com corpo de tijolo + saia de pedra + borda discreta + sombra. Chão em tijolos tileáveis (`T1Scenery.TiledBlock`); calçada azul-pedra. Brilhos `T1Glow`; chuva 55 gotas. Fallback procedural mantido.  
Pendência de arte: inimigos ainda são quads roxos (`EnemyData.Color`) — precisam de sprite pixel próprio.  
Sprint 4 (ondas) só depois de aprovar essa arte no T1.
