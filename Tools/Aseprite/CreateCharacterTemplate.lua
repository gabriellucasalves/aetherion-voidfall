-- CreateCharacterTemplate.lua
-- Aetherion: Voidfall — template 64x64 com guias do Guerreiro (audit Etapa 1).
-- Uso: File > Scripts > CreateCharacterTemplate (copie este arquivo para a pasta Scripts do Aseprite).

local CELL = 64

-- Y a partir do TOPO da célula (igual CharacterStyleGuide / CharacterPixelArtStandards)
local GUIDE = {
  { name = "GUIDE - Bounding Box", y = 61, color = Color{ r=80,  g=80,  b=80,  a=255 } },
  { name = "GUIDE - Skeleton",     y = -1, color = Color{ r=120, g=120, b=140, a=255 } }, -- verticals only
  { name = "GUIDE - Head",         y = 34, color = Color{ r=220, g=80,  b=80,  a=255 } },
  { name = "GUIDE - Shoulders",    y = 47, color = Color{ r=220, g=160, b=60,  a=255 } },
  { name = "GUIDE - Hands",        y = 49, color = Color{ r=80,  g=180, b=220, a=255 } },
  { name = "GUIDE - Waist",        y = 53, color = Color{ r=80,  g=220, b=120, a=255 } },
  { name = "GUIDE - Knees",        y = 56, color = Color{ r=160, g=100, b=220, a=255 } },
  { name = "GUIDE - Feet",         y = 60, color = Color{ r=240, g=220, b=80,  a=255 } },
  { name = "GUIDE - Weapon Area",  y = -2, color = Color{ r=200, g=100, b=200, a=255 } },
}

local PAINT_LAYERS = {
  "BODY",
  "CLOTHING",
  "EQUIPMENT",
  "OUTLINE",
  "SHADOW",
  "HIGHLIGHT",
  "FX",
}

local function clear_layer(layer, rgba)
  app.transaction(function()
    local cel = layer:cel(1)
    if not cel then
      app.activeLayer = layer
      -- ensure cel exists by putting a transparent pixel then clearing
    end
  end)
end

local function draw_hline(img, y, color)
  if y < 0 or y >= CELL then return end
  for x = 0, CELL - 1 do
    img:drawPixel(x, y, color)
  end
end

local function draw_vline(img, x, y0, y1, color)
  if x < 0 or x >= CELL then return end
  if y0 > y1 then y0, y1 = y1, y0 end
  for y = y0, y1 do
    if y >= 0 and y < CELL then
      img:drawPixel(x, y, color)
    end
  end
end

local function draw_rect_outline(img, x0, y0, x1, y1, color)
  for x = x0, x1 do
    img:drawPixel(x, y0, color)
    img:drawPixel(x, y1, color)
  end
  for y = y0, y1 do
    img:drawPixel(x0, y, color)
    img:drawPixel(x1, y, color)
  end
end

app.transaction("Create Character Template", function()
  local spr = Sprite(CELL, CELL, ColorMode.RGB)
  app.activeSprite = spr

  -- Rename default layer to first GUIDE, then create the rest
  local base = spr.layers[1]
  base.name = GUIDE[1].name
  base.isVisible = true
  base.isEditable = true

  -- Create remaining GUIDE layers + paint layers
  for i = 2, #GUIDE do
    local layer = spr:newLayer()
    layer.name = GUIDE[i].name
    layer.isVisible = true
  end
  for _, name in ipairs(PAINT_LAYERS) do
    local layer = spr:newLayer()
    layer.name = name
    layer.isVisible = true
  end

  -- Helper: find layer by name
  local function layer_by_name(name)
    for i = 1, #spr.layers do
      if spr.layers[i].name == name then return spr.layers[i] end
    end
    return nil
  end

  local function ensure_cel_image(layer)
    local cel = layer:cel(1)
    if not cel then
      app.activeLayer = layer
      local img = Image(CELL, CELL, ColorMode.RGB)
      img:clear()
      spr:newCel(layer, 1, img, Point(0, 0))
      cel = layer:cel(1)
    end
    return cel.image
  end

  -- Bounding box: safe content rect (x 12-51, y 34-60) + ground line 61
  do
    local img = ensure_cel_image(layer_by_name("GUIDE - Bounding Box"))
    img:clear()
    local c = GUIDE[1].color
    draw_rect_outline(img, 12, 34, 51, 60, c)
    draw_hline(img, 61, c)
    -- outer cell edge faint
    draw_rect_outline(img, 0, 0, 63, 63, Color{ r=50, g=50, b=50, a=255 })
  end

  -- Skeleton: center spine + shoulder / hip ticks
  do
    local img = ensure_cel_image(layer_by_name("GUIDE - Skeleton"))
    img:clear()
    local c = Color{ r=120, g=120, b=140, a=255 }
    local cx = 31
    draw_vline(img, cx, 34, 60, c)
    -- shoulder width tick
    for x = 22, 40 do img:drawPixel(x, 47, c) end
    -- hip width tick
    for x = 24, 38 do img:drawPixel(x, 53, c) end
  end

  -- Horizontal guides
  local horizontals = {
    { "GUIDE - Head", 34 },
    { "GUIDE - Shoulders", 47 },
    { "GUIDE - Hands", 49 },
    { "GUIDE - Waist", 53 },
    { "GUIDE - Knees", 56 },
    { "GUIDE - Feet", 60 },
  }
  for _, g in ipairs(horizontals) do
    local layer = layer_by_name(g[1])
    local img = ensure_cel_image(layer)
    img:clear()
    local color = nil
    for _, def in ipairs(GUIDE) do
      if def.name == g[1] then color = def.color break end
    end
    draw_hline(img, g[2], color)
    -- head also marks base of head at 46
    if g[1] == "GUIDE - Head" then
      draw_hline(img, 46, Color{ r=180, g=60, b=60, a=200 })
    end
  end

  -- Weapon area (right-side idle pocket like Guerreiro sword)
  do
    local img = ensure_cel_image(layer_by_name("GUIDE - Weapon Area"))
    img:clear()
    local c = Color{ r=200, g=100, b=200, a=255 }
    draw_rect_outline(img, 34, 36, 46, 58, c)
  end

  -- Clear paint layers (empty transparent)
  for _, name in ipairs(PAINT_LAYERS) do
    local layer = layer_by_name(name)
    local img = ensure_cel_image(layer)
    img:clear()
  end

  -- Select BODY for painting; leave GUIDEs visible but user can hide group easily
  app.activeLayer = layer_by_name("BODY")
end)

app.alert("Template 64x64 criado.\nGuias = proporções do Guerreiro (audit).\nOculte as camadas GUIDE - * antes de exportar.")
