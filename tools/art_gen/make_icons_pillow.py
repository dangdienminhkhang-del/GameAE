#!/usr/bin/env python3
"""make_glyphs_v3.py - bo icon nut "gon sach" cho Game2D (chi can Pillow + numpy).

Y tuong: moi icon la mot hinh trang-bac sang, co vien toi quanh hinh + bong do nhe,
nen doc duoc tren moi nen the gioi (co, da lat, cat, nuoc) ma KHONG can o nen hay khung.
Cac nut ky nang dung "vong" rieng (Ring_L / Ring_M / Ring_S).

Cach chay:  python make_glyphs_v3.py <thu_muc_ra> [kich_thuoc_icon=192]
Ve o khong gian thiet ke 128x128, sieu lay mau x4 roi thu nho -> muot, khong rang cua.
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

D = 128.0          # khong gian thiet ke
SS = 4             # sieu lay mau
NAVY = (9, 17, 31)

# (mau tren, mau duoi) - bang mau lay theo bang mau du an (muoi, thep, dong, ri set, vang beacon)
STYLES = {
    'steel':  ((255, 255, 255), (172, 193, 215)),
    'mist':   ((236, 247, 255), (186, 212, 236)),
    'brass':  ((255, 233, 152), (196, 138, 36)),
    'wood':   ((218, 180, 120), (138, 94, 50)),
    'rust':   ((228, 112, 80), (150, 56, 38)),
    'teal':   ((140, 232, 212), (44, 148, 138)),
    'beacon': ((255, 243, 168), (240, 168, 28)),
}


# ----------------------------------------------------------------- hinh hoc
def arc(cx, cy, r, a0, a1, n=48):
    """Diem tren cung tron (goc do, truc y huong xuong: 90 = xuong, 270 = len)."""
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def bez(p0, p1, p2, p3, n=36):
    out = []
    for i in range(n + 1):
        t = i / n
        u = 1 - t
        out.append((u ** 3 * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t ** 3 * p3[0],
                    u ** 3 * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t ** 3 * p3[1]))
    return out


def xf(pts, rot=0.0, s=1.0, cx=64.0, cy=64.0, dx=0.0, dy=0.0):
    """Quay (do, chieu kim dong ho tren man hinh) + co + doi cac diem quanh (cx, cy)."""
    c, sn = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    out = []
    for x, y in pts:
        x, y = (x - cx) * s, (y - cy) * s
        out.append((cx + dx + x * c - y * sn, cy + dy + x * sn + y * c))
    return out


def dilate(mask, r_px):
    """Noi rong mat na tron deu r_px diem anh (lam mem + cat nguong)."""
    b = mask.filter(ImageFilter.GaussianBlur(max(0.5, r_px / 2.0)))
    return b.point(lambda v: 255 if v > 6 else 0)


def solid(color, mask, opacity=1.0):
    a = mask if opacity >= 1.0 else mask.point(lambda v: int(v * opacity))
    im = Image.new('RGBA', mask.size, color + (0,))
    im.putalpha(a)
    return im


# ----------------------------------------------------------------- Glyph
class Glyph:
    def __init__(self, n):
        self.n = n
        self.w = n * SS
        self.k = self.w / D
        self.layers = {}

    def layer(self, name):
        if name not in self.layers:
            self.layers[name] = Image.new('L', (self.w, self.w), 0)
        return self.layers[name]

    def _dr(self, name):
        return ImageDraw.Draw(self.layer(name))

    def P(self, pts):
        return [(x * self.k, y * self.k) for x, y in pts]

    def poly(self, name, pts, v=255):
        self._dr(name).polygon(self.P(pts), fill=v)

    def ellipse(self, name, cx, cy, rx, ry=None, v=255):
        ry = rx if ry is None else ry
        k = self.k
        self._dr(name).ellipse([(cx - rx) * k, (cy - ry) * k, (cx + rx) * k, (cy + ry) * k], fill=v)

    def rrect(self, name, x0, y0, x1, y1, r, v=255):
        k = self.k
        self._dr(name).rounded_rectangle([x0 * k, y0 * k, x1 * k, y1 * k], radius=r * k, fill=v)

    def stroke(self, name, pts, w, v=255):
        k = self.k
        d = self._dr(name)
        d.line(self.P(pts), fill=v, width=max(1, int(round(w * k))), joint='curve')
        for (x, y) in (pts[0], pts[-1]):
            d.ellipse([(x - w / 2) * k, (y - w / 2) * k, (x + w / 2) * k, (y + w / 2) * k], fill=v)

    # ---- ghep lop
    def _fill(self, mask, style):
        top, bot = STYLES[style]
        bb = mask.getbbox()
        w = self.w
        y0, y1 = (bb[1], bb[3]) if bb else (0, w)
        t = np.clip((np.arange(w, dtype=np.float32) - y0) / max(1, (y1 - y0)), 0, 1)[:, None, None]
        base = np.array(top, np.float32) * (1 - t) + np.array(bot, np.float32) * t
        base = np.repeat(base, w, axis=1)
        d = max(1, int(round(0.9 * self.k)))
        soft = ImageFilter.GaussianBlur(0.30 * self.k)
        light = np.asarray(ImageChops.subtract(mask, ImageChops.offset(mask, d, d)).filter(soft), np.float32) / 255.0
        dark = np.asarray(ImageChops.subtract(mask, ImageChops.offset(mask, -d, -d)).filter(soft), np.float32) / 255.0
        rgb = base * (1 - 0.55 * light[..., None]) + 255.0 * 0.55 * light[..., None]
        rgb = rgb * (1 - 0.26 * dark[..., None])
        m = np.asarray(mask, np.float32)
        out = np.dstack([np.clip(rgb, 0, 255), m]).astype(np.uint8)
        return Image.fromarray(out, 'RGBA')

    def render(self, fills, details=(), outline=2.6, shadow=True):
        """fills: [(ten_lop, style, vien_rieng_don_vi)], details: [(ten_lop, (r,g,b))]"""
        w, k = self.w, self.k
        union = Image.new('L', (w, w), 0)
        for f in fills:
            union = ImageChops.lighter(union, self.layer(f[0]))
        canvas = Image.new('RGBA', (w, w), (0, 0, 0, 0))
        if shadow:
            sh = dilate(union, outline * k).filter(ImageFilter.GaussianBlur(1.5 * k))
            sh = ImageChops.offset(sh, 0, int(2.0 * k)).point(lambda v: int(v * 0.55))
            canvas = Image.alpha_composite(canvas, solid(NAVY, sh))
        canvas = Image.alpha_composite(canvas, solid(NAVY, dilate(union, outline * k)))
        for f in fills:
            name, style = f[0], f[1]
            edge = f[2] if len(f) > 2 else 0
            m = self.layer(name)
            if edge > 0:
                canvas = Image.alpha_composite(canvas, solid(NAVY, dilate(m, edge * k)))
            canvas = Image.alpha_composite(canvas, self._fill(m, style))
        for name, col in details:
            m = ImageChops.darker(self.layer(name), union)
            canvas = Image.alpha_composite(canvas, solid(col, m))
        return canvas.resize((self.n, self.n), Image.LANCZOS)


# ----------------------------------------------------------------- vu khi dung chung
def sword(g, tag, rot, s=1.0, dx=0.0, dy=0.0):
    T = lambda pts: xf(pts, rot, s, 64, 64, dx, dy)
    edge = bez((58, 88), (58, 62), (64, 34), (78, 4))
    spine = bez((78, 4), (84, 34), (77, 64), (70, 88))
    g.poly('blade_' + tag, T(edge + spine))
    g.stroke('ridge_' + tag, T(bez((64, 86), (65, 62), (69, 38), (77, 12))), 1.6 * s)
    g.poly('guard_' + tag, T([(42, 86), (86, 86), (88, 93), (76, 96), (52, 96), (40, 93)]))
    g.poly('grip_' + tag, T([(60, 96), (68, 96), (68, 114), (60, 114)]))
    g.poly('guard_' + tag, T(arc(64, 118, 6.0, 0, 360, 24)))


# ----------------------------------------------------------------- bo icon menu
def icon_bag(n):
    """Tui deo cheo: than tui, nap cong, day deo cheo qua vai, khoa tron."""
    g = Glyph(n)
    g.stroke('strap', bez((34, 40), (30, 12), (92, 4), (98, 44)), 7.0)
    g.poly('body', [(20, 50), (108, 50), (114, 108), (14, 108)])
    g.rrect('body', 14, 98, 114, 114, 8)
    flap = [(18, 44), (110, 44), (104, 76)] + bez((104, 76), (84, 98), (44, 98), (24, 76)) + [(18, 44)]
    g.poly('flap', flap)
    g.stroke('seam', bez((24, 76), (44, 98), (84, 98), (104, 76)), 2.8)
    g.ellipse('clasp', 64, 92, 8.5)
    g.ellipse('dot', 64, 92, 2.6)
    return g.render([('strap', 'wood'), ('body', 'steel'), ('flap', 'steel'), ('clasp', 'beacon', 1.2)],
                    [('seam', NAVY), ('dot', NAVY)])


def icon_character(n):
    g = Glyph(n)
    g.ellipse('head', 64, 38, 20)
    g.poly('torso', arc(64, 110, 42, 180, 360, 60) + [(106, 116), (22, 116)])
    g.stroke('collar', [(46, 78), (64, 98), (82, 78)], 3.6)
    return g.render([('head', 'steel'), ('torso', 'steel')], [('collar', NAVY)])


def icon_quest(n):
    """Cuon giay hai dau cuon tron, day buoc o giua, dau cham than vang."""
    g = Glyph(n)
    T = lambda pts: xf(pts, -18, 1.0, 64, 64)
    g.poly('paper', T([(28, 30), (100, 30), (100, 98), (28, 98)]))
    g.poly('roll', T(arc(28, 64, 7, 90, 270, 24) + [(30, 57), (30, 71)]))
    for x in (22, 106):
        g.poly('roll', T([(x - 9, 24), (x + 9, 24), (x + 9, 104), (x - 9, 104)]))
        g.poly('roll', T(arc(x, 24, 9, 180, 360, 20) + arc(x, 104, 9, 0, 180, 20)))
    g.poly('tie', T([(79, 22), (89, 22), (89, 106), (79, 106)]))
    g.poly('tie', T([(80, 106), (88, 106), (93, 121), (76, 121)]))
    g.rrect('mark', 47, 42, 55, 68, 4)
    g.ellipse('mark', 51, 81, 5.0)
    return g.render([('paper', 'steel'), ('roll', 'steel'), ('tie', 'wood'), ('mark', 'rust', 0.4)])


def tower_x(y):
    t = (y - 56) / 56.0
    return 50 - 10 * t, 78 + 10 * t


def icon_floor(n):
    g = Glyph(n)
    g.poly('tower', [(tower_x(56)[0], 56), (tower_x(56)[1], 56), (tower_x(112)[1], 112), (tower_x(112)[0], 112)])
    for y0, y1 in ((68, 80), (92, 104)):
        g.poly('band', [(tower_x(y0)[0], y0), (tower_x(y0)[1], y0), (tower_x(y1)[1], y1), (tower_x(y1)[0], y1)])
    g.rrect('gal', 40, 47, 88, 58, 3)
    g.rrect('lamp', 54, 29, 74, 48, 2)
    g.stroke('lampline', [(64, 29), (64, 48)], 2.2)
    g.stroke('lampline', [(54, 38.5), (74, 38.5)], 2.2)
    g.poly('roof', [(49, 31), (64, 14), (79, 31)])
    g.ellipse('roof', 64, 11.5, 3.4)
    g.rrect('base', 32, 108, 96, 118, 3)
    g.poly('beam', [(46, 34), (46, 44), (10, 52), (10, 26)])
    g.poly('beam', [(82, 34), (82, 44), (118, 52), (118, 26)])
    return g.render([('beam', 'beacon'), ('tower', 'steel'), ('band', 'rust'), ('gal', 'steel'),
                     ('base', 'steel'), ('roof', 'steel'), ('lamp', 'beacon', 1.0)], [('lampline', NAVY)])


def icon_pause(n):
    g = Glyph(n)
    g.rrect('bar', 32, 22, 56, 106, 9)
    g.rrect('bar', 72, 22, 96, 106, 9)
    return g.render([('bar', 'steel')])


def icon_talk(n):
    g = Glyph(n)
    g.rrect('b', 12, 20, 116, 88, 26)
    g.poly('b', [(30, 80), (26, 112), (64, 86)])
    for x in (40, 64, 88):
        g.ellipse('dots', x, 54, 6.8)
    return g.render([('b', 'steel')], [('dots', NAVY)])


def icon_settings(n):
    g = Glyph(n)
    g.ellipse('gear', 64, 64, 34)
    for i in range(8):
        th = math.radians(i * 45)
        c, s = math.cos(th), math.sin(th)
        loc = [(30, -10.5), (53, -7.5), (53, 7.5), (30, 10.5)]
        g.poly('gear', [(64 + u * c - v * s, 64 + u * s + v * c) for u, v in loc])
    g.ellipse('hole', 64, 64, 14)
    return g.render([('gear', 'steel')], [('hole', NAVY)])


def icon_close(n):
    g = Glyph(n)
    g.stroke('x', [(30, 30), (98, 98)], 16)
    g.stroke('x', [(98, 30), (30, 98)], 16)
    return g.render([('x', 'steel')])


# ----------------------------------------------------------------- bo icon chien dau
def icon_attack(n):
    g = Glyph(n)
    g.ellipse('slash', 62, 66, 56)
    g.ellipse('slash', 76, 52, 56, v=0)
    sword(g, 'a', 40, 0.92, 4, 0)
    return g.render([('slash', 'rust'), ('blade_a', 'steel'), ('guard_a', 'brass'), ('grip_a', 'wood')],
                    [('ridge_a', (122, 144, 172))])


def icon_dodge(n):
    g = Glyph(n)
    g.stroke('chev', [(66, 30), (96, 64), (66, 98)], 15)
    g.stroke('chev', [(34, 30), (64, 64), (34, 98)], 15)
    g.stroke('trail', [(8, 46), (24, 46)], 6)
    g.stroke('trail', [(2, 64), (22, 64)], 6)
    g.stroke('trail', [(8, 82), (24, 82)], 6)
    return g.render([('trail', 'teal'), ('chev', 'steel')])


def icon_core(n):
    g = Glyph(n)
    rot = 45
    T = lambda pts: xf(pts, rot, 1.14, 64, 64, 0, 2)
    g.stroke('speed', T([(36, 66), (36, 98)]), 5.5)
    g.stroke('speed', T([(92, 72), (92, 106)]), 5.5)
    g.poly('shaft', T([(60, 40), (68, 40), (68, 118), (60, 118)]))
    g.poly('head', T([(64, 0), (82, 30), (74, 44), (54, 44), (46, 30)]))
    g.stroke('rib', T([(64, 9), (64, 40)]), 2.2)
    g.poly('collar', T([(54, 42), (74, 42), (74, 52), (54, 52)]))
    g.poly('collar', T(arc(64, 121, 5.2, 0, 360, 20)))
    return g.render([('speed', 'mist'), ('shaft', 'wood'), ('head', 'steel'), ('collar', 'brass')],
                    [('rib', (122, 144, 172))])


def icon_traverse(n):
    g = Glyph(n)
    g.stroke('ring', arc(64, 20, 8.5, 0, 360, 36), 5.2)
    g.rrect('shaft', 60.5, 28, 67.5, 108, 3)
    g.rrect('stock', 38, 40, 90, 48, 4)
    band = arc(64, 74, 42, 12, 168, 60) + arc(64, 74, 34, 168, 12, 60)
    g.poly('arms', band)
    for a in (12, 168):
        sgn = 1 if a == 12 else -1
        base = (64 + 38 * math.cos(math.radians(a)), 74 + 38 * math.sin(math.radians(a)))
        g.poly('arms', [(base[0] - sgn * 5, base[1] - 6), (base[0] + sgn * 11, base[1] - 24), (base[0] + sgn * 12, base[1] + 3), (base[0] + sgn * 3, base[1] + 8)])
    g.ellipse('arms', 64, 112, 9, 5)
    return g.render([('ring', 'steel'), ('shaft', 'steel'), ('stock', 'steel'), ('arms', 'steel')])


def icon_ultimate(n):
    g = Glyph(n)
    sword(g, 'l', -36, 1.0, 0, 2)
    sword(g, 'r', 36, 1.0, 0, 2)
    cx, cy, ro, ri = 64, 62, 21, 6.2
    star = []
    for i in range(8):
        a = math.radians(i * 45 - 90)
        r = ro if i % 2 == 0 else ri
        star.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    g.poly('star', star)
    return g.render([('blade_l', 'steel'), ('blade_r', 'steel'),
                     ('guard_l', 'brass'), ('guard_r', 'brass'), ('grip_l', 'wood'), ('grip_r', 'wood'),
                     ('star', 'beacon', 1.2)],
                    [('ridge_l', (122, 144, 172)), ('ridge_r', (122, 144, 172))])


# ----------------------------------------------------------------- vong nut ky nang
RING_COLORS = {
    'L': ((255, 244, 190), (196, 138, 36)),     # vang dong: nut Danh
    'M': ((250, 252, 255), (104, 128, 156)),    # bac
    'S': ((240, 246, 252), (110, 132, 158)),
}


def make_ring(n, kind):
    w = n * SS
    c = (w - 1) / 2.0
    yy, xx = np.mgrid[0:w, 0:w].astype(np.float32)
    r = np.sqrt((xx - c) ** 2 + (yy - c) ** 2)
    ang = np.degrees(np.arctan2(yy - c, xx - c))
    R = w * 0.5 - w * 0.04
    rim = {'L': 0.052, 'M': 0.046, 'S': 0.042}[kind] * w
    band = lambda a, b: ((r >= a) & (r <= b)).astype(np.float32)

    def layer(rgb, alpha):
        arr = np.zeros((w, w, 4), np.float32)
        arr[..., :3] = rgb
        arr[..., 3] = alpha * 255.0
        return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), 'RGBA')

    comp = Image.new('RGBA', (w, w), (0, 0, 0, 0))
    disc = (r <= R + 0.012 * w).astype(np.float32)
    sh = Image.fromarray((disc * 255).astype(np.uint8), 'L').filter(ImageFilter.GaussianBlur(0.03 * w))
    sh = ImageChops.offset(sh, 0, int(0.022 * w)).point(lambda v: int(v * 0.55))
    comp = Image.alpha_composite(comp, solid(NAVY, sh))
    # than nut: xanh than dam, sang hon o giua
    t = np.clip(r / R, 0, 1)[..., None]
    body = np.array((24, 50, 78), np.float32) * (1 - t) + np.array((9, 19, 34), np.float32) * t
    comp = Image.alpha_composite(comp, layer(body, (r <= R).astype(np.float32) * 0.90))
    # vanh sang theo duong cheo (sang tren-trai, toi duoi-phai)
    u = np.clip((xx + yy) / (2.0 * w), 0, 1)[..., None]
    lt, dk = RING_COLORS[kind]
    rimc = np.array(lt, np.float32) * (1 - u) + np.array(dk, np.float32) * u
    comp = Image.alpha_composite(comp, layer(NAVY, band(R, R + 0.014 * w)))
    comp = Image.alpha_composite(comp, layer(rimc, band(R - rim, R)))
    comp = Image.alpha_composite(comp, layer(NAVY, band(R - rim - 0.011 * w, R - rim) * 0.9))
    # vanh mong ben trong
    ri = 0.80 * R
    comp = Image.alpha_composite(comp, layer(rimc, band(ri, ri + 0.010 * w) * 0.55))
    # vet sang tren-trai
    fade = np.clip(1 - np.abs(((ang - 225 + 180) % 360) - 180) / 38.0, 0, 1)
    comp = Image.alpha_composite(comp, layer((255, 255, 255), band(0.885 * R, 0.915 * R) * fade * 0.45))
    if kind != 'S':   # 4 diem danh dau Bac/Dong/Nam/Tay tren vanh mong
        for a in (0, 90, 180, 270):
            ca, sa = math.cos(math.radians(a)), math.sin(math.radians(a))
            px, py = c + ri * ca, c + ri * sa
            m = ((np.abs((xx - px) * ca + (yy - py) * sa) < 0.012 * w) & (np.abs(-(xx - px) * sa + (yy - py) * ca) < 0.022 * w)).astype(np.float32)
            comp = Image.alpha_composite(comp, layer(rimc, m * 0.9))
    return comp.resize((n, n), Image.LANCZOS)


# ----------------------------------------------------------------- chay
MENU = {
    'Glyph_Bag': icon_bag, 'Glyph_Character': icon_character, 'Glyph_Quest': icon_quest,
    'Glyph_Floor': icon_floor, 'Glyph_Pause': icon_pause, 'Glyph_Talk': icon_talk,
    'Glyph_Settings': icon_settings, 'Glyph_Close': icon_close,
}
COMBAT = {
    'Glyph_Attack': icon_attack, 'Glyph_Dodge': icon_dodge, 'Glyph_Core': icon_core,
    'Glyph_Traverse': icon_traverse, 'Glyph_Ultimate': icon_ultimate,
}
RINGS = {'Ring_L': ('L', 2), 'Ring_M': ('M', 1.5), 'Ring_S': ('S', 1.25)}   # (loai, he so x so voi 192)


def build_all(n=192):
    out = {}
    for name, fn in {**MENU, **COMBAT}.items():
        out[name] = fn(n)
    for name, (kind, mul) in RINGS.items():
        out[name] = make_ring(int(n * mul), kind)
    return out


if __name__ == '__main__':
    dest = sys.argv[1] if len(sys.argv) > 1 else 'out_glyph'
    size = int(sys.argv[2]) if len(sys.argv) > 2 else 192
    os.makedirs(dest, exist_ok=True)
    for name, im in build_all(size).items():
        im.save(os.path.join(dest, name + '.png'))
        print('ok', name, im.size)
