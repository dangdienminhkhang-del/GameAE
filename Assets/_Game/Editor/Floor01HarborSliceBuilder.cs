using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Game.PvE.Enemy;
using Game.PvE.Floor;
using Object = UnityEngine.Object;

namespace Game.EditorTools
{
    /// <summary>
    /// Dựng Tầng 1 dạng bến cảng (Phase 9.7a prototype): tile placeholder + tilemap theo vùng + spawner từng khu.
    /// Chạy lại được nhiều lần (idempotent). Tile là PLACEHOLDER theo docs/14: Art Track có thể ghi đè PNG cùng tên.
    /// Vùng (docs/05 §9.7a): Thị trấn (Quay) -> Vùng Farm (Pier, 3 bãi + 1 vịnh) -> Hầm Boss (Hold: hành lang, tiền sảnh, phòng boss).
    /// </summary>
    public static class Floor01HarborSliceBuilder
    {
        private const string ArtDir = "Assets/_Game/Art/Environment/Floor01";
        private const string SlimePath = "Assets/_Game/Data/Enemies/Enemy_Slime.asset";
        private const string BatPath = "Assets/_Game/Data/Enemies/Enemy_Bat.asset";

        // Bảng màu docs/14 (tools/art_gen/pixelkit.py): chỉ số 0 tối nhất -> 4 điểm sáng.
        private static readonly int[] Water = { 0x0a1b27, 0x0f2a3a, 0x173d52, 0x245672, 0x357490 };
        private static readonly int[] Wood2 = { 0x2a1f16, 0x43321f, 0x5f482f, 0x7a5e3f, 0x99794f };
        private static readonly int[] Nail = { 0x15110e, 0x2a2622, 0x5a554e, 0x8a8478, 0xb8b2a4 };
        private static readonly int[] Stone = { 0x171b24, 0x262c38, 0x363e4e, 0x4a5568, 0x61708a };
        private static readonly int[] Stone2 = { 0x1a1e28, 0x2a303d, 0x3b4354, 0x505c72, 0x67768f };
        private static readonly int[] Iron = { 0x14181f, 0x252b35, 0x3a4250, 0x566073, 0x7c889d };
        private static readonly int[] Rust = { 0x33150f, 0x66261a, 0x97402b, 0xc06848, 0xdf9570 };

        // ── Menu ─────────────────────────────────────────────────────────────

        [MenuItem("Game/Floor 01/Dump Art Palette (debug)")]
        private static void DumpPalette()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var path = Path.Combine(root, "tools/art_gen/pixelkit.py");
            if (!File.Exists(path)) { Debug.LogWarning("[Floor01Builder] Không thấy " + path); return; }
            var t = File.ReadAllText(path);
            int i = t.IndexOf("PALETTE");
            Debug.Log("[Floor01Builder] PALETTE:\n" + (i >= 0 ? t.Substring(i, Mathf.Min(3000, t.Length - i)) : "(không có PALETTE)"));
        }

        [MenuItem("Game/Floor 01/Build Harbor Slice")]
        private static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Floor01Builder] Thoát Play Mode trước khi dựng.");
                return;
            }

            var tmGo = GameObject.Find("/Grid/Tilemap");
            var content = GameObject.Find("/Floor01/Content");
            var boss = GameObject.Find("/Floor01/Content/Boss_Floor01");
            var lamp = GameObject.Find("/Floor01/Content/SafeZone_Lamp");
            var registrar = Object.FindFirstObjectByType<FloorSpawnRegistrar>();
            if (tmGo == null || content == null || boss == null || lamp == null || registrar == null)
            {
                Debug.LogError("[Floor01Builder] Thiếu object trong scene (cần mở PvE_Gameplay): Grid/Tilemap, Floor01/Content, Boss_Floor01, SafeZone_Lamp, FloorSpawnRegistrar.");
                return;
            }

            var tm = tmGo.GetComponent<Tilemap>();
            var layout = BuildLayout();

            // 1) Tile placeholder
            var water = MakeTile("Water", WriteTile("Tiles_Floor01_Water", WaterPx), Tile.ColliderType.Grid);
            var pier = MakeTile("Pier", WriteTile("Tiles_Floor01_Pier", PierPx), Tile.ColliderType.None);
            var quay = MakeTile("Quay", WriteTile("Tiles_Floor01_Quay", QuayPx), Tile.ColliderType.None);
            var hold = MakeTile("HoldFloor", WriteTile("Tiles_Floor01_HoldFloor", (x, y) => HoldFloorPx(x, y, false)), Tile.ColliderType.None);
            var bossFloor = MakeTile("BossFloor", WriteTile("Tiles_Floor01_BossFloor", (x, y) => HoldFloorPx(x, y, true)), Tile.ColliderType.None);
            var wall = MakeTile("HoldWall", WriteTile("Tiles_Floor01_HoldWall", HoldWallPx), Tile.ColliderType.Grid);

            // 2) Vẽ tilemap: nước phủ rộng (chặn), sàn từng vùng, tường tàu quanh Hầm Boss
            var positions = new List<Vector3Int>();
            var tiles = new List<TileBase>();
            for (int x = -22; x <= 86; x++)
            {
                for (int y = -34; y <= 38; y++)
                {
                    var c = new Vector2Int(x, y);
                    TileBase t = water;
                    if (layout.Quay.Contains(c)) t = quay;
                    else if (layout.Pier.Contains(c)) t = pier;
                    else if (layout.Hold.Contains(c)) t = hold;
                    else if (layout.Boss.Contains(c)) t = bossFloor;
                    else if (layout.Wall.Contains(c)) t = wall;
                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(t);
                }
            }
            tm.ClearAllTiles();
            tm.SetTiles(positions.ToArray(), tiles.ToArray());
            tm.RefreshAllTiles();
            var tr = tmGo.GetComponent<TilemapRenderer>();
            if (tr != null) tr.sortingOrder = -10;
            EditorUtility.SetDirty(tm);

            // 3) Boss vào giữa phòng boss (cách cửa phòng > 13 ô, DetectionRange 10 nên chỉ thức khi vào phòng)
            boss.transform.position = new Vector3(67.5f, -11.5f, 0f);

            // 4) Safe Zone = cả Thị trấn (docs/05 §9.7a). Collider nằm trên đèn có scale, nên quy đổi sang không gian local.
            var box = lamp.GetComponent<BoxCollider2D>();
            var ls = lamp.transform.lossyScale;
            var lp = lamp.transform.position;
            box.size = new Vector2(17f / ls.x, 11f / ls.y);
            box.offset = new Vector2((0.5f - lp.x) / ls.x, (0.5f - lp.y) / ls.y);
            EditorUtility.SetDirty(box);

            // 5) Spawner từng khu (A dùng lại spawner cũ để giữ nguyên tham chiếu của FloorSpawnRegistrar)
            var parent = content.transform;
            var tplT = parent.Find("Spawner_FarmA");
            if (tplT == null) tplT = parent.Find("EnemySpawner");
            if (tplT == null) { Debug.LogError("[Floor01Builder] Không thấy spawner gốc."); return; }
            var spA = tplT.gameObject;
            spA.name = "Spawner_FarmA";
            var spB = GetOrClone(parent, "Spawner_FarmB", spA);
            var spC = GetOrClone(parent, "Spawner_FarmC", spA);
            var spV = GetOrClone(parent, "Spawner_Cove", spA);

            Configure(spA, new Vector3(21.5f, 0.5f, 0f), E(SlimePath, 3, 5f, 8f), E(BatPath, 1, 5f, 12f));
            Configure(spB, new Vector3(21.5f, 17.5f, 0f), E(BatPath, 3, 4.5f, 12f), E(SlimePath, 1, 4.5f, 8f));
            Configure(spC, new Vector3(41.5f, 17.5f, 0f), E(SlimePath, 2, 5f, 8f), E(BatPath, 2, 5f, 12f));
            Configure(spV, new Vector3(20.5f, -13.5f, 0f), E(SlimePath, 2, 2.5f, 8f));

            var rso = new SerializedObject(registrar);
            var arr = rso.FindProperty("additionalSpawners");
            var extra = new[] { spB, spC, spV };
            arr.arraySize = extra.Length;
            for (int i = 0; i < extra.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = extra[i].GetComponent<EnemySpawner>();
            rso.ApplyModifiedProperties();

            // 6) Tự kiểm tra: đường đi + spawn không rơi xuống nước
            Verify(layout, new[] { spA, spB, spC, spV });

            AssetDatabase.SaveAssets();
            var scene = tmGo.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Floor01Builder] Xong: đã dựng và lưu scene " + scene.path);
        }

        // ── Bố cục (toạ độ ô, bao gồm hai đầu) ───────────────────────────────

        private sealed class Layout
        {
            public readonly HashSet<Vector2Int> Quay = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> Pier = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> Hold = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> Boss = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> Wall = new HashSet<Vector2Int>();

            public bool IsFloor(Vector2Int c)
            {
                return Quay.Contains(c) || Pier.Contains(c) || Hold.Contains(c) || Boss.Contains(c);
            }
        }

        private static void Fill(HashSet<Vector2Int> set, int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    set.Add(new Vector2Int(x, y));
        }

        private static Layout BuildLayout()
        {
            var l = new Layout();
            // Thị trấn (Safe Zone)
            Fill(l.Quay, -8, -5, 8, 5);
            // Vùng Farm: cầu nối + 3 bãi cầu tàu + vịnh nhỏ có đồ
            Fill(l.Pier, 9, -1, 13, 1);      // cầu ra khỏi thị trấn
            Fill(l.Pier, 14, -6, 26, 6);     // bãi A
            Fill(l.Pier, 19, 7, 21, 11);     // cầu A -> B
            Fill(l.Pier, 13, 12, 29, 22);    // bãi B
            Fill(l.Pier, 30, 16, 34, 18);    // cầu B -> C
            Fill(l.Pier, 35, 10, 47, 24);    // bãi C (cổng Hầm ở đáy)
            Fill(l.Pier, 19, -10, 21, -7);   // cầu A -> vịnh
            Fill(l.Pier, 14, -17, 26, -11);  // vịnh
            // Hầm Boss: hành lang -> tiền sảnh -> hành lang -> phòng boss
            Fill(l.Hold, 40, 4, 42, 9);
            Fill(l.Hold, 40, -6, 42, 3);
            Fill(l.Hold, 35, -17, 47, -7);
            Fill(l.Hold, 48, -13, 53, -11);
            Fill(l.Boss, 54, -20, 72, -4);

            // Tường tàu bao quanh mọi ô Hầm/Boss tiếp giáp ô không đi được
            foreach (var set in new[] { l.Hold, l.Boss })
            {
                foreach (var c in set)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            var n = new Vector2Int(c.x + dx, c.y + dy);
                            if (!l.IsFloor(n)) l.Wall.Add(n);
                        }
                    }
                }
            }
            return l;
        }

        // ── Kiểm tra ─────────────────────────────────────────────────────────

        private static void Verify(Layout l, GameObject[] spawners)
        {
            bool Walk(Vector2Int c)
            {
                return l.IsFloor(c) && l.IsFloor(c + Vector2Int.up) && l.IsFloor(c + Vector2Int.down)
                    && l.IsFloor(c + Vector2Int.left) && l.IsFloor(c + Vector2Int.right);
            }

            var start = new Vector2Int(0, 0);
            var goals = new[]
            {
                new KeyValuePair<string, Vector2Int>("cổng Hầm (đáy bãi C)", new Vector2Int(41, 10)),
                new KeyValuePair<string, Vector2Int>("cửa phòng boss", new Vector2Int(54, -12)),
                new KeyValuePair<string, Vector2Int>("vị trí boss", new Vector2Int(67, -12)),
                new KeyValuePair<string, Vector2Int>("vịnh có đồ", new Vector2Int(20, -13)),
            };

            var dist = new Dictionary<Vector2Int, float>();
            var open = new SortedSet<(float d, int x, int y)>();
            dist[start] = 0f;
            open.Add((0f, start.x, start.y));
            while (open.Count > 0)
            {
                var cur = open.Min;
                open.Remove(cur);
                var c = new Vector2Int(cur.x, cur.y);
                if (cur.d > dist[c]) continue;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var n = new Vector2Int(c.x + dx, c.y + dy);
                        if (!Walk(n)) continue;
                        bool diag = dx != 0 && dy != 0;
                        if (diag && (!Walk(new Vector2Int(c.x + dx, c.y)) || !Walk(new Vector2Int(c.x, c.y + dy)))) continue;
                        float nd = cur.d + (diag ? 1.4142f : 1f);
                        float old;
                        if (!dist.TryGetValue(n, out old) || nd < old)
                        {
                            dist[n] = nd;
                            open.Add((nd, n.x, n.y));
                        }
                    }
                }
            }

            var sb = new System.Text.StringBuilder("[Floor01Builder] Đường đi từ Thị trấn (0,0), chừa chỗ cho nhân vật:\n");
            foreach (var g in goals)
            {
                float d;
                sb.AppendLine("  -> " + g.Key + ": " + (dist.TryGetValue(g.Value, out d) ? d.ToString("0.0") + " ô" : "KHÔNG ĐẾN ĐƯỢC"));
            }

            int bad = 0;
            foreach (var sp in spawners)
            {
                var spc = sp.GetComponent<EnemySpawner>();
                var so = new SerializedObject(spc);
                var entries = so.FindProperty("entries");
                var ctr = sp.transform.position;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    float r = entries.GetArrayElementAtIndex(i).FindPropertyRelative("radius").floatValue;
                    for (int a = 0; a < 360; a += 5)
                    {
                        float rad = a * Mathf.Deg2Rad;
                        var p = new Vector2(ctr.x + Mathf.Cos(rad) * r, ctr.y + Mathf.Sin(rad) * r);
                        var cell = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
                        if (!l.IsFloor(cell)) bad++;
                    }
                }
            }
            sb.AppendLine("  Điểm biên vùng spawn rơi ngoài sàn: " + bad + " (mong đợi 0)");
            Debug.Log(sb.ToString());
        }

        // ── Spawner ──────────────────────────────────────────────────────────

        private struct SpawnEntryData
        {
            public string DefPath;
            public int Count;
            public float Radius;
            public float Respawn;
        }

        private static SpawnEntryData E(string path, int count, float radius, float respawn)
        {
            return new SpawnEntryData { DefPath = path, Count = count, Radius = radius, Respawn = respawn };
        }

        private static GameObject GetOrClone(Transform parent, string name, GameObject template)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = Object.Instantiate(template, parent);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        private static void Configure(GameObject go, Vector3 pos, params SpawnEntryData[] entries)
        {
            go.transform.position = pos;
            var spawner = go.GetComponent<EnemySpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("spawnCenter").objectReferenceValue = go.transform;
            var arr = so.FindProperty("entries");
            arr.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(entries[i].DefPath);
                e.FindPropertyRelative("count").intValue = entries[i].Count;
                e.FindPropertyRelative("radius").floatValue = entries[i].Radius;
                e.FindPropertyRelative("respawnSeconds").floatValue = entries[i].Respawn;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);
        }

        // ── Tile placeholder (32x32, PPU 32, Point — docs/14) ────────────────

        private static Color32 Col(int[] ramp, int i)
        {
            int v = ramp[i];
            return new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);
        }

        private static int Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return h & 0x7fffffff;
            }
        }

        private static Color32 WaterPx(int x, int y)
        {
            int h = Hash(x, y, 1) % 100;
            if ((x + y * 3) % 20 == 0 && Hash(x / 4, y / 4, 9) % 3 == 0) return Col(Water, 3);
            if (h == 50) return Col(Water, 4);
            if (h < 8) return Col(Water, 3);
            if (h >= 92) return Col(Water, 1);
            return Col(Water, 2);
        }

        private static Color32 PierPx(int x, int y)
        {
            int plank = y / 8;
            int ry = y % 8;
            int joint = (plank % 2 == 0) ? 11 : 27;
            if (ry == 0 || x == joint) return Col(Wood2, 0);
            if (ry == 4 && (x == joint - 3 || x == joint + 3)) return Col(Nail, 3);
            if (ry == 1) return Col(Wood2, 3);
            int h = Hash(x, y, 2) % 100;
            if (h < 12) return Col(Wood2, 1);
            if (h >= 94) return Col(Wood2, 3);
            return Col(Wood2, 2);
        }

        private static Color32 QuayPx(int x, int y)
        {
            int row = y / 8;
            int ry = y % 8;
            int xo = (x + (row % 2) * 8) % 16;
            if (ry == 0 || xo == 0) return Col(Stone, 1);
            if (ry == 1 || xo == 1) return Col(Stone, 3);
            int h = Hash(x, y, 3) % 100;
            if (h < 10) return Col(Stone2, 2);
            if (h < 14) return Col(Stone, 1);
            if (h >= 95) return Col(Stone, 3);
            return Col(Stone, 2);
        }

        private static Color32 HoldFloorPx(int x, int y, bool boss)
        {
            int px = x % 16;
            int py = y % 16;
            if (px == 0 || py == 0) return boss ? Col(Rust, 0) : Col(Iron, 0);
            if (px == 1 || py == 1) return Col(Iron, 3);
            if ((px == 3 || px == 12) && (py == 3 || py == 12)) return boss ? Col(Rust, 3) : Col(Iron, 4);
            int h = Hash(x, y, 5) % 100;
            if (h < 6) return Col(Iron, 1);
            if (boss && h >= 95) return Col(Rust, 1);
            if (h >= 96) return Col(Iron, 3);
            return Col(Iron, 2);
        }

        private static Color32 HoldWallPx(int x, int y)
        {
            if (y == 31) return Col(Iron, 4);
            if (y == 30) return Col(Iron, 3);
            if (y == 29) return Col(Iron, 2);
            if (y == 0 || x % 16 == 0 || y % 16 == 15) return Col(Iron, 0);
            if ((x % 16 == 4 || x % 16 == 12) && y % 16 == 8) return Col(Iron, 3);
            int h = Hash(x, y, 7) % 100;
            if (h < 3) return Col(Rust, 1);
            if (h < 12) return Col(Iron, 0);
            return Col(Iron, 1);
        }

        private static Sprite WriteTile(string fileName, System.Func<int, int, Color32> px)
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color32[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    pixels[y * 32 + x] = px(x, y);
            tex.SetPixels32(pixels);
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string assetPath = ArtDir + "/" + fileName + ".png";
            string abs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(abs));
            File.WriteAllBytes(abs, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 32;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Tile MakeTile(string name, Sprite sprite, Tile.ColliderType collider)
        {
            string path = ArtDir + "/Tile_Floor01_" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            tile.colliderType = collider;
            EditorUtility.SetDirty(tile);
            return tile;
        }
    }
}
