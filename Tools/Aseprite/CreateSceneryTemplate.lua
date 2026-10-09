-- CreateSceneryTemplate.lua
-- Aetherion: Voidfall — templates de cenário (camadas de parallax, props, plataformas,
-- tileset 16x16 e fundos de espaço) nos tamanhos e na paleta do T1.
-- Uso: File > Scripts > CreateSceneryTemplate (copie este arquivo para a pasta Scripts do Aseprite).
-- Linha de comando (sem UI):
--   aseprite -b --script-param kind=city --script-param out=city.aseprite --script CreateSceneryTemplate.lua
--
-- Contrato de runtime (T1Scenery.cs): PPU 16, filtro Point.
-- Camadas/props: pivô bottom-center (0.5, 0). Plataformas: pivô no centro do corpo de pedra.
-- 1 pixel = 1/16 de unidade. Câmera ortográfica 5.35 => ~304x171 px visíveis.

local PPU = 16
local CAM_W, CAM_H = 304, 171

-- Tamanhos iguais aos PNGs de Assets/Resources/T1 (medidos no repo)
local KINDS = {
  { id = "sky",      label = "Céu (parallax 0.95) 480x192",            w = 480, h = 192, seam = false, cam = true  },
  { id = "far",      label = "Fundo distante (0.9) 512x130",           w = 512, h = 130, seam = false, cam = true  },
  { id = "city",     label = "Cidade / meio (0.8) 640x176",            w = 640, h = 176, seam = false, cam = true  },
  { id = "pavement", label = "Calçada do plano jogável (loop) 512x44", w = 512, h = 44,  seam = true,  cam = false },
  { id = "prop",     label = "Prop (tamanho livre)",                    w = 48,  h = 96,  seam = false, cam = false },
  { id = "platform", label = "Plataforma (largura em unidades)",        w = 0,   h = 19,  seam = false, cam = false },
  { id = "tileset",  label = "Tileset 16x16 (8x4 tiles = 128x64)",      w = 128, h = 64,  seam = false, cam = false },
  { id = "space_v",  label = "Espaço — loop vertical 320x384",          w = 320, h = 384, seam = true,  cam = true  },
  { id = "space_h",  label = "Espaço — loop horizontal 640x192",        w = 640, h = 192, seam = true,  cam = true  },
}

-- Camadas de pintura por tipo (de baixo para cima)
local PAINT_LAYERS = {
  default = { "BASE", "SHADOW", "DETAIL", "HIGHLIGHT", "MOSS", "FX" },
  space   = { "SPACE_BASE", "STARS_FAR", "NEBULA", "STARS_NEAR", "PLANETS", "DEBRIS", "FX" },
  tileset = { "TILES", "DETAIL", "HIGHLIGHT" },
}

-- Paleta nomeada do T1 (Assets/Art/T1/build_t1.py) + 2 acentos de espaço PROPOSTOS
local PALETTE = {
  {5,5,13},{8,9,23},{20,15,38},{36,23,51},{77,18,26},{224,230,247},{158,171,209},
  {14,13,29},{19,19,36},{26,24,43},{36,33,54},{56,54,82},{22,20,36},{77,77,112},
  {255,158,56},{13,13,26},{255,115,26},{255,209,89},{26,43,31},{41,66,41},{107,23,31},
  {71,15,23},{23,18,26},{36,28,38},{69,64,87},{104,99,128},{40,37,56},{150,148,181},
  {48,87,51},{79,128,66},{199,154,66},{130,96,38},{156,94,224},{97,54,148},
  {89,217,255},{38,122,153}, -- VOID_CYAN / VOID_TEAL (propostos para o espaço)
}

local C_GRID   = Color{ r=60,  g=56,  b=84,  a=150 }
local C_CAM    = Color{ r=80,  g=180, b=220, a=255 }
local C_SEAM   = Color{ r=240, g=90,  b=90,  a=255 }
local C_GROUND = Color{ r=240, g=220, b=80,  a=255 }
local C_EDGE   = Color{ r=50,  g=50,  b=50,  a=255 }

-- Dialog:number devolve número no Aseprite 1.3 atual, mas builds anteriores
-- devolvem o texto. math.floor em string quebra no Lua 5.4.
local function as_number(value, fallback)
  local n = tonumber(value)
  if not n then return fallback end
  return n
end

local function as_int(value, fallback)
  return math.floor(as_number(value, fallback))
end

local function draw_hline(img, y, color)
  if y < 0 or y >= img.height then return end
  for x = 0, img.width - 1 do img:drawPixel(x, y, color) end
end

local function draw_vline(img, x, color)
  if x < 0 or x >= img.width then return end
  for y = 0, img.height - 1 do img:drawPixel(x, y, color) end
end

local function draw_rect_outline(img, x0, y0, x1, y1, color)
  for x = x0, x1 do
    if x >= 0 and x < img.width then
      if y0 >= 0 and y0 < img.height then img:drawPixel(x, y0, color) end
      if y1 >= 0 and y1 < img.height then img:drawPixel(x, y1, color) end
    end
  end
  for y = y0, y1 do
    if y >= 0 and y < img.height then
      if x0 >= 0 and x0 < img.width then img:drawPixel(x0, y, color) end
      if x1 >= 0 and x1 < img.width then img:drawPixel(x1, y, color) end
    end
  end
end

local function find_kind(id)
  for _, k in ipairs(KINDS) do
    if k.id == id or k.label == id then return k end
  end
  return KINDS[1]
end

-- ---------- parâmetros: diálogo (UI) ou --script-param (CLI) ----------
local kind, propW, propH, platUnits, outFile

if app.isUIAvailable then
  local labels = {}
  for _, k in ipairs(KINDS) do labels[#labels + 1] = k.label end
  local dlg = Dialog("Aetherion — Template de Cenário")
  dlg:combobox{ id = "kind", label = "Tipo:", option = labels[1], options = labels }
  dlg:separator{ text = "Prop" }
  dlg:number{ id = "pw", label = "Largura (px):", text = "48", decimals = 0 }
  dlg:number{ id = "ph", label = "Altura (px):",  text = "96", decimals = 0 }
  dlg:separator{ text = "Plataforma" }
  dlg:number{ id = "units", label = "Largura (unidades):", text = "4.20", decimals = 2 }
  dlg:label{ text = "PNG = round(unidades*16)+4 x 19  (ex.: 4.20 -> plat420 71x19)" }
  dlg:button{ id = "ok", text = "Criar", focus = true }
  dlg:button{ id = "cancel", text = "Cancelar" }
  dlg:show()
  if not dlg.data.ok then return end
  kind = find_kind(dlg.data.kind)
  propW = as_int(dlg.data.pw, 48)
  propH = as_int(dlg.data.ph, 96)
  platUnits = as_number(dlg.data.units, 4.2)
else
  local p = app.params or {}
  kind = find_kind(p.kind or "sky")
  propW = as_int(p.w, 48)
  propH = as_int(p.h, 96)
  platUnits = as_number(p.units, 4.2)
  outFile = p.out
end

local W, H = kind.w, kind.h
local suggested = kind.id
if kind.id == "prop" then
  W, H = math.max(8, propW), math.max(8, propH)
elseif kind.id == "platform" then
  -- Sprite() rejeita largura < 1. Unidades negativas cairiam nesse caso.
  W = math.max(1, math.floor(platUnits * PPU + 0.5) + 4)
  suggested = "plat" .. math.floor(platUnits * 100 + 0.5)
end

local spr = Sprite(W, H, ColorMode.RGB)
app.activeSprite = spr

app.transaction("Create Scenery Template", function()
  -- paleta
  local pal = Palette(#PALETTE + 1)
  pal:setColor(0, Color{ r=0, g=0, b=0, a=0 })
  for i, c in ipairs(PALETTE) do
    pal:setColor(i, Color{ r=c[1], g=c[2], b=c[3], a=255 })
  end
  spr:setPalette(pal)
  spr.gridBounds = Rectangle(0, 0, 16, 16)

  local function ensure_cel_image(layer)
    local cel = layer:cel(1)
    if not cel then
      local img = Image(W, H, ColorMode.RGB)
      img:clear()
      spr:newCel(layer, 1, img, Point(0, 0))
      cel = layer:cel(1)
    end
    return cel.image
  end

  local function guide_layer(name, painter)
    local layer = spr:newLayer()
    layer.name = name
    local cel = spr:newCel(layer, 1, Image(W, H, ColorMode.RGB), Point(0, 0))
    local img = cel.image
    img:clear()
    painter(img)
    cel.image = img
    layer.opacity = 160
    return layer
  end

  -- camadas de pintura (a camada padrão vira a primeira)
  local list = PAINT_LAYERS.default
  if kind.id == "space_v" or kind.id == "space_h" then list = PAINT_LAYERS.space end
  if kind.id == "tileset" then list = PAINT_LAYERS.tileset end
  local base = spr.layers[1]
  base.name = list[1]
  ensure_cel_image(base):clear()
  for i = 2, #list do
    local layer = spr:newLayer()
    layer.name = list[i]
  end

  -- guias (ficam por cima; o ExportForUnity.lua ignora "GUIDE - *")
  guide_layer("GUIDE - Grid 16", function(img)
    for x = 0, W - 1, 16 do draw_vline(img, x, C_GRID) end
    for y = H % 16, H - 1, 16 do draw_hline(img, y, C_GRID) end
    draw_rect_outline(img, 0, 0, W - 1, H - 1, C_EDGE)
  end)

  -- base do sprite = pivô bottom-center (0.5, 0): o que encosta no chão fica na última linha.
  -- Plataforma: T1Scenery.RuinPlatform usa o centro do corpo de pedra, não a base do PNG.
  guide_layer("GUIDE - Pivot / Chão", function(img)
    local cx = math.floor(W / 2)
    if kind.id == "platform" then
      -- Unity y=0 é a base. Corpo [10, 16) px; pivô em 10 + (0.38*16)/2 ≈ 13.
      -- Aseprite y=0 é o topo, então o pixel do pivô é (H-1) - 13.
      local pivotY = H - 1 - 13
      draw_hline(img, pivotY, C_GROUND)
      for y = pivotY - 4, pivotY + 4 do
        if y >= 0 and y < img.height then img:drawPixel(cx, y, C_GROUND) end
      end
      draw_hline(img, H - 17, C_CAM)   -- topo pisável (topo do colisor, ~16 px da base)
      draw_hline(img, H - 11, C_CAM)   -- base do corpo de pedra (10 px da base)
      draw_vline(img, 2, C_SEAM); draw_vline(img, W - 3, C_SEAM) -- 2 px de borda irregular por lado
    else
      draw_hline(img, H - 1, C_GROUND)
      for y = math.max(0, H - 6), H - 1 do img:drawPixel(cx, y, C_GROUND) end
    end
  end)

  if kind.cam then
    guide_layer("GUIDE - Camera 304x171", function(img)
      local x0 = math.floor((W - CAM_W) / 2)
      local y0 = H - CAM_H
      if y0 < 0 then y0 = 0 end
      draw_rect_outline(img, x0, y0, x0 + CAM_W - 1, math.min(H - 1, y0 + CAM_H - 1), C_CAM)
    end)
  end

  if kind.seam then
    guide_layer("GUIDE - Loop Seam", function(img)
      if kind.id == "space_v" then
        draw_hline(img, 0, C_SEAM); draw_hline(img, H - 1, C_SEAM)
        draw_hline(img, 16, C_SEAM); draw_hline(img, H - 17, C_SEAM)
      else
        draw_vline(img, 0, C_SEAM); draw_vline(img, W - 1, C_SEAM)
        draw_vline(img, 16, C_SEAM); draw_vline(img, W - 17, C_SEAM)
      end
    end)
  end

  app.activeLayer = spr.layers[1]
end)

if outFile then
  spr:saveAs(outFile)
elseif app.isUIAvailable then
  local pivot = "PPU 16, pivô bottom-center, Point."
  if kind.id == "platform" then
    pivot = "PPU 16, pivô no centro do corpo de pedra (não na base do PNG), Point."
  end
  app.alert("Template '" .. kind.id .. "' " .. W .. "x" .. H .. " criado.\n" ..
    "Nome sugerido do PNG: " .. suggested .. ".png (Assets/Resources/<Fase>/)\n" ..
    pivot .. "\nOculte as camadas GUIDE - * antes de exportar\n" ..
    "(ou use ExportForUnity.lua). Use Ctrl+' para mostrar a grade 16x16.")
end
