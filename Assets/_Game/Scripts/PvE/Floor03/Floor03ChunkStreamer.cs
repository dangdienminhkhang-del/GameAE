using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Game.Core.Floor;
using Game.PvE.Enemy;

namespace Game.PvE.Floor03
{
    /// <summary>
    /// Sinh và huỷ địa hình + spawner theo chunk quanh camera chính → thế giới tầng 3 vô hạn.
    /// Địa hình sinh từ Floor03World (xác định theo seed). Tile/Spawner prefab do Floor03Builder gán sẵn.
    /// Nằm trong nội dung tầng nên được bật/tắt cùng tầng (IFloorActivationListener).
    /// </summary>
    public class Floor03ChunkStreamer : MonoBehaviour, IFloorActivationListener
    {
        [Header("Tilemap")]
        [SerializeField] private Tilemap groundMap;
        [SerializeField] private Tilemap propsMap;
        [SerializeField] private Tilemap wallsMap;
        [Tooltip("Dịch toạ độ cục bộ (Zumfut = 0,0) sang toạ độ ô trong scene.")]
        [SerializeField] private int originX = 20000;
        [SerializeField] private Transform entryPoint;

        [Header("Tile (Floor03Builder gán)")]
        [Tooltip("Chỉ số = (int)Biome * 4 + biến thể 0..3")]
        [SerializeField] private TileBase[] groundTiles;
        [Tooltip("Chỉ số = (int)Prop")]
        [SerializeField] private TileBase[] propTiles;
        [SerializeField] private TileBase blockTile;

        [Header("Spawner prefab (Floor03Builder gán)")]
        [Tooltip("Chỉ số = (int)Biome")]
        [SerializeField] private EnemySpawner[] biomeSpawners;
        [SerializeField] private string[] bossKeys;
        [SerializeField] private EnemySpawner[] bossSpawners;

        [Header("Phạm vi (đơn vị chunk)")]
        [SerializeField] private int loadRadius = 2;
        [SerializeField] private int spawnRadius = 1;
        [SerializeField] private int chunksPerFrame = 2;

        private readonly HashSet<Vector2Int> _loaded = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _spawnChecked = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, List<GameObject>> _spawners = new Dictionary<Vector2Int, List<GameObject>>();
        private readonly List<Vector2Int> _tmp = new List<Vector2Int>();
        private TileBase[] _empty;
        private FloorDefinition _def;
        private int _baseFloor = 3;
        private Transform _spawnRoot;
        private float _nextSpawnCheck;

        private const int N = Floor03World.Chunk;

        // ── IFloorActivationListener ─────────────────────────────────────

        public void OnFloorActivated(FloorDefinition def)
        {
            _def = def;
            _baseFloor = def != null ? def.floorNumber : 3;
            Prime();
        }

        public void OnFloorDeactivated()
        {
            ClearAll();
        }

        // ── Vòng đời ─────────────────────────────────────────────────────

        private void Update()
        {
            if (_def == null || groundMap == null) return;

            Vector2Int cc = ChunkOf(FocusLocal());
            LoadAround(cc, loadRadius, chunksPerFrame);

            if (Time.unscaledTime >= _nextSpawnCheck)
            {
                _nextSpawnCheck = Time.unscaledTime + 0.5f;
                ManageSpawners(cc);
                UnloadFar(cc);
            }
        }

        /// <summary>Sinh sẵn các chunk quanh điểm vào ngay khi tầng được bật.</summary>
        public void Prime()
        {
            if (groundMap == null) return;
            EnsureRoot();
            Vector2Int cc = ChunkOf(EntryLocal());
            LoadAround(cc, loadRadius, int.MaxValue);
            ManageSpawners(cc);
        }

        /// <summary>Dùng trong Editor để kiểm tra: sinh chunk quanh một điểm cục bộ mà không cần chạy game.</summary>
        public void PrimeAt(FloorDefinition def, Vector2 localFocus, int radius, bool withSpawners)
        {
            _def = def;
            _baseFloor = def != null ? def.floorNumber : 3;
            EnsureRoot();
            Vector2Int cc = ChunkOf(localFocus);
            LoadAround(cc, radius, int.MaxValue);
            if (withSpawners && def != null) ManageSpawners(cc);
        }

        public int LoadedChunkCount { get { return _loaded.Count; } }
        public int SpawnerCount { get { int n = 0; foreach (var kv in _spawners) n += kv.Value.Count; return n; } }

        // ── Địa hình ─────────────────────────────────────────────────────

        private static Vector2Int ChunkOf(Vector2 local)
        {
            return Floor03World.ChunkOf(local.x, local.y);
        }

        private Vector2 EntryLocal()
        {
            return entryPoint != null ? new Vector2(entryPoint.position.x - originX, entryPoint.position.y) : Vector2.zero;
        }

        private Vector2 FocusLocal()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 p = cam.transform.position;
                float lx = p.x - originX, ly = p.y;
                // Camera còn ở tầng khác (rất xa) thì bám theo điểm vào, tránh sinh địa hình vô ích.
                if (Mathf.Abs(lx) < 12000f && Mathf.Abs(ly) < 12000f) return new Vector2(lx, ly);
            }
            return EntryLocal();
        }

        private void LoadAround(Vector2Int cc, int radius, int budget)
        {
            for (int r = 0; r <= radius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var c = new Vector2Int(cc.x + dx, cc.y + dy);
                        if (_loaded.Contains(c)) continue;
                        LoadChunk(c);
                        if (--budget <= 0) return;
                    }
                }
            }
        }

        private BoundsInt BoundsOf(Vector2Int c)
        {
            return new BoundsInt(originX + c.x * N, c.y * N, 0, N, N, 1);
        }

        private void LoadChunk(Vector2Int c)
        {
            var ground = new TileBase[N * N];
            var props = new TileBase[N * N];
            var walls = new TileBase[N * N];
            int x0 = c.x * N, y0 = c.y * N;

            for (int j = 0; j < N; j++)
            {
                for (int i = 0; i < N; i++)
                {
                    TileInfo t = Floor03World.Sample(x0 + i, y0 + j);
                    int idx = j * N + i;

                    int gi = (int)t.Biome * 4 + t.Variant;
                    if (groundTiles != null && gi < groundTiles.Length) ground[idx] = groundTiles[gi];

                    int pi = (int)t.Prop;
                    if (pi != 0 && propTiles != null && pi < propTiles.Length) props[idx] = propTiles[pi];

                    if (t.Blocked) walls[idx] = blockTile;
                }
            }

            BoundsInt b = BoundsOf(c);
            groundMap.SetTilesBlock(b, ground);
            if (propsMap != null) propsMap.SetTilesBlock(b, props);
            if (wallsMap != null) wallsMap.SetTilesBlock(b, walls);
            _loaded.Add(c);
        }

        private void UnloadChunk(Vector2Int c)
        {
            if (_empty == null) _empty = new TileBase[N * N];
            BoundsInt b = BoundsOf(c);
            groundMap.SetTilesBlock(b, _empty);
            if (propsMap != null) propsMap.SetTilesBlock(b, _empty);
            if (wallsMap != null) wallsMap.SetTilesBlock(b, _empty);
            _loaded.Remove(c);
        }

        private void UnloadFar(Vector2Int cc)
        {
            _tmp.Clear();
            foreach (var c in _loaded)
            {
                if (Mathf.Max(Mathf.Abs(c.x - cc.x), Mathf.Abs(c.y - cc.y)) > loadRadius + 1) _tmp.Add(c);
            }
            for (int i = 0; i < _tmp.Count; i++) UnloadChunk(_tmp[i]);
        }

        // ── Spawner ──────────────────────────────────────────────────────

        private void EnsureRoot()
        {
            if (_spawnRoot != null) return;
            var go = new GameObject("ChunkSpawners");
            go.transform.SetParent(transform, false);
            _spawnRoot = go.transform;
        }

        private void ManageSpawners(Vector2Int cc)
        {
            if (_def == null) return;
            EnsureRoot();

            for (int dy = -spawnRadius; dy <= spawnRadius; dy++)
            {
                for (int dx = -spawnRadius; dx <= spawnRadius; dx++)
                {
                    var c = new Vector2Int(cc.x + dx, cc.y + dy);
                    if (_loaded.Contains(c)) EnsureSpawners(c);
                }
            }

            // Chunk đã đi xa: huỷ spawner (và quái con của nó); quay lại sẽ có bầy mới.
            _tmp.Clear();
            foreach (var c in _spawnChecked)
            {
                if (Mathf.Max(Mathf.Abs(c.x - cc.x), Mathf.Abs(c.y - cc.y)) > spawnRadius + 1) _tmp.Add(c);
            }
            for (int i = 0; i < _tmp.Count; i++)
            {
                DestroySpawners(_tmp[i]);
                _spawnChecked.Remove(_tmp[i]);
            }
        }

        private void EnsureSpawners(Vector2Int c)
        {
            if (!_spawnChecked.Add(c)) return;

            var list = new List<GameObject>();
            Vector2Int tile;
            Biome biome;
            string key;

            if (Floor03World.TrySpawnerSite(c.x, c.y, out tile, out biome))
            {
                int bi = (int)biome;
                if (biomeSpawners != null && bi < biomeSpawners.Length && biomeSpawners[bi] != null)
                    list.Add(Make(biomeSpawners[bi], tile));
            }

            var pois = Floor03World.Pois;
            for (int i = 0; i < pois.Length; i++)
            {
                if (string.IsNullOrEmpty(pois[i].Boss)) continue;
                if (Floor03World.ChunkOf(pois[i].Pos.x, pois[i].Pos.y) != c) continue;
                EnemySpawner bp = FindBoss(pois[i].Boss);
                if (bp != null)
                    list.Add(Make(bp, new Vector2Int(Mathf.FloorToInt(pois[i].Pos.x), Mathf.FloorToInt(pois[i].Pos.y))));
            }

            if (Floor03World.TryOuterBoss(c.x, c.y, out tile, out key))
            {
                EnemySpawner bp = FindBoss(key);
                if (bp != null) list.Add(Make(bp, tile));
            }

            if (list.Count > 0) _spawners[c] = list;
        }

        private EnemySpawner FindBoss(string key)
        {
            if (bossKeys == null || bossSpawners == null) return null;
            for (int i = 0; i < bossKeys.Length && i < bossSpawners.Length; i++)
                if (bossKeys[i] == key) return bossSpawners[i];
            return null;
        }

        private GameObject Make(EnemySpawner prefab, Vector2Int tile)
        {
            EnemySpawner s = Instantiate(prefab, new Vector3(originX + tile.x + 0.5f, tile.y + 0.5f, 0f),
                Quaternion.identity, _spawnRoot);
            // Phải gọi trước Start() của spawner (nó sẽ SpawnAll ở khung hình kế tiếp).
            s.SetAllowedEnemyIds(_def.spawnEnemyIds);
            s.SetFloorNumber(Floor03World.FloorNumberAt(tile.x, tile.y, _baseFloor));
            return s.gameObject;
        }

        private void DestroySpawners(Vector2Int c)
        {
            List<GameObject> list;
            if (!_spawners.TryGetValue(c, out list)) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                if (Application.isPlaying) Destroy(list[i]); else DestroyImmediate(list[i]);
            }
            _spawners.Remove(c);
        }

        public void ClearAll()
        {
            if (groundMap != null) groundMap.ClearAllTiles();
            if (propsMap != null) propsMap.ClearAllTiles();
            if (wallsMap != null) wallsMap.ClearAllTiles();
            _loaded.Clear();

            _tmp.Clear();
            foreach (var kv in _spawners) _tmp.Add(kv.Key);
            for (int i = 0; i < _tmp.Count; i++) DestroySpawners(_tmp[i]);
            _spawners.Clear();
            _spawnChecked.Clear();
        }
    }
}
