# Guia de estilo — construção dos heróis (Etapa 2)

**Projeto:** Aetherion: Voidfall  
**Base estrutural:** Guerreiro (ver `CharacterPixelArtAudit.md`)  
**Contrato de runtime:** PPU **15**, pivot `(0.5, 3/64)`, `FilterMode.Point`, GO pixel `localPosition.y = -0.5`, scale `(1,1,1)`  

> Este guia fixa **construção interna** (proporções, linhas, silhueta legível).  
> **Não** redefine o tamanho global dos personagens no mundo.

---

## 1. Canvas e célula

| Item | Valor |
|------|-------|
| Célula | **64×64** px |
| Sheet | 7 colunas × N linhas (Guerreiro/Arqueiro: 3 → 448×192; Mago: 4 → 448×256) |
| Leitura de frames | L→R, cima→baixo (igual aos `*Visual.cs`) |
| Outline | **1 px** contínuo (cor de contorno da paleta) |
| Antialias / blur | Proibido nos sprites de personagem |

---

## 2. Bounding box e altura visual (referência Guerreiro)

Medição idle do Guerreiro (frame 0):

| Marco | Y do topo da célula | Notas |
|-------|---------------------|-------|
| Topo da silhueta (coroa/elmo) | **34** | |
| Base da cabeça / ombros | **46–47** | elmo termina; torso começa |
| Mãos / guarda | **49** | zona de empunhadura |
| Cintura | **53** | estreita vs peito |
| Joelhos | **56** | |
| Solas | **60** | última opacidade |
| Chão / FootPivot | **61** | 3 px vazios (61–63); pivot Unity `3/64` |

| Métrica | Alvo (construção) |
|---------|-------------------|
| Altura de silhueta (pés→topo) | **~27 px** (Guerreiro); teto de construção **≤ 32 px** |
| Largura de torso | **~19 px** |
| Largura máx. corpo+gear em idle | **≤ 32 px** (ataques podem exceder) |
| Padding inferior até a base da célula | **exatamente 3 px** (solas em y=60) |

**Regra de baseline:** todo herói jogável deve ter a última linha opaca dos pés em **y=60** (ou ±1 px). O Mago atual (pés em y=48) está fora do contrato — ver `MagoRedesignSpec.md`.

---

## 3. Linhas de construção (esqueleto)

Use as camadas `GUIDE - *` do template Aseprite (`Tools/Aseprite/CreateCharacterTemplate.lua`).

```
Y(topo) 34 ──── GUIDE - Head (topo)
Y       46 ──── GUIDE - Head (base) / ombros
Y       47 ──── GUIDE - Shoulders
Y       49 ──── GUIDE - Hands
Y       53 ──── GUIDE - Waist
Y       56 ──── GUIDE - Knees
Y       60 ──── GUIDE - Feet
Y       61 ──── GUIDE - Bounding Box / chão (pivot)
```

Proporção chibi alvo (~2,1 “cabeças” como o Guerreiro):

- Cabeça/elmo/capuz ≈ **45–50%** da altura da silhueta  
- Torso curto  
- Pernas curtas e legíveis (dois volumes na barra da capa/armadura)

---

## 4. Larguras e arma

| Zona | Largura alvo |
|------|--------------|
| Cabeça | 12–18 px (elmo do Guerreiro ~25 c/ chifres; capuz do Mago deve caber sem virar retângulo) |
| Ombros / peito | 16–22 px |
| Cintura | 12–16 px (mais estreita que o peito) |
| Barra da capa / pernas | duas massas ou fenda ≥ 1–2 px |
| Arma em idle | altura ≈ **50–75%** da silhueta do corpo (Guerreiro espada ~15 px; cajado pode ser um pouco mais alto, sem atravessar a célula inteira no idle) |

Área de arma (idle, lado típico):

- Guerreiro: espada à **direita** do corpo (~x 34–44)  
- Arqueiro: arco à **esquerda**  
- Mago: cajado fino à esquerda/direita, cristal como ponto focal (não blob de partículas)

---

## 5. Camadas de pintura (ordem)

1. **Silhueta** (preto/outline) — leitura a 50% de escala  
2. **Cor base** por região (corpo, roupa, equipamento)  
3. **Sombra** (1 tom abaixo) nas dobras / lado oposto à luz  
4. **Luz** (1 tom acima) no volume principal  
5. **Highlight / accent** pontual (metal, cristal, magma) — poucos px  
6. **FX** (glow, smear) só em frames de ação; não poluir idle

Luz padrão do projeto: **canto superior esquerdo** (como no audit / arte-conceito).

---

## 6. Contagem de cores

| Herói | Idle medido | Meta de construção |
|-------|-------------|--------------------|
| Guerreiro | 10 | 8–12 |
| Arqueiro | 16 | 12–16 |
| Mago | 15 | 12–16 (vários roxos ok se **regiões** forem claras) |

Evitar ruído de 1 px “sujo” que não descreve volume nem material.

---

## 7. Hierarquia de paleta (Etapa 9)

Papéis semânticos (todo herói deve cobrir estes papéis; RGB de referência = builds atuais):

| Papel | Guerreiro (magma) | Mago (índigo) | Arqueiro (floresta) |
|-------|-------------------|---------------|---------------------|
| **Outline** | `#0E0608` (14,6,8) | `#0A0812` (10,8,18) | `#0A0806` (10,8,6) |
| **Shadow** | `#2A060A` / `#180A0E` | `#120E20` / `#20163A` | `#1A120C` / `#2A1812` |
| **Base dark** | `#480C0E` / `#801012` | `#302258` / `#483080` | `#3A2414` / `#624020` |
| **Base** | `#BC1414` | `#6244A8` | `#1C6640` / `#309E5C` |
| **Light** | `#E0301C` | `#8A66D2` / `#B294EB` | `#46D282` / `#C49448` |
| **Highlight** | `#FFDC44` / `#FFF8D2` | `#F8ECFF` / `#D2D6E6` | `#FFFAE6` / `#ECF0DC` |
| **Accent** | `#FF6610` (magma) | `#A03CDC` → `#DC8CFF` (cristal) | `#FFD640` / `#6EF0B4` |

### Mago — vários roxos, regiões legíveis

Mesmo com rampa índigo→lilás, cada zona precisa contrastar com o fundo escuro da Terra 1:

| Região | Papel | Notas |
|--------|-------|-------|
| Interior do capuz | Outline / shadow profundo | “rosto” vazio — sem pele bege |
| Capuz externo | Base → light | volume, não disco plano |
| Manto / robe | Base dark → base | quebras de dobra (ombro, cintura, barra) |
| Trim lilás | Light / highlight | orla do capuz e mangas — lê movimento |
| Cinto / bolsa | Accent marrom `#5C3A2A` | marca a cintura |
| Cajado | Cinza `#9696A8` + highlight | silhueta fina e contínua |
| Cristal | Accent ciano-roxo | menor área, maior brilho — ponto focal |

---

## 8. Import Unity (o que de fato funciona)

| Setting | Valor correto no projeto |
|---------|--------------------------|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single (sheets fatiados em código) |
| Pixels Per Unit | **15** (Visuals; ignore o 28 legado nos .meta se divergir) |
| Pivot | Custom `(0.5, 0.046875)` = `(0.5, 3/64)` |
| Filter Mode | **Point** |
| Compression | **None** |
| Generate Mip Maps | **Off** |
| Read/Write | **On** (Resources + `Sprite.Create`) |

Use **Tools > Pixel Art > Import Tool** para aplicar/conferir.  
Use **Tools > Pixel Art > Character Validator** para comparar os três heróis (nunca auto-corrige arte).

---

## 9. Checklist rápido antes de exportar frame

- [ ] Célula 64×64, pés em y≈60  
- [ ] Silhueta legível em preto puro (sem cor)  
- [ ] Ombros / cintura / duas pernas ou fenda na barra  
- [ ] Outline 1 px; ≤16 cores  
- [ ] Arma com silhueta clara; accent só no focal  
- [ ] Sem mudança de PPU/escala global “para parecer maior”
