# Aetherion: Voidfall — Plano de estudo

Documento de partida para criar, estudar e apresentar o jogo.

O plano operacional, com 6 fases e sprints detalhadas, está em `SPRINTS.md`. Este arquivo continua sendo o mapa curto de estudo.

---

## A pergunta principal: Unity ou Vercel?

**O jogo se desenvolve na Unity. O Vercel só hospeda a versão web.**

| Ferramenta | Função | Quando usar |
|---|---|---|
| **Unity** | Motor do jogo: cenas, personagens, combate, física, UI, pixel art, modelo 8-bit | Todos os dias de desenvolvimento |
| **Blender** | Modelos 3D em blocos (guerreiro, depois mago e anjo) | Arte 3D pontual |
| **Vercel / Netlify / GitHub Pages** | Coloca a build WebGL no ar e gera um link | Quando for apresentar e os colegas precisarem jogar |

O Vercel **não substitui** a Unity. Ele não tem cena, ScriptableObject, onda de inimigos nem WebGL pronto. Ele só recebe uma pasta exportada e entrega um endereço.

Fluxo certo para a apresentação:

```
Unity (criar o jogo)
    ↓
Build WebGL (File → Build Profiles → WebGL)
    ↓
Vercel / Netlify (publicar a pasta da build)
    ↓
Link público para os colegas jogarem no navegador
```

Não precisa instalar Unity no computador de ninguém. Eles abrem o link no Chrome.

---

## O que já está pronto

- [x] Projeto Unity 6 aberto e compilando
- [x] Boot → Menu
- [x] Introdução narrativa
- [x] Escolha de Guerreiro, Mago e Anjo
- [x] Modelo 8-bit do Guerreiro no Blender (`Assets/Art/Guerreiro8bit.blend` e `.glb`)

Próximo bloco real de jogo: **combate na superfície**.

---

## Como estudar (método)

1. Uma etapa por vez. Não pular para espaço, cartas lendárias ou WebGL enquanto a etapa atual não roda no Play.
2. Sempre testar na Unity antes de avançar.
3. Anotar o que quebrou no Console. Corrigir isso faz parte do estudo.
4. Arte 8-bit: blocos, paleta curta, sem realismo.
5. Quando a etapa funcionar, só então pensar em publicar.

---

## Trilha de estudo

### Semana 1 — Fundamentos (já em andamento)

- Entender cenas: Boot, MainMenu, Intro, CharacterSelection
- Entender `GameManager` e troca de cena
- Ver o Guerreiro no Blender em **Material Preview** (não Solid)

**Entrega:** menu + intro + escolha de herói no Play.

### Semana 2 — Combate terrestre

- Movimentação lateral tipo Metal Slug (A/D + pulo)
- Ataque automático por classe (Corte, Orbe, Pena)
- 3 inimigos: básico, veloz, resistente
- Vida, dano, morte

**Estudar:** `Transform`, `Rigidbody2D`, colliders, `ScriptableObject` de inimigo.

**Entrega:** uma arena jogável com o herói escolhido.

### Semana 3 — Ondas e progressão

- Spawner e lista de ondas
- XP e level up
- 3 cartas: dano, vida, velocidade
- HUD: vida, nível, XP, onda

**Estudar:** UI no Canvas, pausar o jogo, dados configuráveis.

**Entrega:** sobreviver algumas ondas e escolher upgrade.

### Semana 4 — Chefe, nave e espaço

- Mini-chefe da fase 1
- Transição para a nave
- Shoot'em up curto com 2 inimigos
- Tela de vitória
- Preservar o herói e os upgrades entre as fases

**Entrega:** MVP completo do `prompt.md`.

### Semana 5 — Apresentação jogável

- Build WebGL na Unity
- Publicar no Vercel (ou Netlify)
- Testar no Chrome, no celular e em outro Mac
- Ensaio de 5 minutos: história, 3 heróis, demonstração ao vivo

**Entrega:** link + fala da apresentação.

---

## Arte 8-bit no fluxo

1. Conceito em pixel art (como o Guerreiro)
2. Modelo em blocos no Blender
3. Exportar `.glb`
4. Importar em `Assets/Art`
5. Usar na seleção de herói e, depois, no gameplay se couber

Ordem sugerida dos modelos: Guerreiro (feito) → Mago → Anjo.

---

## Como os colegas vão jogar

Opção recomendada para o trabalho:

1. Na Unity: instale o módulo **WebGL** no Hub, se ainda não tiver
2. `File → Build Profiles → WebGL → Build`
3. A pasta gerada vai para o Vercel (`vercel` no terminal, ou arrastar no site)
4. Manda o link no grupo da turma

Plano B, se a build WebGL atrasar no dia: build **Mac/Windows** e abrir no seu notebook na hora da apresentação. O link web continua sendo o objetivo, porque qualquer um joga sem instalar nada.

Não faça o jogo “dentro do Vercel”. Faça na Unity e só publique o resultado.

---

## Checklist da apresentação

- [ ] MVP rodando no Play
- [ ] Guerreiro, Mago e Anjo jogáveis
- [ ] Pelo menos uma fase com ondas
- [ ] Build WebGL abre no Chrome
- [ ] Link testado por outra pessoa
- [ ] História contada em menos de 1 minuto

---

## Regra de ouro

Unity cria. Vercel mostra. Blender veste.

Se faltar tempo, corta conteúdo da fase espacial. Não corta o link jogável.
