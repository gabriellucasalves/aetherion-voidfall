# Style guide de pixel art — heróis (sensação 16-bit)

**Molde estrutural:** Guerreiro (`GuerreiroVisual` + `Resources/Guerreiro/sheet.png`).  
**Identidade:** cada herói mantém paleta, arma e leitura própria.  
**Auditoria de sensação:** `CharacterPixelArt16BitAudit.md`.  
**Medição antiga (chibi, PPU 15):** `CharacterPixelArtAudit.md` — histórico, não é mais o alvo.  
**Não redesenha sprites sozinho.** Este arquivo trava o contrato do próximo desenho.

Referência de densidade: Mega Man X, Super Castlevania IV, Demon’s Crest, Contra III. Metal Slug fica de fora.

## Contrato de runtime

| Item | Valor |
|------|--------|
| Célula | **64×64** |
| Colunas | **7** |
| PPU | **`PixelArt.Ppu` (16)** — herói, inimigo e T1 |
| Pivot | **`(0.5, 3/64)`** — 3 px vazios abaixo dos pés |
| Filter | **Point** |
| GO pixel | `localPosition (0, -0.5, 0)`, scale 1 |
| Tela | **384×216**, escala inteira, Point (`PixelPresentation`) |

O `.meta` de `Resources/*/sheet.png` ainda lista PPU 28 / pivot y=0. **O jogo ignora isso.** O runtime usa `Sprite.Create` com `PixelArt.Ppu`.

Os sheets atuais ainda desenham a silhueta chibi (~27 px). O contrato abaixo vale para o **próximo** desenho. Não esticar o sprite velho para fingir os 44 px.

## Molde do herói (próximo desenho)

| Medida | Alvo |
|--------|------|
| Altura de silhueta (idle, alpha > 10) | **44–52 px** |
| Baseline | última linha opaca em **y = 60** (3 px de padding) |
| Cabeça | **25–32%** da altura (cerca de 3,5 a 5 cabeças) |
| Largura de corpo (torso, sem arma) | **22–30 px** |
| Paleta | **12–15 cores** no idle |
| Matéria principal | rampa de **4 tons**: sombra, base, luz, brilho |
| Contorno | seletivo — só onde a silhueta se perde no fundo |

Qualquer herói “no tamanho do Guerreiro” significa **esta caixa**, não o chibi de 24–30 px.

## Guerreiro — clip de especial

O sheet atual (21 frames) reusa o ataque no especial. O próximo sheet precisa de um clip próprio, no mesmo padrão do Mago:

- antecipação, golpe, recuperação (**6+ frames** no corte)
- clip `special` separado (onda / lâmina), sem reusar o corte
- se a grade 7×3 não couber, a grade 7×4 é válida (como o Mago)

## O que o Mago pode (e deve) manter

- Capuz com void (sem rosto)
- Robe + capa (tecido, não armadura)
- Cajado prata + cristal com luz
- Paleta indigo/roxo com rampa de 4 tons no tecido
- Clip `special` (frames 17–20) — sheet 7×4 é válido

O que não pode manter:

- Silhueta fora de **44–52 px**
- Pés/base da robe acima de y=58 (flutuar)
- Blob sem capa, cajado e mãos distinguíveis
- Cajado ocupando a célula inteira no idle
- Cabeça/capuz acima de ~32% da altura

## Animações — o que precisa ler

1. **Idle** — respiração ou capa; o corpo não fica congelado.
2. **Capa** — secundária: leve no idle, mais larga no walk.
3. **Cajado** — brilho na rampa do cristal; no cast o cristal cresce.
4. **Pés no chão** — robe ou bota na baseline y=60.
5. **Ação** — antecipação, golpe, recuperação. O contorno muda; não é só tint.
6. **Especial do Guerreiro** — clip próprio, distinto do corte.

## Cenário

- Paleta no pixel. Sem multiply que lave a cidade.
- Tiles com variação (rachadura, musgo, janela acesa).
- Primeiro plano (`ForeProp`, sorting acima do herói) continua na frente do corpo.

## Checklist antes de exportar um sheet

- [ ] 64×64, Point, sem blur
- [ ] Idle: bbox altura 44–52, bottom y=60 ±1
- [ ] Cabeça entre 25% e 32% dessa altura
- [ ] Rampa de 4 tons na matéria principal; 12–15 cores
- [ ] Contorno seletivo, não moldura preta em tudo
- [ ] Pés na baseline (`Assets/Art/_pipeline/guia_baseline.png` — a caixa do guia antigo de 24–30 px não vale mais)
- [ ] Corte com 6+ frames; Guerreiro com clip `special` próprio
- [ ] Tags Aseprite batem com o FRAME_MAP
- [ ] Cópia Art e `Resources/<Hero>/sheet.png` idênticas (MD5)
- [ ] Ao lado de um frame de Mega Man X ou Super Castlevania, no mesmo zoom, a densidade de sombra conversa

## Próximo desenho

Fonte: **Aseprite**. Não redesenhar via `build_sheet.py` ASCII.  
Não dobrar a célula para 128. Não aplicar filtro de CRT para compensar o chibi.
