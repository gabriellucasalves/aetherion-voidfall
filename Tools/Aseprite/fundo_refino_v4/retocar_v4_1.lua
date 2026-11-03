-- Retoque V4.1 — atualiza clouds_a..d e city nos .aseprite existentes.
-- Troca só as células das camadas de pintura; guias GUIDE e paleta GPL ficam.
-- Uso: aseprite -b --script-param dir=<fundo_refino_v4> --script-param cam=<camadas> \
--               --script-param gpl=<paleta.gpl> --script retocar_v4_1.lua
local DIR, CAM, GPL = app.params.dir, app.params.cam, app.params.gpl
assert(DIR and CAM and GPL, "parametros dir, cam e gpl obrigatorios")
local LAYERS = { "BASE", "SHADOW", "DETAIL", "HIGHLIGHT", "MOSS", "FX" }
local pal = Palette{ fromFile = GPL }

for _, name in ipairs({ "clouds_a", "clouds_b", "clouds_c", "clouds_d", "city" }) do
  local spr = app.open(DIR .. "/" .. name .. "_v4.aseprite")
  assert(spr, "arquivo ausente: " .. name)
  local probe = Image{ fromFile = CAM .. "/" .. name .. "__BASE.png" }
  if spr.width ~= probe.width or spr.height ~= probe.height then
    spr:crop(Rectangle(0, 0, probe.width, probe.height))
  end
  spr:setPalette(pal)
  for _, lname in ipairs(LAYERS) do
    local layer
    for _, l in ipairs(spr.layers) do
      if l.name == lname then layer = l end
    end
    assert(layer, "camada ausente: " .. lname)
    local img = Image{ fromFile = CAM .. "/" .. name .. "__" .. lname .. ".png" }
    spr:newCel(layer, 1, img, Point(0, 0))
  end
  spr:saveAs(DIR .. "/" .. name .. "_v4.aseprite")
  print("retocado=" .. name .. "_v4.aseprite " .. spr.width .. "x" .. spr.height .. " camadas=" .. #spr.layers)
  spr:close()
end
