import os
import numpy as np
from PIL import Image
import subprocess

BRAIN_DIR = "/Users/gabriellucas/.gemini/antigravity-ide/brain/75535716-5cfd-46b4-9f91-336328628b2a"
RESOURCES_DIR = "Assets/Resources/GoldenAge"
ART_DIR = "Assets/Art/GoldenAge"
ASEPRITE_BIN = "/Applications/Aseprite.app/Contents/MacOS/aseprite"

os.makedirs(RESOURCES_DIR, exist_ok=True)
os.makedirs(ART_DIR, exist_ok=True)

def make_transparent(im, thresh=18, feather=8):
    im = im.convert("RGBA")
    arr = np.array(im, dtype=np.float32)
    rgb = arr[:, :, :3]
    max_c = np.max(rgb, axis=2)
    alpha = np.clip((max_c - thresh) / max(feather, 1) * 255.0, 0, 255).astype(np.uint8)
    arr[:, :, 3] = alpha
    return Image.fromarray(arr.astype(np.uint8))

def dither_with_aseprite(input_png, output_png, max_w=None, max_h=None):
    cmd = [ASEPRITE_BIN, "-b", input_png]
    if max_w and max_h:
        cmd += ["--shrink-to", f"{max_w},{max_h}"]
    cmd += ["--color-mode", "indexed", "--dithering-algorithm", "ordered", "--dithering-matrix", "bayer4x4", "--save-as", output_png]
    subprocess.run(cmd, check=True)

# 1. Source Images
bg_src = os.path.join(BRAIN_DIR, "fundo_aetherion_16bit_1792099172013.jpg")
props_src = os.path.join(BRAIN_DIR, "props_aetherion_16bit_1792099207346.jpg")
temples_src = os.path.join(BRAIN_DIR, "temples_ground_16bit_1792099244598.jpg")

im_bg = Image.open(bg_src)
im_props = Image.open(props_src)
im_temples = Image.open(temples_src)

print("Extracting Props...")
# --- Runic Block (1x1 in Unity = 32x32 at 16 PPU or 16x16)
# In props_src: x: 640 to 940, y: 35 to 350
crop_rune = im_props.crop((640, 35, 940, 350))
rune_trans = make_transparent(crop_rune, thresh=15, feather=8)
# Save high res and 16-bit indexed at 32x32 (2 units) or 16x16 (1 unit).
# At 16 PPU, 1.0 unit = 16 pixels. We can make it 16x16 or 32x32 with PPU 32.
# Let's resize rune to 32x32 for super clean 16-bit detail
rune_trans_resized = rune_trans.resize((32, 32), Image.Resampling.LANCZOS)
rune_trans_resized.save(os.path.join(RESOURCES_DIR, "rune_block.png"))

# --- Floating Island Tall (width ~ 4.5 units in Unity = ~72 px wide, height ~32 px)
crop_island = im_props.crop((30, 30, 535, 380))
isl_trans = make_transparent(crop_island, thresh=15, feather=8)
isl_tall = isl_trans.resize((80, 56), Image.Resampling.LANCZOS)
isl_tall.save(os.path.join(RESOURCES_DIR, "island_tall.png"))

# --- Floating Island Low (width ~ 3.5 units in Unity = ~56 px wide, height ~26 px)
isl_low = isl_trans.resize((64, 44), Image.Resampling.LANCZOS)
isl_low.save(os.path.join(RESOURCES_DIR, "island_low.png"))

# --- Column Intact & Broken
crop_column = im_props.crop((1080, 35, 1270, 335))
col_trans = make_transparent(crop_column, thresh=15, feather=8)
col_intact = col_trans.resize((24, 64), Image.Resampling.LANCZOS)
col_intact.save(os.path.join(RESOURCES_DIR, "column_intact.png"))

# Broken column: crop top capital
crop_col_broken = col_trans.crop((0, 0, col_trans.width, int(col_trans.height * 0.55)))
col_broken = crop_col_broken.resize((24, 35), Image.Resampling.LANCZOS)
col_broken.save(os.path.join(RESOURCES_DIR, "column_broken.png"))

# --- Stairs Step (width ~ 1.32 units = ~22px, height ~14px)
crop_stairs = im_props.crop((60, 400, 490, 740))
stairs_trans = make_transparent(crop_stairs, thresh=15, feather=8)
stairs_sprite = stairs_trans.resize((48, 40), Image.Resampling.LANCZOS)
stairs_sprite.save(os.path.join(RESOURCES_DIR, "stair_step.png"))

# --- Fountain Basin & Water animation
crop_fountain = im_props.crop((580, 390, 1005, 740))
fount_trans = make_transparent(crop_fountain, thresh=15, feather=8)
fount_sprite = fount_trans.resize((72, 60), Image.Resampling.LANCZOS)
fount_sprite.save(os.path.join(RESOURCES_DIR, "fountain_basin.png"))

# Animated water frames (hue / tint shifted for water sparkle)
w0 = fount_sprite.copy()
w0.save(os.path.join(RESOURCES_DIR, "fountain_water_0.png"))
# frame 1: slight brightness shift
arr_w = np.array(fount_sprite, dtype=np.float32)
arr_w1 = arr_w.copy()
arr_w1[:, :, :3] = np.clip(arr_w1[:, :, :3] * 1.08, 0, 255)
Image.fromarray(arr_w1.astype(np.uint8)).save(os.path.join(RESOURCES_DIR, "fountain_water_1.png"))
# frame 2
arr_w2 = arr_w.copy()
arr_w2[:, :, :3] = np.clip(arr_w2[:, :, :3] * 0.94, 0, 255)
Image.fromarray(arr_w2.astype(np.uint8)).save(os.path.join(RESOURCES_DIR, "fountain_water_2.png"))

# --- Statue on pedestal
crop_statue = im_props.crop((1080, 340, 1340, 740))
statue_trans = make_transparent(crop_statue, thresh=15, feather=8)
statue_sprite = statue_trans.resize((36, 68), Image.Resampling.LANCZOS)
statue_sprite.save(os.path.join(RESOURCES_DIR, "statue.png"))

print("Extracting Temples & Ground...")
# --- West Temple Facade (exclude number '1' on left)
crop_t1 = im_temples.crop((45, 20, 730, 582))
t1_trans = make_transparent(crop_t1, thresh=15, feather=8)
t1_sprite = t1_trans.resize((140, 115), Image.Resampling.LANCZOS)
t1_sprite.save(os.path.join(RESOURCES_DIR, "west_temple.png"))

# --- East Temple Ruins (exclude number '2' on left)
crop_t2 = im_temples.crop((810, 85, 1360, 582))
t2_trans = make_transparent(crop_t2, thresh=15, feather=8)
t2_sprite = t2_trans.resize((120, 110), Image.Resampling.LANCZOS)
t2_sprite.save(os.path.join(RESOURCES_DIR, "east_temple.png"))

# --- Ground / Pavement (seamless continuous ground)
crop_ground = im_temples.crop((45, 620, 1200, 760))
ground_trans = make_transparent(crop_ground, thresh=12, feather=6)
# tile size 64 x 48 (4 units wide x 3 units tall)
ground_tile = ground_trans.resize((256, 48), Image.Resampling.LANCZOS)
ground_tile.save(os.path.join(RESOURCES_DIR, "pavement.png"))

# --- Laje (marble architrave platform)
laje_crop = crop_t1.crop((40, 120, 340, 155))
laje_trans = make_transparent(laje_crop, thresh=15, feather=8)
laje_sprite = laje_trans.resize((56, 14), Image.Resampling.LANCZOS)
laje_sprite.save(os.path.join(RESOURCES_DIR, "laje.png"))

# --- Cypress & Bush from background
# In im_bg: cypresses around x=980 to 1060, y=520 to 710
crop_cyp = im_bg.crop((980, 520, 1060, 710))
cyp_trans = make_transparent(crop_cyp, thresh=25, feather=10)
cyp_sprite = cyp_trans.resize((24, 64), Image.Resampling.LANCZOS)
cyp_sprite.save(os.path.join(RESOURCES_DIR, "cypress.png"))

# Bush / flowers around x=610 to 670, y=670 to 720
crop_bush = im_bg.crop((610, 670, 670, 720))
bush_trans = make_transparent(crop_bush, thresh=25, feather=10)
bush_sprite = bush_trans.resize((32, 18), Image.Resampling.LANCZOS)
bush_sprite.save(os.path.join(RESOURCES_DIR, "bush.png"))

# --- Soft Shadows
# Island shadow: elliptical dark shadow with soft alpha
shadow_isl = Image.new("RGBA", (64, 16), (0, 0, 0, 0))
arr_sh = np.zeros((16, 64, 4), dtype=np.uint8)
for y in range(16):
    for x in range(64):
        nx = (x - 31.5) / 31.5
        ny = (y - 7.5) / 7.5
        dist = nx * nx + ny * ny
        if dist <= 1.0:
            a = int((1.0 - dist) * 110)
            arr_sh[y, x] = [15, 12, 28, a]
Image.fromarray(arr_sh).save(os.path.join(RESOURCES_DIR, "shadow_island.png"))

# Hero shadow
hero_sh = Image.new("RGBA", (32, 10), (0, 0, 0, 0))
arr_hsh = np.zeros((10, 32, 4), dtype=np.uint8)
for y in range(10):
    for x in range(32):
        nx = (x - 15.5) / 15.5
        ny = (y - 4.5) / 4.5
        dist = nx * nx + ny * ny
        if dist <= 1.0:
            a = int((1.0 - dist) * 130)
            arr_hsh[y, x] = [15, 12, 28, a]
Image.fromarray(arr_hsh).save(os.path.join(RESOURCES_DIR, "hero_shadow.png"))

print("Extracting Parallax Background Layers...")
# 1. Sky layer (sky, celestial ringed planets, sun, sunbeams)
# Top 50% of im_bg
crop_sky = im_bg.crop((0, 0, 1376, 420))
sky_resized = crop_sky.resize((688, 210), Image.Resampling.LANCZOS)
sky_resized.save(os.path.join(RESOURCES_DIR, "sky.png"))

# 2. Clouds layer (soft clouds drift)
crop_clouds = im_bg.crop((0, 80, 1376, 320))
# make sky color transparent
clouds_trans = crop_clouds.convert("RGBA")
arr_c = np.array(clouds_trans)
# cloud mask: high brightness, low saturation
clouds_trans.resize((512, 90), Image.Resampling.LANCZOS).save(os.path.join(RESOURCES_DIR, "clouds.png"))

# 3. Mountains layer (emerald mountains, rock peak with glowing Acropolis, spires, floating crystals)
crop_mountains = im_bg.crop((0, 180, 1376, 560))
mount_resized = crop_mountains.resize((688, 190), Image.Resampling.LANCZOS)
mount_resized.save(os.path.join(RESOURCES_DIR, "mountains.png"))

# 4. Forest layer (whimsical alien forest, giant purple/teal mushrooms)
crop_forest = im_bg.crop((0, 420, 1376, 680))
forest_resized = crop_forest.resize((688, 130), Image.Resampling.LANCZOS)
forest_resized.save(os.path.join(RESOURCES_DIR, "forest.png"))

# 5. Colonnade layer (distant marble colonnades in forest)
crop_col = im_bg.crop((680, 520, 1300, 680))
col_trans2 = make_transparent(crop_col, thresh=25, feather=10)
col_resized = col_trans2.resize((310, 80), Image.Resampling.LANCZOS)
col_resized.save(os.path.join(RESOURCES_DIR, "colonnade.png"))

print("Saving native .aseprite project files for user...")
# Convert to .aseprite so user can inspect and edit directly in Aseprite
subprocess.run([ASEPRITE_BIN, "-b", os.path.join(RESOURCES_DIR, "sky.png"), "--save-as", os.path.join(ART_DIR, "cenario_aetherion_16bit.aseprite")], check=True)
subprocess.run([ASEPRITE_BIN, "-b", os.path.join(RESOURCES_DIR, "rune_block.png"), "--save-as", os.path.join(ART_DIR, "bloco_runico.aseprite")], check=True)
subprocess.run([ASEPRITE_BIN, "-b", os.path.join(RESOURCES_DIR, "island_tall.png"), "--save-as", os.path.join(ART_DIR, "ilha_flutuante.aseprite")], check=True)
subprocess.run([ASEPRITE_BIN, "-b", os.path.join(RESOURCES_DIR, "west_temple.png"), "--save-as", os.path.join(ART_DIR, "templo_oeste.aseprite")], check=True)

print("All 16-bit sprites and Aseprite projects generated successfully!")
