using Game.PvE.Enemy;
using UnityEngine;

namespace Game.PvE.Items
{
    /// <summary>Gắn lên prefab quái: quái chết → roll bảng rơi đồ trong EnemyDefinition → tạo ItemPickup.</summary>
    [RequireComponent(typeof(EnemyController))]
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private EnemyController enemy;
        [SerializeField] private ItemPickup pickupPrefab;

        private void Awake()
        {
            if (enemy == null) TryGetComponent(out enemy);
        }

        private void OnEnable()
        {
            if (enemy != null) enemy.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (enemy != null) enemy.Died -= HandleDied;
        }

        private void HandleDied(EnemyController dead)
        {
            if (dead == null || dead.Definition == null || pickupPrefab == null) return;

            var drops = dead.Definition.Drops;
            for (int i = 0; i < drops.Count; i++)
            {
                EnemyDefinition.EnemyDrop drop = drops[i];
                if (drop == null || drop.item == null) continue;
                if (Random.value > drop.chance) continue;

                int count = Random.Range(drop.minCount, drop.maxCount + 1);

                // Rơi lệch khỏi xác quái để player thấy được (nếu rơi ngay dưới chân thì bị nhặt luôn).
                Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(0.9f, 1.5f);
                Vector3 spawnPosition = dead.transform.position + (Vector3)offset;

                ItemPickup pickup = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
                pickup.SetItem(drop.item, count);
                Debug.Log($"[LootDropper] {dead.Definition.DisplayName} rơi {drop.item.DisplayName} x{count}", pickup);
            }
        }
    }
}
