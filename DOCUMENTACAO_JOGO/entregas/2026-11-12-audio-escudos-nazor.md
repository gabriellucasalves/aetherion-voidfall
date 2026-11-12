# Entrega — áudio, mobile, escudos e Nazor

Data: 12 de novembro de 2026. Repositório: https://github.com/gabriellucasalves/aetherion-voidfall. Branch: `codex/combate-hud-local`.

## Conteúdo salvo

- Trilha de abertura Infernal Loop e músicas das três fases, com efeitos exclusivos para heróis, inimigos e prólogo.
- Vozes japonesas fornecidas para os três supremos e as cinco situações do boss, com redução automática da música durante as falas. As gravações de apresentação que dizem o nome antigo foram mantidas por escolha do usuário.
- Nomes e textos japoneses dos supremos, com fonte incorporada e licença OFL.
- Controles de toque no mapa, retorno ao início após concluir a primeira fase e personagem maior no percurso.
- Defesa específica para os três heróis: escudo físico, campo rúnico e égide de cristal; dano consome o escudo antes da vida e apenas o excedente atravessa quando ele quebra.
- Nazor, o Devorador: três formas, artes e retratos próprios, vida de 1.000/1.500/2.200 e barras restantes 3 → 2 → 1. Encerramento definitivo na terceira forma, com diálogo, vitória e salvamento, sem quarta forma ou teleporte espacial.
- Articulações locais para asas, ombros, mandíbula, tentáculos, manto e halo, preservando as texturas fornecidas. Preparação, disparo, recuo, rugido, conjuração, investida e recuperação têm poses distintas.
- Combate mais ativo: raios com duas salvas e mira renovada, varredura de ida e volta, ondas sônicas, Chuva do Eclipse e investidas físicas com trajetória marcada e janela de contra-ataque.
- Build WebGL e configuração de hospedagem versionados; os cabeçalhos gzip de JavaScript, dados e WebAssembly ficam em `vercel.json`.

## Validação

- Última validação completa do boss: 91 verificações aprovadas, nenhuma falha. Cobre articulação das três formas, texturas, congelamento das articulações, preparação/disparo, investida e recuperação, vida, escudos, vozes, retry e vitória.
- Suíte de áudio: 206 verificações aprovadas; suíte de escudos: 56 aprovadas. Os relatórios preservados estão em `docs/qa/nazor-audio-mobile/`.
- Build Unity WebGL concluído com sucesso; as três formas foram conferidas em navegador, sem erros no console durante a verificação.
- Prévia capturada da versão jogável: `output/previews/nazor-animado.mp4`.
- Hashes SHA-256 e tamanhos dos arquivos publicados: `docs/qa/nazor-audio-mobile/webgl-sha256.json`.

## Reprodução

1. Abra o projeto com Unity 6000.5.9f1.
2. Execute `Aetherion > Validar três formas e vozes do boss` para verificar o encontro; os menus de áudio e escudos executam as outras suítes.
3. Gere o build pelo `WebGLBuilder` (ou solicite ao editor aberto com `Library/GameAudioBuild.request`).
4. Publique o diretório de saída `webgl/` no projeto Vercel `aetherion-voidfall`, usando a configuração de raiz `vercel.json`. Não é necessário compilar Unity nos servidores Vercel: o build já acompanha o repositório.

As pastas locais de recuperação do Unity, caches, credenciais, configurações pessoais e logs de automação não são parte da entrega.
