using Game.Core.Floor;
using UnityEngine;

namespace Game.PvE.Floor
{
    /// <summary>
    /// Đọc FloorDefinition và kích hoạt EnemySpawner theo data — KHÔNG có if floorId cụ thể.
    ///
    /// Đặt MonoBehaviour này trong nội dung của mỗi tầng. FloorSceneBinder gọi Register(def)
    /// mỗi khi tầng được bật (qua FloorActivation, do FloorLoader gọi sau khi vào tầng thành công).
    /// Gọi lại Register nhiều lần là an toàn: EnemySpawner chống spawn hai lần, và hồi sinh quái
    /// được xử lý riêng qua IFloorActivationListener.
    ///
    /// Một tầng có thể có nhiều khu (Phase 9.7a), mỗi khu một EnemySpawner riêng:
    /// <see cref="enemySpawner"/> giữ nguyên cho scene cũ; các khu còn lại gán vào additionalSpawners.
    /// </summary>
    public class FloorSpawnRegistrar : MonoBehaviour
    {
        [Header("Scene refs — gán trong Inspector của scene floor này")]
        [SerializeField] private Enemy.EnemySpawner enemySpawner;

        [Tooltip("Spawner của các khu khác trong cùng tầng (mỗi khu một spawner). Có thể để trống.")]
        [SerializeField] private Enemy.EnemySpawner[] additionalSpawners;

        private FloorDefinition _def;

        /// <summary>ID của tầng đã đăng ký gần nhất; null nếu chưa đăng ký.</summary>
        public string RegisteredFloorId => _def != null ? _def.id : null;

        /// <summary>
        /// Gọi khi tầng được bật, truyền FloorDefinition tương ứng.
        /// </summary>
        public void Register(FloorDefinition def)
        {
            if (def == null)
            {
                Debug.LogError("[FloorSpawnRegistrar] FloorDefinition null.", this);
                return;
            }

            _def = def;
            RegisterSpawners();
            EnsureShrinesInScene();

            Debug.Log($"[FloorSpawnRegistrar] Đã đăng ký tầng '{def.id}' " +
                      $"({def.spawnEnemyIds.Count} enemy ID, {def.safeZoneConfigIds.Count} safe zone config).");
        }

        // ── Spawn ─────────────────────────────────────────────────────────────────────

        private void RegisterSpawners()
        {
            bool registeredAny = RegisterSpawner(enemySpawner);

            if (additionalSpawners != null)
            {
                for (int i = 0; i < additionalSpawners.Length; i++)
                {
                    if (RegisterSpawner(additionalSpawners[i])) registeredAny = true;
                }
            }

            if (!registeredAny)
            {
                Debug.LogWarning($"[FloorSpawnRegistrar] Không có EnemySpawner được gán " +
                                 $"trong scene '{_def.id}'.", this);
            }
        }

        private bool RegisterSpawner(Enemy.EnemySpawner spawner)
        {
            if (spawner == null) return false;

            // Gán danh sách enemy IDs và floor number từ definition
            spawner.SetAllowedEnemyIds(_def.spawnEnemyIds);
            spawner.SetFloorNumber(_def.floorNumber);
            spawner.SpawnAll();
            return true;
        }

        private void EnsureShrinesInScene()
        {
            if (FindFirstObjectByType<Environment.ShrineOfBlessing>() != null) return;

            // Dựng tế đàn phước lành ở lối vào Vùng Farm
            GameObject shrine = new GameObject("Shrine_Swiftness", typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(Environment.ShrineOfBlessing));
            shrine.transform.position = new Vector3(18f, -2f, 0f);
            CircleCollider2D col = shrine.GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.3f;

            // Tế đàn hút máu ở sâu trong Farm
            GameObject shrine2 = new GameObject("Shrine_Vampirism", typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(Environment.ShrineOfBlessing));
            shrine2.transform.position = new Vector3(34f, -9f, 0f);
            CircleCollider2D col2 = shrine2.GetComponent<CircleCollider2D>();
            col2.isTrigger = true;
            col2.radius = 1.3f;
        }
    }
}
