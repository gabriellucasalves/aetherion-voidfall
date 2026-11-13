# Trilha e combate de Nazor — 12/11/2026

## Música no momento do combate

A gravação enviada, `musica para quando o boss surgir.m4a`, foi convertida integralmente para `Assets/Resources/Audio/Music/boss_combat.mp3`, em estéreo, sem cortes ou alteração de velocidade. Duração aproximada: 149,88 segundos. O arquivo original do usuário permanece intacto.

Durante a entrada, a trilha da terceira fase fica reduzida sob a voz japonesa. Ao terminar a fala e liberar o combate, a nova música começa do início e entra em loop, com transição de volume. As falas das transformações reduzem a mesma faixa e restauram o volume depois, sem reiniciar a música. Derrota, pausa, saída e nova tentativa seguem os controles existentes; o retry também aguarda a fala curta para iniciar a trilha.

## Nome e barras

O título mostra apenas **NAZOR, O DESTRUIDOR DE MUNDOS**, sem complemento do Vazio, número de forma ou contagem textual. As barras continuam diminuindo visualmente: três, duas e uma. Aparência e gravações japonesas permanecem as atuais.

## Pressão no chão e nas plataformas

- Segunda forma: três salvas sônicas com três ondas em leque de ±14°. A mira considera a altura real do herói; acompanha no começo e fixa nos últimos 0,32 segundos antes de disparar. Avisos de 0,95 s na primeira salva e 0,70 s nas seguintes. As ondas seguem em linha reta após sair, permitindo esquiva e bloqueio.
- Terceira forma: três rajadas de cinco colunas do Eclipse, separadas por 2,3 unidades. Avisos de 1,05 s na superfície da plataforma ou no chão; projéteis surgem acima da arena e atravessam as alturas até o piso. Cada rajada renova o alvo; as marcas individuais ficam fixas após aparecer. O ciclo alterna chuva, três salvas de lasers, investida, tentáculos e janela de núcleo exposto.
- As investidas existentes foram preservadas. Os escudos continuam absorvendo os projéteis pelo mesmo caminho de dano. A colisão faz uma varredura entre a posição anterior e a atual para evitar atravessar o herói em FPS menor.

## Verificação

O teste do boss passou em **145 verificações**, sem erros ou exceções. Inclui dano real das duas formas contra o herói parado em uma plataforma da própria arena, aviso sem dano, três salvas/rajadas, limpeza de perigos, nome simplificado, trilha após a fala, continuidade nas transformações, retry e todo o encerramento. Resultados e capturas em `docs/qa/boss-combat/`.

A regressão geral de áudio passou em **208 verificações**, cobrindo música das três fases, armas, especiais, supremos, vozes, pausa, volumes, boss, retry e retorno ao menu. O build WebGL foi concluído com sucesso.

## Publicação confirmada

- Commit de código/build: `94e8ca05575f9173d3490594c7aeca3ef1a86fe4`, enviado à branch `codex/combate-hud-local` no GitHub.
- Vercel: `dpl_CzvaEtkZbejGAXhNRAAEaPqhFTLT`, produção, **READY**.
- Link: https://aetherion-voidfall-gules.vercel.app
- Cinco artefatos retornaram HTTP 200 com SHA-256 idêntico ao build local testado e MIME/gzip corretos. Registro em `docs/qa/boss-combat/production-verification.json`.
- Combate conferido no navegador mobile emulado 844 × 390, sem erros; abertura de produção sem instrumentação em desktop 1920 × 1080 também sem erros ou requisições falhas. Capturas em `docs/qa/boss-combat/`.
