#!/usr/bin/env python3
"""
Sinh gói âm thanh PLACEHOLDER cho Game2D bằng tổng hợp thuần code (numpy + scipy).
Không dùng sample, không lấy từ nguồn ngoài: mọi file đều do script này tạo ra và thuộc dự án.
Thay bằng âm thanh thật ở Art Track; tên file khớp id trong AudioIds (sfx.player_swing -> sfx_player_swing.wav).

Chạy:  python3 synth_placeholder_audio.py <thư mục Assets/_Game/Audio>
Cần:   numpy, scipy, và ffmpeg (có libvorbis) để mã hóa nhạc sang OGG.
Kết quả: <out>/Sfx/*.wav (mono, 44100 Hz, 16-bit) và <out>/Music/*.ogg (stereo, 44100 Hz, loop liền mạch).
"""
import os
import subprocess
import sys
import tempfile

import numpy as np
from scipy import signal
from scipy.io import wavfile

SR = 44100
RNG = np.random.default_rng(20261014)  # cố định để lần chạy nào cũng ra cùng kết quả


# ---------------------------------------------------------------- tiện ích cơ bản
def n_of(sec):
    return int(round(sec * SR))


def tarr(sec):
    return np.arange(n_of(sec)) / SR


def midi_hz(m):
    return 440.0 * 2.0 ** ((m - 69) / 12.0)


def noise(sec):
    return RNG.standard_normal(n_of(sec))


def env(n, attack=0.005, decay=0.15, curve=5.0):
    """Bao: lên nhanh tuyến tính rồi tắt dần theo hàm mũ."""
    a = max(1, int(attack * SR))
    e = np.exp(-curve * np.arange(n) / max(1, int(decay * SR)))
    e[:a] *= np.linspace(0, 1, a)
    return e


def fade(x, ms_in=2, ms_out=8):
    x = x.copy()
    a, b = int(ms_in * SR / 1000), int(ms_out * SR / 1000)
    if a > 0:
        x[:a] *= np.linspace(0, 1, a)
    if b > 0:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def sweep_sine(f0, f1, sec, expo=True):
    n = n_of(sec)
    f = np.geomspace(f0, f1, n) if expo else np.linspace(f0, f1, n)
    return np.sin(2 * np.pi * np.cumsum(f) / SR)


def lowpass(x, fc, order=4):
    sos = signal.butter(order, fc, "low", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def highpass(x, fc, order=4):
    sos = signal.butter(order, fc, "high", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def bandpass(x, lo, hi, order=3):
    sos = signal.butter(order, [lo, hi], "band", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def swept_band_noise(sec, f0, f1, q=2.5, block=256):
    """Nhiễu qua bộ lọc băng thông có tần số trung tâm quét từ f0 tới f1 (tiếng gió/vút)."""
    x = noise(sec)
    out = np.zeros_like(x)
    nb = int(np.ceil(len(x) / block))
    zi = None
    for i in range(nb):
        fc = f0 * (f1 / f0) ** (i / max(1, nb - 1))
        lo, hi = fc / (1 + 1 / q), min(fc * (1 + 1 / q), SR / 2 - 100)
        sos = signal.butter(2, [lo, hi], "band", fs=SR, output="sos")
        if zi is None or zi.shape[0] != sos.shape[0]:
            zi = np.zeros((sos.shape[0], 2))
        seg = x[i * block:(i + 1) * block]
        y, zi = signal.sosfilt(sos, seg, zi=zi)
        out[i * block:i * block + len(seg)] = y
    return out


def bell(freq, sec, decay=0.5, partials=((1, 1.0, 1.0), (2.76, 0.5, 0.6), (5.4, 0.25, 0.35), (8.93, 0.12, 0.2))):
    t = tarr(sec)
    y = np.zeros_like(t)
    for ratio, amp, dk in partials:
        y += amp * np.sin(2 * np.pi * freq * ratio * t) * np.exp(-t / (decay * dk))
    return y


def synth_ir(sec=0.9, tau=0.28, hp=200.0, lp=6000.0):
    n = n_of(sec)
    ir = RNG.standard_normal(n) * np.exp(-np.arange(n) / (tau * SR))
    ir = lowpass(highpass(ir, hp, 2), lp, 2)
    return ir / np.sqrt(np.sum(ir ** 2))


def reverb(x, wet=0.2, sec=0.9, tau=0.28):
    ir = synth_ir(sec, tau)
    wetsig = signal.fftconvolve(x, ir)[: len(x) + len(ir) - 1]
    dry = np.pad(x, (0, len(wetsig) - len(x)))
    return (1 - wet) * dry + wet * wetsig * (np.std(x) / (np.std(wetsig) + 1e-9))


def place(buf, x, start):
    end = min(len(buf), start + len(x))
    if end > start:
        buf[start:end] += x[: end - start]


def peak_norm(x, peak_db=-1.5):
    m = np.max(np.abs(x)) + 1e-9
    return x * (10 ** (peak_db / 20) / m)


def to_i16(x):
    return np.clip(x * 32767, -32768, 32767).astype(np.int16)


# ---------------------------------------------------------------- SFX
def sfx_player_swing():
    n = 0.20
    x = swept_band_noise(n, 500, 3200, q=1.6)
    x *= env(len(x), 0.012, 0.09, 4.5)
    x += 0.15 * lowpass(noise(n), 400) * env(n_of(n), 0.01, 0.05)
    return fade(x)


def sfx_hit_enemy():
    n = 0.24
    t = tarr(n)
    thud = sweep_sine(210, 55, n) * env(n_of(n), 0.001, 0.09, 5)
    click = highpass(noise(0.03), 2500) * env(n_of(0.03), 0.0005, 0.012, 5)
    metal = (np.sin(2 * np.pi * 1250 * t) + 0.6 * np.sin(2 * np.pi * 1870 * t) + 0.3 * np.sin(2 * np.pi * 3110 * t)) * np.exp(-t / 0.05)
    x = 0.9 * thud + 0.6 * np.pad(click, (0, n_of(n) - len(click))) + 0.22 * metal
    return fade(np.tanh(1.3 * x))


def sfx_hit_player():
    n = 0.34
    t = tarr(n)
    thud = sweep_sine(150, 42, n) * env(n_of(n), 0.001, 0.14, 5)
    crunch = lowpass(noise(n), 1800) * env(n_of(n), 0.001, 0.07, 6)
    buzz = np.sin(2 * np.pi * 95 * t) * np.exp(-t / 0.12) * np.sign(np.sin(2 * np.pi * 47 * t))
    x = 0.9 * thud + 0.6 * crunch + 0.2 * buzz
    return fade(np.tanh(1.8 * x))


def sfx_enemy_die():
    n = 0.55
    t = tarr(n)
    tone = sweep_sine(420, 70, n) * env(n_of(n), 0.003, 0.30, 4)
    hiss = lowpass(noise(n), 3000) * np.linspace(1, 0.15, n_of(n)) * env(n_of(n), 0.002, 0.25, 4)
    thump = sweep_sine(130, 45, 0.18) * env(n_of(0.18), 0.001, 0.08, 5)
    x = 0.55 * tone + 0.5 * hiss
    x[: len(thump)] += 0.7 * thump
    return fade(np.tanh(1.2 * x), 2, 15)


def sfx_technique():
    n = 0.55
    t = tarr(n)
    f = np.geomspace(260, 1900, n_of(n))
    ph = 2 * np.pi * np.cumsum(f) / SR
    saw = signal.sawtooth(ph) * 0.4 + np.sin(ph) * 0.6
    saw = lowpass(saw, 5000)
    ring = bell(1568, n, 0.4) * 0.35
    air = swept_band_noise(n, 800, 5000, q=1.2) * 0.35
    e = np.minimum(1, t / 0.04) * np.exp(-np.maximum(0, t - 0.3) / 0.09)
    x = (saw + ring + air) * e
    return fade(reverb(x * 0.9, wet=0.12, sec=0.5, tau=0.15)[: n_of(n)], 2, 20)


def sfx_dodge():
    n = 0.17
    x = highpass(swept_band_noise(n, 1500, 4800, q=1.0), 700)
    x *= env(len(x), 0.006, 0.07, 4.5)
    x += 0.15 * lowpass(noise(n), 500) * env(n_of(n), 0.005, 0.05)
    return fade(x * 0.9)


def sfx_pickup():
    notes = [(1318.5, 0.0), (1760.0, 0.075)]
    buf = np.zeros(n_of(0.38))
    for f, st in notes:
        b = bell(f, 0.3, 0.18, partials=((1, 1.0, 1.0), (2.0, 0.3, 0.5), (3.0, 0.1, 0.3)))
        b *= np.minimum(1, tarr(0.3) / 0.003)
        place(buf, fade(b, 0, 25), n_of(st))
    return fade(buf * 0.7, 1, 25)


def sfx_level_up():
    notes = [72, 76, 79, 84]  # C5 E5 G5 C6 (sol-fa chung, không thuộc giai điệu nào)
    buf = np.zeros(n_of(1.4))
    for i, m in enumerate(notes):
        b = bell(midi_hz(m), 0.9, 0.5)
        b *= np.minimum(1, tarr(0.9) / 0.004)
        place(buf, fade(b, 0, 40) * (0.55 + 0.1 * i), n_of(0.11 * i))
    sh = bandpass(noise(1.4), 4000, 9000) * env(n_of(1.4), 0.05, 0.5, 4) * 0.06
    buf += sh
    buf = reverb(buf, wet=0.22, sec=1.0, tau=0.35)[: n_of(1.4)]
    return fade(buf, 1, 60)


def sfx_ui_click():
    n = 0.06
    t = tarr(n)
    x = np.sin(2 * np.pi * 1900 * t) * np.exp(-t / 0.012) + 0.4 * highpass(noise(n), 3000) * np.exp(-t / 0.004)
    return fade(x * 0.8, 0.5, 8)


def sfx_ui_back():
    n = 0.07
    f = np.geomspace(1300, 850, n_of(n))
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-tarr(n) / 0.02)
    x += 0.25 * highpass(noise(n), 2500) * np.exp(-tarr(n) / 0.004)
    return fade(x * 0.75, 0.5, 10)


def sfx_potion():
    buf = np.zeros(n_of(0.55))
    starts = [0.0, 0.11, 0.2, 0.31]
    for i, st in enumerate(starts):
        d = 0.09
        f0 = 380 + 90 * i
        f1 = f0 * 1.9
        b = sweep_sine(f0, f1, d, expo=True) * env(n_of(d), 0.004, 0.05, 5)
        place(buf, b * (0.7 + 0.1 * i), n_of(st))
    gl = lowpass(noise(0.5), 1600) * env(n_of(0.5), 0.02, 0.2, 4) * 0.18
    buf[: len(gl)] += gl
    return fade(buf, 1, 30)


def sfx_boss_telegraph():
    n = 1.0
    t = tarr(n)
    f = np.geomspace(70, 140, n_of(n))
    ph = 2 * np.pi * np.cumsum(f) / SR
    low = np.sin(ph) + 0.4 * np.sin(2 * ph)
    trem = 0.6 + 0.4 * np.sin(2 * np.pi * (6 + 8 * t) * t)
    alarm = bandpass(signal.square(2 * np.pi * np.cumsum(np.geomspace(420, 900, n_of(n))) / SR), 380, 2200) * 0.25
    rise = swept_band_noise(n, 400, 4500, q=1.4) * (t / n) * 0.45
    e = np.minimum(1, t / 0.08) * np.minimum(1, (n - t) / 0.12)
    x = (low * trem * 0.7 + alarm * (t / n) + rise) * e
    return fade(np.tanh(1.1 * x), 5, 40)


SFX = {
    "sfx_player_swing": sfx_player_swing,
    "sfx_hit_enemy": sfx_hit_enemy,
    "sfx_hit_player": sfx_hit_player,
    "sfx_enemy_die": sfx_enemy_die,
    "sfx_technique": sfx_technique,
    "sfx_dodge": sfx_dodge,
    "sfx_pickup": sfx_pickup,
    "sfx_level_up": sfx_level_up,
    "sfx_ui_click": sfx_ui_click,
    "sfx_ui_back": sfx_ui_back,
    "sfx_potion": sfx_potion,
    "sfx_boss_telegraph": sfx_boss_telegraph,
    # id bổ sung ở P9.8 (khớp AudioIds sfx.enemy_shoot)
    "sfx_enemy_shoot": lambda: fade(
        np.tanh(1.2 * (
            0.7 * swept_band_noise(0.16, 2600, 700, q=1.4) * env(n_of(0.16), 0.002, 0.07, 5)
            + 0.5 * sweep_sine(520, 160, 0.16) * env(n_of(0.16), 0.002, 0.06, 5)
        )), 1, 10),
}


# ---------------------------------------------------------------- Nhạc (loop liền mạch)
def pluck(freq, sec, bright=0.6):
    t = tarr(sec)
    y = np.sin(2 * np.pi * freq * t) + bright * 0.5 * np.sin(2 * np.pi * 2 * freq * t) * np.exp(-t / 0.12) + 0.2 * np.sin(2 * np.pi * 3 * freq * t) * np.exp(-t / 0.06)
    return y * np.exp(-t / (sec * 0.35)) * np.minimum(1, t / 0.004)


def pad_voice(freq, sec, wave="tri"):
    t = tarr(sec)
    y = np.zeros_like(t)
    for det in (-0.07, 0.0, 0.08):  # lệch nhẹ cho dày
        f = freq * 2 ** (det / 12)
        ph = 2 * np.pi * f * t
        y += signal.sawtooth(ph, 0.5) if wave == "tri" else signal.sawtooth(ph)
    y = lowpass(y / 3, 1800 if wave == "tri" else 1400, 2)
    a, r = int(0.5 * SR), int(0.8 * SR)
    e = np.ones_like(t)
    e[:a] = np.linspace(0, 1, a)
    e[-r:] *= np.linspace(1, 0, r)
    return y * e


def kick(vol=1.0):
    n = 0.28
    return vol * sweep_sine(130, 44, n) * env(n_of(n), 0.001, 0.12, 5)


def hat(vol=1.0, sec=0.05):
    return vol * highpass(noise(sec), 6500) * env(n_of(sec), 0.0005, sec * 0.5, 5)


def clap(vol=1.0):
    n = 0.18
    x = bandpass(noise(n), 1200, 3800) * env(n_of(n), 0.001, 0.07, 5)
    return vol * x


def wind_bed(sec, lo=300, hi=1100, level=0.05):
    x = lowpass(noise(sec), hi, 2)
    x = highpass(x, lo, 2)
    t = tarr(sec)
    lfo = 0.6 + 0.4 * np.sin(2 * np.pi * (1 / 9.0) * t) * np.sin(2 * np.pi * (1 / 13.0) * t + 1.0)
    return x * lfo * level / (np.std(x) + 1e-9) * 0.2


def loop_fold(buf, n):
    """Gấp phần đuôi tràn vòng lại đầu để loop liền mạch."""
    out = buf[:n].copy()
    tail = buf[n:]
    if len(tail):
        out[: len(tail)] += tail[: n]
    return out


def circ_reverb(x, wet=0.2, sec=1.2, tau=0.4):
    n = len(x)
    ir = np.zeros(n)
    m = min(n, n_of(sec))
    ir[:m] = synth_ir(sec, tau)[:m]
    y = np.fft.irfft(np.fft.rfft(x) * np.fft.rfft(ir), n)
    y *= np.std(x) / (np.std(y) + 1e-9)
    return (1 - wet) * x + wet * y


def stereoize(x, haas_ms=11, mix=0.5):
    d = int(haas_ms * SR / 1000)
    r = np.roll(x, d)  # loop liền mạch nên roll vòng là hợp lệ
    left = (1 - mix) * x + mix * r
    right = (1 - mix) * r + mix * x
    return np.stack([left, right], axis=1)


def render_music(bpm, bars, chords, melody_scale, seed, style):
    rng = np.random.default_rng(seed)
    beat = 60.0 / bpm
    bar = 4 * beat
    total = bars * bar
    n = n_of(total)
    buf = np.zeros(n + n_of(4))
    step = beat / 2  # nốt tám

    for b in range(bars):
        root, triad = chords[(b // style["bars_per_chord"]) % len(chords)]
        t0 = n_of(b * bar)
        if b % style["bars_per_chord"] == 0:
            span = style["bars_per_chord"] * bar + 0.6
            for m in triad:
                place(buf, pad_voice(midi_hz(m), span, style["pad"]) * style["pad_vol"], t0)
            if style.get("drone"):
                place(buf, np.sin(2 * np.pi * midi_hz(style["drone"]) * tarr(span)) * style["drone_vol"] * np.minimum(1, tarr(span) / 1.0), t0)
        for s in range(8):
            ts = t0 + n_of(s * step)
            if style["bass"] and (s % style["bass_every"] == 0):
                bl = lowpass(pluck(midi_hz(root - 24), step * 1.6, 0.2), 400)
                place(buf, bl * style["bass_vol"], ts)
            if style["arp"] and rng.random() < style["arp_p"]:
                if rng.random() < 0.65:
                    note = triad[rng.integers(len(triad))] + 12
                else:
                    note = melody_scale[rng.integers(len(melody_scale))]
                place(buf, pluck(midi_hz(note), 0.5) * style["arp_vol"], ts)
        for k in style["kicks"]:
            place(buf, kick(style["kick_vol"]), t0 + n_of(k * beat))
        for c in style["claps"]:
            place(buf, clap(style["clap_vol"]), t0 + n_of(c * beat))
        if style["hat_vol"]:
            for s in range(8):
                place(buf, hat(style["hat_vol"] * (1.0 if s % 2 == 0 else 0.6)), t0 + n_of(s * step))
        if style.get("riser") and (b % 4 == 3):
            r = swept_band_noise(bar, 300, 6000, q=1.3) * np.linspace(0, 1, n_of(bar)) * style["riser"]
            place(buf, r, t0)

    x = loop_fold(buf, n)
    x += wind_bed(total, level=style["wind"])
    x = circ_reverb(x, wet=style["reverb"], sec=1.3, tau=0.45)
    x = np.tanh(style["drive"] * x) / np.tanh(style["drive"])
    x = highpass(x, 30, 2)
    # đưa RMS về khoảng -17 dBFS rồi giới hạn đỉnh
    x *= 10 ** (-17 / 20) / (np.sqrt(np.mean(x ** 2)) + 1e-9)
    x = np.tanh(x * 1.1) / 1.1
    x = peak_norm(x, -1.5)
    return stereoize(x, style["haas"], 0.45)


MUSIC = {
    "music_town": dict(
        bpm=72, bars=16, seed=11,
        chords=[(57, [57, 60, 64]), (53, [53, 57, 60]), (48, [48, 52, 55]), (55, [55, 59, 62])],  # Am F C G
        scale=[69, 72, 74, 76, 79, 81],
        style=dict(bars_per_chord=1, pad="tri", pad_vol=0.5, bass=True, bass_every=8, bass_vol=0.35,
                   arp=True, arp_p=0.38, arp_vol=0.28, kicks=[], claps=[], kick_vol=0, clap_vol=0, hat_vol=0.0,
                   wind=0.35, reverb=0.35, drive=1.1, haas=13),
    ),
    "music_gameplay": dict(
        bpm=96, bars=16, seed=23,
        chords=[(52, [52, 55, 59]), (48, [48, 52, 55]), (55, [55, 59, 62]), (50, [50, 54, 57])],  # Em C G D
        scale=[64, 67, 69, 71, 74, 76],
        style=dict(bars_per_chord=1, pad="tri", pad_vol=0.32, bass=True, bass_every=1, bass_vol=0.42,
                   arp=True, arp_p=0.5, arp_vol=0.22, kicks=[0, 2], claps=[], kick_vol=0.55, clap_vol=0, hat_vol=0.16,
                   wind=0.25, reverb=0.25, drive=1.3, haas=9),
    ),
    "music_boss": dict(
        bpm=128, bars=16, seed=37,
        chords=[(50, [50, 53, 57]), (46, [46, 50, 53]), (55, [55, 58, 62]), (57, [57, 61, 64])],  # Dm Bb Gm A
        scale=[62, 65, 67, 68, 70, 73],
        style=dict(bars_per_chord=1, pad="saw", pad_vol=0.3, bass=True, bass_every=1, bass_vol=0.5,
                   arp=True, arp_p=0.55, arp_vol=0.2, kicks=[0, 1, 2, 3], claps=[1, 3], kick_vol=0.7, clap_vol=0.35, hat_vol=0.2,
                   wind=0.2, reverb=0.2, drive=1.6, haas=7, drone=26, drone_vol=0.18, riser=0.3),
    ),
}


# ---------------------------------------------------------------- Ghi file
def write_wav(path, x):
    wavfile.write(path, SR, to_i16(x))


def write_ogg(path, stereo):
    with tempfile.TemporaryDirectory() as td:
        tmp = os.path.join(td, "m.wav")
        wavfile.write(tmp, SR, to_i16(stereo))
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", tmp, "-c:a", "libvorbis", "-q:a", "4", path], check=True)


def main(out_dir):
    sfx_dir = os.path.join(out_dir, "Sfx")
    mus_dir = os.path.join(out_dir, "Music")
    os.makedirs(sfx_dir, exist_ok=True)
    os.makedirs(mus_dir, exist_ok=True)
    for name, fn in SFX.items():
        x = np.asarray(fn(), dtype=np.float64)
        x = peak_norm(x, -3.0)
        write_wav(os.path.join(sfx_dir, name + ".wav"), x)
        print(f"{name}.wav  {len(x)/SR:.2f}s  peak={np.max(np.abs(x)):.2f}")
    for name, cfg in MUSIC.items():
        st = render_music(cfg["bpm"], cfg["bars"], cfg["chords"], cfg["scale"], cfg["seed"], cfg["style"])
        write_ogg(os.path.join(mus_dir, name + ".ogg"), st)
        print(f"{name}.ogg  {len(st)/SR:.1f}s")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print(__doc__)
        sys.exit(1)
    main(sys.argv[1])
