-- CreateShipTemplate.lua
-- Aetherion: Voidfall — template de nave (jogador ou inimiga) com guias, camadas,
-- frames e tags prontos para virar spritesheet de 7 colunas.
-- Uso: File > Scripts > CreateShipTemplate (copie este arquivo para a pasta Scripts do Aseprite).
-- Linha de comando (sem UI):
--   aseprite -b --script-param size=64 --script-param dir=right --script-param out=nave.aseprite --script CreateShipTemplate.lua
--
-- Contrato de runtime proposto para naves: PPU 15 (mesma escala dos heróis), pivô no CENTRO
-- (0.5, 0.5), filtro Point, sem compressão. A nave cabe na caixa 48x48 (célula 64) e o
-- hitbox fica bem menor que o desenho (~14x14), como em shmups 16-bit.

-- Tags na ordem do sheet (frames 1-based, ~12 fps = 83 ms)
local TAGS = {
  { name = "idle",    count = 4 },
  { name = "bank_up", count = 2 }, -- "inclinar" para cima / esquerda (nave vertical)
  { name = "bank_dn", count = 2 }, -- para baixo / direita
  { name = "hit",     count = 2 },
  { name = "special", count = 3 },
  { name = "death",   count = 4 },
}
local FRAME_MS = 83

local PAINT_LAYERS = {
  "HULL",
  "COCKPIT",
  "DETAIL",
  "OUTLINE",
  "SHADOW",
  "HIGHLIGHT",
  "ENGINE_FX",
  "FX",
}

-- Paleta: base fria do T1 + fogo/orbe + acentos de espaço PROPOSTOS
local PALETTE = {
  {5,5,13},{13,13,26},{22,20,36},{36,33,54},{56,54,82},{77,77,112},{104,99,128},
  {150,148,181},{224,230,247},{158,171,209},{77,18,26},{107,23,31},{255,115,26},
  {255,158,56},{255,209,89},{199,154,66},{130,96,38},{156,94,224},{97,54,148},
  {89,217,255},{38,122,153},
}

local function as_int(value, fallback)
  local n = tonumber(value)
  if not n then return fallback end
  return math.floor(n)
end

-- UI manda "cima (fase vertical)"; CLI aceita up/cima/right.
local function normalize_dir(value)
  local v = string.lower(tostring(value or "right"))
  if v == "up" or v == "cima" or string.sub(v, 1, 4) == "cima" then
    return "up"
  end
  return "right"
end

local function draw_hline(img, y, x0, x1, color)
  if y < 0 or y >= img.height or x0 > x1 then return end
  for x = math.max(0, x0), math.min(img.width - 1, x1) do img:drawPixel(x, y, color) end
end

local function draw_vline(img, x, y0, y1, color)
  if x < 0 or x >= img.width or y0 > y1 then return end
  for y = math.max(0, y0), math.min(img.height - 1, y1) do img:drawPixel(x, y, color) end
end

local function draw_rect_outline(img, x0, y0, x1, y1, color)
  draw_hline(img, y0, x0, x1, color); draw_hline(img, y1, x0, x1, color)
  draw_vline(img, x0, y0, y1, color); draw_vline(img, x1, y0, y1, color)
end

-- ---------- parâmetros ----------
local CELL, DIR, outFile = 64, "right", nil
if app.isUIAvailable then
  local dlg = Dialog("Aetherion — Template de Nave")
  dlg:combobox{ id = "size", label = "Célula:", option = "64 (nave do jogador / elite)",
    options = { "64 (nave do jogador / elite)", "32 (inimigo pequeno)", "128 (chefe)" } }
  dlg:combobox{ id = "dir", label = "Bico aponta para:", option = "direita (fase lateral)",
    options = { "direita (fase lateral)", "cima (fase vertical)" } }
  dlg:button{ id = "ok", text = "Criar", focus = true }
  dlg:button{ id = "cancel", text = "Cancelar" }
  dlg:show()
  if not dlg.data.ok then return end
  CELL = as_int(string.match(tostring(dlg.data.size or ""), "^(%d+)"), 64)
  DIR = normalize_dir(dlg.data.dir)
else
  local p = app.params or {}
  CELL = as_int(p.size, 64)
  DIR = normalize_dir(p.dir)
  outFile = p.out
end

if CELL < 8 then CELL = 64 end

local MID = CELL // 2
local BOX = (CELL * 3) // 4              -- silhueta máxima (48 em 64)
local BOX0 = (CELL - BOX) // 2
local HIT = math.max(6, (CELL * 14) // 64) -- hitbox (~14 em 64)

local GUIDE = {
  { name = "GUIDE - Cell",      color = Color{ r=50,  g=50,  b=50,  a=255 } },
  { name = "GUIDE - Center",    color = Color{ r=120, g=120, b=140, a=255 } },
  { name = "GUIDE - Max Box",   color = Color{ r=220, g=160, b=60,  a=255 } },
  { name = "GUIDE - Hitbox",    color = Color{ r=240, g=90,  b=90,  a=255 } },
  { name = "GUIDE - Engine",    color = Color{ r=80,  g=180, b=220, a=255 } },
  { name = "GUIDE - Muzzle",    color = Color{ r=240, g=220, b=80,  a=255 } },
}

local function paint_guide(name, img, color)
  if name == "GUIDE - Cell" then
    draw_rect_outline(img, 0, 0, CELL - 1, CELL - 1, color)
  elseif name == "GUIDE - Center" then
    draw_hline(img, MID, 0, CELL - 1, color); draw_vline(img, MID, 0, CELL - 1, color)
  elseif name == "GUIDE - Max Box" then
    draw_rect_outline(img, BOX0, BOX0, BOX0 + BOX - 1, BOX0 + BOX - 1, color)
  elseif name == "GUIDE - Hitbox" then
    local h0 = MID - HIT // 2
    draw_rect_outline(img, h0, h0, h0 + HIT - 1, h0 + HIT - 1, color)
  elseif name == "GUIDE - Engine" then
    -- área do jato: atrás da nave (esquerda se aponta à direita; embaixo se aponta para cima)
    local e = math.max(4, CELL // 8)
    if DIR == "up" then
      draw_rect_outline(img, MID - e, BOX0 + BOX, MID + e - 1, CELL - 2, color)
    else
      draw_rect_outline(img, 1, MID - e, BOX0 - 1, MID + e - 1, color)
    end
  elseif name == "GUIDE - Muzzle" then
    -- ponto de saída do tiro (bico)
    if DIR == "up" then
      draw_hline(img, BOX0, MID - 2, MID + 2, color)
    else
      draw_vline(img, BOX0 + BOX - 1, MID - 2, MID + 2, color)
    end
  end
end

local spr = Sprite(CELL, CELL, ColorMode.RGB)
app.activeSprite = spr

app.transaction("Create Ship Template", function()
  local pal = Palette(#PALETTE + 1)
  pal:setColor(0, Color{ r=0, g=0, b=0, a=0 })
  for i, c in ipairs(PALETTE) do pal:setColor(i, Color{ r=c[1], g=c[2], b=c[3], a=255 }) end
  spr:setPalette(pal)
  spr.gridBounds = Rectangle(0, 0, CELL, CELL)

  -- frames. frame.duration é em SEGUNDOS (83/1000 = 0.083 → 83 ms).
  local total = 0
  for _, t in ipairs(TAGS) do total = total + t.count end
  for _ = 2, total do spr:newEmptyFrame() end
  for i = 1, #spr.frames do spr.frames[i].duration = FRAME_MS / 1000 end

  -- tags (newTag é inclusivo e 1-based)
  local f = 1
  for _, t in ipairs(TAGS) do
    local tag = spr:newTag(f, f + t.count - 1)
    tag.name = t.name
    f = f + t.count
  end

  -- camadas de pintura (a camada padrão vira HULL)
  local base = spr.layers[1]
  base.name = PAINT_LAYERS[1]
  for i = 2, #PAINT_LAYERS do
    local layer = spr:newLayer()
    layer.name = PAINT_LAYERS[i]
  end

  -- guias em TODOS os frames (para ver a caixa enquanto anima).
  -- Cada cel recebe um clone: a API não liga cels que compartilham a mesma Image.
  for _, g in ipairs(GUIDE) do
    local layer = spr:newLayer()
    layer.name = g.name
    layer.opacity = 170
    local img = Image(CELL, CELL, ColorMode.RGB)
    img:clear()
    paint_guide(g.name, img, g.color)
    for fr = 1, #spr.frames do
      spr:newCel(layer, fr, img:clone(), Point(0, 0))
    end
  end

  for i = 1, #spr.layers do
    if spr.layers[i].name == "HULL" then app.activeLayer = spr.layers[i] end
  end
  app.activeFrame = spr.frames[1]
end)

if outFile then
  spr:saveAs(outFile)
elseif app.isUIAvailable then
  app.alert("Template de nave " .. CELL .. "x" .. CELL .. " criado (" .. #spr.frames .. " frames).\n" ..
    "Tags: idle 4 · bank_up 2 · bank_dn 2 · hit 2 · special 3 · death 4.\n" ..
    "Silhueta <= " .. BOX .. "x" .. BOX .. ", hitbox ~" .. HIT .. "x" .. HIT .. ", 1px de outline, luz do topo-esquerdo.\n" ..
    "Exporte com ExportForUnity.lua (7 colunas). PPU 15, pivô no centro.")
end
