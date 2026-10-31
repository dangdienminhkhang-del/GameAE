using UnityEngine;

namespace Game.Core.Combat
{
    /// <summary>
    /// Loại cursor và mức độ đe doạ theo phong cách Sword Art Online (SAO).
    /// </summary>
    public enum CursorThreatLevel
    {
        Weak,           // Xám / Nhạt (quái thấp hơn người chơi >= 5 cấp)
        Equal,          // Vàng (ngang cấp, trong khoảng ±4 cấp)
        Dangerous,      // Đỏ (quái cao hơn người chơi >= 5 cấp)

        // Dành riêng cho PvP (Luồng B):
        PlayerGreen,    // Người chơi vô tội / đồng minh (Con trỏ xanh lá)
        PlayerOrange,   // Người chơi phạm tội / PK (Con trỏ cam)
        PlayerRed       // Người chơi sát thủ / PKer (Con trỏ đỏ)
    }

    /// <summary>
    /// Quy tắc xác định màu cursor theo chênh lệch cấp độ giữa Quái và Người chơi.
    /// - Ngang cấp (±4): Vàng
    /// - Cao hơn từ +5 trở lên: Đỏ
    /// - Thấp hơn từ -5 trở xuống: Nhạt / Xám
    /// </summary>
    public static class EnemyCursorRules
    {
        /// <summary>Ngưỡng chênh lệch cấp độ để coi là quái nguy hiểm (Đỏ).</summary>
        public const int HighLevelThreshold = 5;

        /// <summary>Ngưỡng chênh lệch cấp độ để coi là quái yếu (Xám/Nhạt).</summary>
        public const int LowLevelThreshold = -5;

        /// <summary>
        /// Đánh giá mức độ đe doạ của quái so với người chơi.
        /// delta = enemyLevel - playerLevel.
        /// </summary>
        public static CursorThreatLevel EvaluateEnemyThreat(int enemyLevel, int playerLevel)
        {
            int delta = enemyLevel - playerLevel;
            if (delta >= HighLevelThreshold) return CursorThreatLevel.Dangerous;
            if (delta <= LowLevelThreshold) return CursorThreatLevel.Weak;
            return CursorThreatLevel.Equal;
        }

        /// <summary>
        /// Lấy màu tương ứng cho mức độ đe doạ cursor.
        /// </summary>
        public static Color GetCursorColor(CursorThreatLevel level)
        {
            switch (level)
            {
                case CursorThreatLevel.Dangerous:
                    return new Color(0.96f, 0.22f, 0.22f, 1f); // Đỏ rực nguy hiểm
                case CursorThreatLevel.Equal:
                    return new Color(0.96f, 0.84f, 0.18f, 1f); // Vàng ngang cấp
                case CursorThreatLevel.Weak:
                    return new Color(0.65f, 0.70f, 0.75f, 0.75f); // Xám nhạt
                case CursorThreatLevel.PlayerGreen:
                    return new Color(0.20f, 0.85f, 0.35f, 1f); // Xanh lá SAO
                case CursorThreatLevel.PlayerOrange:
                    return new Color(0.98f, 0.55f, 0.15f, 1f); // Cam tội trạng
                case CursorThreatLevel.PlayerRed:
                    return new Color(0.88f, 0.12f, 0.12f, 1f); // Đỏ sát thủ
                default:
                    return Color.white;
            }
        }
    }
}
