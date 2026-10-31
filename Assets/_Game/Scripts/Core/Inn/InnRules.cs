using System;

namespace Game.Core.Inn
{
    public enum InnRestResult
    {
        Success = 0,
        NotEnoughCol = 1,
        InCombat = 2,
        IsDead = 3,
        InvalidArguments = 4
    }

    /// <summary>
    /// Quy tắc nghỉ trọ tại Quán trọ (Inn) theo Sword Art Online (docs/16 & prompt).
    /// Người chơi trả Col để ngủ qua đêm: hồi phục toàn bộ HP và thời gian thế giới tua sang sáng (Morning).
    /// Điều kiện: không đang trong giao chiến, nhân vật còn sống, đủ Col.
    /// Lớp thuần C# không MonoBehaviour, dễ kiểm thử.
    /// </summary>
    public static class InnRules
    {
        /// <summary>Chi phí nghỉ trọ mặc định tại thị trấn khởi đầu (50 Col).</summary>
        public const int DefaultRestCost = 50;

        /// <summary>Giờ buổi sáng mục tiêu khi thức dậy (8:00 sáng, pha Morning 7h-11h).</summary>
        public const double DefaultMorningHour = 8.0;

        /// <summary>
        /// Kiểm tra xem người chơi có đủ điều kiện nghỉ ngơi tại quán trọ hay không.
        /// </summary>
        public static InnRestResult CheckCanRest(int playerCol, int cost, bool inCombat, bool isDead)
        {
            if (cost < 0) return InnRestResult.InvalidArguments;
            if (isDead) return InnRestResult.IsDead;
            if (inCombat) return InnRestResult.InCombat;
            if (playerCol < cost) return InnRestResult.NotEnoughCol;

            return InnRestResult.Success;
        }

        /// <summary>
        /// Tính lượng HP cần hồi phục để đầy cây máu.
        /// </summary>
        public static double CalculateHealAmount(double currentHealth, double maxHealth)
        {
            if (maxHealth <= 0.0) return 0.0;
            if (currentHealth >= maxHealth) return 0.0;
            return Math.Max(0.0, maxHealth - currentHealth);
        }

        /// <summary>
        /// Lấy mốc giờ sáng mục tiêu để tua đồng hồ thế giới.
        /// </summary>
        public static double GetTargetMorningHour(double morningHour = DefaultMorningHour)
        {
            return ((morningHour % 24.0) + 24.0) % 24.0;
        }

        /// <summary>
        /// Diễn giải kết quả nghỉ ngơi thành thông điệp thân thiện.
        /// </summary>
        public static string GetResultMessage(InnRestResult result, int cost = DefaultRestCost)
        {
            switch (result)
            {
                case InnRestResult.Success:
                    return $"Đã nghỉ ngơi tại quán trọ (-{cost} Col). Thể lực hồi phục hoàn toàn, chào buổi sáng!";
                case InnRestResult.NotEnoughCol:
                    return $"Không đủ Col! Bạn cần {cost} Col để thuê phòng trọ.";
                case InnRestResult.InCombat:
                    return "Đang trong trạng thái giao chiến, không thể nghỉ trọ lúc này!";
                case InnRestResult.IsDead:
                    return "Nhân vật đã tử trận, không thể thực hiện hành động này.";
                default:
                    return "Không thể nghỉ ngơi lúc này.";
            }
        }
    }
}
