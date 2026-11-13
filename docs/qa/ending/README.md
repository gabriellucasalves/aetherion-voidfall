# Encerramento e legendas

Validação Unity 6000.5.9f1: **131 verificações aprovadas, zero falhas**, executando `BossLifecycleSmoke` no editor aberto. O teste percorre a entrada de Nazor, três formas e ataques, áudios, pausa, escudo, derrota definitiva, epílogo de quatro páginas, créditos integrais, rolagem e retornos. O progresso anterior é restaurado ao terminar.

`results.txt` contém o resultado por verificação. As imagens `intro-portuguese` e `defeat-portuguese` mostram as legendas; `epilogue-1` a `epilogue-4` mostram as quatro narrativas; `credits-start`, `credits-team` e `credits-end` mostram a leitura dos agradecimentos.

Os nomes, os parágrafos e a ordem dos créditos são comparados com `CreditsContent`. As quatro páginas verificam conteúdo, arte distinta, altura de texto e presença dos heróis no último panorama. A gravação japonesa final é reproduzida até o fim antes da troca de cena.

Build WebGL verificado em Chromium com emulação mobile iPhone, viewport 844 × 390 e toque. As quatro páginas avançaram pela área da ilustração; o último botão abriu os créditos. Pausar e arrastar alteraram a leitura; FIM e retorno ao menu foram capturados. Zero erros de console ou exceções no percurso. Somente a resposta HTML do servidor local de teste expôs a instância Unity para navegação de QA; os arquivos publicados não incluem essa instrumentação.

A configuração central `AetherionProjectSetup` inclui as duas novas cenas e mantém seu registro após recarregar scripts. A validação verifica a cena realmente ativa e falha diante de qualquer erro/exception do Unity.
