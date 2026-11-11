# PROMPT PARA O GEMINI FLASH — Integrar arte nova das Fases 1 e 2 + frente nova da Fase 3 (Aetherion: Voidfall)

Cole tudo abaixo no Flash, junto com a pasta `fases_123_final_v1/`, que tem estas subpastas:
- `fase1/`, `fase2/`, `fase3/`: cada uma com o seu `MANIFEST.md`.
- `previews/`: só referência.

---

Você vai **integrar arte pronta** no projeto Unity **Aetherion: Voidfall**. A arte está na pasta `fases_123_final_v1/`.

## Regras gerais (obrigatórias)
1. **Você NÃO desenha nada.**
   - Não gere, não edite e não redimensione nenhum PNG.
   - Não crie sprites procedurais novos para substituir os do pacote.
   - Seu trabalho é só **importar**, **fatiar** (os FX) e **trocar referências**.
2. Siga **exatamente** os `MANIFEST.md` de cada fase. Eles trazem:
   - o import: PPU 32, filtro Point, sem compressão, sem mipmaps e Repeat só onde está marcado;
   - a paralaxe e o sorting;
   - a linha do chão y=372;
   - os topos de colisor;
   - os pivôs e o fps dos FX;
   - os caminhos de Resources.
3. Onde estiver escrito **"confirme no projeto"**, abra o código e confirme o nome real da pasta ou do script antes de mexer. Se for diferente, use o nome real e registre isso no relatório.
4. **Não mexa** nestes itens:
   - na lógica, nos colisores e na arte de fundo da **Fase 3**;
   - no boss, nos inimigos, no portão e nos FX da Fase 3 (só a frente da Fase 3 troca, veja a seção C);
   - no **HUD do topo**, nos supremos, nos especiais, nos stats dos heróis e na lógica de boss e inimigos;
   - no **mapa-múndi**;
   - nos sprites dos heróis.
5. Proibido:
   - `git commit`, `git push` ou force-push;
   - deploy na Vercel ou build WebGL;
   - rodar o Unity por CLI (`-batchmode` / `-executeMethod`), porque isso já causou Safe Mode;
   - commitar `mapa_assets_v1.zip`.

   Quem abre o Unity é o Gabriel, pelo **Unity Hub**.
6. Não apague os assets antigos. Crie as pastas `v3/` ao lado deles. A única exceção é a Fase 3, cujos arquivos são **sobrescritos** (troca drop-in).

## A) FASE 1 — Aetherion intacta (`fase1/`)
1. Copie `fase1/{camadas,plataformas,tiles,props,fx}` para `Assets/Resources/<pasta de Resources atual da Fase 1>/v3/...`.
   - Confirme o nome da pasta no builder de cenário da Fase 1 (provavelmente `T1AtmosScenery.cs` ou similar; confirme no projeto).
   - Aplique o import do MANIFEST.
2. No builder de cenário da Fase 1, **troque** as camadas, as plataformas, os tiles (chão, subsolo, bordas e abismo) e os props para os sprites `v3/`.
   - **Mantenha** o layout: as posições e larguras das plataformas, os vãos, os colisores do chão, os spawns, os hazards, o portal e toda a gameplay.
   - **Ajuste só o topo do colisor** de cada plataforma para o `y` da tabela do MANIFEST. O colisor é one-way, com 6 px de altura e a largura indicada.
   - O chão (`chao_grama_128`) tem a **linha caminhável em y=6**: o topo do PNG fica 6 px acima do GroundTop e o colisor continua no GroundTop.
   - As bordas têm a linha caminhável em y=7 (dir) e y=8 (esq) e sobrepõem 4 px no vão.
3. **Remova os visuais antigos** procedurais e placeholder da Fase 1: retângulos, gradientes gerados por código e sprites de teste que o novo cenário substitui.
4. FX de ambiente, só visual (sem colisão e sem dano):
   - **pétalas** (`fx_petalas_4f_32x32`, 8 fps em loop);
   - **motes de Aether** (`fx_motes_aether_4f_32x32`, 6 fps em loop);
   - a quantidade, o pivô e o sorting estão no MANIFEST.

## B) FASE 2 — Aetherion em destruição (`fase2/`)
1. Copie `fase2/{camadas,plataformas,tiles,props,fx}` para `Assets/Resources/T2_Palace_Atmos/v3/...` (confirme no projeto) e aplique o import.
2. No builder de cenário da Fase 2 (confirme o nome do script), troque as camadas, as plataformas, os tiles e os props para `v3/`.
   - Mantenha o layout, os vãos, os colisores, os spawns, os hazards, o portal e a gameplay.
   - Ajuste o topo do colisor das plataformas conforme a tabela: y=1, 2 ou 3, com 6 px de altura.
   - O chão `chao_palacio_128` é caminhável em y=0, com o topo do PNG no GroundTop.
3. Remova os visuais antigos procedurais e placeholder da Fase 2.
4. FX de ambiente, só visual:
   - **Meteoros no fundo** (`fx_meteoro_queda_8f_48x48`, 14 fps):
     - surgem de cima e de fora da tela e caem na diagonal, 1 a cada 1,5–3 s;
     - sorting -25.
   - **Explosão de impacto ocasional no chão** (`fx_impacto_explosao_6f_64x64`, 14 fps, toca uma vez):
     - pivô base-centro no GroundTop;
     - **puramente visual**, **sem dano**, a não ser que já exista um hazard naquele ponto;
     - um leve camera shake é opcional;
     - fica atrás das plataformas (sorting -8).
   - **Fumaça** (`fx_fumaca_6f_32x32`, 10 fps): depois da explosão e nos braseiros, atrás das plataformas.
   - **Brasas** subindo (`fx_brasas_4f_32x32`, 8 fps em loop).
   - Use pooling simples e um limite de instâncias, sem afetar a performance.

## C) FASE 3 — FRENTE NOVA v2 (`fase3/`) — troca de sprites + ajuste de pivô das plataformas
1. **Sobrescreva** os PNGs mantendo os `.meta`/GUIDs existentes:
   - `fase3/plataformas/*` vai para `Assets/Resources/T3_Ruins_Atmos/v2/`: as 3 plataformas flutuantes, chao_tile, 2 pontas, 2 bordas, o capitel e a estátua.
   - `fase3/props/*` vai para `Assets/Resources/T3_Ruins_Atmos/v3/props/`.
   - `fase3/tiles/*` vai para `Assets/Resources/T3_Ruins_Atmos/v3/tiles/`.

   Confirme esses caminhos no projeto. Reaplique o import do MANIFEST: Repeat X no chao_tile e no abismo, Repeat X/Y no subsolo, Clamp no resto.
2. **As 3 plataformas flutuantes ficaram mais altas**, porque o corpo cresceu só para baixo. Os nomes `_x48` continuam iguais.
   - Os tamanhos novos são 96×60, 128×79 e 160×100 (antes eram todas de 48 px de altura).
   - O topo caminhável fica no mesmo lugar, com a mesma largura.
   - **Você precisa ajustar:**
     - o pivô para **topo-centro (0,5; 1,0)**. Se o projeto usava pivô Center, use pivô Y = 1 − 24/H, ou seja 0,600 / 0,6962 / 0,760;
     - o colisor one-way: 92/124/156 × 8 px (0,25 u), com a borda de cima no topo do sprite;
     - qualquer código que derive o colisor de `sprite.bounds` (altura ou centro), que deve passar a usar `bounds.max.y` com altura fixa.

     Detalhes no `fase3/MANIFEST.md`.
   - **Verifique** no validador que o topo do colisor de cada plataforma da Fase 3 ficou **na mesma coordenada Y do mundo** de antes (tolerância de 1 px).
3. Todo o resto da Fase 3 mantém o tamanho em px, os topos de colisor e a linha caminhável. **Nenhuma outra mudança** de código, colisor ou layout.
4. **NÃO toque** em nada disto:
   - nas camadas de fundo da Fase 3;
   - no boss, nos inimigos e no `portao_arena_2f_164x100`;
   - nos FX da Fase 3;
   - na lógica da Fase 3.

## D) Validação (o Gabriel roda pelo Unity Hub)
1. Use o menu **Aetherion > Validar Combate HUD e Fase 3**. Se for preciso, **adicione casos para a Fase 1 e a Fase 2** nesse mesmo validador, sem quebrar os casos existentes. Cada caso deve conferir:
   - que os sprites `v3/` carregam (nenhum null);
   - que os topos de colisor batem com o MANIFEST;
   - que o jogador anda no chão e pousa em cada plataforma;
   - que nenhum objeto procedural antigo sobrou;
   - que os FX são só visuais (não causam dano).
2. Prints, salvos em `/tmp/aetherion-combate-hud-local/`:
   - `fase1_estilo3_caminho.png`: caminho da Fase 1 com chão, props e FX;
   - `fase1_estilo3_plataformas.png`: o herói sobre uma plataforma da Fase 1;
   - `fase2_estilo3_caminho.png`: caminho da Fase 2;
   - `fase2_estilo3_meteoros.png`: meteoros no fundo + uma explosão de impacto;
   - `fase3_frente_nova.png`: arena da Fase 3 com a frente nova v2 e o herói em pé sobre uma plataforma flutuante (é a regressão da Fase 3).
3. **Relate só os prints que realmente existem** no disco. Liste-os com o tamanho em bytes. **Não invente nem descreva prints que você não gerou.** Se algum falhar, diga qual e por quê.

## E) Checklist de regressão (responda item por item)
- [ ] Todos os testes e validações existentes: **PASS** (liste-os).
- [ ] Fase 3 sem mudança de lógica, boss, inimigos, portão ou fundo. Só os sprites da frente foram trocados, e o pivô e o colisor das 3 plataformas foram ajustados: topo no mesmo Y do mundo (informe os valores antes e depois).
- [ ] HUD do topo, supremos, especiais e stats dos heróis sem alteração.
- [ ] Mapa-múndi sem alteração.
- [ ] Layout, spawns, hazards e portal das Fases 1 e 2 iguais aos de antes.
- [ ] Nenhum git commit, push, deploy ou WebGL feito.
- [ ] Lista de arquivos alterados e criados (paths).
- [ ] Tudo o que foi "confirmado no projeto": os nomes reais das pastas e dos scripts e os fatores de paralaxe usados.

Observação: o **contorno (rim) de 1 px do Mago NÃO está neste pacote**, porque os sprites dos heróis estão fora do escopo. Não mexa nos heróis.
