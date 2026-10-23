#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Game.PvE.Environment;
using Game.PvE.Floor;
using Game.PvE.Enemy;

namespace Game.EditorTools
{
    /// <summary>
    /// Bộ dựng Map tự động cho 3 thế giới phiêu lưu:
    /// 1. Rừng Rậm Cổ Đại (Ancient Forest)
    /// 2. Hầm Mỏ Dung Nham (Lava Mines)
    /// 3. Lâu Đài Bóng Tối (Dark Castle)
    /// 
    /// Tự động thiết lập Sprite, tạo Tile, quét ma trận Tilemap, đặt Safe Zone,
    /// bãi quái, cạm bẫy môi trường (dung nham/bụi gai/vực thẳm) và các công trình tương tác.
    /// </summary>
    public static class ThreeRealmsMapBuilder
    {
        private const string ForestArtDir = "Assets/_Game/Art/Environment/AncientForest";
        private const string LavaArtDir = "Assets/_Game/Art/Environment/LavaMines";
        private const string CastleArtDir = "Assets/_Game/Art/Environment/DarkCastle";

        private const string SlimePath = "Assets/_Game/Data/Enemies/Enemy_Slime.asset";
        private const string BatPath = "Assets/_Game/Data/Enemies/Enemy_Bat.asset";

        // ═════════════════════════════════════════════════════════════════════════
        // MENU ITEMS
        // ═════════════════════════════════════════════════════════════════════════

        [MenuItem("Game/Realms/1. Dựng Rừng Rậm Cổ Đại (Ancient Forest)")]
        public static void BuildAncientForest()
        {
            var ctx = PrepareSceneContext();
            if (ctx == null) return;

            // 1. Tạo Tile
            var grass = GetOrCreateTile(ForestArtDir, "Forest_Grass", Tile.ColliderType.None);
            var path = GetOrCreateTile(ForestArtDir, "Forest_Path", Tile.ColliderType.None);
            var water = GetOrCreateTile(ForestArtDir, "Forest_Water", Tile.ColliderType.Grid);
            var wall = GetOrCreateTile(ForestArtDir, "Forest_TreeWall", Tile.ColliderType.Grid);

            // 2. Vẽ Bản Đồ Rừng Rậm (-20..70, -20..30)
            ctx.Tilemap.ClearAllTiles();
            var positions = new List<Vector3Int>();
            var tiles = new List<TileBase>();

            for (int x = -16; x <= 64; x++)
            {
                for (int y = -16; y <= 24; y++)
                {
                    TileBase t = wall; // Mặc định là tường rừng cây cổ thụ

                    // Thị trấn Rừng Cổ (Safe Zone)
                    if (x >= -8 && x <= 8 && y >= -6 && y <= 6)
                    {
                        t = (Mathf.Abs(x) <= 2 || Mathf.Abs(y) <= 2) ? path : grass;
                    }
                    // Đường mòn phía Bắc tới Bãi Nấm Dạ Quang
                    else if (x >= -4 && x <= 4 && y > 6 && y <= 18)
                    {
                        t = path;
                    }
                    else if (x >= -12 && x <= 12 && y >= 12 && y <= 20)
                    {
                        t = grass; // Bãi Nấm
                    }
                    // Đường sông rừng ngăn cách phía Đông
                    else if (x >= 14 && x <= 18 && y >= -14 && y <= 22)
                    {
                        // Cầu gỗ bắc qua sông ở y = 0..2
                        t = (y >= -1 && y <= 3) ? path : water;
                    }
                    // Vùng Rễ Gai Cổ Thụ (East Farm Zone)
                    else if (x >= 20 && x <= 40 && y >= -12 && y <= 16)
                    {
                        t = (x >= 28 && x <= 32) ? path : grass;
                    }
                    // Lối vào Đền Rừng Cổ / Phòng Boss
                    else if (x >= 42 && x <= 46 && y >= -2 && y <= 2)
                    {
                        t = path;
                    }
                    else if (x >= 46 && x <= 60 && y >= -10 && y <= 10)
                    {
                        t = (x == 46 || x == 60 || y == -10 || y == 10) ? wall : path; // Đấu Trường Boss
                    }

                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(t);
                }
            }

            ctx.Tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            ctx.Tilemap.RefreshAllTiles();

            // 3. Đặt các thực thể tương tác & props
            ClearProps(ctx.ContentRoot);

            // Suối Sinh Mệnh trong Safe Zone
            var springSprite = LoadSprite(ForestArtDir + "/Prop_Forest_AncientShrine.png");
            CreateInteractiveProp(ctx.ContentRoot, "AncientLifeSpring", new Vector3(0f, 3.5f, 0f), springSprite, typeof(AncientLifeSpring));

            // Bụi gai cản đường (Thorn Hazards) ở Farm Zone East
            var thornSprite = LoadSprite(ForestArtDir + "/Prop_Forest_ThornBush.png");
            CreateHazardProp(ctx.ContentRoot, "ThornBramble_1", new Vector3(25f, 5f, 0f), thornSprite, HazardType.ThornBramble);
            CreateHazardProp(ctx.ContentRoot, "ThornBramble_2", new Vector3(35f, -4f, 0f), thornSprite, HazardType.ThornBramble);

            // Cây nấm dạ quang ở Bãi Bắc
            var shroomSprite = LoadSprite(ForestArtDir + "/Prop_Forest_Mushroom.png");
            CreateSimpleProp(ctx.ContentRoot, "GlowMushroom_1", new Vector3(-8f, 16f, 0f), shroomSprite);
            CreateSimpleProp(ctx.ContentRoot, "GlowMushroom_2", new Vector3(6f, 17f, 0f), shroomSprite);

            // Safe Zone
            ConfigureSafeZone(ctx.Lamp, new Vector2(18f, 14f), new Vector2(0f, 0f));

            // Spawners
            ConfigureSpawners(ctx.ContentRoot, ctx.Registrar,
                new Vector3(-2f, 15f, 0f),   // Bắc: Bãi Nấm
                new Vector3(30f, 4f, 0f),    // Đông: Bãi Rễ Gai
                new Vector3(30f, -6f, 0f)    // Nam Đông
            );

            // Boss vị trí giữa đấu trường đền rừng
            if (ctx.Boss != null) ctx.Boss.transform.position = new Vector3(53f, 0f, 0f);

            SaveAll(ctx.Tilemap);
            Debug.Log("[ThreeRealmsMapBuilder] ✓ Đã dựng xong RỪNG RẬM CỔ ĐẠI (Ancient Forest)!");
        }

        [MenuItem("Game/Realms/2. Dựng Hầm Mỏ Dung Nham (Lava Mines)")]
        public static void BuildLavaMines()
        {
            var ctx = PrepareSceneContext();
            if (ctx == null) return;

            var basalt = GetOrCreateTile(LavaArtDir, "Lava_Basalt", Tile.ColliderType.None);
            var ash = GetOrCreateTile(LavaArtDir, "Lava_Ash", Tile.ColliderType.None);
            var magma = GetOrCreateTile(LavaArtDir, "Lava_Magma", Tile.ColliderType.Grid);
            var rockWall = GetOrCreateTile(LavaArtDir, "Lava_RockWall", Tile.ColliderType.Grid);

            ctx.Tilemap.ClearAllTiles();
            var positions = new List<Vector3Int>();
            var tiles = new List<TileBase>();

            for (int x = -16; x <= 64; x++)
            {
                for (int y = -16; y <= 24; y++)
                {
                    TileBase t = rockWall; // Mặc định là tường đá núi lửa

                    // Tiền Đồn Thợ Mỏ (Safe Zone)
                    if (x >= -8 && x <= 8 && y >= -6 && y <= 6)
                    {
                        t = basalt;
                    }
                    // Đường hầm mỏ quặng phía Bắc
                    else if (x >= -4 && x <= 4 && y > 6 && y <= 20)
                    {
                        t = ash;
                    }
                    else if (x >= -14 && x <= 14 && y >= 14 && y <= 22)
                    {
                        t = basalt;
                    }
                    // Hồ dung nham rực cháy
                    else if (x >= 12 && x <= 22 && y >= -14 && y <= 22)
                    {
                        // Cầu sắt bắc qua hồ dung nham
                        t = (y >= -1 && y <= 3) ? basalt : magma;
                    }
                    // Khu Đào Quặng (East Mine Zone)
                    else if (x >= 24 && x <= 42 && y >= -12 && y <= 16)
                    {
                        t = (x % 6 == 0 || y % 6 == 0) ? ash : basalt;
                    }
                    // Hang Lò Nấu Quặng (Boss Arena)
                    else if (x >= 44 && x <= 48 && y >= -2 && y <= 2)
                    {
                        t = basalt;
                    }
                    else if (x >= 48 && x <= 62 && y >= -10 && y <= 10)
                    {
                        t = (x == 48 || x == 62 || y == -10 || y == 10) ? rockWall : basalt;
                    }

                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(t);
                }
            }

            ctx.Tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            ctx.Tilemap.RefreshAllTiles();

            ClearProps(ctx.ContentRoot);

            // Đe Rèn Dung Nham (Molten Anvil - buff +35% dmg)
            var anvilSprite = LoadSprite(LavaArtDir + "/Prop_Lava_Anvil.png");
            CreateInteractiveProp(ctx.ContentRoot, "MoltenForgeAnvil", new Vector3(0f, 3.5f, 0f), anvilSprite, typeof(MoltenForgeAnvil));

            // Xe goòng pha lê
            var cartSprite = LoadSprite(LavaArtDir + "/Prop_Lava_OreCart.png");
            CreateSimpleProp(ctx.ContentRoot, "OreCart_1", new Vector3(-8f, 18f, 0f), cartSprite);
            CreateSimpleProp(ctx.ContentRoot, "OreCart_2", new Vector3(30f, 10f, 0f), cartSprite);

            // Ống phun nhiệt / Miệng dung nham (Geyser hazard)
            var geyserSprite = LoadSprite(LavaArtDir + "/Prop_Lava_Geyser.png");
            CreateHazardProp(ctx.ContentRoot, "MagmaVent_1", new Vector3(28f, -4f, 0f), geyserSprite, HazardType.MagmaLava);
            CreateHazardProp(ctx.ContentRoot, "MagmaVent_2", new Vector3(36f, 6f, 0f), geyserSprite, HazardType.MagmaLava);

            // Safe Zone
            ConfigureSafeZone(ctx.Lamp, new Vector2(18f, 14f), new Vector2(0f, 0f));

            // Spawners
            ConfigureSpawners(ctx.ContentRoot, ctx.Registrar,
                new Vector3(0f, 18f, 0f),    // Bắc: Hang Quặng
                new Vector3(32f, 4f, 0f),    // Đông: Mỏ Sắt
                new Vector3(32f, -6f, 0f)    // Nam Đông
            );

            if (ctx.Boss != null) ctx.Boss.transform.position = new Vector3(55f, 0f, 0f);

            SaveAll(ctx.Tilemap);
            Debug.Log("[ThreeRealmsMapBuilder] ✓ Đã dựng xong HẦM MỎ DUNG NHAM (Lava Mines)!");
        }

        [MenuItem("Game/Realms/3. Dựng Lâu Đài Bóng Tối (Dark Castle)")]
        public static void BuildDarkCastle()
        {
            var ctx = PrepareSceneContext();
            if (ctx == null) return;

            var cobble = GetOrCreateTile(CastleArtDir, "Castle_Cobble", Tile.ColliderType.None);
            var carpet = GetOrCreateTile(CastleArtDir, "Castle_Carpet", Tile.ColliderType.None);
            var castleWall = GetOrCreateTile(CastleArtDir, "Castle_Wall", Tile.ColliderType.Grid);
            var voidChasm = GetOrCreateTile(CastleArtDir, "Castle_Void", Tile.ColliderType.Grid);

            ctx.Tilemap.ClearAllTiles();
            var positions = new List<Vector3Int>();
            var tiles = new List<TileBase>();

            for (int x = -16; x <= 64; x++)
            {
                for (int y = -16; y <= 24; y++)
                {
                    TileBase t = castleWall; // Mặc định là tường lâu đài Gothic

                    // Đại Sảnh Hoàng Gia (Safe Zone)
                    if (x >= -8 && x <= 8 && y >= -6 && y <= 6)
                    {
                        // Thảm đỏ chạy dọc giữa sảnh
                        t = (y >= -1 && y <= 1) ? carpet : cobble;
                    }
                    // Hành lang cánh Bắc
                    else if (x >= -4 && x <= 4 && y > 6 && y <= 20)
                    {
                        t = (x >= -1 && x <= 1) ? carpet : cobble;
                    }
                    else if (x >= -14 && x <= 14 && y >= 14 && y <= 22)
                    {
                        t = cobble;
                    }
                    // Hào Vực Thẳm Hư Vô (Void Chasm)
                    else if (x >= 12 && x <= 20 && y >= -14 && y <= 22)
                    {
                        // Cầu đá trải thảm đỏ bắc qua vực thẳm
                        t = (y >= -1 && y <= 3) ? carpet : voidChasm;
                    }
                    // Sảnh Mê Cung Bóng Tối (East Hall)
                    else if (x >= 22 && x <= 42 && y >= -12 && y <= 16)
                    {
                        t = (y >= -1 && y <= 1) ? carpet : cobble;
                    }
                    // Phòng Ngai Vàng Của Chúa Tể (Throne Room / Boss Arena)
                    else if (x >= 44 && x <= 48 && y >= -2 && y <= 2)
                    {
                        t = carpet;
                    }
                    else if (x >= 48 && x <= 62 && y >= -10 && y <= 10)
                    {
                        t = (x == 48 || x == 62 || y == -10 || y == 10) ? castleWall :
                            (y >= -2 && y <= 2) ? carpet : cobble;
                    }

                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(t);
                }
            }

            ctx.Tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            ctx.Tilemap.RefreshAllTiles();

            ClearProps(ctx.ContentRoot);

            // Chân Nến Gothic Ngọn Lửa Linh Hồn trong Safe Zone
            var candleSprite = LoadSprite(CastleArtDir + "/Prop_Castle_Candelabra.png");
            CreateSimpleProp(ctx.ContentRoot, "Candelabra_Left", new Vector3(-6f, 3f, 0f), candleSprite);
            CreateSimpleProp(ctx.ContentRoot, "Candelabra_Right", new Vector3(6f, 3f, 0f), candleSprite);

            // Bàn Thờ Bóng Tối (Dark Castle Altar - Bất tử 5s & 50 Adrenaline)
            var altarSprite = LoadSprite(CastleArtDir + "/Prop_Castle_Altar.png");
            CreateInteractiveProp(ctx.ContentRoot, "DarkCastleAltar", new Vector3(0f, 3.5f, 0f), altarSprite, typeof(DarkCastleAltar));

            // Tượng Gargoyle cạm bẫy ở hành lang
            var gargoyleSprite = LoadSprite(CastleArtDir + "/Prop_Castle_Gargoyle.png");
            CreateHazardProp(ctx.ContentRoot, "GargoyleTrap_1", new Vector3(30f, 8f, 0f), gargoyleSprite, HazardType.VoidAbyss);
            CreateHazardProp(ctx.ContentRoot, "GargoyleTrap_2", new Vector3(30f, -8f, 0f), gargoyleSprite, HazardType.VoidAbyss);

            // Safe Zone
            ConfigureSafeZone(ctx.Lamp, new Vector2(18f, 14f), new Vector2(0f, 0f));

            // Spawners
            ConfigureSpawners(ctx.ContentRoot, ctx.Registrar,
                new Vector3(0f, 18f, 0f),    // Cánh Bắc
                new Vector3(34f, 4f, 0f),    // Sảnh Đông
                new Vector3(34f, -6f, 0f)    // Sảnh Nam Đông
            );

            if (ctx.Boss != null) ctx.Boss.transform.position = new Vector3(56f, 0f, 0f);

            SaveAll(ctx.Tilemap);
            Debug.Log("[ThreeRealmsMapBuilder] ✓ Đã dựng xong LÂU ĐÀI BÓNG TỐI (Dark Castle)!");
        }

        /// <summary>
        /// The playable replacement for the three isolated showcase maps.  The regions are
        /// deliberately built in one coordinate space so routes, landmarks and progression
        /// read as a journey rather than a menu selection.
        /// </summary>
        [MenuItem("Game/World/Dựng Overworld Liền Mạch")]
        public static void BuildConnectedOverworld()
        {
            const string gameplayScenePath = "Assets/_Game/Scenes/PvE_Gameplay.unity";
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (activeScene.path != gameplayScenePath)
            {
                if (activeScene.isDirty)
                {
                    Debug.LogError("[ThreeRealmsMapBuilder] Scene đang mở có thay đổi chưa lưu; không tự mở PvE_Gameplay để tránh mất dữ liệu.");
                    return;
                }

                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    gameplayScenePath,
                    UnityEditor.SceneManagement.OpenSceneMode.Single);
            }

            var ctx = PrepareSceneContext();
            if (ctx == null) return;

            var forestGrass = GetOrCreateTile(ForestArtDir, "Forest_Grass", Tile.ColliderType.None);
            var forestPath = GetOrCreateTile(ForestArtDir, "Forest_Path", Tile.ColliderType.None);
            var forestWater = GetOrCreateTile(ForestArtDir, "Forest_Water", Tile.ColliderType.Grid);
            var forestWall = GetOrCreateTile(ForestArtDir, "Forest_TreeWall", Tile.ColliderType.Grid);
            var basalt = GetOrCreateTile(LavaArtDir, "Lava_Basalt", Tile.ColliderType.None);
            var ash = GetOrCreateTile(LavaArtDir, "Lava_Ash", Tile.ColliderType.None);
            var magma = GetOrCreateTile(LavaArtDir, "Lava_Magma", Tile.ColliderType.Grid);
            var castleCobble = GetOrCreateTile(CastleArtDir, "Castle_Cobble", Tile.ColliderType.None);
            var castleCarpet = GetOrCreateTile(CastleArtDir, "Castle_Carpet", Tile.ColliderType.None);
            var castleWall = GetOrCreateTile(CastleArtDir, "Castle_Wall", Tile.ColliderType.Grid);

            const int left = -38;
            const int right = 108;
            const int bottom = -31;
            const int top = 43;
            ctx.Tilemap.ClearAllTiles();
            var positions = new List<Vector3Int>();
            var tiles = new List<TileBase>();

            for (var x = left; x <= right; x++)
            {
                for (var y = bottom; y <= top; y++)
                {
                    var edge = x == left || x == right || y == bottom || y == top;
                    var routeY = RouteHeightAt(x);
                    var onRoute = Mathf.Abs(y - routeY) <= 2;
                    TileBase tile;

                    if (x < 24)
                    {
                        // Verdant starting lands: a lake forces the player to use the old bridge.
                        var river = x >= 10 && x <= 15 && Mathf.Abs(y - routeY) > 2;
                        tile = edge ? forestWall : river ? forestWater : onRoute ? forestPath : forestGrass;
                    }
                    else if (x < 68)
                    {
                        // A deep rift makes the volcanic territory a distinct crossing, not a new map.
                        var lavaRiver = x >= 43 && x <= 50 && Mathf.Abs(y - routeY) > 2;
                        tile = edge ? forestWall : lavaRiver ? magma : onRoute ? basalt : ash;
                    }
                    else
                    {
                        // The final landmark is visible across the world and reached through its causeway.
                        var throneHall = x >= 88 && y >= -12 && y <= 12;
                        tile = edge || (throneHall && (x == 107 || y == -12 || y == 12))
                            ? castleWall
                            : onRoute ? castleCarpet : castleCobble;
                    }

                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(tile);
                }
            }

            ctx.Tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            ctx.Tilemap.RefreshAllTiles();
            ClearProps(ctx.ContentRoot);
            BuildOverworldBarriers(ctx.ContentRoot, forestWall, castleWall);

            // Readable landmarks establish the player's mental map from left to right.
            CreateInteractiveProp(ctx.ContentRoot, "LifeSpring_HarborTrail", new Vector3(-21f, 4f, 0f),
                LoadSprite(ForestArtDir + "/Prop_Forest_AncientShrine.png"), typeof(AncientLifeSpring));
            CreateSimpleProp(ctx.ContentRoot, "GlowMushrooms_NorthGrove", new Vector3(-4f, 18f, 0f),
                LoadSprite(ForestArtDir + "/Prop_Forest_Mushroom.png"));
            CreateHazardProp(ctx.ContentRoot, "ThornBramble_ForestFork", new Vector3(2f, -9f, 0f),
                LoadSprite(ForestArtDir + "/Prop_Forest_ThornBush.png"), HazardType.ThornBramble);
            CreateInteractiveProp(ctx.ContentRoot, "MoltenAnvil_RiftCamp", new Vector3(33f, 6f, 0f),
                LoadSprite(LavaArtDir + "/Prop_Lava_Anvil.png"), typeof(MoltenForgeAnvil));
            CreateSimpleProp(ctx.ContentRoot, "OreCart_RiftEdge", new Vector3(38f, -13f, 0f),
                LoadSprite(LavaArtDir + "/Prop_Lava_OreCart.png"));
            CreateHazardProp(ctx.ContentRoot, "MagmaVent_DeepRift", new Vector3(58f, -7f, 0f),
                LoadSprite(LavaArtDir + "/Prop_Lava_Geyser.png"), HazardType.MagmaLava);
            CreateSimpleProp(ctx.ContentRoot, "Candelabra_CitadelGate", new Vector3(80f, 5f, 0f),
                LoadSprite(CastleArtDir + "/Prop_Castle_Candelabra.png"));
            CreateInteractiveProp(ctx.ContentRoot, "CitadelAltar", new Vector3(93f, 5f, 0f),
                LoadSprite(CastleArtDir + "/Prop_Castle_Altar.png"), typeof(DarkCastleAltar));
            CreateHazardProp(ctx.ContentRoot, "Gargoyle_CitadelWing", new Vector3(99f, -7f, 0f),
                LoadSprite(CastleArtDir + "/Prop_Castle_Gargoyle.png"), HazardType.VoidAbyss);

            ConfigureSafeZone(ctx.Lamp, new Vector2(22f, 16f), new Vector2(-21f, 0f));
            ConfigureSpawners(ctx.ContentRoot, ctx.Registrar,
                new Vector3(-2f, 16f, 0f), new Vector3(32f, 11f, 0f), new Vector3(62f, -8f, 0f));
            if (ctx.Boss != null) ctx.Boss.transform.position = new Vector3(98f, 0f, 0f);

            SaveAll(ctx.Tilemap);
            Debug.Log("[ThreeRealmsMapBuilder] ✓ Đã dựng Overworld liền mạch: Bến Khởi Hành → Rừng Rì Rào → Khe Dung Nham → Thành Trì Hoàng Hôn.");
        }

        private static int RouteHeightAt(int x)
        {
            if (x < 18) return 0;
            if (x < 43) return Mathf.RoundToInt(Mathf.Sin((x - 18) * 0.22f) * 5f);
            if (x < 68) return Mathf.RoundToInt(Mathf.Sin((x - 18) * 0.22f) * 5f) - 3;
            return Mathf.RoundToInt(Mathf.Sin((x - 68) * 0.18f) * 3f);
        }

        /// <summary>
        /// Creates visible, non-trigger walls independently from the TilemapCollider.  This
        /// keeps the boundary reliable even while a large Tilemap is rebuilt in one batch.
        /// The deliberate gaps are the route's bridge and citadel gate, not accidental holes.
        /// </summary>
        private static void BuildOverworldBarriers(GameObject parent, Tile forestWall, Tile castleWall)
        {
            const string rootName = "OverworldBarriers";
            Transform previous = parent.transform.Find(rootName);
            if (previous != null) Object.DestroyImmediate(previous.gameObject);

            var root = new GameObject(rootName).transform;
            root.SetParent(parent.transform, false);

            const int left = -38;
            const int right = 108;
            const int bottom = -31;
            const int top = 43;
            for (int x = left; x <= right; x++)
            {
                CreateBarrierTile(root, new Vector2Int(x, bottom), forestWall);
                CreateBarrierTile(root, new Vector2Int(x, top), castleWall);
            }
            for (int y = bottom + 1; y < top; y++)
            {
                CreateBarrierTile(root, new Vector2Int(left, y), forestWall);
                CreateBarrierTile(root, new Vector2Int(right, y), castleWall);
            }

            // Interior ridges give each region a silhouette and real tactical cover.
            AddBarrierLine(root, new Vector2Int(-15, 8), Vector2Int.up, 11, forestWall);
            AddBarrierLine(root, new Vector2Int(-15, 8), Vector2Int.right, 7, forestWall);
            AddBarrierLine(root, new Vector2Int(21, -16), Vector2Int.up, 10, forestWall);
            AddBarrierLine(root, new Vector2Int(27, 18), Vector2Int.right, 12, forestWall);
            AddBarrierLine(root, new Vector2Int(75, -13), Vector2Int.up, 9, castleWall);
            AddBarrierLine(root, new Vector2Int(82, 14), Vector2Int.right, 16, castleWall);
        }

        private static void AddBarrierLine(Transform parent, Vector2Int start, Vector2Int step, int length, Tile tile)
        {
            for (int i = 0; i < length; i++) CreateBarrierTile(parent, start + step * i, tile);
        }

        private static void CreateBarrierTile(Transform parent, Vector2Int cell, Tile tile)
        {
            if (tile == null || tile.sprite == null) return;
            var wall = new GameObject($"Wall_{cell.x}_{cell.y}");
            wall.transform.SetParent(parent, false);
            wall.transform.position = new Vector3(cell.x, cell.y, 0f);
            var renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = tile.sprite;
            renderer.sortingOrder = 4;
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═════════════════════════════════════════════════════════════════════════

        private class SceneContext
        {
            public Tilemap Tilemap;
            public GameObject ContentRoot;
            public GameObject Boss;
            public GameObject Lamp;
            public FloorSpawnRegistrar Registrar;
        }

        private static SceneContext PrepareSceneContext()
        {
            var tmGo = GameObject.Find("/Grid/Tilemap");
            var content = GameObject.Find("/Floor01/Content");
            var boss = GameObject.Find("/Floor01/Content/Boss_Floor01");
            var lamp = GameObject.Find("/Floor01/Content/SafeZone_Lamp");
            var registrar = Object.FindFirstObjectByType<FloorSpawnRegistrar>();

            if (tmGo == null || content == null || lamp == null || registrar == null)
            {
                Debug.LogError("[ThreeRealmsMapBuilder] Cần mở Scene PvE_Gameplay trước khi dựng!");
                return null;
            }

            return new SceneContext
            {
                Tilemap = tmGo.GetComponent<Tilemap>(),
                ContentRoot = content,
                Boss = boss,
                Lamp = lamp,
                Registrar = registrar
            };
        }

        private static void ClearProps(GameObject contentRoot)
        {
            var tr = contentRoot.transform;
            var toDestroy = new List<GameObject>();
            for (int i = 0; i < tr.childCount; i++)
            {
                var c = tr.GetChild(i);
                if (c.name.StartsWith("Prop_") || c.name.Contains("Spring") || c.name.Contains("Anvil") ||
                    c.name.Contains("Altar") || c.name.Contains("Thorn") || c.name.Contains("Vent") ||
                    c.name.Contains("Trap") || c.name.Contains("Gargoyle") || c.name.Contains("Candelabra") ||
                    c.name.Contains("OreCart") || c.name.Contains("Mushroom"))
                {
                    toDestroy.Add(c.gameObject);
                }
            }
            foreach (var g in toDestroy) Object.DestroyImmediate(g);
        }

        private static Sprite LoadSprite(string assetPath)
        {
            EnsureSpriteImportSettings(assetPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void EnsureSpriteImportSettings(string assetPath)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (imp == null) return;
            if (imp.textureType != TextureImporterType.Sprite || imp.spritePixelsPerUnit != 32 || imp.filterMode != FilterMode.Point)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 32;
                imp.filterMode = FilterMode.Point;
                imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
        }

        private static Tile GetOrCreateTile(string dir, string name, Tile.ColliderType colType)
        {
            string pngPath = $"{dir}/Tile_{name}.png";
            var sprite = LoadSprite(pngPath);

            string assetPath = $"{dir}/Tile_{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, assetPath);
            }
            tile.sprite = sprite;
            tile.colliderType = colType;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static GameObject CreateSimpleProp(GameObject parent, string name, Vector3 pos, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;
            return go;
        }

        private static GameObject CreateInteractiveProp(GameObject parent, string name, Vector3 pos, Sprite sprite, System.Type compType)
        {
            var go = CreateSimpleProp(parent, name, pos, sprite);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.5f;
            go.AddComponent(compType);
            return go;
        }

        private static GameObject CreateHazardProp(GameObject parent, string name, Vector3 pos, Sprite sprite, HazardType type)
        {
            var go = CreateSimpleProp(parent, name, pos, sprite);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.2f;
            var haz = go.AddComponent<EnvironmentalHazard>();
            var so = new SerializedObject(haz);
            so.FindProperty("hazardType").enumValueIndex = (int)type;
            so.ApplyModifiedProperties();
            return go;
        }

        private static void ConfigureSafeZone(GameObject lamp, Vector2 size, Vector2 center)
        {
            var box = lamp.GetComponent<BoxCollider2D>();
            if (box == null) return;
            var ls = lamp.transform.lossyScale;
            var lp = lamp.transform.position;
            box.size = new Vector2(size.x / ls.x, size.y / ls.y);
            box.offset = new Vector2((center.x - lp.x) / ls.x, (center.y - lp.y) / ls.y);
            EditorUtility.SetDirty(box);
        }

        private static void ConfigureSpawners(GameObject content, FloorSpawnRegistrar registrar, Vector3 sp1, Vector3 sp2, Vector3 sp3)
        {
            var parent = content.transform;
            var tplT = parent.Find("Spawner_FarmA") ?? parent.Find("EnemySpawner");
            if (tplT == null) return;

            var spA = tplT.gameObject;
            spA.transform.position = sp1;

            var spB = GetOrClone(parent, "Spawner_FarmB", spA);
            spB.transform.position = sp2;

            var spC = GetOrClone(parent, "Spawner_FarmC", spA);
            spC.transform.position = sp3;

            // The old farm zone covered only the previous small map.  Keeping it would reject
            // the new centres and silently collapse encounter density.  SafeZone still blocks
            // spawn around the harbour; with Cove's existing pair, these counts total 16 (4+4+6+2).
            ConfigureSpawnerEntries(spA.GetComponent<EnemySpawner>(), 3, 1);
            ConfigureSpawnerEntries(spB.GetComponent<EnemySpawner>(), 3, 1);
            ConfigureSpawnerEntries(spC.GetComponent<EnemySpawner>(), 2, 2, 2);

            var rso = new SerializedObject(registrar);
            var arr = rso.FindProperty("additionalSpawners");
            var extra = new[] { spB, spC };
            arr.arraySize = extra.Length;
            for (int i = 0; i < extra.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = extra[i].GetComponent<EnemySpawner>();
            rso.ApplyModifiedProperties();
        }

        private static void ConfigureSpawnerEntries(EnemySpawner spawner, params int[] counts)
        {
            if (spawner == null) return;
            var so = new SerializedObject(spawner);
            so.FindProperty("spawnZone").objectReferenceValue = null;
            var entries = so.FindProperty("entries");
            for (int i = 0; i < counts.Length && i < entries.arraySize; i++)
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue = counts[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);
        }

        private static GameObject GetOrClone(Transform parent, string name, GameObject template)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = Object.Instantiate(template, parent);
            go.name = name;
            return go;
        }

        private static void SaveAll(Tilemap tm)
        {
            EditorUtility.SetDirty(tm);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tm.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(tm.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
