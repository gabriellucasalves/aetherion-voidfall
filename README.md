# Aetherion: Voidfall

Protótipo acadêmico de action platformer 16-bit dark fantasy.

Jogar agora (PC e celular): **https://aetherion-voidfall-gules.vercel.app**

Repositório: **https://github.com/gabriellucasalves/aetherion-voidfall**

## Fluxo

Menu → história → escolha do herói → mapa → três fases → Nazor (três formas) → epílogo de quatro telas → agradecimentos e equipe.

## Controles (PC)

- A / D ou setas — andar
- Espaço / W — pular
- Mouse esquerdo ou J — atacar
- Mouse direito / K / S — defender (escudo do guerreiro, campo rúnico do mago, égide de cristal do arqueiro)
- L / Q — especial da classe
- E — especial supremo (barra de Ressonância cheia)
- Shift — dash
- ESC — pausa

No celular aparecem botões virtuais (analógico, pular, atacar, escudo, especial, dash). O botão SUPREMO só aparece quando a Ressonância enche.

No mapa, o celular tem setas para segurar e caminhar pela trilha, **ENTRAR** no destino e **MENU**. Soltar ou tirar o dedo da seta interrompe o movimento. Ao concluir ou rejogar a Fase 1, o herói retorna ao início do mapa sem perder as fases liberadas; os avatares do mapa usam escala de 3 pixels (50% maior que antes).

Validação no Unity: **Aetherion → Validar mapa mobile e retorno das fases**. O teste cobre toque, interrupções, replay, retorno, entrada nas fases e os três avatares; relatório em `/tmp/aetherion-worldmap-mobile-qa/results.txt`.

## Time

| Pessoa | GitHub | Papel |
| --- | --- | --- |
| Gabriel Lucas Alves da Silva | [gabriellucasalves](https://github.com/gabriellucasalves) | admin |
| Bianca Xavier de Oliveira | [bianca602](https://github.com/bianca602) | write |
| Samer Osama Mohammad Taleeb | [Samerzs](https://github.com/Samerzs) | write |
| Thiago Costa Renovato | [ThiagoRenovato](https://github.com/ThiagoRenovato) | write |
| Isabela Rosa Dos Santos Gontijo | [batmavis](https://github.com/batmavis) | write |
| Wesley Thiago Matias Xavier | [wesshub](https://github.com/wesshub) | write |

## Pixel art — tools (Etapas 2–10)

Docs: `Assets/Documentation/` (`CharacterPixelArtAudit.md`, `CharacterStyleGuide.md`, `MagoRedesignSpec.md`, `PixelArtTools.md`, `GuiaAsepriteCenariosNaves.md`).

| Ferramenta | Como abrir |
| --- | --- |
| Template Aseprite | `Tools/Aseprite/CreateCharacterTemplate.lua` → no Aseprite: **File → Scripts** (copie o `.lua` para a pasta Scripts) |
| Template de cenário | `Tools/Aseprite/CreateSceneryTemplate.lua` → parallax, props, plataformas, tileset e fundo de espaço (tamanhos e paleta do T1) |
| Template de nave | `Tools/Aseprite/CreateShipTemplate.lua` → células 32 / 64 / 128, 17 frames com tags |
| Exportar para o Unity | `Tools/Aseprite/ExportForUnity.lua` → PNG + JSON sem as camadas `GUIDE` |
| Import Tool | Unity: **Tools → Pixel Art → Import Tool** |
| Character Validator | Unity: **Tools → Pixel Art → Character Validator** |
| Character Comparison | Unity: **Tools → Pixel Art → Character Comparison** |
| Proportion Debug | Componente `CharacterProportionDebug` no herói (Scene View) |

Contrato de runtime (não mudar tamanho global à toa): **PPU 15**, pivot `(0.5, 3/64)`, **Point**, GO pixel `y = -0.5`. Referência estrutural: **Guerreiro**.

Cenários e naves: copie os `.lua` de `Tools/Aseprite/` para a pasta Scripts do Aseprite (**File → Scripts → Open Scripts Folder**, depois **Rescan**). O passo a passo (paleta do T1, onde salvar o PNG, PPU 16 no cenário e PPU 15 com pivô no centro na nave) está em `Assets/Documentation/GuiaAsepriteCenariosNaves.md`. Os três scripts novos também aceitam `--script-param` para rodar sem interface.

## Áudio

- Tela inicial: **Infernal Loop** (`Assets/Resources/Audio/Music/menu_infernal_loop.mp3`), em loop, com o mesmo controle de volume das fases.

- Fase I: **Ancient World** (`Assets/Resources/Audio/Music/stage_1.mp3`).
- Fase II: **Cathedral of Ashes** (`stage_2.mp3`).
- Fase III: **funeral bells** (`stage_3.mp3`).
- Boss: faixa instrumental sintetizada **provisória**, em loop de 32 segundos (`boss_placeholder.wav`). Para trocar pela composição final, substitua esse arquivo mantendo o nome e o `.meta`.
- Os efeitos são sintetizados por material e personagem e substituíveis em `Assets/Resources/Audio/Sfx/`. O nome do arquivo corresponde a `GameAudio.Cue`.
- Música e efeitos têm volumes independentes em **Configurações** e **Pausa**, salvos entre sessões. A pausa suspende as vozes; supremos reduzem a trilha a 20% do volume escolhido, sem alterar o tom ou a velocidade, e restauram o volume suavemente ao terminar (preservando o final da gravação); a apresentação do boss também reduz a música. Derrota, vitória, retry e saída da fase tratam o áudio automaticamente.
- Seleção: assinatura própria para Guerreiro (metal), Mago (cristais) e Arqueiro (corda). Arco: puxada separada do disparo, cancelada ao receber dano ou interromper a carga. Cavaleiro, golem, sentinela e tentáculo não compartilham mais o mesmo ataque; cada um dos sete inimigos tem feedback de impacto próprio. Supremos incluem canalização, a gravação completa enviada para cada personagem, liberação de poder e efeitos de impacto final. As vozes ficam em `Assets/Resources/Audio/Voices/{Warrior,Mage,Archer}Callout.mp3`, copiadas dos arquivos originais sem cortes. As antigas vozes sintéticas de impacto não se sobrepõem às gravações. Mago usa ressonância grave e sopro arcano, sem o sino agudo do protótipo anterior.
- Prólogo: seis ambiências em loop e acentos por painel (calma, presságio, ruptura, Vazio, memória da cidade, determinação). Avançar ou pular encerra os sons anteriores.
- Japonês: nomes e falas autorais em `SpecialJapanese.cs`; fonte japonesa Noto Sans JP (OFL, incluída junto da licença), reduzida para o jogo com `python3 Tools/Audio/prepare_japanese_font.py`. A seleção, o cut-in e o grito de impacto exibem os mesmos textos. As defesas mantêm os modelos atuais: o guerreiro ergue um escudo físico vermelho e dourado; o mago canaliza o campo rúnico; o arqueiro projeta uma égide de cristal. As poses usam os quadros atuais, com animação de elevação e reação ao impacto. Uma faixa dedicada `TrioAnimations/guerreiro/defesa.png`, se disponível, substitui as poses reutilizadas.
- `python3 Tools/Audio/cinematic_sfx.py` refaz os sons do mago, supremos e prólogo. `python3 Tools/Audio/generate_callouts.py` permanece disponível para regenerar as antigas falas sintéticas em `Audio/Sfx`, mas o jogo usa as gravações fornecidas em `Audio/Voices`; o gerador não as altera.
- `python3 Tools/Audio/character_sfx.py` regenera apenas os efeitos de personagens, inimigos e seleção, sem alterar música.
- `python3 Tools/Audio/generate_audio.py` regenera apenas os sons sintetizados e a faixa provisória do boss (requer NumPy); não altera as três músicas fornecidas.
- No Unity: **Aetherion → Validar áudio das fases e combate** executa o teste das três fases, ataques, pausa, volumes, boss, retry e retorno ao menu. Relatório: `/tmp/aetherion-audio-qa/result.txt`.

## Build WebGL (Unity 6)

No terminal, com o Editor fechado:

```bash
/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$(pwd)" \
  -executeMethod WebGLBuilder.Build \
  -logFile Logs/webgl-build.log
```

A pasta `webgl/` é o que o Vercel publica.

### Defesa dos três heróis

- Ao segurar a defesa, todo dano efetivo consome primeiro o escudo (guerreiro 110, mago 100, arqueiro 75). Apenas o excedente de um golpe que quebre o escudo chega à vida, sem aplicar a armadura duas vezes. Ataques, projéteis e perigos de boss passam pela mesma regra. Quedas no abismo continuam fatais.
- A defesa impede ataques/especiais enquanto estiver sendo segurada. Solte a defesa para regenerar após 1,35 s; a recuperação é de 22% da capacidade por segundo. Segurar não drena nem regenera a barra.
- Os três heróis têm barra de defesa e ícone mobile próprio. O campo do mago usa a barra compartilhada de absorção, sem o antigo vazamento de dano para a vida.
- Validação no editor: `Aetherion > Validar escudos dos três heróis`, ou criar `Library/HeroShieldQA.request`. Verifica toque, absorção, quebra, excedente, recuperação e identidade visual; relatório em `/tmp/aetherion-shield-qa/results.txt`.

### Nazor, o Devorador: três formas e encerramento

- Entrada inédita: `Audio/Voices/BossIntro.mp3`; tentativa novamente: `BossRetry.mp3`; transições: `BossForm2.mp3` e `BossForm3.mp3`; derrota definitiva: `BossDefeat.mp3`. São as cinco gravações fornecidas, sem cortes, com legendas em português em `BossDialogue.cs` e áudio japonês preservado. A trilha reduz durante as falas, e a duração dos painéis acompanha os áudios completos. Primeiro toque revela o texto; segundo toque pula a entrada/transição e encerra a voz.
- Cada forma precisa ser derrotada: 1.000 → 1.500 → 2.200 de vida, sem carregar dano excedente entre formas. Ataques causam 100% → 115% → 130% do dano base, com cadência e movimentação crescentes (108% → 120% → 132%). A primeira forma usa raios; a segunda prioriza rugidos sônicos; a terceira usa a Chuva do Eclipse. Tentáculos, invocações e janelas de núcleo exposto complementam os ciclos de combate.
- A primeira forma usa a arte original. A segunda e a terceira usam as artes fornecidas, preparadas como sprites transparentes em `Assets/Resources/Bosses/DevoradorDoVazio/nazor/form2.png` e `form3.png`. O voo, a respiração, a canalização e a queda animam os desenhos completos, sem voltar ao corpo antigo nos ataques ou na derrota.
- Somente a terceira forma pode morrer definitivamente. Ela cai, pronuncia as quatro falas finais, se desfaz e inicia o **epílogo**, salvando a conclusão da fase e do jogo. Após as quatro telas, são exibidos os agradecimentos completos. O antigo quarto corpo, o teleporte e o gancho para o espaço foram removidos do encerramento.
- Teste: **Aetherion → Validar três formas e vozes do boss**, ou `Library/BossLifecycleQA.request`. Relatório e capturas em `/tmp/aetherion-boss-qa/`.

- A barra do boss mostra apenas vidas restantes: **3 → 2 → 1**. A primeira linha é a forma atual; as linhas roxas representam as próximas. Cada barra vencida desaparece na transformação. Nome nos painéis, legendas e vitória: **Nazor, o Devorador** (ナゾール). As gravações fornecidas anteriormente continuam sendo arquivos originais, portanto ainda pronunciam o nome antigo.
- Segunda forma: **Rugido Sônico**, com canalização de 0,95 s e duas ondas baixas separadas por 0,95 s, saltáveis e bloqueáveis. Terceira forma: **Chuva do Eclipse**, com três marcas fixadas no chão por 1,05 s antes de cada rajada de projéteis. Sair das marcas evita os tiros; os escudos também absorvem o dano. Transformar, morrer ou tentar novamente cancela todos os perigos ativos. Efeitos próprios: `python3 Tools/Audio/nazor_sfx.py`.
- Sprites preparados pela ferramenta integrada de imagens com o prompt: “Remover somente o fundo escuro, preservar a identidade, pose, paleta, silhueta e detalhes pixel art da imagem fornecida, sem redesenho; gerar transparência real e mostrar todas as extremidades.” Uma extração separada foi aplicada a cada forma.

### Movimento e combate articulado de Nazor

- As três formas usam `NazorArticulation`: asas giram nas raízes, braços nos ombros, mandíbula abre nas poses de esforço, tentáculos ondulam com fases independentes e o manto/halo da terceira forma acompanham a canalização. A malha contínua mantém as texturas e evita recortes e emendas. As artes PNG não são repintadas; na primeira forma, os quadros originais continuam alimentando a animação. Atualização das articulações a 24 poses por segundo, com pausa/congelamento.
- Poses próprias para foco, disparo/recuo, rugido, conjuração, antecipação, investida, recuperação, transformação e derrota. O corpo de referência não é desenhado em duplicidade, e os colisores de combate permanecem estáveis durante as batidas das asas.
- Primeira forma: duas salvas consecutivas que renovam a mira; a mira acompanha no começo e fixa por 0,32 s antes do disparo. O ciclo alterna isso com tentáculos, investida, varredura de ida e volta e exposição do núcleo.
- Todas as formas usam uma investida física com trajetória fixada e aviso de 0,85 s, avanço de 0,70 s e janela de recuperação/núcleo exposto perto do chão por 0,75 s. Sair da trajetória ou defender evita o golpe; aproximar-se após a passagem permite contra-atacar. A segunda mantém os rugidos e a terceira a Chuva do Eclipse.
- O teste do boss verifica movimento local de asas/caudas, centro estável, texturas originais, poses de preparação/disparo, congelamento, duas salvas e deslocamento/recuperação da investida, além de vida 3→2→1, vozes e encerramento. Capturas `motion-form*-*.png` em `/tmp/aetherion-boss-qa/`.

Entrega consolidada de áudio, mobile, escudos e Nazor: [documentação e validação](DOCUMENTACAO_JOGO/entregas/2026-11-12-audio-escudos-nazor.md).

### Epílogo e agradecimentos

- Quatro telas com os textos aprovados: **O fim da escuridão**, **Um novo amanhecer**, **O renascimento** e **A esperança prevalece**. Ilustrações originais mantêm a arquitetura, a paleta e a direção artística da abertura; o último panorama recebe os três heróis animados do próprio jogo.
- Toque na imagem, botão **Continuar** ou Espaço: primeiro revela o parágrafo, depois avança. **Voltar** permite reler; **Pular** leva aos agradecimentos. As ambiências evoluem de calma para esperança.
- Créditos com o texto integral fornecido pela equipe, sete integrantes, Professor Weverson Garcia Medeiros e UNICEPLAC. Rolagem automática pausável; arrastar, roda do mouse ou setas permite ler manualmente. **Mapa** e **Menu** preservam o progresso salvo.
- As cinco gravações japonesas de Nazor têm legenda portuguesa: encontro, nova tentativa, segunda forma, terceira forma e derrota. As gravações continuam intactas, inclusive a pronúncia anterior do nome, conforme solicitado.
- Arte em `Assets/Resources/Menu/epilogue/`; prompts completos e modo da ferramenta integrada em `Tools/Art/epilogue-prompts.json`. Cenas `Epilogue` e `Credits` incluídas no build.
- O teste **Validar três formas e vozes do boss** agora percorre a derrota, as quatro páginas, os créditos, a rolagem e os retornos ao mapa/menu.
