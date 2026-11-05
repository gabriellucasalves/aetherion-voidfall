# Layout do trecho típico (viewport 768×432, PPU 32)
Coordenadas em px do conceito, com origem no canto superior esquerdo. As unidades são relativas à borda inferior esquerda do viewport: u_x = x/32, u_y = (432 − y)/32.

| Elemento | x px | topo y px | u_x | topo u_y |
|---|---|---|---|---|
| Ground L (início seguro) | 0–240 | 392 | 0–7.5 | 1.25 |
| **GAP 1 (mortal)** | 240–320 (80) | — | 7.5–10 | abismo |
| Ilha mid | 320–460 | 392 | 10–14.375 | 1.25 |
| **GAP 2 (mortal)** | 460–560 (100) | — | 14.375–17.5 | abismo |
| Ground R | 560–768 | 392 | 17.5–24 | 1.25 |
| Plat A | 200–280 | 300 | 6.25–8.75 | 4.125 |
| Plat B | 480–580 | 270 | 15–18.125 | 5.06 |
| Plat C (opcional) | 620–700 | 240 | 19.375–21.875 | 6.0 |

- O chão começa no y 386 px por causa dos tufos de musgo, que são só cosméticos. O collider deve usar o topo em y392 px.
- Os abismos são opacos (#12141C / #0A0B12) até a base da tela. Abaixo do chão, colocar um trigger de kill que reusa a lógica existente de "VOCÊ CAIU".
- A partir do centro da câmera (y = 216 px): o topo do chão fica em −5.5 u, A em −2.625 u, B em −1.69 u e C em −0.75 u.
- O level completo pode ter mais gaps. Este é o padrão a replicar.
