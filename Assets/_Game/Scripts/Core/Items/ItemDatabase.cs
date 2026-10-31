using System.Collections.Generic;
using Game.Core.Combat;
using UnityEngine;

namespace Game.Core.Items
{
    /// <summary>Danh sách ItemDefinition để tra itemId → definition khi load save / roll drop.</summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Core/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        private Dictionary<string, ItemDefinition> _lookup;
        private bool _built;

        public IReadOnlyList<ItemDefinition> Items => items;

        // OnEnable: ScriptableObject được nạp hoặc re-enable (Enter Play Mode không domain-reload).
        // OnValidate: Inspector thay đổi nội dung → cache có thể cũ.
        // Cả hai reset cache để EnsureLookup xây lại lần sau.
        private void OnEnable()  => ResetCache();
        private void OnValidate() => ResetCache();

        private void ResetCache()
        {
            _built  = false;
            _lookup = null;
        }

        public ItemDefinition Find(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            EnsureLookup();
            // _lookup luôn không-null sau EnsureLookup
            return _lookup.TryGetValue(itemId, out ItemDefinition definition) ? definition : null;
        }

        private void EnsureLookup()
        {
            // Kiểm tra cả _built VÀ _lookup để chịu trạng thái lệch
            // (Enter Play Mode / DisableDomainReload: _built=true nhưng _lookup=null)
            if (_built && _lookup != null) return;

            // Xây vào biến cục bộ, chỉ gán field khi xong để tránh state không nhất quán nếu có exception
            var map = new Dictionary<string, ItemDefinition>();
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.Id))
                {
                    Debug.LogError($"[ItemDatabase] '{item.name}' thiếu id — bỏ qua (docs/06: lỗi phải rõ ràng)", item);
                    continue;
                }
                if (map.ContainsKey(item.Id))
                {
                    Debug.LogError($"[ItemDatabase] id trùng '{item.Id}' ở '{item.name}' — asset sau bị bỏ", item);
                    continue;
                }
                map[item.Id] = item;
            }

            _lookup = map;
            _built  = true;
        }

        /// <summary>
        /// Tổng chỉ số của tất cả trang bị đang mặc. Mỗi món = (gốc + chỉ số ngẫu nhiên) + cộng từ cường hóa;
        /// món HỎNG (độ bền 0) không cộng gì cho tới khi sửa. Tay phụ đóng góp 60% công theo DualWieldRules.
        /// </summary>
        public ItemStats SumEquipment(Equipment equipment, Inventory inventory)
        {
            if (equipment == null || inventory == null) return default;

            ItemStats total = default;
            foreach (var pair in equipment.All)
            {
                ItemInstance instance = inventory.FindInstance(pair.Value);
                if (instance == null) continue;

                ItemDefinition definition = Find(instance.itemId);
                if (definition == null) continue;

                // Hỏng → không cộng chỉ số (SAO: vũ khí gần vỡ phải sửa ở thợ rèn).
                if (DurabilityRules.IsBroken(instance.durability, definition.MaxDurability)) continue;

                ItemStats stats = definition.BaseStats + instance.rolledStats;
                stats += UpgradeRules.BonusStats(stats, instance.upgradeLevel);
                if (pair.Key == EquipmentSlot.OffHand)
                {
                    // Tay phụ (Dual Wielding): đóng góp 60% sát thương tấn công theo DualWieldRules (chuẩn SAO)
                    stats.attack *= DualWieldRules.OffHandAttackContribution;
                }
                total += stats;
            }
            return total;
        }
    }
}
