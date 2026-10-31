using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public static class ContentArt
{
    public const string Dir = "Assets/_Game/Art/Environment/Anime/World/Content/";

    public sealed class Cv
    {
        public readonly int w, h;
        public readonly Color[] p;
        public Cv(int w, int h) { this.w = w; this.h = h; p = new Color[w * h]; }
        public Color Get(int x, int y) { return (x < 0 || y < 0 || x >= w || y >= h) ? Color.clear : p[y * w + x]; }
        public void Set(int x, int y, Color c) { if (x < 0 || y < 0 || x >= w || y >= h) return; p[y * w + x] = c; }
        public void Mix(int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            a *= c.a;
            if (a <= 0f) return;
            int i = y * w + x;
            if (a >= 0.999f) { p[i] = new Color(c.r, c.g, c.b, 1f); return; }
            Color o = p[i];
            float k = o.a * (1f - a);
            float na = a + k;
            p[i] = new Color((c.r * a + o.r * k) / na, (c.g * a + o.g * k) / na, (c.b * a + o.b * k) / na, na);
        }
        public void Rect(int x0, int y0, int x1, int y1, Color c, float a = 1f)
        {
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Mix(x, y, c, a);
        }
        public void Ell(float cx, float cy, float rx, float ry, Color c, float a = 1f)
        {
            for (int y = (int)(cy - ry - 1); y <= (int)(cy + ry + 1); y++)
                for (int x = (int)(cx - rx - 1); x <= (int)(cx + rx + 1); x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Mix(x, y, c, a);
                }
        }
        public void Line(int x0, int y0, int x1, int y1, Color c, float a = 1f)
        {
            int n = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (n == 0) { Mix(x0, y0, c, a); return; }
            for (int i = 0; i <= n; i++) Mix(Mathf.RoundToInt(x0 + (x1 - x0) * (float)i / n), Mathf.RoundToInt(y0 + (y1 - y0) * (float)i / n), c, a);
        }
        public void Poly(List<Vector2> pts, Color c, float a = 1f)
        {
            float miny = 1e9f, maxy = -1e9f;
            foreach (var q in pts) { miny = Mathf.Min(miny, q.y); maxy = Mathf.Max(maxy, q.y); }
            var xs = new List<float>();
            for (int y = Mathf.FloorToInt(miny); y <= Mathf.CeilToInt(maxy); y++)
            {
                xs.Clear();
                float yy = y + 0.5f;
                for (int i = 0; i < pts.Count; i++)
                {
                    Vector2 p0 = pts[i], p1 = pts[(i + 1) % pts.Count];
                    if ((p0.y <= yy && yy < p1.y) || (p1.y <= yy && yy < p0.y))
                        xs.Add(p0.x + (yy - p0.y) * (p1.x - p0.x) / (p1.y - p0.y));
                }
                xs.Sort();
                for (int i = 0; i + 1 < xs.Count; i += 2)
                    for (int x = Mathf.RoundToInt(xs[i]); x < Mathf.RoundToInt(xs[i + 1]); x++) Mix(x, y, c, a);
            }
        }
    }

    static Color C(int rgb) { return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255); }
    static Color[] T(params int[] h) { var a = new Color[h.Length]; for (int i = 0; i < h.Length; i++) a[i] = C(h[i]); return a; }
    static float Hs(int a, int b, int s)
    {
        unchecked
        {
            uint h = (uint)a * 374761393u + (uint)b * 668265263u + (uint)s * 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }
    static List<Vector2> P(params float[] v)
    {
        var l = new List<Vector2>();
        for (int i = 0; i + 1 < v.Length; i += 2) l.Add(new Vector2(v[i], v[i + 1]));
        return l;
    }

    // tone sets, dark -> light
    static readonly Color[] STONE = T(0x4a3c40, 0x6e5e5c, 0x978676, 0xbba98f, 0xdeceb0);
    static readonly Color[] COOL = T(0x2f3342, 0x4b5068, 0x6d7390, 0x9aa0bc, 0xc4c9de);
    static readonly Color[] IRON = T(0x2d2a33, 0x4b4755, 0x6f6a7a, 0x948fa0);
    static readonly Color[] BASALT = T(0x1e1a24, 0x2d2a33, 0x4b4755, 0x6f6a7a, 0x948fa0);
    static readonly Color[] WOOD = T(0x3a2216, 0x5c3a22, 0x84552f, 0xa87a45);
    static readonly Color[] FIRE = T(0xc4361a, 0xf27a24, 0xffc040, 0xfff2a0);
    static readonly Color[] GOLD = T(0xb88a2e, 0xe8b84a, 0xffe08a);
    static readonly Color[] GREEN = T(0x2e5e2e, 0x4a8a38, 0x70b04c, 0x9fd16a);
    static readonly Color[] VIOLET = T(0x4a2a7a, 0x7a44c8, 0xb078ff, 0xe4c8ff);
    static readonly Color[] TEAL = T(0x1a6a78, 0x2fb0b8, 0x7de8e0, 0xd0fffa);
    static readonly Color[] CREAM = T(0x8a7a66, 0xb8a88c, 0xdeceb0, 0xf4ead2);

    static int Tone(float lit, int n) { return Mathf.Clamp((int)((lit + 0.45f) / 1.4f * n), 0, n - 1); }
    static void Ball(Cv c, float cx, float cy, float rx, float ry, Color[] t)
    {
        for (int y = (int)(cy - ry - 1); y <= (int)(cy + ry + 1); y++)
            for (int x = (int)(cx - rx - 1); x <= (int)(cx + rx + 1); x++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                float d2 = u * u + v * v;
                if (d2 > 1f) continue;
                float lit = -0.5f * u - 0.55f * v + 0.75f * Mathf.Sqrt(1f - d2);
                c.Set(x, y, t[Tone(lit, t.Length)]);
            }
    }
    static void Cyl(Cv c, int x0, int x1, int y0, int y1, Color[] t)
    {
        float mid = (x0 + x1 + 1) / 2f, hw = (x1 - x0 + 1) / 2f;
        for (int x = x0; x <= x1; x++)
        {
            float u = (x + 0.5f - mid) / hw;
            float lit = -0.55f * u + 0.7f * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
            Color col = t[Tone(lit, t.Length)];
            for (int y = y0; y <= y1; y++) c.Set(x, y, col);
        }
    }
    static void Trap(Cv c, float cx, int y0, int y1, float hw0, float hw1, Color[] t)
    {
        int n = t.Length;
        for (int y = y0; y <= y1; y++)
        {
            float k = (y1 == y0) ? 0f : (y - y0) / (float)(y1 - y0);
            float hw = Mathf.Lerp(hw0, hw1, k);
            for (int x = Mathf.FloorToInt(cx - hw); x < Mathf.CeilToInt(cx + hw); x++)
            {
                float u = (x + 0.5f - cx) / hw;
                int idx = u < -0.35f ? n - 2 : (u < 0.35f ? n - 3 : 1);
                if (y == y0) idx = n - 1;
                else if (y == y1) idx = 0;
                c.Set(x, y, t[idx]);
            }
        }
    }
    static void Thick(Cv c, int x0, int y0, int x1, int y1, Color col)
    {
        c.Line(x0, y0, x1, y1, col);
        c.Line(x0 + 1, y0, x1 + 1, y1, col);
    }
    static void Outline(Cv s)
    {
        Color ol = C(0x2b1b1f);
        var src = (Color[])s.p.Clone();
        for (int y = 0; y < s.h; y++)
            for (int x = 0; x < s.w; x++)
            {
                if (src[y * s.w + x].a > 0f) continue;
                bool near = (x > 0 && src[y * s.w + x - 1].a > 0f) || (x < s.w - 1 && src[y * s.w + x + 1].a > 0f)
                         || (y > 0 && src[(y - 1) * s.w + x].a > 0f) || (y < s.h - 1 && src[(y + 1) * s.w + x].a > 0f);
                if (near) s.Set(x, y, ol);
            }
    }
    static void Shadow(Cv s, float cx, float cy, float rx, float ry)
    {
        for (int y = (int)(cy - ry - 1); y <= (int)(cy + ry + 1); y++)
            for (int x = (int)(cx - rx - 1); x <= (int)(cx + rx + 1); x++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f && s.Get(x, y).a <= 0.01f) s.Set(x, y, new Color(26 / 255f, 16 / 255f, 32 / 255f, 0.36f));
            }
    }
    static void Glow(Cv s, float cx, float cy, float rx, float ry, Color col, float a) { s.Ell(cx, cy, rx, ry, col, a); }

    // ---------------------------------------------------------------- sprites
    static Cv Anvil()
    {
        var c = new Cv(40, 36);
        Trap(c, 20, 24, 33, 8f, 10.5f, STONE);
        c.Rect(12, 28, 28, 28, STONE[1]); c.Rect(12, 31, 28, 31, STONE[1]);
        c.Poly(P(3, 17, 10, 14, 34, 14, 34, 17, 29, 19, 28, 23, 31, 26, 11, 26, 14, 23, 15, 19, 10, 18), IRON[1]);
        c.Rect(10, 14, 34, 14, IRON[3]); c.Rect(10, 15, 34, 15, IRON[2]);
        c.Line(3, 17, 10, 14, IRON[3]);
        c.Rect(26, 17, 28, 25, IRON[0]); c.Rect(15, 19, 16, 23, IRON[2]);
        c.Rect(11, 24, 31, 25, IRON[1]); c.Rect(11, 24, 31, 24, IRON[2]); c.Rect(27, 24, 31, 25, IRON[0]);
        c.Rect(15, 12, 28, 13, FIRE[1]); c.Rect(15, 12, 28, 12, FIRE[2]); c.Rect(19, 12, 23, 12, FIRE[3]);
        c.Line(33, 31, 36, 22, WOOD[2]); c.Line(34, 31, 37, 22, WOOD[1]);
        c.Rect(33, 19, 38, 22, IRON[2]); c.Rect(33, 19, 38, 19, IRON[3]); c.Rect(36, 20, 38, 22, IRON[1]);
        Outline(c);
        Glow(c, 21.5f, 12f, 11f, 4.5f, FIRE[1], 0.14f); Glow(c, 21.5f, 12.5f, 6f, 2.6f, FIRE[2], 0.20f);
        c.Set(13, 7, FIRE[3]); c.Set(25, 5, FIRE[2]); c.Set(29, 8, FIRE[3]); c.Set(18, 3, FIRE[2]); c.Set(31, 4, FIRE[3]);
        Shadow(c, 20, 34.3f, 16f, 2.2f);
        return c;
    }

    static Cv OreCart()
    {
        var c = new Cv(44, 30);
        for (int x = 5; x < 40; x += 11) { c.Rect(x, 26, x + 2, 28, WOOD[1]); c.Rect(x, 26, x + 2, 26, WOOD[2]); }
        c.Rect(2, 25, 41, 25, IRON[2]); c.Rect(2, 26, 41, 26, IRON[1]);
        c.Poly(P(5, 12, 39, 12, 36, 23, 8, 23), WOOD[2]);
        c.Line(7, 16, 37, 16, WOOD[1]); c.Line(8, 19, 36, 19, WOOD[1]);
        c.Poly(P(5, 12, 9, 12, 11, 23, 8, 23), WOOD[3]);
        c.Poly(P(35, 12, 39, 12, 36, 23, 33, 23), WOOD[1]);
        c.Rect(14, 12, 15, 23, IRON[1]); c.Rect(29, 12, 30, 23, IRON[1]);
        c.Rect(4, 11, 40, 12, IRON[2]); c.Rect(4, 11, 40, 11, IRON[3]);
        Ball(c, 12, 23.5f, 3.4f, 3.4f, IRON); c.Set(12, 23, IRON[3]);
        Ball(c, 32, 23.5f, 3.4f, 3.4f, IRON); c.Set(32, 23, IRON[3]);
        c.Poly(P(6, 12, 9, 8, 15, 5, 21, 6, 27, 3, 33, 6, 38, 12), STONE[1]);
        Ball(c, 17, 10, 2.6f, 2f, STONE); Ball(c, 25, 10, 2.6f, 2f, STONE); Ball(c, 9.5f, 10, 2f, 1.8f, STONE);
        Ball(c, 13, 8, 3f, 2.4f, FIRE); Ball(c, 21, 6.5f, 3.4f, 2.8f, FIRE); Ball(c, 29, 6, 3f, 2.6f, FIRE); Ball(c, 34, 9.5f, 2.4f, 2f, FIRE);
        Outline(c);
        Glow(c, 22, 7, 17f, 7f, FIRE[1], 0.20f);
        Shadow(c, 22, 29, 19f, 1.6f);
        return c;
    }

    static Cv Geyser()
    {
        var c = new Cv(44, 44);
        Ball(c, 22, 35, 19f, 8f, BASALT);
        Ball(c, 5, 35, 4f, 3.2f, BASALT); Ball(c, 39, 36, 4.5f, 3.4f, BASALT);
        Ball(c, 12, 41, 3.2f, 2.2f, BASALT); Ball(c, 33, 41, 3.5f, 2.4f, BASALT);
        c.Ell(22, 34, 12.5f, 5f, FIRE[0]); c.Ell(22, 34.3f, 11f, 4.2f, FIRE[1]);
        c.Ell(21.5f, 34, 7.5f, 2.8f, FIRE[2]); c.Ell(20.5f, 33.5f, 3.5f, 1.3f, FIRE[3]);
        c.Line(10, 36, 6, 39, FIRE[1]); c.Line(34, 37, 38, 40, FIRE[1]); c.Line(22, 39, 22, 42, FIRE[0]); c.Line(15, 39, 13, 42, FIRE[0]);
        Outline(c);
        Color st = new Color(0.82f, 0.76f, 0.86f, 1f);
        c.Ell(22, 27, 6f, 5f, st, 0.42f); c.Ell(24, 20, 5.2f, 4.4f, st, 0.34f); c.Ell(21, 13, 4.4f, 3.8f, st, 0.26f); c.Ell(23, 7, 3.4f, 3f, st, 0.18f);
        Glow(c, 22, 34, 22f, 10f, FIRE[1], 0.20f);
        c.Set(14, 24, FIRE[2]); c.Set(30, 19, FIRE[3]); c.Set(27, 12, FIRE[2]); c.Set(17, 16, FIRE[3]); c.Set(32, 28, FIRE[2]);
        return c;
    }

    static Cv Altar()
    {
        var c = new Cv(44, 38);
        Trap(c, 22, 29, 35, 19.5f, 21f, COOL);
        Trap(c, 22, 23, 28, 15f, 16.5f, COOL);
        Trap(c, 22, 19, 22, 12f, 13f, COOL);
        c.Set(22, 25, VIOLET[3]); c.Set(21, 26, VIOLET[2]); c.Set(22, 26, VIOLET[3]); c.Set(23, 26, VIOLET[2]); c.Set(22, 27, VIOLET[2]);
        c.Rect(16, 26, 18, 26, VIOLET[1]); c.Rect(26, 26, 28, 26, VIOLET[1]);
        foreach (int x in new int[] { 4, 38 })
        {
            c.Rect(x, 25, x + 1, 28, STONE[4]); c.Rect(x, 25, x, 28, CREAM[3]);
            c.Set(x, 24, FIRE[2]); c.Set(x + 1, 24, FIRE[1]); c.Set(x, 23, FIRE[3]);
        }
        Ball(c, 22, 11, 5f, 5f, VIOLET);
        Outline(c);
        Glow(c, 22, 11, 13f, 13f, VIOLET[2], 0.11f); Glow(c, 22, 11, 8f, 8f, VIOLET[3], 0.14f);
        foreach (int x in new int[] { 4, 38 }) Glow(c, x + 0.5f, 24, 5f, 5f, FIRE[1], 0.22f);
        c.Set(14, 7, VIOLET[3]); c.Set(30, 5, VIOLET[3]); c.Set(27, 15, VIOLET[2]); c.Set(16, 15, VIOLET[3]);
        Shadow(c, 22, 36.3f, 20f, 1.8f);
        return c;
    }

    static Cv Candelabra()
    {
        var c = new Cv(28, 50);
        Ball(c, 14, 46, 8f, 2.6f, IRON); Ball(c, 14, 43, 3f, 1.8f, IRON);
        Cyl(c, 13, 15, 12, 43, IRON);
        Ball(c, 14, 35, 3.2f, 1.8f, GOLD); Ball(c, 14, 28, 3f, 1.7f, GOLD);
        Color arm = IRON[2], armHi = GOLD[1];
        int[,] tips = { { 3, 16 }, { 8, 12 }, { 14, 10 }, { 20, 12 }, { 25, 16 } };
        Thick(c, 14, 23, 8, 23, arm); Thick(c, 8, 23, 3, 16, arm);
        Thick(c, 14, 21, 9, 17, arm); Thick(c, 9, 17, 8, 12, arm);
        Thick(c, 14, 23, 20, 23, arm); Thick(c, 20, 23, 25, 16, arm);
        Thick(c, 14, 21, 19, 17, arm); Thick(c, 19, 17, 20, 12, arm);
        for (int i = 0; i < 5; i++)
        {
            int x = tips[i, 0], y = tips[i, 1];
            c.Rect(x - 2, y, x + 1, y + 1, GOLD[1]); c.Rect(x - 2, y, x + 1, y, GOLD[2]);
            c.Rect(x - 1, y - 4, x, y - 1, STONE[4]); c.Rect(x - 1, y - 4, x - 1, y - 1, CREAM[3]);
        }
        Outline(c);
        for (int i = 0; i < 5; i++)
        {
            int x = tips[i, 0], y = tips[i, 1];
            c.Set(x - 1, y - 5, FIRE[1]); c.Set(x, y - 5, FIRE[0]); c.Set(x - 1, y - 6, FIRE[2]); c.Set(x - 1, y - 7, FIRE[3]);
            Glow(c, x - 0.5f, y - 6, 3.2f, 3.2f, FIRE[1], 0.22f);
        }
        Shadow(c, 14, 48.4f, 9f, 1.5f);
        return c;
    }

    static Cv Gargoyle()
    {
        var c = new Cv(36, 48);
        Trap(c, 18, 38, 45, 11f, 13f, COOL);
        c.Poly(P(12, 29, 3, 17, 1, 27, 4, 33, 9, 35), COOL[2]);
        c.Poly(P(24, 29, 33, 17, 35, 27, 32, 33, 27, 35), COOL[1]);
        c.Line(12, 29, 3, 17, COOL[3]); c.Line(10, 31, 2, 25, COOL[1]); c.Line(25, 31, 33, 25, COOL[0]); c.Line(24, 29, 33, 17, COOL[2]);
        c.Poly(P(24, 36, 31, 38, 32, 35, 28, 34), COOL[1]);
        Ball(c, 18, 32, 6.5f, 7f, COOL);
        c.Rect(12, 36, 15, 38, COOL[2]); c.Rect(21, 36, 24, 38, COOL[1]);
        Ball(c, 18, 22, 5.5f, 5f, COOL);
        c.Poly(P(13, 19, 11, 14, 15, 17), COOL[3]); c.Poly(P(23, 19, 25, 14, 21, 17), COOL[1]);
        c.Rect(16, 24, 20, 26, COOL[2]); c.Set(17, 26, COOL[0]); c.Set(19, 26, COOL[0]); c.Set(16, 27, COOL[4]); c.Set(20, 27, COOL[4]);
        Outline(c);
        c.Set(15, 21, FIRE[2]); c.Set(16, 21, FIRE[3]); c.Set(20, 21, FIRE[2]); c.Set(21, 21, FIRE[3]);
        Glow(c, 16, 21, 4f, 3f, FIRE[0], 0.30f); Glow(c, 21, 21, 4f, 3f, FIRE[0], 0.30f);
        Shadow(c, 18, 46.3f, 13f, 1.6f);
        return c;
    }

    static Cv Mushrooms()
    {
        var c = new Cv(36, 34);
        for (int x = 3; x < 33; x += 3)
        {
            int dx = (Hs(x, 1, 4) < 0.5f) ? -1 : 1;
            c.Line(x, 31, x + dx, 28, GREEN[2]);
        }
        Cyl(c, 16, 19, 19, 30, CREAM);
        Ball(c, 17.5f, 16f, 9.5f, 6.5f, TEAL);
        c.Set(14, 14, TEAL[3]); c.Set(19, 12, TEAL[3]); c.Set(22, 16, TEAL[3]); c.Set(15, 17, TEAL[2]);
        Cyl(c, 7, 9, 23, 30, CREAM);
        Ball(c, 8f, 22f, 6f, 4.2f, VIOLET);
        c.Set(6, 21, VIOLET[3]); c.Set(10, 20, VIOLET[3]);
        Cyl(c, 27, 28, 25, 30, CREAM);
        Ball(c, 27.5f, 24.5f, 4.6f, 3.3f, TEAL);
        c.Set(26, 23, TEAL[3]);
        Outline(c);
        Glow(c, 17.5f, 16f, 13f, 9f, TEAL[2], 0.18f); Glow(c, 8, 22, 9f, 7f, VIOLET[2], 0.18f); Glow(c, 27.5f, 24.5f, 7f, 5f, TEAL[2], 0.16f);
        c.Set(11, 8, TEAL[3]); c.Set(24, 10, TEAL[3]); c.Set(6, 15, VIOLET[3]); c.Set(30, 18, TEAL[3]); c.Set(20, 5, TEAL[3]);
        Shadow(c, 18, 31.6f, 15f, 1.6f);
        return c;
    }

    static Cv ThornBush()
    {
        var c = new Cv(40, 30);
        Color[] dg = T(0x1f3a1c, 0x2e5e2e, 0x437d36, 0x62a044);
        Ball(c, 12, 19, 8f, 7f, dg); Ball(c, 28, 19.5f, 9f, 7f, dg);
        Ball(c, 20, 15, 10f, 9f, dg); Ball(c, 19, 22, 10f, 6f, dg);
        for (int i = 0; i < 7; i++)
        {
            float a0 = 3.14159f * (0.05f + 0.9f * i / 6f);
            int x0 = Mathf.RoundToInt(20 + 18 * Mathf.Cos(a0)), y0 = Mathf.RoundToInt(22 - 3 * Mathf.Sin(a0));
            int x1 = Mathf.RoundToInt(20 + 8 * Mathf.Cos(a0 + 0.9f)), y1 = Mathf.RoundToInt(14 - 7 * Mathf.Sin(a0 + 0.9f));
            c.Line(x0, y0, x1, y1, WOOD[1]);
            c.Line(x0, y0 - 1, x1, y1 - 1, WOOD[2]);
            c.Set((x0 + x1) / 2, (y0 + y1) / 2 - 2, C(0xe6d2a8));
        }
        for (int i = 0; i < 18; i++)
        {
            float a = Mathf.PI * (0.04f + 0.92f * (i / 17f)) + (Hs(i, 3, 2) - 0.5f) * 0.2f;
            float px = 20f + 18.5f * Mathf.Cos(a), py = 17f - 10.5f * Mathf.Sin(a);
            float nx = Mathf.Cos(a), ny = -Mathf.Sin(a);
            float len = 4f + Hs(i, 4, 2) * 3f;
            int tx = Mathf.RoundToInt(px + nx * len), ty = Mathf.RoundToInt(py + ny * len);
            c.Line(Mathf.RoundToInt(px), Mathf.RoundToInt(py), tx, ty, C(0xe6d2a8));
            c.Set(Mathf.RoundToInt(px), Mathf.RoundToInt(py), WOOD[2]);
        }
        for (int x = 6; x < 36; x += 4) c.Line(x, 26, x + ((x % 8 == 6) ? -1 : 1), 28, GREEN[2]);
        Color berry = C(0xd44a5a);
        foreach (int[] b in new int[][] { new int[] { 14, 12 }, new int[] { 25, 9 }, new int[] { 31, 17 }, new int[] { 19, 19 } })
        {
            c.Rect(b[0], b[1], b[0] + 1, b[1] + 1, berry); c.Set(b[0], b[1], C(0xff9aa8));
        }
        Outline(c);
        Shadow(c, 20, 28.2f, 17f, 1.8f);
        return c;
    }

    static Cv Shrine()
    {
        var c = new Cv(44, 44);
        Trap(c, 22, 33, 41, 15f, 17f, STONE);
        Color moss = C(0x56783c), mossHi = C(0x70b04c);
        c.Rect(8, 33, 15, 34, moss); c.Rect(8, 33, 12, 33, mossHi); c.Rect(9, 35, 10, 36, moss);
        c.Rect(28, 38, 33, 39, moss); c.Rect(28, 38, 30, 38, mossHi);
        Cyl(c, 8, 13, 10, 34, STONE); Cyl(c, 31, 36, 10, 34, STONE);
        foreach (int y in new int[] { 16, 26 }) { c.Rect(8, y, 13, y, STONE[1]); c.Rect(31, y, 36, y, STONE[1]); }
        c.Rect(7, 9, 14, 10, STONE[3]); c.Rect(30, 9, 37, 10, STONE[3]); c.Rect(7, 9, 14, 9, STONE[4]); c.Rect(30, 9, 37, 9, STONE[4]);
        c.Rect(7, 5, 37, 8, STONE[2]); c.Rect(7, 5, 37, 5, STONE[4]); c.Rect(7, 6, 37, 6, STONE[3]); c.Rect(7, 8, 37, 8, STONE[1]);
        c.Rect(9, 5, 14, 5, mossHi); c.Rect(9, 6, 11, 6, moss);
        Ball(c, 22, 33, 12f, 4.2f, STONE);
        c.Ell(22, 32.6f, 9.5f, 3f, TEAL[1]); c.Ell(21, 32.2f, 6f, 2f, TEAL[2]); c.Ell(20, 31.8f, 2.5f, 0.9f, TEAL[3]);
        foreach (int x in new int[] { 10, 15, 29, 34 })
        {
            int len = 3 + (int)(Hs(x, 5, 3) * 5);
            c.Line(x, 9, x, 9 + len, GREEN[1]); c.Set(x, 9 + len, GREEN[3]);
        }
        Ball(c, 22, 20, 4.2f, 4.2f, TEAL);
        c.Poly(P(16, 21, 12, 19, 15, 23), GREEN[2]); c.Poly(P(28, 21, 32, 19, 29, 23), GREEN[1]);
        Outline(c);
        Glow(c, 22, 20, 12f, 12f, TEAL[2], 0.16f); Glow(c, 22, 20, 7f, 7f, TEAL[3], 0.18f); Glow(c, 22, 32, 14f, 5f, TEAL[2], 0.18f);
        c.Set(17, 13, TEAL[3]); c.Set(28, 15, TEAL[3]); c.Set(24, 8, TEAL[3]); c.Set(19, 26, TEAL[2]); c.Set(26, 27, TEAL[3]);
        Shadow(c, 22, 42f, 17f, 1.6f);
        return c;
    }

    // ---------------------------------------------------------------- export
    static void SaveImport(Cv c, string name)
    {
        Directory.CreateDirectory(Dir);
        var t = new Texture2D(c.w, c.h, TextureFormat.RGBA32, false);
        var px = new Color[c.w * c.h];
        for (int y = 0; y < c.h; y++)
            for (int x = 0; x < c.w; x++) px[(c.h - 1 - y) * c.w + x] = c.p[y * c.w + x];
        t.SetPixels(px);
        t.Apply();
        string path = Dir + name + ".png";
        File.WriteAllBytes(path, t.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 20;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.wrapMode = TextureWrapMode.Clamp;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = new Vector2(0.5f, 10f / c.h);
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
    }

    public static string Run()
    {
        SaveImport(Anvil(), "Content_LavaAnvil");
        SaveImport(OreCart(), "Content_OreCart");
        SaveImport(Geyser(), "Content_Geyser");
        SaveImport(Altar(), "Content_Altar");
        SaveImport(Candelabra(), "Content_Candelabra");
        SaveImport(Gargoyle(), "Content_Gargoyle");
        SaveImport(Mushrooms(), "Content_Mushrooms");
        SaveImport(ThornBush(), "Content_ThornBush");
        SaveImport(Shrine(), "Content_Shrine");
        return "content art ok";
    }
}
