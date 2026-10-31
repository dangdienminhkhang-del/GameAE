using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Game.Core.Floor;
using Game.PvE.Enemy;
using Game.PvE.Floor;
using Game.PvE.Floor03;

namespace Game.EditorTools.F03
{
    /// <summary>
    /// Dựng toàn bộ tầng 3 "Zumfut và Rừng Cổ Thụ": art pixel 32x32, tile, EnemyDefinition (x5 so với tầng 2),
    /// prefab spawner, FloorDefinition + đăng ký FloorDatabase, scene Floor03 (nhân bản khung từ Floor02) + Build Settings.
    /// Chạy lại được nhiều lần (ghi đè phần đã sinh). Menu: Game/Floor 03/Build All.
    /// </summary>
    public static class Floor03Builder
    {
        const string Root = "Assets/_Game";
        const string DirChar = Root + "/Art/Characters/Enemies/Floor03";
        const string DirTiles = Root + "/Art/Environment/Floor03";
        const string DirEnemyData = Root + "/Data/Enemies/Floor03";
        const string DirPrefabs = Root + "/Prefabs/Floor03";
        const string DirFloorDef = Root + "/Content/Floors/Floor03";
        const string FloorDefPath = DirFloorDef + "/FloorDefinition_Floor03.asset";
        const string FloorDefSrc = Root + "/Content/Floors/Floor02/FloorDefinition_Floor02.asset";
        const string DbPath = Root + "/Data/Databases/FloorDatabase.asset";
        const string EnemyPrefabPath = Root + "/Prefabs/Enemy.prefab";
        const string SceneSrc = Root + "/Scenes/Floor02.unity";
        const string ScenePath = Root + "/Scenes/Floor03.unity";
        const int OriginX = 20000;
        const int FloorNo = 3;

        sealed class Built
        {
            public Dictionary<string, Sprite> Monsters;
            public Tile[] Ground;          // [biome * 4 + variant]
            public Tile[] Props;           // [Prop]
            public Tile Block;
            public Dictionary<string, EnemyDefinition> Defs;
            public EnemySpawner[] BiomeSpawners;
            public List<string> BossKeys = new List<string>();
            public List<EnemySpawner> BossSpawners = new List<EnemySpawner>();
            public FloorDefinition Floor;
        }

        // ═════════════════════════ Menu

        [MenuItem("Game/Floor 03/Build All")]
        public static void BuildAll()
        {
            var b = new Built();
            try
            {
                EditorUtility.DisplayProgressBar("Floor 03", "Sprite quái", 0.05f);
                b.Monsters = BuildMonsterSprites();
                EditorUtility.DisplayProgressBar("Floor 03", "Tile và vật trang trí", 0.25f);
                BuildTiles(b);
                EditorUtility.DisplayProgressBar("Floor 03", "EnemyDefinition", 0.45f);
                BuildEnemies(b);
                EditorUtility.DisplayProgressBar("Floor 03", "Spawner prefab", 0.60f);
                BuildSpawners(b);
                EditorUtility.DisplayProgressBar("Floor 03", "FloorDefinition", 0.75f);
                BuildFloorDefinition(b);
                EditorUtility.DisplayProgressBar("Floor 03", "Scene Floor03", 0.85f);
                BuildScene(b);
                AddToBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[Floor03] HOÀN TẤT: " + b.Monsters.Count + " sprite quái, " + b.Defs.Count + " EnemyDefinition, "
                          + (b.BossSpawners.Count + b.BiomeSpawners.Count(s => s != null)) + " spawner prefab, scene " + ScenePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[Floor03] LỖI khi dựng: " + e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ═════════════════════════ Tiện ích

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void WritePng(string assetPath, Texture2D tex)
        {
            string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        static void ConfigureSprite(string path, int ppu, Vector2 pivot)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) throw new Exception("Không lấy được TextureImporter: " + path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            imp.SetTextureSettings(s);
            imp.spritePivot = pivot;
            imp.SaveAndReimport();
        }

        static SerializedProperty P(SerializedObject so, string name)
        {
            var p = so.FindProperty(name);
            if (p == null) throw new Exception("Thiếu trường '" + name + "' trên " + so.targetObject.GetType().Name);
            return p;
        }

        static void SetNum(SerializedObject so, string name, float v)
        {
            var p = so.FindProperty(name);
            if (p == null) { Debug.LogWarning("[Floor03] Bỏ qua trường không có: " + name); return; }
            if (p.propertyType == SerializedPropertyType.Integer) p.intValue = Mathf.RoundToInt(v);
            else p.floatValue = v;
        }

        // ═════════════════════════ 1) Sprite quái từ dữ liệu RLE

        static Texture2D Decode(Floor03SpriteData.S s, string key)
        {
            string[] hex = s.Pal.Split(',');
            var pal = new Color32[hex.Length];
            for (int i = 0; i < hex.Length; i++)
            {
                pal[i] = new Color32(Convert.ToByte(hex[i].Substring(0, 2), 16), Convert.ToByte(hex[i].Substring(2, 2), 16),
                    Convert.ToByte(hex[i].Substring(4, 2), 16), Convert.ToByte(hex[i].Substring(6, 2), 16));
            }

            var top = new Color32[s.W * s.H];
            int n = 0, pos = 0;
            string d = s.Rle;
            while (pos < d.Length)
            {
                char ch = d[pos++];
                int run = 0;
                while (pos < d.Length && char.IsDigit(d[pos])) run = run * 10 + (d[pos++] - '0');
                if (run == 0) run = 1;
                Color32 col = new Color32(0, 0, 0, 0);
                if (ch != '.')
                {
                    int pi = Floor03SpriteData.Symbols.IndexOf(ch);
                    if (pi >= 0 && pi < pal.Length) col = pal[pi];
                    else Debug.LogWarning("[Floor03] Sprite " + key + ": ký hiệu màu lạ '" + ch + "'");
                }
                for (int k = 0; k < run && n < top.Length; k++) top[n++] = col;
            }
            if (n != top.Length) Debug.LogWarning("[Floor03] Sprite " + key + ": giải mã được " + n + "/" + top.Length + " điểm ảnh");

            var flipped = new Color32[top.Length];
            for (int y = 0; y < s.H; y++) Array.Copy(top, (s.H - 1 - y) * s.W, flipped, y * s.W, s.W);
            var tex = new Texture2D(s.W, s.H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels32(flipped);
            tex.Apply();
            return tex;
        }

        static Dictionary<string, Sprite> BuildMonsterSprites()
        {
            EnsureFolder(DirChar);
            var result = new Dictionary<string, Sprite>();
            foreach (var kv in Floor03SpriteData.All)
            {
                string path = DirChar + "/Enemy_F3_" + kv.Key + ".png";
                Texture2D tex = Decode(kv.Value, kv.Key);
                WritePng(path, tex);
                UnityEngine.Object.DestroyImmediate(tex);
                ConfigureSprite(path, 32, new Vector2(0.5f, 0.5f));
                result[kv.Key] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return result;
        }

        // ═════════════════════════ 2) Tile và vật trang trí (vẽ pixel art bằng code)

        static Color32 C(int r, int g, int b) { return new Color32((byte)r, (byte)g, (byte)b, 255); }
        static float Hs(int x, int y, int s) { return Floor03World.Hash(x, y, s); }

        struct Style
        {
            public Color32 A, B, Light, Dark, Acc;
            public int Decor;     // 0 chấm, 1 sóng nước, 2 cỏ, 3 bùn, 4 vết nứt đá, 5 tầng đất
        }

        static Style St(int decor, Color32 a, Color32 b, Color32 light, Color32 dark, Color32 acc)
        {
            return new Style { Decor = decor, A = a, B = b, Light = light, Dark = dark, Acc = acc };
        }

        static Style[] Styles()
        {
            return new[]
            {
                St(1, C(30, 66, 122), C(24, 54, 104), C(40, 84, 146), C(70, 120, 180), C(120, 170, 220)),     // DeepWater
                St(1, C(66, 128, 176), C(56, 114, 162), C(84, 148, 196), C(150, 200, 230), C(190, 225, 245)), // ShallowWater
                St(0, C(222, 208, 156), C(206, 190, 138), C(236, 224, 176), C(180, 160, 110), C(160, 140, 96)), // Beach
                St(2, C(146, 194, 96), C(128, 176, 82), C(164, 210, 112), C(110, 150, 70), C(88, 128, 58)),   // Plains
                St(2, C(78, 148, 76), C(64, 128, 64), C(94, 166, 88), C(50, 104, 52), C(40, 88, 44)),         // Forest
                St(2, C(40, 106, 62), C(32, 90, 52), C(52, 124, 72), C(24, 72, 42), C(18, 60, 34)),           // GiantForest
                St(2, C(62, 56, 104), C(52, 46, 92), C(76, 70, 122), C(40, 34, 72), C(130, 108, 190)),        // DarkForest
                St(3, C(96, 120, 84), C(82, 104, 74), C(110, 136, 96), C(60, 84, 70), C(132, 172, 112)),      // Swamp
                St(4, C(166, 156, 114), C(150, 140, 100), C(182, 172, 130), C(120, 112, 84), C(104, 96, 70)), // Hills
                St(4, C(128, 124, 130), C(112, 108, 116), C(146, 142, 150), C(88, 84, 94), C(70, 66, 78)),    // Mountain
                St(0, C(236, 242, 248), C(218, 228, 240), C(250, 252, 255), C(190, 204, 226), C(170, 190, 220)), // Snow
                St(5, C(192, 110, 72), C(172, 96, 62), C(208, 128, 86), C(140, 74, 48), C(120, 62, 40)),      // Canyon
            };
        }

        static void Put(Color32[] px, int x, int y, Color32 c)
        {
            if (x >= 0 && y >= 0 && x < 32 && y < 32) px[y * 32 + x] = c;
        }

        static Texture2D PaintTile(Style st, int variant, int biome)
        {
            const int S = 32;
            var px = new Color32[S * S];
            int seed = 4000 + biome * 131 + variant * 17;
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float h = Hs(x, y, seed);
                    px[y * S + x] = h < .58f ? st.A : h < .80f ? st.B : h < .94f ? st.Light : st.Dark;
                }
            }

            switch (st.Decor)
            {
                case 1:                                                      // sóng nước
                    for (int k = 0; k < 4; k++)
                    {
                        int wy = 3 + k * 8 + (int)(Hs(k, variant, seed + 5) * 3f);
                        int wx = (int)(Hs(k, variant, seed + 6) * 22f);
                        int len = 5 + (int)(Hs(k, variant, seed + 7) * 6f);
                        for (int i = 0; i < len; i++) Put(px, (wx + i) & 31, wy & 31, st.Acc);
                    }
                    break;
                case 2:                                                      // chồi cỏ / lá rụng
                    for (int k = 0; k < 6; k++)
                    {
                        int tx = 1 + (int)(Hs(k, variant, seed + 1) * 29f), ty = 1 + (int)(Hs(k, variant, seed + 2) * 27f);
                        Put(px, tx, ty, st.Acc);
                        Put(px, tx, ty + 1, st.Acc);
                        Put(px, tx + 1, ty + 2, st.Acc);
                        Put(px, tx - 1, ty + 1, st.Light);
                    }
                    break;
                case 3:                                                      // mảng bùn
                    for (int k = 0; k < 3; k++)
                    {
                        int cx = (int)(Hs(k, variant, seed + 1) * 32f), cy = (int)(Hs(k, variant, seed + 2) * 32f);
                        int r = 3 + (int)(Hs(k, variant, seed + 3) * 3f);
                        for (int y = -r; y <= r; y++)
                            for (int x = -r; x <= r; x++)
                                if (x * x + y * y <= r * r) Put(px, (cx + x) & 31, (cy + y) & 31, st.Dark);
                        Put(px, (cx + 1) & 31, (cy + 1) & 31, st.Acc);
                    }
                    break;
                case 4:                                                      // vết nứt đá
                    for (int k = 0; k < 3; k++)
                    {
                        int cx = 2 + (int)(Hs(k, variant, seed + 1) * 27f), cy = 2 + (int)(Hs(k, variant, seed + 2) * 27f);
                        for (int step = 0; step < 9; step++)
                        {
                            Put(px, cx, cy, st.Acc);
                            cx += (int)(Hs(step, k + variant * 3, seed + 3) * 3f) - 1;
                            cy += (int)(Hs(step, k + variant * 3, seed + 4) * 3f) - 1;
                        }
                    }
                    break;
                case 5:                                                      // tầng đất hẻm đá đỏ
                    for (int y = 0; y < S; y++)
                        if (((y / 4) + variant) % 3 == 0)
                            for (int x = 0; x < S; x++)
                                if (Hs(x, y, seed + 9) < .55f) Put(px, x, y, st.Acc);
                    break;
            }

            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        sealed class Cvs
        {
            public readonly int W, H;
            public readonly Color32[] Px;
            public Cvs(int w, int h) { W = w; H = h; Px = new Color32[w * h]; }

            public void Set(int x, int y, Color32 c)
            {
                if (x >= 0 && y >= 0 && x < W && y < H) Px[y * W + x] = c;
            }

            public void Rect(int x0, int y0, int x1, int y1, Color32 c)
            {
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++) Set(x, y, c);
            }

            public void Ell(float cx, float cy, float rx, float ry, Color32 c)
            {
                for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                {
                    for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                    {
                        float dx = (x + .5f - cx) / rx, dy = (y + .5f - cy) / ry;
                        if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                    }
                }
            }

            public void Tri(float ax, float ay, float bx, float by, float cx, float cy, Color32 c)
            {
                int x0 = Mathf.FloorToInt(Mathf.Min(ax, Mathf.Min(bx, cx))), x1 = Mathf.CeilToInt(Mathf.Max(ax, Mathf.Max(bx, cx)));
                int y0 = Mathf.FloorToInt(Mathf.Min(ay, Mathf.Min(by, cy))), y1 = Mathf.CeilToInt(Mathf.Max(ay, Mathf.Max(by, cy)));
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        float px = x + .5f, py = y + .5f;
                        float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
                        float d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
                        float d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
                        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                        if (!(neg && pos)) Set(x, y, c);
                    }
                }
            }

            public Texture2D Finish()
            {
                var outl = new Color32(14, 12, 22, 255);
                var q = (Color32[])Px.Clone();
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        if (Px[y * W + x].a == 0) continue;
                        bool edge = x == 0 || y == 0 || x == W - 1 || y == H - 1
                                    || Px[y * W + x - 1].a == 0 || Px[y * W + x + 1].a == 0
                                    || Px[(y - 1) * W + x].a == 0 || Px[(y + 1) * W + x].a == 0;
                        if (edge) q[y * W + x] = outl;
                    }
                }
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.SetPixels32(q);
                tex.Apply();
                return tex;
            }
        }

        static Texture2D PaintProp(Prop p)
        {
            Color32 trunk = C(110, 76, 46), trunkD = C(80, 54, 34);
            switch (p)
            {
                case Prop.Tree:
                {
                    var v = new Cvs(32, 48);
                    Color32 a = C(66, 142, 70), b = C(46, 108, 56), l = C(104, 176, 92);
                    v.Rect(14, 0, 17, 13, trunkD); v.Rect(14, 0, 15, 13, trunk);
                    v.Ell(16, 28, 13, 12, b); v.Ell(10, 25, 8, 8, a); v.Ell(22, 25, 8, 8, a); v.Ell(16, 34, 10, 9, a); v.Ell(12, 36, 5, 4, l);
                    return v.Finish();
                }
                case Prop.GiantTree:
                {
                    var v = new Cvs(48, 64);
                    Color32 a = C(38, 108, 64), b = C(26, 80, 48), l = C(70, 146, 92);
                    v.Rect(20, 0, 27, 20, trunkD); v.Rect(20, 0, 23, 20, trunk);
                    v.Tri(20, 0, 14, 0, 20, 8, trunkD); v.Tri(27, 0, 33, 0, 27, 8, trunkD);
                    v.Ell(24, 42, 20, 17, b); v.Ell(14, 38, 12, 11, a); v.Ell(34, 38, 12, 11, a); v.Ell(24, 50, 14, 12, a);
                    v.Ell(18, 54, 6, 5, l); v.Ell(30, 46, 4, 3, l);
                    return v.Finish();
                }
                case Prop.Pine:
                {
                    var v = new Cvs(32, 48);
                    Color32 a = C(76, 66, 128), b = C(52, 44, 96), l = C(110, 98, 170);
                    v.Rect(14, 0, 17, 8, trunkD);
                    v.Tri(16, 24, 1, 5, 31, 5, b); v.Tri(16, 36, 3, 16, 29, 16, a); v.Tri(16, 46, 5, 28, 27, 28, a);
                    v.Tri(16, 46, 5, 28, 16, 28, l); v.Tri(16, 36, 3, 16, 16, 16, l);
                    return v.Finish();
                }
                case Prop.Rock:
                {
                    var v = new Cvs(32, 24);
                    v.Ell(16, 9, 14, 9, C(100, 98, 106)); v.Ell(15, 11, 11, 7, C(130, 128, 134)); v.Ell(11, 14, 4, 2, C(170, 168, 176));
                    return v.Finish();
                }
                case Prop.Peak:
                {
                    var v = new Cvs(32, 40);
                    v.Tri(16, 38, 1, 2, 31, 2, C(96, 92, 100)); v.Tri(16, 38, 1, 2, 16, 2, C(128, 124, 130)); v.Tri(16, 38, 11, 26, 21, 26, C(160, 156, 164));
                    return v.Finish();
                }
                case Prop.SnowPeak:
                {
                    var v = new Cvs(32, 40);
                    v.Tri(16, 38, 1, 2, 31, 2, C(170, 186, 214)); v.Tri(16, 38, 1, 2, 16, 2, C(214, 224, 240)); v.Tri(16, 38, 10, 26, 22, 26, C(250, 252, 255));
                    return v.Finish();
                }
                case Prop.Pillar:
                {
                    var v = new Cvs(32, 40);
                    v.Rect(8, 0, 23, 32, C(176, 92, 60));
                    for (int y = 4; y < 32; y += 6) v.Rect(8, y, 23, y + 1, C(140, 70, 46));
                    v.Rect(8, 0, 11, 32, C(208, 128, 86));
                    v.Ell(15.5f, 32, 8, 4, C(208, 128, 86));
                    return v.Finish();
                }
                default:                                                     // Reed
                {
                    var v = new Cvs(32, 32);
                    int[] xs = { 6, 11, 16, 21, 26 }, hs = { 14, 22, 18, 26, 16 };
                    for (int i = 0; i < xs.Length; i++)
                    {
                        v.Rect(xs[i], 0, xs[i] + 1, hs[i], C(92, 130, 70));
                        v.Rect(xs[i], hs[i] - 3, xs[i] + 1, hs[i], C(140, 100, 60));
                    }
                    return v.Finish();
                }
            }
        }

        static Tile MakeTile(string path, Sprite sprite, Tile.ColliderType ct)
        {
            var t = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(t, path);
            }
            t.sprite = sprite;
            t.colliderType = ct;
            EditorUtility.SetDirty(t);
            return t;
        }

        static void BuildTiles(Built b)
        {
            EnsureFolder(DirTiles + "/Ground");
            EnsureFolder(DirTiles + "/Props");
            EnsureFolder(DirTiles + "/TileAssets");
            Style[] styles = Styles();
            b.Ground = new Tile[styles.Length * 4];
            for (int biome = 0; biome < styles.Length; biome++)
            {
                for (int v = 0; v < 4; v++)
                {
                    string name = ((Biome)biome).ToString() + "_" + v;
                    string png = DirTiles + "/Ground/T_" + name + ".png";
                    Texture2D tex = PaintTile(styles[biome], v, biome);
                    WritePng(png, tex);
                    UnityEngine.Object.DestroyImmediate(tex);
                    ConfigureSprite(png, 32, new Vector2(0.5f, 0.5f));
                    b.Ground[biome * 4 + v] = MakeTile(DirTiles + "/TileAssets/Tile_" + name + ".asset",
                        AssetDatabase.LoadAssetAtPath<Sprite>(png), Tile.ColliderType.None);
                }
            }

            int propCount = Enum.GetValues(typeof(Prop)).Length;
            b.Props = new Tile[propCount];
            for (int pi = 1; pi < propCount; pi++)
            {
                var prop = (Prop)pi;
                string png = DirTiles + "/Props/P_" + prop + ".png";
                Texture2D tex = PaintProp(prop);
                WritePng(png, tex);
                UnityEngine.Object.DestroyImmediate(tex);
                ConfigureSprite(png, 32, new Vector2(0.5f, 0f));
                b.Props[pi] = MakeTile(DirTiles + "/TileAssets/Tile_Prop_" + prop + ".asset",
                    AssetDatabase.LoadAssetAtPath<Sprite>(png), Tile.ColliderType.None);
            }

            // Ô chặn: không có hình, chỉ có collider theo lưới. Đặt trên tilemap Walls.
            b.Block = MakeTile(DirTiles + "/TileAssets/Tile_Block.asset", null, Tile.ColliderType.Grid);
        }

        // ═════════════════════════ 3) EnemyDefinition (chỉ số = x5 tầng 2, chỉ thêm dữ liệu)

        struct Arch { public float Hp, Atk, Def, Exp; }

        // Số GỐC của các loại quái tầng 2 (đọc từ Enemy_*.asset): BoilerGuard, Bat, WireStalker, PipeSpitter, PressureWarden.
        static readonly Dictionary<string, Arch> Archs = new Dictionary<string, Arch>
        {
            { "trash", new Arch { Hp = 70, Atk = 13, Def = 4, Exp = 14 } },
            { "flyer", new Arch { Hp = 40, Atk = 12, Def = 1, Exp = 14 } },
            { "skirm", new Arch { Hp = 230, Atk = 11, Def = 1, Exp = 14 } },
            { "ranged", new Arch { Hp = 240, Atk = 14, Def = 1, Exp = 14 } },
            { "elite", new Arch { Hp = 300, Atk = 18, Def = 6, Exp = 36 } },
            { "mini", new Arch { Hp = 1800, Atk = 26, Def = 12, Exp = 180 } },
        };

        struct Mon
        {
            public string Key, Name, Arch, Role;
            public float Hp, Atk, Def, Spd;
            public int Lv;
        }

        static Mon Mn(string key, string name, string arch, float hp, float atk, float def, float spd, string role, int lv)
        {
            return new Mon { Key = key, Name = name, Arch = arch, Hp = hp, Atk = atk, Def = def, Spd = spd, Role = role, Lv = lv };
        }

        static readonly Mon[] Roster =
        {
            Mn("dire_wolf", "Lang Rừng", "skirm", .8f, 1.1f, 1f, 2.8f, "Skirmisher", 11),
            Mn("frost_wolf", "Lang Tuyết", "skirm", .9f, 1.2f, 1.5f, 2.8f, "Skirmisher", 13),
            Mn("frenzy_boar", "Heo Rừng Cuồng Nộ", "trash", 2f, 1.3f, 1.5f, 2.2f, "Chaser", 11),
            Mn("nepent", "Nepent Chồi Non", "ranged", .8f, .9f, 1f, 1.2f, "Ranged", 11),
            Mn("hornet", "Ong Rừng Khổng Lồ", "flyer", 1f, 1.2f, 1f, 3f, "Chaser", 12),
            Mn("weaver", "Nhện Dệt Độc", "ranged", .9f, 1f, 1f, 1.6f, "Ranged", 12),
            Mn("sapling", "Cây Quỷ Non", "elite", .8f, .9f, 1.2f, 1.3f, "Chaser", 14),
            Mn("moss_golem", "Golem Rêu Đá", "elite", 1.6f, 1f, 2f, 1.2f, "Chaser", 14),
            Mn("wisp", "Hồn Rừng", "ranged", .5f, 1.3f, .5f, 2f, "Ranged", 12),
            Mn("bat", "Dơi Hang Hút Máu", "flyer", 1f, 1f, 1f, 3f, "Chaser", 11),
            Mn("kobold", "Kobold Gác Phế Tích", "trash", 1.2f, 1.1f, 1.2f, 2.2f, "Chaser", 12),
            Mn("scorpion", "Bọ Cạp Đá Đỏ", "skirm", .8f, 1.2f, 2f, 2.4f, "Skirmisher", 13),
            Mn("serpent", "Mãng Xà Đầm Lầy", "skirm", .9f, 1.1f, 1f, 2.5f, "Skirmisher", 12),
            Mn("toad", "Cóc Độc Nhày", "trash", 1.6f, .8f, 1.2f, 1.6f, "Chaser", 11),
            Mn("forest_elf", "Lính Săn Elf Rừng", "elite", .6f, 1f, .8f, 2f, "Ranged", 14),
            Mn("dark_elf", "Kỵ Sĩ Elf Bóng Tối", "elite", 1f, 1.2f, 1.2f, 2.6f, "Skirmisher", 15),
            Mn("alpha_wolf", "Chúa Sói Alpha", "mini", 1f, 1f, 1f, 3f, "Skirmisher", 16),
            Mn("queen_weaver", "Nữ Hoàng Nhện", "mini", .9f, 1f, 1f, 1.8f, "Ranged", 16),
            Mn("elder_golem", "Cổ Nham Thủ Hộ", "mini", 1.4f, 1.1f, 1.6f, 1.3f, "Chaser", 17),
            Mn("dark_commander", "Chỉ Huy Elf Bóng Tối", "mini", 1f, 1.3f, 1.2f, 2.6f, "Skirmisher", 17),
        };

        static void SetRole(SerializedObject so, string role)
        {
            var p = so.FindProperty("role");
            if (p == null) { Debug.LogWarning("[Floor03] Không có trường 'role'"); return; }
            string[] names = p.enumNames;
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], role, StringComparison.OrdinalIgnoreCase)) { p.enumValueIndex = i; return; }
            }
            Debug.LogWarning("[Floor03] Không tìm thấy role '" + role + "' trong: " + string.Join(", ", names));
        }

        static void BuildEnemies(Built b)
        {
            EnsureFolder(DirEnemyData);
            // Hệ số để sau khi FloorScaling tầng 3 nhân lên, chỉ số cuối cùng đúng bằng 5 lần chỉ số cuối của tầng 2.
            float pk = 5f * FloorScaling.EnemyPowerMultiplier(2) / FloorScaling.EnemyPowerMultiplier(3);
            float ek = 5f * FloorScaling.ExpMultiplier(2) / FloorScaling.ExpMultiplier(3);
            Debug.Log("[Floor03] Hệ số dữ liệu quái: sức mạnh x" + pk.ToString("0.000") + ", EXP x" + ek.ToString("0.000"));

            b.Defs = new Dictionary<string, EnemyDefinition>();
            foreach (Mon m in Roster)
            {
                string path = DirEnemyData + "/Enemy_F3_" + m.Key + ".asset";
                var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<EnemyDefinition>();
                    AssetDatabase.CreateAsset(def, path);
                }

                Arch a = Archs[m.Arch];
                bool elite = m.Arch == "elite", mini = m.Arch == "mini";
                var so = new SerializedObject(def);
                P(so, "id").stringValue = "enemy.f3_" + m.Key;
                P(so, "displayName").stringValue = m.Name;
                P(so, "sprite").objectReferenceValue = b.Monsters[m.Key];
                SetNum(so, "level", m.Lv);
                SetNum(so, "maxHealth", Mathf.Max(1f, a.Hp * m.Hp * pk));
                SetNum(so, "attack", Mathf.Max(1f, a.Atk * m.Atk * pk));
                SetNum(so, "defense", Mathf.Max(0f, a.Def * m.Def * pk));
                SetNum(so, "experienceReward", Mathf.Max(1f, a.Exp * ek));
                SetNum(so, "moveSpeed", m.Spd);
                SetRole(so, m.Role);
                SetNum(so, "detectionRange", m.Role == "Ranged" ? 8f : 6.5f);
                SetNum(so, "attackRange", mini ? 1.6f : 1.2f);
                SetNum(so, "attackCooldownSeconds", mini ? 1.6f : 1.4f);
                SetNum(so, "attackWindupSeconds", mini ? .65f : elite ? .55f : .45f);
                SetNum(so, "hurtSeconds", .25f);
                SetNum(so, "deathSeconds", .6f);
                if (m.Role == "Ranged")
                {
                    SetNum(so, "preferredRange", 4.5f);
                    SetNum(so, "retreatRange", 2.5f);
                    SetNum(so, "retreatSeconds", .8f);
                    SetNum(so, "shootRange", 6.5f);
                    SetNum(so, "projectileSpeed", 7f);
                    SetNum(so, "projectileLifetime", 1.4f);
                    SetNum(so, "projectileRadius", .18f);
                    SetNum(so, "projectileDamageScale", .9f);
                }
                else if (m.Role == "Skirmisher")
                {
                    SetNum(so, "preferredRange", 2.2f);
                    SetNum(so, "retreatRange", 1.2f);
                    SetNum(so, "retreatSeconds", .8f);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(def);
                b.Defs[m.Key] = def;
            }
        }

        // ═════════════════════════ 4) Prefab spawner theo địa hình và theo trùm

        static EnemySpawner MakeSpawner(string name, SpawnGroup[] groups, Built b, EnemyController enemyPrefab, float radius, float respawn)
        {
            var list = groups.Where(g => b.Defs.ContainsKey(g.Key)).ToArray();
            var go = new GameObject(name);
            try
            {
                var sp = go.AddComponent<EnemySpawner>();
                var so = new SerializedObject(sp);
                P(so, "enemyPrefab").objectReferenceValue = enemyPrefab;
                var entries = P(so, "entries");
                entries.arraySize = list.Length;
                int maxCount = 1;
                for (int i = 0; i < list.Length; i++)
                {
                    var e = entries.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("definition").objectReferenceValue = b.Defs[list[i].Key];
                    e.FindPropertyRelative("count").intValue = list[i].Count;
                    e.FindPropertyRelative("radius").floatValue = radius;
                    e.FindPropertyRelative("respawnSeconds").floatValue = respawn;
                    maxCount = Mathf.Max(maxCount, list[i].Count);
                }
                P(so, "poolSizePerDefinition").intValue = maxCount + 1;
                P(so, "spawnOnStart").boolValue = true;
                P(so, "floorNumber").intValue = FloorNo;
                so.ApplyModifiedPropertiesWithoutUndo();

                string path = DirPrefabs + "/Spawners/" + name + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
                return prefab.GetComponent<EnemySpawner>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void BuildSpawners(Built b)
        {
            EnsureFolder(DirPrefabs + "/Spawners");
            var enemyGo = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            var enemyPrefab = enemyGo != null ? enemyGo.GetComponent<EnemyController>() : null;
            if (enemyPrefab == null) throw new Exception("Không thấy prefab quái dùng chung: " + EnemyPrefabPath);

            int biomeCount = Enum.GetValues(typeof(Biome)).Length;
            b.BiomeSpawners = new EnemySpawner[biomeCount];
            for (int i = 0; i < biomeCount; i++)
            {
                var groups = Floor03World.SpawnFor((Biome)i);
                if (groups.Length == 0) continue;
                b.BiomeSpawners[i] = MakeSpawner("Spawner_F3_" + (Biome)i, groups, b, enemyPrefab, 5f, 15f);
            }

            foreach (string key in new[] { "alpha_wolf", "queen_weaver", "elder_golem", "dark_commander" })
            {
                var g = new[] { new SpawnGroup { Key = key, Count = 1 } };
                b.BossKeys.Add(key);
                b.BossSpawners.Add(MakeSpawner("Spawner_F3_Boss_" + key, g, b, enemyPrefab, 2.5f, 180f));
            }
        }

        // ═════════════════════════ 5) FloorDefinition + đăng ký FloorDatabase

        static void BuildFloorDefinition(Built b)
        {
            EnsureFolder(DirFloorDef);
            var def = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FloorDefPath);
            if (def == null)
            {
                if (!AssetDatabase.CopyAsset(FloorDefSrc, FloorDefPath)) throw new Exception("Không sao chép được " + FloorDefSrc);
                def = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FloorDefPath);
            }
            def.id = "floor.03";
            def.floorNumber = FloorNo;
            def.displayName = "Tầng 3";
            def.sceneKey = "Floor03";
            def.recommendedLevelMin = 11;
            def.recommendedLevelMax = 17;
            def.spawnEnemyIds = Roster.Select(m => "enemy.f3_" + m.Key).ToList();
            def.questIds = new List<string>();
            def.safeZoneConfigIds = new List<string>();
            def.bossId = "";                                   // trùm Nerius sẽ làm ở bước sau
            def.requiredBossDefeatedId = "boss.floor_02";
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssets();
            b.Floor = def;

            var db = AssetDatabase.LoadMainAssetAtPath(DbPath);
            if (db == null) throw new Exception("Không thấy " + DbPath);
            var so = new SerializedObject(db);
            var arr = P(so, "floors");
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == def) return;
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
        }

        // ═════════════════════════ 6) Scene Floor03 (nhân bản khung Floor02 rồi thay nội dung)

        static void SetArray<T>(SerializedProperty arr, IList<T> items) where T : UnityEngine.Object
        {
            arr.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        static void BuildScene(Built b)
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path == ScenePath) EditorSceneManager.CloseScene(s, true);
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(SceneSrc, ScenePath)) throw new Exception("Không sao chép được " + SceneSrc);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject root = scene.GetRootGameObjects().FirstOrDefault(g => g.GetComponent<FloorSceneBinder>() != null);
                if (root == null) throw new Exception("Scene sao chép không có root FloorSceneBinder");
                Debug.Log("[Floor03] Root gốc: " + root.name);
                root.name = "Floor03";

                Transform content = root.transform.Find("Content");
                if (content == null) throw new Exception("Không thấy Content");

                var keep = new HashSet<string> { "NPC_Cartographer", "NPC_Merchant", "SafeZone_Lamp" };
                Grid grid = content.GetComponentInChildren<Grid>(true);
                if (grid == null) throw new Exception("Không thấy Grid trong Content");
                Transform gridTf = grid.transform;
                while (gridTf.parent != content) gridTf = gridTf.parent;          // phần tử con trực tiếp của Content chứa Grid
                var kept = new List<string>();
                var removed = new List<string>();
                for (int i = content.childCount - 1; i >= 0; i--)
                {
                    Transform ch = content.GetChild(i);
                    if (ch == gridTf || keep.Contains(ch.name)) { kept.Add(ch.name); continue; }
                    removed.Add(ch.name);
                    UnityEngine.Object.DestroyImmediate(ch.gameObject);
                }
                Debug.Log("[Floor03] Content giữ: [" + string.Join(", ", kept) + "] | xoá: [" + string.Join(", ", removed) + "]");

                // Grid và các tilemap
                grid.gameObject.name = "Grid_Floor03";
                var maps = grid.GetComponentsInChildren<Tilemap>(true);
                Debug.Log("[Floor03] Tilemap có sẵn: " + string.Join(", ", maps.Select(t => t.name)) + " | cellSize=" + grid.cellSize);
                Tilemap ground = maps.FirstOrDefault(t => t.name == "Floor") ?? maps.FirstOrDefault(t => t.name.Contains("Floor"));
                Tilemap walls = maps.FirstOrDefault(t => t.name == "Walls") ?? maps.FirstOrDefault(t => t.name.Contains("Wall") && !t.name.Contains("Fill"));
                if (ground == null) ground = maps.FirstOrDefault();
                if (walls == null) walls = maps.FirstOrDefault(t => t != ground);
                if (ground == null || walls == null) throw new Exception("Không xác định được tilemap Floor/Walls");
                foreach (Tilemap t in maps)
                {
                    if (t == ground || t == walls) { t.ClearAllTiles(); continue; }
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
                if (walls.GetComponent<TilemapCollider2D>() == null) walls.gameObject.AddComponent<TilemapCollider2D>();

                var propsGo = new GameObject("Props");
                propsGo.transform.SetParent(grid.transform, false);
                var props = propsGo.AddComponent<Tilemap>();
                props.tileAnchor = new Vector3(.5f, 0f, 0f);
                var pr = propsGo.AddComponent<TilemapRenderer>();
                var gr = ground.GetComponent<TilemapRenderer>();
                if (gr != null) { pr.sortingLayerID = gr.sortingLayerID; pr.sortingOrder = gr.sortingOrder + 1; }
                pr.sortOrder = TilemapRenderer.SortOrder.TopLeft;

                // Streamer thế giới vô hạn
                Transform entry = root.transform.Find("EntryPoint");
                if (entry != null) entry.position = new Vector3(OriginX, -5f, 0f);
                else Debug.LogWarning("[Floor03] Không thấy EntryPoint");

                var sgo = new GameObject("ChunkWorld");
                sgo.transform.SetParent(content, false);
                var st = sgo.AddComponent<Floor03ChunkStreamer>();
                var sso = new SerializedObject(st);
                P(sso, "groundMap").objectReferenceValue = ground;
                P(sso, "propsMap").objectReferenceValue = props;
                P(sso, "wallsMap").objectReferenceValue = walls;
                P(sso, "originX").intValue = OriginX;
                P(sso, "entryPoint").objectReferenceValue = entry;
                SetArray(P(sso, "groundTiles"), b.Ground);
                SetArray(P(sso, "propTiles"), b.Props);
                P(sso, "blockTile").objectReferenceValue = b.Block;
                SetArray(P(sso, "biomeSpawners"), b.BiomeSpawners);
                var bk = P(sso, "bossKeys");
                bk.arraySize = b.BossKeys.Count;
                for (int i = 0; i < b.BossKeys.Count; i++) bk.GetArrayElementAtIndex(i).stringValue = b.BossKeys[i];
                SetArray(P(sso, "bossSpawners"), b.BossSpawners);
                sso.ApplyModifiedPropertiesWithoutUndo();

                // Khai báo tầng
                var binder = root.GetComponent<FloorSceneBinder>();
                var bso = new SerializedObject(binder);
                P(bso, "floorId").stringValue = "floor.03";
                bso.ApplyModifiedPropertiesWithoutUndo();

                var reg = root.GetComponent<FloorSpawnRegistrar>();
                if (reg != null)
                {
                    var rso = new SerializedObject(reg);
                    P(rso, "enemySpawner").objectReferenceValue = null;
                    P(rso, "additionalSpawners").arraySize = 0;
                    rso.ApplyModifiedPropertiesWithoutUndo();
                }

                // NPC và đèn an toàn về Zumfut
                MoveIf(content, "NPC_Cartographer", new Vector3(OriginX + 4f, 3f, 0f));
                MoveIf(content, "NPC_Merchant", new Vector3(OriginX - 4f, 3f, 0f));
                MoveIf(content, "SafeZone_Lamp", new Vector3(OriginX, 0f, 0f));

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void MoveIf(Transform content, string name, Vector3 pos)
        {
            Transform t = content.Find(name);
            if (t != null) t.position = pos;
            else Debug.LogWarning("[Floor03] Không thấy " + name);
        }

        static void AddToBuildSettings()
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == ScenePath)) return;
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ═════════════════════════ Kiểm tra thế giới (không đụng tới scene)

        [MenuItem("Game/Floor 03/Validate World")]
        public static void Validate()
        {
            float core = Floor03World.CoreRadius;
            int R = Mathf.CeilToInt(core) + 6, N = 2 * R + 1;
            var blocked = new bool[N * N];
            var inCore = new bool[N * N];
            var counts = new int[12];
            int coreTiles = 0, coreBlocked = 0;
            for (int y = -R; y <= R; y++)
            {
                for (int x = -R; x <= R; x++)
                {
                    int i = (y + R) * N + (x + R);
                    TileInfo t = Floor03World.Sample(x, y);
                    blocked[i] = t.Blocked;
                    if (x * x + y * y <= core * core)
                    {
                        inCore[i] = true;
                        coreTiles++;
                        counts[(int)t.Biome]++;
                        if (t.Blocked) coreBlocked++;
                    }
                }
            }

            var seen = new bool[N * N];
            var q = new Queue<int>();
            int start = R * N + R;
            seen[start] = true;
            q.Enqueue(start);
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                int cx = cur % N, cy = cur / N;
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + dx[k], ny = cy + dy[k];
                    if (nx < 0 || ny < 0 || nx >= N || ny >= N) continue;
                    int ni = ny * N + nx;
                    if (seen[ni] || blocked[ni]) continue;
                    seen[ni] = true;
                    q.Enqueue(ni);
                }
            }

            int walkable = 0, reach = 0;
            for (int i = 0; i < N * N; i++)
            {
                if (!inCore[i] || blocked[i]) continue;
                walkable++;
                if (seen[i]) reach++;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Floor03] KIỂM TRA THẾ GIỚI");
            sb.AppendLine("Bán kính lõi " + core.ToString("0.0") + " tile, " + coreTiles + " ô lõi, tầng 2 = 350x196 = 68600 ô bao ngoài, tỉ lệ diện tích = "
                          + (coreTiles / 68600f).ToString("0.00"));
            sb.AppendLine("Ô bị chặn trong lõi: " + (100f * coreBlocked / coreTiles).ToString("0.0") + "%. Đi được từ Zumfut: "
                          + (100f * reach / Mathf.Max(1, walkable)).ToString("0.0") + "% số ô đi được trong lõi");
            for (int i = 0; i < counts.Length; i++)
                sb.AppendLine("  " + ((Biome)i) + ": " + (100f * counts[i] / coreTiles).ToString("0.0") + "%");
            foreach (Poi p in Floor03World.Pois)
            {
                int px = Mathf.FloorToInt(p.Pos.x), py = Mathf.FloorToInt(p.Pos.y);
                int idx = (py + R) * N + (px + R);
                TileInfo t = Floor03World.Sample(px, py);
                sb.AppendLine("  POI " + p.Name + " (" + px + "," + py + ") " + t.Biome + (blocked[idx] ? " BỊ CHẶN" : seen[idx] ? " đến được" : " KHÔNG ĐẾN ĐƯỢC"));
            }
            Debug.Log(sb.ToString());
        }
    }
}
