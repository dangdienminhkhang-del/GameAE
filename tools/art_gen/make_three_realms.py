"""make_three_realms.py
Tạo bộ Pixel Art tileset & props tuyệt đẹp cho 3 chủ đề thế giới:
1. Rừng Rậm Cổ Đại (Ancient Forest) - Emerald, thảo nguyên xanh ngát, đền linh hồn
2. Hầm Mỏ Dung Nham (Lava Mines) - Basalt núi lửa, dung nham rực cháy, đe rèn
3. Lâu Đài Bóng Tối (Dark Castle) - Đá Gothic, thảm nhung đỏ thêu vàng, vực thẳm hư vô

Chạy: python tools/art_gen/make_three_realms.py
Đầu ra: Assets/_Game/Art/Environment/AncientForest/
        Assets/_Game/Art/Environment/LavaMines/
        Assets/_Game/Art/Environment/DarkCastle/
"""
import os
import math
import numpy as np
from PIL import Image

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(os.path.dirname(BASE_DIR))
ART_ENV_DIR = os.path.join(PROJECT_ROOT, "Assets", "_Game", "Art", "Environment")

FOREST_DIR = os.path.join(ART_ENV_DIR, "AncientForest")
LAVA_DIR = os.path.join(ART_ENV_DIR, "LavaMines")
CASTLE_DIR = os.path.join(ART_ENV_DIR, "DarkCastle")

for d in [FOREST_DIR, LAVA_DIR, CASTLE_DIR]:
    os.makedirs(d, exist_ok=True)

def hexc(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))

def blank(w, h):
    return np.zeros((h, w, 4), dtype=np.uint8)

def save_img(arr, path):
    img = Image.fromarray(arr, 'RGBA')
    img.save(path)
    print(f"Saved: {os.path.basename(path)}")

# ══════════════════════════════════════════════════════════════════════════════
# 1. RỪNG RẬM CỔ ĐẠI (ANCIENT FOREST)
# ══════════════════════════════════════════════════════════════════════════════

def make_forest_grass():
    # 32x32 mượt mà, màu xanh ngọc bích đậm đà, điểm xuyết hoa nhỏ và cụm cỏ mềm
    img = blank(32, 32)
    c_base = np.array([32, 75, 42], dtype=np.float32)       # #204b2a
    c_light = np.array([48, 108, 58], dtype=np.float32)     # #306c3a
    c_dark = np.array([22, 54, 30], dtype=np.float32)       # #16361e
    
    # Tạo nền cỏ bằng perlin/simplex giả lập mềm mại
    for y in range(32):
        for x in range(32):
            noise = math.sin(x * 0.35) * math.cos(y * 0.35) * 0.5 + math.sin((x+y)*0.2) * 0.3
            t = np.clip(0.5 + noise, 0.0, 1.0)
            col = c_dark * (1 - t) + c_light * t
            img[y, x] = [int(col[0]), int(col[1]), int(col[2]), 255]
            
    # Cụm cỏ tỉa mềm mại (tufts)
    tufts = [(7, 8), (22, 6), (14, 18), (27, 22), (5, 25)]
    for tx, ty in tufts:
        for ox, oy, cl in [(-1, 0, [58, 125, 68]), (0, 0, [72, 148, 82]), (1, 0, [58, 125, 68]),
                           (0, -1, [72, 148, 82]), (0, -2, [86, 172, 98]), (-1, -1, [58, 125, 68])]:
            px, py = tx + ox, ty + oy
            if 0 <= px < 32 and 0 <= py < 32:
                img[py, px] = [*cl, 255]

    # Hoa nhỏ rừng rậm vàng & xanh lơ điểm xuyết
    flowers = [(11, 10, [240, 215, 80]), (25, 14, [100, 180, 255]), (18, 28, [240, 215, 80])]
    for fx, fy, fcol in flowers:
        img[fy, fx] = [*fcol, 255]
        img[fy+1, fx] = [20, 50, 25, 255]
        
    return img

def make_forest_path():
    # Đường đất cổ có lát các phiến đá cuội bo góc và viền cỏ ven đường
    img = blank(32, 32)
    c_dirt = [95, 72, 54, 255]       # Đất ấm
    c_dirt_d = [68, 50, 36, 255]     # Vết nứt đất
    c_stone = [135, 120, 105, 255]   # Đá cuội
    c_stone_h = [165, 150, 135, 255] # Viền sáng đá
    c_moss = [55, 105, 50, 255]      # Rêu bám
    
    # Nền đất
    for y in range(32):
        for x in range(32):
            val = c_dirt if (x * 3 + y * 7) % 5 != 0 else c_dirt_d
            img[y, x] = val

    # Các phiến đá cuội tự nhiên bo góc
    stones = [
        (4, 5, 8, 6),
        (18, 4, 10, 7),
        (8, 16, 12, 8),
        (22, 19, 7, 7),
        (2, 24, 9, 6)
    ]
    for sx, sy, sw, sh in stones:
        for y in range(sy, sy + sh):
            for x in range(sx, sx + sw):
                if 0 <= x < 32 and 0 <= y < 32:
                    # bo 4 góc
                    if (x == sx or x == sx+sw-1) and (y == sy or y == sy+sh-1):
                        continue
                    if y == sy or x == sx:
                        img[y, x] = c_stone_h # sáng trên trái
                    elif y == sy+sh-1 or x == sx+sw-1:
                        img[y, x] = c_dirt_d  # bóng dưới phải
                    else:
                        img[y, x] = c_stone

    # Chấm rêu xanh ở kẽ đá
    for rx, ry in [(12, 11), (20, 12), (15, 24), (27, 26)]:
        if 0 <= rx < 32 and 0 <= ry < 32:
            img[ry, rx] = c_moss

    return img

def make_forest_tree_wall():
    # Tường Rừng Cây Cổ Thụ: Tán lá rậm rạp tầng tầng lớp lớp màu xanh lục sâu thẳm
    img = blank(32, 32)
    # Tán lá hình vòm đầy đặn
    c_deep = [16, 42, 24, 255]
    c_mid = [28, 78, 42, 255]
    c_light = [48, 122, 64, 255]
    c_high = [75, 175, 90, 255]

    for y in range(32):
        for x in range(32):
            img[y, x] = c_deep

    # Vẽ các khối tán lá tròn (foliage clusters)
    clusters = [
        (8, 8, 8),
        (23, 7, 9),
        (16, 15, 10),
        (6, 23, 8),
        (24, 24, 8)
    ]
    for cx, cy, rad in clusters:
        for y in range(max(0, cy - rad), min(32, cy + rad + 1)):
            for x in range(max(0, cx - rad), min(32, cx + rad + 1)):
                d = np.hypot(x - cx, y - cy)
                if d <= rad:
                    if y < cy - 2 and d <= rad - 2:
                        img[y, x] = c_high
                    elif y < cy:
                        img[y, x] = c_light
                    elif d <= rad - 2:
                        img[y, x] = c_mid
                    else:
                        img[y, x] = c_deep

    return img

def make_forest_water():
    # Sông suối rừng ngọc bích trong veo với sóng nước lung linh
    img = blank(32, 32)
    c_deep = [14, 52, 68, 255]
    c_mid = [24, 86, 108, 255]
    c_light = [42, 138, 168, 255]
    c_foam = [165, 230, 245, 255]

    for y in range(32):
        for x in range(32):
            w = math.sin(x * 0.4 + y * 0.3) * 0.5 + math.cos(x * 0.2 - y * 0.4) * 0.5
            if w > 0.4:
                img[y, x] = c_light
            elif w < -0.3:
                img[y, x] = c_deep
            else:
                img[y, x] = c_mid

    # Bọt sóng trắng lăn tăn
    ripples = [(6, 7), (7, 7), (8, 7), (20, 16), (21, 16), (22, 16), (12, 25), (13, 25)]
    for rx, ry in ripples:
        img[ry, rx] = c_foam

    return img

def make_forest_prop_shrine():
    # Suối Sinh Mệnh Cổ Đại: Tháp đá cổ kính với viên ngọc pha lê phát sáng xanh cyan
    img = blank(32, 32)
    c_stone = [100, 95, 90, 255]
    c_stone_d = [60, 55, 52, 255]
    c_stone_l = [150, 145, 140, 255]
    c_crystal = [60, 230, 210, 255]
    c_glow = [180, 255, 245, 255]

    # Bệ đá tròn chân tháp
    for y in range(24, 30):
        for x in range(5, 27):
            if (y == 24 and (x < 7 or x > 24)) or (y == 29 and (x < 6 or x > 25)): continue
            img[y, x] = c_stone_l if y == 24 else (c_stone if y < 28 else c_stone_d)

    # Thân cột tháp
    for y in range(8, 24):
        for x in range(10, 22):
            if x == 10: img[y, x] = c_stone_l
            elif x == 21: img[y, x] = c_stone_d
            else: img[y, x] = c_stone

    # Đỉnh tháp mái vòm
    for y in range(4, 9):
        dx = abs(y - 8)
        for x in range(10 - dx, 22 + dx):
            if 0 <= x < 32: img[y, x] = c_stone_l if y < 6 else c_stone

    # Viên ngọc Sinh Mệnh ở giữa phát quang
    for y in range(13, 19):
        for x in range(13, 19):
            img[y, x] = c_crystal
    img[15, 15] = c_glow
    img[15, 16] = c_glow
    img[16, 15] = c_glow

    return img

def make_forest_prop_thorn():
    # Bụi Gai Rừng Rậm độc hại (Thorn Hazard)
    img = blank(32, 32)
    c_wood = [75, 45, 28, 255]
    c_thorn = [190, 45, 75, 255]
    c_leaf = [35, 85, 45, 255]

    # Dây gai uốn lượn
    for t in np.linspace(0, 1, 80):
        x = int(16 + 12 * math.cos(t * math.pi * 3) * (1 - t*0.3))
        y = int(28 - 20 * t + 4 * math.sin(t * math.pi * 4))
        if 0 <= x < 32 and 0 <= y < 32:
            img[y, x] = c_wood
            if x+1 < 32: img[y, x+1] = c_wood
            # Gai nhọn đâm ra
            if int(t * 10) % 2 == 0:
                gx = x + int(3 * math.cos(t * 10))
                gy = y + int(3 * math.sin(t * 10))
                if 0 <= gx < 32 and 0 <= gy < 32:
                    img[gy, gx] = c_thorn
            else:
                lx = x - 2
                if 0 <= lx < 32: img[y, lx] = c_leaf

    return img

def make_forest_prop_mushroom():
    # Nấm Dạ Quang Rực Rỡ (Bioluminescent Mushrooms)
    img = blank(32, 32)
    c_stem = [220, 225, 230, 255]
    c_cap = [40, 210, 200, 255]
    c_cap_h = [160, 255, 240, 255]
    c_cap_p = [180, 50, 210, 255] # Nấm tím nhỏ

    # Nấm lớn màu cyan
    for y in range(16, 28):
        img[y, 15] = c_stem
        img[y, 16] = c_stem

    for y in range(9, 17):
        rad = int(7 * math.sin((y - 9) / 8 * math.pi))
        for x in range(15 - rad, 16 + rad + 1):
            img[y, x] = c_cap_h if y < 12 else c_cap

    # Đốm sáng trên mũ nấm
    img[11, 14] = [255, 255, 255, 255]
    img[12, 17] = [255, 255, 255, 255]

    # Nấm tím nhỏ bên cạnh
    for y in range(21, 28):
        img[y, 23] = c_stem
    for y in range(17, 22):
        rad = int(4 * math.sin((y - 17) / 5 * math.pi))
        for x in range(23 - rad, 23 + rad + 1):
            img[y, x] = c_cap_p

    return img


# ══════════════════════════════════════════════════════════════════════════════
# 2. HẦM MỎ DUNG NHAM (LAVA MINES)
# ══════════════════════════════════════════════════════════════════════════════

def make_lava_basalt():
    # Đá Basalt núi lửa sẫm màu với các đường rãnh dung nham rực lửa
    img = blank(32, 32)
    c_rock = [42, 36, 42, 255]
    c_rock_d = [26, 22, 28, 255]
    c_rock_l = [62, 54, 62, 255]
    c_glow = [255, 90, 20, 255]

    for y in range(32):
        for x in range(32):
            img[y, x] = c_rock if (x + y) % 3 != 0 else c_rock_l

    # Các rãnh nứt giữa các tảng đá basalt
    for y in range(32):
        img[y, 15] = c_rock_d
        img[y, 16] = c_rock_d
    for x in range(32):
        img[15, x] = c_rock_d
        img[16, x] = c_rock_d

    # Đốm than hồng phát sáng trong kẽ nứt
    for ex, ey in [(15, 8), (16, 22), (6, 16), (25, 15)]:
        img[ey, ex] = c_glow

    return img

def make_lava_ash():
    # Đường tro bụi khoáng thạch xám sẫm
    img = blank(32, 32)
    c_ash1 = [58, 52, 54, 255]
    c_ash2 = [48, 42, 45, 255]
    c_spark = [255, 140, 30, 255]

    for y in range(32):
        for x in range(32):
            v = c_ash1 if (x * 5 + y * 7) % 4 != 0 else c_ash2
            img[y, x] = v

    # Hạt than hồng li ti
    for sx, sy in [(9, 12), (21, 24), (17, 6), (28, 18)]:
        img[sy, sx] = c_spark

    return img

def make_lava_magma():
    # Hồ Dung Nham sôi sùng sục rực rỡ màu cam đỏ và vàng chói
    img = blank(32, 32)
    c_deep = [170, 35, 10, 255]
    c_mid = [245, 90, 15, 255]
    c_hot = [255, 195, 30, 255]
    c_core = [255, 250, 180, 255]
    c_crust = [70, 25, 15, 255]

    for y in range(32):
        for x in range(32):
            val = math.sin(x * 0.3) * math.cos(y * 0.3) + math.sin((x + y) * 0.25)
            if val > 0.8:
                img[y, x] = c_core
            elif val > 0.3:
                img[y, x] = c_hot
            elif val > -0.4:
                img[y, x] = c_mid
            else:
                img[y, x] = c_deep

    # Vài mảng vỏ đá mỏng trôi trên bề mặt
    for cx, cy in [(6, 8), (7, 8), (22, 24), (23, 24), (22, 25)]:
        img[cy, cx] = c_crust

    return img

def make_lava_rock_wall():
    # Tường Đá Núi Lửa Vách Đứng
    img = blank(32, 32)
    c_dark = [24, 20, 24, 255]
    c_mid = [45, 38, 45, 255]
    c_light = [72, 60, 70, 255]
    c_vein = [225, 65, 15, 255]

    for y in range(32):
        for x in range(32):
            img[y, x] = c_dark

    # Các gờ đá sắc cạnh
    for y in range(4, 28):
        for x in range(2, 30):
            if (x + y) % 7 == 0:
                img[y, x] = c_light
            elif (x + y) % 7 == 1:
                img[y, x] = c_mid

    # Mạch dung nham chảy dọc theo vách nứt
    vein_pts = [(12, 4), (13, 8), (14, 12), (13, 16), (15, 20), (16, 25), (15, 30)]
    for vx, vy in vein_pts:
        img[vy, vx] = c_vein
        if vx+1 < 32: img[vy, vx+1] = [255, 150, 40, 255]

    return img

def make_lava_prop_anvil():
    # Đe Rèn Núi Lửa Cổ (Molten Forge Anvil) - buff 35% damage
    img = blank(32, 32)
    c_iron = [95, 95, 105, 255]
    c_iron_d = [50, 50, 60, 255]
    c_glow = [255, 120, 20, 255]
    c_gold = [255, 215, 0, 255]

    # Bệ đá nung
    for y in range(22, 29):
        for x in range(6, 26):
            img[y, x] = c_iron_d

    # Thân đe
    for y in range(14, 22):
        for x in range(11, 21):
            img[y, x] = c_iron

    # Mặt đe rèn
    for y in range(9, 14):
        for x in range(4, 28):
            img[y, x] = c_iron

    # Thanh kiếm đỏ rực đang được rèn trên mặt đe
    for x in range(8, 24):
        img[8, x] = c_glow
        img[7, x] = c_gold if 12 <= x <= 18 else c_glow

    return img

def make_lava_prop_geyser():
    # Miệng Phun Nhiệt Dung Nham (Geyser Hazard)
    img = blank(32, 32)
    c_crater = [35, 30, 35, 255]
    c_magma = [255, 80, 15, 255]
    c_smoke = [160, 140, 150, 200]

    # Miệng hố hình phễu
    for y in range(16, 28):
        rad = int(10 * math.sin((y - 16) / 12 * math.pi))
        for x in range(16 - rad, 16 + rad + 1):
            img[y, x] = c_crater if (x == 16 - rad or x == 16 + rad) else c_magma

    # Cột khói bốc lên phía trên
    smoke_pts = [(16, 12), (15, 9), (17, 7), (14, 5), (18, 3)]
    for sx, sy in smoke_pts:
        img[sy, sx] = c_smoke
        img[sy, sx+1] = c_smoke

    return img

def make_lava_prop_ore_cart():
    # Xe Goòng Chở Quặng Pha Lê Lửa
    img = blank(32, 32)
    c_wood = [80, 52, 35, 255]
    c_metal = [130, 130, 140, 255]
    c_crystal = [255, 50, 40, 255]
    c_crystal_h = [255, 180, 80, 255]

    # Bánh xe kim loại
    for bx in [9, 22]:
        for y in range(24, 28):
            for x in range(bx-2, bx+3):
                img[y, x] = c_metal

    # Thùng xe goòng
    for y in range(14, 24):
        for x in range(5, 27):
            img[y, x] = c_metal if (y == 23 or x == 5 or x == 26) else c_wood

    # Quặng pha lê đỏ đầy ắp trong thùng
    crystals = [(10, 11), (14, 9), (17, 12), (21, 10), (13, 13), (18, 14)]
    for cx, cy in crystals:
        img[cy, cx] = c_crystal_h
        img[cy+1, cx] = c_crystal
        img[cy, cx+1] = c_crystal

    return img


# ══════════════════════════════════════════════════════════════════════════════
# 3. LÂU ĐÀI BÓNG TỐI (DARK CASTLE)
# ══════════════════════════════════════════════════════════════════════════════

def make_castle_cobble():
    # Sàn đá lâu đài Gothic màu chàm đen trang nhã, viền gạch tinh xảo
    img = blank(32, 32)
    c_tile = [38, 36, 52, 255]
    c_tile_l = [56, 54, 76, 255]
    c_mortar = [18, 16, 25, 255]

    for y in range(32):
        for x in range(32):
            img[y, x] = c_tile

    # Vạch vữa chia ô gạch 16x16
    for y in range(32):
        img[y, 0] = c_mortar
        img[y, 15] = c_mortar
        img[y, 16] = c_mortar
        img[y, 31] = c_mortar
    for x in range(32):
        img[0, x] = c_mortar
        img[15, x] = c_mortar
        img[16, x] = c_mortar
        img[31, x] = c_mortar

    # Viền nổi gạch đá
    for y in [1, 17]:
        for x in range(y, y+14):
            if x < 32 and y < 32:
                img[y, x] = c_tile_l

    return img

def make_castle_carpet():
    # Thảm nhung đỏ hoàng gia viền thêu chỉ vàng quý phái
    img = blank(32, 32)
    c_velvet = [118, 22, 42, 255]
    c_velvet_d = [88, 14, 30, 255]
    c_gold = [225, 178, 48, 255]

    for y in range(32):
        for x in range(32):
            img[y, x] = c_velvet if (x + y) % 4 != 0 else c_velvet_d

    # Viền chỉ vàng ở mép trên và dưới
    for x in range(32):
        img[1, x] = c_gold
        img[2, x] = c_gold
        img[29, x] = c_gold
        img[30, x] = c_gold

    return img

def make_castle_wall():
    # Tường lâu đài Gothic bằng đá tảng kiên cố có hoa văn cửa tò vò
    img = blank(32, 32)
    c_stone = [45, 42, 58, 255]
    c_stone_d = [25, 22, 34, 255]
    c_stone_l = [72, 68, 92, 255]
    c_void_inside = [12, 10, 18, 255]

    for y in range(32):
        for x in range(32):
            img[y, x] = c_stone

    # Các đường vữa đá
    for y in [0, 10, 20, 31]:
        for x in range(32):
            img[y, x] = c_stone_d

    for y in range(1, 10):
        img[y, 15] = c_stone_d
    for y in range(11, 20):
        img[y, 7] = c_stone_d
        img[y, 23] = c_stone_d

    # Cửa sổ tò vò hẹp ở giữa
    for y in range(8, 24):
        for x in range(14, 18):
            img[y, x] = c_void_inside

    return img

def make_castle_void():
    # Vực Thẳm Hư Vô (Void Chasm) - Đen tím bí ẩn với bụi sao lấp lánh
    img = blank(32, 32)
    c_abyss = [12, 10, 22, 255]
    c_purple = [45, 24, 68, 255]
    c_nebula = [115, 60, 175, 255]
    c_star = [225, 195, 255, 255]

    for y in range(32):
        for x in range(32):
            v = math.sin(x * 0.2) * math.cos(y * 0.2)
            img[y, x] = c_purple if v > 0.4 else c_abyss

    # Tinh vân mờ ảo
    for nx, ny in [(10, 12), (11, 12), (22, 20), (21, 21)]:
        img[ny, nx] = c_nebula

    # Ngôi sao xa xôi lấp lánh
    for sx, sy in [(6, 7), (25, 14), (16, 26), (28, 5)]:
        img[sy, sx] = c_star

    return img

def make_castle_prop_candelabra():
    # Chân nến Gothic ngọn lửa linh hồn xanh lam (Soul Flame)
    img = blank(32, 32)
    c_iron = [45, 45, 55, 255]
    c_iron_l = [85, 85, 100, 255]
    c_wax = [220, 215, 210, 255]
    c_flame = [65, 185, 255, 255]
    c_flame_core = [220, 245, 255, 255]

    # Chân đế
    for x in range(10, 22):
        img[29, x] = c_iron

    # Trụ đứng
    for y in range(12, 29):
        img[y, 15] = c_iron_l
        img[y, 16] = c_iron

    # 3 nhánh nến
    branches = [8, 15, 23]
    for bx in branches:
        # Cây nến trắng
        for y in range(10, 16):
            img[y, bx] = c_wax
        # Ngọn lửa xanh linh hồn
        for y in range(5, 10):
            img[y, bx] = c_flame
        img[7, bx] = c_flame_core

    return img

def make_castle_prop_altar():
    # Bàn Thờ Bóng Tối (Dark Castle Altar) - Bất tử 5s + 50 Adrenaline
    img = blank(32, 32)
    c_obsidian = [28, 25, 36, 255]
    c_obsidian_l = [65, 58, 80, 255]
    c_rune = [220, 60, 220, 255]
    c_rune_h = [255, 175, 255, 255]

    # Bệ thờ obsidian
    for y in range(18, 29):
        for x in range(4, 28):
            img[y, x] = c_obsidian_l if y == 18 else c_obsidian

    # Rãnh rune cổ phát sáng tím
    for x in range(8, 24):
        if x % 3 == 0:
            img[22, x] = c_rune
            img[23, x] = c_rune_h

    # Viên đá hư vô bay lơ lửng trên bàn thờ
    for y in range(7, 15):
        for x in range(12, 20):
            d = abs(x - 15.5) + abs(y - 10.5)
            if d <= 3.5:
                img[y, x] = c_rune_h if d <= 1.5 else c_rune

    return img

def make_castle_prop_gargoyle():
    # Tượng Quỷ Đá Gargoyle bảo vệ (Void Trap)
    img = blank(32, 32)
    c_stone = [65, 62, 75, 255]
    c_stone_d = [38, 35, 45, 255]
    c_stone_l = [105, 100, 120, 255]
    c_eye = [255, 40, 40, 255] # Mắt đỏ ngầu

    # Bệ đứng
    for y in range(25, 30):
        for x in range(8, 24):
            img[y, x] = c_stone_l if y == 25 else c_stone_d

    # Thân tượng quỷ đá ngồi xổm
    for y in range(12, 25):
        for x in range(11, 21):
            img[y, x] = c_stone

    # Đầu quỷ đá
    for y in range(6, 13):
        for x in range(12, 20):
            img[y, x] = c_stone_l if y < 9 else c_stone

    # Mắt đỏ rực
    img[9, 14] = c_eye
    img[9, 17] = c_eye

    # Cánh dơi quỷ dang ra 2 bên
    for y in range(7, 18):
        img[y, 8] = c_stone_d
        img[y, 23] = c_stone_d
        img[y, 9] = c_stone
        img[y, 22] = c_stone

    return img


# ══════════════════════════════════════════════════════════════════════════════
# MAIN GENERATOR
# ══════════════════════════════════════════════════════════════════════════════

def main():
    print("=== Generating Ancient Forest Assets ===")
    save_img(make_forest_grass(), os.path.join(FOREST_DIR, "Tile_Forest_Grass.png"))
    save_img(make_forest_path(), os.path.join(FOREST_DIR, "Tile_Forest_Path.png"))
    save_img(make_forest_tree_wall(), os.path.join(FOREST_DIR, "Tile_Forest_TreeWall.png"))
    save_img(make_forest_water(), os.path.join(FOREST_DIR, "Tile_Forest_Water.png"))
    save_img(make_forest_prop_shrine(), os.path.join(FOREST_DIR, "Prop_Forest_AncientShrine.png"))
    save_img(make_forest_prop_mushroom(), os.path.join(FOREST_DIR, "Prop_Forest_Mushroom.png"))
    save_img(make_forest_prop_thorn(), os.path.join(FOREST_DIR, "Prop_Forest_ThornBush.png"))

    print("\n=== Generating Lava Mines Assets ===")
    save_img(make_lava_basalt(), os.path.join(LAVA_DIR, "Tile_Lava_Basalt.png"))
    save_img(make_lava_ash(), os.path.join(LAVA_DIR, "Tile_Lava_Ash.png"))
    save_img(make_lava_magma(), os.path.join(LAVA_DIR, "Tile_Lava_Magma.png"))
    save_img(make_lava_rock_wall(), os.path.join(LAVA_DIR, "Tile_Lava_RockWall.png"))
    save_img(make_lava_prop_geyser(), os.path.join(LAVA_DIR, "Prop_Lava_Geyser.png"))
    save_img(make_lava_prop_anvil(), os.path.join(LAVA_DIR, "Prop_Lava_Anvil.png"))
    save_img(make_lava_prop_ore_cart(), os.path.join(LAVA_DIR, "Prop_Lava_OreCart.png"))

    print("\n=== Generating Dark Castle Assets ===")
    save_img(make_castle_cobble(), os.path.join(CASTLE_DIR, "Tile_Castle_Cobble.png"))
    save_img(make_castle_carpet(), os.path.join(CASTLE_DIR, "Tile_Castle_Carpet.png"))
    save_img(make_castle_wall(), os.path.join(CASTLE_DIR, "Tile_Castle_Wall.png"))
    save_img(make_castle_void(), os.path.join(CASTLE_DIR, "Tile_Castle_Void.png"))
    save_img(make_castle_prop_candelabra(), os.path.join(CASTLE_DIR, "Prop_Castle_Candelabra.png"))
    save_img(make_castle_prop_altar(), os.path.join(CASTLE_DIR, "Prop_Castle_Altar.png"))
    save_img(make_castle_prop_gargoyle(), os.path.join(CASTLE_DIR, "Prop_Castle_Gargoyle.png"))

    print("\nAll 3 realms art generated with enhanced visual quality!")

if __name__ == '__main__':
    main()
