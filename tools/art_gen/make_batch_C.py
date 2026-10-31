"""make_batch_C.py - Bộ sprite UI pixel art mới cho Game2D: LÔ C (HUD Chiến Đấu).

Tuân thủ nghiêm ngặt:
- Art Contract (docs/ref/14_ART_CONTRACT.md, 14A, 15)
- Bảng màu: PALETTE (pixelkit.py) + STEEL, BRASS, GRAY (make_ui_pack.py)
- Quy tắc tỷ lệ: 1 art pixel = 3 canvas pixels (1080p canvas)
- Bộ nút chiến đấu:
  UI_Joystick_Base (64x64), UI_Joystick_Knob (24x24),
  UI_ActionRing_L (64x64, đòn đánh 190x190 canvas),
  UI_ActionRing_M (48x48, né/kỹ thuật 140x140 canvas),
  UI_ActionRing_S (40x40, bình máu/phụ 120x120 canvas),
  UI_Cooldown (48x48, lớp phủ tròn tối hồi chiêu),
  UI_LevelBadge (32x32, huy hiệu cấp độ),
  Icon_Combat_Attack (40x40), Icon_Combat_Dodge (32x32),
  Icon_Combat_Core (32x32), Icon_Combat_Traverse (32x32),
  Icon_Coin (16x16),
  Icon_Ultimate (48x48, vẽ lại pixel art thay bản mượt cũ),
  Icon_UltimateRing (48x48, vẽ lại pixel art).
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

# ── 1. UI_Joystick_Base (64x64, đế la bàn bán trong suốt) ─────────────────────────
def make_joystick_base():
    """Đế joystick 64x64 bán trong suốt, có vòng la bàn thép-đồng và 4 mũi tên chỉ hướng."""
    s = 64
    img = blank(s, s)
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0

    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - cy)
            if dist <= 31.0:
                if dist > 29.5:
                    col = STEEL[0]
                    alpha = 180
                elif dist > 27.0:
                    # Vành kim loại ngoài
                    t = (x - cx) * -0.7 + (y - cy) * -0.7
                    col = STEEL[3] if t > 0 else STEEL[1]
                    alpha = 180
                elif dist > 25.5:
                    col = STEEL[0]
                    alpha = 160
                elif dist > 18.0:
                    # Lòng la bàn bán trong suốt
                    col = WATER[0]
                    alpha = 75
                elif dist > 16.5:
                    # Vòng tròn định vị phụ bên trong
                    col = WATER[2]
                    alpha = 90
                elif dist > 8.0:
                    col = WATER[0]
                    alpha = 75
                elif dist > 6.5:
                    col = WATER[1]
                    alpha = 90
                else:
                    col = WATER[0]
                    alpha = 60
                img[y, x] = hexc(col, alpha)

    # 4 mũi tên phương hướng N, S, W, E viền đồng
    for dx, dy, ax, ay in ((0, -28, 0, 1), (0, 28, 0, -1), (-28, 0, 1, 0), (28, 0, -1, 0)):
        px, py = int(cx + dx), int(cy + dy)
        img[py, px] = hexc(SPECULAR_GOLD, 255)
        img[py + ay, px + ax] = hexc(BEACON[2], 255)

    return Image.fromarray(img, 'RGBA')

# ── 2. UI_Joystick_Knob (24x24, núm joystick đồng đồng bộ) ────────────────────────
def make_joystick_knob():
    """Núm joystick 24x24 bằng đồng có khấc và điểm sáng hải đăng."""
    s = 24
    img = blank(s, s)
    c = (s - 1) / 2.0
    for y in range(s):
        for x in range(s):
            dx = x - c
            dy = y - c
            dist = np.hypot(dx, dy)
            if dist <= 11.2:
                if dist > 10.0:
                    col = STEEL[0]
                elif dist > 8.5:
                    t = (dx * -0.7 + dy * -0.7) / dist
                    col = BRASS[4] if t > 0.4 else BRASS[3] if t > 0.0 else BRASS[2] if t > -0.4 else BRASS[1]
                elif dist > 7.2:
                    col = STEEL[1]
                else:
                    t = (dx * -0.6 + dy * -0.8) / 7.2
                    col = BRASS[4] if t > 0.5 else BRASS[3] if t > 0.1 else BRASS[2] if t > -0.4 else BRASS[1]
                img[y, x] = hexc(col, 255)

    img[2, 11] = hexc(BRASS[4]); img[2, 12] = hexc(BRASS[4])
    img[21, 11] = hexc(BRASS[1]); img[21, 12] = hexc(BRASS[1])
    img[11, 2] = hexc(BRASS[4]); img[12, 2] = hexc(BRASS[4])
    img[11, 21] = hexc(BRASS[1]); img[12, 21] = hexc(BRASS[1])

    img[7, 8] = hexc(SPECULAR_GOLD)
    img[7, 9] = hexc(BRASS[4])
    img[8, 8] = hexc(BRASS[4])
    return Image.fromarray(img, 'RGBA')

# ── 3. UI_ActionRing_L (64x64, vòng nút Đánh lớn) ─────────────────────────────────
def make_action_ring_l():
    """Vòng nút Đánh thường 64x64 (chừa khoảng trống 40x40 ở giữa cho icon).
    Viền thép đúc dày, 4 ngàm nẹp đồng ở 4 góc chéo."""
    s = 64
    img = blank(s, s)
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0

    for y in range(s):
        for x in range(s):
            dx = x - cx
            dy = y - cx
            dist = np.hypot(dx, dy)
            if 19.5 <= dist <= 31.0:
                if dist > 29.5:
                    col = STEEL[0]
                elif dist > 27.5:
                    t = (dx * -0.7 + dy * -0.7) / dist
                    col = STEEL[4] if t > 0.3 else STEEL[3] if t > -0.2 else STEEL[1]
                elif dist > 22.0:
                    col = STEEL[2]
                elif dist > 20.5:
                    col = STEEL[1]
                else:
                    col = STEEL[0]
                img[y, x] = hexc(col, 255)

    # 4 ngàm nẹp đồng ở góc 45, 135, 225, 315 độ
    for ang in (np.pi / 4, 3 * np.pi / 4, 5 * np.pi / 4, 7 * np.pi / 4):
        for r in (24, 25, 26, 27):
            rx = int(cx + r * np.cos(ang))
            ry = int(cy + r * np.sin(ang))
            img[ry, rx] = hexc(BRASS[4] if ang < np.pi else BRASS[2], 255)
            # Chốt đinh tán đồng
            if r == 25:
                img[ry, rx] = hexc(SPECULAR_GOLD if ang < np.pi else BRASS[3], 255)

    return Image.fromarray(img, 'RGBA')

# ── 4. UI_ActionRing_M (48x48, vòng nút Né / Kỹ năng) ─────────────────────────────
def make_action_ring_m():
    """Vòng nút Kỹ năng / Né 48x48 (khoảng trống 32x32 bên trong cho icon)."""
    s = 48
    img = blank(s, s)
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0

    for y in range(s):
        for x in range(s):
            dx = x - cx
            dy = y - cy
            dist = np.hypot(dx, dy)
            if 14.5 <= dist <= 23.0:
                if dist > 21.8:
                    col = STEEL[0]
                elif dist > 20.0:
                    t = (dx * -0.7 + dy * -0.7) / dist
                    col = STEEL[4] if t > 0.25 else STEEL[3] if t > -0.25 else STEEL[1]
                elif dist > 16.5:
                    col = STEEL[2]
                elif dist > 15.2:
                    col = STEEL[1]
                else:
                    col = STEEL[0]
                img[y, x] = hexc(col, 255)

    # 4 đinh tán đồng ở N, S, W, E
    for dx, dy in ((0, -18), (0, 18), (-18, 0), (18, 0)):
        px, py = int(cx + dx), int(cy + dy)
        img[py, px] = hexc(BRASS[4] if dy < 0 or dx < 0 else BRASS[2], 255)

    return Image.fromarray(img, 'RGBA')

# ── 5. UI_ActionRing_S (40x40, vòng nút Bình máu / Phụ) ───────────────────────────
def make_action_ring_s():
    """Vòng nút phụ 40x40 viền mảnh (khoảng trống 32x32 vừa icon)."""
    s = 40
    img = blank(s, s)
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0

    for y in range(s):
        for x in range(s):
            dx = x - cx
            dy = y - cy
            dist = np.hypot(dx, dy)
            if 14.5 <= dist <= 19.5:
                if dist > 18.5:
                    col = STEEL[0]
                elif dist > 16.5:
                    t = (dx * -0.7 + dy * -0.7) / dist
                    col = BRASS[4] if t > 0.2 else BRASS[3] if t > -0.2 else BRASS[1]
                else:
                    col = STEEL[1]
                img[y, x] = hexc(col, 255)

    return Image.fromarray(img, 'RGBA')

# ── 6. UI_Cooldown (48x48, lớp phủ hồi chiêu tròn) ────────────────────────────────
def make_cooldown():
    """Lớp phủ hồi chiêu 48x48 tròn tối (alpha 180) cho Unity Filled Radial 360."""
    s = 48
    img = blank(s, s)
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0

    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - cy)
            if dist <= 23.0:
                # Đậm ở giữa, viền tối
                a = 210 if dist > 21.0 else 180
                img[y, x] = hexc(DARK_INSET, a)

    return Image.fromarray(img, 'RGBA')

# ── 7. UI_LevelBadge (32x32, huy hiệu cấp độ) ─────────────────────────────────────
def make_level_badge():
    """Huy hiệu cấp độ 32x32: Khiên đồng vòm neo hải cảng, lòng xanh than tối để ghi số cấp."""
    s = 32
    img = blank(s, s)
    cx = 15.5

    for y in range(3, 29):
        # Dạng khiên nhọn đáy
        if y < 18:
            w_half = 12
        else:
            w_half = max(1, 12 - int((y - 17) * 1.1))
        for x in range(int(cx - w_half), int(cx + w_half + 1)):
            dx = abs(x - cx)
            if dx == w_half or y == 3 or y == 28:
                col = BRASS[1]
            elif dx == w_half - 1 or y == 4:
                col = BRASS[4] if (x < cx or y < 10) else BRASS[2]
            elif dx == w_half - 2 or y == 5:
                col = BRASS[3]
            else:
                col = DARK_PANEL
            img[y, x] = hexc(col, 255)

    # Đỉnh khiên có vương miện 3 ngọn đồng & điểm sáng vàng beacon
    img[2, int(cx)] = hexc(SPECULAR_GOLD)
    img[3, int(cx) - 1] = hexc(BRASS[4])
    img[3, int(cx) + 1] = hexc(BRASS[4])
    img[2, 7] = hexc(BRASS[4]); img[2, 24] = hexc(BRASS[2])

    return Image.fromarray(img, 'RGBA')

# ── 8. Icon_Combat_Attack (40x40, thanh kiếm chém) ────────────────────────────────
def make_icon_attack():
    """Icon đòn đánh 40x40: Thanh đao bến cảng chém chéo, vệt sáng kim loại vút qua."""
    s = 40
    img = blank(s, s)

    # 1. Vệt gió chém kiếm khí hình vòng cung (xanh bọt biển)
    for t in np.linspace(0.1, 0.9, 50):
        arc_x = int(6 + 28 * t)
        arc_y = int(32 - 26 * np.sin(t * np.pi * 0.85))
        for dx, dy in ((0, 0), (0, 1), (-1, 0)):
            if 0 <= arc_x + dx < s and 0 <= arc_y + dy < s:
                img[arc_y + dy, arc_x + dx] = hexc(FOAM[3] if t < 0.6 else FOAM[2], 240)

    # 2. Lưỡi đao thép chéo từ (7, 33) tới (32, 8)
    for i in range(28):
        x = 7 + i
        y = 33 - i
        # Lưỡi kiếm rộng 3-4 px
        img[y, x] = hexc(GRAY[4])      # Vệt sáng lưỡi kiếm
        img[y + 1, x] = hexc(GRAY[3])
        img[y, x - 1] = hexc(GRAY[2])
        img[y + 1, x - 1] = hexc(GRAY[1])
        img[y + 2, x] = hexc(GRAY[0])  # Sống đao viền tối

    # Mũi đao sắc nhọn
    img[6, 33] = hexc(GRAY[4])
    img[7, 34] = hexc(GRAY[4])
    img[6, 34] = hexc(SPECULAR_GOLD)

    # Chuôi kiếm và đốc kiếm đồng ở góc dưới-trái (x = 5..10, y = 30..35)
    for dx, dy in ((0, 0), (1, 1), (2, 2)):
        img[32 + dy, 8 - dx] = hexc(BRASS[4])
        img[33 + dy, 8 - dx] = hexc(ROPE[2])
        img[34 + dy, 7 - dx] = hexc(ROPE[1])
    img[36, 5] = hexc(BRASS[1])  # Quả cầu chuôi

    return Image.fromarray(img, 'RGBA')

# ── 9. Icon_Combat_Dodge (32x32, bước lướt né) ────────────────────────────────────
def make_icon_dodge():
    """Icon né 32x32: Cánh gió rẽ sóng cuộn lướt nhanh, dư ảnh bước chân bến cảng."""
    s = 32
    img = blank(s, s)

    # 3 luồng gió lướt song song cuộn vút
    for streak_idx, (y_off, x_len) in enumerate(((7, 18), (14, 24), (21, 16))):
        for i in range(x_len):
            x = 4 + i
            y = int(y_off + 3 * np.sin(i / 3.5))
            if 0 <= y < s and 0 <= x < s:
                c = FOAM[3] if i > x_len - 6 else FOAM[2] if i > 6 else WATER[3]
                img[y, x] = hexc(c, 255)
                if y + 1 < s:
                    img[y + 1, x] = hexc(WATER[2], 220)

    # Điểm sao lướt tốc biến ở đuôi gió
    img[12, 26] = hexc(SPECULAR_GOLD)
    img[13, 27] = hexc(FOAM[4])
    img[14, 28] = hexc(SPECULAR_GOLD)

    return Image.fromarray(img, 'RGBA')

# ── 10. Icon_Combat_Core (32x32, kỹ thuật Core - Slot 1) ──────────────────────────
def make_icon_core():
    """Icon kỹ thuật Core: Lưỡi mỏ neo xuyên phá bổ thẳng, năng lượng vàng beacon bùng nổ."""
    s = 32
    img = blank(s, s)
    cx = 15.5

    # Tia sáng bùng nổ xung quanh
    for ang in (0, np.pi/4, np.pi/2, 3*np.pi/4, np.pi, 5*np.pi/4, 7*np.pi/4):
        for r in (9, 10, 11):
            rx = int(cx + r * np.cos(ang))
            ry = int(15.5 + r * np.sin(ang))
            if 0 <= rx < s and 0 <= ry < s:
                img[ry, rx] = hexc(BEACON[2], 200)

    # Lưỡi mỏ neo bổ thẳng đứng (y = 5..26)
    for y in range(5, 27):
        prog = (y - 5) / 21.0
        # Mũi nhọn ở đáy y=26, rộng dần lên
        w_half = int(1 + 4 * (1 - abs(prog - 0.4)))
        for x in range(int(cx - w_half), int(cx + w_half + 1)):
            dx = x - cx
            if abs(dx) == w_half:
                c = STEEL[0]
            elif dx < 0:
                c = STEEL[4] if y < 14 else STEEL[3]
            else:
                c = STEEL[1]
            img[y, x] = hexc(c, 255)

    # Tâm ngọc năng lượng vàng rực ở giữa
    img[13, int(cx)] = hexc(SPECULAR_GOLD)
    img[14, int(cx)] = hexc(SPECULAR_GOLD)
    img[13, int(cx) - 1] = hexc(BEACON[3])
    img[13, int(cx) + 1] = hexc(BEACON[3])

    return Image.fromarray(img, 'RGBA')

# ── 11. Icon_Combat_Traverse (32x32, kỹ thuật Traverse - Slot 2) ───────────────────
def make_icon_traverse():
    """Icon kỹ thuật Traverse: Móc neo phóng vút vượt sóng triều, dây kéo căng."""
    s = 32
    img = blank(s, s)

    # Dây thừng kéo chéo căng từ góc trái-dưới
    for i in range(16):
        x = 4 + i
        y = 26 - i
        img[y, x] = hexc(ROPE[3] if i % 2 == 0 else ROPE[2], 255)
        img[y + 1, x] = hexc(ROPE[1], 255)

    # Đầu móc mỏ neo ở góc trên-phải (x = 18..28, y = 4..14)
    anchor_pts = [
        (20, 10), (21, 9), (22, 8), (23, 7), (24, 6), (25, 5), (26, 6),
        (26, 8), (25, 9), (24, 10), (23, 11), (22, 12), (21, 13)
    ]
    for px, py in anchor_pts:
        img[py, px] = hexc(STEEL[4], 255)
        img[py + 1, px] = hexc(STEEL[1], 255)

    # Cánh sóng cuộn nâng đỡ bên dưới
    for i in range(12):
        img[22 + int(np.sin(i / 2.0) * 3), 10 + i] = hexc(FOAM[3], 240)

    # Điểm lóe sáng đầu móc
    img[5, 27] = hexc(SPECULAR_GOLD)
    return Image.fromarray(img, 'RGBA')

# ── 12. Icon_Coin (16x16, đồng tiền Col) ──────────────────────────────────────────
def make_icon_coin():
    """Icon đồng tiền Col 16x16: Đồng vàng bến cảng có khắc hình mỏ neo/sóng nổi."""
    s = 16
    img = blank(s, s)
    c = (s - 1) / 2.0

    for y in range(s):
        for x in range(s):
            dx = x - c
            dy = y - c
            dist = np.hypot(dx, dy)
            if dist <= 7.2:
                if dist > 6.4:
                    col = BRASS[1]
                elif dist > 5.0:
                    t = (dx * -0.7 + dy * -0.7) / dist
                    col = BRASS[4] if t > 0.3 else BRASS[3] if t > -0.3 else BRASS[2]
                else:
                    col = BEACON[2] if (dx * -0.5 + dy * -0.5 > 0) else BEACON[1]
                img[y, x] = hexc(col, 255)

    # Biểu tượng mỏ neo dập nổi ở tâm
    img[5, 7] = hexc(SPECULAR_GOLD); img[5, 8] = hexc(SPECULAR_GOLD)
    img[6, 7] = hexc(BRASS[4]);      img[7, 7] = hexc(BRASS[4])
    img[8, 7] = hexc(BRASS[4]);      img[9, 7] = hexc(BRASS[4])
    img[9, 6] = hexc(BRASS[4]);      img[9, 8] = hexc(BRASS[2])
    img[8, 5] = hexc(BRASS[4]);      img[8, 9] = hexc(BRASS[1])

    return Image.fromarray(img, 'RGBA')

# ── 13. Icon_Ultimate (48x48, vẽ lại pixel art) ───────────────────────────────────
def make_icon_ultimate():
    """Icon Tuyệt Kỹ 48x48 pixel art: Song đao bão tố rực lửa đỏ gỉ và vàng beacon."""
    s = 48
    img = blank(s, s)
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0

    # Vòng hào quang năng lượng cuồng nộ
    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - cy)
            if 15.0 <= dist <= 21.0:
                ang = np.arctan2(y - cy, x - cx)
                spark = np.sin(ang * 6 + dist * 0.5)
                if spark > 0.3:
                    img[y, x] = hexc(RUST[3] if spark < 0.7 else BEACON[2], 220)

    # Hai lưỡi đao liềm trăng khuyết bắt chéo hình chữ X
    for i in range(24):
        # Đao 1: Chéo từ góc trên-trái xuống dưới-phải
        x1 = int(12 + i)
        y1 = int(12 + i)
        # Đao 2: Chéo từ góc trên-phải xuống dưới-trái
        x2 = int(35 - i)
        y2 = int(12 + i)
        for w in (-1, 0, 1):
            if 0 <= y1 < s and 0 <= x1 + w < s:
                img[y1, x1 + w] = hexc(STEEL[4] if w == -1 else RUST[3] if w == 0 else RUST[1], 255)
            if 0 <= y2 < s and 0 <= x2 + w < s:
                img[y2, x2 + w] = hexc(STEEL[4] if w == 1 else RUST[3] if w == 0 else RUST[1], 255)

    # Tâm ngọc nộ khí vàng rực
    for dy in (-2, -1, 0, 1, 2):
        for dx in (-2, -1, 0, 1, 2):
            if abs(dx) + abs(dy) <= 2:
                img[int(cy + dy), int(cx + dx)] = hexc(BEACON[3], 255)
    img[int(cy), int(cx)] = hexc(SPECULAR_GOLD, 255)
    img[int(cy) - 1, int(cx)] = hexc(SPECULAR_GOLD, 255)

    return Image.fromarray(img, 'RGBA')

# ── 14. Icon_UltimateRing (48x48, vẽ lại pixel art) ───────────────────────────────
def make_icon_ultimate_ring():
    """Vòng năng lượng Tuyệt Kỹ 48x48 pixel art: Viền đồng cổ có 8 mốc khấc năng lượng."""
    s = 48
    img = blank(s, s)
    cx, cy = (s - 1) / 2.0, (s - 1) / 2.0

    for y in range(s):
        for x in range(s):
            dist = np.hypot(x - cx, y - cy)
            if 19.5 <= dist <= 23.0:
                if dist > 22.0 or dist < 20.5:
                    col = BRASS[1]
                else:
                    t = (x - cx) * -0.7 + (y - cy) * -0.7
                    col = BRASS[4] if t > 0 else BRASS[2]
                img[y, x] = hexc(col, 255)

    # 8 mốc khấc năng lượng vàng beacon xung quanh
    for i in range(8):
        ang = i * (np.pi / 4)
        rx = int(cx + 21.0 * np.cos(ang))
        ry = int(cy + 21.0 * np.sin(ang))
        img[ry, rx] = hexc(SPECULAR_GOLD, 255)
        # Khấc dày thêm 1 px
        if 0 <= ry + 1 < s:
            img[ry + 1, rx] = hexc(BEACON[3], 255)

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

SPRITES_BATCH_C = [
    ('UI_Joystick_Base',     make_joystick_base(),     64, 64),
    ('UI_Joystick_Knob',     make_joystick_knob(),     24, 24),
    ('UI_ActionRing_L',      make_action_ring_l(),      64, 64),
    ('UI_ActionRing_M',      make_action_ring_m(),      48, 48),
    ('UI_ActionRing_S',      make_action_ring_s(),      40, 40),
    ('UI_Cooldown',          make_cooldown(),          48, 48),
    ('UI_LevelBadge',        make_level_badge(),        32, 32),
    ('Icon_Combat_Attack',   make_icon_attack(),       40, 40),
    ('Icon_Combat_Dodge',    make_icon_dodge(),        32, 32),
    ('Icon_Combat_Core',     make_icon_core(),         32, 32),
    ('Icon_Combat_Traverse', make_icon_traverse(),     32, 32),
    ('Icon_Coin',            make_icon_coin(),         16, 16),
    ('Icon_Ultimate',        make_icon_ultimate(),     48, 48),
    ('Icon_UltimateRing',    make_icon_ultimate_ring(),48, 48),
]

def generate_previews(sprites_dict):
    """Ghép cụm nút chiến đấu HUD lên screenshot gameplay thật ở 1920x1080 và 2400x1080."""
    pve_bg_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', '_Game', 'screenshot_pve_new_look.png')
    bg_orig = Image.open(pve_bg_path).convert('RGBA')

    for cw, ch, suffix in ((1920, 1080, '1920'), (2400, 1080, '2400')):
        bg = bg_orig.resize((cw, ch), Image.Resampling.BILINEAR)

        # 1. Cần điều khiển Joystick góc dưới-trái
        # Đế 64x64 art px * 3 = 192x192 canvas px
        base_art = sprites_dict['UI_Joystick_Base'].resize((192, 192), Image.Resampling.NEAREST)
        knob_art = sprites_dict['UI_Joystick_Knob'].resize((72, 72), Image.Resampling.NEAREST)
        jx, jy = 100, ch - 260
        bg.paste(base_art, (jx, jy), base_art)
        bg.paste(knob_art, (jx + 60, jy + 60), knob_art)

        # 2. Cụm chiến đấu góc dưới-phải (bố trí chuẩn công thái học)
        # Nút Attack (192x192 canvas = Ring 64x64 * 3, Icon 40x40 * 3 = 120x120)
        ring_l = sprites_dict['UI_ActionRing_L'].resize((192, 192), Image.Resampling.NEAREST)
        icon_atk = sprites_dict['Icon_Combat_Attack'].resize((120, 120), Image.Resampling.NEAREST)
        ax, ay = cw - 280, ch - 280
        bg.paste(ring_l, (ax, ay), ring_l)
        bg.paste(icon_atk, (ax + 36, ay + 36), icon_atk)

        # Nút Dodge (144x144 canvas = Ring M 48x48 * 3, Icon 32x32 * 3 = 96x96)
        ring_m = sprites_dict['UI_ActionRing_M'].resize((144, 144), Image.Resampling.NEAREST)
        icon_ddg = sprites_dict['Icon_Combat_Dodge'].resize((96, 96), Image.Resampling.NEAREST)
        dx, dy = cw - 460, ch - 220
        bg.paste(ring_m, (dx, dy), ring_m)
        bg.paste(icon_ddg, (dx + 24, dy + 24), icon_ddg)

        # Nút Technique Core (Slot 1)
        icon_core = sprites_dict['Icon_Combat_Core'].resize((96, 96), Image.Resampling.NEAREST)
        tx, ty = cw - 290, ch - 460
        bg.paste(ring_m, (tx, ty), ring_m)
        bg.paste(icon_core, (tx + 24, ty + 24), icon_core)

        # Nút Technique Traverse (Slot 2)
        icon_trv = sprites_dict['Icon_Combat_Traverse'].resize((96, 96), Image.Resampling.NEAREST)
        rx, ry = cw - 460, ch - 420
        bg.paste(ring_m, (rx, ry), ring_m)
        bg.paste(icon_trv, (rx + 24, ry + 24), icon_trv)

        # Nút Tuyệt Kỹ Ultimate (144x144 canvas)
        icon_ult = sprites_dict['Icon_Ultimate'].resize((144, 144), Image.Resampling.NEAREST)
        ring_ult = sprites_dict['Icon_UltimateRing'].resize((144, 144), Image.Resampling.NEAREST)
        ux, uy = cw - 620, ch - 430
        bg.paste(ring_m, (ux, uy), ring_m)
        bg.paste(icon_ult, (ux, uy), icon_ult)
        bg.paste(ring_ult, (ux, uy), ring_ult)

        # Nút Bình Máu (120x120 canvas = Ring S 40x40 * 3)
        ring_s = sprites_dict['UI_ActionRing_S'].resize((120, 120), Image.Resampling.NEAREST)
        px, py = cw - 640, ch - 210
        bg.paste(ring_s, (px, py), ring_s)
        # Cooldown mẫu phủ lên nửa nút bình
        cd_art = sprites_dict['UI_Cooldown'].resize((120, 120), Image.Resampling.NEAREST)
        bg.paste(cd_art, (px, py), cd_art)

        # 3. Huy hiệu Level Badge góc trên-trái (96x96 canvas)
        badge_art = sprites_dict['UI_LevelBadge'].resize((96, 96), Image.Resampling.NEAREST)
        bg.paste(badge_art, (40, 40), badge_art)

        # 4. Icon Đồng Tiền Col (48x48 canvas)
        coin_art = sprites_dict['Icon_Coin'].resize((48, 48), Image.Resampling.NEAREST)
        bg.paste(coin_art, (50, 150), coin_art)

        preview_path = os.path.join(OUT_DIR, f'preview_C_{suffix}.png')
        bg.save(preview_path)
        print(f"  [PREVIEW] Đã tạo ảnh xem thử: {preview_path} ({cw}x{ch})")

def main():
    print("=== TẠO BỘ GIAO DIỆN PIXEL ART LÔ C (HUD CHIẾN ĐẤU) ===")
    sprites_dict = {}
    for name, im, exp_w, exp_h in SPRITES_BATCH_C:
        validate_sprite(name, im, exp_w, exp_h)
        save_path = os.path.join(OUT_DIR, f"{name}.png")
        im.save(save_path)
        sprites_dict[name] = im

    print("\n--- Đang tạo ảnh xem thử Preview HUD Chiến Đấu (1920x1080 & 2400x1080) ---")
    generate_previews(sprites_dict)
    print("\n>>> HOÀN THÀNH TẤT CẢ SPRITE LÔ C!")

if __name__ == '__main__':
    main()
