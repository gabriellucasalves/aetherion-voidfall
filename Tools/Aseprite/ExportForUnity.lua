-- ExportForUnity.lua
-- Aetherion: Voidfall — exporta o sprite ativo para o Unity sem as camadas GUIDE.
--   * modo "sheet":  spritesheet PNG em linhas de 7 colunas + JSON (hash) com tags
--                    (personagens, inimigos e naves; mesmo layout dos sheet.png de Resources)
--   * modo "flat":   1 PNG achatado (cenário: sky.png, city.png, plat420.png...)
--   * modo "layers": 1 PNG por camada visível que não seja GUIDE (nome do arquivo = nome da camada)
-- Uso: File > Scripts > ExportForUnity
-- Linha de comando:
--   aseprite -b nave.aseprite --script-param mode=sheet --script-param out=Assets/Resources/Naves/nave_vazio --script ExportForUnity.lua
-- Nunca suaviza nem redimensiona: o Unity recebe exatamente os pixels do arquivo.
--
-- ExportSpriteSheet com layer="" (o padrão) exporta só as camadas visíveis.
-- sprite.layers é só o primeiro nível: guias dentro de um grupo também são ocultadas.

local spr = app.activeSprite
if not spr then
  if app.isUIAvailable then app.alert("Abra um sprite antes de exportar.") end
  return
end

local function is_guide(layer)
  local name = layer.name or ""
  return string.sub(name, 1, 5) == "GUIDE"
end

-- layer.parent de uma camada raiz é o Sprite, não nil. Parar aí:
-- Sprite não tem isVisible/isGroup, e ler isso quebraria o script.
local function parent_layer(layer)
  local parent = layer.parent
  if not parent or parent == spr then return nil end
  local ok, isGroup = pcall(function() return parent.isGroup end)
  if not ok or not isGroup then return nil end
  return parent
end

local function under_guide(layer)
  local current = layer
  local guard = 0
  while current and guard < 64 do
    if is_guide(current) then return true end
    current = parent_layer(current)
    guard = guard + 1
  end
  return false
end

local function layer_key(layer)
  local parts = {}
  local current = layer
  local guard = 0
  while current and guard < 64 do
    parts[#parts + 1] = tostring(current.stackIndex) .. ":" .. tostring(current.name)
    current = parent_layer(current)
    guard = guard + 1
  end
  return table.concat(parts, "/")
end

local function as_int(value, fallback)
  local n = tonumber(value)
  if not n then return fallback end
  return math.floor(n)
end

-- Lua patterns não têm alternância com "|".
local function strip_ext(path)
  local lower = string.lower(path)
  for _, ext in ipairs({ ".png", ".json", ".aseprite" }) do
    if string.sub(lower, -#ext) == ext then
      return string.sub(path, 1, #path - #ext)
    end
  end
  return path
end

local mode, outBase, columns
local dir = app.fs.filePath(spr.filename)
local title = app.fs.fileTitle(spr.filename)
if title == nil or title == "" then title = "sprite" end

if app.isUIAvailable then
  local dlg = Dialog("Aetherion — Exportar para Unity")
  dlg:combobox{ id = "mode", label = "Modo:", option = "sheet",
    options = { "sheet", "flat", "layers" } }
  dlg:number{ id = "cols", label = "Colunas (sheet):", text = "7", decimals = 0 }
  dlg:entry{ id = "out", label = "Saída (sem extensão):", text = app.fs.joinPath(dir, title) }
  dlg:button{ id = "ok", text = "Exportar", focus = true }
  dlg:button{ id = "cancel", text = "Cancelar" }
  dlg:show()
  if not dlg.data.ok then return end
  mode = dlg.data.mode
  outBase = dlg.data.out
  columns = as_int(dlg.data.cols, 7)
else
  local p = app.params or {}
  mode = p.mode or "sheet"
  outBase = p.out or app.fs.joinPath(dir, title)
  columns = as_int(p.columns, 7)
end

mode = string.lower(tostring(mode or "sheet"))
outBase = strip_ext(tostring(outBase or ""))
if columns < 1 then columns = 1 end

if mode ~= "sheet" and mode ~= "flat" and mode ~= "layers" then
  local msg = "Modo desconhecido: " .. mode .. "\nUse sheet, flat ou layers."
  if app.isUIAvailable then app.alert(msg) else print(msg) end
  return
end

if outBase == "" then
  local msg = "Informe o caminho de saída (sem extensão)."
  if app.isUIAvailable then app.alert(msg) else print(msg) end
  return
end

local function join_out(folder, name)
  if folder == nil or folder == "" then return name end
  return app.fs.joinPath(folder, name)
end

local function safe_layer_filename(name)
  local cleaned = string.gsub(tostring(name or ""), "[\\/:*?\"<>|]", "_")
  if cleaned == "" then cleaned = "layer" end
  return cleaned
end

-- sprite.layers não inclui filhos de grupos.
local snapshot = {}
local visible_by_key = {}

local function collect(layers)
  for _, layer in ipairs(layers) do
    local key = layer_key(layer)
    snapshot[#snapshot + 1] = { layer = layer, visible = layer.isVisible, key = key }
    visible_by_key[key] = layer.isVisible and true or false
    if layer.isGroup then collect(layer.layers) end
  end
end
collect(spr.layers)

local function was_visible(layer)
  local current = layer
  local guard = 0
  while current and guard < 64 do
    if not visible_by_key[layer_key(current)] then return false end
    current = parent_layer(current)
    guard = guard + 1
  end
  return true
end

local function hide_guides()
  for _, item in ipairs(snapshot) do
    if under_guide(item.layer) then item.layer.isVisible = false end
  end
end

local function restore()
  for _, item in ipairs(snapshot) do
    item.layer.isVisible = item.visible
  end
end

local function show_only(target)
  for _, item in ipairs(snapshot) do
    item.layer.isVisible = false
  end
  local current = target
  local guard = 0
  while current and guard < 64 do
    current.isVisible = true
    current = parent_layer(current)
    guard = guard + 1
  end
end

local written = {}

local function do_export()
  hide_guides()

  if mode == "sheet" then
    local ok = app.command.ExportSpriteSheet{
      ui = false,
      askOverwrite = false,
      openGenerated = false,
      type = SpriteSheetType.ROWS,
      columns = columns,
      textureFilename = outBase .. ".png",
      dataFilename = outBase .. ".json",
      dataFormat = SpriteSheetDataFormat.JSON_HASH,
      borderPadding = 0, shapePadding = 0, innerPadding = 0,
      trim = false,
      ignoreEmpty = false,
      mergeDuplicates = false,
      listTags = true,
      listLayers = false,
      listSlices = false,
      splitLayers = false,
    }
    if ok == false then error("ExportSpriteSheet recusou a exportação") end
    written[#written + 1] = outBase .. ".png"
    written[#written + 1] = outBase .. ".json"
  elseif mode == "flat" then
    -- PNG = frame atual, camadas visíveis achatadas. Cenário tem 1 frame.
    spr:saveCopyAs(outBase .. ".png")
    written[#written + 1] = outBase .. ".png"
  else
    local folder = app.fs.filePath(outBase)
    local used = {}
    for _, item in ipairs(snapshot) do
      local layer = item.layer
      if not layer.isGroup and not under_guide(layer) and was_visible(layer) then
        show_only(layer)
        local name = safe_layer_filename(layer.name)
        local unique = name
        local n = 2
        while used[unique] do
          unique = name .. "_" .. n
          n = n + 1
        end
        used[unique] = true
        local file = join_out(folder, unique .. ".png")
        spr:saveCopyAs(file)
        written[#written + 1] = file
      end
    end
  end
end

local ok, err = pcall(do_export)
restore()

if not ok then
  local msg = "Falha ao exportar:\n" .. tostring(err)
  if app.isUIAvailable then app.alert(msg) else print(msg) end
  return
end

if #written == 0 then
  local msg = "Nada para exportar (nenhuma camada visível além das GUIDE)."
  if app.isUIAvailable then app.alert(msg) else print(msg) end
  return
end

local msg = "Exportado (" .. mode .. "):\n" .. table.concat(written, "\n") ..
  "\n\nNo Unity: Tools > Pixel Art > Import Tool (Point, sem compressão)."
if app.isUIAvailable then app.alert(msg) else print(msg) end
