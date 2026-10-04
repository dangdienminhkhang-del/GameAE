using System.Collections.Generic;
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

        public ItemDefinition Find(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            EnsureLookup();
            return _lookup.TryGetValue(itemId, out ItemDefinition definition) ? definition : null;
        }

        private void EnsureLookup()
        {
            if (_built) return;
            _built = true;

            _lookup = new Dictionary<string, ItemDefinition>();
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.Id))
                {
                    Debug.LogError($"[ItemDatabase] '{item.name}' thiếu id — bỏ qua (docs/06: lỗi phải rõ ràng)", item);
                    continue;
                }
                if (_lookup.ContainsKey(item.Id))
                {
                    Debug.LogError($"[ItemDatabase] id trùng '{item.Id}' ở '{item.name}' — asset sau bị bỏ", item);
                    continue;
                }
                _lookup[item.Id] = item;
            }
        }

        /// <summary>Tổng chỉ số của tất cả trang bị đang mặc.</summary>
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

                total += definition.BaseStats + instance.rolledStats;
            }
            return total;
        }
    }
}
