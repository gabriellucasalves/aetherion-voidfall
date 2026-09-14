# Aetherion: Voidfall

Protótipo acadêmico de action platformer 16-bit dark fantasy.

Jogar agora (PC e celular): **https://aetherion-voidfall.vercel.app**

Repositório: **https://github.com/gabriellucasalves/aetherion-voidfall**

## Fluxo

Menu → história → escolha do herói → Terra 1 (ruínas de Aetherion).

## Controles (PC)

- A / D ou setas — andar
- Espaço / W — pular
- Mouse esquerdo ou J — atacar
- Mouse direito / K / S — escudo (guerreiro)
- Shift — dash
- ESC — pausa

No celular aparecem botões virtuais (analógico, pular, atacar, escudo, dash).

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

Docs: `Assets/Documentation/` (`CharacterPixelArtAudit.md`, `CharacterStyleGuide.md`, `MagoRedesignSpec.md`, `PixelArtTools.md`).

| Ferramenta | Como abrir |
| --- | --- |
| Template Aseprite | `Tools/Aseprite/CreateCharacterTemplate.lua` → no Aseprite: **File → Scripts** (copie o `.lua` para a pasta Scripts) |
| Import Tool | Unity: **Tools → Pixel Art → Import Tool** |
| Character Validator | Unity: **Tools → Pixel Art → Character Validator** |
| Character Comparison | Unity: **Tools → Pixel Art → Character Comparison** |
| Proportion Debug | Componente `CharacterProportionDebug` no herói (Scene View) |

Contrato de runtime (não mudar tamanho global à toa): **PPU 15**, pivot `(0.5, 3/64)`, **Point**, GO pixel `y = -0.5`. Referência estrutural: **Guerreiro**.

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
