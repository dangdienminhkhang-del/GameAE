"""Lô A: Tầng 2 (Lò Rèn Ngập Nước) - gạch, vật dụng; icon giáp; hiệu ứng báo đòn / tan khi chết.
Chạy: python make_floor02_batch.py  -> ghi PNG vào ./out_batchA
Cần: Python 3, Pillow, numpy. Mọi màu lấy từ PALETTE (pixelkit) cộng thêm vài dải màu cho Tầng 2.
"""
import os, math
import numpy as np
from PIL import Image
import pixelkit
from pixelkit import Canvas, strip, hexc, PALETTE

# ---- dải màu riêng của Tầng 2 (tông xanh thép + đồng gỉ + than hồng) ----
PALETTE.update({
    'forge':  ['#0d1618', '#17262a', '#25393d', '#375257', '#4f7378'],
    'boiler': ['#1c0f0b', '#3a1d14', '#5e2f1e', '#85472d', '#b0683f'],
    'brass':  ['#2e2008', '#5c4210', '#94701f', '#c7a03a', '#efd47a'],
    'ember':  ['#3a0f08', '#8a2410', '#d9501a', '#ff8c2e', '#ffd27a'],
    'murk':   ['#07191a', '#0d2e2e', '#154645', '#216561', '#3a8a82'],
    'pipe':   ['#10181b', '#1f2f33', '#34494e', '#52707a', '#7fa3ad'],
    'void':   ['#05080b', '#070b0f', '#0a1015', '#0e161c', '#142029'],
})

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out_batchA')
os.makedirs(OUT, exist_ok=True)


def col(name, i, a=255):
    return (*hexc(PALETTE[name][i]), a)


def blank(w, h):
    return np.zeros((h, w, 4), dtype=np.uint8)


# ================================================================ GẠCH 32x32 (liền mạch)
def rivet(img, x, y):
    img[y % 32, x % 32] = col('pipe', 4)
    img[(y + 1) % 32, (x + 1) % 32] = col('forge', 0)


def forge_floor(variant=0, seed=0):
    r = np.random.RandomState(seed + variant * 7)
    img = blank(32, 32)
    for py in range(2):
        for px_ in range(2):
            x0, y0 = px_ * 16, py * 16
            tone = r.choice([2, 2, 1])
            ramp = PALETTE['forge']
            for y in range(16):
                for x in range(16):
                    t = ramp[tone]
                    if y == 0 or x == 0:
                        t = ramp[min(4, tone + 2)]
                    elif y == 1 or x == 1:
                        t = ramp[min(4, tone + 1)]
                    elif y == 15 or x == 15:
                        t = ramp[0]
                    elif y == 14 or x == 14:
                        t = ramp[max(0, tone - 1)]
                    img[y0 + y, x0 + x] = (*hexc(t), 255)
            if variant == 0:      # vân chống trượt (chấm xen kẽ)
                for y in range(4, 13, 3):
                    for x in range(4, 13, 3):
                        if ((x + y) // 3) % 2 == 0:
                            img[y0 + y, x0 + x] = col('forge', 4)
                            img[y0 + y + 1, x0 + x] = col('forge', 1)
            elif variant == 1:    # khe lưới thoát nước
                for gy in (5, 8, 11):
                    for x in range(4, 12):
                        img[y0 + gy, x0 + x] = col('forge', 0)
                        img[y0 + gy + 1, x0 + x] = col('forge', 3)
            else:                 # tấm trơn, vết gỉ
                for _ in range(5):
                    rx_, ry_ = r.randint(3, 13), r.randint(3, 13)
                    img[y0 + ry_, x0 + rx_] = col('boiler', 1)
                    img[y0 + ry_ + 1, x0 + rx_] = col('boiler', 2)
            for (rx_, ry_) in ((3, 3), (12, 3), (3, 12), (12, 12)):
                rivet(img, x0 + rx_, y0 + ry_)
    return Image.fromarray(img, 'RGBA')


def boiler_wall(seed=3):
    r = np.random.RandomState(seed)
    img = blank(32, 32)
    ramp = PALETTE['boiler']
    for y in range(32):
        for x in range(32):
            img[y, x] = (*hexc(ramp[2]), 255)
    for sy in (0, 10, 21):                     # mối hàn ngang giữa các tấm
        for x in range(32):
            img[sy % 32, x] = col('boiler', 0)
            img[(sy + 1) % 32, x] = col('boiler', 3)
            img[(sy + 2) % 32, x] = col('boiler', 3) if x % 2 else col('boiler', 2)
    for ry_ in (4, 15, 26):                    # hàng đinh tán
        for x in range(2, 32, 8):
            img[ry_, x] = col('brass', 3)
            img[ry_ + 1, x] = col('boiler', 0)
    for _ in range(9):                         # vệt gỉ chảy dọc
        x = r.randint(0, 32)
        y = r.randint(0, 24)
        for k in range(r.randint(3, 8)):
            if img[(y + k) % 32, x][0] > 0 and tuple(img[(y + k) % 32, x][:3]) != hexc(ramp[0]):
                img[(y + k) % 32, x] = col('boiler', 1)
    for y in range(32):                        # ống thép đứng
        for x in range(13, 20):
            t = 2
            if x == 13:
                t = 4
            elif x == 14:
                t = 3
            elif x == 19:
                t = 0
            elif x == 18:
                t = 1
            img[y, x] = col('pipe', t)
    for fy in (5, 16, 27):                     # mặt bích đồng
        for x in range(12, 21):
            img[fy, x] = col('brass', 4)
            img[fy + 1, x] = col('brass', 3)
            img[fy + 2, x] = col('brass', 1)
    return Image.fromarray(img, 'RGBA')


def flooded(phase):
    img = blank(32, 32)
    base = PALETTE['murk']
    for y in range(32):
        for x in range(32):
            v = math.sin((x + phase * 8) / 32 * 2 * math.pi * 2 + y * 0.4) + math.sin((x * 0.5 - phase * 8) / 32 * 2 * math.pi + y * 0.25)
            idx = 2
            if v > 1.5:
                idx = 3
            elif v < -1.4:
                idx = 1
            if (x % 16 == 0 or y % 16 == 0):       # mối tấm sàn lờ mờ dưới nước
                idx = max(0, idx - 1)
            img[y, x] = (*hexc(base[idx]), 255)
    for (fx, fy) in ((5 + phase * 8, 8), (21 - phase * 8, 20), (12 + phase * 8, 28)):
        for k in range(3):
            img[fy % 32, (fx + k) % 32] = col('murk', 4)
    return Image.fromarray(img, 'RGBA')


def vent(phase):
    t = forge_floor(1, 11)
    img = np.array(t)
    for gy in (7, 14, 21):
        for x in range(6, 26):
            if phase == 0:
                body, edge, halo = 2, 1, 1
            else:
                body, edge, halo = 3, 2, 2
            img[gy, x] = col('ember', 0)
            img[gy + 1, x] = col('ember', body)
            img[gy + 2, x] = col('ember', 4 if phase else 3) if 9 <= x <= 22 else col('ember', edge)
            img[gy + 3, x] = col('ember', 0)
            img[gy - 1, x] = col('ember', halo) if x % 3 else img[gy - 1, x]
    return Image.fromarray(img, 'RGBA')


def conveyor(frame):
    img = blank(32, 32)
    for y in range(32):
        for x in range(32):
            if y < 5 or y >= 27:       # thanh ray hai bên
                t = 3 if y in (0, 27) else 2
                if y in (4, 31):
                    t = 0
                img[y, x] = col('pipe', t)
            else:                       # băng tải
                shift = (x - frame * 4) % 8
                t = 1
                if shift == 0:
                    t = 0
                elif shift == 1:
                    t = 3
                img[y, x] = col('iron', t)
    for x in range(3, 32, 8):
        img[2, x] = col('brass', 3)
    for cx in (4, 20):                  # mũi tên chỉ chiều chạy
        cx = (cx + frame * 4) % 32
        for d in range(5):
            for yy in (16 - d, 16 + d):
                img[yy, (cx - d + 6) % 32] = col('ember', 2)
    return Image.fromarray(img, 'RGBA')


# ================================================================ VẬT DỤNG
def erase_inside(c, pid, cx, cy, rx, ry):
    for y in range(c.h):
        for x in range(c.w):
            if c.part[y, x] == pid and ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                c.mat[y, x] = 0
                c.part[y, x] = 0


def prop_crate():
    c = Canvas(32, 32)
    c.rect(5, 10, 27, 28, 'wood')
    c.rect(5, 10, 9, 28, 'wood2'); c.rect(23, 10, 27, 28, 'wood2')
    c.rect(5, 10, 27, 14, 'wood2'); c.rect(5, 24, 27, 28, 'wood2')
    c.line(9, 14, 23, 24, 'wood2', th=2)
    for (x, y) in ((6, 11), (25, 11), (6, 26), (25, 26)):
        c.px(x, y, 'nail')
    c.rect(13, 12, 19, 13, 'rust')
    return c.render()


def prop_barrel():
    c = Canvas(32, 32)
    c.rect(8, 9, 24, 27, 'boiler')
    c.ellipse(16, 27, 8, 2.6, 'boiler')
    c.rect(8, 14, 24, 16, 'iron'); c.rect(8, 22, 24, 24, 'iron')
    c.ellipse(16, 9, 8, 3.2, 'iron')
    c.ellipse(16, 9, 3.2, 1.4, 'brass')
    c.px(11, 19, 'ember'); c.px(20, 12, 'rust')
    return c.render()


def prop_ropecoil():
    c = Canvas(32, 32)
    p1 = c.ellipse(16, 23, 10, 5.5, 'rope'); erase_inside(c, p1, 16, 23, 5.4, 2.4)
    p2 = c.ellipse(16, 18, 10, 5.5, 'rope'); erase_inside(c, p2, 16, 18, 5.4, 2.4)
    c.line(25, 21, 28, 27, 'rope', th=2)
    c.px(28, 27, 'rope')
    return c.render()


def prop_sign():
    c = Canvas(24, 32)
    c.rect(10, 14, 14, 31, 'wood')
    c.poly([(1, 5), (17, 5), (22, 10), (17, 15), (1, 15)], 'wood2')
    c.rect(3, 9, 16, 11, 'rust')
    c.px(3, 6, 'nail'); c.px(3, 13, 'nail')
    return c.render()


def prop_valve():
    c = Canvas(32, 32)
    c.rect(0, 20, 32, 27, 'pipe')
    c.rect(6, 18, 9, 29, 'brass'); c.rect(23, 18, 26, 29, 'brass')
    c.rect(14, 12, 18, 20, 'pipe')
    p = c.ellipse(16, 7, 8, 5.5, 'rust'); erase_inside(c, p, 16, 7, 5.2, 3.2)
    c.line(16, 2, 16, 12, 'rust', th=1); c.line(9, 7, 23, 7, 'rust', th=1)
    c.ellipse(16, 7, 2.0, 1.5, 'brass')
    return c.render()


def prop_gate():
    c = Canvas(64, 64)
    c.rect(4, 22, 60, 62, 'stone')
    c.ellipse(32, 22, 28, 18, 'stone')
    for y in range(64):                            # lỗ cổng vòm
        for x in range(64):
            inside = (14 <= x < 50 and 24 <= y < 62) or (((x + 0.5 - 32) / 18) ** 2 + ((y + 0.5 - 24) / 16) ** 2 <= 1.0 and y < 24)
            if inside and c.mat[y, x] != 0:
                c.mat[y, x] = 0; c.part[y, x] = 0
    c.rect(14, 54, 50, 62, 'ember')               # ánh lò rèn lọt qua khe cổng
    c.rect(14, 24, 50, 54, 'void')
    c.ellipse(32, 24, 18, 16, 'void')
    # nén lại phần trên vòm để ô trống đúng hình vòm
    for y in range(8, 24):
        for x in range(14, 50):
            if ((x + 0.5 - 32) / 18) ** 2 + ((y + 0.5 - 24) / 16) ** 2 > 1.0 and c.names[c.mat[y, x]] == 'void':
                c.mat[y, x] = c._m('stone'); c.part[y, x] = 1
    for x in range(17, 49, 6):                     # song sắt
        top = 24 - int(15 * math.sqrt(max(0.0, 1 - ((x + 1 - 32) / 18) ** 2)))
        c.rect(x, top + 1, x + 2, 62, 'iron')
    c.rect(14, 34, 50, 36, 'iron'); c.rect(14, 46, 50, 48, 'iron')
    c.rect(29, 3, 35, 9, 'brass')                  # đá đỉnh vòm
    c.rect(2, 58, 62, 63, 'stone2')
    return c.render()


def prop_forge_lamp():
    c = Canvas(32, 32)
    c.rect(14, 12, 18, 30, 'iron')
    c.rect(9, 28, 23, 31, 'iron')
    c.rect(14, 10, 26, 13, 'iron')
    c.rect(20, 13, 27, 24, 'brass')
    c.rect(22, 15, 25, 22, 'ember')
    c.poly([(19, 13), (28, 13), (26, 9), (21, 9)], 'rust')
    return c.render()


# ================================================================ ICON GIÁP 32x32
def icon_armor_anchor():
    c = Canvas(32, 32)
    c.poly([(8, 9), (13, 6), (15, 10), (17, 10), (19, 6), (24, 9), (28, 14), (23, 16), (22, 27), (10, 27), (9, 16), (4, 14)], 'coat')
    c.rect(13, 6, 19, 9, 'rust')
    c.line(10, 12, 23, 25, 'rope', th=2); c.line(22, 12, 10, 25, 'rope', th=2)
    c.rect(15, 15, 17, 18, 'beacon'); c.rect(14, 16, 18, 17, 'beacon')
    c.rect(10, 22, 22, 24, 'rope')
    return c.render()


def icon_armor_leather():
    c = Canvas(32, 32)
    c.poly([(8, 9), (13, 6), (15, 10), (17, 10), (19, 6), (24, 9), (28, 14), (23, 16), (22, 27), (10, 27), (9, 16), (4, 14)], 'boot')
    c.rect(13, 6, 19, 9, 'wood')
    for y in range(11, 26, 3):
        c.px(16, y, 'rope')
    c.rect(10, 22, 22, 24, 'wood2')
    c.px(11, 14, 'nail'); c.px(21, 14, 'nail'); c.px(12, 23, 'brass'); c.px(20, 23, 'brass')
    return c.render()


# ================================================================ HIỆU ỨNG (màu trắng trung tính, để code nhuộm màu báo đòn)
def fx_telegraph_ring(size=64):
    img = blank(size, size)
    c = (size - 1) / 2.0
    for y in range(size):
        for x in range(size):
            dx, dy = x - c, y - c
            d = math.hypot(dx, dy)
            ang = (math.degrees(math.atan2(dy, dx)) + 360) % 360
            if 27.0 <= d < 30.0:
                if (ang % 15) < 12:
                    img[y, x] = (*hexc(PALETTE['white'][3]), 255 if d < 29 else 200)
            elif 23.0 <= d < 24.0:
                img[y, x] = (*hexc(PALETTE['white'][2]), 150)
            elif 24.0 <= d < 27.0:
                for a0 in (0, 90, 180, 270):         # 4 mũi tên chỉ vào tâm
                    da = abs(((ang - a0 + 180) % 360) - 180)
                    if da < (27.0 - d) * 3.2:
                        img[y, x] = (*hexc(PALETTE['white'][2]), 230)
    return Image.fromarray(img, 'RGBA')


def fx_dissipate(frames=6, size=32):
    out = []
    r = np.random.RandomState(5)
    jit = [(r.uniform(-0.25, 0.25), r.uniform(0.8, 1.2)) for _ in range(12)]
    for k in range(frames):
        img = blank(size, size)
        c = (size - 1) / 2.0
        t = k / (frames - 1)
        for i in range(12):
            ang = math.radians(i * 30 + jit[i][0] * 40)
            dist = (3 + k * 2.6) * jit[i][1]
            px_, py_ = c + math.cos(ang) * dist, c + math.sin(ang) * dist - k * 0.6
            sz = max(1, 3 - k // 2)
            tone = PALETTE['white'][max(1, 4 - k // 2)]
            alpha = int(255 * (1 - t * 0.85))
            for dy in range(sz):
                for dx in range(sz):
                    xx, yy = int(px_) + dx, int(py_) + dy
                    if 0 <= xx < size and 0 <= yy < size:
                        img[yy, xx] = (*hexc(tone), alpha)
        rad = max(0.0, 7.5 - k * 1.5)            # đám khói tan ở giữa
        for y in range(size):
            for x in range(size):
                if rad > 0 and math.hypot(x - c, y - c) <= rad:
                    img[y, x] = (*hexc(PALETTE['white'][2]), int(200 * (1 - t)))
        out.append(Image.fromarray(img, 'RGBA'))
    return strip(out)


# ================================================================ MAIN
if __name__ == '__main__':
    def save(img, name):
        img.save(os.path.join(OUT, name))
    forge = [forge_floor(0, 1), forge_floor(0, 2), forge_floor(1, 3), forge_floor(2, 4),
             boiler_wall(), flooded(0), flooded(1), vent(0)]
    save(strip(forge), 'Tiles_Floor02_Forge.png')
    save(strip([vent(0), vent(1)]), 'Tiles_Floor02_Vent.png')
    save(strip([conveyor(i) for i in range(4)]), 'Tiles_Floor02_Conveyor.png')
    save(prop_crate(), 'Prop_Crate.png')
    save(prop_barrel(), 'Prop_Barrel.png')
    save(prop_ropecoil(), 'Prop_RopeCoil.png')
    save(prop_sign(), 'Prop_Sign.png')
    save(prop_valve(), 'Prop_Valve.png')
    save(prop_gate(), 'Prop_DungeonGate.png')
    save(prop_forge_lamp(), 'Prop_ForgeLamp.png')
    save(icon_armor_anchor(), 'Item_Armor_AnchorRope.png')
    save(icon_armor_leather(), 'Item_Armor_Leather.png')
    save(fx_telegraph_ring(), 'FX_TelegraphRing.png')
    save(fx_dissipate(), 'FX_Dissipate.png')
    save(forge_floor(0, 9), 'Backdrop_Floor02.png')
    save(prop_forge_lamp(), 'Lamp_Post.png')
    print(sorted(os.listdir(OUT)))
