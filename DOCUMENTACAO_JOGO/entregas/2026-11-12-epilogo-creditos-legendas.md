# Epílogo, créditos e legendas — 12/11/2026

## Experiência final

A derrota da terceira forma de Nazor termina sua gravação japonesa antes da dissolução. O jogo salva a conclusão e carrega `Epilogue`, sem quarta forma ou gancho espacial. As quatro páginas usam os títulos e os parágrafos enviados pela equipe, com transição de imagem, revelação gradual, voltar e avançar. O primeiro toque revela o texto inteiro; o seguinte avança. A última página segue para `Credits`; pular também leva aos agradecimentos.

Os créditos preservam integralmente o texto aprovado, os sete integrantes, o Professor Weverson Garcia Medeiros, a UNICEPLAC e a mensagem final. A leitura tem rolagem automática pausável, arraste, roda e setas. Mapa e menu mantêm o progresso concluído.

## Arte e áudio

Quatro ilustrações originais em `Assets/Resources/Menu/epilogue/`, geradas com a ferramenta integrada `image_gen` a partir da arquitetura e da direção artística das imagens de abertura. Os originais foram copiados para o projeto e importados com Point, sem mipmaps e sem compressão destrutiva. Prompts completos em `Tools/Art/epilogue-prompts.json`. O panorama final recebe os sprites animados atuais dos três heróis.

As ambiências existentes evoluem de calma a memória e determinação, respeitando o volume salvo. A fanfarra dos créditos usa o canal cinematográfico; as músicas têm transição suave. As cinco falas do boss agora exibem legendas em português com fonte ampliada: primeiro encontro, tentativa novamente, segunda forma, terceira forma e derrota. Nenhuma gravação foi substituída ou cortada; a pronúncia antiga do nome permanece conforme a preferência registrada.

## Implementação e verificação

`EpilogueController`, `CreditsController` e `CreditsContent` constroem a interface com o kit visual existente. As novas cenas estão habilitadas no build. `BossDialogue` mantém os originais japoneses e suas traduções portuguesas.

`BossLifecycleSmoke` percorre combate e transformação das três formas, voz original, redução de música, derrota completa, quatro páginas, arte distinta, tamanho dos textos, nomes, rolagem automática/manual, FIM, mapa, menu e nova tentativa. Resultados finais e capturas ficam em `docs/qa/ending/` após a validação. O teste restaura as preferências de progresso que existiam antes da execução.

Resultado: **131 verificações aprovadas, zero falhas ou erros de execução**. Build WebGL concluído; fluxo mobile por toque validado em 844 × 390, incluindo arraste dos créditos e retorno ao menu, sem erros de console. Manifesto SHA-256 dos cinco artefatos em `docs/qa/ending/build-manifest.json`.
