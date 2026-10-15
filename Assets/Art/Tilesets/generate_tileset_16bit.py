import os
import subprocess
import numpy as np
from PIL import Image

OUT_DIR = "Assets/Resources/Tilesets"
ART_DIR = "Assets/Art/Tilesets"
ASEPRITE_BIN = "/Applications/Aseprite.app/Contents/MacOS/aseprite"

os.makedirs(OUT_DIR, exist_ok=True)
os.makedirs(ART_DIR, exist_ok=True)

# Tile size: 32x32 pixels (at 16 PPU, each tile is 2x2 units, or at 32 PPU it is 1x1 unit)
TS = 32

# Palette definition (Authentic 16-bit SNES)
C_TRANS = (0, 0, 0, 0)

# Grass tones
G_H = (155, 230, 66, 255)
G_M = (72, 178, 42, 255)
G_D = (36, 109, 27, 255)
G_O = (18, 56, 18, 255)

# Soil tones
S_H = (158, 107, 69, 255)
S_M = (109, 69, 40, 255)
S_D = (72, 42, 23, 255)
S_O = (38, 20, 11, 255)

# Marble tones
M_H = (255, 255, 255, 255)
M_L = (235, 235, 242, 255)
M_M = (195, 198, 212, 255)
M_D = (145, 148, 168, 255)
M_O = (78, 78, 98, 255)

# Gold Rune tones
GOLD_H = (255, 238, 110, 255)
GOLD_M = (225, 165, 25, 255)
GOLD_D = (140, 95, 8, 255)

# Flower pink/cyan
FL_P1 = (255, 110, 175, 255)
FL_P2 = (210, 45, 120, 255)
FL_C1 = (95, 235, 255, 255)
FL_Y1 = (255, 235, 80, 255)

def draw_top_grass(arr, y_start=0, y_end=12, has_left_edge=False, has_right_edge=False):
    for y in range(y_start, y_end):
        for x in range(TS):
            if has_left_edge and x < 4:
                if y < 4 - x: continue
            if has_right_edge and x >= TS - 4:
                if y < (x - (TS - 5)): continue
            
            # Grass blades variation
            wave = int(np.sin(x * 0.8) * 2 + np.cos(x * 1.5))
            if y == y_start:
                if (x % 3 == 0 or x % 5 == 0) and not (has_left_edge and x < 2) and not (has_right_edge and x > TS - 3):
                    arr[y, x] = G_H
            elif y < y_start + 4 + wave:
                arr[y, x] = G_H if (x + y) % 2 == 0 else G_M
            elif y < y_start + 8 + wave:
                arr[y, x] = G_M if (x * y) % 3 != 0 else G_D
            else:
                arr[y, x] = G_D if (x + y) % 2 == 0 else G_O

def draw_soil_fill(arr, y_start=12, y_end=TS, left_margin=0, right_margin=TS):
    for y in range(y_start, y_end):
        for x in range(left_margin, right_margin):
            # Organic dirt texture with dithering
            noise = (x * 17 + y * 31 + (x ^ y) * 7) % 19
            if noise < 3:
                arr[y, x] = S_H  # pebble highlight
            elif noise < 6:
                arr[y, x] = S_D  # pebble shadow
            elif noise < 14:
                arr[y, x] = S_M  # base dirt
            else:
                # Bayer-style ordered dither
                dither = ((x % 2) ^ (y % 2))
                arr[y, x] = S_M if dither == 0 else S_D

def draw_flower(arr, cx, cy, col1, col2):
    if 2 <= cx < TS - 2 and 2 <= cy < TS - 2:
        arr[cy, cx] = col1
        arr[cy-1, cx] = col2
        arr[cy+1, cx] = col2
        arr[cy, cx-1] = col2
        arr[cy, cx+1] = col2
        arr[cy+2, cx] = G_D  # stem

# Create a 8x8 tileset grid (256x256 px)
grid = np.zeros((TS * 8, TS * 8, 4), dtype=np.uint8)

def get_tile_slice(col, row):
    return grid[row * TS : (row + 1) * TS, col * TS : (col + 1) * TS]

# --- Row 0: Ground Top (Left Edge, Center, Right Edge, Lone Island Top)
# 0,0: Top-Left Grass Corner
tl = get_tile_slice(0, 0)
draw_top_grass(tl, y_start=2, y_end=12, has_left_edge=True)
draw_soil_fill(tl, y_start=12, y_end=TS, left_margin=4, right_margin=TS)
for y in range(4, TS):
    tl[y, 3] = S_O
    tl[y, 4] = S_D

# 0,1: Top-Center Grass (Tileable)
tc = get_tile_slice(1, 0)
draw_top_grass(tc, y_start=2, y_end=12)
draw_soil_fill(tc, y_start=12, y_end=TS)
draw_flower(tc, 8, 5, FL_P1, FL_P2)
draw_flower(tc, 22, 6, FL_Y1, FL_C1)

# 0,2: Top-Right Grass Corner
tr = get_tile_slice(2, 0)
draw_top_grass(tr, y_start=2, y_end=12, has_right_edge=True)
draw_soil_fill(tr, y_start=12, y_end=TS, left_margin=0, right_margin=TS-4)
for y in range(4, TS):
    tr[y, TS - 4] = S_D
    tr[y, TS - 3] = S_O

# 0,3: Thin Grass One-Way Platform (Tileable Center)
plat_c = get_tile_slice(3, 0)
draw_top_grass(plat_c, y_start=2, y_end=10)
for x in range(TS):
    plat_c[10, x] = S_D
    plat_c[11, x] = S_O
    # small hanging roots
    if x in (4, 12, 19, 27):
        plat_c[12, x] = S_M
        plat_c[13, x] = S_O
    if x in (13, 20):
        plat_c[14, x] = S_O

# --- Row 1: Soil Body (Left Wall, Center Fill, Right Wall, Thin Platform Left/Right Ends)
# 1,0: Mid-Left Wall
ml = get_tile_slice(0, 1)
draw_soil_fill(ml, y_start=0, y_end=TS, left_margin=4, right_margin=TS)
for y in range(TS):
    ml[y, 3] = S_O
    ml[y, 4] = S_D

# 1,1: Mid-Center Soil (Tileable Full Fill)
mc = get_tile_slice(1, 1)
draw_soil_fill(mc, y_start=0, y_end=TS)
# add fossil/root detail
mc[10:14, 12:16] = [[M_D, M_M, M_M, M_D],
                    [M_M, M_H, M_L, M_M],
                    [M_M, M_L, M_H, M_M],
                    [M_O, M_D, M_D, M_O]]

# 1,2: Mid-Right Wall
mr = get_tile_slice(2, 1)
draw_soil_fill(mr, y_start=0, y_end=TS, left_margin=0, right_margin=TS-4)
for y in range(TS):
    mr[y, TS - 4] = S_D
    mr[y, TS - 3] = S_O

# --- Row 2: Bottom Soil Edges
# 2,0: Bot-Left
bl = get_tile_slice(0, 2)
draw_soil_fill(bl, y_start=0, y_end=TS-4, left_margin=4, right_margin=TS)
for x in range(4, TS):
    bl[TS-4, x] = S_D
    bl[TS-3, x] = S_O

# 2,1: Bot-Center
bc = get_tile_slice(1, 2)
draw_soil_fill(bc, y_start=0, y_end=TS-4)
for x in range(TS):
    bc[TS-4, x] = S_D
    bc[TS-3, x] = S_O
    if x % 6 == 0:
        bc[TS-2, x] = S_O

# 2,2: Bot-Right
br = get_tile_slice(2, 2)
draw_soil_fill(br, y_start=0, y_end=TS-4, left_margin=0, right_margin=TS-4)
for x in range(0, TS-4):
    br[TS-4, x] = S_D
    br[TS-3, x] = S_O

# --- Row 3: Aetherion Classical Architecture (Rune Block, Column, Architrave, Stairs)
# 3,0: Runic Marble Block 1x1
rune = get_tile_slice(0, 3)
# Outer beveled stone border
for y in range(TS):
    for x in range(TS):
        if x == 0 or y == 0:
            rune[y, x] = M_H
        elif x == TS - 1 or y == TS - 1:
            rune[y, x] = M_O
        elif x < 3 or y < 3:
            rune[y, x] = M_L
        elif x >= TS - 3 or y >= TS - 3:
            rune[y, x] = M_D
        else:
            rune[y, x] = M_M

# Golden Diamond Rune in center
cx, cy = 16, 16
for y in range(4, TS - 4):
    for x in range(4, TS - 4):
        dist = abs(x - cx) + abs(y - cy)
        if dist <= 8:
            if dist == 8:
                rune[y, x] = GOLD_D
            elif dist == 7:
                rune[y, x] = GOLD_H
            elif dist >= 5:
                rune[y, x] = GOLD_M
            elif dist == 4:
                rune[y, x] = GOLD_H
            elif dist <= 3:
                rune[y, x] = GOLD_M

# Ivy in corner
for (ix, iy) in [(20, 26), (21, 27), (22, 26), (23, 28), (24, 27), (25, 28), (26, 29), (27, 28)]:
    if ix < TS and iy < TS:
        rune[iy, ix] = G_M
        if iy + 1 < TS: rune[iy+1, ix] = G_D

# 3,1: Column Capital (Ionic / Corinthian)
col_cap = get_tile_slice(1, 3)
for y in range(TS):
    for x in range(4, TS - 4):
        if y < 8:
            col_cap[y, x] = M_H if y == 0 or x == 4 else (M_O if y == 7 or x == TS - 5 else M_L)
        else:
            # fluting lines
            flute = (x - 4) % 6
            if flute == 0: col_cap[y, x] = M_H
            elif flute == 5: col_cap[y, x] = M_D
            else: col_cap[y, x] = M_L

# 3,2: Column Shaft (Tileable Vertically)
col_shaft = get_tile_slice(2, 3)
for y in range(TS):
    for x in range(6, TS - 6):
        flute = (x - 6) % 5
        if flute == 0: col_shaft[y, x] = M_H
        elif flute == 4: col_shaft[y, x] = M_D
        else: col_shaft[y, x] = M_L

# 3,3: Column Base
col_base = get_tile_slice(3, 3)
for y in range(TS):
    for x in range(4, TS - 4):
        if y > TS - 8:
            col_base[y, x] = M_H if y == TS - 8 else (M_O if y == TS - 1 else M_L)
        else:
            flute = (x - 4) % 6
            if flute == 0: col_base[y, x] = M_H
            elif flute == 5: col_base[y, x] = M_D
            else: col_base[y, x] = M_L

# 3,4: Stair Step (Ascending marble step)
stair = get_tile_slice(4, 3)
for y in range(TS):
    for x in range(TS):
        step_diag = x - (TS - y)
        if step_diag >= 0:
            if step_diag == 0 or y == 0 or x == TS - 1:
                stair[y, x] = M_H
            elif step_diag == 1:
                stair[y, x] = M_L
            elif y == TS - 1:
                stair[y, x] = M_O
            else:
                stair[y, x] = M_M

# Save Master Tileset PNG
tileset_img = Image.fromarray(grid)
tileset_path = os.path.join(OUT_DIR, "aetherion_tileset.png")
tileset_img.save(tileset_path)

# Convert to native .aseprite project file for the user
aseprite_path = os.path.join(ART_DIR, "aetherion_tileset.aseprite")
subprocess.run([ASEPRITE_BIN, "-b", tileset_path, "--save-as", aseprite_path], check=True)

print("Tileset PNG and Aseprite created successfully at:")
print(" -", tileset_path)
print(" -", aseprite_path)
