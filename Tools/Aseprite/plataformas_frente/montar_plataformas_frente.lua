-- Plataformas da frente — monta os .aseprite nativos a partir das camadas pintadas.
-- Uso:
--   aseprite -b --script-param cam=<pasta camadas> --script-param out=<pasta .aseprite> \
--            --script-param gpl=<paleta.gpl> --script montar_plataformas_frente.lua
-- Camadas de pintura iguais às do platform_template (BASE…FX) + guias:
--   "GUIDE - Grid 16"       grade de tiles 16×16
--   "GUIDE - Superficie"    linha de caminhada (topo do colisor no Unity)
-- O ExportForUnity (modo flat) ignora as camadas GUIDE.

local CAM, OUT, GPL = app.params.cam, app.params.out, app.params.gpl
assert(CAM and OUT and GPL, "parametros cam, out e gpl obrigatorios")
local LAYERS = { "BASE", "SHADOW", "DETAIL", "HIGHLIGHT", "MOSS", "FX" }
local pal = Palette{ fromFile = GPL }
assert(#pal == 58, "paleta GPL deveria ter 58 cores")

-- nome = linha da superfície (contrato com T1FrontLevel.cs)
local ASSETS = {
  { "ground", 8 }, { "ground_edge_l", 8 }, { "ground_edge_r", 8 }, { "pit", 8 },
  { "plat_s", 4 }, { "plat_m", 4 }, { "plat_l", 4 },
}

local function guide(spr, name, paint)
  local layer = spr:newLayer()
  layer.name = name
  layer.opacity = 110
  local img = Image(spr.width, spr.height, ColorMode.RGB)
  paint(img)
  spr:newCel(layer, 1, img, Point(0, 0))
end

for _, item in ipairs(ASSETS) do
  local name, surf = item[1], item[2]
  local probe = Image{ fromFile = CAM .. "/" .. name .. "__BASE.png" }
  local spr = Sprite(probe.width, probe.height, ColorMode.RGB)
  spr:setPalette(pal)
  spr.layers[1].name = LAYERS[1]
  for i = 2, #LAYERS do spr:newLayer().name = LAYERS[i] end
  for i, lname in ipairs(LAYERS) do
    local img = Image{ fromFile = CAM .. "/" .. name .. "__" .. lname .. ".png" }
    assert(img.width == spr.width and img.height == spr.height, "tamanho divergente: " .. name)
    spr:newCel(spr.layers[i], 1, img, Point(0, 0))
  end
  local grid = pal:getColor(51)   -- Aether_Ciano_Vivo
  local line = pal:getColor(54)   -- Luz_Rubi_Exotico
  guide(spr, "GUIDE - Grid 16", function(img)
    for y = 0, img.height - 1 do
      for x = 0, img.width - 1 do
        if x % 16 == 0 or y % 16 == 0 then img:drawPixel(x, y, grid) end
      end
    end
  end)
  guide(spr, "GUIDE - Superficie", function(img)
    for x = 0, img.width - 1 do img:drawPixel(x, surf, line) end
  end)
  spr:saveAs(OUT .. "/" .. name .. ".aseprite")
  print("salvo=" .. name .. ".aseprite " .. spr.width .. "x" .. spr.height .. " camadas=" .. #spr.layers)
  spr:close()
end
