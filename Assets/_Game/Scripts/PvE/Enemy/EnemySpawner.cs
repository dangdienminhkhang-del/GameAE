using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.PvE.Enemy
{
    /// <summary>
    /// Sinh quái theo cấu hình của scene, dùng pool (không Instantiate/Destroy trong combat).
    /// Một prefab dùng cho mọi loại quái — sprite lấy từ EnemyDefinition lúc spawn.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Serializable]
        public class SpawnEntry
        {
            [SerializeField] private EnemyDefinition definition;
            [SerializeField] private int count = 2;
            [SerializeField] private float radius = 4f;
            [SerializeField] private float respawnSeconds = 8f;

            public EnemyDefinition Definition => definition;
            public int Count => count;
            public float Radius => radius;
            public float RespawnSeconds => respawnSeconds;
        }

        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField] private SpawnEntry[] entries;
        [SerializeField] private Transform spawnCenter;
        [SerializeField] private int poolSizePerDefinition = 6;
        [SerializeField] private bool spawnOnStart = true;

        [Tooltip("Tầng hiện tại — quyết định hệ số sức mạnh quái (docs/07 §5). Phase 7 sẽ lấy từ FloorDefinition.")]
        [SerializeField] private int floorNumber = 1;

        private readonly Dictionary<EnemyDefinition, Queue<EnemyController>> _pools = new Dictionary<EnemyDefinition, Queue<EnemyController>>();
        private readonly List<EnemyController> _all = new List<EnemyController>();

        public IReadOnlyList<EnemyController> Spawned => _all;

        // ── Floor data integration (Wave 1B) ────────────────────────────────

        /// <summary>
        /// Gán danh sách enemy ID cho phép spawn từ FloorDefinition (docs/06 §2 Wave 1B).
        /// Nếu danh sách rỗng thì không lọc — tất cả entries đều được spawn.
        /// </summary>
        private System.Collections.Generic.HashSet<string> _allowedEnemyIds;

        public void SetAllowedEnemyIds(System.Collections.Generic.List<string> ids)
        {
            if (ids == null || ids.Count == 0)
                _allowedEnemyIds = null;
            else
                _allowedEnemyIds = new System.Collections.Generic.HashSet<string>(ids);
        }

        public void SetFloorNumber(int floor)
        {
            floorNumber = Mathf.Max(1, floor);
        }

        // ────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (spawnOnStart) SpawnAll();
        }

        public void SpawnAll()
        {
            if (enemyPrefab == null || entries == null) return;
            if (_all.Count > 0 || _pools.Count > 0) return; // tránh tạo pool 2 lần
            foreach (SpawnEntry entry in entries)
            {
                if (entry == null || entry.Definition == null) continue;
                // Lọc theo FloorDefinition.spawnEnemyIds nếu có
                if (_allowedEnemyIds != null && !_allowedEnemyIds.Contains(entry.Definition.Id))
                    continue;
                Prewarm(entry);
                for (int i = 0; i < entry.Count; i++) SpawnOne(entry);
            }
        }

        private void Prewarm(SpawnEntry entry)
        {
            if (_pools.ContainsKey(entry.Definition)) return;

            var pool = new Queue<EnemyController>();
            for (int i = 0; i < poolSizePerDefinition; i++)
            {
                EnemyController enemy = Instantiate(enemyPrefab, transform);
                enemy.gameObject.SetActive(false);
                AssignDefinition(enemy, entry.Definition);
                enemy.Died += HandleEnemyDied;
                pool.Enqueue(enemy);
                _all.Add(enemy);
            }
            _pools[entry.Definition] = pool;
        }

        private EnemyController SpawnOne(SpawnEntry entry)
        {
            Queue<EnemyController> pool = _pools[entry.Definition];
            EnemyController enemy = null;

            while (pool.Count > 0 && enemy == null)
            {
                EnemyController candidate = pool.Dequeue();
                if (candidate != null) enemy = candidate;
            }
            if (enemy == null)
            {
                // Tạo mới trong nhánh dự phòng: PHẢI tắt trước khi gán definition,
                // nếu không OnEnable chạy với definition = null → quái chỉ có 1 HP.
                enemy = Instantiate(enemyPrefab, transform);
                enemy.gameObject.SetActive(false);
                AssignDefinition(enemy, entry.Definition);
                enemy.Died += HandleEnemyDied;
                _all.Add(enemy);
            }

            enemy.transform.position = RandomPoint(entry.Radius);
            enemy.gameObject.SetActive(true);
            return enemy;
        }

        /// <summary>Gán loại quái + hệ số tầng lúc runtime (một prefab dùng cho mọi loại).</summary>
        private void AssignDefinition(EnemyController enemy, EnemyDefinition definition)
        {
            enemy.SetDefinition(definition);
            enemy.SetFloor(floorNumber);
        }

        private Vector2 RandomPoint(float radius)
        {
            Vector2 center = spawnCenter != null ? spawnCenter.position : transform.position;
            return center + UnityEngine.Random.insideUnitCircle * Mathf.Max(0.1f, radius);
        }

        private void HandleEnemyDied(EnemyController enemy)
        {
            StartCoroutine(RespawnAfterDelay(enemy));
        }

        private IEnumerator RespawnAfterDelay(EnemyController enemy)
        {
            float delay = 8f;
            foreach (SpawnEntry entry in entries)
            {
                if (entry != null && entry.Definition != null && entry.Definition == GetDefinition(enemy))
                {
                    delay = entry.RespawnSeconds;
                    break;
                }
            }
            yield return new WaitForSeconds(delay);
            if (enemy != null && !enemy.gameObject.activeSelf)
            {
                enemy.transform.position = RandomPoint(GetRadius(enemy));
                enemy.gameObject.SetActive(true);
            }
        }

        private static EnemyDefinition GetDefinition(EnemyController enemy)
        {
            return enemy != null ? enemy.Definition : null;
        }

        private float GetRadius(EnemyController enemy)
        {
            EnemyDefinition definition = GetDefinition(enemy);
            if (entries != null)
            {
                foreach (SpawnEntry entry in entries)
                {
                    if (entry != null && entry.Definition == definition) return entry.Radius;
                }
            }
            return 3f;
        }
    }
}
