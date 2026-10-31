#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.EditorTools
{
    /// <summary>
    /// Bakes the designed Floor 1 terrain (map.txt) into painted ground chunks (64 px / tile),
    /// builds the collision tilemap data and the prop list. Editor only.
    /// </summary>
    public static class AnimeWorldBaker
    {
        const int WATER = 0, GRASS = 1, FOREST = 2, SAND = 3, DIRT = 4, PAVE = 5, RIFT = 6, ASH = 7, LAVA = 8, CASTLE = 9,
                  CARPET = 10, PLANK = 11, WALL = 12, ROCK = 13, VOID = 14, FIELD = 15, NCLS = 16;

        const int PPT = 64, CH = 16, PAD = 1;
        const string Root = "Assets/_Game/Art/Environment/Anime/";
        const string OutDir = "Assets/_Game/Art/Environment/Anime/World/";

        public class PropData { public string k, layer; public float x, y; public int f, c; }
        public class MoveData { public string n; public float x, y; }

        public static List<PropData> Props = new List<PropData>();
        public static List<MoveData> Moves = new List<MoveData>();
        static int X0, Y0, W, H;
        static byte[] cls;
        static bool[] solid, treeSolid;
        static float[][] blur;
        static float[] occl, glow, wetW, depth;
        static int[][] near;
        static Tex grass, path, water, cobble, ashT, lavaT;
        static bool loaded;

        class Tex
        {
            public int w, h; public Color32[] p;
            public Vector3 At(int x, int y)
            {
                x = ((x % w) + w) % w; y = ((y % h) + h) % h; var c = p[y * w + x];
                return new Vector3(c.r / 255f, c.g / 255f, c.b / 255f);
            }
            public Vector3 Pt(float x, float y) { return At(Mathf.FloorToInt(x), Mathf.FloorToInt(y)); }
            public Vector3 Bil(float x, float y)
            {
                x -= 0.5f; y -= 0.5f;
                int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
                float fx = x - x0, fy = y - y0;
                var a = At(x0, y0); var b = At(x0 + 1, y0); var c = At(x0, y0 + 1); var d = At(x0 + 1, y0 + 1);
                return Vector3.Lerp(Vector3.Lerp(a, b, fx), Vector3.Lerp(c, d, fx), fy);
            }
        }

        static Tex LoadPieces(string name)
        {
            var t = new Tex { w = 512, h = 512, p = new Color32[512 * 512] };
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    var tex = new Texture2D(2, 2);
                    tex.LoadImage(File.ReadAllBytes(Path.GetFullPath(Root + name + "/" + name + "_" + i + j + ".png")));
                    var px = tex.GetPixels32();
                    for (int y = 0; y < 128; y++)
                        for (int x = 0; x < 128; x++)
                            t.p[(j * 128 + y) * 512 + i * 128 + x] = px[y * 128 + x];
                    UnityEngine.Object.DestroyImmediate(tex);
                }
            return t;
        }

        static Tex LoadSingle(string rel)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(Path.GetFullPath(Root + rel)));
            var t = new Tex { w = tex.width, h = tex.height, p = tex.GetPixels32() };
            UnityEngine.Object.DestroyImmediate(tex);
            return t;
        }

        static uint Hash(int x, int y, int s)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + s * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }
        static float HF(int x, int y, int s) { return (Hash(x, y, s) & 0xFFFFFF) / 16777216f; }
        static float VN(float x, float y, int s)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = HF(xi, yi, s), b = HF(xi + 1, yi, s), c = HF(xi, yi + 1, s), d = HF(xi + 1, yi + 1, s);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
        static float FBM(float x, float y, int s) { return 0.55f * VN(x, y, s) + 0.3f * VN(x * 2.1f, y * 2.1f, s + 1) + 0.15f * VN(x * 4.3f, y * 4.3f, s + 2); }
        static float SS(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }
        static Vector3 V(float r, float g, float b) { return new Vector3(r / 255f, g / 255f, b / 255f); }
        static Vector3 Mul(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
        static float F(string s) { return float.Parse(s, CultureInfo.InvariantCulture); }

        public static string Load(string txtPath)
        {
            var lines = File.ReadAllLines(Path.GetFullPath(txtPath));
            var hd = lines[0].Split(' ');
            X0 = int.Parse(hd[0]); Y0 = int.Parse(hd[1]); W = int.Parse(hd[2]); H = int.Parse(hd[3]);
            cls = new byte[W * H]; solid = new bool[W * H]; treeSolid = new bool[W * H];
            for (int j = 0; j < H; j++)
            {
                string row = lines[1 + j]; int i = 0, p = 0;
                while (p < row.Length)
                {
                    int c = row[p++] - 'a'; int n = 0;
                    while (p < row.Length && char.IsDigit(row[p])) n = n * 10 + (row[p++] - '0');
                    for (int q = 0; q < n; q++, i++)
                    {
                        bool woodsCell = c == 16; int cc = woodsCell ? FOREST : c;
                        int idx = j * W + i;
                        cls[idx] = (byte)cc;
                        treeSolid[idx] = cc == FOREST && !woodsCell;
                        solid[idx] = treeSolid[idx] || cc == WATER || cc == LAVA || cc == WALL || cc == ROCK || cc == VOID;
                    }
                }
            }
            Props.Clear(); Moves.Clear();
            for (int l = 1 + H; l < lines.Length; l++)
            {
                var t = lines[l].Split(' ');
                if (t.Length < 4) continue;
                if (t[0] == "P") Props.Add(new PropData { k = t[1], x = F(t[2]), y = F(t[3]), f = int.Parse(t[4]), c = int.Parse(t[5]), layer = t[6] });
                else if (t[0] == "M") Moves.Add(new MoveData { n = t[1], x = F(t[2]), y = F(t[3]) });
            }
            GenForestTrees();
            grass = LoadPieces("Grass"); path = LoadPieces("Path"); water = LoadPieces("Water"); cobble = LoadPieces("Cobble");
            ashT = LoadSingle("Pixel/Ash_base.png"); lavaT = LoadSingle("Pixel/Lava_base.png");
            Precompute();
            loaded = true;
            return "loaded " + W + "x" + H + " props=" + Props.Count;
        }

        static void Chamfer(float[] d)
        {
            for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)
                {
                    int n = j * W + i; float best = d[n]; if (best == 0) continue;
                    if (i > 0) best = Mathf.Min(best, d[n - 1] + 1); if (j > 0) best = Mathf.Min(best, d[n - W] + 1);
                    if (i > 0 && j > 0) best = Mathf.Min(best, d[n - W - 1] + 1.41f); if (i < W - 1 && j > 0) best = Mathf.Min(best, d[n - W + 1] + 1.41f);
                    d[n] = best;
                }
            for (int j = H - 1; j >= 0; j--) for (int i = W - 1; i >= 0; i--)
                {
                    int n = j * W + i; float best = d[n]; if (best == 0) continue;
                    if (i < W - 1) best = Mathf.Min(best, d[n + 1] + 1); if (j < H - 1) best = Mathf.Min(best, d[n + W] + 1);
                    if (i < W - 1 && j < H - 1) best = Mathf.Min(best, d[n + W + 1] + 1.41f); if (i > 0 && j < H - 1) best = Mathf.Min(best, d[n + W - 1] + 1.41f);
                    d[n] = best;
                }
        }

        static void GenForestTrees()
        {
            var d = new float[W * H];
            for (int n = 0; n < d.Length; n++) d[n] = treeSolid[n] ? 99f : 0f;
            Chamfer(d);
            string[] kinds = { "Tree_A", "Tree_B", "Tree_C" };
            int row = 0;
            for (float y = Y0 + 0.4f; y < Y0 + H - 0.2f; y += 1.0f, row++)
            {
                for (float x = X0 + ((row & 1) * 0.65f) + 0.3f; x < X0 + W; x += 1.3f)
                {
                    int hx = Mathf.FloorToInt(x * 10), hy = Mathf.FloorToInt(y * 10);
                    float px = x + (HF(hx, hy, 61) - 0.5f) * 0.7f, py = y + (HF(hx, hy, 62) - 0.5f) * 0.6f;
                    int i = Mathf.FloorToInt(px) - X0, j = Mathf.FloorToInt(py) - Y0;
                    if (i < 0 || j < 0 || i >= W || j >= H) continue;
                    int n = j * W + i;
                    if (!treeSolid[n] || d[n] > 6.5f) continue;
                    if (HF(hx, hy, 63) < 0.12f) continue;
                    Props.Add(new PropData { k = kinds[(int)(HF(hx, hy, 64) * 3f) % 3], x = px, y = py, f = HF(hx, hy, 65) < 0.5f ? 1 : 0, c = 0, layer = "forest" });
                }
            }
        }

        static float[] Blur(float[] src, int passes)
        {
            var a = (float[])src.Clone(); var b = new float[a.Length];
            for (int p = 0; p < passes; p++)
            {
                for (int j = 0; j < H; j++)
                    for (int i = 0; i < W; i++)
                    {
                        float l = a[j * W + Mathf.Max(0, i - 1)], c = a[j * W + i], r = a[j * W + Mathf.Min(W - 1, i + 1)];
                        b[j * W + i] = (l + 2 * c + r) * 0.25f;
                    }
                for (int j = 0; j < H; j++)
                    for (int i = 0; i < W; i++)
                    {
                        float d = b[Mathf.Max(0, j - 1) * W + i], c = b[j * W + i], u = b[Mathf.Min(H - 1, j + 1) * W + i];
                        a[j * W + i] = (d + 2 * c + u) * 0.25f;
                    }
            }
            return a;
        }

        static void Precompute()
        {
            blur = new float[NCLS][];
            for (int k = 0; k < NCLS; k++)
            {
                var ind = new float[W * H];
                for (int n = 0; n < ind.Length; n++) ind[n] = cls[n] == k ? 1f : 0f;
                blur[k] = Blur(ind, 2);
            }
            var o = new float[W * H]; var g = new float[W * H]; var wt = new float[W * H];
            for (int n = 0; n < o.Length; n++)
            {
                byte c = cls[n];
                o[n] = (treeSolid[n] || c == WALL || c == ROCK || c == VOID) ? 1f : 0f;
                g[n] = c == LAVA ? 1f : 0f;
                wt[n] = c == WATER ? 1f : 0f;
            }
            occl = Blur(o, 4); glow = Blur(g, 5); wetW = Blur(wt, 3);
            depth = new float[W * H];
            for (int n = 0; n < depth.Length; n++) depth[n] = cls[n] == WATER ? 99f : 0f;
            Chamfer(depth);
            near = new int[W * H][];
            var set = new List<int>();
            for (int j = 0; j < H; j++)
                for (int i = 0; i < W; i++)
                {
                    set.Clear();
                    for (int dj = -1; dj <= 1; dj++)
                        for (int di = -1; di <= 1; di++)
                        {
                            int ii = Mathf.Clamp(i + di, 0, W - 1), jj = Mathf.Clamp(j + dj, 0, H - 1);
                            int c = cls[jj * W + ii];
                            if (!set.Contains(c)) set.Add(c);
                        }
                    near[j * W + i] = set.ToArray();
                }
        }

        static float Samp(float[] f, float wx, float wy)
        {
            float x = wx - 0.5f - X0, y = wy - 0.5f - Y0;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            int xa = Mathf.Clamp(x0, 0, W - 1), xb = Mathf.Clamp(x0 + 1, 0, W - 1), ya = Mathf.Clamp(y0, 0, H - 1), yb = Mathf.Clamp(y0 + 1, 0, H - 1);
            return Mathf.Lerp(Mathf.Lerp(f[ya * W + xa], f[ya * W + xb], fx), Mathf.Lerp(f[yb * W + xa], f[yb * W + xb], fx), fy);
        }

        static Vector3 Paint(Tex t, float wx, float wy, float scale = 1f)
        {
            float u = wx * 128f * scale, v = wy * 128f * scale;
            var a = t.Bil(u, v);
            var b = t.Bil(v * 0.43f + 97f, u * 0.43f + 33f);
            float m = SS(0.38f, 0.62f, FBM(wx * 0.33f, wy * 0.33f, 31)) * 0.45f;
            return Vector3.Lerp(a, b, m);
        }

        static Vector3 PaveColor(float wx, float wy)
        {
            const float S = 0.52f;
            float px = wx / S, py = wy / S;
            int ix = Mathf.FloorToInt(px), iy = Mathf.FloorToInt(py);
            float f1 = 9, f2 = 9; int id = 0;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = ix + dx, cy = iy + dy;
                    float rx = cx + 0.15f + 0.7f * HF(cx, cy, 5), ry = cy + 0.15f + 0.7f * HF(cx, cy, 6);
                    float d = Mathf.Sqrt((px - rx) * (px - rx) + (py - ry) * (py - ry));
                    if (d < f1) { f2 = f1; f1 = d; id = cx * 7919 + cy; } else if (d < f2) f2 = d;
                }
            float edge = (f2 - f1) * S;
            float hv = HF(id, 3, 9);
            var stone = V(184, 176, 162) * (0.86f + 0.22f * hv);
            stone = Vector3.Lerp(stone, V(172, 170, 172), 0.25f * HF(id, 4, 9));
            stone *= 1f + 0.10f * (0.5f - Mathf.Clamp01(f1 * 0.9f));
            float mortar = 1f - SS(0.014f, 0.05f, edge);
            var col = Vector3.Lerp(stone, V(96, 90, 86), mortar);
            float dx0 = wx - 0.5f, dy0 = wy - 0.5f; float r = Mathf.Sqrt(dx0 * dx0 + dy0 * dy0);
            if (r < 7.2f)
            {
                float ang = Mathf.Atan2(dy0, dx0);
                if (r > 6.25f && r < 6.55f) col = Vector3.Lerp(col, V(206, 178, 108), 0.9f);
                if (r > 6.55f && r < 6.62f) col = Vector3.Lerp(col, V(96, 80, 60), 0.8f);
                if (r > 3.05f && r < 3.25f) col = Vector3.Lerp(col, V(104, 126, 160), 0.85f);
                if (r < 3.05f)
                {
                    int sect = Mathf.FloorToInt((ang + Mathf.PI) / (Mathf.PI * 2f) * 16f);
                    float shade = (sect % 2 == 0) ? 1.0f : 0.9f;
                    col *= shade;
                    if (r < 2.6f) col = Vector3.Lerp(col, V(198, 188, 170) * shade, 0.35f * (1f - mortar));
                    float spoke = Mathf.Abs(Mathf.Repeat(ang * 16f / (Mathf.PI * 2f), 1f) - 0.5f);
                    if (spoke > 0.47f && r > 0.9f) col = Vector3.Lerp(col, V(206, 178, 108), 0.75f);
                    if (r > 0.85f && r < 1.0f) col = Vector3.Lerp(col, V(206, 178, 108), 0.9f);
                    if (r <= 0.85f) col = Vector3.Lerp(col, V(122, 140, 176), 0.8f - mortar * 0.6f);
                }
            }
            return col;
        }

        static Vector3 CastleColor(float wx, float wy)
        {
            var c = Paint(cobble, wx, wy);
            c *= 1.02f;
            float lx = Mathf.Abs(Mathf.Repeat(wx * 0.5f + 0.5f, 1f) - 0.5f), ly = Mathf.Abs(Mathf.Repeat(wy * 0.5f + 0.5f, 1f) - 0.5f);
            if (lx > 0.488f || ly > 0.488f) c *= 0.78f;
            float dx = wx - 97.5f, dy = wy; float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r < 11.4f)
            {
                if (r > 10.35f && r < 10.6f) c = Vector3.Lerp(c, V(40, 46, 66), 0.85f);
                if (r > 9.9f && r < 10.05f) c = Vector3.Lerp(c, V(170, 150, 100), 0.8f);
                if (r > 4.0f && r < 4.25f) c = Vector3.Lerp(c, V(170, 150, 100), 0.85f);
                if (r < 4.0f)
                {
                    float ang = Mathf.Atan2(dy, dx);
                    float spoke = Mathf.Abs(Mathf.Repeat(ang * 8f / (Mathf.PI * 2f), 1f) - 0.5f);
                    c *= 0.82f;
                    if (spoke > 0.46f) c = Vector3.Lerp(c, V(170, 150, 100), 0.7f);
                    if (r < 0.9f) c = Vector3.Lerp(c, V(120, 40, 60), 0.6f);
                }
            }
            return c;
        }

        static Vector3 ClassColor(int k, float wx, float wy)
        {
            switch (k)
            {
                case GRASS:
                    {
                        var c = Paint(grass, wx, wy);
                        float n = FBM(wx * 0.11f, wy * 0.11f, 77);
                        c = Mul(c, new Vector3(0.93f + 0.14f * n, 0.95f + 0.10f * n, 0.92f + 0.08f * (1f - n)));
                        const float S = 0.2f;
                        int fx = Mathf.FloorToInt(wx / S), fy = Mathf.FloorToInt(wy / S);
                        float dens = SS(0.52f, 0.72f, FBM(wx * 0.16f, wy * 0.16f, 41)) * 0.42f + 0.006f;
                        if (HF(fx, fy, 11) < dens)
                        {
                            float cx = (fx + 0.25f + 0.5f * HF(fx, fy, 12)) * S, cy = (fy + 0.25f + 0.5f * HF(fx, fy, 13)) * S;
                            float d = Mathf.Sqrt((wx - cx) * (wx - cx) + (wy - cy) * (wy - cy)) * PPT;
                            int pal = (int)(HF(fx, fy, 14) * 4f);
                            Vector3 pc = pal == 0 ? V(250, 248, 240) : pal == 1 ? V(255, 224, 90) : pal == 2 ? V(250, 150, 190) : V(150, 190, 255);
                            if (d < 2.4f) c = Vector3.Lerp(c, pc, 0.95f);
                            if (d < 0.9f) c = V(255, 200, 60);
                        }
                        c *= 1f + 0.05f * (VN(wx * 0.5f, wy * 0.5f, 91) - 0.5f);
                        return c;
                    }
                case FOREST:
                    {
                        var c = Paint(grass, wx + 13f, wy);
                        c = Mul(c, V(120, 168, 150));
                        c *= 0.86f + 0.2f * FBM(wx * 0.6f, wy * 0.6f, 53);
                        const float S = 0.16f; int fx = Mathf.FloorToInt(wx / S), fy = Mathf.FloorToInt(wy / S);
                        if (HF(fx, fy, 21) < 0.06f) c = Vector3.Lerp(c, V(88, 76, 40), 0.55f);
                        return c;
                    }
                case SAND:
                    {
                        var c = Paint(path, wx, wy);
                        float wet = Mathf.Clamp01(Samp(wetW, wx, wy) * 1.6f);
                        c = Vector3.Lerp(c, Mul(c, V(184, 170, 150)), wet * 0.8f);
                        return c;
                    }
                case DIRT:
                    {
                        var c = Paint(path, wx, wy);
                        c = Mul(c, V(184, 142, 108));
                        c *= 0.92f + 0.14f * FBM(wx * 0.9f, wy * 0.9f, 63);
                        const float S = 0.21f; int fx = Mathf.FloorToInt(wx / S), fy = Mathf.FloorToInt(wy / S);
                        if (HF(fx, fy, 31) < 0.16f)
                        {
                            float cx = (fx + 0.3f + 0.4f * HF(fx, fy, 32)) * S, cy = (fy + 0.3f + 0.4f * HF(fx, fy, 33)) * S;
                            float d = Mathf.Sqrt((wx - cx) * (wx - cx) + (wy - cy) * (wy - cy)) * PPT;
                            float rad = 1.3f + 1.4f * HF(fx, fy, 34);
                            if (d < rad) c = Vector3.Lerp(c, V(128, 112, 98), 0.85f);
                        }
                        return c;
                    }
                case PAVE: return PaveColor(wx, wy);
                case RIFT:
                    {
                        var c = Paint(path, wx, wy);
                        c = Mul(c, V(118, 96, 84));
                        c *= 0.70f + 0.55f * FBM(wx * 0.35f, wy * 0.35f, 65);
                        c *= 0.88f + 0.24f * FBM(wx * 1.1f, wy * 1.1f, 66);
                        if (Mathf.Abs(VN(wx * 1.25f, wy * 1.25f, 67) - 0.5f) < 0.018f) c *= 0.55f;
                        const float S = 0.18f; int fx = Mathf.FloorToInt(wx / S), fy = Mathf.FloorToInt(wy / S);
                        if (HF(fx, fy, 36) < 0.10f)
                        {
                            float cx = (fx + 0.5f) * S, cy = (fy + 0.5f) * S;
                            float d = Mathf.Sqrt((wx - cx) * (wx - cx) + (wy - cy) * (wy - cy)) * PPT;
                            if (d < 2.0f) c = V(64, 58, 60);
                        }
                        return c;
                    }
                case ASH: return ashT.Pt(wx * 21f, wy * 21f) * 0.92f;
                case ROCK:
                    {
                        var c = ashT.Pt(wx * 21f + 5f, wy * 21f + 11f) * 0.62f;
                        c *= 0.8f + 0.4f * FBM(wx * 0.5f, wy * 0.5f, 71);
                        return c;
                    }
                case LAVA:
                    {
                        float s = 0.35f * Mathf.Sin(wx * 0.9f + wy * 1.3f);
                        return lavaT.Pt(wx * 21f, wy * 21f) * (1.0f + 0.08f * s);
                    }
                case CASTLE: return CastleColor(wx, wy);
                case CARPET:
                    {
                        var c = V(142, 22, 42);
                        float dx = Mathf.Abs(Mathf.Repeat(wx * 1.5f, 1f) - 0.5f), dy = Mathf.Abs(Mathf.Repeat(wy * 1.5f, 1f) - 0.5f);
                        if (dx + dy < 0.30f) c = V(168, 36, 58);
                        if (dx + dy < 0.12f) c = V(214, 176, 80);
                        float wc = Samp(blur[CARPET], wx, wy);
                        if (wc < 0.60f) c = V(110, 14, 30);
                        if (wc >= 0.60f && wc < 0.70f) c = V(214, 176, 80);
                        c *= 0.95f + 0.10f * VN(wx * 18f, wy * 18f, 3);
                        return c;
                    }
                case PLANK:
                    {
                        bool horizontal = wx < -19f;
                        float u = horizontal ? wy : wx, v = horizontal ? wx : wy;
                        float b = u * 5f; int id = Mathf.FloorToInt(b); float fr = b - id;
                        var c = V(156, 110, 66) * (0.85f + 0.3f * HF(id, 7, 8));
                        c *= 0.94f + 0.12f * VN(v * 7f, id * 3.1f, 2);
                        if (fr < 0.07f) c = V(78, 50, 30);
                        float vv = Mathf.Repeat(v, 1f);
                        if ((vv < 0.1f || vv > 0.9f) && fr > 0.4f && fr < 0.6f) c = V(60, 44, 32);
                        return c;
                    }
                case WALL:
                    {
                        float bx = wx / 0.5f, by = wy / 0.25f; int row = Mathf.FloorToInt(by);
                        if ((row & 1) == 1) bx += 0.5f;
                        int col = Mathf.FloorToInt(bx);
                        float fx = bx - col, fy = by - row;
                        var c = V(92, 98, 120) * (0.84f + 0.3f * HF(col, row, 17));
                        if (fx < 0.05f || fy < 0.10f) c = V(40, 44, 58);
                        else if (fy > 0.82f) c *= 1.12f;
                        c *= 0.8f + 0.25f * SS(0f, 1f, Mathf.Repeat(wy, 1f));
                        return c;
                    }
                case VOID: return V(11, 11, 18) * (0.8f + 0.5f * VN(wx * 3f, wy * 3f, 5));
                case FIELD:
                    {
                        float b = wy * 3.4f; int id = Mathf.FloorToInt(b); float fr = b - id;
                        var c = fr < 0.36f ? V(86, 58, 38) : V(128, 92, 62);
                        c *= 0.92f + 0.16f * VN(wx * 9f, wy * 9f, 14);
                        float bxx = Mathf.Repeat(wx * 3.4f, 1f);
                        if (fr > 0.45f && fr < 0.98f && bxx > 0.30f && bxx < 0.70f)
                        {
                            float tt = (fr - 0.45f) / 0.53f;
                            c = Vector3.Lerp(c, V(88, 156, 64) * (0.8f + 0.4f * tt), 0.9f);
                        }
                        return c;
                    }
                case WATER:
                    {
                        var c = Paint(water, wx * 0.8f, wy * 0.8f);
                        float d = Samp(depth, wx, wy);
                        c = Vector3.Lerp(c * 1.12f + V(14, 22, 18), c * 0.72f, SS(0.6f, 3.8f, d));
                        float rip = SS(0.80f, 0.92f, VN(wx * 2.6f, wy * 11f, 8)) * 0.16f;
                        c += new Vector3(rip, rip, rip);
                        return c;
                    }
            }
            return V(255, 0, 255);
        }

        static float Feather(int k)
        {
            switch (k)
            {
                case WALL: case VOID: case PLANK: case CARPET: case CASTLE: return 0.02f;
                case PAVE: return 0.05f;
                case FIELD: return 0.045f;
                default: return 0.085f;
            }
        }

        static Color32 Shade(float wx, float wy)
        {
            int ci = Mathf.Clamp(Mathf.FloorToInt(wx) - X0, 0, W - 1), cj = Mathf.Clamp(Mathf.FloorToInt(wy) - Y0, 0, H - 1);
            int[] nr = near[cj * W + ci];
            float n1 = FBM(wx * 2.1f, wy * 2.1f, 101) - 0.5f, n2 = VN(wx * 7f, wy * 7f, 202) - 0.5f;
            float w1 = -9, w2 = -9; int c1 = nr[0], c2 = nr[0];
            float nf = 1f; for (int q = 0; q < nr.Length; q++) { int kk = nr[q]; if (kk == WALL || kk == VOID || kk == CASTLE || kk == CARPET || kk == PLANK) nf = 0.1f; }
            n1 *= nf; n2 *= nf;
            for (int a = 0; a < nr.Length; a++)
            {
                int k = nr[a];
                float sg = ((k * 37) % 7) / 3.5f - 1f;
                float w = Samp(blur[k], wx, wy) + n1 * 0.55f * sg + n2 * 0.22f * ((k * 13) % 5 / 2f - 1f);
                if (w > w1) { w2 = w1; c2 = c1; w1 = w; c1 = k; } else if (w > w2) { w2 = w; c2 = k; }
            }
            Vector3 col;
            if (nr.Length == 1) col = ClassColor(c1, wx, wy);
            else
            {
                float f = Mathf.Max(Feather(c1), Feather(c2) * 0.6f);
                float t = SS(-f, f, w1 - w2);
                col = ClassColor(c1, wx, wy);
                if (t < 0.999f) col = Vector3.Lerp(ClassColor(c2, wx, wy), col, t);
                if ((c1 == WATER) != (c2 == WATER) && (c1 != PLANK && c2 != PLANK))
                {
                    float d = Mathf.Abs(w1 - w2);
                    float foam = Mathf.Pow(Mathf.Clamp01(1f - d / 0.26f), 1.6f) * (0.5f + 0.5f * VN(wx * 6f, wy * 6f, 9));
                    col = Vector3.Lerp(col, V(236, 248, 250), foam * 0.65f);
                }
            }
            if (c1 != WALL && c1 != VOID && c1 != ROCK)
            {
                float oc = Samp(occl, wx, wy);
                col *= 1f - 0.30f * SS(0.12f, 0.8f, oc);
            }
            if (c1 != LAVA)
            {
                float g = Samp(glow, wx, wy);
                col += new Vector3(1.0f, 0.42f, 0.08f) * (0.55f * SS(0.02f, 0.38f, g));
            }
            float tint = FBM(wx * 0.05f, wy * 0.05f, 303);
            col = Mul(col, new Vector3(0.97f + 0.06f * tint, 0.98f + 0.04f * tint, 1.01f - 0.04f * tint));
            return new Color32((byte)Mathf.Clamp(col.x * 255f, 0, 255), (byte)Mathf.Clamp(col.y * 255f, 0, 255), (byte)Mathf.Clamp(col.z * 255f, 0, 255), 255);
        }

        static void ShadowFor(string k, out float rx, out float ry, out float dx, out float dy, out float s)
        {
            rx = 0.5f; ry = 0.2f; dx = 0.1f; dy = 0f; s = 0.4f;
            if (k.StartsWith("House")) { rx = 1.9f; ry = 0.75f; dx = 0.22f; dy = 0.0f; s = 0.5f; }
            else if (k.StartsWith("Tree")) { rx = 1.0f; ry = 0.42f; dx = 0.32f; dy = 0.05f; s = 0.42f; }
            else if (k.StartsWith("Bush")) { rx = 0.62f; ry = 0.24f; dx = 0.12f; dy = 0.03f; s = 0.32f; }
            else if (k == "Lamp") { rx = 0.3f; ry = 0.12f; s = 0.3f; }
            else if (k == "Barrel" || k == "Crate") { rx = 0.5f; ry = 0.2f; s = 0.4f; }
            else if (k == "Sign") { rx = 0.35f; ry = 0.14f; s = 0.3f; }
        }

        public static string BakeRows(int cy0, int cy1)
        {
            if (!loaded) return "not loaded";
            Directory.CreateDirectory(Path.GetFullPath(OutDir));
            int nx = Mathf.CeilToInt(W / (float)CH);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int count = 0;
            for (int cy = cy0; cy < cy1; cy++)
                for (int cx = 0; cx < nx; cx++) { BakeChunk(cx, cy); count++; }
            return "baked " + count + " chunks in " + sw.ElapsedMilliseconds + " ms";
        }

        public static string BakeOne(int cx, int cy)
        {
            if (!loaded) return "not loaded";
            Directory.CreateDirectory(Path.GetFullPath(OutDir));
            var sw = System.Diagnostics.Stopwatch.StartNew();
            BakeChunk(cx, cy);
            return "baked " + cx + "," + cy + " in " + sw.ElapsedMilliseconds + " ms";
        }

        static void BakeChunk(int cx, int cy)
        {
            int size = CH * PPT + 2 * PAD;
            var pix = new Color32[size * size];
            float tx0 = X0 + cx * CH, ty0 = Y0 + cy * CH;
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float wx = tx0 + (px - PAD + 0.5f) / PPT, wy = ty0 + (py - PAD + 0.5f) / PPT;
                    pix[py * size + px] = Shade(wx, wy);
                }
            var alpha = new float[size * size];
            float minX = tx0 - 3, maxX = tx0 + CH + 3, minY = ty0 - 3, maxY = ty0 + CH + 3;
            foreach (var p in Props)
            {
                if (p.x < minX || p.x > maxX || p.y < minY || p.y > maxY) continue;
                float rx, ry, dx, dy, s;
                ShadowFor(p.k, out rx, out ry, out dx, out dy, out s);
                float ex = p.x + dx, ey = p.y + dy;
                int x0 = Mathf.Max(0, Mathf.FloorToInt((ex - rx - tx0) * PPT) + PAD), x1 = Mathf.Min(size - 1, Mathf.CeilToInt((ex + rx - tx0) * PPT) + PAD);
                int y0 = Mathf.Max(0, Mathf.FloorToInt((ey - ry - ty0) * PPT) + PAD), y1 = Mathf.Min(size - 1, Mathf.CeilToInt((ey + ry - ty0) * PPT) + PAD);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float wx = tx0 + (x - PAD + 0.5f) / PPT, wy = ty0 + (y - PAD + 0.5f) / PPT;
                        float ux = (wx - ex) / rx, uy = (wy - ey) / ry;
                        float r2 = ux * ux + uy * uy;
                        if (r2 >= 1f) continue;
                        float a = s * (1f - r2) * (1f - r2) * 1.4f;
                        if (a > alpha[y * size + x]) alpha[y * size + x] = Mathf.Min(a, s);
                    }
            }
            for (int i = 0; i < pix.Length; i++)
            {
                float a = alpha[i];
                if (a <= 0.001f) continue;
                var c = pix[i];
                float m = 1f - a;
                pix[i] = new Color32((byte)(c.r * m * 0.98f), (byte)(c.g * m * 0.98f), (byte)Mathf.Min(255f, c.b * m * 1.02f), 255);
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pix);
            File.WriteAllBytes(Path.GetFullPath(OutDir + "Ground_" + cx + "_" + cy + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        public static int ChunksX { get { return Mathf.CeilToInt(W / (float)CH); } }
        public static int ChunksY { get { return Mathf.CeilToInt(H / (float)CH); } }
        public static Vector2 ChunkCenter(int cx, int cy)
        {
            return new Vector2(X0 + cx * CH + CH * 0.5f, Y0 + cy * CH + CH * 0.5f);
        }

        public static string ImportChunks()
        {
            AssetDatabase.Refresh();
            int n = 0;
            for (int cy = 0; cy < ChunksY; cy++)
                for (int cx = 0; cx < ChunksX; cx++)
                {
                    string p = OutDir + "Ground_" + cx + "_" + cy + ".png";
                    var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                    if (imp == null) continue;
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.spritePixelsPerUnit = PPT;
                    imp.filterMode = FilterMode.Bilinear;
                    imp.mipmapEnabled = false;
                    imp.wrapMode = TextureWrapMode.Clamp;
                    imp.maxTextureSize = 2048;
                    imp.textureCompression = TextureImporterCompression.Compressed;
                    imp.alphaIsTransparency = false;
                    imp.SaveAndReimport();
                    n++;
                }
            return "imported " + n;
        }

        public static Sprite BlockerSprite()
        {
            string p = OutDir + "Blocker.png";
            if (!File.Exists(Path.GetFullPath(p)))
            {
                var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color32[16]; for (int i = 0; i < 16; i++) px[i] = new Color32(0, 0, 0, 0);
                t.SetPixels32(px);
                File.WriteAllBytes(Path.GetFullPath(p), t.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(p);
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                imp.textureType = TextureImporterType.Sprite; imp.spritePixelsPerUnit = 4; imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }

        public static Tile BlockerTile()
        {
            string p = OutDir + "Tile_Blocker.asset";
            var t = AssetDatabase.LoadAssetAtPath<Tile>(p);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = BlockerSprite();
                t.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(t, p);
            }
            return t;
        }

        public static string PaintCollision(Tilemap tm)
        {
            tm.ClearAllTiles();
            var tile = BlockerTile();
            var pos = new List<Vector3Int>(); var tiles = new List<TileBase>();
            for (int j = 0; j < H; j++)
                for (int i = 0; i < W; i++)
                {
                    bool s = solid[j * W + i] || i == 0 || j == 0 || i == W - 1 || j == H - 1;
                    if (!s) continue;
                    pos.Add(new Vector3Int(X0 + i, Y0 + j, 0)); tiles.Add(tile);
                }
            tm.SetTiles(pos.ToArray(), tiles.ToArray());
            return "solid cells " + pos.Count;
        }

        public static int GetClass(float x, float y)
        {
            int i = Mathf.FloorToInt(x) - X0, j = Mathf.FloorToInt(y) - Y0;
            if (i < 0 || j < 0 || i >= W || j >= H) return VOID;
            return cls[j * W + i];
        }
        public static bool IsSolid(float x, float y)
        {
            int i = Mathf.FloorToInt(x) - X0, j = Mathf.FloorToInt(y) - Y0;
            if (i < 0 || j < 0 || i >= W || j >= H) return true;
            return solid[j * W + i];
        }
    }
}
#endif
