using System;
using UnityEngine;

namespace Game.Core.Items
{
    public enum ItemType { Weapon = 0, Armor = 1, Consumable = 2, Material = 3 }

    public enum EquipmentSlot { None = 0, Weapon = 1, Armor = 2 }

    /// <summary>Chỉ số cộng thêm của item (docs/03 §3).</summary>
    [Serializable]
    public struct ItemStats
    {
        public double attack;
        public double defense;
        public double maxHealth;

        public ItemStats(double attack, double defense, double maxHealth)
        {
            this.attack = attack;
            this.defense = defense;
            this.maxHealth = maxHealth;
        }

        public static ItemStats operator +(ItemStats a, ItemStats b)
        {
            return new ItemStats(a.attack + b.attack, a.defense + b.defense, a.maxHealth + b.maxHealth);
        }

        public bool IsZero => attack == 0.0 && defense == 0.0 && maxHealth == 0.0;
    }

    /// <summary>Definition tĩnh của item — asset trong Data/Items (docs/03 §3, §5).</summary>
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Game/Core/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Nhận dạng")]
        [SerializeField] private string id = "item.new";
        [SerializeField] private string displayName = "Item";
        [SerializeField] private ItemType type = ItemType.Material;
        [SerializeField] private int tier = 1;

        [Header("Hiển thị")]
        [SerializeField] private Sprite icon;

        [Header("Chỉ số")]
        [SerializeField] private ItemStats baseStats;
        [SerializeField] private EquipmentSlot equipmentSlot = EquipmentSlot.None;

        [Header("Tồn kho")]
        [SerializeField] private bool stackable = true;
        [SerializeField] private int maxStack = 99;
        [SerializeField] private int sellValue = 1;

        [Header("Tiêu hao (chỉ có nghĩa với ItemType.Consumable)")]
        [Tooltip("Số máu hồi mỗi lần dùng. 0 = không dùng được để hồi máu.")]
        [SerializeField] private double healAmount;
        [Tooltip("Thời gian chờ giữa 2 lần dùng cùng loại vật phẩm này (giây).")]
        [SerializeField] private float useCooldownSeconds;

        public string Id => id;
        public string DisplayName => displayName;
        public ItemType Type => type;
        public int Tier => tier;
        public Sprite Icon => icon;
        public ItemStats BaseStats => baseStats;
        public EquipmentSlot Slot => equipmentSlot;
        public bool Stackable => stackable;
        public int MaxStack => maxStack < 1 ? 1 : maxStack;
        public int SellValue => sellValue;
        public double HealAmount => healAmount < 0.0 ? 0.0 : healAmount;
        public float UseCooldownSeconds => useCooldownSeconds < 0f ? 0f : useCooldownSeconds;

        private void Reset()
        {
            id = "item.new";
            displayName = "Item";
            type = ItemType.Material;
            tier = 1;
            stackable = true;
            maxStack = 99;
            sellValue = 1;
            equipmentSlot = EquipmentSlot.None;
            healAmount = 0.0;
            useCooldownSeconds = 0f;
        }
    }
}
