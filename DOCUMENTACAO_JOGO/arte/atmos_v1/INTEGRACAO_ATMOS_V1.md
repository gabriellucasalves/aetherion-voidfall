# Atmos V1 — integração Unity

Branch codex/atmos-v1, base art/cinematic-v3-far-city (65e19e8). Não foi feito merge em main. Os arquivos de arte não foram redesenhados, retocados, filtrados ou reexportados: os 24 arquivos originais do tar estão conferidos em pack-original-sha256.json.

## Decisão de layout da Direção

Resposta humana durante a execução: **72 unidades, 2 gaps do pack no início e chão contínuo no restante**.

O trecho original ocupa X=-36..-12. Os gaps físicos são -28,5..-26 (2,5 u) e -21,625..-18,5 (3,125 u). Três segmentos de chão, com Ground R estendido até X=36 por cópias nativas de ground.png; nenhum gap adicional. Plataformas A/B/C seguem exatamente as coordenadas de LAYOUT_GAPS.md. Seus visuais vêm do layer_p5 original, que já contém as três plataformas nas dimensões aprovadas. Os plat_s/m/l independentes foram preservados e importados, sem redimensioná-los para forçar correspondência.

P5 tem BoxCollider2D apenas nas superfícies sólidas e plataformas one-way. A zona de queda reutiliza T1FallZone/HealthSystem.Kill e o painel real VOCÊ CAIU. Sem chão contínuo legado escondido nos gaps. Portal e waves permanecem funcionais no chão estendido.

## Importação e camadas

Todos os 18 PNGs: Sprite, PPU 32, Point, Compression None, mipmaps off, Full Rect. Clamp nas camadas; Repeat horizontal no ground. Pivot central para planos e overlays, base para ground e topo para plat_s/m/l nos importadores. Personagens conservam seus sprites e PPU 16.

Planos: P1=0,08/-50; P2=0,22/-40; fog=0,22/-35; P3=0,40/-30; veil=0,40/-25; P4=0,62/-20; P5=1/0. O texto de PARALLAX usa terminologia ambígua ao dizer fração de câmera e P5 acompanha o mundo. Para manter P5 fixo junto aos colisores, factor foi tratado como velocidade aparente do plano: compensação de câmera = 1-factor. P1 mantém cobertura vertical acompanhando Y da câmera; P2-P4 têm parallax vertical. Fog/veil seguem os mesmos fatores dos seus planos.

P1-P4 e overlays têm três cópias de 24 u, recicladas fora da visão. As cópias vizinhas são espelhadas alternadamente para encostar a mesma borda do PNG, eliminando emendas sem retocar arte. O índice global de espelhamento é preservado na reciclagem; o trecho de referência permanece com a orientação original. O QA compara pixels reais das bordas antes e depois da reciclagem. Câmera e camadas são alinhadas a 1/32 u apenas no modo Atmos. O repouso da câmera fica 5,5 u acima do chão, colocando a superfície na linha Y=392 do preview. GroundTop é alinhado à grade (-2,34375). PixelPresentation foi preservado byte a byte: RT 768×432 Point, OrthoSize 6,75; inclusive o Fit e suas barras existentes em Full HD.

## Ajustes funcionais necessários

O salto antigo de 13,2 alcançava aproximadamente 2,72 u, abaixo da plataforma A (2,875 u). No Atmos, a velocidade de salto é 15,6 (altura ideal 3,80 u), permitindo A e a travessia A→B sem mover as plataformas. O modo legado mantém 13,2. A navegação dos inimigos usa alcance aéreo adequado ao novo espaçamento; inimigos completam o salto antes de iniciar outro ataque. O detector de apoio ignora lateral e face inferior de plataformas one-way, evitando apoio falso. Não foram redesenhados o Guerreiro ou inimigos.

## Flags e fallback

Atmos é o padrão (DefaultEnabled=true). Editor: Aetherion → Atmos V1 → Usar pack Atmos V1. WebGL: ?fundo=atmos, ?fundo=hibrido e ?fundo=classico. AllowLegacyFallback=false: recuperação automática pelo cenário antigo desativada, conforme pedido posterior da Direção. GoldenAge/T1 só é construído quando o Atmos está desativado ou em fallback; não há mistura de raízes ou colisores.

## Verificação local executada

Unity 6000.5.9f1: 46 checks Atmos e 46 do legado passaram, sem erros C#. Os 46 verificam todos os importadores, separação de planos, cobertura/snap, dois gaps realmente mortais com UI, apoio dos pés, saltos para A/B/C e navegação dos três inimigos até C. Também verificam a extensão do chão e rejeição de spawn em gaps. Scripts: AtmosV1Validation.Run e F1Validation.Run. Os testes são excluídos do WebGL por UNITY_EDITOR.

Capturas locais em qa/inicio.png, gap_1.png, plat_A.png, plat_B.png e plat_C.png: RenderTexture da câmera real em Play Mode, com pose de câmera de referência para comparar o pack, sem o Canvas do HUD. A imagem sem personagens foi comparada a preview.png; métricas em qa/preview-comparison.json, erro médio RGB aproximadamente 0,18/255. Não são mockups. As capturas de A/B/C vêm de saltos físicos do corpo do jogador.

Uma rodada ampliada detectou o ranged interrompendo o salto para atirar; foi corrigido e os 46 checks passaram. Uma tentativa de regressão com -nographics sofreu crash nativo em DrawSharedGeometryJobs; a execução com backend gráfico normal passou. O Editor também registra a exceção de inicialização já observada na base; não apresentar o Console do Editor como inteiramente limpo.

Build WebGL pelo WebGLBuilder.Build do Feixe, com wrapper que deixa a flag do Editor em Atmos. Sucesso: 18.520.754 bytes. Sem serviços externos de produção/edição de arte. Publicado e verificado conforme registro abaixo.

## Publicação verificada — 05/11/2026

- URL pública: https://aetherion-voidfall-gules.vercel.app/
- Conta pessoal: gabriellucassilva81-8983; projeto aetherion-voidfall, ID prj_BjRWPsfINvsxLR7ixioA1AVhrbWM. A conta Hobby antiga não foi usada.
- Build do commit 9ebd2e1, deployment dpl_HK4RaHSnHZ4MHUnLc4WyEDsZ7sxF, Ready e promovido após testes.
- Deployment imutável: https://aetherion-voidfall-qa9fsmkpp-gabriellucassilva81-8983.vercel.app/
- Os quatro arquivos WebGL publicados responderam HTTP 200 e SHA-256 idêntico ao build local, tanto no staging quanto no domínio público; prova em qa/production-hashes.json.
- Percurso no navegador com teclado normal: menu → introdução → seleção → Ground_T1 → A → B → C. Capturas reais com HUD em qa/webgl-inicio.png e qa/webgl-plat-A/B/C.png. A execução foi pausada somente após as capturas.
- Queda real sem salto no primeiro gap e painel VOCÊ CAIU: qa/webgl-gap-1.png e qa/webgl-voce-caiu.png. Os dois gaps também passaram no Play Mode.
- Domínio público novamente aberto pelo menu até Ground_T1: qa/webgl-producao-inicio.png; console do navegador sem entradas de erro em qa/webgl-producao-console.txt.
- O Fit/barras e o HUD que excede a margem esquerda em 1920×1200 já pertencem à apresentação base; não foram alterados neste escopo. O Console do Editor tem a ressalva de inicialização descrita acima.
- A nitidez e continuidade foram inspecionadas nas capturas e movimento; homologação artística final continua com a Direção.

Lista e estatística de arquivos: diff-arquivos.txt. Diff textual da implementação e assets: integracao.patch (binários identificados pelo Git, sem payload binário). Nenhum merge em main foi realizado.

## Conferência posterior — pack ativo, fallback off

DefaultEnabled=true e AllowLegacyFallback=false. Os 18 PNGs conferidos como Sprite, Point, PPU 32, sem mipmaps e sem compressão. Nova execução real em Play Mode passou nos 46 checks. Captura do início sem personagens e sem HUD: qa/fallback-off/inicio-sem-personagem.png; RT nativa 768×432. Comparação contra preview.png: erro RGB médio 0,19/0,17/0,18 em 255. Assets originais preservados. Menu Aetherion → Atmos V1 → Abrir início em Play Mode habilita explicitamente a preferência do Editor e abre Ground_T1. Após esta conferência, a Direção solicitou novo WebGL e deploy pessoal. Build reconstruído de Aetherion_Atmos_V1 com fallback automático desligado; publicação registrada no bloco seguinte.
