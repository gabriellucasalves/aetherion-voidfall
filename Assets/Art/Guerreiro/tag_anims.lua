local spr = app.activeSprite
if not spr then return end
while #spr.tags > 0 do
  spr:deleteTag(spr.tags[1])
end
local function tag(name, a, b)
  local t = spr:newTag(a, b)
  t.name = name
end
tag("idle", 1, 4)
tag("walk", 5, 10)
tag("jump", 11, 13)
tag("attack", 14, 17)
tag("block", 18, 19)
tag("hurt", 20, 21)
spr:saveAs(spr.filename)
