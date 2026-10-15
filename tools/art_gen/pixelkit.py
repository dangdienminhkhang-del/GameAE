"""pixelkit - bộ công cụ vẽ pixel art bằng code cho Game2D (Art Contract A0, docs/14).

Ý tưởng: vẽ hình theo "vật liệu" (da, áo, kim loại...), sau đó tự động:
  1) tô 3 tông sáng/trung/tối theo hướng sáng từ trên-trái,
  2) kẻ viền tối quanh hình.
Mọi màu lấy từ PALETTE (khớp docs/14 §3) nên không bao giờ lệch bảng màu.
"""
import numpy as np
from PIL import Image


def hexc(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


# Mỗi dải gồm 5 màu: 0 viền/tối nhất, 1 bóng, 2 màu gốc, 3 sáng, 4 điểm sáng.
PALETTE = {
    'skin':   ['#3f2a24', '#7a4e3c', '#b98163', '#dba883', '#f2cfae'],
    'hair':   ['#3a404c', '#6b7686', '#aab4c2', '#d3dae3', '#f1f4f8'],   # xám muối
    'coat':   ['#0f2c33', '#1b4a52', '#2a6a6a', '#418d86', '#6bb3a6'],   # áo mưa xanh than-lục
    'sleeve': ['#12353d', '#24636a', '#37878a', '#58aba3', '#8fd4c3'],
    'coat_far': ['#0b2026', '#143a41', '#1f5559', '#2f7573', '#4d9a90'],
    'rust':   ['#33150f', '#66261a', '#97402b', '#c06848', '#df9570'],   # đỏ gỉ
    'rope':   ['#3b2c19', '#6e5530', '#a88a55', '#cdb27c', '#ecdba8'],
    'beacon': ['#5a3f0c', '#a87a18', '#e9b82c', '#f7d65e', '#fff2b0'],   # vàng beacon
    'pants':  ['#171c27', '#2a3242', '#414c61', '#5d6b85', '#8190aa'],
    'boot':   ['#1c120d', '#3a271b', '#5b3e2a', '#7d5a3f', '#a07b58'],
    'steel':  ['#262c36', '#4a5361', '#7d8898', '#b3bdca', '#e6ecf3'],
    'blade':  ['#2a2f38', '#575f6b', '#8d97a5', '#c2cad5', '#f0f4f8'],
    'eye':    ['#0b1220', '#0b1220', '#0b1220', '#1d2b4a', '#2c4068'],
    'white':  ['#9aa5b5', '#c9d1dc', '#f0f3f7', '#ffffff', '#ffffff'],
    'slime':  ['#2c5e5a', '#6fb5a6', '#a5dccb', '#cbf0e2', '#f1fff9'],   # kính biển, sáng để màu báo đòn vẫn đọc được
    'wood':   ['#241a12', '#3a2a1c', '#55402a', '#6f5539', '#8d7050'],
    'wood2':  ['#2a1f16', '#43321f', '#5f482f', '#7a5e3f', '#99794f'],
    'nail':   ['#15110e', '#2a2622', '#5a554e', '#8a8478', '#b8b2a4'],
    'stone':  ['#171b24', '#262c38', '#363e4e', '#4a5568', '#61708a'],
    'stone2': ['#1a1e28', '#2a303d', '#3b4354', '#505c72', '#67768f'],
    'water':  ['#0a1b27', '#0f2a3a', '#173d52', '#245672', '#357490'],   # tối, ít bão hòa: nhường xanh nhạt cho "punish"
    'foam':   ['#1c3a4a', '#35607a', '#5a8aa3', '#86b0c4', '#b4d2df'],
    'iron':   ['#14181f', '#252b35', '#3a4250', '#566073', '#7c889d'],
    'glow':   ['#a87a18', '#e9b82c', '#f7d65e', '#fff2b0', '#ffffff'],
}
OUTLINE_FALLBACK = '#0b1220'


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.mat = np.zeros((h, w), dtype=np.int16)     # 0 = trống
        self.part = np.zeros((h, w), dtype=np.int16)
        self.names = ['']
        self.pid = 0

    def _m(self, name):
        if name not in self.names:
            self.names.append(name)
        return self.names.index(name)

    def _put(self, x, y, m, p):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.mat[y, x] = m
            self.part[y, x] = p

    def newpart(self):
        self.pid += 1
        return self.pid

    def px(self, x, y, name, part=None):
        self._put(int(x), int(y), self._m(name), self.newpart() if part is None else part)

    def rect(self, x0, y0, x1, y1, name):
        p = self.newpart(); m = self._m(name)
        for y in range(int(y0), int(y1)):
            for x in range(int(x0), int(x1)):
                self._put(x, y, m, p)
        return p

    def ellipse(self, cx, cy, rx, ry, name):
        p = self.newpart(); m = self._m(name)
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                    self._put(x, y, m, p)
        return p

    def poly(self, pts, name):
        p = self.newpart(); m = self._m(name)
        ys = [q[1] for q in pts]
        for y in range(int(min(ys)), int(max(ys)) + 1):
            xs = []
            n = len(pts)
            for i in range(n):
                (xa, ya), (xb, yb) = pts[i], pts[(i + 1) % n]
                if (ya <= y + 0.5 < yb) or (yb <= y + 0.5 < ya):
                    xs.append(xa + (y + 0.5 - ya) * (xb - xa) / (yb - ya))
            xs.sort()
            for i in range(0, len(xs) - 1, 2):
                for x in range(int(round(xs[i])), int(round(xs[i + 1]))):
                    self._put(x, y, m, p)
        return p

    def line(self, x0, y0, x1, y1, name, th=1, part=None):
        p = self.newpart() if part is None else part; m = self._m(name)
        steps = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
        for i in range(steps + 1):
            t = i / steps
            x = x0 + (x1 - x0) * t
            y = y0 + (y1 - y0) * t
            for dy in range(th):
                for dx in range(th):
                    self._put(int(x) + dx - (th - 1) // 2, int(y) + dy - (th - 1) // 2, m, p)
        return p

    # ---------- hậu kỳ ----------
    def render(self, outline=True, light=(-0.6, -0.8), volume=True):
        h, w = self.mat.shape
        out = np.zeros((h, w, 4), dtype=np.uint8)
        # trọng tâm từng part cho tô khối
        info = {}
        for p in np.unique(self.part):
            if p == 0:
                continue
            ys, xs = np.nonzero(self.part == p)
            info[p] = (xs.mean(), ys.mean(), max(1.0, (xs.max() - xs.min() + 1) / 2), max(1.0, (ys.max() - ys.min() + 1) / 2))
        for y in range(h):
            for x in range(w):
                m = self.mat[y, x]
                if m == 0:
                    continue
                ramp = PALETTE[self.names[m]]
                p = self.part[y, x]

                def diff(xx, yy):
                    if not (0 <= xx < w and 0 <= yy < h):
                        return True
                    return self.part[yy, xx] != p
                lit = diff(x, y - 1) or diff(x - 1, y)
                shd = diff(x, y + 1) or diff(x + 1, y)
                idx = 2
                if volume:
                    cx, cy, rx, ry = info[p]
                    t = ((x + 0.5 - cx) / rx) * light[0] + ((y + 0.5 - cy) / ry) * light[1]
                    if t > 0.55:
                        idx = 3
                    elif t < -0.55:
                        idx = 1
                if lit and not shd:
                    idx = min(4, idx + 1)
                elif shd and not lit:
                    idx = max(1, idx - 1)
                out[y, x] = (*hexc(ramp[idx]), 255)
        if outline:
            res = out.copy()
            for y in range(h):
                for x in range(w):
                    if self.mat[y, x] != 0:
                        continue
                    best = None
                    for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
                        xx, yy = x + dx, y + dy
                        if 0 <= xx < w and 0 <= yy < h and self.mat[yy, xx] != 0:
                            best = PALETTE[self.names[self.mat[yy, xx]]][0]
                            break
                    if best:
                        res[y, x] = (*hexc(best), 255)
            out = res
        return Image.fromarray(out, 'RGBA')


def strip(frames):
    """Ghép các khung thành một dải ngang (chuẩn nhập Unity: Multiple, lưới)."""
    w = sum(f.width for f in frames)
    h = max(f.height for f in frames)
    s = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    x = 0
    for f in frames:
        s.paste(f, (x, 0))
        x += f.width
    return s


def up(img, k=8, bg=None):
    big = img.resize((img.width * k, img.height * k), Image.NEAREST)
    if bg:
        base = Image.new('RGBA', big.size, bg)
        base.paste(big, (0, 0), big)
        return base
    return big
