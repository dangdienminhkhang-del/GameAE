"""Mẫu A0: người chơi (thân + vũ khí tách lớp), quái Slime, gạch sàn, đèn hải lưu.
Chạy: python make_sample.py  -> ghi PNG vào ./out
Cần: Python 3, Pillow, numpy (pip install pillow numpy). Xem docs/14_ART_CONTRACT.md."""
import os, math
import numpy as np
from PIL import Image
from pixelkit import Canvas, strip, up, hexc, PALETTE

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out')
os.makedirs(OUT, exist_ok=True)

# ---------------------------------------------------------------- NGƯỜI CHƠI
W, H = 48, 48          # khung 48x48 (chừa chỗ cho vũ khí vung), pivot ở chân: giữa, cách đáy 2 px
OX = 8                 # thân nhân vật đặt lệch phải 8 px để kiếm có chỗ vươn về phía trước


def player_pose(bob=0, st=0, lift_f=0, lift_b=0, sw=0, scarf=0, breathe=0):
    """Trả về (ảnh thân, vị trí bàn tay cầm vũ khí). Nhân vật quay PHẢI; quay trái bằng flipX trong game."""
    c = Canvas(W, H)
    by = -bob
    ox = OX
    # chân sau, chân trước (ván trượt nhẹ khi bước)
    for (cx, lift) in ((14 - st, lift_b), (18 + st, lift_f)):
        top = 35 + by
        end = 45 - lift
        c.rect(ox + cx - 2, top, ox + cx + 2, end - 3, 'pants')
        c.rect(ox + cx - 2, end - 4, ox + cx + 3, end + 1, 'boot')
    # đuôi khăn rủ phía sau
    c.poly([(ox + 11, 20 + by), (ox + 8 - scarf, 22 + by), (ox + 7 - scarf, 27 + by + scarf), (ox + 11, 25 + by)], 'rust')
    # tay trái (phía xa, nằm ngoài mép trái áo) – vẽ trước thân
    lhx, lhy = ox + 8 - sw, 31 + by
    c.line(ox + 11, 22 + by, lhx, lhy - 2, 'sleeve', th=3)
    c.rect(lhx - 1, lhy - 2, lhx + 2, lhy, 'rust')          # cổ tay áo (viền đỏ gỉ)
    c.rect(lhx - 1, lhy, lhx + 2, lhy + 2, 'skin')
    # áo mưa dài tới gối
    c.poly([(ox + 11, 21 + by), (ox + 21, 21 + by), (ox + 23, 36 + by + breathe), (ox + 9, 36 + by + breathe)], 'coat')
    # đai dây thừng, khóa beacon, cuộn dây
    c.rect(ox + 10, 28 + by, ox + 22, 30 + by, 'rope')
    c.rect(ox + 16, 28 + by, ox + 18, 30 + by, 'beacon')
    c.ellipse(ox + 11.5, 32 + by, 2.0, 2.4, 'rope')
    # khăn quàng cổ + ghim vàng
    c.rect(ox + 10, 19 + by, ox + 22, 22 + by, 'rust')
    c.px(ox + 17, 20 + by, 'beacon')
    # tay phải (cầm kiếm) – vẽ trước mặt thân, ra mép phải
    rhx, rhy = ox + 24 + sw, 31 + by
    c.line(ox + 21, 22 + by, rhx, rhy - 2, 'sleeve', th=3)
    c.rect(rhx - 1, rhy - 2, rhx + 2, rhy, 'rust')
    c.rect(rhx - 1, rhy, rhx + 2, rhy + 2, 'skin')
    # đầu: tóc sau -> mặt -> mái -> mắt
    c.ellipse(ox + 15.5, 10 + by, 7.6, 7.2, 'hair')
    c.ellipse(ox + 17.0, 12 + by, 5.9, 5.3, 'skin')
    c.poly([(ox + 9, 7 + by), (ox + 20, 4.5 + by), (ox + 23.5, 8 + by), (ox + 20, 9 + by), (ox + 17, 8 + by), (ox + 14, 10 + by), (ox + 10, 11 + by)], 'hair')
    c.rect(ox + 8, 9 + by, ox + 12, 16 + by, 'hair')
    c.rect(ox + 17, 11 + by, ox + 19, 14 + by, 'eye')
    c.rect(ox + 21, 11 + by, ox + 23, 14 + by, 'eye')
    c.px(ox + 17, 11 + by, 'white'); c.px(ox + 21, 11 + by, 'white')
    img = c.render()
    return img, (rhx + 0.5, rhy + 1)


IDLE = [dict(bob=0, sw=0, scarf=0), dict(bob=0, sw=0, scarf=1), dict(bob=1, sw=0, scarf=1), dict(bob=1, sw=0, scarf=0)]
STR = [0, 2, 3, 0, -2, -3]
WALK = []
for i, s in enumerate(STR):
    fwd_lift = 2 if i in (1, 2) else 0
    back_lift = 2 if i in (4, 5) else 0
    WALK.append(dict(bob=1 if i in (1, 4) else 0, st=s, lift_f=back_lift, lift_b=fwd_lift,
                     sw=(-1 if s > 0 else 1 if s < 0 else 0), scarf=1 + (i % 2)))


def weapon_frame(hand):
    """Kiếm Neo Gỉ: vũ khí là LỚP RIÊNG, cùng khung 48x48 với thân (gắn qua Sprite Library)."""
    c = Canvas(W, H)
    hx, hy = hand
    a = math.radians(-48)
    dx, dy = math.cos(a), math.sin(a)
    c.line(hx - dx * 2, hy - dy * 2, hx + dx * 2, hy + dy * 2, 'rope', th=2)            # cán
    gx, gy = hx + dx * 3, hy + dy * 3
    c.line(gx - dy * 3, gy + dx * 3, gx + dy * 3, gy - dx * 3, 'rust', th=2)             # chắn kiếm hình neo
    c.px(int(gx - dy * 4), int(gy + dx * 4) + 1, 'rust'); c.px(int(gx + dy * 4), int(gy - dx * 4) + 1, 'rust')
    ex, ey = gx + dx * 15, gy + dy * 15
    c.line(gx + dx, gy + dy, ex, ey, 'blade', th=3)                                       # lưỡi
    c.px(int(ex + dx * 1.5), int(ey + dy * 1.5), 'blade')
    c.px(int(gx + dx * 7), int(gy + dy * 7) + 1, 'rust')                                  # vết gỉ
    c.px(int(gx + dx * 11), int(gy + dy * 11), 'rust')
    return c.render()


def build_player():
    idle_b, idle_w, walk_b, walk_w = [], [], [], []
    for p in IDLE:
        b, hand = player_pose(**p)
        idle_b.append(b); idle_w.append(weapon_frame(hand))
    for p in WALK:
        b, hand = player_pose(**p)
        walk_b.append(b); walk_w.append(weapon_frame(hand))
    return idle_b, idle_w, walk_b, walk_w


# ---------------------------------------------------------------- SLIME
def slime_frame(squash=0):
    c = Canvas(32, 32)
    ry = 8 - squash
    rx = 11 + squash
    cy = 24 - ry + 1
    c.ellipse(16, cy, rx, ry, 'slime')
    c.ellipse(11, cy - ry * 0.45, 3, 1.6, 'white')            # điểm sáng
    ey = int(cy) - 1
    c.rect(11, ey, 13, ey + 3, 'eye'); c.rect(18, ey, 20, ey + 3, 'eye')
    c.px(11, ey, 'white'); c.px(18, ey, 'white')
    c.rect(14, ey + 4, 18, ey + 5, 'eye')                     # miệng nhỏ
    return c.render()


def build_slime():
    return [slime_frame(s) for s in (0, 1, 2, 1)]


# ---------------------------------------------------------------- GẠCH SÀN (32x32, liền mạch)
def dock_tile(seed):
    """Ván cầu tàu: 4 hàng ván, mỗi hàng có mối nối riêng, đinh, vân gỗ liền mạch hai mép."""
    r = np.random.RandomState(seed)
    img = np.zeros((32, 32, 4), dtype=np.uint8)
    for row in range(4):
        y0 = row * 8
        joint = r.randint(6, 26)
        tones = (['wood', 'wood2'])
        for seg, (xa, xb) in enumerate(((0, joint), (joint, 32))):
            ramp = PALETTE[tones[(row + seg + seed) % 2]]
            for y in range(y0, y0 + 8):
                for x in range(xa, xb):
                    t = ramp[2]
                    ly = y - y0
                    if ly == 0:
                        t = ramp[4 if seg == 0 else 3]          # mép ván sáng phía trên
                    elif ly == 7:
                        t = ramp[0]                              # khe giữa các hàng
                    elif ly >= 6:
                        t = ramp[1]
                    if x == xa and xa != 0:
                        t = ramp[0]                              # mối nối dọc
                    img[y, x] = (*hexc(t), 255)
            # vân gỗ (liền mạch: dùng modulo)
            for g in range(r.randint(2, 4)):
                gy = y0 + r.randint(2, 6)
                gx = r.randint(0, 32)
                ln = r.randint(5, 12)
                for k in range(ln):
                    x = (gx + k) % 32
                    if xa <= x < xb and img[gy, x][3] and gy % 8 not in (0, 7):
                        img[gy, x] = (*hexc(ramp[1]), 255)
            # đinh ở hai đầu ván
            for nx_ in (xa + 2, xb - 3):
                if xa <= nx_ < xb and 1 <= nx_ < 31:
                    img[y0 + 3, nx_] = (*hexc(PALETTE['nail'][3]), 255)
                    img[y0 + 4, nx_] = (*hexc(PALETTE['nail'][1]), 255)
    return Image.fromarray(img, 'RGBA')


def wet_stone_tile(seed):
    r = np.random.RandomState(seed)
    img = np.zeros((32, 32, 4), dtype=np.uint8)
    img[:, :] = (*hexc(PALETTE['stone'][0]), 255)
    # lát đá lệch hàng
    for row in range(4):
        y0 = row * 8
        off = (row % 2) * 8
        for k in range(4):
            xa = (off + k * 16 - 0) % 32
            wdt = 16
            ramp = PALETTE['stone' if (row + k + seed) % 2 else 'stone2']
            for y in range(y0 + 1, y0 + 8):
                for x in range(xa + 1, xa + wdt):
                    xx = x % 32
                    ly = y - y0
                    t = ramp[2]
                    if ly == 1:
                        t = ramp[3]
                    elif ly >= 6:
                        t = ramp[1]
                    img[y, xx] = (*hexc(t), 255)
            # đốm rêu/ẩm
            for _ in range(2):
                px_ = (xa + r.randint(2, 14)) % 32
                py_ = y0 + r.randint(2, 6)
                img[py_, px_] = (*hexc(PALETTE['stone'][1]), 255)
    return Image.fromarray(img, 'RGBA')


def water_tile(phase):
    img = np.zeros((32, 32, 4), dtype=np.uint8)
    base = PALETTE['water']
    for y in range(32):
        for x in range(32):
            v = math.sin((x + phase * 8) / 32 * 2 * math.pi * 2 + y * 0.35) + math.sin((x * 0.5 - phase * 8) / 32 * 2 * math.pi + y * 0.2)
            idx = 2
            if v > 1.55:
                idx = 3
            elif v < -1.4:
                idx = 1
            img[y, x] = (*hexc(base[idx]), 255)
    # bọt nhỏ
    for (fx, fy) in ((4 + phase * 8, 9), (20 + phase * 8, 22), (12 - phase * 8, 27)):
        for k in range(4):
            img[fy % 32, (fx + k) % 32] = (*hexc(PALETTE['foam'][2]), 255)
    return Image.fromarray(img, 'RGBA')


# ---------------------------------------------------------------- ĐÈN HẢI LƯU (16x48; chân ở đáy)
def lamp_post(glow_on=True):
    c = Canvas(16, 48)
    c.rect(7, 18, 9, 44, 'iron')
    c.rect(4, 42, 12, 46, 'iron')                 # đế
    c.rect(5, 12, 11, 20, 'iron')                 # khung đèn
    c.rect(6, 13, 10, 19, 'glow' if glow_on else 'steel')
    c.poly([(4, 12), (12, 12), (10, 8), (6, 8)], 'rust')   # mũ đèn
    c.rect(7, 5, 9, 8, 'iron')
    return c.render()


if __name__ == '__main__':
    idle_b, idle_w, walk_b, walk_w = build_player()
    strip(idle_b).save(f'{OUT}/Player_Body_Idle.png')
    strip(walk_b).save(f'{OUT}/Player_Body_Walk.png')
    strip(idle_w).save(f'{OUT}/Player_Weapon_RustedAnchorBlade_Idle.png')
    strip(walk_w).save(f'{OUT}/Player_Weapon_RustedAnchorBlade_Walk.png')
    sl = build_slime()
    strip(sl).save(f'{OUT}/Enemy_Slime_Idle.png')
    tiles = [dock_tile(s) for s in range(4)] + [wet_stone_tile(s) for s in range(2)] + [water_tile(0), water_tile(1)]
    strip(tiles).save(f'{OUT}/Tiles_Floor01_Harbor.png')
    lamp_post().save(f'{OUT}/Prop_BeaconLamp.png')
    print('ok', os.listdir(OUT))
