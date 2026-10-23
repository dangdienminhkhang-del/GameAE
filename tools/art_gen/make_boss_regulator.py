"""Lô D: boss Tầng 2 - Bộ Điều Áp Cổ (golem lò hơi). Khung 96x96, pivot giữa.
Chạy: python make_boss_regulator.py -> out_batchD/Boss_PressureRegulator.png (11 khung: idle, walk x4, attack x6)
Boss là cỗ máy đối xứng nên dùng chung một bộ khung cho cả 8 hướng.
Cần make_floor02_batch.py (bảng màu Tầng 2 + hàm erase_inside) và pixelkit.py cùng thư mục.
"""
import os
import make_floor02_batch as A
from pixelkit import Canvas, strip

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out_batchD')
os.makedirs(OUT, exist_ok=True)
S = 96


def arm(c, shoulder, elbow, fist):
    """Cánh tay ống thép: khớp vai, đoạn trên, khuỷu đồng, đoạn dưới, nắm đấm vô-lăng van."""
    c.line(shoulder[0], shoulder[1], elbow[0], elbow[1], 'pipe', th=7)
    c.ellipse(shoulder[0], shoulder[1], 6, 6, 'iron')
    c.line(elbow[0], elbow[1], fist[0], fist[1], 'iron', th=7)
    c.ellipse(elbow[0], elbow[1], 4.5, 4.5, 'brass')
    p = c.ellipse(fist[0], fist[1], 9, 9, 'rust')
    A.erase_inside(c, p, fist[0], fist[1], 5.5, 5.5)
    c.line(fist[0] - 9, fist[1], fist[0] + 9, fist[1], 'rust', th=1)
    c.line(fist[0], fist[1] - 9, fist[0], fist[1] + 9, 'rust', th=1)
    c.ellipse(fist[0], fist[1], 2.5, 2.5, 'brass')


def draw(b=0, lift_l=0, lift_r=0, arm_l=None, arm_r=None, needle=(2, -6), eye='ember', puffs=(), shock=None):
    c = Canvas(S, S)
    arm_l = arm_l or ((12, 56), (12, 72))
    arm_r = arm_r or ((84, 56), (84, 72))
    # ống xả sau vai
    c.rect(20, 22 + b, 27, 44 + b, 'pipe'); c.rect(18, 18 + b, 29, 23 + b, 'brass')
    c.rect(69, 22 + b, 76, 44 + b, 'pipe'); c.rect(67, 18 + b, 78, 23 + b, 'brass')
    # chân
    c.rect(30, 70 + b, 43, 88 - lift_l, 'iron'); c.rect(27, 86 - lift_l, 45, 92 - lift_l, 'rust')
    c.rect(53, 70 + b, 66, 88 - lift_r, 'iron'); c.rect(51, 86 - lift_r, 69, 92 - lift_r, 'rust')
    # thân nồi hơi
    c.rect(23, 30 + b, 73, 76 + b, 'boiler')
    c.rect(23, 40 + b, 73, 43 + b, 'iron'); c.rect(23, 63 + b, 73, 66 + b, 'iron')
    for x in range(28, 72, 8):
        c.px(x, 41 + b, 'brass'); c.px(x, 64 + b, 'brass')
    # cửa lò than hồng
    c.rect(39, 67 + b, 57, 75 + b, 'iron'); c.rect(41, 69 + b, 55, 74 + b, 'ember')
    # đồng hồ áp suất
    c.ellipse(48, 52 + b, 10, 10, 'brass'); c.ellipse(48, 52 + b, 7.5, 7.5, 'white')
    c.line(48, 52 + b, 48 + needle[0], 52 + b + needle[1], 'ember' if eye == 'glow' else 'rust', th=1)
    # đầu vòm và van trên đỉnh
    c.ellipse(48, 24 + b, 15, 11, 'iron')
    c.rect(45, 10 + b, 51, 20 + b, 'pipe'); c.rect(42, 8 + b, 54, 11 + b, 'brass')
    c.rect(40, 23 + b, 46, 26 + b, eye); c.rect(50, 23 + b, 56, 26 + b, eye)
    # giáp vai
    c.ellipse(22, 38 + b, 8, 7, 'iron'); c.ellipse(74, 38 + b, 8, 7, 'iron')
    arm(c, (18, 42 + b), arm_l[0], arm_l[1])
    arm(c, (78, 42 + b), arm_r[0], arm_r[1])
    for (x, y, r) in puffs:
        c.ellipse(x, y + b, r, r, 'foam')
    if shock is not None:
        p = c.ellipse(shock, 89, 22, 5, 'white'); A.erase_inside(c, p, shock, 89, 16, 3.2)
    return c.render()


def frames():
    out = []
    out.append(draw())                                                           # idle
    L = ((12, 56), (12, 72)); R = ((84, 56), (84, 72))
    out.append(draw(b=0, lift_r=4, arm_l=((12, 54), (14, 70)), arm_r=((84, 58), (82, 74)), puffs=((23, 14, 2),)))
    out.append(draw(b=-1, arm_l=L, arm_r=R))
    out.append(draw(b=0, lift_l=4, arm_l=((12, 58), (10, 74)), arm_r=((84, 54), (86, 70)), puffs=((73, 14, 2),)))
    out.append(draw(b=-1, arm_l=L, arm_r=R))
    # attack
    out.append(draw(b=1, arm_r=((86, 50), (88, 60)), needle=(4, -4), puffs=((73, 12, 3),)))
    out.append(draw(b=2, arm_r=((90, 38), (88, 28)), needle=(5, -3), eye='glow', puffs=((23, 12, 4), (73, 12, 4))))
    out.append(draw(b=3, arm_r=((88, 22), (80, 8)), needle=(6, -2), eye='glow', puffs=((23, 10, 5), (73, 10, 5))))
    out.append(draw(b=-1, arm_l=((26, 56), (28, 76)), arm_r=((76, 56), (70, 78)), needle=(6, 0), eye='glow', puffs=((23, 14, 3),)))
    out.append(draw(b=-2, arm_l=((26, 58), (28, 80)), arm_r=((74, 66), (66, 86)), needle=(6, 2), eye='glow', puffs=((23, 12, 4), (73, 12, 4)), shock=66))
    out.append(draw(b=0, arm_r=((84, 56), (80, 76)), needle=(3, -5), puffs=((73, 14, 2),)))
    return out


if __name__ == '__main__':
    fr = frames()
    strip(fr).save(os.path.join(OUT, 'Boss_PressureRegulator.png'))
    print(len(fr), 'khung', fr[0].size)
