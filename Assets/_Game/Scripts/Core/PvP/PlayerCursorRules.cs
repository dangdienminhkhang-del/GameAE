using Game.Core.Combat;
using UnityEngine;

namespace Game.Core.PvP
{
    public enum PlayerCrimeStatus
    {
        Innocent = 0, // Con trỏ xanh (Green Player) - Người chơi trong sạch
        Criminal = 1, // Con trỏ cam (Orange Player) - Người chơi phạm tội
        Murderer = 2  // Con trỏ đỏ (Red Player) - Sát nhân / Laughing Coffin PKer
    }

    /// <summary>
    /// Quy tắc phân định màu Cursor người chơi theo SAO (docs/16 & prompt).
    /// - Green: Người chơi vô tội / bình thường
    /// - Orange: Người chơi phạm tội (tấn công người chơi khác)
    /// - Red: Kẻ giết người (Player Killer - PKer)
    /// </summary>
    public static class PlayerCursorRules
    {
        public const float OrangeDurationSeconds = 600f; // 10 phút cam trước khi trở lại xanh

        /// <summary>
        /// Ánh xạ trạng thái tội phạm của người chơi sang CursorThreatLevel của Core Combat.
        /// </summary>
        public static CursorThreatLevel ToThreatLevel(PlayerCrimeStatus status)
        {
            switch (status)
            {
                case PlayerCrimeStatus.Criminal:
                    return CursorThreatLevel.PlayerOrange;
                case PlayerCrimeStatus.Murderer:
                    return CursorThreatLevel.PlayerRed;
                case PlayerCrimeStatus.Innocent:
                default:
                    return CursorThreatLevel.PlayerGreen;
            }
        }

        /// <summary>
        /// Lấy màu Color tương ứng cho Cursor người chơi.
        /// </summary>
        public static Color GetCursorColor(PlayerCrimeStatus status)
        {
            return EnemyCursorRules.GetCursorColor(ToThreatLevel(status));
        }

        /// <summary>
        /// Xử lý hành vi tấn công người chơi khác ngoài vùng an toàn.
        /// Tấn công người chơi vô tội sẽ chuyển ngay sang màu Cam.
        /// </summary>
        public static PlayerCrimeStatus OnAttackPlayer(PlayerCrimeStatus currentStatus, PlayerCrimeStatus targetStatus)
        {
            if (currentStatus == PlayerCrimeStatus.Murderer)
            {
                return PlayerCrimeStatus.Murderer; // Đã là đỏ thì vẫn là đỏ
            }

            if (targetStatus == PlayerCrimeStatus.Innocent)
            {
                return PlayerCrimeStatus.Criminal; // Tấn công người vô tội -> Cam
            }

            return currentStatus; // Đánh kẻ thù Cam hoặc Đỏ thì không bị đổi sang tội phạm
        }

        /// <summary>
        /// Xử lý hành vi kết liễu người chơi khác ngoài vùng an toàn.
        /// Giết người chơi vô tội sẽ biến vĩnh viễn thành Sát Thủ (Đỏ).
        /// </summary>
        public static PlayerCrimeStatus OnKillPlayer(PlayerCrimeStatus currentStatus, PlayerCrimeStatus targetStatus)
        {
            if (targetStatus == PlayerCrimeStatus.Innocent)
            {
                return PlayerCrimeStatus.Murderer; // Giết người vô tội -> Đỏ
            }

            return currentStatus; // Tiêu diệt kẻ có tội trạng thì không bị phạt thành sát thủ
        }
    }
}
