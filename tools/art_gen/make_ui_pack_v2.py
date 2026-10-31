"""make_ui_pack_v2.py - Bộ sprite UI pixel art mới cho Game2D (Lô A - Khung, Nút, Thanh).

Tuân thủ nghiêm ngặt:
- Art Contract (docs/ref/14_ART_CONTRACT.md, 14A, 15)
- Bảng màu: PALETTE (pixelkit.py) + STEEL, BRASS, GRAY (make_ui_pack.py)
- Quy tắc tỷ lệ: 1 art pixel = 3 canvas pixels (1080p canvas)
- Alpha: 0 hoặc 255 (trừ vùng mờ cố ý: UI_Panel alpha=230, UI_PanelInset alpha=240, UI_Toast alpha=220, UI_Row alpha=240)
"""
import os
import sys
import numpy as np
from PIL import Image

# Import PALETTE from pixelkit
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pixelkit import PALETTE

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out_ui_v2')
os.makedirs(OUT_DIR, exist_ok=True)

# ── BẢNG MÀU CHÍNH THỨC SÁNG ĐẸP CHO GIAO DIỆN (THEME HẢI ĐẢO / THÁP THỦY TRIỀU) ──
STEEL = ['#141d26', '#25374a', '#364c66', '#5d7d9f', '#91b1d4']
BRASS = ['#3a2609', '#6d4c12', '#ad8322', '#e2b63f', '#ffe894']
GRAY  = ['#2a333e', '#485566', '#728296', '#c0ccdc', '#ffffff']

DARK_INSET    = '#162330'
DARK_PANEL    = '#223244'   # Tông xanh than slate sáng rõ, không bị tối đen
DARK_INSET_BG = '#1a2838'   # Tông nền ô chứa/slot
CHECK_GREEN   = '#4ee44e'   # Xanh ngọc tươi
SPECULAR_GOLD = '#fff2b0'   # Điểm sáng vàng beacon
OUTLINE_DARK  = '#101720'

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
    if count >= 2:
        for x, y in ((1, 0), (0, 1), (w - 2, 0), (w - 1, 1),
                     (0, h - 2), (1, h - 1), (w - 1, h - 2), (w - 2, h - 1)):
            img[y, x] = (0, 0, 0, 0)
    return img

# ── 1. UI_Panel (48x48, 9-slice border 8) ─────────────────────────────────────────
def make_panel():
    """Khung bảng 48x48, 9-slice border 8.
    Tâm mờ (alpha 230 ~ 90%), viền thép nhiều tầng, 4 góc đinh tán đồng."""
    s = 48
    img = blank(s, s)
    for y in range(s):
        for x in range(s):
            d = min(x, y, s - 1 - x, s - 1 - y)
            if d == 0:
                c = STEEL[0]
            elif d == 1:
                # Vát sáng trên-trái, bóng dưới-phải
                c = STEEL[4] if (y <= 1 or x <= 1) and not (y >= s - 2 or x >= s - 2) else STEEL[1]
            elif d == 2:
                c = STEEL[3] if (y <= 2 or x <= 2) and not (y >= s - 3 or x >= s - 3) else STEEL[1]
            elif d == 3:
                c = STEEL[2]
            elif d == 4:
                c = STEEL[0]
            elif d == 5:
                c = STEEL[1] if (y <= 5 or x <= 5) else STEEL[2]
            elif d == 6:
                c = STEEL[1]
            elif d == 7:
                c = DARK_INSET
            else:
                c = DARK_PANEL
            
            # Gán màu và alpha (tâm d >= 8 có alpha 230)
            a = 230 if d >= 8 else 255
            img[y, x] = hexc(c, a)

    # Đinh tán đồng 2x2 ở 4 góc (khoảng cách an toàn d = 3..4)
    for cx, cy in ((3, 3), (s - 5, 3), (3, s - 5), (s - 5, s - 5)):
        img[cy, cx] = hexc(BRASS[4])
        img[cy, cx + 1] = hexc(BRASS[3])
        img[cy + 1, cx] = hexc(BRASS[3])
        img[cy + 1, cx + 1] = hexc(BRASS[1])
        # Điểm sáng vàng beacon ở góc trên-trái đinh tán
        img[cy, cx] = hexc(SPECULAR_GOLD)

    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 2. UI_Button (24x24, 9-slice border 6) ────────────────────────────────────────
def make_button():
    """Nút 24x24, 9-slice border 6. Màu xám trung tính sáng vừa để game nhuộm màu."""
    s = 24
    img = blank(s, s)
    for y in range(s):
        for x in range(s):
            d = min(x, y, s - 1 - x, s - 1 - y)
            if d == 0:
                c = GRAY[0]
            elif d == 1:
                c = GRAY[4] if (y <= 1 or x <= 1) and not (y >= s - 2 or x >= s - 2) else GRAY[1]
            elif d == 2:
                c = GRAY[3] if (y <= 2 or x <= 2) and not (y >= s - 3 or x >= s - 3) else GRAY[1]
            elif d == 3:
                c = GRAY[2]
            elif d == 4:
                c = GRAY[3] if (y <= 4 or x <= 4) and not (y >= s - 5 or x >= s - 5) else GRAY[1]
            elif d == 5:
                c = GRAY[2]
            else:
                c = GRAY[2]
            img[y, x] = hexc(c)

    # Chấm đinh tán nhẹ ở góc (d=2..3)
    for cx, cy in ((3, 3), (s - 4, 3), (3, s - 4), (s - 4, s - 4)):
        img[cy, cx] = hexc(GRAY[4])

    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 3. UI_BarFrame (16x16, 9-slice border 4) ──────────────────────────────────────
def make_bar_frame():
    """Khung thanh trũng 16x16, 9-slice border 4. Góc đinh tán đồng."""
    s = 16
    img = blank(s, s)
    for y in range(s):
        for x in range(s):
            d = min(x, y, s - 1 - x, s - 1 - y)
            if d == 0:
                c = STEEL[0]
            elif d == 1:
                c = STEEL[4] if (y <= 1 or x <= 1) and not (y >= s - 2 or x >= s - 2) else STEEL[1]
            elif d == 2:
                c = STEEL[1] if (y <= 2 or x <= 2) else STEEL[2]
            elif d == 3:
                c = STEEL[0]
            else:
                c = DARK_INSET
            img[y, x] = hexc(c)

    # Đinh tán góc bằng đồng
    for cx, cy in ((1, 1), (s - 2, 1), (1, s - 2), (s - 2, s - 2)):
        img[cy, cx] = hexc(BRASS[3])
    img[1, 1] = hexc(BRASS[4])

    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 4. UI_BarFill (8x16, dải màu đổ đầy) ──────────────────────────────────────────
def make_bar_fill():
    """Dải đổ đầy 8x16, chuyển sắc xám trung tính theo chiều dọc để game nhuộm màu."""
    w, h = 8, 16
    img = blank(w, h)
    # Gradient từ sáng trên đỉnh xuống tối dần ở đáy, vệt phản quang ở giữa
    rows = [
        GRAY[4],  # Hàng 0: Điểm sáng bóng viền
        GRAY[4],  # Hàng 1
        GRAY[3],  # Hàng 2
        GRAY[3],  # Hàng 3
        GRAY[3],  # Hàng 4
        GRAY[3],  # Hàng 5
        GRAY[2],  # Hàng 6
        GRAY[2],  # Hàng 7
        GRAY[2],  # Hàng 8
        GRAY[2],  # Hàng 9
        GRAY[1],  # Hàng 10
        GRAY[1],  # Hàng 11
        GRAY[1],  # Hàng 12
        GRAY[1],  # Hàng 13
        GRAY[0],  # Hàng 14
        GRAY[0],  # Hàng 15: Chân đáy tối
    ]
    for y in range(h):
        for x in range(w):
            c = rows[y]
            # Mép trái và phải sẫm nhẹ 1 bậc để tạo độ cong hình ống năng lượng
            if (x == 0 or x == w - 1) and y > 1 and y < h - 1:
                # Sẫm hơn 1 bậc
                if c == GRAY[4]: c = GRAY[3]
                elif c == GRAY[3]: c = GRAY[2]
                elif c == GRAY[2]: c = GRAY[1]
                elif c == GRAY[1]: c = GRAY[0]
            img[y, x] = hexc(c)
    return Image.fromarray(img, 'RGBA')

# ── 5. UI_Knob (24x24, núm tròn đồng joystick) ────────────────────────────────────
def make_knob():
    """Núm tròn 24x24 bằng đồng có khấc định hướng và điểm sáng hải đăng."""
    s = 24
    img = blank(s, s)
    c = (s - 1) / 2.0
    for y in range(s):
        for x in range(s):
            dx = x - c
            dy = y - c
            dist = (dx * dx + dy * dy) ** 0.5
            if dist <= 11.2:
                if dist > 10.0:
                    col = STEEL[0]
                elif dist > 8.5:
                    # Vành ngoài bằng đồng có ánh sáng từ trên-trái
                    t = (dx * -0.7 + dy * -0.7) / dist
                    col = BRASS[4] if t > 0.4 else BRASS[3] if t > 0.0 else BRASS[2] if t > -0.4 else BRASS[1]
                elif dist > 7.2:
                    col = STEEL[1]
                else:
                    # Mái vòm tròn trung tâm
                    t = (dx * -0.6 + dy * -0.8) / 7.2
                    col = BRASS[4] if t > 0.5 else BRASS[3] if t > 0.1 else BRASS[2] if t > -0.4 else BRASS[1]
                img[y, x] = hexc(col)

    # 4 khấc định hướng N, S, W, E
    img[2, 11] = hexc(BRASS[4]); img[2, 12] = hexc(BRASS[4])
    img[21, 11] = hexc(BRASS[1]); img[21, 12] = hexc(BRASS[1])
    img[11, 2] = hexc(BRASS[4]); img[12, 2] = hexc(BRASS[4])
    img[11, 21] = hexc(BRASS[1]); img[12, 21] = hexc(BRASS[1])

    # Điểm sáng vàng beacon specular
    img[7, 8] = hexc(SPECULAR_GOLD)
    img[7, 9] = hexc(BRASS[4])
    img[8, 8] = hexc(BRASS[4])

    return Image.fromarray(img, 'RGBA')

# ── 6. UI_Check (16x16, dấu tích xanh ngọc) ───────────────────────────────────────
def make_check():
    """Dấu tích 16x16 xanh ngọc viền tối sắc nét."""
    s = 16
    img = blank(s, s)
    pts = [(3, 8), (4, 9), (5, 10), (6, 11), (7, 10), (8, 9), (9, 8), (10, 7), (11, 6), (12, 5)]
    # Thân dấu tích dày 2px
    for x, y in pts:
        img[y, x] = hexc(CHECK_GREEN)
        img[y + 1, x] = hexc(CHECK_GREEN)
        img[y, x + 1] = hexc(CHECK_GREEN)

    # Viền đen bao bọc
    for x, y in pts:
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (1, 1), (-1, 1), (1, -1),
                       (0, 2), (1, 2), (-1, 2), (2, 0), (2, 1), (0, -2)):
            xx, yy = x + dx, y + dy
            if 0 <= xx < s and 0 <= yy < s:
                if img[yy, xx][3] == 0:
                    img[yy, xx] = hexc(STEEL[0])

    # Điểm sáng đỉnh ngọn
    img[5, 12] = hexc(SPECULAR_GOLD)
    return Image.fromarray(img, 'RGBA')

# ── 7. UI_PanelInset (24x24, 9-slice border 6) ───────────────────────────────────
def make_panel_inset():
    """Ô trũng tối 24x24, 9-slice border 6. Dùng cho vùng danh sách, ô mô tả, kho đồ."""
    s = 24
    img = blank(s, s)
    for y in range(s):
        for x in range(s):
            d = min(x, y, s - 1 - x, s - 1 - y)
            if d == 0:
                c = STEEL[0]
            elif d == 1:
                # Đổ bóng tối ở mép trên-trái, viền sáng hắt ngược ở mép dưới-phải
                c = DARK_INSET if (y <= 1 or x <= 1) else STEEL[2]
            elif d == 2:
                c = STEEL[0] if (y <= 2 or x <= 2) else STEEL[1]
            elif d == 3:
                c = STEEL[1] if (y <= 3 or x <= 3) else DARK_INSET_BG
            elif d == 4:
                c = DARK_INSET
            else:
                c = DARK_INSET_BG
            
            a = 240 if d >= 5 else 255
            img[y, x] = hexc(c, a)

    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 8. UI_Header (48x24, 9-slice border L=16, R=16, T=6, B=6) ─────────────────────
def make_header():
    """Tấm tiêu đề 48x24, 9-slice border (16, 6, 16, 6). Hai đầu nẹp đồng có đinh tán."""
    w, h = 48, 24
    img = blank(w, h)
    for y in range(h):
        for x in range(w):
            d_tb = min(y, h - 1 - y)
            if d_tb == 0:
                c = STEEL[0]
            elif d_tb == 1:
                c = STEEL[4] if y <= 1 else STEEL[1]
            elif d_tb == 2:
                c = STEEL[3] if y <= 2 else STEEL[1]
            elif d_tb == 3:
                c = STEEL[2]
            elif d_tb == 4:
                c = STEEL[1]
            else:
                c = STEEL[2]
            img[y, x] = hexc(c)

    # Nẹp đồng 2 bên (x < 8 và x >= w - 8)
    for y in range(1, h - 1):
        for x in list(range(1, 8)) + list(range(w - 8, w - 1)):
            dx = x if x < 8 else w - 1 - x
            if dx == 1:
                img[y, x] = hexc(BRASS[1])
            elif dx == 2:
                img[y, x] = hexc(BRASS[4] if y < 4 else BRASS[3])
            elif dx == 7:
                img[y, x] = hexc(BRASS[0])
            else:
                img[y, x] = hexc(BRASS[2])

    # Đinh tán đồng ở 2 đầu nẹp
    for cx, cy in ((4, 5), (4, h - 6), (w - 5, 5), (w - 5, h - 6)):
        img[cy, cx] = hexc(BRASS[4])
        img[cy, cx + 1] = hexc(BRASS[3])
        img[cy + 1, cx] = hexc(BRASS[3])
        img[cy + 1, cx + 1] = hexc(BRASS[1])
        img[cy, cx] = hexc(SPECULAR_GOLD)

    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 9. UI_Slot (24x24, 9-slice border 5) ──────────────────────────────────────────
def make_slot():
    """Ô chứa vật phẩm 24x24, 9-slice border 5. Viền ngàm đồng 4 góc."""
    s = 24
    img = blank(s, s)
    for y in range(s):
        for x in range(s):
            d = min(x, y, s - 1 - x, s - 1 - y)
            if d == 0:
                c = STEEL[0]
            elif d == 1:
                c = DARK_INSET if (y <= 1 or x <= 1) else STEEL[2]
            elif d == 2:
                c = STEEL[0] if (y <= 2 or x <= 2) else STEEL[1]
            elif d == 3:
                c = DARK_INSET
            else:
                c = DARK_INSET_BG
            img[y, x] = hexc(c)

    # 4 ngàm góc bằng đồng 3x3
    for cx, cy, sx, sy in ((1, 1, 1, 1), (s - 2, 1, -1, 1), (1, s - 2, 1, -1), (s - 2, s - 2, -1, -1)):
        img[cy, cx] = hexc(BRASS[4] if sy > 0 else BRASS[2])
        img[cy, cx + sx] = hexc(BRASS[3])
        img[cy + sy, cx] = hexc(BRASS[3])

    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 10. UI_Row (32x16, 9-slice border L=8, R=8, T=4, B=4) ─────────────────────────
def make_row():
    """Hàng danh sách 32x16, 9-slice border (8, 4, 8, 4). Có khấc viền thép."""
    w, h = 32, 16
    img = blank(w, h)
    for y in range(h):
        for x in range(w):
            d = min(x, y, w - 1 - x, h - 1 - y)
            if d == 0:
                c = STEEL[0]
            elif d == 1:
                c = STEEL[3] if y <= 1 or x <= 1 else STEEL[1]
            elif d == 2:
                c = STEEL[3] if y <= 2 or x <= 2 else STEEL[2]
            else:
                c = STEEL[2]
            img[y, x] = hexc(c, 240)

    # Khấc sáng nhẹ ở 2 đầu
    img[1, 2] = hexc(STEEL[4]); img[1, w - 3] = hexc(STEEL[4])
    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 11. UI_Toast (32x20, 9-slice border L=8, R=8, T=6, B=6) ───────────────────────
def make_toast():
    """Khung thông báo nổi 32x20, 9-slice border (8, 6, 8, 6). Nền bán mờ (alpha 220)."""
    w, h = 32, 20
    img = blank(w, h)
    for y in range(h):
        for x in range(w):
            d = min(x, y, w - 1 - x, h - 1 - y)
            if d == 0:
                c = STEEL[0]
            elif d == 1:
                c = STEEL[4] if (y <= 1 or x <= 1) and not (y >= h - 2 or x >= w - 2) else STEEL[1]
            elif d == 2:
                c = STEEL[3] if y <= 2 else STEEL[1]
            elif d == 3:
                c = STEEL[2]
            elif d == 4:
                c = STEEL[0]
            else:
                c = DARK_PANEL
            
            a = 220 if d >= 4 else 255
            img[y, x] = hexc(c, a)

    # Đinh tán đồng ở 2 mép
    for cx, cy in ((3, h // 2), (w - 4, h // 2)):
        img[cy, cx] = hexc(BRASS[4])
        img[cy, cx + 1] = hexc(BRASS[2])

    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 12. UI_ScrollTrack (8x24, 9-slice border L=2, R=2, T=4, B=4) ───────────────────
def make_scroll_track():
    """Rãnh trượt dọc 8x24, 9-slice border (2, 4, 2, 4)."""
    w, h = 8, 24
    img = blank(w, h)
    for y in range(h):
        for x in range(w):
            if x == 0:
                c = STEEL[0]
            elif x == 1:
                c = DARK_INSET
            elif x == w - 2:
                c = STEEL[2]
            elif x == w - 1:
                c = STEEL[0]
            else:
                c = DARK_INSET_BG
            if y == 0 or y == h - 1:
                c = STEEL[0]
            img[y, x] = hexc(c)
    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 13. UI_ScrollHandle (8x16, 9-slice border L=2, R=2, T=4, B=4) ─────────────────
def make_scroll_handle():
    """Con trượt 8x16, 9-slice border (2, 4, 2, 4). Có khấc tay cầm đồng."""
    w, h = 8, 16
    img = blank(w, h)
    for y in range(h):
        for x in range(w):
            d = min(x, y, w - 1 - x, h - 1 - y)
            if d == 0:
                c = BRASS[0]
            elif d == 1:
                c = BRASS[4] if y <= 1 or x <= 1 else BRASS[1]
            else:
                c = BRASS[3] if y < h // 2 else BRASS[2]
            img[y, x] = hexc(c)

    # 3 khấc gờ ngang tay cầm
    for y in (h // 2 - 2, h // 2, h // 2 + 2):
        img[y, 2:w - 2] = [hexc(BRASS[4])] * (w - 4)
        img[y + 1, 2:w - 2] = [hexc(BRASS[1])] * (w - 4)

    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')

# ── 14. UI_Divider (16x4, 9-slice border L=4, R=4, T=1, B=1) ──────────────────────
def make_divider():
    """Đường kẻ phân cách 16x4, 9-slice border (4, 1, 4, 1). Có viên thoi đồng ở giữa."""
    w, h = 16, 4
    img = blank(w, h)
    # Đường kẻ thép
    for x in range(w):
        img[0, x] = hexc(STEEL[0])
        img[1, x] = hexc(STEEL[3])
        img[2, x] = hexc(STEEL[1])
        img[3, x] = hexc(STEEL[0])

    # Viên thoi đồng trung tâm ở x = 6..9
    mid = w // 2
    img[0, mid - 1 : mid + 1] = [hexc(BRASS[4]), hexc(BRASS[4])]
    img[1, mid - 2 : mid + 2] = [hexc(BRASS[4]), hexc(SPECULAR_GOLD), hexc(BRASS[3]), hexc(BRASS[2])]
    img[2, mid - 2 : mid + 2] = [hexc(BRASS[2]), hexc(BRASS[3]), hexc(BRASS[2]), hexc(BRASS[1])]
    img[3, mid - 1 : mid + 1] = [hexc(BRASS[1]), hexc(BRASS[1])]

    return Image.fromarray(img, 'RGBA')


# ── KIỂM TRA MÀU VÀ KÍCH THƯỚC (VALIDATION) ───────────────────────────────────────
ALLOWED_HEX = set()
for d, colors in PALETTE.items():
    for col in colors:
        ALLOWED_HEX.add(col.lower())
for col in STEEL + BRASS + GRAY:
    ALLOWED_HEX.add(col.lower())
ALLOWED_HEX.add(DARK_INSET.lower())
ALLOWED_HEX.add(DARK_PANEL.lower())
ALLOWED_HEX.add(DARK_INSET_BG.lower())
ALLOWED_HEX.add(CHECK_GREEN.lower())
ALLOWED_HEX.add(SPECULAR_GOLD.lower())
ALLOWED_HEX.add(OUTLINE_DARK.lower())

def validate_sprite(name, im, expected_w, expected_h, allowed_translucent=False):
    w, h = im.size
    assert (w, h) == (expected_w, expected_h), f"{name}: kích thước ({w},{h}) != ({expected_w},{expected_h})"
    arr = np.array(im)
    for y in range(h):
        for x in range(w):
            r, g, b, a = arr[y, x]
            if a == 0:
                continue
            if not allowed_translucent:
                assert a == 255, f"{name} pixel ({x},{y}) có alpha {a} không phải 255"
            else:
                assert a in (220, 230, 240, 255), f"{name} pixel ({x},{y}) có alpha {a} không hợp lệ"
            hcode = f"#{r:02x}{g:02x}{b:02x}"
            assert hcode in ALLOWED_HEX, f"{name} pixel ({x},{y}) màu {hcode} nằm ngoài bảng màu!"
    print(f"  [OK] {name} ({w}x{h}, alpha valid, palette valid)")


# ── XUẤT SPRITE VÀ BẢO ĐẢM TOÀN BỘ LÔ A ───────────────────────────────────────────
SPRITES_BATCH_A = [
    ('UI_Panel',        make_panel(),        48, 48, True),
    ('UI_Button',       make_button(),       24, 24, False),
    ('UI_BarFrame',     make_bar_frame(),    16, 16, False),
    ('UI_BarFill',      make_bar_fill(),      8, 16, False),
    ('UI_Knob',         make_knob(),         24, 24, False),
    ('UI_Check',        make_check(),        16, 16, False),
    ('UI_PanelInset',   make_panel_inset(),  24, 24, True),
    ('UI_Header',       make_header(),       48, 24, False),
    ('UI_Slot',         make_slot(),         24, 24, False),
    ('UI_Row',          make_row(),          32, 16, True),
    ('UI_Toast',        make_toast(),        32, 20, True),
    ('UI_ScrollTrack',  make_scroll_track(),  8, 24, False),
    ('UI_ScrollHandle', make_scroll_handle(), 8, 16, False),
    ('UI_Divider',      make_divider(),      16,  4, False),
]

def render_9slice(sprite, target_w, target_h, border):
    """Giả lập 9-slice của Unity trong PIL."""
    sw, sh = sprite.size
    bl, bt, br, bb = border
    result = Image.new('RGBA', (target_w, target_h), (0, 0, 0, 0))
    
    # Góc không kéo giãn
    c_tl = sprite.crop((0, 0, bl, bt))
    c_tr = sprite.crop((sw - br, 0, sw, bt))
    c_bl = sprite.crop((0, sh - bb, bl, sh))
    c_br = sprite.crop((sw - br, sh - bb, sw, sh))
    
    result.paste(c_tl, (0, 0))
    result.paste(c_tr, (target_w - br, 0))
    result.paste(c_bl, (0, target_h - bb))
    result.paste(c_br, (target_w - br, target_h - bb))
    
    # Cạnh kéo giãn (nearest)
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

def generate_previews(sprites_dict):
    """Ghép ảnh xem thử lên screenshot thật ở 1920x1080 và 2400x1080."""
    pve_bg_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', '_Game', 'screenshot_pve_new_look.png')
    bg_orig = Image.open(pve_bg_path).convert('RGBA')

    for cw, ch, suffix in ((1920, 1080, '1920'), (2400, 1080, '2400')):
        # Phủ kín nền
        bg = bg_orig.resize((cw, ch), Image.Resampling.BILINEAR)
        # Làm tối nền 30% để UI nổi bật
        darken = Image.new('RGBA', (cw, ch), (0, 0, 0, 75))
        bg = Image.alpha_composite(bg, darken)

        # 1. Bảng lớn giữa màn hình (9-slice UI_Panel 720x540 canvas px)
        panel_sliced = render_9slice(sprites_dict['UI_Panel'], 240, 180, (8, 8, 8, 8))
        panel_art = panel_sliced.resize((720, 540), Image.Resampling.NEAREST)
        px = (cw - 720) // 2
        py = (ch - 540) // 2
        bg.paste(panel_art, (px, py), panel_art)

        # 2. Header banner trên bảng (UI_Header 360x72 canvas px)
        header_sliced = render_9slice(sprites_dict['UI_Header'], 120, 24, (16, 6, 16, 6))
        header_art = header_sliced.resize((360, 72), Image.Resampling.NEAREST)
        bg.paste(header_art, (px + (720 - 360) // 2, py - 36), header_art)

        # 3. Vùng ô trũng chứa đồ (UI_PanelInset 640x300 canvas px)
        inset_sliced = render_9slice(sprites_dict['UI_PanelInset'], 214, 100, (6, 6, 6, 6))
        inset_art = inset_sliced.resize((642, 300), Image.Resampling.NEAREST)
        bg.paste(inset_art, (px + 39, py + 70), inset_art)

        # 4. Các ô slot bên trong (UI_Slot 72x72 canvas px = 24x24 art x 3)
        slot_art = sprites_dict['UI_Slot'].resize((72, 72), Image.Resampling.NEAREST)
        for col in range(6):
            for row in range(3):
                sx = px + 65 + col * 90
                sy = py + 95 + row * 85
                bg.paste(slot_art, (sx, sy), slot_art)

        # 5. Thanh trượt dọc bên phải (UI_ScrollTrack + UI_ScrollHandle)
        st_sliced = render_9slice(sprites_dict['UI_ScrollTrack'], 8, 90, (2, 4, 2, 4)).resize((24, 270), Image.Resampling.NEAREST)
        sh_art = sprites_dict['UI_ScrollHandle'].resize((24, 48), Image.Resampling.NEAREST)
        bg.paste(st_sliced, (px + 640, py + 85), st_sliced)
        bg.paste(sh_art, (px + 640, py + 120), sh_art)

        # 6. Đường phân cách (UI_Divider 640x12 canvas px)
        div_sliced = render_9slice(sprites_dict['UI_Divider'], 214, 4, (4, 1, 4, 1)).resize((642, 12), Image.Resampling.NEAREST)
        bg.paste(div_sliced, (px + 39, py + 390), div_sliced)

        # 7. Nút tương tác bên dưới (UI_Button 210x66 canvas px)
        btn_sliced = render_9slice(sprites_dict['UI_Button'], 70, 22, (6, 6, 6, 6)).resize((210, 66), Image.Resampling.NEAREST)
        # Nút Đóng (nhuộm đỏ)
        btn_close = btn_sliced.copy()
        bg.paste(btn_close, (px + 80, py + 430), btn_close)
        # Nút Xác Nhận (nhuộm xanh)
        bg.paste(btn_sliced, (px + 430, py + 430), btn_sliced)

        # 8. Toast thông báo góc trên (UI_Toast 480x75 canvas px)
        toast_sliced = render_9slice(sprites_dict['UI_Toast'], 160, 25, (8, 6, 8, 6)).resize((480, 75), Image.Resampling.NEAREST)
        bg.paste(toast_sliced, ((cw - 480) // 2, 35), toast_sliced)

        # 9. Thanh máu mẫu góc trên-trái (UI_BarFrame + UI_BarFill 360x36 canvas px)
        bar_f = render_9slice(sprites_dict['UI_BarFrame'], 120, 12, (4, 4, 4, 4)).resize((360, 36), Image.Resampling.NEAREST)
        bar_fill = sprites_dict['UI_BarFill'].resize((280, 24), Image.Resampling.NEAREST)
        bg.paste(bar_f, (60, 50), bar_f)
        bg.paste(bar_fill, (70, 56), bar_fill)

        # 10. Núm joystick góc dưới-trái (UI_Knob 72x72 canvas px)
        knob_art = sprites_dict['UI_Knob'].resize((72, 72), Image.Resampling.NEAREST)
        bg.paste(knob_art, (120, ch - 190), knob_art)

        preview_file = os.path.join(OUT_DIR, f'preview_A_{suffix}.png')
        bg.save(preview_file)
        print(f"  [PREVIEW] Đã tạo ảnh xem thử: {preview_file} ({cw}x{ch})")

def main():
    print("=== TẠO BỘ GIAO DIỆN PIXEL ART LÔ A (KHUNG, NÚT, THANH) ===")
    root_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    art_ui_dir = os.path.join(root_dir, 'Assets', '_Game', 'Art', 'UI')
    res_dir = os.path.join(root_dir, 'Assets', '_Game', 'Resources', 'UISkin')
    os.makedirs(art_ui_dir, exist_ok=True)
    os.makedirs(res_dir, exist_ok=True)

    sprites_dict = {}
    for name, im, exp_w, exp_h, allow_trans in SPRITES_BATCH_A:
        validate_sprite(name, im, exp_w, exp_h, allow_trans)
        save_path = os.path.join(OUT_DIR, f"{name}.png")
        im.save(save_path)
        sprites_dict[name] = im

        # Chép sang Assets/_Game/Art/UI/
        art_path = os.path.join(art_ui_dir, f"{name}.png")
        im.save(art_path)

        # Chép sang Assets/_Game/Resources/UISkin/
        res_path = os.path.join(res_dir, f"{name}.png")
        if os.path.exists(res_path) or name in ('UI_Panel', 'UI_Button', 'UI_BarFrame', 'UI_PanelInset', 'UI_Header', 'UI_Slot', 'UI_Row', 'UI_Toast'):
            im.save(res_path)

    print("\n--- Đang tạo ảnh xem thử Preview (1920x1080 & 2400x1080) ---")
    generate_previews(sprites_dict)
    print("\n>>> HOÀN THÀNH TẤT CẢ SPRITE LÔ A!")

if __name__ == '__main__':
    main()
