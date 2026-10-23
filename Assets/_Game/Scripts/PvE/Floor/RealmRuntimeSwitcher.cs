using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Events;
using Game.PvE.Camera;
using Game.PvE.Enemy;
using Game.PvE.Environment;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Game.PvE.Floor
{
    public enum RealmTheme
    {
        AncientForest = 1,
        LavaMines = 2,
        DarkCastle = 3
    }

    /// <summary>
    /// Legacy preview utility retained for editor experiments.  It is no longer created at
    /// runtime: the live game uses the one connected overworld authored in the scene.
    /// </summary>
    public class RealmRuntimeSwitcher : MonoBehaviour
    {
        public static RealmRuntimeSwitcher Instance { get; private set; }

        [Header("State")]
        [SerializeField] private RealmTheme currentRealm = RealmTheme.AncientForest;

        private Tilemap _tilemap;
        private GameObject _contentRoot;
        private GameObject _boss;
        private GameObject _lamp;
        private FloorSpawnRegistrar _registrar;

        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Tile> _tileCache = new Dictionary<string, Tile>();

        public RealmTheme CurrentRealm => currentRealm;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            _tilemap = null;
            _contentRoot = null;
            _boss = null;
            _lamp = null;
            _registrar = null;
            FindSceneReferences();
        }

        private void Start()
        {
            FindSceneReferences();
        }

        private void FindSceneReferences()
        {
            if (_tilemap == null)
            {
                var tmGo = GameObject.Find("/Grid/Tilemap");
                if (tmGo != null) _tilemap = tmGo.GetComponent<Tilemap>();
            }
            if (_contentRoot == null)
            {
                _contentRoot = GameObject.Find("/Floor01/Content");
            }
            if (_boss == null && _contentRoot != null)
            {
                var b = _contentRoot.transform.Find("Boss_Floor01");
                if (b != null) _boss = b.gameObject;
            }
            if (_lamp == null && _contentRoot != null)
            {
                var l = _contentRoot.transform.Find("SafeZone_Lamp");
                if (l != null) _lamp = l.gameObject;
            }
            if (_registrar == null)
            {
                _registrar = Object.FindFirstObjectByType<FloorSpawnRegistrar>();
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Game/Realms/PlayMode: Chuyển Sang Rừng Rậm Cổ Đại")]
        public static void MenuSwitchToForest()
        {
            if (Instance != null) Instance.SwitchRealm(RealmTheme.AncientForest);
        }

        [UnityEditor.MenuItem("Game/Realms/PlayMode: Chuyển Sang Hầm Mỏ Dung Nham")]
        public static void MenuSwitchToLava()
        {
            if (Instance != null) Instance.SwitchRealm(RealmTheme.LavaMines);
        }

        [UnityEditor.MenuItem("Game/Realms/PlayMode: Chuyển Sang Lâu Đài Bóng Tối")]
        public static void MenuSwitchToCastle()
        {
            if (Instance != null) Instance.SwitchRealm(RealmTheme.DarkCastle);
        }
#endif

        public void SwitchRealm(RealmTheme theme)
        {
            FindSceneReferences();
            if (_tilemap == null || _contentRoot == null)
            {
                Debug.LogWarning("[RealmRuntimeSwitcher] Không tìm thấy Grid/Tilemap hoặc Floor01/Content!");
                return;
            }

            currentRealm = theme;
            CameraFollow.RequestShake(0.25f, 0.3f);

            switch (theme)
            {
                case RealmTheme.AncientForest:
                    ApplyAncientForest();
                    GameEvents.RaiseCombatNotice("ĐÃ VÀO: RỪNG RẬM CỔ ĐẠI", new Color(0.3f, 1f, 0.4f));
                    break;
                case RealmTheme.LavaMines:
                    ApplyLavaMines();
                    GameEvents.RaiseCombatNotice("ĐÃ VÀO: HẦM MỎ DUNG NHAM", new Color(1f, 0.45f, 0.1f));
                    break;
                case RealmTheme.DarkCastle:
                    ApplyDarkCastle();
                    GameEvents.RaiseCombatNotice("ĐÃ VÀO: LÂU ĐÀI BÓNG TỐI", new Color(0.75f, 0.35f, 1f));
                    break;
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // REALM 1: RỪNG RẬM CỔ ĐẠI
        // ═════════════════════════════════════════════════════════════════════════

        private void ApplyAncientForest()
        {
            var grass = GetTile("AncientForest/Tile_Forest_Grass.png", Tile.ColliderType.None);
            var path = GetTile("AncientForest/Tile_Forest_Path.png", Tile.ColliderType.None);
            var water = GetTile("AncientForest/Tile_Forest_Water.png", Tile.ColliderType.Grid);
            var wall = GetTile("AncientForest/Tile_Forest_TreeWall.png", Tile.ColliderType.Grid);

            _tilemap.ClearAllTiles();
            var positions = new List<Vector3Int>();
            var tiles = new List<TileBase>();

            for (int x = -16; x <= 64; x++)
            {
                for (int y = -16; y <= 24; y++)
                {
                    TileBase t = wall;
                    if (x >= -8 && x <= 8 && y >= -6 && y <= 6)
                        t = (Mathf.Abs(x) <= 2 || Mathf.Abs(y) <= 2) ? path : grass;
                    else if (x >= -4 && x <= 4 && y > 6 && y <= 18)
                        t = path;
                    else if (x >= -12 && x <= 12 && y >= 12 && y <= 20)
                        t = grass;
                    else if (x >= 14 && x <= 18 && y >= -14 && y <= 22)
                        t = (y >= -1 && y <= 3) ? path : water;
                    else if (x >= 20 && x <= 40 && y >= -12 && y <= 16)
                        t = (x >= 28 && x <= 32) ? path : grass;
                    else if (x >= 42 && x <= 46 && y >= -2 && y <= 2)
                        t = path;
                    else if (x >= 46 && x <= 60 && y >= -10 && y <= 10)
                        t = (x == 46 || x == 60 || y == -10 || y == 10) ? wall : path;

                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(t);
                }
            }

            _tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            _tilemap.RefreshAllTiles();

            ClearRealmProps();

            // Props Rừng Rậm
            var springSpr = GetSprite("AncientForest/Prop_Forest_AncientShrine.png");
            CreateInteractiveProp("AncientLifeSpring", new Vector3(0f, 3.5f, 0f), springSpr, typeof(AncientLifeSpring));

            var thornSpr = GetSprite("AncientForest/Prop_Forest_ThornBush.png");
            CreateHazardProp("ThornBramble_1", new Vector3(25f, 5f, 0f), thornSpr, HazardType.ThornBramble);
            CreateHazardProp("ThornBramble_2", new Vector3(35f, -4f, 0f), thornSpr, HazardType.ThornBramble);

            var shroomSpr = GetSprite("AncientForest/Prop_Forest_Mushroom.png");
            CreateSimpleProp("GlowMushroom_1", new Vector3(-8f, 16f, 0f), shroomSpr);
            CreateSimpleProp("GlowMushroom_2", new Vector3(6f, 17f, 0f), shroomSpr);

            if (_boss != null) _boss.transform.position = new Vector3(53f, 0f, 0f);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // REALM 2: HẦM MỎ DUNG NHAM
        // ═════════════════════════════════════════════════════════════════════════

        private void ApplyLavaMines()
        {
            var basalt = GetTile("LavaMines/Tile_Lava_Basalt.png", Tile.ColliderType.None);
            var ash = GetTile("LavaMines/Tile_Lava_Ash.png", Tile.ColliderType.None);
            var magma = GetTile("LavaMines/Tile_Lava_Magma.png", Tile.ColliderType.Grid);
            var wall = GetTile("LavaMines/Tile_Lava_RockWall.png", Tile.ColliderType.Grid);

            _tilemap.ClearAllTiles();
            var positions = new List<Vector3Int>();
            var tiles = new List<TileBase>();

            for (int x = -16; x <= 64; x++)
            {
                for (int y = -16; y <= 24; y++)
                {
                    TileBase t = wall;
                    if (x >= -8 && x <= 8 && y >= -6 && y <= 6)
                        t = basalt;
                    else if (x >= -4 && x <= 4 && y > 6 && y <= 20)
                        t = ash;
                    else if (x >= -14 && x <= 14 && y >= 14 && y <= 22)
                        t = basalt;
                    else if (x >= 12 && x <= 22 && y >= -14 && y <= 22)
                        t = (y >= -1 && y <= 3) ? basalt : magma;
                    else if (x >= 24 && x <= 42 && y >= -12 && y <= 16)
                        t = (x % 6 == 0 || y % 6 == 0) ? ash : basalt;
                    else if (x >= 44 && x <= 48 && y >= -2 && y <= 2)
                        t = basalt;
                    else if (x >= 48 && x <= 62 && y >= -10 && y <= 10)
                        t = (x == 48 || x == 62 || y == -10 || y == 10) ? wall : basalt;

                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(t);
                }
            }

            _tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            _tilemap.RefreshAllTiles();

            ClearRealmProps();

            var anvilSpr = GetSprite("LavaMines/Prop_Lava_Anvil.png");
            CreateInteractiveProp("MoltenForgeAnvil", new Vector3(0f, 3.5f, 0f), anvilSpr, typeof(MoltenForgeAnvil));

            var geyserSpr = GetSprite("LavaMines/Prop_Lava_Geyser.png");
            CreateHazardProp("MagmaVent_1", new Vector3(28f, -4f, 0f), geyserSpr, HazardType.MagmaLava);
            CreateHazardProp("MagmaVent_2", new Vector3(36f, 6f, 0f), geyserSpr, HazardType.MagmaLava);

            var cartSpr = GetSprite("LavaMines/Prop_Lava_OreCart.png");
            CreateSimpleProp("OreCart_1", new Vector3(-8f, 18f, 0f), cartSpr);
            CreateSimpleProp("OreCart_2", new Vector3(30f, 10f, 0f), cartSpr);

            if (_boss != null) _boss.transform.position = new Vector3(55f, 0f, 0f);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // REALM 3: LÂU ĐÀI BÓNG TỐI
        // ═════════════════════════════════════════════════════════════════════════

        private void ApplyDarkCastle()
        {
            var cobble = GetTile("DarkCastle/Tile_Castle_Cobble.png", Tile.ColliderType.None);
            var carpet = GetTile("DarkCastle/Tile_Castle_Carpet.png", Tile.ColliderType.None);
            var wall = GetTile("DarkCastle/Tile_Castle_Wall.png", Tile.ColliderType.Grid);
            var voidChasm = GetTile("DarkCastle/Tile_Castle_Void.png", Tile.ColliderType.Grid);

            _tilemap.ClearAllTiles();
            var positions = new List<Vector3Int>();
            var tiles = new List<TileBase>();

            for (int x = -16; x <= 64; x++)
            {
                for (int y = -16; y <= 24; y++)
                {
                    TileBase t = wall;
                    if (x >= -8 && x <= 8 && y >= -6 && y <= 6)
                        t = (y >= -1 && y <= 1) ? carpet : cobble;
                    else if (x >= -4 && x <= 4 && y > 6 && y <= 20)
                        t = (x >= -1 && x <= 1) ? carpet : cobble;
                    else if (x >= -14 && x <= 14 && y >= 14 && y <= 22)
                        t = cobble;
                    else if (x >= 12 && x <= 20 && y >= -14 && y <= 22)
                        t = (y >= -1 && y <= 3) ? carpet : voidChasm;
                    else if (x >= 22 && x <= 42 && y >= -12 && y <= 16)
                        t = (y >= -1 && y <= 1) ? carpet : cobble;
                    else if (x >= 44 && x <= 48 && y >= -2 && y <= 2)
                        t = carpet;
                    else if (x >= 48 && x <= 62 && y >= -10 && y <= 10)
                        t = (x == 48 || x == 62 || y == -10 || y == 10) ? wall :
                            (y >= -2 && y <= 2) ? carpet : cobble;

                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(t);
                }
            }

            _tilemap.SetTiles(positions.ToArray(), tiles.ToArray());
            _tilemap.RefreshAllTiles();

            ClearRealmProps();

            var candleSpr = GetSprite("DarkCastle/Prop_Castle_Candelabra.png");
            CreateSimpleProp("Candelabra_Left", new Vector3(-6f, 3f, 0f), candleSpr);
            CreateSimpleProp("Candelabra_Right", new Vector3(6f, 3f, 0f), candleSpr);

            var altarSpr = GetSprite("DarkCastle/Prop_Castle_Altar.png");
            CreateInteractiveProp("DarkCastleAltar", new Vector3(0f, 3.5f, 0f), altarSpr, typeof(DarkCastleAltar));

            var gargoyleSpr = GetSprite("DarkCastle/Prop_Castle_Gargoyle.png");
            CreateHazardProp("GargoyleTrap_1", new Vector3(30f, 8f, 0f), gargoyleSpr, HazardType.VoidAbyss);
            CreateHazardProp("GargoyleTrap_2", new Vector3(30f, -8f, 0f), gargoyleSpr, HazardType.VoidAbyss);

            if (_boss != null) _boss.transform.position = new Vector3(56f, 0f, 0f);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════════

        private void ClearRealmProps()
        {
            if (_contentRoot == null) return;
            var tr = _contentRoot.transform;
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
            foreach (var g in toDestroy) Destroy(g);
        }

        private Sprite GetSprite(string relativePath)
        {
            if (_spriteCache.TryGetValue(relativePath, out var cached) && cached != null) return cached;

#if UNITY_EDITOR
            string assetPath = "Assets/_Game/Art/Environment/" + relativePath;
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (asset != null)
            {
                _spriteCache[relativePath] = asset;
                return asset;
            }
#endif

            string fullPath = Path.Combine(Application.dataPath, "_Game/Art/Environment", relativePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[RealmRuntimeSwitcher] Không thấy file ảnh: {fullPath}");
                return null;
            }

            byte[] data = File.ReadAllBytes(fullPath);
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.LoadImage(data);

            var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
            spr.name = Path.GetFileNameWithoutExtension(relativePath);
            _spriteCache[relativePath] = spr;
            return spr;
        }

        private Tile GetTile(string relativePath, Tile.ColliderType colType)
        {
            if (_tileCache.TryGetValue(relativePath, out var cached)) return cached;

            var spr = GetSprite(relativePath);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = spr;
            tile.colliderType = colType;
            _tileCache[relativePath] = tile;
            return tile;
        }

        private GameObject CreateSimpleProp(string name, Vector3 pos, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_contentRoot.transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;
            return go;
        }

        private GameObject CreateInteractiveProp(string name, Vector3 pos, Sprite sprite, Type compType)
        {
            var go = CreateSimpleProp(name, pos, sprite);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.5f;
            go.AddComponent(compType);
            return go;
        }

        private GameObject CreateHazardProp(string name, Vector3 pos, Sprite sprite, HazardType type)
        {
            var go = CreateSimpleProp(name, pos, sprite);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.2f;
            var haz = go.AddComponent<EnvironmentalHazard>();
            return go;
        }

    }
}
