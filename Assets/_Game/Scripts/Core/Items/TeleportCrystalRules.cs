using System;

namespace Game.Core.Items
{
    public enum TeleportCrystalResult
    {
        Ok = 0,
        OutOfStock = 1,
        PlayerDead = 2,
        AlreadyChanneling = 3
    }

    /// <summary>
    /// Luật sử dụng Tinh Thể Dịch Chuyển (Teleport Crystal theo Sword Art Online).
    /// Người chơi hô khẩu lệnh ("Teleport!") và đứng yên niệm chú 3 giây để dịch chuyển về thị trấn an toàn.
    /// Nếu bị tấn công hoặc tự di chuyển trong thời gian này, quá trình dịch chuyển sẽ bị huỷ bỏ ngay lập tức.
    /// Logic C# thuần, dễ kiểm thử.
    /// </summary>
    public static class TeleportCrystalRules
    {
        public const string ItemId = "item.teleport_crystal";
        public const string ItemDisplayName = "Tinh Thể Dịch Chuyển";

        /// <summary>Thời gian đứng yên niệm chú dịch chuyển (giây).</summary>
        public const float ChannelSeconds = 3.0f;

        /// <summary>Khoảng cách di chuyển tối đa cho phép trước khi bị coi là đứt niệm chú.</summary>
        public const float MoveCancelDistance = 0.5f;

        /// <summary>
        /// Kiểm tra xem người chơi có thể bắt đầu kích hoạt tinh thể dịch chuyển hay không.
        /// </summary>
        public static TeleportCrystalResult CanUse(int crystalCount, bool isDead, bool isChanneling)
        {
            if (isDead) return TeleportCrystalResult.PlayerDead;
            if (isChanneling) return TeleportCrystalResult.AlreadyChanneling;
            if (crystalCount <= 0) return TeleportCrystalResult.OutOfStock;

            return TeleportCrystalResult.Ok;
        }

        /// <summary>
        /// Kiểm tra xem việc di chuyển có làm huỷ niệm chú không.
        /// </summary>
        public static bool IsCancelledByMovement(float distanceMoved)
        {
            return distanceMoved > MoveCancelDistance;
        }

        /// <summary>
        /// Kiểm tra xem sát thương nhận vào có làm huỷ niệm chú không.
        /// </summary>
        public static bool IsCancelledByDamage(double damageTaken)
        {
            return damageTaken > 0.0;
        }

        /// <summary>
        /// Chuỗi giải thích kết quả bằng tiếng Việt.
        /// </summary>
        public static string DescribeResult(TeleportCrystalResult result)
        {
            switch (result)
            {
                case TeleportCrystalResult.Ok:
                    return string.Empty;
                case TeleportCrystalResult.OutOfStock:
                    return "Không còn Tinh Thể Dịch Chuyển trong túi.";
                case TeleportCrystalResult.PlayerDead:
                    return "Nhân vật đã tử trận, không thể dùng tinh thể.";
                case TeleportCrystalResult.AlreadyChanneling:
                    return "Đang trong quá trình niệm chú dịch chuyển.";
                default:
                    return "Không thể dùng lúc này.";
            }
        }
    }
}
