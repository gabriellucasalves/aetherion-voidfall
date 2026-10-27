-- Fundo Refino V4 — monta os .aseprite nativos a partir das camadas pintadas.
-- Uso:
--   aseprite -b --script-param tpl=<pasta templates> --script-param cam=<pasta camadas> \
--            --script-param gpl=<paleta.gpl> --script montar_fundo_v4.lua
-- sky/far/city partem do *_template.aseprite (guias GUIDE preservadas);
-- as nuvens são sprites novos com as mesmas camadas de pintura.

local TPL, CAM, GPL = app.params.tpl, app.params.cam, app.params.gpl
assert(TPL and CAM and GPL, "parametros tpl, cam e gpl obrigatorios")
local LAYERS = { "BASE", "SHADOW", "DETAIL", "HIGHLIGHT", "MOSS", "FX" }
local pal = Palette{ fromFile = GPL }
assert(#pal == 58, "paleta GPL deveria ter 58 cores")

local function fill(spr, name)
  for _, lname in ipairs(LAYERS) do
    local layer
    for _, l in ipairs(spr.layers) do
      if l.name == lname then layer = l end
    end
    assert(layer, "camada ausente: " .. lname)
    local img = Image{ fromFile = CAM .. "/" .. name .. "__" .. lname .. ".png" }
    assert(img.width == spr.width and img.height == spr.height, "tamanho divergente: " .. name)
    spr:newCel(layer, 1, img, Point(0, 0))
  end
end

for _, name in ipairs({ "sky", "far", "city" }) do
  local spr = app.open(TPL .. "/" .. name .. "_template.aseprite")
  assert(spr, "template ausente: " .. name)
  spr:setPalette(pal)
  fill(spr, name)
  spr:saveAs(TPL .. "/" .. name .. "_v4.aseprite")
  print("salvo=" .. name .. "_v4.aseprite " .. spr.width .. "x" .. spr.height)
  spr:close()
end

local CLOUDS = { clouds_a = { 168, 52 }, clouds_b = { 184, 60 }, clouds_c = { 72, 22 }, clouds_d = { 48, 16 } }
for _, name in ipairs({ "clouds_a", "clouds_b", "clouds_c", "clouds_d" }) do
  local size = CLOUDS[name]
  local spr = Sprite(size[1], size[2], ColorMode.RGB)
  spr:setPalette(pal)
  spr.layers[1].name = LAYERS[1]
  for i = 2, #LAYERS do spr:newLayer().name = LAYERS[i] end
  fill(spr, name)
  spr:saveAs(TPL .. "/" .. name .. "_v4.aseprite")
  print("salvo=" .. name .. "_v4.aseprite " .. spr.width .. "x" .. spr.height)
  spr:close()
end
