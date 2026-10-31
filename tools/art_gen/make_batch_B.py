"""make_batch_B.py - Bộ sprite UI pixel art mới cho Game2D: LÔ B (Màn hình chính, Đăng nhập, Tải).

Tuân thủ nghiêm ngặt:
- Art Contract (docs/ref/14_ART_CONTRACT.md, 14A, 15)
- Bảng màu: PALETTE (pixelkit.py) + STEEL, BRASS, GRAY (make_ui_pack.py)
- Quy tắc tỷ lệ: 1 art pixel = 3 canvas pixels (1080p canvas)
- Bg_MainMenu: 800x360 art px (= 2400x1080 canvas), chủ thể ở khung 480x360 giữa
- Logo_Emblem: 96x96 art px (tháp/hải đăng + vòng sóng + nút dây thừng, KHÔNG chữ)
- UI_TitlePlate: 80x28 art px (9-slice L=20, R=20, T=7, B=7)
- UI_ButtonWide: 48x24 art px (9-slice L=12, R=12, T=6, B=6, xám trung tính để game nhuộm)
"""
import os
import sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pixelkit import PALETTE

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out_ui_v2')
os.makedirs(OUT_DIR, exist_ok=True)

# ── BẢNG MÀU CHÍNH THỨC ──────────────────────────────────────────────────────────
STEEL  = ['#0b1114', '#17262a', '#25393d', '#375257', '#5d8187']
BRASS  = ['#2e2008', '#5c4210', '#94701f', '#c7a03a', '#efd47a']
GRAY   = ['#262a31', '#555b66', '#8c94a1', '#c2c9d4', '#f4f7fb']
WATER  = PALETTE['water']    # ['#0a1b27', '#0f2a3a', '#173d52', '#245672', '#357490']
FOAM   = PALETTE['foam']     # ['#1c3a4a', '#35607a', '#5a8aa3', '#86b0c4', '#b4d2df']
BEACON = PALETTE['beacon']   # ['#5a3f0c', '#a87a18', '#e9b82c', '#f7d65e', '#fff2b0']
ROPE   = PALETTE['rope']     # ['#3b2c19', '#6e5530', '#a88a55', '#cdb27c', '#ecdba8']
WOOD   = PALETTE['wood']     # ['#241a12', '#3a2a1c', '#55402a', '#6f5539', '#8d7050']
WOOD2  = PALETTE['wood2']    # ['#2a1f16', '#43321f', '#5f482f', '#7a5e3f', '#99794f']
STONE  = PALETTE['stone']    # ['#171b24', '#262c38', '#363e4e', '#4a5568', '#61708a']
IRON   = PALETTE['iron']     # ['#14181f', '#252b35', '#3a4250', '#566073', '#7c889d']

DARK_INSET    = '#070b0e'
DARK_PANEL    = '#10161d'
DARK_INSET_BG = '#0d1318'
SPECULAR_GOLD = '#fff2b0'
OUTLINE_DARK  = '#0b1220'

def hexc(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

def blank(w, h):
    return np.zeros((h, w, 4), dtype=np.uint8)

def cut_corners(img, count=1):
    h, w = img.shape[:2]
    if count >= 1:
        for x, y in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
            img[y, x] = (0, 0, 0, 0)
    return img

# ── 1. Bg_MainMenu (800x360) ─────────────────────────────────────────────────────
def make_bg_mainmenu():
    """Nền màn hình chính 800x360.
    Cảng Luminport lúc chạng vạng, bóng Tháp Thủy Triều ở xa, đèn hải lưu nhỏ, sương mặn.
    Chủ thể chính nằm trọn trong khoảng x = 160..640 (khung 480x360 an toàn cho 4:3)."""
    w, h = 800, 360
    img = blank(w, h)

    # 1. Bầu trời chạng vạng (y = 0..170)
    sky_colors = [
        (0,   STEEL[0]),
        (35,  WATER[0]),
        (75,  WATER[1]),
        (120, WATER[2]),
        (150, WATER[3]),
        (170, FOAM[1]),
    ]
    for y in range(175):
        # Tìm màu bầu trời tương ứng
        col = sky_colors[-1][1]
        for idx in range(len(sky_colors) - 1):
            y0, c0 = sky_colors[idx]
            y1, c1 = sky_colors[idx + 1]
            if y0 <= y <= y1:
                # Dither nhẹ giữa các nấc
                t = (y - y0) / float(y1 - y0)
                col = c1 if (t > 0.65 or (t > 0.35 and (y + (x := 0)) % 2 == 0)) else c0
                break
        for x in range(w):
            img[y, x] = hexc(col)

    # 2. Dải sương mù chân trời mặn mòi (y = 155..185)
    for y in range(155, 185):
        for x in range(w):
            dither = ((x ^ (y * 3)) % 7)
            if y < 165 and dither < 3:
                img[y, x] = hexc(FOAM[1])
            elif y >= 165 and dither < 4:
                img[y, x] = hexc(FOAM[0])

    # 3. Bóng núi xa và Tháp Thủy Triều (Tidal Tower) ở đường chân trời (trong vùng an toàn x = 360..440)
    # Núi đá xa bên trái và phải
    for x in range(w):
        # Đường cong đồi xa
        hill_left = int(175 - 28 * np.exp(-((x - 220) / 120.0) ** 2))
        hill_right = int(175 - 22 * np.exp(-((x - 580) / 130.0) ** 2))
        hill_y = min(hill_left, hill_right)
        for y in range(hill_y, 185):
            img[y, x] = hexc(WATER[1])

    # Tháp Thủy Triều vươn cao ở x = 370..430, y = 50..185 (giữa an toàn)
    tower_cx = 400
    for y in range(48, 185):
        prog = (185 - y) / 137.0
        half_w = int(22 - 16 * prog)
        # Các tầng mở rộng của tháp
        if y in (140, 115, 90, 70):
            half_w += 3
        # Đỉnh tháp nhọn ngọn hải đăng
        if y < 62:
            half_w = max(2, 6 - int((62 - y) * 0.4))
        for x in range(tower_cx - half_w, tower_cx + half_w + 1):
            # Thân tháp tông xanh than trầm
            c = STEEL[1] if (x == tower_cx - half_w or x == tower_cx + half_w) else STEEL[0]
            if y % 12 == 0 and abs(x - tower_cx) < half_w - 1:
                c = WATER[2]
            img[y, x] = hexc(c)

    # Đèn hải lưu ngọn tháp nhấp nháy sáng vàng beacon
    img[58, tower_cx] = hexc(SPECULAR_GOLD)
    img[58, tower_cx - 1] = hexc(BEACON[2])
    img[58, tower_cx + 1] = hexc(BEACON[2])
    img[57, tower_cx] = hexc(BEACON[3])
    img[59, tower_cx] = hexc(BEACON[1])

    # 4. Mặt biển tĩnh lặng (y = 180..280)
    for y in range(180, 280):
        t = (y - 180) / 100.0
        base_water = WATER[0] if t < 0.35 else STEEL[0] if t < 0.7 else WATER[1]
        for x in range(w):
            img[y, x] = hexc(base_water)
            # Sóng ngang và phản chiếu mờ của tháp
            if abs(x - tower_cx) < 25 and (y % 4 == 0) and (x % 3 != 0):
                img[y, x] = hexc(WATER[2])
            elif abs(x - tower_cx) < 4 and (y % 6 == 0):
                img[y, x] = hexc(BEACON[0])
            elif (x + y * 7) % 29 == 0:
                img[y, x] = hexc(FOAM[0])

    # 5. Cảng Luminport ở tiền cảnh (y = 265..360)
    # Cầu tàu gỗ và kè đá ở góc dưới
    for x in range(w):
        pier_top = 275 + int(12 * np.sin(x / 60.0))
        for y in range(pier_top, h):
            if y == pier_top:
                c = WOOD[3] if (x % 16 < 14) else WOOD[1]
            elif y == pier_top + 1:
                c = WOOD[2]
            elif (y - pier_top) % 18 == 0:
                c = WOOD[0]  # Khe rãnh giữa các thanh ván gỗ
            else:
                c = WOOD[2] if (x % 22 < 18) else WOOD[1]
            img[y, x] = hexc(c)

    # Các cọc gỗ cầu cảng cắm xuống nước (pilings)
    for px in (90, 190, 310, 490, 610, 710):
        for y in range(250, 320):
            for dx in (-3, -2, -1, 0, 1, 2, 3):
                col = WOOD[0] if abs(dx) == 3 else WOOD[1] if dx > 0 else WOOD[3]
                if y > 275:
                    col = WOOD[0] if abs(dx) == 3 else WOOD[1]
                img[y, px + dx] = hexc(col)
        # Bọt sóng quanh chân cọc
        for bx in (-5, -4, 4, 5):
            img[280, px + bx] = hexc(FOAM[1])

    # Đèn hải lưu tiền cảnh (Beacon Lamps) trên các mỏm cọc cảng
    for lx, ly in ((190, 246), (610, 246)):
        # Trụ sắt giữ đèn
        img[ly + 3, lx] = hexc(STEEL[0])
        img[ly + 2, lx] = hexc(BRASS[2])
        # Lồng đèn 3x3
        for dy in (-2, -1, 0, 1):
            for dx in (-2, -1, 0, 1, 2):
                if abs(dx) == 2 or dy == -2 or dy == 1:
                    img[ly + dy, lx + dx] = hexc(BRASS[1])
                else:
                    img[ly + dy, lx + dx] = hexc(BEACON[2])
        # Điểm sáng vàng rực
        img[ly - 1, lx] = hexc(SPECULAR_GOLD)
        img[ly, lx] = hexc(BEACON[3])
        # Vầng sáng mờ quanh đèn
        for gdx, gdy in ((-3, 0), (3, 0), (0, -3), (0, 2)):
            img[ly + gdy, lx + gdx] = hexc(BEACON[0])

    return Image.fromarray(img, 'RGBA')

# ── 2. Logo_Emblem (96x96) ───────────────────────────────────────────────────────
def make_logo_emblem():
    """Biểu tượng nguyên bản 96x96:
    Tháp hải đăng + vòng sóng xoáy + nút dây thừng bện, KHÔNG CÓ CHỮ."""
    s = 96
    img = blank(s, s)
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0

    # 1. Vòng dây thừng bện bên ngoài (R = 40..46)
    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - cy)
            if 39.5 <= dist <= 46.5:
                angle = np.arctan2(y - cy, x - cx)
                # Bện xoắn dây thừng theo góc
                twist = int((angle * 12) / np.pi) % 3
                if dist > 45.2 or dist < 40.8:
                    col = ROPE[0]
                else:
                    col = ROPE[3] if twist == 0 else ROPE[2] if twist == 1 else ROPE[1]
                img[y, x] = hexc(col)

    # Nút thắt dây thừng hải cảng ở đáy (y = 82..94, x = 40..56)
    for y in range(83, 94):
        for x in range(41, 55):
            d_knot = abs(x - cx) + abs(y - 88) * 0.7
            if d_knot < 8:
                col = ROPE[4] if (x < cx and y < 88) else ROPE[2] if d_knot < 6 else ROPE[1]
                if abs(x - cx) < 2 or y == 88:
                    col = ROPE[0]
                img[y, x] = hexc(col)

    # 2. Vòng sóng thủy triều xoáy bên trong (R = 28..39)
    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - cy)
            if 27.5 <= dist <= 39.0:
                angle = np.arctan2(y - cy, x - cx)
                # Sóng xoắn cuộn
                wave = (angle + dist * 0.25)
                wave_val = np.sin(wave * 4)
                if wave_val > 0.4:
                    col = FOAM[3] if wave_val > 0.8 else FOAM[2]
                elif wave_val > -0.2:
                    col = WATER[3]
                else:
                    col = WATER[1]
                img[y, x] = hexc(col)

    # 3. Bệ đỡ nẹp đồng cánh cung ở 2 bên (x = 14..24 và 72..82)
    for y in range(36, 60):
        for x in list(range(14, 25)) + list(range(71, 82)):
            dist = np.hypot(x - cx, y - cy)
            if 34 <= dist <= 42:
                img[y, x] = hexc(BRASS[3] if x < cx else BRASS[2])
                if (y in (38, 47, 56)):
                    img[y, x] = hexc(BRASS[4] if x < cx else BRASS[1])

    # 4. Ngọn Tháp Thủy Triều uy nghiêm ở trung tâm (x = 34..62, y = 14..78)
    for y in range(16, 80):
        prog = (78 - y) / 62.0
        w_half = int(14 - 10 * prog)
        if y in (64, 48, 32):
            w_half += 2  # Gờ bao lơn các tầng
        for x in range(int(cx - w_half), int(cx + w_half + 1)):
            dx = x - cx
            if abs(dx) == w_half:
                c = STEEL[0]
            elif dx < -w_half + 2:
                c = STEEL[4] if y < 40 else STEEL[3]
            elif dx > w_half - 2:
                c = STEEL[1]
            else:
                c = STEEL[2]
            img[y, x] = hexc(c)

    # Cửa sổ vòm trên tháp
    for wy in (40, 56, 70):
        for wx in (-2, -1, 0, 1, 2):
            img[wy, int(cx + wx)] = hexc(DARK_INSET)
            img[wy - 1, int(cx + wx)] = hexc(DARK_INSET)

    # 5. Hải đăng đỉnh tháp (Lantern Room) rực sáng hào quang (y = 12..28)
    # Mái chóp đồng
    for y in range(14, 22):
        cone_w = int((y - 14) * 0.9)
        for x in range(int(cx - cone_w), int(cx + cone_w + 1)):
            img[y, x] = hexc(BRASS[4] if x <= cx else BRASS[2])
    img[13, int(cx)] = hexc(SPECULAR_GOLD)

    # Buồng kính đèn hải lưu (y = 22..28, x = cx-4 .. cx+4)
    for y in range(22, 28):
        for x in range(int(cx - 4), int(cx + 5)):
            if abs(x - cx) == 4 or y == 22 or y == 27:
                img[y, x] = hexc(BRASS[1])
            else:
                img[y, x] = hexc(BEACON[3])
    # Tâm đèn rực sáng vàng beacon
    img[24, int(cx)] = hexc(SPECULAR_GOLD)
    img[25, int(cx)] = hexc(SPECULAR_GOLD)
    img[24, int(cx) - 1] = hexc(BEACON[4])
    img[24, int(cx) + 1] = hexc(BEACON[4])

    return Image.fromarray(img, 'RGBA')

# ── 3. UI_TitlePlate (80x28, 9-slice border L=20, R=20, T=7, B=7) ─────────────────
def make_title_plate():
    """Tấm tiêu đề tên game 80x28, 9-slice border (20, 7, 20, 7).
    Hai đầu ngàm đồng sóng cuộn, viền thép nẹp đôi, lòng bảng xanh than tối sang trọng."""
    w, h = 80, 28
    img = blank(w, h)

    for y in range(h):
        for x in range(w):
            d_tb = min(y, h - 1 - y)
            if d_tb == 0:
                c = STEEL[0]
            elif d_tb == 1:
                c = STEEL[4] if y <= 1 else STEEL[1]
            elif d_tb == 2:
                c = BRASS[4] if y <= 2 else BRASS[1]  # Đường chỉ vàng đồng viền trên dưới
            elif d_tb == 3:
                c = STEEL[0]
            elif d_tb == 4:
                c = DARK_INSET
            else:
                c = DARK_PANEL
            img[y, x] = hexc(c)

    # Ngàm đồng hoa văn sóng ở 2 đầu (x < 18 và x >= w - 18)
    for y in range(1, h - 1):
        for x in list(range(1, 18)) + list(range(w - 18, w - 1)):
            dx = x if x < 18 else w - 1 - x
            if dx == 1:
                col = BRASS[0]
            elif dx <= 3:
                col = BRASS[4] if y < 14 else BRASS[2]
            elif dx == 4:
                col = BRASS[1]
            elif dx <= 6:
                # Họa tiết sóng cuộn
                col = BRASS[3] if (y % 4 < 2) else BRASS[2]
            elif dx == 17:
                col = BRASS[0]
            else:
                col = STEEL[1] if (dx > 12) else BRASS[2]
            img[y, x] = hexc(col)

    # 4 đinh tán đồng lớn ở 4 góc cánh nẹp
    for cx_b, cy_b in ((6, 6), (6, h - 7), (w - 7, 6), (w - 7, h - 7)):
        img[cy_b, cx_b] = hexc(BRASS[4])
        img[cy_b, cx_b + 1] = hexc(BRASS[3])
        img[cy_b + 1, cx_b] = hexc(BRASS[3])
        img[cy_b + 1, cx_b + 1] = hexc(BRASS[1])
        img[cy_b, cx_b] = hexc(SPECULAR_GOLD)

    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 4. UI_ButtonWide (48x24, 9-slice border L=12, R=12, T=6, B=6) ─────────────────
def make_button_wide():
    """Nút menu lớn 48x24, 9-slice border (12, 6, 12, 6).
    Khung kim loại vân ván gỗ công nghiệp, xám trung tính sáng để game nhuộm màu hoàn hảo."""
    w, h = 48, 24
    img = blank(w, h)

    for y in range(h):
        for x in range(w):
            d_tb = min(y, h - 1 - y)
            d_lr = min(x, w - 1 - x)
            d = min(d_tb, d_lr)
            if d == 0:
                c = GRAY[0]
            elif d == 1:
                c = GRAY[4] if (y <= 1 or x <= 1) and not (y >= h - 2 or x >= w - 2) else GRAY[1]
            elif d == 2:
                c = GRAY[3] if (y <= 2 or x <= 2) and not (y >= h - 3 or x >= w - 3) else GRAY[1]
            elif d == 3:
                c = GRAY[0]  # Khung rãnh nẹp
            elif d == 4:
                c = GRAY[3] if (y <= 4 or x <= 4) else GRAY[1]
            else:
                # Mặt thân nút: vân ván gỗ ngang xám trung tính
                if y in (8, 15):
                    c = GRAY[1]  # Khe ngang giữa các tấm ván
                elif y in (9, 16):
                    c = GRAY[3]  # Mép sáng ván
                else:
                    c = GRAY[2]
            img[y, x] = hexc(c)

    # Đinh tán nẹp 2 đầu (x = 5, y = 5 và h - 6)
    for cx_btn, cy_btn in ((5, 6), (5, h - 7), (w - 6, 6), (w - 6, h - 7)):
        img[cy_btn, cx_btn] = hexc(GRAY[4])
        img[cy_btn, cx_btn + 1] = hexc(GRAY[3])
        img[cy_btn + 1, cx_btn] = hexc(GRAY[3])
        img[cy_btn + 1, cx_btn + 1] = hexc(GRAY[1])

    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')


# ── VALIDATION ───────────────────────────────────────────────────────────────────
ALLOWED_HEX = set()
for d, colors in PALETTE.items():
    for col in colors:
        ALLOWED_HEX.add(col.lower())
for col in STEEL + BRASS + GRAY:
    ALLOWED_HEX.add(col.lower())
ALLOWED_HEX.add(DARK_INSET.lower())
ALLOWED_HEX.add(DARK_PANEL.lower())
ALLOWED_HEX.add(DARK_INSET_BG.lower())
ALLOWED_HEX.add(SPECULAR_GOLD.lower())
ALLOWED_HEX.add(OUTLINE_DARK.lower())

def validate_sprite(name, im, exp_w, exp_h):
    w, h = im.size
    assert (w, h) == (exp_w, exp_h), f"{name}: size ({w},{h}) != ({exp_w},{exp_h})"
    arr = np.array(im)
    for y in range(h):
        for x in range(w):
            r, g, b, a = arr[y, x]
            if a == 0:
                continue
            assert a == 255, f"{name} pixel ({x},{y}) có alpha {a} != 255"
            hcode = f"#{r:02x}{g:02x}{b:02x}"
            assert hcode in ALLOWED_HEX, f"{name} pixel ({x},{y}) màu {hcode} nằm ngoài bảng màu!"
    print(f"  [OK] {name} ({w}x{h}, alpha 255, palette 100% hợp lệ)")


# ── PREVIEW COMPOSITE CHO LÔ B ───────────────────────────────────────────────────
def render_9slice(sprite, target_w, target_h, border):
    sw, sh = sprite.size
    bl, bt, br, bb = border
    result = Image.new('RGBA', (target_w, target_h), (0, 0, 0, 0))
    c_tl = sprite.crop((0, 0, bl, bt))
    c_tr = sprite.crop((sw - br, 0, sw, bt))
    c_bl = sprite.crop((0, sh - bb, bl, sh))
    c_br = sprite.crop((sw - br, sh - bb, sw, sh))
    result.paste(c_tl, (0, 0))
    result.paste(c_tr, (target_w - br, 0))
    result.paste(c_bl, (0, target_h - bb))
    result.paste(c_br, (target_w - br, target_h - bb))
    mw = target_w - bl - br
    mh = target_h - bt - bb
    if mw > 0:
        edge_t = sprite.crop((bl, 0, sw - br, bt)).resize((mw, bt), Image.Resampling.NEAREST)
        edge_b = sprite.crop((bl, sh - bb, sw - br, sh)).resize((mw, bb), Image.Resampling.NEAREST)
        result.paste(edge_t, (bl, 0))
        result.paste(edge_b, (bl, target_h - bb))
    if mh > 0:
        edge_l = sprite.crop((0, bt, bl, sh - bb)).resize((bl, mh), Image.Resampling.NEAREST)
        edge_r = sprite.crop((sw - br, bt, sw, sh - bb)).resize((br, mh), Image.Resampling.NEAREST)
        result.paste(edge_l, (0, bt))
        result.paste(edge_r, (target_w - br, bt))
    if mw > 0 and mh > 0:
        center = sprite.crop((bl, bt, sw - br, sh - bb)).resize((mw, mh), Image.Resampling.NEAREST)
        result.paste(center, (bl, bt))
    return result

def tint_image(img, tint_rgba):
    """Nhuộm màu Image.color giống Unity."""
    arr = np.array(img, dtype=np.float32)
    tr, tg, tb, ta = tint_rgba
    arr[:, :, 0] = arr[:, :, 0] * (tr / 255.0)
    arr[:, :, 1] = arr[:, :, 1] * (tg / 255.0)
    arr[:, :, 2] = arr[:, :, 2] * (tb / 255.0)
    arr[:, :, 3] = arr[:, :, 3] * (ta / 255.0)
    return Image.fromarray(arr.astype(np.uint8), 'RGBA')

def generate_previews(bg_art, emblem_art, title_plate_art, button_wide_art):
    """Tạo ảnh xem thử giao diện MainMenu ở 1920x1080 (16:9) và 2400x1080 (20:9)."""
    for cw, ch, suffix in ((1920, 1080, '1920'), (2400, 1080, '2400')):
        # 1. Nền Bg_MainMenu (phóng to theo EnvelopeParent)
        # Nền gốc là 800x360 (tỉ lệ 20:9 = 2.222)
        # Ở 2400x1080, tỉ lệ là 20:9 -> khớp hoàn hảo (800x3 = 2400, 360x3 = 1080)
        # Ở 1920x1080, tỉ lệ là 16:9 -> cắt đều 2 bên (240px mỗi bên) giữ an toàn trung tâm!
        bg_scaled = bg_art.resize((2400, 1080), Image.Resampling.NEAREST)
        crop_x = (2400 - cw) // 2
        bg = bg_scaled.crop((crop_x, 0, crop_x + cw, 1080))

        # Phủ lớp tối 25% phía sau các nút ở trung tâm để menu nổi bật
        center_x = cw // 2

        # 2. Logo Emblem (96x96 art x 3 = 288x288 canvas px)
        emblem_scaled = emblem_art.resize((260, 260), Image.Resampling.NEAREST)
        bg.paste(emblem_scaled, (center_x - 130, 80), emblem_scaled)

        # 3. Title Plate (80x28 art -> 9-slice 620x140 canvas px)
        plate_sliced = render_9slice(title_plate_art, 206, 46, (20, 7, 20, 7)).resize((618, 138), Image.Resampling.NEAREST)
        bg.paste(plate_sliced, (center_x - 309, 360), plate_sliced)

        # 4. Các nút menu lớn UI_ButtonWide (500x110 canvas px)
        btn_sliced = render_9slice(button_wide_art, 166, 36, (12, 6, 12, 6)).resize((498, 108), Image.Resampling.NEAREST)

        # Nhuộm màu các nút theo palette đã chốt trong ApplyPackageU4:
        # PlayPvE (emerald)
        btn_pve = tint_image(btn_sliced, (45, 155, 90, 255))
        bg.paste(btn_pve, (center_x - 249, 530), btn_pve)

        # PlayPvP (royal blue)
        btn_pvp = tint_image(btn_sliced, (50, 115, 190, 255))
        bg.paste(btn_pvp, (center_x - 249, 660), btn_pvp)

        # Settings (slate gray)
        btn_settings = tint_image(btn_sliced, (90, 105, 125, 255))
        bg.paste(btn_settings, (center_x - 249, 790), btn_settings)

        # Quit (crimson)
        btn_quit = tint_image(btn_sliced, (140, 55, 55, 255))
        bg.paste(btn_quit, (center_x - 249, 920), btn_quit)

        # 5. Nút Reset Guest ở góc trên-trái (280x90 canvas px)
        btn_guest_sliced = render_9slice(button_wide_art, 94, 30, (12, 6, 12, 6)).resize((282, 90), Image.Resampling.NEAREST)
        btn_guest = tint_image(btn_guest_sliced, (105, 105, 120, 240))
        bg.paste(btn_guest, (50, 45), btn_guest)

        preview_path = os.path.join(OUT_DIR, f'preview_B_{suffix}.png')
        bg.save(preview_path)
        print(f"  [PREVIEW] Đã tạo ảnh xem thử: {preview_path} ({cw}x{ch})")

def main():
    print("=== TẠO BỘ GIAO DIỆN PIXEL ART LÔ B (MÀN HÌNH CHÍNH, ĐĂNG NHẬP, TẢI) ===")
    bg = make_bg_mainmenu()
    validate_sprite('Bg_MainMenu', bg, 800, 360)
    bg.save(os.path.join(OUT_DIR, 'Bg_MainMenu.png'))

    emblem = make_logo_emblem()
    validate_sprite('Logo_Emblem', emblem, 96, 96)
    emblem.save(os.path.join(OUT_DIR, 'Logo_Emblem.png'))

    plate = make_title_plate()
    validate_sprite('UI_TitlePlate', plate, 80, 28)
    plate.save(os.path.join(OUT_DIR, 'UI_TitlePlate.png'))

    btn_wide = make_button_wide()
    validate_sprite('UI_ButtonWide', btn_wide, 48, 24)
    btn_wide.save(os.path.join(OUT_DIR, 'UI_ButtonWide.png'))

    print("\n--- Đang tạo ảnh xem thử Preview MainMenu (1920x1080 & 2400x1080) ---")
    generate_previews(bg, emblem, plate, btn_wide)
    print("\n>>> HOÀN THÀNH TẤT CẢ SPRITE LÔ B!")

if __name__ == '__main__':
    main()
