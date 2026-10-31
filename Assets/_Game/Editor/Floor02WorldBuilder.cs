using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Game.PvE.Zones;
using Object = UnityEngine.Object;

namespace Game.EditorTools
{
    /// <summary>
    /// Dựng lại Tầng 2 "Lò Rèn Ngập Nước" thành thế giới lớn (~16 lần bản cũ), data-driven, chạy lại được.
    /// Toạ độ "local" có Trạm Van ở (0,0); thế giới = local + (OriginX, 0). Chạy: Game/Floor 02/Build Big World
    /// (cần scene Floor02.unity đang mở/nạp).
    /// </summary>
    public static class Floor02WorldBuilder
    {
        // ---------- hằng số ----------
        private const int OriginX = 1000;
        private const float OX = OriginX;
        private const int MinX = -125, MinY = -100, GW = 350, GH = 196;
        private const int FillMargin = 12, ColliderMargin = 2;
        private const string ArtDir = "Assets/_Game/Art/Environment/Floor02";

        private static bool[,] _floor;

        // ---------- mô tả khu ----------
        private sealed class Dist
        {
            public string Key, Name, Music;
            public RectInt Aabb, Core;
            public ZoneKind Kind;
            public int Prio, Props, Vents, Puddles;
            public bool Existing;
            public ZoneVolume Zone;
            public Transform Root, SpawnersRoot, PropsRoot;
        }

        private sealed class Spawn
        {
            public string Key, Dist, Spec;
            public float X, Y, R;
            public Spawn(string key, string dist, float x, float y, string spec, float r) { Key = key; Dist = dist; X = x; Y = y; Spec = spec; R = r; }
        }

        private sealed class Belt
        {
            public string Dist; public float X, Y, W, H; public bool Right;
            public Belt(string d, float x, float y, float w, float h, bool right) { Dist = d; X = x; Y = y; W = w; H = h; Right = right; }
        }

        private sealed class Refuge
        {
            public string Key, Name, ParentDist; public RectInt Rect;
            public Refuge(string key, string name, string parent, RectInt r) { Key = key; Name = name; ParentDist = parent; Rect = r; }
        }

        private static RectInt R(int x0, int y0, int x1, int y1) { return new RectInt(x0, y0, x1 - x0, y1 - y0); }

        private static List<Dist> Dists()
        {
            return new List<Dist>
            {
                new Dist { Key = "Town",     Name = "Trạm Van",                          Aabb = R(-22,-14,22,14),  Core = R(-22,-14,22,14),  Kind = ZoneKind.Town, Prio = 0, Music = "music.town",     Props = 22, Vents = 0, Puddles = 1, Existing = true },
                new Dist { Key = "Scrap",    Name = "Bãi Phế Liệu (Lv 6–7)",              Aabb = R(-105,-26,-22,36), Core = R(-105,-26,-29,36), Kind = ZoneKind.Farm, Prio = 0, Music = "music.gameplay", Props = 44, Vents = 3, Puddles = 4 },
                new Dist { Key = "Boiler",   Name = "Lò Hơi Trung Tâm (Lv 7–8)",          Aabb = R(-25,14,45,78),    Core = R(-25,30,45,78),    Kind = ZoneKind.Farm, Prio = 0, Music = "music.gameplay", Props = 36, Vents = 9, Puddles = 3 },
                new Dist { Key = "Pipes",    Name = "Mê Cung Ống (Lv 8–9)",               Aabb = R(-40,-82,66,-9),   Core = R(-40,-82,50,-29),  Kind = ZoneKind.Farm, Prio = 0, Music = "music.gameplay", Props = 30, Vents = 8, Puddles = 6 },
                new Dist { Key = "Hall",     Name = "Hành Lang Băng Tải (Lv 7)",          Aabb = R(22,-9,96,9),      Core = R(26,-9,92,9),      Kind = ZoneKind.Farm, Prio = 0, Music = "music.gameplay", Props = 22, Vents = 4, Puddles = 2 },
                new Dist { Key = "Forge",    Name = "Xưởng Rèn (Lv 9–10)",                Aabb = R(45,9,131,72),     Core = R(56,24,131,72),    Kind = ZoneKind.Farm, Prio = 0, Music = "music.gameplay", Props = 36, Vents = 9, Puddles = 3 },
                new Dist { Key = "Plaza",    Name = "Ngã Tư Áp Lực (Trạm nghỉ)",          Aabb = R(96,-14,116,14),   Core = R(96,-14,116,14),   Kind = ZoneKind.Refuge, Prio = 2, Music = "music.town",   Props = 10, Vents = 0, Puddles = 1 },
                new Dist { Key = "Corridor", Name = "Hành Lang Hầm Điều Áp (Lv 9–10)",    Aabb = R(116,-12,166,12),  Core = R(116,-12,166,12),  Kind = ZoneKind.Farm, Prio = 0, Music = "music.gameplay", Props = 24, Vents = 3, Puddles = 0 },
            };
        }

        private static readonly Refuge[] Refuges =
        {
            new Refuge("R2", "Trạm Nghỉ Đường Ống", "Pipes",  R(12,-51,28,-45)),
            new Refuge("R3", "Trạm Nghỉ Xưởng Rèn", "Forge",  R(86,41,100,48)),
            new Refuge("R4", "Trạm Phế Liệu",       "Scrap",  R(-72,-3,-60,3)),
            new Refuge("R5", "Trạm Nghỉ Lò Hơi",    "Boiler", R(8,45,22,51)),
        };

        // Chữ cái: G=BoilerGuard W=PressureWarden B=Bat P=PipeSpitter S=WireStalker; số = số lượng.
        private static readonly Spawn[] Spawns =
        {
            new Spawn("Scrap1","Scrap",-88,-14,"G2B2",6), new Spawn("Scrap2","Scrap",-50,-14,"G3B2",6),
            new Spawn("Scrap3","Scrap",-90,18,"G2P1",6),  new Spawn("Scrap4","Scrap",-55,22,"B3G2",6),
            new Spawn("Scrap5","Scrap",-98,-2,"G2B2",5),
            new Spawn("Hall1","Hall",38,0,"G2P1",5), new Spawn("Hall2","Hall",58,0,"G2P2",5), new Spawn("Hall3","Hall",82,0,"G2S1",5),
            new Spawn("Boiler1","Boiler",-5.5f,47,"G3P2",3), new Spawn("Boiler2","Boiler",11,60,"P2G2",3), new Spawn("Boiler3","Boiler",27,48,"G2S2",3),
            new Spawn("Boiler4","Boiler",18,34,"G2B2",3),    new Spawn("Boiler5","Boiler",-5,74,"P2B2",2), new Spawn("Boiler6","Boiler",36,74,"G2S2",2),
            new Spawn("Pipes1","Pipes",20,-33,"G2B1",1.8f), new Spawn("Pipes2","Pipes",-5,-41,"S2B1",1.8f), new Spawn("Pipes3","Pipes",20,-49,"G2S1",1.8f),
            new Spawn("Pipes4","Pipes",-10,-57,"S2G1",1.8f), new Spawn("Pipes5","Pipes",25,-65,"P2S1",1.8f), new Spawn("Pipes6","Pipes",-5,-73,"P2G1",1.8f),
            new Spawn("Pipes7","Pipes",20,-80,"B2S1",1.8f),
            new Spawn("Forge1","Forge",66,29,"W1G2",3), new Spawn("Forge2","Forge",100,29,"G2B2",3), new Spawn("Forge3","Forge",120,29,"B2S1",3),
            new Spawn("Forge4","Forge",82,44,"P2S2",4), new Spawn("Forge5","Forge",112,59,"G2P2",3), new Spawn("Forge6","Forge",84,59,"W1B2",3),
            new Spawn("Forge7","Forge",64,59,"S2G2",3),
            new Spawn("Corr1","Corridor",124,0,"B2P1",1.8f), new Spawn("Corr2","Corridor",140,0,"B2G1",1.8f), new Spawn("Corr3","Corridor",160,0,"G2B1",1.8f),
        };

        private static readonly Belt[] Belts =
        {
            new Belt("Hall",40,6,20,2,true), new Belt("Hall",62,-5,20,2,false), new Belt("Hall",80,6,16,2,true),
            new Belt("Forge",69,44,22,2,true), new Belt("Forge",112,44,24,2,false),
            new Belt("Pipes",-17,-40,26,2,false), new Belt("Pipes",15,-64,30,2,true),
        };

        // ---------- bố cục ----------
        private static bool In(int x, int y) { return x >= MinX && x < MinX + GW && y >= MinY && y < MinY + GH; }
        private static bool F(int x, int y) { return In(x, y) && _floor[x - MinX, y - MinY]; }
        private static bool Walk(int x, int y) { return F(x, y) && F(x + 1, y) && F(x - 1, y) && F(x, y + 1) && F(x, y - 1); }
        private static bool WalkP(Vector2 p) { return Walk(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y)); }

        private static void Fill(int x0, int y0, int x1, int y1, bool v = true)
        {
            for (int x = x0; x < x1; x++) for (int y = y0; y < y1; y++) if (In(x, y)) _floor[x - MinX, y - MinY] = v;
        }

        private static void BuildLayout()
        {
            _floor = new bool[GW, GH];
            // Trạm Van
            Fill(-22,-14,22,14); Fill(-16,6,-8,11,false); Fill(8,-11,16,-6,false);
            // Hành Lang Băng Tải + trụ
            Fill(22,-3,26,3); Fill(26,-9,92,9);
            foreach (int px in new[] { 30, 40, 60, 80 }) Fill(px,-1,px+2,1,false);
            // Ngã Tư Áp Lực
            Fill(92,-3,96,3); Fill(96,-14,116,14);
            Fill(99,8,101,10,false); Fill(111,8,113,10,false); Fill(99,-10,101,-8,false); Fill(111,-10,113,-8,false);
            // Hành lang tới Hầm Điều Áp + phòng phụ + phòng boss
            Fill(116,-3,177,3);
            Fill(130,4,140,12); Fill(133,3,137,4);
            Fill(150,-12,160,-4); Fill(153,-4,157,-3);
            Fill(177,-9,199,10);
            // Lò Hơi Trung Tâm
            Fill(-3,14,3,30); Fill(-25,30,45,78);
            foreach (int bx in new[] { -17, -1, 15, 31 }) foreach (int by in new[] { 38, 52, 65 }) Fill(bx,by,bx+8,by+6,false);
            // Xưởng Rèn
            Fill(45,44,56,50); Fill(56,24,131,72);
            Fill(64,34,74,39,false); Fill(84,34,94,39,false); Fill(104,34,114,39,false);
            Fill(74,50,84,55,false); Fill(94,50,104,55,false); Fill(114,50,124,55,false);
            Fill(64,64,74,68,false); Fill(104,64,114,68,false);
            Fill(104,14,110,24); Fill(70,9,76,24);
            // Mê Cung Ống
            Fill(-3,-29,3,-14); Fill(-40,-82,50,-29);
            Fill(-30,-37,40,-35,false);                                   // (sửa dưới)
            Fill(-40,-37,40,-35,false);
            Fill(-30,-45,0,-43,false); Fill(3,-45,50,-43,false);
            Fill(-40,-53,5,-51,false); Fill(8,-53,40,-51,false);
            Fill(-30,-61,50,-59,false);
            Fill(-40,-69,-12,-67,false); Fill(-9,-69,40,-67,false);
            Fill(-30,-77,50,-75,false);
            Fill(50,-60,66,-54); Fill(60,-54,66,-9);
            // Bãi Phế Liệu
            Fill(-29,-3,-22,3); Fill(-105,-26,-29,36);
            int[,] piles = { {-95,-18},{-85,-10},{-75,-20},{-60,-14},{-45,-18},{-98,0},{-80,6},{-65,8},{-50,8},{-92,16},{-72,20},{-58,26},{-42,22},{-88,28},{-100,-24} };
            for (int i = 0; i < piles.GetLength(0); i++) Fill(piles[i,0], piles[i,1], piles[i,0]+3, piles[i,1]+3, false);
            Fill(-29,31,-25,35); Fill(-36,-29,-32,-26);
        }

        // ---------- tile ----------
        private static int Hash(int x, int y)
        {
            unchecked { int h = x * 73856093 ^ y * 19349663 ^ 0x5bd1e995; h ^= h >> 13; h *= 0x27d4eb2d; h ^= h >> 15; return h & 0x7fffffff; }
        }

        private static bool[,] Dilate(bool[,] src, int r)
        {
            var tmp = new bool[GW, GH]; var dst = new bool[GW, GH];
            for (int y = 0; y < GH; y++) for (int x = 0; x < GW; x++)
            {
                bool v = false;
                for (int dx = -r; dx <= r && !v; dx++) { int xx = x + dx; if (xx >= 0 && xx < GW && src[xx, y]) v = true; }
                tmp[x, y] = v;
            }
            for (int x = 0; x < GW; x++) for (int y = 0; y < GH; y++)
            {
                bool v = false;
                for (int dy = -r; dy <= r && !v; dy++) { int yy = y + dy; if (yy >= 0 && yy < GH && tmp[x, yy]) v = true; }
                dst[x, y] = v;
            }
            return dst;
        }

        private static Dictionary<string, TileBase> LoadTiles()
        {
            var d = new Dictionary<string, TileBase>();
            foreach (var g in AssetDatabase.FindAssets("t:TileBase", new[] { ArtDir }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var t = AssetDatabase.LoadAssetAtPath<TileBase>(p);
                if (t == null) continue;
                foreach (var k in new[] { "FloorPlain", "FloorA", "FloorB", "FloorGrate", "FloorShadowA", "FloorShadowB", "WallTopA", "WallTopB", "BoilerWall" })
                    if (t.name.EndsWith(k)) d[k] = t;
            }
            return d;
        }

        private static Tilemap EnsureFillMap(Tilemap walls)
        {
            var existing = walls.transform.parent.Find("WallsFill");
            GameObject go = existing != null ? existing.gameObject : new GameObject("WallsFill");
            if (existing == null) go.transform.SetParent(walls.transform.parent, false);
            var tm = go.GetComponent<Tilemap>(); if (tm == null) tm = go.AddComponent<Tilemap>();
            var tr = go.GetComponent<TilemapRenderer>(); if (tr == null) tr = go.AddComponent<TilemapRenderer>();
            var wr = walls.GetComponent<TilemapRenderer>();
            tr.sortingLayerID = wr.sortingLayerID; tr.sortingOrder = wr.sortingOrder; tr.sharedMaterial = wr.sharedMaterial; tr.mode = wr.mode;
            return tm;
        }

        private static string BuildTiles(Tilemap floorMap, Tilemap wallsMap, Tilemap fillMap)
        {
            var T = LoadTiles();
            foreach (var k in new[] { "FloorPlain", "FloorA", "FloorB", "FloorGrate", "FloorShadowA", "FloorShadowB", "WallTopA", "WallTopB", "BoilerWall" })
                if (!T.ContainsKey(k)) throw new System.Exception("Thiếu tile " + k + " trong " + ArtDir);

            var fill12 = new bool[GW, GH]; // dilate(floor, 12)
            var d12 = Dilate(_floor, FillMargin); var d2 = Dilate(_floor, ColliderMargin);
            var fp = new List<Vector3Int>(); var ft = new List<TileBase>();
            var wp = new List<Vector3Int>(); var wt = new List<TileBase>();
            var xp = new List<Vector3Int>(); var xt = new List<TileBase>();
            int floorCount = 0;
            for (int gy = 0; gy < GH; gy++) for (int gx = 0; gx < GW; gx++)
            {
                int lx = gx + MinX, ly = gy + MinY; var cell = new Vector3Int(lx + OriginX, ly, 0);
                if (_floor[gx, gy])
                {
                    floorCount++;
                    int r = Hash(lx, ly) % 100; TileBase t;
                    if (!F(lx, ly + 1)) t = (Hash(lx + 7, ly) & 1) == 0 ? T["FloorShadowA"] : T["FloorShadowB"];
                    else if (lx >= 96 && lx < 116 && ly >= -14 && ly < 14) t = r < 60 ? T["FloorPlain"] : T["FloorA"];
                    else if (ly >= -82 && ly < -29 && lx >= -40 && lx < 50) t = r < 40 ? T["FloorGrate"] : r < 65 ? T["FloorA"] : r < 85 ? T["FloorB"] : T["FloorPlain"];
                    else t = r < 37 ? T["FloorA"] : r < 65 ? T["FloorB"] : r < 78 ? T["FloorGrate"] : T["FloorPlain"];
                    fp.Add(cell); ft.Add(t);
                }
                else if (d12[gx, gy])
                {
                    TileBase t = F(lx, ly - 1) ? T["BoilerWall"] : (Hash(lx, ly + 3) % 100 < 12 ? T["WallTopB"] : T["WallTopA"]);
                    if (d2[gx, gy]) { wp.Add(cell); wt.Add(t); } else { xp.Add(cell); xt.Add(t); }
                }
            }
            floorMap.ClearAllTiles(); wallsMap.ClearAllTiles(); fillMap.ClearAllTiles();
            floorMap.SetTiles(fp.ToArray(), ft.ToArray());
            wallsMap.SetTiles(wp.ToArray(), wt.ToArray());
            fillMap.SetTiles(xp.ToArray(), xt.ToArray());
            floorMap.CompressBounds(); wallsMap.CompressBounds(); fillMap.CompressBounds();
            return "sàn=" + floorCount + " tường(collider)=" + wp.Count + " tường(lấp)=" + xp.Count;
        }

        // ---------- tiện ích scene ----------
        private static Vector3 W(float x, float y) { return new Vector3(OX + x, y, 0f); }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name); if (t != null) return t;
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }

        private static void Wipe(Transform parent, string name)
        {
            var t = parent.Find(name); if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        private static GameObject Tpl(Transform tplRoot, Transform search, string key, bool copy, params string[] originals)
        {
            var t = tplRoot.Find(key); if (t != null) return t.gameObject;
            foreach (var n in originals)
            {
                var f = FindDeep(search, n); if (f == null) continue;
                if (copy) { var c = Object.Instantiate(f.gameObject, tplRoot); c.name = key; return c; }
                f.SetParent(tplRoot, true); f.name = key; return f.gameObject;
            }
            throw new System.Exception("Không tìm thấy template '" + key + "' (" + string.Join("/", originals) + ")");
        }

        private static void SetObj(SerializedObject so, string prop, Object v)
        {
            var p = so.FindProperty(prop); if (p == null) { Debug.LogWarning("[Floor02] thiếu field " + prop + " trên " + so.targetObject.GetType().Name); return; }
            p.objectReferenceValue = v;
        }

        private static void SetZone(GameObject go, string id, string display, ZoneKind kind, int prio, string music, Vector2 center, Vector2 size, BoxCollider2D cam, bool keepId = false)
        {
            var t = go.transform; t.position = W(center.x, center.y); t.localScale = Vector3.one;
            var box = go.GetComponent<BoxCollider2D>(); box.isTrigger = true; box.size = size; box.offset = Vector2.zero;
            var zv = go.GetComponent<ZoneVolume>(); var so = new SerializedObject(zv);
            if (!keepId) so.FindProperty("zoneId").stringValue = id;
            so.FindProperty("displayName").stringValue = display;
            so.FindProperty("kind").intValue = (int)kind;
            so.FindProperty("priority").intValue = prio;
            so.FindProperty("musicId").stringValue = music;
            so.FindProperty("cameraBoundsCollider").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSafeRect(GameObject go, Vector2 rectCenter, Vector2 rectSize, float lampY)
        {
            go.transform.position = W(rectCenter.x, lampY);
            var box = go.GetComponent<BoxCollider2D>(); var ls = go.transform.lossyScale; box.isTrigger = true;
            box.size = new Vector2(rectSize.x / ls.x, rectSize.y / ls.y);
            box.offset = new Vector2(0f, (rectCenter.y - lampY) / ls.y);
        }

        // ---------- điểm vào chính ----------
        [MenuItem("Game/Floor 02/Build Big World")]
        public static void BuildMenu() { Debug.Log(Build()); }

        public static string Build()
        {
            var rep = new StringBuilder();
            Scene sc = default; bool found = false;
            for (int i = 0; i < SceneManager.sceneCount; i++) { var s = SceneManager.GetSceneAt(i); if (s.name == "Floor02" && s.isLoaded) { sc = s; found = true; } }
            if (!found) throw new System.Exception("Scene Floor02 chưa được mở.");
            Transform rootT = null;
            foreach (var r in sc.GetRootGameObjects()) if (r.name == "Floor02") rootT = r.transform;
            if (rootT == null) throw new System.Exception("Không thấy root 'Floor02'.");
            var content = rootT.Find("Content"); if (content == null) throw new System.Exception("Không thấy Content.");
            bool contentWasActive = content.gameObject.activeSelf;

            // 1. bố cục + tile
            BuildLayout();
            var floorMap = FindDeep(content, "Floor").GetComponent<Tilemap>();
            var wallsMap = FindDeep(content, "Walls").GetComponent<Tilemap>();
            var fillMap = EnsureFillMap(wallsMap);
            rep.AppendLine("Tile: " + BuildTiles(floorMap, wallsMap, fillMap));

            // 2. template (nằm NGOÀI Content để test/validator không đếm nhầm)
            var tplRoot = Child(rootT, "_Templates"); tplRoot.gameObject.SetActive(false);
            var tConvR = Tpl(tplRoot, content, "T_ConvR", false, "Conveyor_A_Right");
            var tConvL = Tpl(tplRoot, content, "T_ConvL", false, "Conveyor_B_Left");
            var tVent = Tpl(tplRoot, content, "T_Vent", false, "SteamVent_1");
            var tSpawner = Tpl(tplRoot, content, "T_Spawner", false, "EnemySpawner");
            var tZone = Tpl(tplRoot, content, "T_Zone", false, "Zone_Farm");
            var tSteam = Tpl(tplRoot, content, "T_Steam", true, "Ambient_Steam_Valve_T");
            var tSparks = Tpl(tplRoot, content, "T_Sparks", true, "Ambient_Sparks_ForgeLamp_T");
            var tEmbers = Tpl(tplRoot, content, "T_Embers", true, "Ambient_Embers_ForgeLamp_T");
            var props = new Dictionary<string, GameObject>
            {
                { "Crate", Tpl(tplRoot, content, "T_Crate", true, "Crate_T1") }, { "Barrel", Tpl(tplRoot, content, "T_Barrel", true, "Barrel_T1") },
                { "Rope", Tpl(tplRoot, content, "T_Rope", true, "Rope_T") }, { "Valve", Tpl(tplRoot, content, "T_Valve", true, "Valve_T") },
                { "Lamp", Tpl(tplRoot, content, "T_Lamp", true, "ForgeLamp_T") }, { "Sign", Tpl(tplRoot, content, "T_Sign", true, "Sign_T") },
            };
            var tPuddle = Tpl(tplRoot, content, "T_Puddle", true, "Puddle_A");
            // định nghĩa quái lấy từ spawner template
            var defs = new Dictionary<char, Object>(); var respawn = new Dictionary<char, float>();
            {
                var so = new SerializedObject(tSpawner.GetComponent<Game.PvE.Enemy.EnemySpawner>());
                var arr = so.FindProperty("entries");
                for (int i = 0; i < arr.arraySize; i++)
                {
                    var e = arr.GetArrayElementAtIndex(i); var d = e.FindPropertyRelative("definition").objectReferenceValue; if (d == null) continue;
                    string n = d.name.ToLowerInvariant(); char c = n.Contains("guard") ? 'G' : n.Contains("warden") ? 'W' : n.Contains("bat") ? 'B' : n.Contains("spitter") ? 'P' : n.Contains("stalker") ? 'S' : '?';
                    if (c != '?') { defs[c] = d; respawn[c] = Mathf.Max(8f, e.FindPropertyRelative("respawnSeconds").floatValue); }
                }
                foreach (char c in "GWBPS") if (!defs.ContainsKey(c)) throw new System.Exception("Spawner template thiếu định nghĩa quái '" + c + "'");
            }

            // 3. dọn nội dung cũ (giữ NPC, Boss, BossRoom, SafeZone_Lamp, Zones)
            var bossRoom = FindDeep(content, "BossRoom_Floor02"); var boss = FindDeep(content, "Boss_Floor02");
            foreach (string oldDecor in new[] { "Decor_Floor02", "Decor" })
            {
                var od = content.Find(oldDecor);
                if (od != null && od != bossRoom)
                {
                    var move = new List<Transform>();
                    foreach (Transform c in od) if (c.position.x >= 140f && c.position.x <= 165f && bossRoom != null) move.Add(c);
                    foreach (var m in move) m.SetParent(bossRoom, true);
                    Object.DestroyImmediate(od.gameObject);
                }
            }
            foreach (string n in new[] { "Hazards_Floor02", "Hazards", "SteamVent_2", "SteamVent_3", "Streamed", "SafeZones" }) Wipe(content, n);
            var zonesRoot = Child(content, "Zones");
            var oldGen = new List<GameObject>(); foreach (Transform z in zonesRoot) if (z.name.StartsWith("ZV_")) oldGen.Add(z.gameObject);
            foreach (var g in oldGen) Object.DestroyImmediate(g);
            var backdrop = FindDeep(content, "Backdrop"); if (backdrop != null) backdrop.gameObject.SetActive(false);

            // 4. khu + zone
            var dists = Dists(); var byKey = new Dictionary<string, Dist>();
            var streamed = Child(content, "Streamed");
            foreach (var d in dists)
            {
                byKey[d.Key] = d;
                d.Root = Child(streamed, "Zone_" + d.Key); d.SpawnersRoot = Child(d.Root, "Spawners"); d.PropsRoot = Child(d.Root, "Props");
                var c = new Vector2(d.Aabb.center.x, d.Aabb.center.y); var sz = new Vector2(d.Aabb.width, d.Aabb.height);
                if (d.Key == "Town")
                {
                    var tz = FindDeep(content, "Zone_Town").gameObject;
                    SetZone(tz, "", d.Name, d.Kind, d.Prio, d.Music, c, sz, null, true); d.Zone = tz.GetComponent<ZoneVolume>();
                }
                else
                {
                    var go = Object.Instantiate(tZone, zonesRoot); go.name = "ZV_" + d.Key;
                    SetZone(go, "zone.floor_02_" + d.Key.ToLowerInvariant(), d.Name, d.Kind, d.Prio, d.Music, c, sz, null);
                    d.Zone = go.GetComponent<ZoneVolume>();
                }
            }
            foreach (var rf in Refuges)
            {
                var go = Object.Instantiate(tZone, zonesRoot); go.name = "ZV_" + rf.Key;
                var parentBox = byKey[rf.ParentDist].Zone.GetComponent<BoxCollider2D>();
                SetZone(go, "zone.floor_02_" + rf.Key.ToLowerInvariant(), rf.Name, ZoneKind.Refuge, 2, "music.town", new Vector2(rf.Rect.center.x, rf.Rect.center.y), new Vector2(rf.Rect.width, rf.Rect.height), parentBox);
            }
            // ghi lại camera bounds của zone Plaza = chính nó (đã null); Town cũng dùng chính nó.

            // 5. boss + vùng boss (tuyệt đối theo local)
            if (boss != null && bossRoom != null)
            {
                var delta = W(188f, 0.5f) - boss.position;
                boss.position += delta; bossRoom.position += delta;
            }
            var zBossA = FindDeep(content, "Zone_BossApproach"); var zBossR = FindDeep(content, "Zone_BossRoom"); var cam = FindDeep(content, "BossArea_CameraBounds");
            if (zBossA != null) { zBossA.position = W(171f, 0.5f); zBossA.GetComponent<BoxCollider2D>().size = new Vector2(10f, 5f); }
            if (zBossR != null) { zBossR.position = W(188f, 0.5f); zBossR.GetComponent<BoxCollider2D>().size = new Vector2(24f, 25f); }
            if (cam != null) { cam.position = W(183.5f, 0.5f); cam.GetComponent<BoxCollider2D>().size = new Vector2(34f, 25f); }

            // 6. Safe Zone + NPC + điểm vào
            var lamp = FindDeep(content, "SafeZone_Lamp").gameObject;
            var safeRects = new List<Rect> { new Rect(-22, -14, 44, 28) };
            SetSafeRect(lamp, Vector2.zero, new Vector2(44f, 28f), 0.6f);
            var safeRoot = Child(content, "SafeZones");
            var plazaRect = byKey["Plaza"].Aabb;
            var allRefuges = new List<KeyValuePair<string, RectInt>> { new KeyValuePair<string, RectInt>("R1_Plaza", plazaRect) };
            foreach (var rf in Refuges) allRefuges.Add(new KeyValuePair<string, RectInt>(rf.Key, rf.Rect));
            foreach (var kv in allRefuges)
            {
                var go = Object.Instantiate(lamp, safeRoot); go.name = "SafeZone_" + kv.Key;
                SetSafeRect(go, new Vector2(kv.Value.center.x, kv.Value.center.y), new Vector2(kv.Value.width, kv.Value.height), kv.Value.center.y + 0.6f);
                safeRects.Add(new Rect(kv.Value.x, kv.Value.y, kv.Value.width, kv.Value.height));
            }
            var cart = FindDeep(content, "NPC_Cartographer"); var merch = FindDeep(content, "NPC_Merchant");
            if (cart != null) cart.position = W(1.8f, 0.9f);
            if (merch != null) merch.position = W(-1.8f, 0.9f);
            var entry = FindDeep(rootT, "EntryPoint"); if (entry != null) entry.position = W(0.5f, -3f);

            // 7. spawner
            var rng = new System.Random(20261024);
            var spawnerList = new List<Game.PvE.Enemy.EnemySpawner>(); int enemyTotal = 0; var warn = new List<string>();
            foreach (var sp in Spawns)
            {
                var d = byKey[sp.Dist]; var center = Snap(new Vector2(sp.X, sp.Y), sp.R, safeRects);
                if (!center.HasValue) { warn.Add("Không đặt được spawner " + sp.Key); continue; }
                var go = Object.Instantiate(tSpawner, d.SpawnersRoot); go.name = "Spawner_" + sp.Key; go.transform.position = W(center.Value.x, center.Value.y);
                var es = go.GetComponent<Game.PvE.Enemy.EnemySpawner>(); var so = new SerializedObject(es);
                var arr = so.FindProperty("entries"); arr.ClearArray(); int maxCount = 1;
                for (int i = 0; i + 1 < sp.Spec.Length; i += 2)
                {
                    char c = sp.Spec[i]; int n = sp.Spec[i + 1] - '0'; maxCount = Mathf.Max(maxCount, n); enemyTotal += n;
                    arr.InsertArrayElementAtIndex(arr.arraySize); var e = arr.GetArrayElementAtIndex(arr.arraySize - 1);
                    e.FindPropertyRelative("definition").objectReferenceValue = defs[c];
                    e.FindPropertyRelative("count").intValue = n; e.FindPropertyRelative("radius").floatValue = sp.R; e.FindPropertyRelative("respawnSeconds").floatValue = respawn[c];
                }
                SetObj(so, "spawnCenter", go.transform); SetObj(so, "spawnZone", d.Zone);
                var pool = so.FindProperty("poolSizePerDefinition"); if (pool != null) pool.intValue = maxCount;
                var fl = so.FindProperty("floorNumber"); if (fl != null) fl.intValue = 2;
                var sos = so.FindProperty("spawnOnStart"); if (sos != null) sos.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo(); spawnerList.Add(es);
            }

            // 8. băng chuyền
            foreach (var b in Belts)
            {
                var go = Object.Instantiate(b.Right ? tConvR : tConvL, byKey[b.Dist].PropsRoot); go.name = "Conveyor_" + b.Dist + "_" + (b.Right ? "R" : "L");
                go.transform.position = W(b.X, b.Y);
                var sr = go.GetComponent<SpriteRenderer>(); if (sr != null) sr.size = new Vector2(b.W, b.H);
                var bc = go.GetComponent<BoxCollider2D>(); if (bc != null) bc.size = new Vector2(b.W, b.H);
            }

            // 9. ống hơi, decor
            var used = new HashSet<Vector2Int>(); int ventCount = 0, propCount = 0, ambientCount = 0;
            Rect Expand(Rect r, float m) { return new Rect(r.x - m, r.y - m, r.width + 2 * m, r.height + 2 * m); }
            var beltRects = new List<Rect>(); foreach (var b in Belts) beltRects.Add(Expand(new Rect(b.X - b.W / 2f, b.Y - b.H / 2f, b.W, b.H), 1f));
            foreach (var d in dists)
            {
                var cells = new List<Vector2Int>();
                for (int x = d.Core.xMin; x < d.Core.xMax; x++) for (int y = d.Core.yMin; y < d.Core.yMax; y++)
                {
                    if (!Walk(x, y)) continue; var p = new Vector2(x + 0.5f, y + 0.5f); bool bad = false;
                    foreach (var br in beltRects) if (br.Contains(p)) { bad = true; break; }
                    if (d.Key == "Town" && Mathf.Abs(x) < 6 && y > -5 && y < 5) bad = true;
                    if (!bad) cells.Add(new Vector2Int(x, y));
                }
                Vector2Int? Pick(int clearance)
                {
                    for (int tries = 0; tries < 60 && cells.Count > 0; tries++)
                    {
                        var c = cells[rng.Next(cells.Count)]; bool ok = true;
                        for (int dx = -clearance; dx <= clearance && ok; dx++) for (int dy = -clearance; dy <= clearance; dy++) if (used.Contains(new Vector2Int(c.x + dx, c.y + dy))) { ok = false; break; }
                        if (ok) { used.Add(c); return c; }
                    }
                    return null;
                }
                for (int i = 0; i < d.Vents; i++)
                {
                    var c = Pick(5); if (!c.HasValue) break;
                    var go = Object.Instantiate(tVent, d.PropsRoot); go.name = "SteamVent_" + d.Key + "_" + i; go.transform.position = W(c.Value.x + 0.5f, c.Value.y + 0.5f); ventCount++;
                    var so = new SerializedObject(go.GetComponent<MonoBehaviour>() != null ? (Object)go.GetComponent(typeof(Game.PvE.Environment.SteamVent)) : go);
                    var it = so.GetIterator(); bool enter = true;
                    while (it.NextVisible(enter)) { enter = false; if (it.propertyType == SerializedPropertyType.Float && it.name.ToLowerInvariant().Contains("phase")) { it.floatValue = (float)(rng.NextDouble() * 4.0); break; } }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                for (int i = 0; i < d.Puddles; i++)
                {
                    var c = Pick(2); if (!c.HasValue) break;
                    var go = Object.Instantiate(tPuddle, d.PropsRoot); go.name = "Puddle"; go.transform.position = W(c.Value.x + 0.5f, c.Value.y + 0.5f);
                    var sr = go.GetComponent<SpriteRenderer>(); if (sr != null && sr.drawMode != SpriteDrawMode.Simple) sr.size = new Vector2(2 + rng.Next(3), 1 + rng.Next(2));
                }
                string[] keys = { "Crate", "Crate", "Crate", "Barrel", "Barrel", "Rope", "Valve", "Valve", "Lamp", "Lamp", "Sign" };
                for (int i = 0; i < d.Props; i++)
                {
                    var c = Pick(1); if (!c.HasValue) break;
                    string k = keys[rng.Next(keys.Length)]; if (k == "Sign" && rng.Next(3) != 0) k = "Crate";
                    var pos = W(c.Value.x + 0.5f + (float)(rng.NextDouble() - 0.5) * 0.5f, c.Value.y + 0.5f + (float)(rng.NextDouble() - 0.5) * 0.5f);
                    var go = Object.Instantiate(props[k], d.PropsRoot); go.name = k; go.transform.position = pos; propCount++;
                    if (k == "Lamp") { foreach (var a in new[] { tSparks, tEmbers }) { var am = Object.Instantiate(a, d.PropsRoot); am.name = a.name.Replace("T_", "Ambient_"); am.transform.position = pos; ambientCount++; } }
                    if (k == "Valve" && rng.Next(100) < 40) { var am = Object.Instantiate(tSteam, d.PropsRoot); am.name = "Ambient_Steam"; am.transform.position = pos; ambientCount++; }
                }
            }
            // đèn dọc hành lang tới boss
            var corr = byKey["Corridor"];
            for (int x = 120; x <= 164; x += 11)
                foreach (float y in new[] { 2.35f, -1.9f })
                {
                    var pos = W(x + 0.5f, y); var go = Object.Instantiate(props["Lamp"], corr.PropsRoot); go.name = "Lamp"; go.transform.position = pos; propCount++;
                    foreach (var a in new[] { tSparks, tEmbers }) { var am = Object.Instantiate(a, corr.PropsRoot); am.name = a.name.Replace("T_", "Ambient_"); am.transform.position = pos; ambientCount++; }
                }

            // 10. streamer
            var sGo = Child(content, "Streamer").gameObject;
            var streamer = sGo.GetComponent<FloorZoneStreamer>(); if (streamer == null) streamer = sGo.AddComponent<FloorZoneStreamer>();
            var sso = new SerializedObject(streamer); var gp = sso.FindProperty("groups"); gp.ClearArray();
            foreach (var d in dists)
            {
                gp.InsertArrayElementAtIndex(gp.arraySize); var e = gp.GetArrayElementAtIndex(gp.arraySize - 1);
                e.FindPropertyRelative("name").stringValue = d.Key; e.FindPropertyRelative("root").objectReferenceValue = d.Root.gameObject;
                e.FindPropertyRelative("area").rectValue = new Rect(OX + d.Aabb.x, d.Aabb.y, d.Aabb.width, d.Aabb.height);
            }
            sso.ApplyModifiedPropertiesWithoutUndo();

            // 11. registrar
            var reg = rootT.GetComponentInChildren<Game.PvE.Floor.FloorSpawnRegistrar>(true);
            if (reg != null && spawnerList.Count > 0)
            {
                var rso = new SerializedObject(reg); SetObj(rso, "enemySpawner", spawnerList[0]);
                var it = rso.GetIterator(); bool enter = true; bool wrote = false;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (it.isArray && it.propertyType != SerializedPropertyType.String && it.name.ToLowerInvariant().Contains("spawner"))
                    {
                        it.ClearArray(); for (int i = 1; i < spawnerList.Count; i++) { it.InsertArrayElementAtIndex(it.arraySize); it.GetArrayElementAtIndex(it.arraySize - 1).objectReferenceValue = spawnerList[i]; }
                        wrote = true; break;
                    }
                }
                rso.ApplyModifiedPropertiesWithoutUndo();
                if (!wrote) warn.Add("Không tìm thấy mảng additionalSpawners trên FloorSpawnRegistrar!");
            }
            else warn.Add("Không có FloorSpawnRegistrar hoặc spawner.");

            // 12. kiểm tra
            rep.AppendLine(Verify(byKey, spawnerList, safeRects, warn));
            rep.AppendLine("Spawner=" + spawnerList.Count + " quái(đồng thời tối đa)=" + enemyTotal + " ống hơi=" + ventCount + " props=" + propCount + " ambient=" + ambientCount);
            foreach (var w in warn) rep.AppendLine("CẢNH BÁO: " + w);
            rep.AppendLine(Floor02MapSystemInstaller.Install(content, new Rect(OX + MinX, MinY, GW, GH)));
            content.gameObject.SetActive(contentWasActive);
            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            rep.AppendLine("Đã lưu Floor02.unity. Content active=" + content.gameObject.activeSelf);
            return rep.ToString();
        }

        // ---------- snap + kiểm tra ----------
        private static bool DiscOk(Vector2 c, float r)
        {
            if (!WalkP(c)) return false;
            for (int k = 0; k < 12; k++)
            {
                float a = k * Mathf.PI / 6f;
                for (int m = 1; m <= 2; m++) if (!WalkP(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * (m / 2f))) return false;
            }
            return true;
        }

        private static Vector2? Snap(Vector2 c, float r, List<Rect> safe)
        {
            for (int d = 0; d <= 24; d++)
                for (int dx = -d; dx <= d; dx++) for (int dy = -d; dy <= d; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != d) continue;
                    var q = c + new Vector2(dx, dy); if (!DiscOk(q, r)) continue;
                    var rect = new Rect(q.x - r - 0.5f, q.y - r - 0.5f, 2 * r + 1f, 2 * r + 1f); bool hit = false;
                    foreach (var s in safe) if (rect.Overlaps(s)) { hit = true; break; }
                    if (!hit) return q;
                }
            return null;
        }

        private static string Verify(Dictionary<string, Dist> byKey, List<Game.PvE.Enemy.EnemySpawner> spawners, List<Rect> safe, List<string> warn)
        {
            var sb = new StringBuilder();
            var start = new Vector2Int(0, -3);
            var dist = new Dictionary<Vector2Int, int>(); var q = new Queue<Vector2Int>(); dist[start] = 0; q.Enqueue(start);
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var dd in dirs) { var n = c + dd; if (!dist.ContainsKey(n) && Walk(n.x, n.y)) { dist[n] = dist[c] + 1; q.Enqueue(n); } }
            }
            int walkable = 0; for (int x = 0; x < GW; x++) for (int y = 0; y < GH; y++) if (Walk(x + MinX, y + MinY)) walkable++;
            sb.AppendLine("Ô đi được=" + walkable + " | tới được từ điểm vào=" + dist.Count + (dist.Count == walkable ? " (tất cả liên thông)" : " (CÓ VÙNG BỊ CÔ LẬP!)"));
            if (dist.Count != walkable) warn.Add("Có " + (walkable - dist.Count) + " ô đi được không nối với điểm vào");
            void Poi(string n, int x, int y) { int v; sb.AppendLine("  " + n + ": " + (dist.TryGetValue(new Vector2Int(x, y), out v) ? v + " bước" : "KHÔNG TỚI ĐƯỢC")); if (v == 0 && !(x == 0 && y == -3)) warn.Add("Không tới được " + n); }
            Poi("Boss (188,0)", 188, 0); Poi("Phòng phụ S1", 135, 8); Poi("Phòng phụ S2", 155, -8);
            Poi("Ngã Tư Áp Lực", 106, 0); Poi("Cuối Mê Cung Ống", 20, -80); Poi("Cuối Bãi Phế Liệu", -100, 30); Poi("Đỉnh Lò Hơi", 20, 75); Poi("Đỉnh Xưởng Rèn", 100, 70);
            foreach (var r in Refuges) Poi("Refuge " + r.Key, Mathf.FloorToInt(r.Rect.center.x), Mathf.FloorToInt(r.Rect.center.y));
            return sb.ToString();
        }
    }
}
