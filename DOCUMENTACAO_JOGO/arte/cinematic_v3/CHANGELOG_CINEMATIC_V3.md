# CHANGELOG — T1_Hybrid_Cinematic_V3 (EXPERIMENTAL · MOCKUP / NÃO É UNITY)
03/11/2026 (BRT). Base: ORIGINAIS de /workspace/aeth_mock/t1h_live/ (não V2). Nada integrado, sem commit/push/deploy.
Reproduzir: `python3 scripts/build_v3.py && python3 scripts/build_sculptures_previews.py`. Só nearest-neighbour; sem blur, sem filtro global, sem downscale automático.

## far.png (512×130) — plano 2
- Cidadela grande (x150–301, y0–91; ilha destacada, sem contato com montanhas) apagada → montanhas abaixo já eram contínuas (começam em y≥98), sem buraco/costura.
- Nova Cidadela redesenhada pixel a pixel 52×34 (~40%), 8 torres+agulhas, muralha, portão/janelas dourados suaves, ventre em lente com fade dither; em x344–395, y38–71, terço direito, ventre tocando o pico em x≈360. Também salva isolada: `far_citadel_small_1x.png`.
- Paleta das montanhas remapeada teal → azul-acinzentado/lavanda (7 cores mapeadas 1:1).
- Luz direcional por inclinação: encostas voltadas à esquerda recebem creme dither + rim light na crista; encostas à direita, sombra lavanda dither (14 px de profundidade). Clusters de rocha roxa preservados.
- 4 faixas atmosféricas quantizadas por altura rumo a HAZE (196,192,216): 30/22/12/4%.
- Névoa dither curta de 6 px (y108–113, densidade ≤32%), substituindo a faixa dura de 12 px do V2.

## city.png (640×176) — planos 3/4
- Luz por borda: borda alfa esquerda/topo → creme-dourado (1 px 38%, 2º px dither 20%); direita/baixo → lavanda-azul (32%/18%). Verdes e faixa de chão (y≥118) intactos.
- Elementos de fundo (colunata x65–115, arco x292–336): −22% contraste/sat rumo à névoa.
- Sombra de contato dither em y111–114 (onde estrutura encontra o chão, dentro do sprite).
- Árvores roxas próximas mantêm a cor; só ganharam rim de luz/sombra.

## Sem mudança (cópias)
sky, clouds_a–d, ground, plat_m. Luz no terreno existe SÓ no stack de preview (rim quente na linha de topo do ground), sem alterar o arquivo.

## Esculturas (propostas, NÃO gravadas no city)
sculptures/escultura_guardiao.png 20×48 · escultura_monolito.png 16×40 (núcleo Aether ciano) · escultura_fragmento.png 28×30 (pilar quebrado + tambor caído + musgo). Mapa: preview/sculpture_position_map_x2.png.

## Precisa de refino no Aseprite antes de integrar
1. Cidadela pequena: AA manual das agulhas, variar alturas, ventre com mais textura de rocha.
2. Luz da city: o rim é algorítmico por borda alfa — pode marcar contorno demais em folhas finas e no espelho do lago; repintar copas à mão.
3. As 3 esculturas são blockouts de 1ª passada: precisam de anatomia/desgaste/AA reais para chegar ao nível 16-bit dos props atuais.
4. Seam de tiling do far (x0/x511): luz por inclinação calculada com wrap, mas conferir no loop.

## Riscos de integração
- O stack é um mock aproximado ≠ câmera/paralaxe da Unity; offsets são estimados.
- O sky original ainda tem faixa teal/ciano no horizonte; montanhas agora frias-lavanda podem destoar ali (risco teal invertido). Não mexi no sky por regra.
- Cidadela a 40% fica legível a 1×, mas mínima; em câmera com zoom-out pode virar "mancha" — testar.
- Props no playfield competem com leitura de gaps/plats → só proposta.
- Paralaxe, colisores e arquivos Unity intocados.
