using UnityEngine;

namespace Game.PvE.Floor03
{
    public enum Biome : byte { DeepWater, ShallowWater, Beach, Plains, Forest, GiantForest, DarkForest, Swamp, Hills, Mountain, Snow, Canyon }

    public enum PoiKind : byte { Town, Village, Labyrinth, Dungeon, CampForest, CampDark, FieldBoss }

    public enum Prop : byte { None, Tree, GiantTree, Pine, Rock, Peak, SnowPeak, Pillar, Reed }

    public struct Poi
    {
        public string Name;
        public PoiKind Kind;
        public Vector2 Pos;
        public string Boss;
    }

    public struct Region
    {
        public string Name;
        public Vector2 C;
        public float Rad, E, M, D;
    }

    public struct TileInfo
    {
        public Biome Biome;
        public bool Blocked;
        public Prop Prop;
        public int Variant;
    }

    public struct SpawnGroup
    {
        public string Key;
        public int Count;
    }

    /// <summary>
    /// Thế giới tầng 3 "Zumfut và Rừng Cổ Thụ": sinh vô hạn, xác định hoàn toàn theo Seed.
    /// Toạ độ cục bộ (0,0) = Zumfut, +y = hướng bắc. Cùng một (x,y) luôn ra cùng một kết quả,
    /// nên chunk nào cũng sinh lại được mà không cần lưu.
    /// Vùng lõi (CoreRadius) có diện tích đúng 2 lần tầng 2 (350 x 196 tile); ngoài lõi là Hoang Dã Vô Tận.
    /// </summary>
    public static class Floor03World
    {
        public const int Seed = 3003;
        public const int Chunk = 32;

        /// <summary>Bán kính vùng lõi: pi * R^2 = 2 * 350 * 196.</summary>
        public static readonly float CoreRadius = Mathf.Sqrt(2f * 350f * 196f / Mathf.PI);

        // Hệ số co so với bản thiết kế gốc (bán kính 1414), dùng để co tần số nhiễu cho vừa kích thước thật.
        static readonly float K = CoreRadius / 1414f;

        public static readonly Region[] Regions;
        public static readonly Poi[] Pois;

        static Floor03World()
        {
            Regions = new[]
            {
                Rg("Rừng Cổ Thụ Zumfut", 0f, 0f, .42f, .52f, .72f, 0f),
                Rg("Cánh Đồng Zumfut", 0f, 0f, .10f, .52f, .40f, 0f),
                Rg("Hồ Gương Sương Mai", .54f, 252f, .15f, .22f, .60f, 0f),
                Rg("Rừng Bóng Tối", .64f, 150f, .26f, .52f, .74f, .95f),
                Rg("Rừng Sương Chao Đảo", .60f, 302f, .17f, .40f, .94f, .10f),
                Rg("Dãy Núi Hoàng Hôn", .88f, 35f, .22f, .80f, .50f, 0f),
                Rg("Hẻm Đá Đỏ", .74f, 196f, .18f, .58f, .10f, 0f),
                Rg("Đỉnh Tuyết Bạch Dương", .86f, 336f, .15f, .93f, .50f, 0f),
                Rg("Thảo Nguyên Gió Hú", .58f, 86f, .20f, .50f, .40f, 0f),
            };

            Pois = new[]
            {
                Pt("Zumfut", PoiKind.Town, 0f, 0f, null),
                Pt("Mê Cung Thụ Tháp", PoiKind.Labyrinth, .80f, 45f, "nerius"),
                Pt("Dessel", PoiKind.Village, .20f, 205f, null),
                Pt("Bến Hồ Gương", PoiKind.Village, .38f, 262f, null),
                Pt("Chốt Gió Hú", PoiKind.Village, .48f, 80f, null),
                Pt("Làng Thợ Đá Đỏ", PoiKind.Village, .66f, 194f, null),
                Pt("Trạm Bạch Dương", PoiKind.Village, .72f, 342f, null),
                Pt("Doanh Trại Elf Rừng", PoiKind.CampForest, .34f, 118f, null),
                Pt("Thành Lũy Elf Bóng Tối", PoiKind.CampDark, .62f, 150f, null),
                Pt("Hang Nhện Mẹ", PoiKind.Dungeon, .30f, 330f, "queen_weaver"),
                Pt("Hầm Mỏ Bỏ Hoang", PoiKind.Dungeon, .66f, 62f, null),
                Pt("Tàn Tích Rễ Cổ", PoiKind.Dungeon, .50f, 232f, null),
                Pt("Hang Dơi Sương", PoiKind.Dungeon, .82f, 10f, null),
                Pt("Chúa Sói Alpha", PoiKind.FieldBoss, .62f, 66f, "alpha_wolf"),
                Pt("Cổ Nham Thủ Hộ", PoiKind.FieldBoss, .80f, 205f, "elder_golem"),
                Pt("Chỉ Huy Elf Bóng Tối", PoiKind.FieldBoss, .72f, 138f, "dark_commander"),
            };
        }

        // r theo CoreRadius, góc theo độ (0 = bắc, chiều kim đồng hồ), +y = bắc.
        static Vector2 Polar(float r, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector2(r * CoreRadius * Mathf.Sin(a), r * CoreRadius * Mathf.Cos(a));
        }

        static Region Rg(string name, float r, float deg, float rad, float e, float m, float d)
        {
            return new Region { Name = name, C = Polar(r, deg), Rad = rad * CoreRadius, E = e, M = m, D = d };
        }

        static Poi Pt(string name, PoiKind kind, float r, float deg, string boss)
        {
            return new Poi { Name = name, Kind = kind, Pos = Polar(r, deg), Boss = boss };
        }

        // ───────────────────────── nhiễu giá trị

        public static float Hash(int ix, int iy, int s)
        {
            unchecked
            {
                uint n = (uint)(ix * 374761393 + iy * 668265263 + s * 1442695041);
                n = (n ^ (n >> 13)) * 1274126177u;
                n ^= n >> 16;
                return (n & 0xFFFFFF) / 16777216f;
            }
        }

        static float VNoise(float x, float y, int s)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0, y0, s), b = Hash(x0 + 1, y0, s);
            float c = Hash(x0, y0 + 1, s), d = Hash(x0 + 1, y0 + 1, s);
            float top = a + (b - a) * fx, bot = c + (d - c) * fx;
            return top + (bot - top) * fy;
        }

        static float Fbm(float x, float y, int s, int octaves)
        {
            float tot = 0f, amp = 1f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                tot += amp * VNoise(x, y, s + o * 131);
                norm += amp;
                x *= 2.03f;
                y *= 2.03f;
                amp *= .5f;
            }
            return tot / norm;
        }

        static float Smooth(float t) { return t * t * (3f - 2f * t); }

        // ───────────────────────── địa hình

        static void Fields(float x, float y, out float e, out float m, out float d, out float pf)
        {
            float k = K;
            e = .5f + 1.4f * (Fbm(x / (520f * k), y / (520f * k), Seed + 1, 6) - .5f);
            m = .58f + 1.5f * (Fbm(x / (380f * k) + 17f, y / (380f * k) - 9f, Seed + 2, 5) - .5f);
            d = .5f + 1.5f * (Fbm(x / (700f * k) - 5f, y / (700f * k) + 3f, Seed + 3, 4) - .5f);
            float wob = Fbm(x / (90f * k), y / (90f * k), Seed + 9, 3);

            for (int i = 0; i < Regions.Length; i++)
            {
                Region r = Regions[i];
                float dx = x - r.C.x, dy = y - r.C.y;
                float w = Smooth(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / (r.Rad * (.65f + .7f * wob))));
                e += (r.E - e) * w;
                m += (r.M - m) * w;
                d += (r.D - d) * w;
            }

            pf = 0f;
            for (int i = 0; i < Pois.Length; i++)
            {
                Poi p = Pois[i];
                float rad = p.Kind == PoiKind.Town ? 14f : 7f;
                float dx = x - p.Pos.x, dy = y - p.Pos.y;
                float w = Smooth(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / rad));
                e += (.52f - e) * w;
                if (w > pf) pf = w;
            }
        }

        static Biome Classify(float x, float y, float e, float m, float d, float pf)
        {
            if (e > .37f && e < .66f && pf < .3f &&
                Mathf.Abs(Fbm(x / (600f * K) + 40f, y / (600f * K) - 20f, Seed + 4, 3) - .5f) < .009f)
                return Biome.ShallowWater;                                  // sông

            Biome b = Biome.Plains;
            if (m >= .46f) b = Biome.Forest;
            if (m >= .62f) b = Biome.GiantForest;
            if (m >= .46f && d > .80f) b = Biome.DarkForest;
            if (m >= .84f && e < .55f) b = Biome.Swamp;
            if (m < .28f) b = Biome.Canyon;
            if (e >= .64f) b = Biome.Hills;
            if (e >= .74f) b = Biome.Mountain;
            if (e >= .86f) b = Biome.Snow;
            if (e < .365f) b = Biome.Beach;
            if (e < .34f) b = Biome.ShallowWater;
            if (e < .30f) b = Biome.DeepWater;
            return b;
        }

        static bool BlockedBiome(Biome b, int x, int y)
        {
            switch (b)
            {
                case Biome.DeepWater:
                case Biome.Snow:
                    return true;
                case Biome.Mountain:                                        // vách đá rải rác, vẫn còn đường đi
                    return VNoise(x / 2.5f, y / 2.5f, Seed + 77) > .62f;
                default:
                    return false;
            }
        }

        static Prop PropAt(Biome b, int x, int y)
        {
            float r = Hash(x, y, Seed + 50);
            switch (b)
            {
                case Biome.GiantForest: return r < .16f ? Prop.GiantTree : Prop.None;
                case Biome.Forest: return r < .10f ? Prop.Tree : Prop.None;
                case Biome.DarkForest: return r < .13f ? Prop.Pine : Prop.None;
                case Biome.Swamp: return r < .06f ? Prop.Reed : Prop.None;
                case Biome.Hills: return r < .04f ? Prop.Rock : Prop.None;
                case Biome.Mountain: return r < .10f ? Prop.Peak : Prop.None;
                case Biome.Snow: return r < .12f ? Prop.SnowPeak : Prop.None;
                case Biome.Canyon: return r < .05f ? Prop.Pillar : Prop.None;
                default: return Prop.None;
            }
        }

        /// <summary>Thông tin một ô tile ở toạ độ cục bộ (x, y).</summary>
        public static TileInfo Sample(int x, int y)
        {
            float fx = x + .5f, fy = y + .5f;
            float e, m, d, pf;
            Fields(fx, fy, out e, out m, out d, out pf);
            Biome b = Classify(fx, fy, e, m, d, pf);
            TileInfo t;
            t.Biome = b;
            t.Blocked = pf < .5f && BlockedBiome(b, x, y);                 // khu địa điểm luôn thông thoáng
            t.Prop = pf > .35f ? Prop.None : PropAt(b, x, y);
            t.Variant = (int)(Hash(x, y, Seed + 60) * 4f) & 3;
            return t;
        }

        public static Vector2Int ChunkOf(float x, float y)
        {
            return new Vector2Int(Mathf.FloorToInt(x / Chunk), Mathf.FloorToInt(y / Chunk));
        }

        // ───────────────────────── vùng an toàn và độ khó

        static float SafeRadius(PoiKind k)
        {
            switch (k)
            {
                case PoiKind.Town: return 30f;
                case PoiKind.Village: return 14f;
                case PoiKind.CampForest:
                case PoiKind.CampDark: return 10f;
                default: return 0f;
            }
        }

        /// <summary>Quanh thị trấn, làng và trại Elf không đặt spawner.</summary>
        public static bool IsSafe(float x, float y)
        {
            for (int i = 0; i < Pois.Length; i++)
            {
                float r = SafeRadius(Pois[i].Kind);
                if (r <= 0f) continue;
                float dx = x - Pois[i].Pos.x, dy = y - Pois[i].Pos.y;
                if (dx * dx + dy * dy < r * r) return true;
            }
            return false;
        }

        /// <summary>
        /// Số tầng dùng để tính hệ số sức mạnh quái (FloorScaling). Trong vùng lõi giữ nguyên số tầng gốc (x5 so với tầng 2 nhờ dữ liệu
        /// EnemyDefinition); ra ngoài rìa lõi cộng thêm 1 tầng cho mỗi nửa bán kính, tối đa +6.
        /// </summary>
        public static int FloorNumberAt(float x, float y, int baseFloor)
        {
            float dist = Mathf.Sqrt(x * x + y * y);
            if (dist <= CoreRadius) return baseFloor;
            return baseFloor + Mathf.Min(6, 1 + Mathf.FloorToInt((dist - CoreRadius) / (CoreRadius * .5f)));
        }

        // ───────────────────────── bầy quái theo địa hình

        static SpawnGroup G(string key, int count)
        {
            SpawnGroup g;
            g.Key = key;
            g.Count = count;
            return g;
        }

        public static SpawnGroup[] SpawnFor(Biome b)
        {
            switch (b)
            {
                case Biome.Plains: return new[] { G("frenzy_boar", 2), G("dire_wolf", 2), G("hornet", 2) };
                case Biome.Forest: return new[] { G("dire_wolf", 2), G("nepent", 2), G("weaver", 1), G("frenzy_boar", 1) };
                case Biome.GiantForest: return new[] { G("sapling", 1), G("weaver", 2), G("nepent", 2), G("wisp", 2), G("forest_elf", 1) };
                case Biome.DarkForest: return new[] { G("dark_elf", 1), G("wisp", 2), G("bat", 2), G("weaver", 1) };
                case Biome.Swamp: return new[] { G("serpent", 2), G("toad", 2), G("nepent", 1), G("wisp", 1) };
                case Biome.Hills: return new[] { G("kobold", 2), G("bat", 2), G("moss_golem", 1), G("hornet", 1) };
                case Biome.Mountain: return new[] { G("moss_golem", 1), G("bat", 2), G("frost_wolf", 2), G("kobold", 1) };
                case Biome.Canyon: return new[] { G("scorpion", 3), G("kobold", 2), G("moss_golem", 1) };
                case Biome.Beach: return new[] { G("toad", 2), G("hornet", 1) };
                default: return new SpawnGroup[0];
            }
        }

        /// <summary>Trùm phụ ngoài hoang dã theo địa hình.</summary>
        public static string OuterBossFor(Biome b)
        {
            switch (b)
            {
                case Biome.GiantForest:
                case Biome.Swamp: return "queen_weaver";
                case Biome.DarkForest: return "dark_commander";
                case Biome.Hills:
                case Biome.Mountain:
                case Biome.Canyon: return "elder_golem";
                default: return "alpha_wolf";
            }
        }

        static bool WalkableSite(int tx, int ty, out Biome biome)
        {
            TileInfo t = Sample(tx, ty);
            biome = t.Biome;
            return !t.Blocked && t.Biome > Biome.ShallowWater && t.Biome != Biome.Snow && !IsSafe(tx, ty);
        }

        /// <summary>Vị trí spawner thường của chunk (nếu có).</summary>
        public static bool TrySpawnerSite(int cx, int cy, out Vector2Int tile, out Biome biome)
        {
            tile = default(Vector2Int);
            biome = Biome.Plains;
            if (Hash(cx, cy, Seed + 301) > .85f) return false;
            int tx = cx * Chunk + 3 + (int)(Hash(cx, cy, Seed + 302) * (Chunk - 6));
            int ty = cy * Chunk + 3 + (int)(Hash(cx, cy, Seed + 303) * (Chunk - 6));
            if (!WalkableSite(tx, ty, out biome)) return false;
            if (SpawnFor(biome).Length == 0) return false;
            tile = new Vector2Int(tx, ty);
            return true;
        }

        /// <summary>Trùm phụ ngẫu nhiên ở vùng ngoài lõi (Hoang Dã Vô Tận).</summary>
        public static bool TryOuterBoss(int cx, int cy, out Vector2Int tile, out string bossKey)
        {
            tile = default(Vector2Int);
            bossKey = null;
            float mx = (cx + .5f) * Chunk, my = (cy + .5f) * Chunk;
            if (Mathf.Sqrt(mx * mx + my * my) < CoreRadius * .9f) return false;
            if (Hash(cx, cy, Seed + 311) > .05f) return false;
            int tx = cx * Chunk + 4 + (int)(Hash(cx, cy, Seed + 312) * (Chunk - 8));
            int ty = cy * Chunk + 4 + (int)(Hash(cx, cy, Seed + 313) * (Chunk - 8));
            Biome b;
            if (!WalkableSite(tx, ty, out b)) return false;
            tile = new Vector2Int(tx, ty);
            bossKey = OuterBossFor(b);
            return true;
        }
    }
}
