using System;
using System.Collections.Generic;

namespace Game.Core.Items
{
    /// <summary>Một vật phẩm cụ thể trong kho (có instanceId — docs/03 §5).</summary>
    [Serializable]
    public class ItemInstance
    {
        public string instanceId;
        public string itemId;
        public int upgradeLevel;
        public ItemStats rolledStats;

        public ItemInstance() { }

        public ItemInstance(string instanceId, string itemId, ItemStats rolledStats, int upgradeLevel = 0)
        {
            this.instanceId = instanceId;
            this.itemId = itemId;
            this.rolledStats = rolledStats;
            this.upgradeLevel = upgradeLevel;
        }
    }

    /// <summary>Một stack item (docs/03 §5: inventory.stacks[] — nhờ vậy một item có thể nhiều stack).</summary>
    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int quantity;

        public ItemStack() { }

        public ItemStack(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
    }

    /// <summary>
    /// Kho đồ thuần logic: stack (item tiêu hao/nguyên liệu) + instance (trang bị).
    /// Không mất đồ: tràn maxStack thì mở stack mới. Không biết UI/save.
    /// </summary>
    public class Inventory
    {
        private readonly List<ItemStack> _stacks = new List<ItemStack>();
        private readonly List<ItemInstance> _instances = new List<ItemInstance>();

        public event Action Changed;

        public IReadOnlyList<ItemStack> Stacks => _stacks;
        public IReadOnlyList<ItemInstance> Instances => _instances;

        public int TotalStackQuantity
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _stacks.Count; i++) total += _stacks[i].quantity;
                return total;
            }
        }

        /// <summary>Tổng số lượng của một item (cộng mọi stack).</summary>
        public int GetStack(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            int total = 0;
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i].itemId == itemId) total += _stacks[i].quantity;
            }
            return total;
        }

        /// <summary>
        /// Thêm item; trả về SỐ LƯỢNG ĐÃ NHẬN THẬT.
        /// Nhồi vào stack sẵn có trước, phần tràn mở stack mới (không nuốt mất đồ).
        /// </summary>
        public int AddStack(string itemId, int quantity, int maxStack)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return 0;
            int capacity = maxStack < 1 ? int.MaxValue : maxStack;
            int remaining = quantity;

            for (int i = 0; i < _stacks.Count && remaining > 0; i++)
            {
                ItemStack stack = _stacks[i];
                if (stack.itemId != itemId) continue;

                int space = capacity - stack.quantity;
                if (space <= 0) continue;

                int add = Math.Min(space, remaining);
                stack.quantity += add;
                remaining -= add;
            }

            while (remaining > 0)
            {
                int add = Math.Min(capacity, remaining);
                _stacks.Add(new ItemStack(itemId, add));
                remaining -= add;
            }

            int accepted = quantity - remaining;
            if (accepted > 0) Changed?.Invoke();
            return accepted;
        }

        /// <summary>Trừ item trên nhiều stack; false nếu không đủ.</summary>
        public bool RemoveStack(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            if (GetStack(itemId) < quantity) return false;

            int remaining = quantity;
            for (int i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStack stack = _stacks[i];
                if (stack.itemId != itemId) continue;

                int take = Math.Min(stack.quantity, remaining);
                stack.quantity -= take;
                remaining -= take;
                if (stack.quantity <= 0) _stacks.RemoveAt(i);
            }

            Changed?.Invoke();
            return true;
        }

        public void AddInstance(ItemInstance instance)
        {
            if (instance == null || string.IsNullOrEmpty(instance.instanceId)) return;
            _instances.Add(instance);
            Changed?.Invoke();
        }

        public bool RemoveInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return false;
            int index = _instances.FindIndex(i => i.instanceId == instanceId);
            if (index < 0) return false;

            _instances.RemoveAt(index);
            Changed?.Invoke();
            return true;
        }

        public ItemInstance FindInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            return _instances.Find(i => i.instanceId == instanceId);
        }

        public void Clear()
        {
            _stacks.Clear();
            _instances.Clear();
            Changed?.Invoke();
        }

        /// <summary>Nạp lại từ save (không phát Changed từng bước).</summary>
        public void LoadFrom(IEnumerable<ItemStack> stacks, IEnumerable<ItemInstance> instances)
        {
            _stacks.Clear();
            _instances.Clear();

            if (stacks != null)
            {
                foreach (ItemStack stack in stacks)
                {
                    if (stack != null && !string.IsNullOrEmpty(stack.itemId) && stack.quantity > 0) _stacks.Add(stack);
                }
            }
            if (instances != null)
            {
                foreach (ItemInstance instance in instances)
                {
                    if (instance != null && !string.IsNullOrEmpty(instance.instanceId)) _instances.Add(instance);
                }
            }
            Changed?.Invoke();
        }
    }
}
