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
    /// </summary>
    public class FloorSpawnRegistrar : MonoBehaviour
    {
        [Header("Scene refs — gán trong Inspector của scene floor này")]
        [SerializeField] private Enemy.EnemySpawner enemySpawner;

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
            RegisterSpawner();

            Debug.Log($"[FloorSpawnRegistrar] Đã đăng ký tầng '{def.id}' " +
                      $"({def.spawnEnemyIds.Count} enemy ID, {def.safeZoneConfigIds.Count} safe zone config).");
        }

        // ── Spawn ────────────────────────────────────────────────────────────

        private void RegisterSpawner()
        {
            if (enemySpawner == null)
            {
                Debug.LogWarning($"[FloorSpawnRegistrar] Không có EnemySpawner được gán " +
                                 $"trong scene '{_def.id}'.", this);
                return;
            }

            // Gán danh sách enemy IDs và floor number từ definition
            enemySpawner.SetAllowedEnemyIds(_def.spawnEnemyIds);
            enemySpawner.SetFloorNumber(_def.floorNumber);
            enemySpawner.SpawnAll();
        }
    }
}
