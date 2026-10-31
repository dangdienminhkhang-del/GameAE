"""make_batch_D.py - Bộ sprite UI pixel art mới cho Game2D: LÔ D (Biểu tượng Menu & Đồng hồ Ngày/Đêm).

Tuân thủ nghiêm ngặt:
- Art Contract (docs/ref/14_ART_CONTRACT.md, 14A, 15)
- Bảng màu: PALETTE (pixelkit.py) + STEEL, BRASS, GRAY (make_ui_pack.py)
- Quy tắc tỷ lệ: 1 art pixel = 3 canvas pixels (1080p canvas)
- Danh mục:
  Menu Icons (24x24): Icon_Menu_Bag, Icon_Menu_Talk, Icon_Menu_Quest,
                      Icon_Menu_Floor, Icon_Menu_Pause, Icon_Menu_Character,
                      Icon_Menu_Settings, Icon_Close.
  Time Icons (24x24): Icon_Time_Dawn, Icon_Time_Morning, Icon_Time_Noon,
                      Icon_Time_Afternoon, Icon_Time_Evening, Icon_Time_Night.
  Seal Icon (48x48):  Icon_Seal (Huy hiệu niêm phong cho màn hình chết: dây thừng + ổ khóa + dấu sáp đỏ gỉ).
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
RUST   = PALETTE['rust']     # ['#33150f', '#66261a', '#97402b', '#c06848', '#df9570']

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

# ── 1. Icon_Menu_Bag (24x24, túi đồ thủy thủ) ────────────────────────────────────
def make_icon_menu_bag():
    """Túi da thủy thủ 24x24: Thân túi da nâu, nắp đậy viền đồng, quai đeo dây thừng."""
    s = 24
    img = blank(s, s)

    # Quai xách dây thừng trên đỉnh (y = 2..5, x = 8..15)
    for x in range(9, 15):
        img[3, x] = hexc(ROPE[3])
        img[4, x] = hexc(ROPE[1])
    img[4, 8] = hexc(ROPE[2]); img[4, 15] = hexc(ROPE[2])

    # Thân túi (y = 6..20, x = 4..19)
    for y in range(6, 21):
        w_half = 7 if y < 18 else 6
        for x in range(12 - w_half, 12 + w_half):
            dx = x - 11.5
            if y < 11:
                # Nắp túi
                col = BRASS[4] if y == 6 and dx < 0 else ROPE[3] if y == 6 else ROPE[2]
                if abs(dx) >= w_half - 1 or y == 10:
                    col = ROPE[1]
            else:
                # Đáy túi da
                col = ROPE[3] if dx < -2 else ROPE[2] if dx < 3 else ROPE[1]
                if abs(dx) == w_half - 1 or y == 20:
                    col = ROPE[0]
            img[y, x] = hexc(col)

    # Khóa gài đồng ở giữa nắp túi (x = 11..12, y = 9..12)
    img[9, 11] = hexc(BRASS[4]);  img[9, 12] = hexc(BRASS[4])
    img[10, 11] = hexc(SPECULAR_GOLD); img[10, 12] = hexc(BRASS[3])
    img[11, 11] = hexc(BRASS[3]); img[11, 12] = hexc(BRASS[1])
    img[12, 11] = hexc(BRASS[1]); img[12, 12] = hexc(BRASS[1])

    # 2 túi con 2 bên
    img[14, 5] = hexc(ROPE[3]); img[15, 5] = hexc(ROPE[1])
    img[14, 18] = hexc(ROPE[3]); img[15, 18] = hexc(ROPE[1])

    return Image.fromarray(img, 'RGBA')

# ── 2. Icon_Menu_Talk (24x24, bóng thoại hội thoại) ──────────────────────────────
def make_icon_menu_talk():
    """Bong bóng đối thoại hải đồ 24x24: Viền đồng, lòng xanh than tối có 3 chấm sáng."""
    s = 24
    img = blank(s, s)

    # Bong bóng oval (cx=11.5, cy=10.5, rx=8.5, ry=6.5)
    for y in range(4, 18):
        for x in range(3, 21):
            val = ((x - 11.5) / 8.5) ** 2 + ((y - 10.5) / 6.5) ** 2
            if val <= 1.05:
                if val > 0.8:
                    t = (x - 11.5) * -0.7 + (y - 10.5) * -0.7
                    col = BRASS[4] if t > 0 else BRASS[2]
                elif val > 0.65:
                    col = STEEL[1]
                else:
                    col = DARK_PANEL
                img[y, x] = hexc(col)

    # Đuôi bong bóng thoại chỉ xuống góc trái (x = 6..9, y = 16..20)
    tail_pts = [(8, 16), (7, 17), (6, 18), (5, 19), (6, 19), (7, 18), (8, 17)]
    for tx, ty in tail_pts:
        img[ty, tx] = hexc(BRASS[3] if tx < 7 else BRASS[1])

    # 3 chấm tròn vàng beacon nhấp nháy (...)
    for cx in (8, 12, 16):
        img[10, cx] = hexc(SPECULAR_GOLD)
        img[11, cx] = hexc(BEACON[3])

    return Image.fromarray(img, 'RGBA')

# ── 3. Icon_Menu_Quest (24x24, cuộn giấy nhiệm vụ) ───────────────────────────────
def make_icon_menu_quest():
    """Cuộn giấy hải đồ nhiệm vụ 24x24: Giấy da cuộn mép, dấu sáp đỏ gỉ niêm phong."""
    s = 24
    img = blank(s, s)

    # Tấm giấy da mở nghiêng (x = 4..19, y = 3..20)
    for y in range(3, 20):
        for x in range(5, 18):
            dx = x - 5
            dy = y - 3
            if y == 3 or x == 5:
                col = ROPE[4]
            elif y == 19 or x == 17:
                col = ROPE[1]
            elif (x + y) % 6 == 0:
                col = ROPE[2]
            else:
                col = ROPE[3]
            img[y, x] = hexc(col)

    # Cuộn mép tròn ở góc trên (x = 4..7, y = 2..5)
    for cx, cy in ((4, 3), (5, 2), (6, 2), (7, 3), (6, 4)):
        img[cy, cx] = hexc(ROPE[4] if cy == 2 else ROPE[2])

    # Dòng chữ tượng trưng trên văn tự quest
    for qy in (7, 10, 13):
        for qx in range(8, 15):
            if (qx + qy) % 2 == 0:
                img[qy, qx] = hexc(STEEL[1])

    # Dấu sáp đỏ gỉ ở góc dưới-phải (x = 14..18, y = 15..19)
    for dy in (-2, -1, 0, 1, 2):
        for dx in (-2, -1, 0, 1, 2):
            if abs(dx) + abs(dy) <= 2:
                img[17 + dy, 16 + dx] = hexc(RUST[3] if dy < 0 else RUST[1])
    img[17, 16] = hexc(SPECULAR_GOLD)  # Tâm ngọc sáp sáng

    return Image.fromarray(img, 'RGBA')

# ── 4. Icon_Menu_Floor (24x24, ngọn tháp nhiều tầng) ─────────────────────────────
def make_icon_menu_floor():
    """Biểu tượng tháp hải đăng leo tầng 24x24: 3 tầng mở rộng, đèn hải lưu đỉnh."""
    s = 24
    img = blank(s, s)
    cx = 11.5

    # Đèn ngọn tháp (y = 2..6)
    img[2, 11] = hexc(SPECULAR_GOLD); img[2, 12] = hexc(SPECULAR_GOLD)
    for x in range(10, 14):
        img[3, x] = hexc(BEACON[3])
        img[4, x] = hexc(BEACON[2])
    img[5, 10] = hexc(BRASS[4]); img[5, 13] = hexc(BRASS[2])

    # Tầng 1: Đỉnh nhọn (y = 6..10, half_w = 3..4)
    for y in range(6, 11):
        for x in range(int(cx - 3), int(cx + 4)):
            col = STEEL[4] if x < cx else STEEL[2]
            if abs(x - cx) >= 3 or y == 10: col = STEEL[1]
            img[y, x] = hexc(col)

    # Tầng 2: Thân giữa (y = 11..15, half_w = 5..6)
    for y in range(11, 16):
        for x in range(int(cx - 5), int(cx + 6)):
            col = STEEL[3] if x < cx else STEEL[1]
            if abs(x - cx) >= 5 or y == 15: col = STEEL[0]
            img[y, x] = hexc(col)
    # Cửa sổ vòm tầng 2
    img[13, 11] = hexc(DARK_INSET); img[13, 12] = hexc(DARK_INSET)

    # Tầng 3: Đế móng tháp (y = 16..21, half_w = 7..8)
    for y in range(16, 22):
        for x in range(int(cx - 7), int(cx + 8)):
            col = STEEL[3] if x < cx else STEEL[1]
            if abs(x - cx) >= 7 or y == 21: col = STEEL[0]
            img[y, x] = hexc(col)
    # Cổng vòm vào tháp ở chân đế
    for x in (11, 12):
        img[19, x] = hexc(DARK_INSET)
        img[20, x] = hexc(DARK_INSET)

    return Image.fromarray(img, 'RGBA')

# ── 5. Icon_Menu_Pause (24x24, hai vạch thép tạm dừng) ────────────────────────────
def make_icon_menu_pause():
    """Hai thanh thép tạm dừng 24x24: Vát cạnh nổi, đầu nẹp đồng."""
    s = 24
    img = blank(s, s)

    for bar_x in (6, 14):
        for y in range(4, 20):
            for dx in range(4):
                x = bar_x + dx
                if y == 4 or dx == 0:
                    c = GRAY[4]
                elif y == 19 or dx == 3:
                    c = GRAY[0]
                elif dx == 1:
                    c = GRAY[3]
                else:
                    c = GRAY[2]
                img[y, x] = hexc(c)
        # Chốt đồng ở 2 đầu thanh
        img[5, bar_x + 1] = hexc(BRASS[4]); img[5, bar_x + 2] = hexc(BRASS[3])
        img[18, bar_x + 1] = hexc(BRASS[2]); img[18, bar_x + 2] = hexc(BRASS[1])

    return Image.fromarray(img, 'RGBA')

# ── 6. Icon_Menu_Character (24x24, tượng bán thân nhân vật) ───────────────────────
def make_icon_menu_character():
    """Hình bán thân nhà thám hiểm 24x24: Mũ trùm cowl, giáp vai thép, kính sáng."""
    s = 24
    img = blank(s, s)
    cx = 11.5

    # Đầu & Mũ trùm cowl (y = 3..11, half_w = 4)
    for y in range(3, 12):
        for x in range(int(cx - 4), int(cx + 5)):
            dist = np.hypot(x - cx, y - 7.5)
            if dist <= 4.6:
                col = STEEL[4] if x < cx and y < 8 else STEEL[3] if x < cx else STEEL[1]
                img[y, x] = hexc(col)
    # Khe mắt kính sáng vàng beacon
    img[8, 10] = hexc(SPECULAR_GOLD); img[8, 11] = hexc(SPECULAR_GOLD)
    img[8, 12] = hexc(BEACON[3]);     img[8, 13] = hexc(BEACON[3])

    # Cổ & Khăn quàng đỏ gỉ (y = 12..14)
    for x in range(8, 16):
        img[12, x] = hexc(RUST[3] if x < 12 else RUST[2])
        img[13, x] = hexc(RUST[2] if x < 12 else RUST[1])

    # Giáp vai thép công nghiệp (y = 14..21, x = 3..20)
    for y in range(14, 21):
        w_half = int(4 + (y - 14) * 1.2)
        for x in range(int(cx - w_half), int(cx + w_half + 1)):
            dx = x - cx
            if abs(dx) == w_half or y == 20:
                col = STEEL[0]
            elif dx < -w_half + 2:
                col = STEEL[4]
            elif dx < 0:
                col = STEEL[3]
            else:
                col = STEEL[1]
            img[y, x] = hexc(col)

    # Đinh tán vai bằng đồng
    img[16, 5] = hexc(BRASS[4]); img[16, 18] = hexc(BRASS[2])

    return Image.fromarray(img, 'RGBA')

# ── 7. Icon_Menu_Settings (24x24, bánh răng lò hơi) ──────────────────────────────
def make_icon_menu_settings():
    """Bánh răng lò hơi 24x24: 8 răng đồng thau vát nổi, trục giữa rỗng."""
    s = 24
    img = blank(s, s)
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0

    # 1. Vành bánh răng (R = 4..9)
    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - cy)
            if dist <= 8.8:
                if dist > 7.5:
                    col = BRASS[1]
                elif dist > 3.8:
                    t = (x - cx) * -0.7 + (y - cy) * -0.7
                    col = BRASS[4] if t > 0.3 else BRASS[3] if t > -0.3 else BRASS[2]
                elif dist > 2.5:
                    col = STEEL[0]
                else:
                    col = DARK_INSET
                img[y, x] = hexc(col)

    # 2. 8 răng nhô ra (8 răng ở bước góc 45 độ, r = 8..11)
    for i in range(8):
        ang = i * (np.pi / 4)
        for r in (8, 9, 10):
            for d_ang in (-0.08, 0, 0.08):
                rx = int(cx + r * np.cos(ang + d_ang))
                ry = int(cy + r * np.sin(ang + d_ang))
                if 0 <= rx < s and 0 <= ry < s:
                    col = BRASS[4] if (np.cos(ang) - np.sin(ang) > 0) else BRASS[2]
                    img[ry, rx] = hexc(col)

    # Điểm sáng bóng trục tâm
    img[10, 10] = hexc(SPECULAR_GOLD)
    return Image.fromarray(img, 'RGBA')

# ── 8. Icon_Close (24x24, chữ X kim loại có đinh tán) ─────────────────────────────
def make_icon_close():
    """Nút đóng X 24x24: Hai thanh thép đỏ gỉ bắt chéo, chốt đinh tán đồng ở tâm."""
    s = 24
    img = blank(s, s)

    # 2 thanh chéo dày 3px từ (4,4) đến (19,19)
    for i in range(16):
        # Đường chéo 1: trên-trái xuống dưới-phải
        x1 = 4 + i
        y1 = 4 + i
        # Đường chéo 2: trên-phải xuống dưới-trái
        x2 = 19 - i
        y2 = 4 + i
        for w in (-1, 0, 1):
            if 0 <= y1 + w < s:
                c1 = STEEL[4] if w == -1 else RUST[3] if w == 0 else RUST[1]
                img[y1 + w, x1] = hexc(c1)
            if 0 <= y2 + w < s:
                c2 = STEEL[4] if w == -1 else RUST[3] if w == 0 else RUST[1]
                img[y2 + w, x2] = hexc(c2)

    # Chốt đinh tán đồng 3x3 ở chính giữa tâm giao nhau
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            img[11 + dy, 11 + dx] = hexc(BRASS[3] if dy > 0 or dx > 0 else BRASS[4])
    img[11, 11] = hexc(SPECULAR_GOLD)

    return Image.fromarray(img, 'RGBA')

# ── 9. Icon_Time_Dawn (24x24, bình minh) ──────────────────────────────────────────
def make_time_dawn():
    """Bình minh 24x24: Mặt trời mọc từ mặt biển, 3 tia sáng sớm vươn lên."""
    s = 24
    img = blank(s, s)
    cx = 11.5

    # Mặt biển ngang chân trời (y = 15..20)
    for y in range(15, 21):
        for x in range(3, 21):
            img[y, x] = hexc(WATER[2] if y == 15 else WATER[1])

    # Bán nguyệt mặt trời mọc (y = 8..14)
    for y in range(8, 15):
        w_half = int(np.sqrt(max(0, 36 - (y - 14) ** 2)))
        for x in range(int(cx - w_half), int(cx + w_half + 1)):
            img[y, x] = hexc(SPECULAR_GOLD if y < 11 and x < 12 else BEACON[3])

    # 3 tia nắng vươn lên (Tây Bắc, Bắc, Đông Bắc)
    for dx, dy in ((-6, -6), (0, -8), (6, -6)):
        img[int(14 + dy), int(cx + dx)] = hexc(BEACON[4])
        img[int(14 + dy + 1), int(cx + dx)] = hexc(BEACON[2])

    return Image.fromarray(img, 'RGBA')

# ── 10. Icon_Time_Morning (24x24, sáng sớm) ──────────────────────────────────────
def make_time_morning():
    """Buổi sáng 24x24: Mặt trời sáng rõ, 4 tia nắng chữ thập."""
    s = 24
    img = blank(s, s)
    cx, cy = 11.5, 11.5

    # Mặt trời tròn r = 5.5
    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - cy)
            if dist <= 5.5:
                col = SPECULAR_GOLD if (x < cx and y < cy) else BEACON[3] if dist < 4.2 else BEACON[2]
                img[y, x] = hexc(col)

    # 4 tia nắng chữ thập vươn dài (N, S, W, E)
    for dx, dy, l in ((0, -8, 3), (0, 8, 3), (-8, 0, 3), (8, 0, 3)):
        for step in range(l):
            px = int(cx + dx + (step if dy == 0 else 0))
            py = int(cy + dy + (step if dx == 0 else 0))
            if 0 <= px < s and 0 <= py < s:
                img[py, px] = hexc(BEACON[4] if step == 0 else BEACON[2])

    return Image.fromarray(img, 'RGBA')

# ── 11. Icon_Time_Noon (24x24, buổi trưa) ─────────────────────────────────────────
def make_time_noon():
    """Buổi trưa 24x24: Mặt trời đỉnh thiên, vầng hào quang 8 cánh rực rỡ."""
    s = 24
    img = blank(s, s)
    cx, cy = 11.5, 11.5

    # Vầng nhật hoa 8 tia nắng nhọn
    for i in range(8):
        ang = i * (np.pi / 4)
        for r in (6, 7, 8, 9):
            rx = int(cx + r * np.cos(ang))
            ry = int(cy + r * np.sin(ang))
            if 0 <= rx < s and 0 <= ry < s:
                img[ry, rx] = hexc(SPECULAR_GOLD if r == 9 else BEACON[3])

    # Mặt trời rực sáng ở giữa
    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - cy)
            if dist <= 5.0:
                img[y, x] = hexc(SPECULAR_GOLD if dist < 3.0 else BEACON[4])

    return Image.fromarray(img, 'RGBA')

# ── 12. Icon_Time_Afternoon (24x24, buổi chiều) ──────────────────────────────────
def make_time_afternoon():
    """Buổi chiều 24x24: Mặt trời xế bóng nghiêng về phía Tây, ánh ấm."""
    s = 24
    img = blank(s, s)
    cx, cy = 13.5, 10.5

    # Mặt trời nghiêng lệch phải
    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - cy)
            if dist <= 5.2:
                col = SPECULAR_GOLD if (x < cx and y < cy) else BEACON[2] if dist < 4.0 else BRASS[2]
                img[y, x] = hexc(col)

    # 3 tia nắng ấm hắt chéo xuống góc trái
    for i in range(3):
        for step in (6, 7, 8):
            rx = int(cx - step + i * 2)
            ry = int(cy + step - i)
            if 0 <= rx < s and 0 <= ry < s:
                img[ry, rx] = hexc(BEACON[2] if step == 6 else BRASS[2])

    return Image.fromarray(img, 'RGBA')

# ── 13. Icon_Time_Evening (24x24, hoàng hôn) ─────────────────────────────────────
def make_time_evening():
    """Hoàng hôn 24x24: Mặt trời lặn đỏ gỉ, dải mây chạng vạng phủ ngang."""
    s = 24
    img = blank(s, s)
    cx = 11.5

    # Mặt trời lặn (y = 8..16) tông đỏ gỉ và vàng beacon sẫm
    for y in range(8, 17):
        w_half = int(np.sqrt(max(0, 32 - (y - 12) ** 2)))
        for x in range(int(cx - w_half), int(cx + w_half + 1)):
            col = BEACON[3] if y < 11 else RUST[3] if y < 14 else RUST[1]
            img[y, x] = hexc(col)

    # 2 dải mây hoàng hôn ngang qua mặt trời
    for x in range(2, 22):
        if (x % 5 != 0):
            img[11, x] = hexc(WATER[2])
            img[15, x] = hexc(WATER[1])

    return Image.fromarray(img, 'RGBA')

# ── 14. Icon_Time_Night (24x24, ban đêm) ──────────────────────────────────────────
def make_time_night():
    """Ban đêm 24x24: Trăng lưỡi liềm vàng beacon và 2 vì tinh tú."""
    s = 24
    img = blank(s, s)
    cx, cy = 10.5, 11.5

    # Trăng lưỡi liềm (hình tròn trừ hình tròn lệch)
    for y in range(4, 20):
        for x in range(4, 18):
            dist1 = np.hypot(x - cx, y - cy)
            dist2 = np.hypot(x - (cx + 3.8), y - (cy - 1.5))
            if dist1 <= 6.8 and dist2 >= 5.2:
                col = SPECULAR_GOLD if (x < cx and y < cy) else BEACON[3] if dist1 < 5.8 else BEACON[2]
                img[y, x] = hexc(col)

    # 2 vì sao đêm lấp lánh ở góc phải
    img[6, 19] = hexc(SPECULAR_GOLD)
    img[5, 19] = hexc(BEACON[3]); img[7, 19] = hexc(BEACON[3])
    img[6, 18] = hexc(BEACON[3]); img[6, 20] = hexc(BEACON[3])

    img[16, 17] = hexc(SPECULAR_GOLD)
    img[16, 16] = hexc(BEACON[2]); img[16, 18] = hexc(BEACON[2])

    return Image.fromarray(img, 'RGBA')

# ── 15. Icon_Seal (48x48, huy hiệu niêm phong cho màn hình chết) ──────────────────
def make_icon_seal():
    """Huy hiệu niêm phong tử thần (Permadeath Seal) 48x48:
    Khóa đồng cổ + dây thừng chéo + dấu sáp niêm phong đỏ gỉ (KHÔNG đầu lâu, KHÔNG máu)."""
    s = 48
    img = blank(s, s)
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0

    # 1. Quai khóa sắt (Shackle) vòng cung phía trên (y = 6..24, x = 14..34)
    for y in range(6, 25):
        for x in range(14, 35):
            dist = np.hypot(x - cx, y - 18.0)
            if 6.5 <= dist <= 10.5 and y <= 19:
                col = STEEL[4] if x < cx else STEEL[2]
                if dist > 9.5 or dist < 7.5: col = STEEL[0]
                img[y, x] = hexc(col)

    # 2. Thân ổ khóa đồng cổ (y = 18..42, x = 10..38)
    for y in range(18, 42):
        for x in range(10, 38):
            d = min(x - 10, 37 - x, y - 18, 41 - y)
            if d == 0:
                col = BRASS[0]
            elif d == 1:
                col = BRASS[4] if (x < cx and y < 30) else BRASS[1]
            elif d == 2:
                col = BRASS[3]
            else:
                col = BRASS[2] if (x < cx) else BRASS[1]
            img[y, x] = hexc(col)

    # 3. Dây thừng niêm phong quấn chéo chữ X qua thân khóa
    for i in range(24):
        # Dây 1: chéo trái sang phải
        x1 = int(12 + i)
        y1 = int(18 + i * 0.9)
        # Dây 2: chéo phải sang trái
        x2 = int(35 - i)
        y2 = int(18 + i * 0.9)
        for w in (-1, 0, 1):
            if 0 <= y1 + w < s and 0 <= x1 < s:
                img[y1 + w, x1] = hexc(ROPE[3] if w == 0 else ROPE[1])
            if 0 <= y2 + w < s and 0 <= x2 < s:
                img[y2 + w, x2] = hexc(ROPE[3] if w == 0 else ROPE[1])

    # 4. Dấu sáp đỏ gỉ niêm phong lớn ở chính giữa (R = 8.5)
    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - (cy + 4.0))
            if dist <= 8.5:
                if dist > 7.5:
                    col = RUST[0]
                elif dist > 6.0:
                    t = (x - cx) * -0.7 + (y - (cy + 4.0)) * -0.7
                    col = RUST[4] if t > 0 else RUST[1]
                else:
                    col = RUST[2]
                img[y, x] = hexc(col)

    # Biểu tượng mỏ neo niêm phong dập chìm trên sáp
    img[int(cy + 2), int(cx)] = hexc(SPECULAR_GOLD)
    img[int(cy + 3), int(cx)] = hexc(RUST[4])
    img[int(cy + 4), int(cx)] = hexc(RUST[4])
    img[int(cy + 5), int(cx)] = hexc(RUST[4])
    img[int(cy + 5), int(cx) - 1] = hexc(RUST[4]); img[int(cy + 5), int(cx) + 1] = hexc(RUST[4])
    img[int(cy + 4), int(cx) - 2] = hexc(RUST[4]); img[int(cy + 4), int(cx) + 2] = hexc(RUST[4])

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
    assert (w, h) == (exp_w, exp_h), f"{name}: kích thước ({w},{h}) != ({exp_w},{exp_h})"
    arr = np.array(im)
    for y in range(h):
        for x in range(w):
            r, g, b, a = arr[y, x]
            if a == 0:
                continue
            hcode = f"#{r:02x}{g:02x}{b:02x}"
            assert hcode in ALLOWED_HEX, f"{name} pixel ({x},{y}) màu {hcode} nằm ngoài bảng màu!"
    print(f"  [OK] {name} ({w}x{h}, palette 100% hợp lệ)")

SPRITES_BATCH_D = [
    ('Icon_Menu_Bag',        make_icon_menu_bag(),        24, 24),
    ('Icon_Menu_Talk',       make_icon_menu_talk(),       24, 24),
    ('Icon_Menu_Quest',      make_icon_menu_quest(),      24, 24),
    ('Icon_Menu_Floor',      make_icon_menu_floor(),      24, 24),
    ('Icon_Menu_Pause',      make_icon_menu_pause(),      24, 24),
    ('Icon_Menu_Character',  make_icon_menu_character(),  24, 24),
    ('Icon_Menu_Settings',   make_icon_menu_settings(),   24, 24),
    ('Icon_Close',           make_icon_close(),           24, 24),
    ('Icon_Time_Dawn',       make_time_dawn(),            24, 24),
    ('Icon_Time_Morning',    make_time_morning(),         24, 24),
    ('Icon_Time_Noon',       make_time_noon(),            24, 24),
    ('Icon_Time_Afternoon',  make_time_afternoon(),       24, 24),
    ('Icon_Time_Evening',    make_time_evening(),         24, 24),
    ('Icon_Time_Night',      make_time_night(),           24, 24),
    ('Icon_Seal',            make_icon_seal(),            48, 48),
]

def generate_previews(sprites_dict):
    """Ghép menu HUD góc trên-phải, đồng hồ thời gian và huy hiệu Seal lên ảnh gameplay."""
    pve_bg_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', '_Game', 'screenshot_pve_new_look.png')
    bg_orig = Image.open(pve_bg_path).convert('RGBA')

    for cw, ch, suffix in ((1920, 1080, '1920'), (2400, 1080, '2400')):
        bg = bg_orig.resize((cw, ch), Image.Resampling.BILINEAR)

        # 1. Cột menu 2 hàng góc trên-phải (nút 120x120, icon 72x72)
        # Hàng 1 (y = 40): Pause (-40), Floor (-180), Quest (-320)
        menu_items_row1 = [
            ('Icon_Menu_Pause', cw - 140, 40),
            ('Icon_Menu_Floor', cw - 280, 40),
            ('Icon_Menu_Quest', cw - 420, 40)
        ]
        # Hàng 2 (y = 180): Bag (-40), Character (-180)
        menu_items_row2 = [
            ('Icon_Menu_Bag', cw - 140, 180),
            ('Icon_Menu_Character', cw - 280, 180)
        ]
        for name, x, y in menu_items_row1 + menu_items_row2:
            icon_art = sprites_dict[name].resize((72, 72), Image.Resampling.NEAREST)
            bg.paste(icon_art, (x, y), icon_art)

        # 2. Đồng hồ 6 pha Ngày/Đêm dàn hàng ở mép trên (y = 40)
        time_phases = ['Icon_Time_Dawn', 'Icon_Time_Morning', 'Icon_Time_Noon',
                       'Icon_Time_Afternoon', 'Icon_Time_Evening', 'Icon_Time_Night']
        for i, t_name in enumerate(time_phases):
            t_art = sprites_dict[t_name].resize((54, 54), Image.Resampling.NEAREST)
            bg.paste(t_art, (cw // 2 - 180 + i * 65, 45), t_art)

        # 3. Nút đóng Close "X" trên góc bảng thoại mẫu
        close_art = sprites_dict['Icon_Close'].resize((72, 72), Image.Resampling.NEAREST)
        bg.paste(close_art, (cw - 140, 320), close_art)

        # 4. Huy hiệu Niêm Phong Tử Thần Icon_Seal ở giữa màn hình (144x144 canvas)
        seal_art = sprites_dict['Icon_Seal'].resize((144, 144), Image.Resampling.NEAREST)
        bg.paste(seal_art, (cw // 2 - 72, ch // 2 - 72), seal_art)

        preview_path = os.path.join(OUT_DIR, f'preview_D_{suffix}.png')
        bg.save(preview_path)
        print(f"  [PREVIEW] Đã tạo ảnh xem thử: {preview_path} ({cw}x{ch})")

def main():
    print("=== TẠO BỘ GIAO DIỆN PIXEL ART LÔ D (BIỂU TƯỢNG MENU & ĐỒNG HỒ NGÀY/ĐÊM) ===")
    sprites_dict = {}
    for name, im, exp_w, exp_h in SPRITES_BATCH_D:
        validate_sprite(name, im, exp_w, exp_h)
        save_path = os.path.join(OUT_DIR, f"{name}.png")
        im.save(save_path)
        sprites_dict[name] = im

    print("\n--- Đang tạo ảnh xem thử Preview Lô D (1920x1080 & 2400x1080) ---")
    generate_previews(sprites_dict)
    print("\n>>> HOÀN THÀNH TẤT CẢ SPRITE LÔ D!")

if __name__ == '__main__':
    main()
