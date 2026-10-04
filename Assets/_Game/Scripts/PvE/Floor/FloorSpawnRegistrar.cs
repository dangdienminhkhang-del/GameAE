using Game.Core.Floor;
using UnityEngine;

namespace Game.PvE.Floor
{
    /// <summary>
    /// Sau khi scene floor được load additive, FloorSpawnRegistrar đọc FloorDefinition
    /// và kích hoạt EnemySpawner theo data — KHÔNG có if floorId cụ thể.
    ///
    /// Đặt MonoBehaviour này lên một root GameObject trong scene của mỗi floor.
    /// FloorSceneController (cũng trong scene đó) gọi Register(def) khi nhận được
    /// FloorDefinition từ FloorLoader.
    /// </summary>
    public class FloorSpawnRegistrar : MonoBehaviour
    {
        [Header("Scene refs — gán trong Inspector của scene floor này")]
        [SerializeField] private Enemy.EnemySpawner enemySpawner;

        private FloorDefinition _def;

        /// <summary>
        /// Gọi sau khi scene floor được load, truyền FloorDefinition tương ứng.
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
