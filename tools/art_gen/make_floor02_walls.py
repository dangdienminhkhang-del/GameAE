"""Lô B: gạch viền tường Tầng 2 - mặt trên tường (tối) và sàn có bóng đổ dưới chân tường.
Chạy: python make_floor02_walls.py  -> out_batchB/Tiles_Floor02_Walls.png (4 ô 32x32)
Cần make_floor02_batch.py (dùng lại bảng màu và gạch sàn) cùng thư mục.
"""
import os
import numpy as np
from PIL import Image
import make_floor02_batch as A
from make_floor02_batch import col, blank, forge_floor
from pixelkit import strip, PALETTE, hexc

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out_batchB')
os.makedirs(OUT, exist_ok=True)


def wall_top(variant, seed=0):
    """Mặt trên tường: tấm thép tối, mối hàn, đinh tán thưa; bản B có ống nằm ngang (liền mạch theo chiều ngang)."""
    r = np.random.RandomState(seed + 31 * variant)
    img = blank(32, 32)
    for y in range(32):
        for x in range(32):
            t = 1
            if y % 16 == 0 or x % 16 == 0:
                t = 0
            elif y % 16 == 1 or x % 16 == 1:
                t = 2
            img[y, x] = col('forge', t)
    for (cx, cy) in ((4, 4), (20, 4), (4, 20), (20, 20)):
        img[cy, cx] = col('pipe', 3)
        img[cy + 1, cx + 1] = col('forge', 0)
    for _ in range(6):                          # vết bẩn
        x, y = r.randint(2, 30), r.randint(2, 30)
        img[y, x] = col('forge', 0)
    if variant == 1:                            # ống dẫn nằm ngang
        for y in range(11, 20):
            t = {11: 0, 12: 4, 13: 3, 14: 3, 15: 2, 16: 2, 17: 1, 18: 1, 19: 0}[y]
            for x in range(32):
                img[y, x] = col('pipe', t)
        for fx in (6, 22):                      # mặt bích đồng
            for y in range(10, 21):
                img[y, fx] = col('brass', 3 if y not in (10, 20) else 1)
                img[y, fx + 1] = col('brass', 2)
    return Image.fromarray(img, 'RGBA')


def floor_shadow(variant, seed):
    """Sàn có bóng đổ ở mép trên (nơi tường ở phía bắc)."""
    base = np.array(forge_floor(variant, seed)).astype(np.float32)
    for y in range(32):
        k = max(0.0, 1.0 - y / 9.0)            # 1 ở mép trên -> 0 sau 9 hàng
        f = 1.0 - 0.62 * k
        base[y, :, :3] *= f
    return Image.fromarray(np.clip(base, 0, 255).astype(np.uint8), 'RGBA')


if __name__ == '__main__':
    tiles = [wall_top(0, 5), wall_top(1, 6), floor_shadow(0, 1), floor_shadow(1, 3)]
    strip(tiles).save(os.path.join(OUT, 'Tiles_Floor02_Walls.png'))
    print(os.listdir(OUT))
