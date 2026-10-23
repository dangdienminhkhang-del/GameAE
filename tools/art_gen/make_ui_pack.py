"""Lô C: bộ sprite UI pixel art (khung thép + đồng, hợp tông Lò Rèn / Hải Cảng).
Chạy: python make_ui_pack.py -> out_batchC/UI_*.png
Nguyên tắc: nút và thanh dùng màu xám trung tính để Image.color của game nhuộm lại (giữ ý nghĩa màu: đỏ máu, xanh lá nhận, ...).
Khung (Panel, BarFrame) có màu riêng, dùng 9-slice. Cần Pillow, numpy.
"""
import os
import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out_batchC')
os.makedirs(OUT, exist_ok=True)


def hexc(h, a=255):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (a,)


STEEL = ['#0b1114', '#17262a', '#25393d', '#375257', '#5d8187']
BRASS = ['#2e2008', '#5c4210', '#94701f', '#c7a03a', '#efd47a']
GRAY = ['#262a31', '#555b66', '#8c94a1', '#c2c9d4', '#f4f7fb']


def blank(w, h):
    return np.zeros((h, w, 4), dtype=np.uint8)


def cut_corners(img, r=2):
    h, w = img.shape[:2]
    # bỏ đúng pixel góc để cạnh nhìn bo nhẹ
    for (x, y) in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        img[y, x] = (0, 0, 0, 0)
    return img


def panel():
    """48x48, 9-slice border 8: khung thép nhiều lớp + đinh tán đồng ở góc."""
    s = 48
    img = blank(s, s)
    for y in range(s):
        for x in range(s):
            d = min(x, y, s - 1 - x, s - 1 - y)          # khoảng cách tới mép
            if d == 0:
                c = STEEL[0]
            elif d == 1:
                c = STEEL[4] if (y <= x and y < s - 1 - x) or (x <= y and x < s - 1 - y) and (x + y) < s else STEEL[3]
            elif d == 2:
                c = STEEL[3]
            elif d == 3:
                c = STEEL[2]
            elif d == 4:
                c = STEEL[0]
            elif d == 5:
                c = STEEL[1]
            else:
                c = STEEL[1]
            img[y, x] = hexc(c)
    # bóng tối ở mép dưới/phải khung ngoài
    for i in range(1, s - 1):
        img[s - 2, i] = hexc(STEEL[1]); img[i, s - 2] = hexc(STEEL[1])
    for (cx, cy) in ((3, 3), (s - 5, 3), (3, s - 5), (s - 5, s - 5)):       # đinh tán đồng 2x2
        img[cy, cx] = hexc(BRASS[4]); img[cy, cx + 1] = hexc(BRASS[3])
        img[cy + 1, cx] = hexc(BRASS[3]); img[cy + 1, cx + 1] = hexc(BRASS[1])
    cut_corners(img)
    return Image.fromarray(img, 'RGBA')


def button():
    """24x24, 9-slice border 6, màu xám trung tính để game nhuộm; vát cạnh nổi."""
    s = 24
    img = blank(s, s)
    for y in range(s):
        for x in range(s):
            d = min(x, y, s - 1 - x, s - 1 - y)
            if d == 0:
                c = GRAY[0]
            elif d == 1:
                c = GRAY[4] if (y == 1 or x == 1) and not (y == s - 2 or x == s - 2) else GRAY[1]
            elif d == 2:
                c = GRAY[3] if (y == 2 or x == 2) and not (y == s - 3 or x == s - 3) else GRAY[1]
            else:
                c = GRAY[2]
            if y >= s - 3 and d >= 1:
                c = GRAY[1] if y >= s - 2 else GRAY[2]
            if x >= s - 3 and d >= 1 and y < s - 3:
                c = GRAY[1] if x >= s - 2 else GRAY[2]
            img[y, x] = hexc(c)
    cut_corners(img)
    return Image.fromarray(img, 'RGBA')


def bar_frame():
    """16x16, 9-slice border 4: khung thanh trũng. Tâm đen đặc; dùng fillCenter=false để làm viền phủ lên thanh."""
    s = 16
    img = blank(s, s)
    for y in range(s):
        for x in range(s):
            d = min(x, y, s - 1 - x, s - 1 - y)
            if d == 0:
                c = STEEL[0]
            elif d == 1:
                c = STEEL[4] if (y == s - 2 or x == s - 2) else STEEL[3]
            elif d == 2:
                c = STEEL[1]
            elif d == 3:
                c = STEEL[0]
            else:
                c = '#070b0e'
            img[y, x] = hexc(c)
    for (cx, cy) in ((1, 1), (s - 2, 1), (1, s - 2), (s - 2, s - 2)):
        img[cy, cx] = hexc(BRASS[3])
    cut_corners(img, 1)
    return Image.fromarray(img, 'RGBA')


def bar_fill():
    """8x16, xám sáng; vệt sáng ở trên, tối dần xuống dưới. Game nhuộm màu (đỏ máu, xanh exp...)."""
    w, h = 8, 16
    img = blank(w, h)
    rows = [3, 4, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 1, 1, 1, 0]
    for y in range(h):
        for x in range(w):
            img[y, x] = hexc(GRAY[rows[y]])
    return Image.fromarray(img, 'RGBA')


def knob():
    """16x16 núm trượt tròn bằng đồng."""
    s = 16
    img = blank(s, s)
    c = (s - 1) / 2.0
    for y in range(s):
        for x in range(s):
            dd = ((x - c) ** 2 + (y - c) ** 2) ** 0.5
            if dd <= 7.5:
                if dd > 6.4:
                    col = BRASS[0]
                else:
                    t = ((x - c) * -0.6 + (y - c) * -0.8) / 6.4
                    col = BRASS[4] if t > 0.55 else BRASS[3] if t > 0.05 else BRASS[2] if t > -0.5 else BRASS[1]
                img[y, x] = hexc(col)
    img[6, 6] = hexc('#fff2b0'); img[5, 6] = hexc(BRASS[4])
    return Image.fromarray(img, 'RGBA')


def check():
    """12x12 dấu tích xanh sáng, viền tối."""
    s = 12
    img = blank(s, s)
    pts = [(2, 6), (3, 7), (4, 8), (5, 7), (6, 6), (7, 5), (8, 4), (9, 3)]
    for (x, y) in pts:
        for dx, dy in ((0, 0), (0, 1)):
            img[y + dy, x + dx] = hexc('#9be564')
    for (x, y) in pts:
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 2), (-1, 1), (1, 1)):
            xx, yy = x + dx, y + dy
            if img[yy, xx][3] == 0:
                img[yy, xx] = hexc('#0b1114')
    return Image.fromarray(img, 'RGBA')


if __name__ == '__main__':
    for name, im in (('UI_Panel', panel()), ('UI_Button', button()), ('UI_BarFrame', bar_frame()),
                     ('UI_BarFill', bar_fill()), ('UI_Knob', knob()), ('UI_Check', check())):
        im.save(os.path.join(OUT, name + '.png'))
    print(sorted(os.listdir(OUT)))
