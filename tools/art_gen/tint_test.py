"""Kiểm tra màu báo đòn (docs/14 §2): nhân hình với từng màu trạng thái, đo độ khác hình gốc.
Chạy: python tint_test.py            (kiểm Slime mẫu + người chơi mẫu + boss tạm nếu có)
      python tint_test.py duong_dan.png ten   (kiểm một hình bất kỳ)
Quy tắc: quái/boss phải khác gốc >= 60 với mọi màu mà con đó dùng (bỏ cột 'Chết')."""
import os, sys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
TINTS = [
    ('Quái báo đòn', (1.00, 0.55, 0.55)),   # EnemyController.SetTelegraph
    ('Boss đòn thường', (1.00, 0.80, 0.20)),  # BossController.TelegraphState
    ('Punish', (0.70, 0.70, 1.00)),         # PunishWindowState
    ('Pressure', (0.75, 0.20, 1.00)),       # PressureTelegraphColor
    ('Cuồng nộ', (1.00, 0.50, 0.50)),       # BossController enrage
]
THRESHOLD = 60.0


def mean_rgb(im):
    a = np.asarray(im.convert('RGBA')).astype(np.float32)
    m = a[..., 3] > 0
    return a[m][:, :3].mean(axis=0)


def check(name, im):
    base = mean_rgb(im)
    print(name)
    for tn, t in TINTS:
        d = float(np.linalg.norm(base * np.array(t, dtype=np.float32) - base))
        print(f'   {tn:16s} khác gốc: {d:6.1f}  {"đạt" if d >= THRESHOLD else "KHÔNG ĐẠT"}')


if __name__ == '__main__':
    if len(sys.argv) >= 2:
        check(sys.argv[2] if len(sys.argv) > 2 else sys.argv[1], Image.open(sys.argv[1]))
    else:
        out = os.path.join(HERE, 'out')
        slime = os.path.join(out, 'Enemy_Slime_Idle.png')
        if os.path.exists(slime):
            check('Slime mẫu', Image.open(slime).crop((32, 0, 64, 32)))
        boss = os.path.join(HERE, '..', '..', 'Assets', '_Game', 'Art', 'Placeholder', 'Boss_AnchorEngine.png')
        if os.path.exists(boss):
            check('Boss tạm hiện tại', Image.open(boss))
