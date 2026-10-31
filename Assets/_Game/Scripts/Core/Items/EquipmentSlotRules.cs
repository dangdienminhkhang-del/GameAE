namespace Game.Core.Items
{
    /// <summary>
    /// Luật chọn ô trang bị khi mặc đồ (thuần, dễ test).
    /// - Vũ khí (Weapon): mặc vào tay chính; nếu tay chính đã có vũ khí và tay phụ trống thì vào tay phụ
    ///   (OffHand) để kích hoạt Nhị Đao Lưu (Dual Wielding theo SAO); nếu cả 2 đầy thì thay tay chính.
    /// - Tay phụ (OffHand): mặc trực tiếp vào ô tay phụ.
    /// - Áo (Armor): mặc vào ô áo.
    /// - Phụ kiện (Accessory): có 2 ô (Accessory, Accessory2), mặc vào ô trống đầu tiên; cả hai đầy thì thay ô đầu.
    /// - Món đã mặc rồi thì giữ nguyên ô hiện tại (không mặc trùng một món vào hai ô).
    /// </summary>
    public static class EquipmentSlotRules
    {
        /// <summary>Tất cả ô trang bị, theo thứ tự hiển thị.</summary>
        public static readonly EquipmentSlot[] AllSlots =
        {
            EquipmentSlot.Weapon,
            EquipmentSlot.OffHand,
            EquipmentSlot.Armor,
            EquipmentSlot.Accessory,
            EquipmentSlot.Accessory2
        };

        public static bool IsAccessorySlot(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.Accessory || slot == EquipmentSlot.Accessory2;
        }

        /// <summary>
        /// Ô thật sẽ mặc vào cho món có ô khai báo là <paramref name="definitionSlot"/>.
        /// Trả None nếu món không mặc được.
        /// </summary>
        public static EquipmentSlot ResolveTarget(EquipmentSlot definitionSlot, Equipment equipment, string instanceId)
        {
            if (definitionSlot == EquipmentSlot.None) return EquipmentSlot.None;
            if (equipment == null) return definitionSlot == EquipmentSlot.Accessory ? EquipmentSlot.Accessory : definitionSlot;

            // Đã mặc rồi → giữ ô hiện tại, tránh một món nằm hai ô.
            if (!string.IsNullOrEmpty(instanceId))
            {
                if (equipment.GetInstanceId(definitionSlot) == instanceId) return definitionSlot;
                if (definitionSlot == EquipmentSlot.Accessory && equipment.GetInstanceId(EquipmentSlot.Accessory2) == instanceId) return EquipmentSlot.Accessory2;
                if (definitionSlot == EquipmentSlot.Weapon && equipment.GetInstanceId(EquipmentSlot.OffHand) == instanceId) return EquipmentSlot.OffHand;
            }

            if (definitionSlot == EquipmentSlot.Weapon)
            {
                // Ưu tiên tay chính (Weapon) trước; nếu đã có vũ khí tay chính và tay phụ trống → vào tay phụ (Dual Wielding).
                if (string.IsNullOrEmpty(equipment.GetInstanceId(EquipmentSlot.Weapon))) return EquipmentSlot.Weapon;
                if (string.IsNullOrEmpty(equipment.GetInstanceId(EquipmentSlot.OffHand))) return EquipmentSlot.OffHand;
                return EquipmentSlot.Weapon;
            }

            if (definitionSlot == EquipmentSlot.OffHand) return EquipmentSlot.OffHand;
            if (definitionSlot == EquipmentSlot.Armor) return EquipmentSlot.Armor;

            if (IsAccessorySlot(definitionSlot))
            {
                if (string.IsNullOrEmpty(equipment.GetInstanceId(EquipmentSlot.Accessory))) return EquipmentSlot.Accessory;
                if (string.IsNullOrEmpty(equipment.GetInstanceId(EquipmentSlot.Accessory2))) return EquipmentSlot.Accessory2;
                return EquipmentSlot.Accessory;
            }

            return definitionSlot;
        }

        public static string DisplayName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: return "Vũ khí";
                case EquipmentSlot.OffHand: return "Tay phụ";
                case EquipmentSlot.Armor: return "Áo";
                case EquipmentSlot.Accessory: return "Phụ kiện 1";
                case EquipmentSlot.Accessory2: return "Phụ kiện 2";
                default: return string.Empty;
            }
        }
    }
}
