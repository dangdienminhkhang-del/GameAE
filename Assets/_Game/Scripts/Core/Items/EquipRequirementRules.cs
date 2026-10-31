namespace Game.Core.Items
{
    /// <summary>
    /// Yêu cầu STR để mặc trang bị (SAO: đồ nặng cần đủ STR). Thuần, dễ test.
    /// "STR" ở đây là số điểm đã cộng vào STR (StatAllocation.Strength); món có yêu cầu 0 thì luôn mặc được.
    /// </summary>
    public static class EquipRequirementRules
    {
        /// <summary>Đủ STR để mặc? Yêu cầu âm hoặc 0 = luôn được.</summary>
        public static bool MeetsStrength(int requiredStrength, int currentStrength)
        {
            if (requiredStrength <= 0) return true;
            return currentStrength >= requiredStrength;
        }

        /// <summary>Thông báo khi không đủ STR (rỗng nếu đủ).</summary>
        public static string MissingMessage(int requiredStrength, int currentStrength)
        {
            if (MeetsStrength(requiredStrength, currentStrength)) return string.Empty;
            return $"Cần STR {requiredStrength} để mặc (hiện {(currentStrength < 0 ? 0 : currentStrength)})";
        }

        /// <summary>Dòng mô tả yêu cầu cho UI (rỗng nếu không yêu cầu).</summary>
        public static string RequirementLabel(int requiredStrength)
        {
            return requiredStrength > 0 ? $"Cần STR {requiredStrength}" : string.Empty;
        }
    }
}
