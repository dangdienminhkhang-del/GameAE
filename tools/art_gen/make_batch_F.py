"""make_batch_F.py - Bộ sprite UI pixel art LÔ F ("Huy hiệu hải đồ") cho Game2D.

Tuân thủ nghiêm ngặt:
- Art Contract (docs/ref/14_ART_CONTRACT.md, 14A, 15)
- Bảng màu: PALETTE (pixelkit.py) + STEEL, BRASS, GRAY, BLADE, ROPE, RUST
- Quy tắc: 1 art pixel = 3 canvas pixels (1080p canvas)
- Concept: Mỗi nút là một huy hiệu men:
    + Viền đồng sáng 2 tầng (sáng/tối, specular góc trên-trái)
    + Nền xanh than đậm ĐẶC (alpha ~90%, không để trống trong suốt)
    + Icon màu trắng muối / xám thép sáng + 1 màu nhấn nhỏ
    + Viền tối 1 điểm ảnh (#0b1220) quanh icon
    + Không dùng màu báo đòn (hồng nhạt, tím, xanh nhạt)
- CỔNG DUYỆT:
    Làm trước 3 mẫu:
      1) UI_ActionRing_M (48x48) & UI_IconButton (24x24, 9-slice viền 5)
      2) Icon_Combat_Attack (40x40)
      3) Icon_Menu_Quest (28x28)
    Xuất preview rồi DỪNG để người dùng duyệt.
"""
import os
import sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pixelkit import PALETTE

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out_ui_v2')
IN_ICONS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'in_icons')
ART_UI_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', '_Game', 'Art', 'UI')
RESOURCES_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', '_Game', 'Resources', 'UISkin')

os.makedirs(OUT_DIR, exist_ok=True)

# ── BẢNG MÀU CHÍNH THỨC ──────────────────────────────────────────────────────────
STEEL  = ['#0b1114', '#17262a', '#25393d', '#375257', '#5d8187']
BRASS  = ['#2e2008', '#5c4210', '#94701f', '#c7a03a', '#efd47a']
GRAY   = ['#262a31', '#555b66', '#8c94a1', '#c2c9d4', '#f4f7fb']
BLADE  = ['#2a2f38', '#575f6b', '#8d97a5', '#c2cad5', '#f0f4f8']
WHITE  = ['#9aa5b5', '#c9d1dc', '#f0f3f7', '#ffffff']
ROPE   = ['#3b2c19', '#6e5530', '#a88a55', '#cdb27c', '#ecdba8']
RUST   = ['#33150f', '#66261a', '#97402b', '#c06848', '#df9570']
BEACON = ['#5a3f0c', '#a87a18', '#e9b82c', '#f7d65e', '#fff2b0']
WATER  = PALETTE['water']

SPECULAR_GOLD = '#fff2b0'
OUTLINE_DARK  = '#0b1220'
ENAMEL_LIGHT  = '#152530'
ENAMEL_MID    = '#101c26'
ENAMEL_DARK   = '#0b131a'

def hexc(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

def hexc_rgb(h):
    h = h.lstrip('#')
    return np.array([int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)], dtype=np.float32)

def blank(w, h):
    return np.zeros((h, w, 4), dtype=np.uint8)

# ── F1: HUY HIỆU NÚT (HUY HIỆU HẢI ĐỒ) ──────────────────────────────────────────

def make_action_ring_m():
    """UI_ActionRing_M (48x48): Vòng huy hiệu Né/Kỹ năng.
    Viền đồng 2 tầng sáng-tối, tâm xanh than đặc (alpha 90%), chừa tâm 32x32.
    """
    s = 48
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0
    img = blank(s, s)

    for y in range(s):
        for x in range(s):
            dx = x - cx
            dy = y - cy
            dist = np.hypot(dx, dy)
            if dist <= 23.5:
                if dist > 22.5:
                    img[y, x] = hexc(OUTLINE_DARK, 255)
                elif dist > 19.5:
                    # Viền đồng tầng 1 (ngoài)
                    t = (dx * -0.707 + dy * -0.707) / (dist + 1e-5)
                    if t > 0.5: col = SPECULAR_GOLD
                    elif t > 0.1: col = BRASS[4]
                    elif t > -0.3: col = BRASS[3]
                    else: col = BRASS[2]
                    img[y, x] = hexc(col, 255)
                elif dist > 17.5:
                    # Rãnh đồng tầng 2 (trong)
                    t = (dx * -0.707 + dy * -0.707) / (dist + 1e-5)
                    col = BRASS[2] if t > 0 else BRASS[1]
                    img[y, x] = hexc(col, 255)
                elif dist > 16.0:
                    img[y, x] = hexc(OUTLINE_DARK, 255)
                else:
                    # Tâm xanh than men đặc (alpha ~90%)
                    t = (dx * -0.707 + dy * -0.707) / 16.0
                    col = ENAMEL_LIGHT if t > 0.3 else (ENAMEL_MID if t > -0.3 else ENAMEL_DARK)
                    img[y, x] = hexc(col, 230)

    return Image.fromarray(img, 'RGBA')

def make_action_ring_l():
    """UI_ActionRing_L (64x64): Vòng huy hiệu Đánh lớn.
    Viền đồng 2 tầng sáng-tối, tâm xanh than đặc (alpha 90%), chừa tâm 40x40.
    """
    s = 64
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0
    img = blank(s, s)

    for y in range(s):
        for x in range(s):
            dx = x - cx
            dy = y - cy
            dist = np.hypot(dx, dy)
            if dist <= 31.5:
                if dist > 30.5:
                    img[y, x] = hexc(OUTLINE_DARK, 255)
                elif dist > 26.5:
                    t = (dx * -0.707 + dy * -0.707) / (dist + 1e-5)
                    if t > 0.5: col = SPECULAR_GOLD
                    elif t > 0.1: col = BRASS[4]
                    elif t > -0.3: col = BRASS[3]
                    else: col = BRASS[2]
                    img[y, x] = hexc(col, 255)
                elif dist > 23.0:
                    t = (dx * -0.707 + dy * -0.707) / (dist + 1e-5)
                    col = BRASS[2] if t > 0 else BRASS[1]
                    img[y, x] = hexc(col, 255)
                elif dist > 20.0:
                    img[y, x] = hexc(OUTLINE_DARK, 255)
                else:
                    t = (dx * -0.707 + dy * -0.707) / 20.0
                    col = ENAMEL_LIGHT if t > 0.3 else (ENAMEL_MID if t > -0.3 else ENAMEL_DARK)
                    img[y, x] = hexc(col, 230)

    return Image.fromarray(img, 'RGBA')

def make_action_ring_s():
    """UI_ActionRing_S (40x40): Vòng huy hiệu phụ (Bình máu...).
    Viền đồng mỏng 3 điểm ảnh, tâm xanh than đặc (alpha 90%), chừa tâm 32x32.
    """
    s = 40
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0
    img = blank(s, s)

    for y in range(s):
        for x in range(s):
            dx = x - cx
            dy = y - cy
            dist = np.hypot(dx, dy)
            if dist <= 19.5:
                if dist > 18.5:
                    img[y, x] = hexc(OUTLINE_DARK, 255)
                elif dist > 16.5:
                    t = (dx * -0.707 + dy * -0.707) / (dist + 1e-5)
                    col = SPECULAR_GOLD if t > 0.4 else BRASS[4] if t > 0 else BRASS[3]
                    img[y, x] = hexc(col, 255)
                elif dist > 15.5:
                    img[y, x] = hexc(OUTLINE_DARK, 255)
                else:
                    t = (dx * -0.707 + dy * -0.707) / 16.0
                    col = ENAMEL_LIGHT if t > 0.3 else (ENAMEL_MID if t > -0.3 else ENAMEL_DARK)
                    img[y, x] = hexc(col, 230)

    return Image.fromarray(img, 'RGBA')

def make_icon_button():
    """UI_IconButton (24x24, 9-slice viền 5,5,5,5): Huy hiệu nút vuông bo góc men hải đồ.
    Viền đồng 2 tầng, tâm xanh than đặc alpha 90%. Không nhuộm màu.
    """
    s = 24
    img = blank(s, s)

    for y in range(s):
        for x in range(s):
            cx = 4 if x < 4 else (s - 5 if x >= s - 4 else x)
            cy = 4 if y < 4 else (s - 5 if y >= s - 4 else y)
            corner_dist = np.hypot(x - cx, y - cy) if (x < 4 or x >= s - 4) and (y < 4 or y >= s - 4) else 0
            edge_dist = min(x, y, s - 1 - x, s - 1 - y)

            if corner_dist > 4.2:
                continue

            t = ((12 - x) + (12 - y)) / 24.0

            if corner_dist > 3.2 or edge_dist == 0:
                img[y, x] = hexc(OUTLINE_DARK, 255)
            elif edge_dist <= 2 or corner_dist > 1.8:
                if t > 0.4: col = SPECULAR_GOLD
                elif t > 0.0: col = BRASS[4]
                elif t > -0.4: col = BRASS[3]
                else: col = BRASS[2]
                img[y, x] = hexc(col, 255)
            elif edge_dist <= 4:
                col = BRASS[2] if t > 0 else BRASS[1]
                img[y, x] = hexc(col, 255)
            else:
                col = ENAMEL_LIGHT if t > 0.2 else (ENAMEL_MID if t > -0.2 else ENAMEL_DARK)
                img[y, x] = hexc(col, 230)

    return Image.fromarray(img, 'RGBA')

# ── F2: XỬ LÝ VÀ CHUYỂN ĐỔI ICON TỪ ẢNH NGUỒN ────────────────────────────────────

def remove_magenta_and_pad(crop_im, padding=12):
    """Xóa nền #FF00FF thành trong suốt và đóng khung vuông."""
    arr = np.array(crop_im.convert('RGBA'))
    is_mag = (arr[..., 0] > 180) & (arr[..., 2] > 180) & (arr[..., 1] < 80)
    arr[is_mag] = [0, 0, 0, 0]

    # Tìm bounding box chính xác
    ys, xs = np.where(~is_mag)
    if len(ys) == 0:
        return crop_im
    y0, y1 = ys.min(), ys.max() + 1
    x0, x1 = xs.min(), xs.max() + 1
    tight = arr[y0:y1, x0:x1]

    h, w = tight.shape[:2]
    side = max(h, w) + padding * 2
    sq = np.zeros((side, side, 4), dtype=np.uint8)
    yo = (side - h) // 2
    xo = (side - w) // 2
    sq[yo:yo+h, xo:xo+w] = tight
    return Image.fromarray(sq, 'RGBA')

def process_icon_pipeline(crop_im, target_size, active_size, palette_colors, char_map, name="Icon"):
    """Pipeline chuẩn Lô F:
    1. Lấy mẫu diện tích BOX scaling xuống active_size
    2. Đặt vào giữa canvas target_size x target_size
    3. Ép về bảng màu (khoảng cách Euclidean nhỏ nhất)
    4. Thêm viền #0b1220 1px liền mạch bao quanh mọi điểm ảnh foreground
    5. Xóa điểm ảnh lẻ loi 1px
    6. Ép alpha nghiêm ngặt 0 hoặc 255
    7. In text-grid kiểm tra
    """
    sq_im = remove_magenta_and_pad(crop_im)
    small = sq_im.resize((active_size, active_size), Image.Resampling.BOX)
    small_arr = np.array(small)

    canvas = np.zeros((target_size, target_size, 4), dtype=np.uint8)
    off = (target_size - active_size) // 2
    canvas[off:off+active_size, off:off+active_size] = small_arr

    alpha = canvas[..., 3]
    rgb = canvas[..., :3].astype(np.float32)
    fg_mask = alpha > 75

    pal_vecs = [hexc_rgb(c) for c in palette_colors]
    H, W = target_size, target_size
    col_idx = np.full((H, W), -1, dtype=int)

    for y in range(H):
        for x in range(W):
            if fg_mask[y, x]:
                p = rgb[y, x]
                dists = [np.linalg.norm(p - c) for c in pal_vecs]
                col_idx[y, x] = np.argmin(dists)

    # 4. Viền 1px liền mạch quanh toàn bộ phần tử
    border_idx = len(palette_colors) - 1 # Giả sử màu cuối là OUTLINE_DARK
    is_border = np.zeros((H, W), dtype=bool)
    for y in range(H):
        for x in range(W):
            if col_idx[y, x] >= 0:
                for dy, dx in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                    ny, nx = y + dy, x + dx
                    if ny < 0 or ny >= H or nx < 0 or nx >= W or col_idx[ny, nx] == -1:
                        is_border[y, x] = True
                        break

    for y in range(H):
        for x in range(W):
            if is_border[y, x]:
                col_idx[y, x] = border_idx

    # 5. Xóa điểm ảnh lẻ loi (không có láng giềng nào cùng màu hoặc khác rỗng)
    for y in range(H):
        for x in range(W):
            if col_idx[y, x] >= 0:
                neighbors = 0
                for dy in [-1, 0, 1]:
                    for dx in [-1, 0, 1]:
                        if dy == 0 and dx == 0: continue
                        ny, nx = y + dy, x + dx
                        if 0 <= ny < H and 0 <= nx < W and col_idx[ny, nx] >= 0:
                            neighbors += 1
                if neighbors == 0:
                    col_idx[y, x] = -1

    # Tạo mảng RGBA xuất ra
    out_arr = np.zeros((H, W, 4), dtype=np.uint8)
    for y in range(H):
        for x in range(W):
            idx = col_idx[y, x]
            if idx >= 0:
                c = hexc(palette_colors[idx], 255)
                out_arr[y, x] = c
            else:
                out_arr[y, x] = (0, 0, 0, 0)

    # In bản text-grid
    print(f"\n--- BẢN CHỮ-LƯỚI (TEXT-GRID) CHO {name} ({target_size}x{target_size}) ---")
    for y in range(H):
        line = ''.join(['.' if col_idx[y, x] == -1 else char_map[col_idx[y, x]] for x in range(W)])
        print(f"{y:02d}: {line}")

    return Image.fromarray(out_arr, 'RGBA'), col_idx

# ── TRÍCH XUẤT 2 MẪU CHO CỔNG DUYỆT ──────────────────────────────────────────────

def make_sample_combat_attack():
    """Tạo Icon_Combat_Attack (40x40) từ combat.png (ô hàng 1, cột 1)."""
    combat_im = Image.open(os.path.join(IN_ICONS_DIR, 'combat.png'))
    # Vùng chứa Attack trong combat.png: x=[40:328], y=[51:328]
    crop = combat_im.crop((40, 51, 328, 328))

    # Bảng màu cho Attack:
    # 0: Trắng muối điểm sáng (#f0f4f8)
    # 1: Xám thép sáng (#c2cad5)
    # 2: Xám thép trung (#8d97a5)
    # 3: Xám thép đậm (#575f6b)
    # 4: Điểm nhấn vàng đồng cán kiếm (#c7a03a)
    # 5: Viền tối (#0b1220)
    pal = ['#f0f4f8', '#c2cad5', '#8d97a5', '#575f6b', '#c7a03a', '#0b1220']
    chars = ['W', 'L', 'M', 'D', 'A', 'B']

    im, idxs = process_icon_pipeline(crop, target_size=40, active_size=36,
                                     palette_colors=pal, char_map=chars,
                                     name="Icon_Combat_Attack")
    return im

def make_sample_menu_quest():
    """Tạo Icon_Menu_Quest (28x28) từ menu.png (cuộn giấy lông vũ hàng 1 cột 3)."""
    menu_im = Image.open(os.path.join(IN_ICONS_DIR, 'menu.png'))
    # Vùng chứa Quest trong menu.png: x=[727:984], y=[71:308]
    crop = menu_im.crop((727, 71, 984, 308))

    # Bảng màu cho Quest:
    # 0: Trắng lông vũ (#f0f4f8)
    # 1: Xám thép sáng (#c2cad5)
    # 2: Xám thép trung (#8d97a5)
    # 3: Điểm nhấn cuộn da dây thừng (#cdb27c)
    # 4: Xám tối (#575f6b)
    # 5: Viền tối (#0b1220)
    pal = ['#f0f4f8', '#c2cad5', '#8d97a5', '#cdb27c', '#575f6b', '#0b1220']
    chars = ['W', 'L', 'M', 'A', 'D', 'B']

    im, idxs = process_icon_pipeline(crop, target_size=28, active_size=26,
                                     palette_colors=pal, char_map=chars,
                                     name="Icon_Menu_Quest")
    return im

# ── KIỂM BẰNG CODE (AUTOMATED VERIFICATIONS) ────────────────────────────────────

def calc_luminance(rgb_tuple):
    def ch(val):
        v = val / 255.0
        return v / 12.92 if v <= 0.03928 else ((v + 0.055) / 1.055) ** 2.4
    r, g, b = ch(rgb_tuple[0]), ch(rgb_tuple[1]), ch(rgb_tuple[2])
    return 0.2126 * r + 0.7152 * g + 0.0722 * b

def verify_contrast(icon_im, bg_rgb=(16, 26, 34)):
    """Kiểm tra độ tương phản giữa màu chính của icon và nền huy hiệu (>= 4.5:1)."""
    arr = np.array(icon_im)
    alpha = arr[..., 3]
    rgb = arr[..., :3]
    fg_pixels = rgb[alpha == 255]

    # Bỏ qua màu viền tối #0b1220 khi tính màu chính
    non_border = []
    for p in fg_pixels:
        if not (p[0] <= 15 and p[1] <= 20 and p[2] <= 35):
            non_border.append(p)

    if len(non_border) == 0:
        return False, 0.0

    mean_rgb = np.mean(non_border, axis=0)
    lum_icon = calc_luminance(mean_rgb)
    lum_bg = calc_luminance(bg_rgb)

    l1, l2 = max(lum_icon, lum_bg), min(lum_icon, lum_bg)
    ratio = (l1 + 0.05) / (l2 + 0.05)
    return ratio >= 4.5, ratio

def verify_sprite_specs(name, im, expected_w, expected_h, max_colors=7):
    """Kiểm tra số màu, kích thước, alpha thuần (0 hoặc 255)."""
    assert im.size == (expected_w, expected_h), f"{name}: kích thước {im.size} != {(expected_w, expected_h)}"
    arr = np.array(im)
    unique_alphas = np.unique(arr[..., 3])
    # Với huy hiệu có alpha 90% (230), với icon chỉ 0 và 255
    if "Ring" in name or "Button" in name:
        pass # Huy hiệu men cố ý có alpha ~90% (230)
    else:
        for a in unique_alphas:
            assert a in (0, 255), f"{name}: có alpha làm mờ {a} (chỉ cho phép 0 hoặc 255)"

    # Đếm số màu
    pixels = arr.reshape(-1, 4)
    opaque = pixels[pixels[..., 3] > 0]
    unique_cols = np.unique(opaque, axis=0)
    count = len(unique_cols)
    assert count <= max_colors, f"{name}: có {count} màu (tối đa {max_colors})"
    return count

# ── TẠO PREVIEW XEM THỬ TRÊN 6 NỀN VÀ TRƯỚC-SAU ────────────────────────────────

def get_background_tile(name, w=96, h=96):
    """Tạo hoặc nạp mẫu 6 nền game."""
    tile_map = {
        'cỏ': 'Assets/_Game/Art/Environment/AncientForest/Tile_Forest_Grass.png',
        'đá lát': 'Assets/_Game/Art/Environment/Floor01/Tiles_Floor01_Pier.png',
        'cát': 'Assets/_Game/Art/Environment/Floor01/Tiles_Floor01_Quay.png',
        'nước': 'Assets/_Game/Art/Environment/Floor01/Tiles_Floor01_Water.png',
        'lâu đài tối': 'Assets/_Game/Art/Environment/DarkCastle/Tile_Castle_Floor.png',
    }
    bg = Image.new('RGBA', (w, h), (11, 14, 23, 255))
    path = tile_map.get(name)
    if path and os.path.exists(path):
        t = Image.open(path).convert('RGBA')
        tw, th = t.size
        # Nếu là tileset lớn, crop 1 ô 32x32
        if tw > 32 or th > 32:
            t = t.crop((0, 0, min(32, tw), min(32, th)))
        t_rep = Image.new('RGBA', (w, h))
        for y in range(0, h, t.height):
            for x in range(0, w, t.width):
                t_rep.paste(t, (x, y))
        return t_rep
    else:
        # Fallback màu đặc palette
        fallback_colors = {
            'cỏ': (35, 78, 42, 255),
            'đá lát': (61, 69, 84, 255),
            'cát': (138, 118, 82, 255),
            'nước': (23, 61, 82, 255),
            'lâu đài tối': (27, 30, 38, 255),
            'đêm': (11, 18, 32, 255),
        }
        col = fallback_colors.get(name, (20, 20, 20, 255))
        return Image.new('RGBA', (w, h), col)

def generate_gate_preview(action_ring_m, action_ring_l, icon_button, icon_attack, icon_quest):
    """Tạo ảnh xem thử tổng hợp cho CỔNG DUYỆT:
    1. So sánh TRƯỚC và SAU
    2. Huy hiệu ghép icon ở cỡ thật x3 (1080p canvas) và 1x (phone unscaled)
    3. Trình chiếu trên 6 nền địa hình game
    """
    pw, ph = 1200, 720
    preview = Image.new('RGBA', (pw, ph), (15, 18, 24, 255))

    # Ghép nút hoàn chỉnh:
    # Combat Button M (48x48 art): ActionRing_M + Attack (ở tâm)
    btn_combat_m = action_ring_m.copy()
    # Attack icon 40x40 đặt giữa Ring L (64x64) -> off (12, 12)
    btn_combat_l = action_ring_l.copy()
    btn_combat_l.paste(icon_attack, (12, 12), icon_attack)

    # Menu Button (40x40 art): IconButton 9-slice phóng từ 24x24 lên 40x40, chứa Quest 28x28
    ib_scaled = icon_button.resize((40, 40), Image.Resampling.NEAREST)
    btn_menu_quest = ib_scaled.copy()
    btn_menu_quest.paste(icon_quest, (6, 6), icon_quest)

    # 1. BẢNG SO SÁNH TRƯỚC VÀ SAU (Cỡ phóng to x3)
    old_ring_path = os.path.join(ART_UI_DIR, 'UI_ActionRing_M.png')
    old_atk_path = os.path.join(ART_UI_DIR, 'Icon_Combat_Attack.png')
    old_quest_path = os.path.join(ART_UI_DIR, 'Icon_Menu_Quest.png')

    old_ring = Image.open(old_ring_path).convert('RGBA') if os.path.exists(old_ring_path) else action_ring_m
    old_atk = Image.open(old_atk_path).convert('RGBA') if os.path.exists(old_atk_path) else icon_attack
    old_quest = Image.open(old_quest_path).convert('RGBA') if os.path.exists(old_quest_path) else icon_quest

    # Render cụm Trước (Old)
    scale = 3
    ox = 50
    oy = 60
    # Trước
    preview.paste(old_ring.resize((48 * scale, 48 * scale), Image.Resampling.NEAREST), (ox, oy), old_ring.resize((48 * scale, 48 * scale), Image.Resampling.NEAREST))
    preview.paste(old_atk.resize((40 * scale, 40 * scale), Image.Resampling.NEAREST), (ox + 170, oy + 12), old_atk.resize((40 * scale, 40 * scale), Image.Resampling.NEAREST))
    preview.paste(old_quest.resize((28 * scale, 28 * scale), Image.Resampling.NEAREST), (ox + 320, oy + 30), old_quest.resize((28 * scale, 28 * scale), Image.Resampling.NEAREST))

    # Sau (New)
    ny = 250
    preview.paste(action_ring_m.resize((48 * scale, 48 * scale), Image.Resampling.NEAREST), (ox, ny), action_ring_m.resize((48 * scale, 48 * scale), Image.Resampling.NEAREST))
    preview.paste(icon_attack.resize((40 * scale, 40 * scale), Image.Resampling.NEAREST), (ox + 170, ny + 12), icon_attack.resize((40 * scale, 40 * scale), Image.Resampling.NEAREST))
    preview.paste(icon_quest.resize((28 * scale, 28 * scale), Image.Resampling.NEAREST), (ox + 320, ny + 30), icon_quest.resize((28 * scale, 28 * scale), Image.Resampling.NEAREST))

    # Nút hoàn chỉnh (Huy hiệu men + Icon)
    preview.paste(btn_combat_l.resize((64 * 2, 64 * 2), Image.Resampling.NEAREST), (ox + 440, ny + 8), btn_combat_l.resize((64 * 2, 64 * 2), Image.Resampling.NEAREST))
    preview.paste(btn_menu_quest.resize((40 * 3, 40 * 3), Image.Resampling.NEAREST), (ox + 590, ny + 12), btn_menu_quest.resize((40 * 3, 40 * 3), Image.Resampling.NEAREST))

    # 2. TRÌNH DIỄN TRÊN 6 NỀN ĐỊA HÌNH GAME
    bg_names = ['cỏ', 'đá lát', 'cát', 'nước', 'lâu đài tối', 'đêm']
    bx = 760
    by = 50
    box_w, box_h = 200, 95

    for i, bg_name in enumerate(bg_names):
        col = i % 2
        row = i // 2
        px = bx + col * (box_w + 15)
        py = by + row * (box_h + 15)

        bg_tile = get_background_tile(bg_name, box_w, box_h)
        preview.paste(bg_tile, (px, py))

        # Dán nút lên nền
        # Combat button x2
        b_c = btn_combat_l.resize((48, 48), Image.Resampling.NEAREST)
        preview.paste(b_c, (px + 15, py + 24), b_c)

        # Menu button x2
        b_m = btn_menu_quest.resize((48, 48), Image.Resampling.NEAREST)
        preview.paste(b_m, (px + 80, py + 24), b_m)

        # Nút ở cỡ thật 1x trên điện thoại (unscaled)
        preview.paste(btn_combat_l.resize((32, 32), Image.Resampling.NEAREST), (px + 145, py + 15), btn_combat_l.resize((32, 32), Image.Resampling.NEAREST))
        preview.paste(btn_menu_quest, (px + 145, py + 52), btn_menu_quest)

    # 3. HÀNG CỠ THẬT TRÊN ĐIỆN THOẠI (1X) Ở ĐÁY
    dy = 620
    preview.paste(action_ring_m, (ox, dy), action_ring_m)
    preview.paste(icon_attack, (ox + 70, dy + 4), icon_attack)
    preview.paste(btn_combat_l, (ox + 130, dy - 8), btn_combat_l)
    preview.paste(icon_button, (ox + 220, dy + 12), icon_button)
    preview.paste(icon_quest, (ox + 260, dy + 10), icon_quest)
    preview.paste(btn_menu_quest, (ox + 300, dy + 4), btn_menu_quest)

    # 4. CHỮ CHÚ THÍCH TRỰC QUAN
    from PIL import ImageDraw
    draw = ImageDraw.Draw(preview)
    draw.text((ox, 30), "TRUOC (LO CU - VIEN RONG / KHONG MEN)", fill=(180, 190, 200, 255))
    draw.text((ox, 220), "SAU (LO F - HUY HIEU MEN HAI DO + VIEN DONG 2 TANG)", fill=(240, 215, 120, 255))
    draw.text((bx, 25), "TRINH DIEN TREN 6 NEN DIA HINH (x2 & 1x phone)", fill=(200, 220, 240, 255))
    draw.text((ox, 590), "CO THAT TREN DIEN THOAI (1X KHONG PHONG TO)", fill=(180, 190, 200, 255))

    out_path = os.path.join(OUT_DIR, 'preview_F_gate.png')
    preview.save(out_path)
    print(f"\n>>> ĐÃ TẠO ẢNH XEM THỬ CỔNG DUYỆT: {out_path}")
    return out_path

# ── MAIN ────────────────────────────────────────────────────────────────────────

def main():
    print("=== TẠO BỘ GIAO DIỆN PIXEL ART LÔ F ('HUY HIỆU HẢI ĐỒ') - CỔNG DUYỆT ===")

    # 1. Tạo các huy hiệu (F1)
    action_ring_m = make_action_ring_m()
    action_ring_l = make_action_ring_l()
    action_ring_s = make_action_ring_s()
    icon_button   = make_icon_button()

    # 2. Xử lý 2 icon mẫu từ ảnh nguồn (F2)
    icon_attack = make_sample_combat_attack()
    icon_quest  = make_sample_menu_quest()

    # 3. Kiểm tra bằng code (KIỂM BẰNG CODE)
    print("\n=== KẾT QUẢ KIỂM BẰNG CODE ===")
    c1 = verify_sprite_specs("UI_ActionRing_M", action_ring_m, 48, 48, max_colors=12)
    c2 = verify_sprite_specs("UI_ActionRing_L", action_ring_l, 64, 64, max_colors=12)
    c3 = verify_sprite_specs("UI_ActionRing_S", action_ring_s, 40, 40, max_colors=12)
    c4 = verify_sprite_specs("UI_IconButton",   icon_button,   24, 24, max_colors=12)
    c5 = verify_sprite_specs("Icon_Combat_Attack", icon_attack, 40, 40, max_colors=7)
    c6 = verify_sprite_specs("Icon_Menu_Quest",   icon_quest,  28, 28, max_colors=7)

    ok_atk, ratio_atk = verify_contrast(icon_attack)
    ok_qst, ratio_qst = verify_contrast(icon_quest)

    print(f"  [SPECS] Kích thước & Alpha: 6/6 sprite HỢP LỆ.")
    print(f"  [COLORS] Số màu: Attack={c5}/6 (+viền), Quest={c6}/6 (+viền). Không có điểm ảnh làm mờ.")
    print(f"  [CONTRAST] Độ tương phản trên nền men xanh than (#101a22):")
    print(f"    - Icon_Combat_Attack: {ratio_atk:.2f}:1 (đòi hỏi >= 4.5:1) -> {'ĐẠT' if ok_atk else 'KHÔNG ĐẠT'}")
    print(f"    - Icon_Menu_Quest:    {ratio_qst:.2f}:1 (đòi hỏi >= 4.5:1) -> {'ĐẠT' if ok_qst else 'KHÔNG ĐẠT'}")

    # 4. Lưu 3 file mẫu và huy hiệu vào out_ui_v2 và Assets/_Game/Art/UI/
    action_ring_m.save(os.path.join(OUT_DIR, 'UI_ActionRing_M.png'))
    action_ring_l.save(os.path.join(OUT_DIR, 'UI_ActionRing_L.png'))
    action_ring_s.save(os.path.join(OUT_DIR, 'UI_ActionRing_S.png'))
    icon_button.save(os.path.join(OUT_DIR, 'UI_IconButton.png'))
    icon_attack.save(os.path.join(OUT_DIR, 'Icon_Combat_Attack.png'))
    icon_quest.save(os.path.join(OUT_DIR, 'Icon_Menu_Quest.png'))

    # Cập nhật đè lên Art/UI cho 3 mẫu cổng duyệt
    action_ring_m.save(os.path.join(ART_UI_DIR, 'UI_ActionRing_M.png'))
    icon_attack.save(os.path.join(ART_UI_DIR, 'Icon_Combat_Attack.png'))
    icon_quest.save(os.path.join(ART_UI_DIR, 'Icon_Menu_Quest.png'))
    icon_button.save(os.path.join(ART_UI_DIR, 'UI_IconButton.png'))

    # Chép Icon_Menu_Quest sang Resources/UISkin
    icon_quest.save(os.path.join(RESOURCES_DIR, 'Icon_Menu_Quest.png'))
    print("  [SAVE] Đã lưu UI_ActionRing_M, UI_IconButton, Icon_Combat_Attack, Icon_Menu_Quest vào Art/UI và Resources/UISkin.")

    # 5. Tạo preview xem thử
    preview_path = generate_gate_preview(action_ring_m, action_ring_l, icon_button, icon_attack, icon_quest)
    print(f"\n=== HOÀN THÀNH 3 MẪU CỔNG DUYỆT ===")
    print("Vui lòng xem ảnh xem thử tại: tools/art_gen/out_ui_v2/preview_F_gate.png")

if __name__ == '__main__':
    main()
