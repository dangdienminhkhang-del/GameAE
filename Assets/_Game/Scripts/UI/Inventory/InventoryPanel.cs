using System.Collections.Generic;
using Game.Core.Items;
using Game.PvE.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Inventory
{
    /// <summary>
    /// UI kho đồ tối thiểu (Phase 5): hiển thị kho + trang bị đang mặc.
    /// Chỉ hiển thị và gửi ý định (mặc/tháo) — không chứa logic inventory/chỉ số.
    /// Phím tắt: I = bật/tắt, 1..9 = mặc instance tương ứng, 0 = tháo hết.
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text bodyText;

        private readonly List<ItemInstance> _equippable = new List<ItemInstance>();

        private void OnEnable()
        {
            if (playerInventory != null) playerInventory.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (playerInventory != null) playerInventory.Changed -= Refresh;
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.iKey.wasPressedThisFrame) Toggle();

            for (int i = 1; i <= 9; i++)
            {
                var key = keyboard[UnityEngine.InputSystem.Key.Digit1 + (i - 1)];
                if (key != null && key.wasPressedThisFrame) EquipByIndex(i);
            }
            if (keyboard.digit0Key.wasPressedThisFrame) UnequipAll();
        }

        public void Toggle()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(!panelRoot.activeSelf);
            if (panelRoot.activeSelf) Refresh();
        }

        public void EquipByIndex(int index)
        {
            if (playerInventory == null) return;
            if (index < 1 || index > _equippable.Count) return;
            playerInventory.Equip(_equippable[index - 1].instanceId);
        }

        public void UnequipAll()
        {
            if (playerInventory == null) return;
            playerInventory.Unequip(EquipmentSlot.Weapon);
            playerInventory.Unequip(EquipmentSlot.Armor);
        }

        public void Refresh()
        {
            if (playerInventory == null) return;

            _equippable.Clear();
            var lines = new List<string>();

            ItemStats bonus = playerInventory.CurrentEquipmentBonus();
            lines.Add($"Trang bị: {Describe(playerInventory.Equipment.GetInstanceId(EquipmentSlot.Weapon))} | {Describe(playerInventory.Equipment.GetInstanceId(EquipmentSlot.Armor))}");
            lines.Add($"Cộng thêm: ATK+{bonus.attack:0.#} DEF+{bonus.defense:0.#} HP+{bonus.maxHealth:0.#}");
            lines.Add("");

            Game.Core.Items.Inventory inventory = playerInventory.Inventory;
            foreach (ItemStack stack in inventory.Stacks)
            {
                ItemDefinition definition = playerInventory.Database != null ? playerInventory.Database.Find(stack.itemId) : null;
                lines.Add($"- {definition?.DisplayName ?? stack.itemId} x{stack.quantity}");
            }

            foreach (ItemInstance instance in inventory.Instances)
            {
                ItemDefinition definition = playerInventory.Database != null ? playerInventory.Database.Find(instance.itemId) : null;
                bool equippable = definition != null && definition.Slot != EquipmentSlot.None;
                if (equippable) _equippable.Add(instance);

                string label = definition?.DisplayName ?? instance.itemId;
                string slot = equippable ? $" [{_equippable.Count}]" : string.Empty;
                string equipped = playerInventory.IsEquipped(instance.instanceId) ? " (đang mặc)" : string.Empty;
                lines.Add($"- {label}{slot}{equipped}");
            }

            if (lines.Count <= 3) lines.Add("(kho trống — đánh quái để nhặt đồ)");
            lines.Add("");
            lines.Add("I: đóng/mở | 1..9: mặc | 0: tháo hết");

            SetBody(string.Join("\n", lines));
        }

        private string Describe(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId) || playerInventory == null) return "(trống)";
            ItemInstance instance = playerInventory.Inventory.FindInstance(instanceId);
            if (instance == null) return "(trống)";
            ItemDefinition definition = playerInventory.Database != null ? playerInventory.Database.Find(instance.itemId) : null;
            return definition != null ? definition.DisplayName : instance.itemId;
        }

        private void SetBody(string text)
        {
            if (bodyText != null) bodyText.text = text;
        }
    }
}
