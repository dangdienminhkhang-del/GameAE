using Game.Core.Stats;
using Game.PvE.Items;
using Game.PvE.SafeZone;
using UnityEngine;

namespace Game.PvE.Player
{
    /// <summary>
    /// Hồi máu tự nhiên (giống SAO): ngoài giao chiến hồi chậm, trong Vùng an toàn hồi nhanh.
    /// Luật tính nằm ở <see cref="HealthRegenRules"/>. Tick mỗi giây (không phải mỗi frame) để không
    /// spam event HealthChanged / lịch tự lưu. Dùng Time.deltaTime nên Pause (timeScale = 0) tự dừng hồi.
    /// Tự được HealthComponent gắn vào người chơi, không cần sửa scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthRegenController : MonoBehaviour
    {
        [SerializeField] private bool regenEnabled = true;
        [SerializeField, Min(0.1f)] private float tickSeconds = 1f;

        private HealthComponent _health;
        private PlayerInventory _inventory;
        private float _accumulator;

        private void Awake()
        {
            TryGetComponent(out _health);
            TryGetComponent(out _inventory);
        }

        private void Update()
        {
            if (!regenEnabled || _health == null) return;

            _accumulator += Time.deltaTime;
            if (_accumulator < tickSeconds) return;

            float elapsed = _accumulator;
            _accumulator = 0f;

            bool inCombat = _inventory != null && _inventory.IsInCombat;
            bool inSafeZone = SafeZoneProvider.GetOrCreate().IsSafeZoneAt(transform.position);

            double heal = HealthRegenRules.ComputeHeal(
                _health.CurrentHealth, _health.MaxHealth, elapsed, inCombat, inSafeZone, _health.IsDead);
            if (heal > 0.0) _health.Heal(heal);
        }
    }
}
