using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public static class MenuArt
{
    public const string Dir = "Assets/_Game/Art/UI/Menu/";

    // ============================ canvas ============================
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
        public void Blit(Cv s, int dx, int dy, float a = 1f, bool flipY = false, bool ripple = false)
        {
            for (int y = 0; y < s.h; y++)
                for (int x = 0; x < s.w; x++)
                {
                    Color c = s.p[y * s.w + x];
                    if (c.a <= 0f) continue;
                    int ty = flipY ? dy + (s.h - 1 - y) : dy + y;
                    if (ripple && Hs((x + dx) / 3, ty, 2) < 0.38f) continue;
                    Mix(x + dx, ty, c, a);
                }
        }
    }

    // ============================ helpers ============================
    static Color C(int rgb) { return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255); }
    static Color Lm(Color a, Color b, float t) { return Color.Lerp(a, b, Mathf.Clamp01(t)); }
    static Color Mul(Color a, float k) { return new Color(a.r * k, a.g * k, a.b * k, a.a); }
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
    static readonly int[] BY = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
    static float Thr(int x, int y) { return (BY[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f; }
    static Color Grad(float y, float[] ys, Color[] cs)
    {
        if (y <= ys[0]) return cs[0];
        for (int i = 1; i < ys.Length; i++)
            if (y <= ys[i]) return Color.Lerp(cs[i - 1], cs[i], (y - ys[i - 1]) / (ys[i] - ys[i - 1]));
        return cs[cs.Length - 1];
    }
    static Color Dith(Color[] ramp, float v, int x, int y)
    {
        v = Mathf.Clamp(v, 0f, ramp.Length - 1.001f);
        int i = (int)v;
        return (v - i) > Thr(x, y) ? ramp[i + 1] : ramp[i];
    }
    static List<Vector2> P(params float[] v)
    {
        var l = new List<Vector2>();
        for (int i = 0; i + 1 < v.Length; i += 2) l.Add(new Vector2(v[i], v[i + 1]));
        return l;
    }

    static void Save(Cv c, string name)
    {
        Directory.CreateDirectory(Dir);
        var t = new Texture2D(c.w, c.h, TextureFormat.RGBA32, false);
        var px = new Color[c.w * c.h];
        for (int y = 0; y < c.h; y++)
            for (int x = 0; x < c.w; x++) px[(c.h - 1 - y) * c.w + x] = c.p[y * c.w + x];
        t.SetPixels(px);
        t.Apply();
        File.WriteAllBytes(Dir + name + ".png", t.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(t);
    }
    // border = (left, bottom, right, top)
    static void Import(string name, Vector4 border)
    {
        string path = Dir + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.spriteBorder = border;
        ti.SaveAndReimport();
    }

    // ============================ background ============================
    static bool InBlobs(List<Vector3> bl, float x, float y, float baseY)
    {
        if (y > baseY) return false;
        for (int i = 0; i < bl.Count; i++)
        {
            var b = bl[i];
            float dx = (x - b.x) / b.z, dy = (y - b.y) / (b.z * 0.8f);
            if (dx * dx + dy * dy <= 1f) return true;
        }
        return false;
    }
    static void Cloud(Cv c, int cx, int baseY, int wd, int ht, int seed, Color lit, Color mid, Color dark, float alpha)
    {
        var r = new System.Random(seed);
        var bl = new List<Vector3>();
        int n = Mathf.Max(4, wd / 9);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            float bx = cx - wd / 2f + t * wd;
            float prof = Mathf.Sin(t * Mathf.PI);
            float br = ht * (0.40f + 0.75f * prof) * (0.65f + 0.5f * (float)r.NextDouble());
            bl.Add(new Vector3(bx, baseY - br * 0.55f, br));
        }
        int x0 = cx - wd / 2 - ht, x1 = cx + wd / 2 + ht, y0 = baseY - ht * 2 - 4;
        for (int y = y0; y <= baseY; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (!InBlobs(bl, x, y, baseY)) continue;
                Color col;
                if (!InBlobs(bl, x - 2, y - 2, baseY)) col = lit;
                else if ((!InBlobs(bl, x, y + 3, baseY) || y >= baseY - 1) && Thr(x, y) > 0.35f) col = dark;
                else if (!InBlobs(bl, x, y + 6, baseY) && Thr(x, y) > 0.6f) col = dark;
                else col = mid;
                c.Mix(x, y, col, alpha);
            }
    }

    static void Moon(Cv c, float mx, float my, float r)
    {
        Color t4 = C(0xf6fbff), t3 = C(0xdcebf8), t2 = C(0xb7cde6), t1 = C(0x8ea8cc);
        for (int y = (int)(my - r - 1); y <= (int)(my + r + 1); y++)
            for (int x = (int)(mx - r - 1); x <= (int)(mx + r + 1); x++)
            {
                float u = (x + 0.5f - mx) / r, v = (y + 0.5f - my) / r;
                float d2 = u * u + v * v;
                if (d2 > 1f) continue;
                float lit = -0.45f * u - 0.5f * v + 0.8f * Mathf.Sqrt(Mathf.Max(0f, 1f - d2));
                Color col = lit > 0.78f ? t4 : lit > 0.46f ? t3 : lit > 0.18f ? t2 : t1;
                c.Set(x, y, col);
            }
        // craters
        float[,] cr = { { -8, -5, 4.5f, 3.5f }, { 7, 8, 5.5f, 4f }, { 3, -12, 3f, 2.5f }, { -11, 8, 3f, 2.5f }, { 11, -4, 2.5f, 2f } };
        for (int i = 0; i < cr.GetLength(0); i++)
        {
            float ex = mx + cr[i, 0], ey = my + cr[i, 1], rx = cr[i, 2], ry = cr[i, 3];
            for (int y = (int)(ey - ry - 1); y <= (int)(ey + ry + 1); y++)
                for (int x = (int)(ex - rx - 1); x <= (int)(ex + rx + 1); x++)
                {
                    float dx = (x + 0.5f - ex) / rx, dy = (y + 0.5f - ey) / ry;
                    if (dx * dx + dy * dy > 1f) continue;
                    float ux = (x + 0.5f - mx) / r, uy = (y + 0.5f - my) / r;
                    if (ux * ux + uy * uy > 0.9f) continue;
                    Color b = c.Get(x, y);
                    c.Set(x, y, Mul(b, dx + dy < -0.2f ? 0.84f : 0.92f));
                }
        }
    }

    static void Beam(Cv c, float x0, float y0, float x1, float ya, float yb, float alpha)
    {
        for (int x = (int)x0; x <= (int)x1; x++)
        {
            float t = (x - x0) / (x1 - x0);
            float top = Mathf.Lerp(y0, ya, t), bot = Mathf.Lerp(y0, yb, t);
            float a = alpha * (1f - 0.7f * t) * Mathf.Clamp01((x - x0) / 14f);
            for (int y = (int)top; y <= (int)bot; y++)
            {
                float edge = Mathf.Min(y - top, bot - y);
                float ea = Mathf.Clamp01(edge / 7f);
                c.Mix(x, y, C(0xfff2cc), a * (0.30f + 0.70f * ea));
            }
        }
    }

    static void City(Cv c, int HZ, int seed)
    {
        var r = new System.Random(seed);
        Color warm = C(0xffc766), warmDim = C(0xb89a62);
        for (int layer = 0; layer < 2; layer++)
        {
            Color body = layer == 0 ? C(0x2a4474) : C(0x16233f);
            Color rim = layer == 0 ? C(0x5b7fb8) : C(0x34508a);
            int x = layer == 0 ? -6 : -10 + r.Next(0, 6);
            while (x < 306)
            {
                int bw = r.Next(8, 19) + (layer == 0 ? 4 : 0);
                float hmax = (layer == 0 ? 30f : 16f) + (layer == 0 ? 34f : 20f) * Mathf.Exp(-Mathf.Pow((x - 150) / 85f, 2f));
                int bh = Mathf.Max(6, (int)(hmax * (0.45f + 0.75f * (float)r.NextDouble())));
                int top = HZ - bh;
                c.Rect(x, top, x + bw - 1, HZ - 1, body);
                int roof = r.Next(5);
                int rh = r.Next(4, 10);
                float mid = x + bw / 2f;
                if (roof == 0) c.Poly(P(x - 1, top, x + bw, top, mid, top - rh), body);
                else if (roof == 2) c.Ell(mid, top, bw / 2f, rh * 0.7f, body);
                else if (roof == 3) c.Poly(P(mid - 1.5f, top, mid + 1.5f, top, mid, top - rh - 8), body);
                else if (roof == 4) c.Rect(x + 2, top - 3, x + bw - 3, top - 1, body);
                c.Rect(x, top, x + bw - 1, top, rim, 0.75f);
                if (roof == 0) c.Line(x - 1, top, (int)mid, top - rh, rim, 0.6f);
                for (int wy = top + 3; wy < HZ - 3; wy += 4)
                    for (int wx = x + 2; wx < x + bw - 2; wx += 4)
                        if (Hs(wx, wy, seed) < (layer == 0 ? 0.12f : 0.30f))
                        {
                            Color wc = layer == 0 ? warmDim : warm;
                            c.Rect(wx - 1, wy - 1, wx + 2, wy + 2, warm, layer == 0 ? 0.06f : 0.12f);
                            c.Rect(wx, wy, wx + 1, wy + 1, wc);
                        }
                x += bw + r.Next(-2, 3);
            }
        }
        // cranes on the quay
        Color cr = C(0x101a30);
        int[] cxs = { 96, 188 };
        foreach (int cx in cxs)
        {
            c.Line(cx, HZ - 2, cx, HZ - 46, cr);
            c.Line(cx - 14, HZ - 46, cx + 20, HZ - 46, cr);
            c.Line(cx, HZ - 52, cx + 20, HZ - 46, cr);
            c.Line(cx - 14, HZ - 46, cx - 14, HZ - 34, cr);
            c.Set(cx - 14, HZ - 33, C(0xff7a5a));
        }
        // quay
        c.Rect(0, HZ - 2, 306, HZ + 1, C(0x0e1830));
        for (int x = 8; x < 306; x += 22)
        {
            c.Set(x, HZ - 4, C(0xffd36e));
            c.Rect(x - 1, HZ - 5, x + 1, HZ - 3, C(0xffb347), 0.18f);
        }
        // harbour lighthouse
        int lx = 268;
        int lh = 70;
        for (int y = 0; y < lh; y++)
        {
            float t = y / (float)lh;
            float hw = 6.2f - 2.4f * t;
            int yy = HZ - 1 - y;
            bool stripe = (y / 9) % 2 == 0;
            for (int x = lx - 7; x <= lx + 7; x++)
            {
                float u = (x - lx) / hw;
                if (Mathf.Abs(u) > 1f) continue;
                Color col;
                if (stripe) col = u < -0.2f ? C(0xe8dcc0) : u < 0.5f ? C(0xb8aa95) : C(0x7a7080);
                else col = u < -0.2f ? C(0x3f5c8a) : u < 0.5f ? C(0x2a4270) : C(0x1c2c50);
                c.Set(x, yy, col);
            }
        }
        int gy = HZ - lh - 1;
        c.Rect(lx - 6, gy, lx + 6, gy + 1, C(0xb98a3a));
        c.Rect(lx - 6, gy, lx - 3, gy, C(0xf0c458));
        c.Rect(lx - 3, gy - 7, lx + 3, gy - 1, C(0xffe9a8));
        c.Rect(lx - 1, gy - 6, lx + 1, gy - 2, C(0xffffff));
        c.Rect(lx - 4, gy - 8, lx + 4, gy - 8, C(0x2b1d26));
        c.Poly(P(lx - 4, gy - 8, lx + 5, gy - 8, lx + 0.5f, gy - 14), C(0x7a2a2e));
        c.Set(lx, gy - 15, C(0xe8b84a));
        c.Ell(lx, gy - 4, 16, 14, C(0xffd36e), 0.08f);
        c.Ell(lx, gy - 4, 9, 8, C(0xffd36e), 0.14f);
        c.Ell(lx, gy - 4, 5, 5, C(0xfff2c0), 0.25f);
    }

    static void Tower(Cv c, int cx, int baseY, int seed)
    {
        int T = 34;
        Color[] stone = { C(0x1a2442), C(0x2a3a62), C(0x3d5282), C(0x5a74a6), C(0x7f9bc8) };
        Color haze = C(0x5aa8b8);
        for (int y = -12; y < baseY; y++)
        {
            float w = 14f + 17f * (y + 12) / (float)(baseY + 12);
            int fb = baseY - y;
            int rem = fb % T;
            bool cornice = fb >= T && rem <= 3;
            float ext = cornice ? 4f : 0f;
            int xa = Mathf.FloorToInt(cx - w - ext), xb = Mathf.CeilToInt(cx + w + ext);
            for (int x = xa; x <= xb; x++)
            {
                float dx = x + 0.5f - cx;
                if (Mathf.Abs(dx) > w + ext) continue;
                Color col;
                if (cornice)
                {
                    bool right = dx > w * 0.15f;
                    Color[] br = { C(0x5a3b1e), C(0x8a6428), C(0xb98a3a), C(0xf0c458) };
                    col = br[rem];
                    if (right) col = Mul(col, 0.72f);
                    if (rem == 3 && dx < -w * 0.4f) col = C(0xffe08a);
                }
                else
                {
                    float u = Mathf.Clamp(dx / w, -0.999f, 0.999f);
                    float it = -0.55f * u + 0.7f * Mathf.Sqrt(1f - u * u);
                    int ti = it > 0.75f ? 4 : it > 0.5f ? 3 : it > 0.2f ? 2 : it > -0.1f ? 1 : 0;
                    // pilasters
                    float[] pu = { -0.64f, 0f, 0.64f };
                    for (int k = 0; k < 3; k++)
                    {
                        int px = Mathf.RoundToInt(pu[k] * w);
                        int ix = Mathf.FloorToInt(dx);
                        if (ix == px) ti = Mathf.Min(4, ti + 1);
                        else if (ix == px + 1) ti = Mathf.Max(0, ti - 1);
                    }
                    col = stone[ti];
                    // brick courses
                    if (fb % 6 == 0) col = Mul(col, 0.86f);
                    if (rem >= T - 3 && fb >= T - 3) col = Mul(col, 0.72f);
                    if (Mathf.Abs(dx) > w - 1f && dx > 0) col = Mul(col, 0.8f);
                    if (dx < -w + 1f) col = Lm(col, C(0xaec6e6), 0.35f);
                }
                Color bg = c.Get(x, y);
                col = Lm(col, bg, 0.30f * (baseY - y) / (float)(baseY + 12));
                col = Lm(col, haze, Mathf.Clamp01(1f - fb / 34f) * 0.5f);
                c.Set(x, y, col);
            }
        }
        // windows
        for (int j = 1; j * T < baseY + 12; j++)
        {
            int yTop = baseY - (j * T + 22), yBot = baseY - (j * T + 14);
            float w = 14f + 17f * (yTop + 12) / (float)(baseY + 12);
            bool cyan = (j % 3) == 1;
            int[] th = { -52, -26, 0, 26, 52 };
            for (int k = 0; k < th.Length; k++)
            {
                float ux = Mathf.Sin(th[k] * Mathf.Deg2Rad);
                int xc = cx + Mathf.RoundToInt(ux * w);
                int ww = (th[k] == 0 || Mathf.Abs(th[k]) == 26) ? 3 : 2;
                bool on = Hs(j, k, seed) < 0.8f;
                Color top = cyan ? C(0xd8fbff) : C(0xfff0b8), mid = cyan ? C(0x7ee6ff) : C(0xffd36e), bot = cyan ? C(0x3aa8d8) : C(0xe39a3a);
                if (on) c.Rect(xc - 3, yTop - 2, xc + 4, yBot + 2, mid, 0.10f);
                for (int y = yTop; y <= yBot; y++)
                    for (int x = xc - ww / 2; x < xc - ww / 2 + ww; x++)
                    {
                        if (y == yTop && ww == 3 && x != xc) continue;
                        Color col = !on ? C(0x141c38) : (y - yTop < 2 ? top : (y - yTop < 6 ? mid : bot));
                        c.Set(x, y, col);
                    }
                c.Rect(xc - ww / 2 - 1, yBot + 1, xc - ww / 2 + ww, yBot + 1, C(0x10182e));
            }
        }
        // great gate
        c.Rect(cx - 7, baseY - 30, cx + 6, baseY - 12, C(0x2b1d26));
        c.Rect(cx - 5, baseY - 28, cx + 4, baseY - 12, C(0xffd36e));
        c.Rect(cx - 5, baseY - 28, cx + 4, baseY - 26, C(0xfff0b8));
        c.Rect(cx - 5, baseY - 18, cx + 4, baseY - 12, C(0xe39a3a));
        c.Rect(cx, baseY - 28, cx, baseY - 12, C(0x6b4326));
        c.Rect(cx - 10, baseY - 32, cx + 9, baseY - 8, C(0xffc766), 0.10f);
    }

    static void Isle(Cv c, int cx, int wl, int seed)
    {
        float[] ox = { -86, -72, -60, -50, -42, -32, -22, 4, 20, 34, 46, 58, 72, 88 };
        float[] oy = { 2, -4, -9, -8, -14, -11, -15, -13, -16, -12, -17, -9, -6, 2 };
        var pts = new List<Vector2>();
        for (int i = 0; i < ox.Length; i++) pts.Add(new Vector2(cx + ox[i], wl + oy[i]));
        pts.Add(new Vector2(cx + 88, wl + 6)); pts.Add(new Vector2(cx - 86, wl + 6));
        c.Poly(pts, C(0x121a33));
        for (int x = cx - 86; x <= cx + 88; x++)
        {
            int i = 0;
            while (i < ox.Length - 2 && cx + ox[i + 1] < x) i++;
            float t = (x - (cx + ox[i])) / (ox[i + 1] - ox[i]);
            int top = Mathf.RoundToInt(wl + Mathf.Lerp(oy[i], oy[i + 1], Mathf.Clamp01(t)));
            if (x < cx) { c.Set(x, top, C(0x4a6a98)); c.Set(x, top + 1, C(0x2b4270)); }
            else { c.Set(x, top, C(0x2b4270)); }
            // strata
            for (int y = top + 3; y < wl + 4; y += 3) if (Hs(x / 4, y, seed) < 0.28f) c.Set(x, y, C(0x1d2a4c));
        }
        // dock lamps
        for (int k = 0; k < 4; k++)
        {
            int lx = cx - 56 + k * 38;
            c.Set(lx, wl - 6, C(0xffd36e));
            c.Rect(lx - 2, wl - 8, lx + 2, wl - 4, C(0xffb347), 0.16f);
        }
        // foam
        for (int x = cx - 90; x <= cx + 92; x += 1)
        {
            if (Hs(x / 3, wl, seed + 1) < 0.62f) { c.Mix(x, wl + 2, C(0xcdf3f5), 0.85f); if (Hs(x, wl, 4) < 0.5f) c.Mix(x, wl + 3, C(0x7fc9d8), 0.6f); }
        }
    }

    static Cv Ship()
    {
        var s = new Cv(60, 52);
        Color hull = C(0x241822), rail = C(0x7a5232), mast = C(0x2b1d26);
        s.Poly(P(6, 39, 50, 37, 46, 50, 14, 50), hull);
        s.Line(6, 39, 50, 37, rail);
        s.Line(8, 42, 48, 40, C(0x5c3b24));
        s.Line(6, 40, 14, 49, C(0x3b4c7a));
        s.Rect(38, 29, 48, 37, C(0x2f2030));
        s.Rect(37, 28, 49, 28, rail);
        s.Rect(41, 31, 43, 33, C(0xffd36e));
        s.Rect(45, 31, 46, 33, C(0xffd36e));
        s.Line(20, 38, 20, 6, mast);
        s.Line(35, 37, 35, 12, mast);
        s.Line(7, 38, 0, 33, mast);
        float[][] sails = {
            new float[]{11,9,29,9,31,21,9,21},
            new float[]{12,23,28,23,30,34,10,34},
            new float[]{30,16,40,16,41,27,29,27} };
        for (int i = 0; i < sails.Length; i++)
        {
            var pts = P(sails[i]);
            s.Poly(pts, C(0x8fa6c6));
            // lit left half
            var half = new List<Vector2>();
            float mxs = (sails[i][0] + sails[i][2]) / 2f;
            float mxb = (sails[i][6] + sails[i][4]) / 2f;
            half.Add(pts[0]); half.Add(new Vector2(mxs, pts[0].y)); half.Add(new Vector2(mxb, pts[2].y)); half.Add(pts[3]);
            s.Poly(half, C(0xc9d9ec));
            s.Line((int)mxs, (int)pts[0].y, (int)mxb, (int)pts[2].y, C(0x5a6f99), 0.6f);
            s.Line((int)pts[3].x, (int)pts[3].y, (int)pts[2].x, (int)pts[2].y, C(0x6f84ad));
        }
        s.Poly(P(20, 6, 28, 8, 20, 10), C(0xd4474a));
        s.Set(50, 33, C(0xffd36e)); s.Set(51, 33, C(0xffd36e));
        s.Ell(50, 33, 6, 6, C(0xffb347), 0.16f);
        return s;
    }

    static void Foreground(Cv c, int W, int H)
    {
        int dy = 336;
        Color[] wd = { C(0x7d4f2c), C(0x6b4326), C(0x5c3a22) };
        for (int y = dy; y < dy + 12; y++)
        {
            int strip = (y - dy) / 4, r = (y - dy) % 4;
            for (int x = 0; x < W; x++)
            {
                Color col = wd[strip % 3];
                if (r == 0) col = C(0x3a2216);
                else if (r == 1) col = Lm(col, C(0xa8794a), 0.35f);
                if ((x + strip * 17) % 46 == 0) col = C(0x3a2216);
                if (Hs(x, y, 5) < 0.035f) col = C(0x9a6a3c);
                c.Set(x, y, col);
            }
        }
        for (int y = dy + 12; y < H; y++)
        {
            float k = (y - dy - 12) / (float)(H - dy - 12);
            for (int x = 0; x < W; x++)
            {
                Color col = Mul(C(0x3a2216), 1f - 0.4f * k);
                if (y == dy + 12) col = C(0xa8794a);
                else if (y == dy + 13) col = C(0x5c3a22);
                if (x % 40 < 6 && y > dy + 13) col = Mul(C(0x24140c), 1f - 0.3f * k);
                c.Set(x, y, col);
            }
        }
        // posts + rope
        int[] posts = { 110, 250, 550, 690 };
        foreach (int px in posts)
        {
            c.Rect(px - 2, 306, px + 1, dy + 1, C(0x6b4326));
            c.Rect(px - 2, 306, px - 2, dy + 1, C(0x9a6a3c));
            c.Rect(px + 1, 306, px + 1, dy + 1, C(0x3a2216));
            c.Rect(px - 3, 304, px + 2, 305, C(0x8a5a30));
            c.Rect(px - 3, 304, px + 2, 304, C(0xb98a58));
        }
        for (int s = 0; s < 2; s++)
        {
            int xa = posts[s * 2], xb = posts[s * 2 + 1];
            for (int x = xa; x <= xb; x++)
            {
                float t = (x - xa) / (float)(xb - xa);
                int y = 314 + Mathf.RoundToInt(10f * 4f * t * (1f - t));
                c.Set(x, y, C(0xb08a58)); c.Set(x, y + 1, C(0x6b4a2c));
            }
        }
        // lanterns
        int[] lx = { 110, 690 };
        foreach (int x0 in lx)
        {
            c.Ell(x0, 297, 30, 25, C(0xffb347), 0.06f);
            c.Ell(x0, 297, 20, 17, C(0xffb347), 0.09f);
            c.Ell(x0, 297, 12, 10, C(0xffc766), 0.14f);
            c.Ell(x0, 342, 40, 6, C(0xffb347), 0.10f);
            c.Line(x0, 304, x0, 301, C(0x2b1d26));
            c.Rect(x0 - 3, 292, x0 + 2, 293, C(0x2b1d26));
            c.Rect(x0 - 2, 294, x0 + 1, 300, C(0xffd36e));
            c.Rect(x0 - 2, 294, x0 + 1, 295, C(0xfff0b8));
            c.Rect(x0 - 1, 296, x0, 298, C(0xffffff), 0.7f);
            c.Rect(x0 - 3, 301, x0 + 2, 301, C(0x2b1d26));
        }
    }

    public static string Bg()
    {
        const int W = 800, H = 360, HZ = 234;
        var c = new Cv(W, H);
        float moonX = 214f, moonY = 66f;

        // ---- sky ----
        float[] sy = { 0, 50, 100, 150, 190, 215, HZ };
        Color[] sc = { C(0x060a1c), C(0x0b1434), C(0x16225a), C(0x24398a), C(0x2e6aa8), C(0x3d97b4), C(0x6cc4c4) };
        const int N = 20;
        var ramp = new Color[N];
        for (int i = 0; i < N; i++) ramp[i] = Grad(i * HZ / (float)(N - 1), sy, sc);
        for (int y = 0; y < HZ; y++)
            for (int x = 0; x < W; x++)
            {
                float v = y * (N - 1) / (float)HZ;
                float dx = x - moonX, dy = y - moonY;
                v += 3.2f * Mathf.Exp(-(dx * dx + dy * dy) / (2f * 60f * 60f));
                c.Set(x, y, Dith(ramp, v, x, y));
            }

        // ---- stars ----
        var rnd = new System.Random(7);
        for (int i = 0; i < 300; i++)
        {
            int x = rnd.Next(W);
            int y = (int)(Mathf.Pow((float)rnd.NextDouble(), 1.5f) * 200);
            float b = (float)rnd.NextDouble();
            if (y > 150 && b < 0.5f) continue;
            float dx = x - moonX, dy = y - moonY;
            if (dx * dx + dy * dy < 36f * 36f) continue;
            Color sc2 = b > 0.9f ? C(0xffffff) : b > 0.55f ? C(0xbcd6ff) : C(0x7f9ad8);
            c.Set(x, y, sc2);
            if (b > 0.94f) { c.Mix(x - 1, y, sc2, 0.5f); c.Mix(x + 1, y, sc2, 0.5f); c.Mix(x, y - 1, sc2, 0.5f); c.Mix(x, y + 1, sc2, 0.5f); }
        }

        Moon(c, moonX, moonY, 24f);

        // ---- high clouds (behind everything) ----
        Color cl = C(0x9cc0e6), cm = C(0x4d6aa8), cd = C(0x2b3d80);
        Cloud(c, 120, 150, 120, 14, 3, cl, cm, cd, 0.92f);
        Cloud(c, 330, 112, 150, 12, 4, cl, cm, cd, 0.85f);
        Cloud(c, 520, 96, 130, 12, 5, cl, cm, cd, 0.85f);
        Cloud(c, 40, 92, 90, 10, 6, cl, cm, cd, 0.85f);
        Cloud(c, 700, 150, 130, 14, 7, cl, cm, cd, 0.9f);
        Cloud(c, 440, 176, 150, 9, 8, C(0x8fc7de), C(0x4a7fb0), C(0x2e5588), 0.85f);

        // ---- harbour beam ----
        Beam(c, 268, HZ - 74, 840, HZ - 74 - 125, HZ - 74 - 45, 0.16f);

        // ---- city + tower ----
        City(c, HZ, 21);
        Tower(c, 596, HZ, 33);
        Isle(c, 596, HZ, 17);

        // ---- low haze clouds in front ----
        Cloud(c, 560, HZ - 46, 220, 8, 12, C(0xb6e6e6), C(0x5f9db4), C(0x356a8c), 0.38f);
        Cloud(c, 120, HZ - 12, 260, 7, 13, C(0xb6e6e6), C(0x5f9db4), C(0x356a8c), 0.40f);

        // ---- sea ----
        var snap = (Color[])c.p.Clone();
        float[] ssy = { 0, 30, 70, H - HZ };
        Color[] ssc = { C(0x277a98), C(0x1a5580), C(0x0f3260), C(0x081a3a) };
        const int M = 16;
        var sramp = new Color[M];
        for (int i = 0; i < M; i++) sramp[i] = Grad(i * (H - HZ) / (float)(M - 1), ssy, ssc);
        for (int y = HZ; y < H; y++)
            for (int x = 0; x < W; x++)
                c.Set(x, y, Dith(sramp, (y - HZ) * (M - 1) / (float)(H - HZ), x, y));

        // reflection of sky/city/tower
        for (int y = HZ; y < HZ + 100; y++)
        {
            int src = 2 * HZ - 1 - y;
            if (src < 0) break;
            float t = (y - HZ) / 100f;
            float wob = Mathf.Sin(y * 0.9f + Hs(y, 3, 1) * 6f) * (0.4f + t * 2.5f);
            int segL = (int)(3 + t * 9);
            for (int x = 0; x < W; x++)
            {
                int sx = Mathf.Clamp(x + Mathf.RoundToInt(wob), 0, W - 1);
                if (Hs(x / segL, y, 5) < 0.30f + 0.2f * t) continue;
                Color s = snap[src * W + sx];
                float a = 0.50f * (1f - 0.75f * t);
                if (s.grayscale > 0.55f) a = Mathf.Min(0.95f, a + 0.4f);
                c.Mix(x, y, Lm(s, C(0x0b2142), 0.25f + 0.35f * t), a);
            }
        }
        // horizon mist
        for (int y = HZ; y < HZ + 9; y++)
            for (int x = 0; x < W; x++)
                if (Thr(x, y) < (1f - (y - HZ) / 9f) * 0.6f) c.Mix(x, y, C(0x9fe2dc), 0.35f);

        // waves + moon glitter
        var wr = new System.Random(11);
        for (int k = 1; ; k++)
        {
            int y = HZ + Mathf.RoundToInt(1.1f * Mathf.Pow(k, 1.42f));
            if (y >= H - 1) break;
            float t = (y - HZ) / (float)(H - HZ);
            int count = 7 + (int)(t * 16);
            for (int i = 0; i < count; i++)
            {
                int x = wr.Next(-10, W);
                int len = 3 + (int)(t * 16 * (0.4 + wr.NextDouble()));
                c.Rect(x, y, x + len - 1, y, C(0x5fb3cc), 0.55f);
                c.Rect(x + 1, y + 1, x + len, y + 1, C(0x0a2244), 0.5f);
            }
            for (int i = 0; i < 5; i++)
            {
                float sp = 5f + t * 26f;
                int x = Mathf.RoundToInt(moonX + ((float)wr.NextDouble() - 0.5f) * 2f * sp);
                int len = 2 + (int)(t * 9 * (0.4 + wr.NextDouble()));
                c.Rect(x, y, x + len - 1, y, C(0xe8f8ff), 0.9f);
                c.Rect(x - 1, y, x + len, y, C(0x9fd6e8), 0.35f);
            }
        }

        // ---- ship ----
        var ship = Ship();
        c.Blit(ship, 214, 288 - 51, 1f);
        c.Blit(ship, 214, 288 + 1, 0.30f, true, true);

        // ---- foreground deck ----
        Foreground(c, W, H);

        Save(c, "Menu_Bg");
        Import("Menu_Bg", Vector4.zero);
        return "bg ok";
    }

    // ============================ UI sprites ============================
    static bool InsetIn(int x, int y, int w, int h, int i)
    {
        int x0 = i, y0 = i, x1 = w - 1 - i, y1 = h - 1 - i;
        if (x < x0 || x > x1 || y < y0 || y > y1) return false;
        int cut = Mathf.Max(0, 2 - i);
        int dx = Mathf.Min(x - x0, x1 - x), dy = Mathf.Min(y - y0, y1 - y);
        return dx + dy >= cut;
    }
    static bool PIn(int x, int y, int px0, int py0, int px1, int py1, int i)
    {
        int x0 = px0 + i, y0 = py0 + i, x1 = px1 - i, y1 = py1 - i;
        if (x < x0 || x > x1 || y < y0 || y > y1) return false;
        int cut = Mathf.Max(0, 3 - i);
        int dx = Mathf.Min(x - x0, x1 - x), dy = Mathf.Min(y - y0, y1 - y);
        return dx + dy >= cut;
    }
    static void OutlinePass(Cv s, Color ol)
    {
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
    static readonly string[] AnchorRows = { "....X....", "...X.X...", "....X....", "..XXXXX..", "....X....", "....X....", "....X....", "X...X...X", "XX..X..XX", ".XX.X.XX.", "..XXXXX.." };
    static void Anchor(Cv s, int cx, int cy)
    {
        int x0 = cx - 4, y0 = cy - 5;
        for (int r = 0; r < AnchorRows.Length; r++)
            for (int k = 0; k < 9; k++)
                if (AnchorRows[r][k] == 'X') s.Set(x0 + k + 1, y0 + r + 1, C(0x5a3b1e));
        for (int r = 0; r < AnchorRows.Length; r++)
            for (int k = 0; k < 9; k++)
                if (AnchorRows[r][k] == 'X') s.Set(x0 + k, y0 + r, k < 4 ? C(0xffe9a0) : C(0xf0c458));
    }
    static void Rivet(Cv s, int x, int y)
    {
        s.Set(x, y, C(0xffe9a0)); s.Set(x + 1, y, C(0xd9a944)); s.Set(x, y + 1, C(0xd9a944)); s.Set(x + 1, y + 1, C(0x5a3b1e));
    }

    static Cv Button(int topHex, int midHex, int botHex)
    {
        const int w = 48, h = 38;
        var s = new Cv(w, h);
        Color top = C(topHex), mid = C(midHex), bot = C(botHex);
        Color[] o1 = { C(0xffe9a0), C(0xf0c458), C(0xb98a3a), C(0x8a6428) };
        Color[] o2 = { C(0xf0c458), C(0xd9a944), C(0x8a6428), C(0x5a3b1e) };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int layer = -1;
                for (int i = 0; i <= 4; i++) if (InsetIn(x, y, w, h, i)) layer = i;
                if (layer < 0) continue;
                int dl = x - layer, dr = (w - 1 - layer) - x, dt = y - layer, db = (h - 1 - layer) - y;
                int side = 0, best = dt;
                if (dl < best) { best = dl; side = 1; }
                if (dr < best) { best = dr; side = 2; }
                if (db < best) { best = db; side = 3; }
                Color col;
                if (layer == 0) col = C(0x0b1020);
                else if (layer == 1) col = o1[side];
                else if (layer == 2) col = o2[side];
                else if (layer == 3) col = C(0x0d1630);
                else
                {
                    float t = (y - 4) / (float)(h - 9);
                    col = t < 0.5f ? Lm(top, mid, t * 2f) : Lm(mid, bot, (t - 0.5f) * 2f);
                    if (y == 4) col = Lm(col, Color.white, 0.5f);
                    else if (y == 5) col = Lm(col, Color.white, 0.22f);
                    else if (y < h / 2) col = Lm(col, Color.white, 0.06f);
                    if (y == h - 5) col = Lm(col, Color.white, 0.14f);
                    if (x == 4) col = Lm(col, Color.white, 0.10f);
                    if (x == w - 5) col = Mul(col, 0.82f);
                }
                s.Set(x, y, col);
            }
        Rivet(s, 6, 6); Rivet(s, w - 8, 6); Rivet(s, 6, h - 8); Rivet(s, w - 8, h - 8);
        return s;
    }

    static Cv Plate()
    {
        const int w = 96, h = 48;
        var s = new Cv(w, h);
        Color ol = C(0x0b1020);
        for (int side = 0; side < 2; side++)
        {
            Func<float, float> mx = x => side == 0 ? x : w - x;
            s.Poly(P(mx(1), 15, mx(17), 15, mx(17), 36, mx(1), 36, mx(8), 25.5f), C(0x1d4f78));
            s.Poly(P(mx(1), 15, mx(17), 15, mx(17), 17, mx(1), 17), C(0x3a86b0));
            s.Poly(P(mx(1), 34, mx(17), 34, mx(17), 36, mx(1), 36), C(0x113a5c));
            s.Poly(P(mx(13), 17, mx(17), 17, mx(17), 34, mx(13), 34), C(0x123e63));
        }
        OutlinePass(s, ol);
        Color[] o1 = { C(0xffe9a0), C(0xf0c458), C(0xb98a3a), C(0x8a6428) };
        Color[] o2 = { C(0xf0c458), C(0xd9a944), C(0x8a6428), C(0x5a3b1e) };
        for (int y = 6; y <= 41; y++)
            for (int x = 12; x <= 83; x++)
            {
                int layer = -1;
                for (int i = 0; i <= 6; i++) if (PIn(x, y, 12, 6, 83, 41, i)) layer = i;
                if (layer < 0) continue;
                int dl = x - (12 + layer), dr = (83 - layer) - x, dt = y - (6 + layer), db = (41 - layer) - y;
                int side = 0, best = dt;
                if (dl < best) { best = dl; side = 1; }
                if (dr < best) { best = dr; side = 2; }
                if (db < best) { best = db; side = 3; }
                Color col;
                if (layer == 0) col = ol;
                else if (layer == 1) col = o1[side];
                else if (layer == 2) col = o2[side];
                else if (layer == 3) col = C(0x0d1630);
                else
                {
                    float t = (y - 10) / 28f;
                    col = Lm(C(0x1a3a6a), C(0x0a1430), t);
                    if (y == 10) col = Lm(col, Color.white, 0.18f);
                    if (layer == 5) col = Lm(col, C(0x2e8fb5), 0.55f);
                }
                s.Set(x, y, col);
            }
        Rivet(s, 19, 13); Rivet(s, 75, 13); Rivet(s, 19, 33); Rivet(s, 75, 33);
        Anchor(s, 25, 24);
        Anchor(s, w - 1 - 25, 24);
        return s;
    }

    static Cv Logo()
    {
        const int S = 92;
        float cx = S / 2f, cy = S / 2f;
        var d = new Cv(S, S);
        int HY = 62;
        float[] sy = { 8, 30, 48, HY };
        Color[] sc = { C(0x0a1230), C(0x16225a), C(0x2a4a9a), C(0x6fc0c8) };
        const int N = 10;
        var ramp = new Color[N];
        for (int i = 0; i < N; i++) ramp[i] = Grad(8 + i * (HY - 8) / (float)(N - 1), sy, sc);
        float[] wy = { HY, HY + 10, S };
        Color[] wc = { C(0x2f8aa4), C(0x1a5580), C(0x0a2146) };
        var sramp = new Color[8];
        for (int i = 0; i < 8; i++) sramp[i] = Grad(HY + i * (S - HY) / 7f, wy, wc);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                if (y < HY) d.Set(x, y, Dith(ramp, (y - 8) * (N - 1) / (float)(HY - 8), x, y));
                else d.Set(x, y, Dith(sramp, (y - HY) * 7f / (S - HY), x, y));
            }
        var r = new System.Random(5);
        for (int i = 0; i < 18; i++)
        {
            int x = r.Next(12, 80), y = r.Next(10, 44);
            if ((x - 66) * (x - 66) + (y - 27) * (y - 27) < 150) continue;
            d.Set(x, y, r.NextDouble() > 0.6 ? C(0xffffff) : C(0xbcd6ff));
        }
        // moon crescent
        d.Ell(66, 27, 8, 8, C(0xf2f8ff));
        for (int y = 15; y < 40; y++)
            for (int x = 54; x < 80; x++)
            {
                float dx = x + 0.5f - 69.5f, dy = y + 0.5f - 24.5f;
                if (dx * dx + dy * dy <= 7.4f * 7.4f && Mathf.Pow(x + 0.5f - 66, 2) + Mathf.Pow(y + 0.5f - 27, 2) <= 64f)
                    d.Set(x, y, Dith(ramp, (y - 8) * (N - 1) / (float)(HY - 8), x, y));
            }
        d.Set(62, 29, C(0xc3d4ea)); d.Set(63, 29, C(0xc3d4ea));
        // light beams
        d.Poly(P(42, 28, -2, 18, -2, 38), C(0xfff2c8), 0.20f);
        d.Poly(P(50, 28, 94, 18, 94, 38), C(0xfff2c8), 0.20f);
        // lighthouse
        int lx = 46;
        for (int y = 36; y <= 64; y++)
        {
            float t = (y - 36) / 28f;
            float hw = 5.5f + 3.2f * t;
            bool cream = ((64 - y) / 7) % 2 == 0;
            for (int x = lx - 10; x <= lx + 10; x++)
            {
                float u = (x + 0.5f - lx) / hw;
                if (Mathf.Abs(u) > 1f) continue;
                Color col;
                if (cream) col = u < -0.2f ? C(0xf4ead2) : u < 0.45f ? C(0xcbbd9f) : C(0x8a8294);
                else col = u < -0.2f ? C(0x4f84b4) : u < 0.45f ? C(0x2a5a86) : C(0x1a3556);
                d.Set(x, y, col);
            }
        }
        d.Rect(lx - 8, 33, lx + 7, 35, C(0xb98a3a));
        d.Rect(lx - 8, 33, lx + 7, 33, C(0xf0c458));
        for (int x = lx - 7; x <= lx + 6; x += 2) d.Set(x, 32, C(0x2b1d26));
        d.Rect(lx - 4, 24, lx + 3, 31, C(0xffe9a8));
        d.Rect(lx - 2, 26, lx + 1, 30, C(0xffffff));
        d.Rect(lx - 3, 24, lx - 3, 31, C(0x7a5526));
        d.Rect(lx + 3, 24, lx + 3, 31, C(0x7a5526));
        d.Rect(lx - 5, 23, lx + 4, 23, C(0x2b1d26));
        d.Poly(P(lx - 5, 23, lx + 5, 23, lx, 13), C(0x7a2a2e));
        d.Poly(P(lx - 5, 23, lx, 23, lx, 13), C(0xb8454a));
        d.Set(lx, 11, C(0xf0c458)); d.Set(lx, 12, C(0xf0c458));
        d.Ell(lx, 28, 16, 13, C(0xffd36e), 0.10f);
        d.Ell(lx, 28, 9, 8, C(0xffd36e), 0.16f);
        // rocks
        d.Poly(P(20, 74, 26, 66, 32, 63, 38, 64, 54, 64, 60, 62, 66, 65, 72, 74), C(0x141c36));
        for (int x = 26; x < 66; x++) { int ty = x < 32 ? 66 - (x - 26) / 2 : (x < 38 ? 64 : (x < 54 ? 64 : 63)); d.Set(x, ty, C(0x3a5a88)); }
        // waves
        for (int x = 0; x < S; x++)
        {
            int yb = 70 + Mathf.RoundToInt(1.5f * Mathf.Sin(x * 0.45f));
            for (int y = yb; y < S; y++) d.Set(x, y, Lm(C(0x1d5f86), C(0x113a66), (y - yb) / 16f));
            d.Set(x, yb, C(0xcdf3f5));
            if (Hs(x, 1, 3) < 0.5f) d.Set(x, yb + 1, C(0x7fc9d8));
        }
        for (int x = 0; x < S; x++)
        {
            int yf = 78 + Mathf.RoundToInt(1.6f * Mathf.Sin(x * 0.38f + 2f));
            for (int y = yf; y < S; y++) d.Set(x, y, Lm(C(0x2f8aa4), C(0x15497a), (y - yf) / 12f));
            d.Set(x, yf, C(0xe8fbfc));
            if (x % 3 == 0) d.Set(x, yf + 1, C(0x9fe0ea));
        }

        var s = new Cv(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float rr = Mathf.Sqrt(dx * dx + dy * dy);
                if (rr <= 36.4f)
                {
                    Color col = d.Get(x, y);
                    float gx = x + 0.5f - (cx + 8f), gy = y + 0.5f - (cy + 10f);
                    if (gx * gx + gy * gy > 34f * 34f) col = Lm(col, Color.white, 0.10f);
                    s.Set(x, y, col);
                }
                else if (rr < 46f)
                {
                    Color col;
                    if (rr >= 45f) col = C(0x0b1020);
                    else if (rr < 37.2f) col = C(0x2e8fb5);
                    else if (rr < 38.2f) col = C(0x2b1d26);
                    else
                    {
                        float rm = (rr - 38.2f) / 6.8f;
                        float prof = Mathf.Abs(rm - 0.5f) * 2f;
                        float sgn = rm >= 0.5f ? 1f : -1f;
                        float ang = Mathf.Atan2(dy, dx);
                        float sh = sgn * Mathf.Cos(ang - Mathf.Atan2(-1f, -1f)) * prof + 0.12f * (1f - prof);
                        col = sh > 0.55f ? C(0xffe9a0) : sh > 0.18f ? C(0xf0c458) : sh > -0.25f ? C(0xd9a944) : sh > -0.6f ? C(0xb98a3a) : C(0x8a6428);
                    }
                    s.Set(x, y, col);
                }
            }
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI / 4f;
            float gx = cx + 41.6f * Mathf.Cos(a), gy = cy + 41.6f * Mathf.Sin(a);
            bool card = (k % 2) == 0;
            float rg = card ? 2.7f : 1.9f;
            s.Ell(gx, gy, rg + 1f, rg + 1f, C(0x0b1020));
            s.Ell(gx, gy, rg, rg, card ? C(0x4fc8e8) : C(0x8a6428));
            s.Set(Mathf.RoundToInt(gx - 1f), Mathf.RoundToInt(gy - 1f), card ? C(0xd8fbff) : C(0xffe9a0));
        }
        return s;
    }

    public static string Ui()
    {
        Save(Button(0x49d0c4, 0x1f9aa8, 0x136784), "Menu_Btn_Primary");
        Save(Button(0x6a7cf0, 0x3d4fc0, 0x27338a), "Menu_Btn_Blue");
        Save(Button(0x6b7c98, 0x46566f, 0x2e3a50), "Menu_Btn_Steel");
        Save(Button(0xe0606a, 0xa8343f, 0x6d1f2c), "Menu_Btn_Crimson");
        Save(Plate(), "Menu_Plate");
        Save(Logo(), "Menu_Logo");
        var bb = new Vector4(14, 12, 14, 12);
        Import("Menu_Btn_Primary", bb);
        Import("Menu_Btn_Blue", bb);
        Import("Menu_Btn_Steel", bb);
        Import("Menu_Btn_Crimson", bb);
        Import("Menu_Plate", new Vector4(34, 14, 34, 14));
        Import("Menu_Logo", Vector4.zero);
        return "ui ok";
    }


    static Cv Panel()
    {
        const int w = 40, h = 40;
        var s = new Cv(w, h);
        Color[] o1 = { C(0xffe9a0), C(0xf0c458), C(0xb98a3a), C(0x8a6428) };
        Color[] o2 = { C(0xf0c458), C(0xd9a944), C(0x8a6428), C(0x5a3b1e) };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int layer = -1;
                for (int i = 0; i <= 6; i++) if (PIn(x, y, 0, 0, w - 1, h - 1, i)) layer = i;
                if (layer < 0) continue;
                int dl = x - layer, dr = (w - 1 - layer) - x, dt = y - layer, db = (h - 1 - layer) - y;
                int side = 0, best = dt;
                if (dl < best) { best = dl; side = 1; }
                if (dr < best) { best = dr; side = 2; }
                if (db < best) { best = db; side = 3; }
                Color col;
                if (layer == 0) col = C(0x0b1020);
                else if (layer == 1) col = o1[side];
                else if (layer == 2) col = o2[side];
                else if (layer == 3) col = C(0x0d1630);
                else
                {
                    col = Lm(C(0x17325e), C(0x0a1430), (y - 4) / (float)(h - 9));
                    if (y == 4) col = Lm(col, Color.white, 0.12f);
                    if (layer == 5) col = Lm(col, C(0x2e8fb5), 0.45f);
                }
                s.Set(x, y, col);
            }
        Rivet(s, 8, 8); Rivet(s, w - 10, 8); Rivet(s, 8, h - 10); Rivet(s, w - 10, h - 10);
        return s;
    }
    public static string Ui2()
    {
        Save(Panel(), "Menu_Panel");
        Import("Menu_Panel", new Vector4(14, 14, 14, 14));
        return "panel ok";
    }

}
