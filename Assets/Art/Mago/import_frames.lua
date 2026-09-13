local root = "/Users/gabriellucas/Desktop/trabalho jogo sexta/Assets/Art/Mago/"
local sprite = app.open(root .. "frames/m_00.png")
local layer = sprite.layers[1]

for i = 1, 27 do
  local img = Image{ fromFile = root .. string.format("frames/m_%02d.png", i) }
  local frame = sprite:newEmptyFrame(i + 1)
  sprite:newCel(layer, frame, img, Point(0, 0))
end

while #sprite.tags > 0 do
  sprite:deleteTag(sprite.tags[1])
end

local function tag(name, a, b)
  local t = sprite:newTag(a, b)
  t.name = name
end

tag("idle", 1, 4)
tag("walk", 5, 10)
tag("jump", 11, 13)
tag("cast", 14, 17)
tag("special", 18, 21)
tag("hurt", 22, 23)

sprite:saveAs(root .. "Mago.aseprite")
print("aseprite ok")
