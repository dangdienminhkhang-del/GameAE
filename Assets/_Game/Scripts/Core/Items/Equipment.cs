using System;
using System.Collections.Generic;

namespace Game.Core.Items
{
    /// <summary>Trang bị đang mặc: mỗi slot giữ instanceId (null = trống). docs/03 §5.</summary>
    public class Equipment
    {
        private readonly Dictionary<EquipmentSlot, string> _slots = new Dictionary<EquipmentSlot, string>();

        public event Action Changed;

        public bool IsEquipped(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return false;
            foreach (var pair in _slots)
            {
                if (pair.Value == instanceId) return true;
            }
            return false;
        }

        public string GetInstanceId(EquipmentSlot slot)
        {
            return _slots.TryGetValue(slot, out string id) ? id : null;
        }

        /// <summary>Mặc instance vào slot; trả về instanceId cũ (nếu có) để caller trả lại kho.</summary>
        public string Equip(EquipmentSlot slot, string instanceId)
        {
            if (slot == EquipmentSlot.None || string.IsNullOrEmpty(instanceId)) return null;

            _slots.TryGetValue(slot, out string previous);
            _slots[slot] = instanceId;
            Changed?.Invoke();
            return previous;
        }

        public string Unequip(EquipmentSlot slot)
        {
            if (!_slots.TryGetValue(slot, out string previous)) return null;
            _slots.Remove(slot);
            Changed?.Invoke();
            return previous;
        }

        public IEnumerable<KeyValuePair<EquipmentSlot, string>> All => _slots;

        public void Clear()
        {
            _slots.Clear();
            Changed?.Invoke();
        }

        public void LoadFrom(IEnumerable<KeyValuePair<EquipmentSlot, string>> slots)
        {
            _slots.Clear();
            if (slots != null)
            {
                foreach (var pair in slots)
                {
                    if (pair.Key != EquipmentSlot.None && !string.IsNullOrEmpty(pair.Value)) _slots[pair.Key] = pair.Value;
                }
            }
            Changed?.Invoke();
        }
    }
}
