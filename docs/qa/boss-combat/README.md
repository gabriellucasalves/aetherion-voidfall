# Música e ataques de Nazor

- `boss-results.txt`: 145 verificações aprovadas, zero falhas, erros ou exceções. Inclui som após a fala, continuidade da música nas transformações e dano real de ondas/Eclipse contra herói sobre uma plataforma da arena.
- `audio-results.txt`: 208 verificações aprovadas, zero falhas. Regressão das trilhas, efeitos, especiais, supremos, pausa, volumes e retry.
- `sonic-wave.png`: segunda forma dispara leque na altura do herói na plataforma.
- `eclipse-warning.png`: terceira forma marca a plataforma e as colunas descendentes antes de causar dano.
- `form-*.png` e `intro-portuguese.png`: três/duas/uma barras e título simplificado, com legendas em português e gravações japonesas preservadas.
- `build-manifest.json`: SHA-256 e tamanho dos cinco arquivos do build WebGL testado.

Os testes executam no Unity 6000.5.9f1 aberto e restauram as preferências de progresso/volume após terminar. Nenhuma voz foi modificada. A música fornecida foi convertida integralmente para MP3 estéreo e importada pelo fluxo de áudio existente.

Build WebGL verificado em navegador Chromium com emulação iPhone, toque e viewport 844 × 390. Foram percorridos seleção do guerreiro, arena, entrada, retry e transformações nas duas novas formas, sem erros de console ou de execução. Capturas `mobile-new-*`. A instrumentação de navegação foi aplicada apenas à resposta HTML do servidor local, sem modificar o build publicado.
