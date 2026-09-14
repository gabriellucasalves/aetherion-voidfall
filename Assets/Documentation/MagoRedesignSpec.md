# Spec de redesign — Mago (Etapa 8)

**Escopo desta etapa:** especificação apenas.  
**Não** sobrescrever `Resources/Mago/sheet.png`, `Art/Mago/Mago_sheet.png` nem frames.  
**Não** alterar combate / movimento / prefabs de produção.

Referências: `CharacterPixelArtAudit.md`, `CharacterStyleGuide.md`, arte-conceito (capuz roxo, face void, cajado + cristal violeta).

---

## 1. Identidade a preservar

| Elemento | Obrigatório |
|----------|-------------|
| Capuz amplo | Sim — silhueta de mago hooded |
| Face escura / void | Sim — **sem** pele bege / sorriso |
| Roxo / índigo no manto | Sim |
| Capa / robe longa | Sim |
| Cinto + bolsa | Sim (marcam cintura) |
| Cajado metálico | Sim |
| Cristal violeta no topo | Sim — ponto focal |
| Proporção chibi (~2 cabeças) | Sim — alinhada ao Guerreiro, não “adult proportion” |

---

## 2. Problemas medidos (audit)

| Problema | Medida atual | Alvo |
|----------|--------------|------|
| Baseline | Pés em y=48 (**15 px** vazios) | Pés em **y=60** (±1), 3 px de padding |
| Altura visual | ~38 px | **27–32 px** (construção Guerreiro) |
| Largura “blob” | robe ~21 px contínua | peito 16–22, cintura **mais estreita**, barra com fenda |
| Membros | mãos/pés embutidos | 2 braços/mãos + 2 botas legíveis |
| Cajado idle | ok, mas cast estica ~59 px | idle ≤ ~ altura do corpo; ataque pode subir sem preencher a célula |

---

## 3. Regras de construção (Mago)

### 3.1 Capuz e ombros

- Capuz grande, mas **ombros legíveis por baixo** (degrau ou overhang de 1–2 px).  
- Interior void (outline/shadow) ≠ um retângulo preto colado no robe.  
- Trim lilás na orla do capuz (ajuda walk/idle).

### 3.2 Capa ≠ um retângulo

- Quebrar o volume em: peito / dobra da cintura / barra.  
- Cintura visível (cinto marrom + fivela).  
- Barra da capa com **separação de pernas** (fenda ≥ 1–2 px ou dois volumes).  
- Botas escuras visíveis sob a barra (não “fantasma flutuante”).

### 3.3 Braços e mãos

- Dois braços (mangas) saindo do volume do peito.  
- Mãos (tom `e` / bege-acinzentado discreto) ou punhos de manga distintos — pelo menos em idle e cast.  
- Evitar mangas que fundem num único blob lateral.

### 3.4 Cajado e cristal

- Cajado: silhueta **simples** (haste 1–2 px + nó/garra).  
- Cristal: diamante pequeno, highlight branco/lilás — **único** accent forte no idle.  
- Sem chuva de partículas 1 px inúteis no idle (FX só em cast/special).

### 3.5 Pipeline de desenho

1. **Silhueta** preta no template (GUIDE - Bounding Box + Skeleton)  
2. **Cores base** por região (capuz / robe / cinto / botas / metal)  
3. **Sombra → luz → highlight** (luz topo-esquerda)  
4. Cristal e trim por último  

---

## 4. Linhas-guia (mesmas do Guerreiro)

Copiar do StyleGuide / template Aseprite:

| Linha | Y (topo) |
|-------|----------|
| Crown | 34 |
| Shoulders | 47 |
| Hands | 49 |
| Waist | 53 |
| Knees | 56 |
| Feet | 60 |
| Ground / pivot | 61 |

O redesign deve **encostar** a silhueta nessas linhas (não redesenhar as linhas para caber o blob atual).

---

## 5. Hierarquia de paleta (Mago)

| Papel | RGB | Hex | Uso |
|-------|-----|-----|-----|
| Outline | 10,8,18 | `#0A0812` | contorno |
| Shadow / void | 18,14,32 | `#120E20` | interior do capuz |
| Base dark | 32,22,58 → 48,34,88 | `#20163A` / `#302258` | robe fundo |
| Base | 72,48,128 → 98,68,168 | `#483080` / `#6244A8` | robe médio |
| Light | 138,102,210 → 178,148,235 | `#8A66D2` / `#B294EB` | trim / volume |
| Highlight | 248,236,255 / 210,214,230 | `#F8ECFF` / `#D2D6E6` | metal / cristal core |
| Accent cristal | 160,60,220 → 220,140,255 | `#A03CDC` / `#DC8CFF` | focal |
| Cinto / bolsa | 92,58,42 | `#5C3A2A` | cintura |
| Botas | 28,18,40 | `#1C1228` | pés |
| Hurt (só frames hurt) | 160,48,72 / 210,80,100 | — | tint |

**Contraste:** robe médio e trim devem permanecer legíveis sobre cenários escuros; void do capuz deve ser o tom mais escuro depois do outline.

---

## 6. Critérios de aceite (quando houver arte nova — etapas futuras)

- [ ] Pés em y≈60; sem flutuação vs Guerreiro na Comparison Window  
- [ ] Altura silhueta 27–32 px  
- [ ] Silhueta preta: capuz ≠ retângulo; ombros; cintura; duas pernas/botas  
- [ ] Cast/special legíveis pelo cajado **e** pose de braço, não só glow  
- [ ] Mesmos PPU 15 / pivot / Point — sem scale no prefab  
- [ ] Validator: sem ❌ em baseline / altura / import  

---

## 7. Fora de escopo agora

- Substituir PNGs de produção  
- Mudar `MagoVisual` frame map / timings de combate  
- Auto-fix de arte pelo Validator
