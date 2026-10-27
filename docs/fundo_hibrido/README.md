# Fundo da Fase I — híbrido V3+V2 → Refino V4

Direção criativa: céu do Gate B.1 V3 (creme/dourado + lavanda) com o layout V2
(Parasol, Weeping Hero, ruínas/estátua, água Aether, Cidadela Celeste ao fundo).
O Refino V4 (`ESPECIFICACAO_ASEPRITE_FUNDO_V4.md`) substitui os placeholders de
céu/far/city por pintura nativa 1×. O retoque V4.1 (`RETOQUE_V4_1_NUVENS_MID.md`,
referência `REFERENCIA_IA_fundo_fase_refino_v4_1.jpg`) refaz só nuvens e mid
(`retocar_v4_1.lua` atualiza os `.aseprite` no lugar).

## Arquivos

- `REFERENCIA_IA_fundo_fase_hibrido_v3ceu_v2layout.png`, `REFERENCIA_IA_fundo_fase_refino_v4.jpg`:
  **referências de IA, só direção visual.** Não são sprites e não entram em `Assets/`.
- `ESPECIFICACAO_ASEPRITE_FUNDO_HIBRIDO.md`, `ESPECIFICACAO_ASEPRITE_FUNDO_V4.md`: contratos.
- Templates vazios: `Tools/Aseprite/fundo_hibrido_v3_v2/templates/`.
- Pinturas V4: `Tools/Aseprite/fundo_refino_v4/aseprite/`.
- No jogo: `Assets/Resources/T1_Hybrid/` (ver o README de lá).

## Estado

| Camada | Estado |
|---|---|
| sky 480×192 | nativo V4 |
| clouds_a..d | retoque V4.1 (sprites separados do céu) |
| far 512×130 | nativo V4 |
| city 640×176 | retoque V4.1 |
| props soltos | não criados (Weeping, Parasol, arco, estelas e cristal estão na city) |
| ground 512×64 | placeholder IA — sprint de plataformas da frente |
