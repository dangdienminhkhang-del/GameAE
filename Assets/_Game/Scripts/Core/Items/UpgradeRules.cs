using System;

namespace Game.Core.Items
{
    public enum UpgradeOutcome { Success, Failed, FailedDowngraded }

    /// <summary>
    /// Luật cường hóa trang bị ở thợ rèn (SAO: thợ rèn cường hóa theo tỉ lệ thành công, càng cao càng khó).
    /// Thuần, dễ test: tỉ lệ, chi phí, cộng chỉ số và kết quả một lần thử (nhận số ngẫu nhiên từ ngoài).
    /// Mọi con số là giả định, đổi ở hằng số/bảng bên dưới.
    /// </summary>
    public static class UpgradeRules
    {
        /// <summary>Cấp cường hóa tối đa (+10).</summary>
        public const int MaxLevel = 10;

        /// <summary>Mỗi cấp cộng thêm bao nhiêu % chỉ số gốc của món (0.08 = +8% mỗi cấp, +10 = +80%).</summary>
        public const double BonusPerLevel = 0.08;

        /// <summary>Thất bại khi đang từ cấp này trở lên thì bị tụt 1 cấp (mạo hiểm như SAO). Đặt MaxLevel + 1 để tắt.</summary>
        public const int DowngradeFromLevel = 7;

        /// <summary>Phí cơ bản (Col) cho mỗi bậc đồ (tier) và mỗi cấp: phí = 50 × tier × (cấp hiện tại + 1).</summary>
        public const int CostPerTierPerStep = 50;

        // Tỉ lệ thành công khi thử nâng từ cấp i lên i+1 (i = 0..9).
        private static readonly double[] SuccessChances =
        {
            1.00, 0.95, 0.90, 0.80, 0.70, 0.60, 0.50, 0.40, 0.30, 0.20
        };

        public static bool CanUpgrade(int level)
        {
            return level >= 0 && level < MaxLevel;
        }

        /// <summary>Tỉ lệ thành công khi nâng từ cấp hiện tại lên cấp kế (0 nếu đã tối đa).</summary>
        public static double SuccessChance(int level)
        {
            return CanUpgrade(level) ? SuccessChances[level] : 0.0;
        }

        /// <summary>Chi phí (Col) cho một lần thử từ cấp hiện tại. Tier nhỏ hơn 1 tính là 1. 0 nếu đã tối đa.</summary>
        public static int Cost(int tier, int level)
        {
            if (!CanUpgrade(level)) return 0;
            int safeTier = tier < 1 ? 1 : tier;
            return CostPerTierPerStep * safeTier * (level + 1);
        }

        /// <summary>Chỉ số cộng thêm từ cường hóa, tính trên chỉ số gốc (gồm cả chỉ số ngẫu nhiên khi rơi đồ).</summary>
        public static ItemStats BonusStats(ItemStats baseStats, int level)
        {
            if (level <= 0) return default;
            double factor = BonusPerLevel * Math.Min(level, MaxLevel);
            return new ItemStats(baseStats.attack * factor, baseStats.defense * factor, baseStats.maxHealth * factor);
        }

        /// <summary>
        /// Kết quả một lần thử. <paramref name="roll01"/> là số ngẫu nhiên trong [0,1). Thành công khi roll nhỏ hơn tỉ lệ.
        /// newLevel là cấp sau khi thử.
        /// </summary>
        public static UpgradeOutcome Resolve(int level, double roll01, out int newLevel)
        {
            newLevel = level;
            if (!CanUpgrade(level)) return UpgradeOutcome.Failed;

            if (roll01 < SuccessChance(level))
            {
                newLevel = level + 1;
                return UpgradeOutcome.Success;
            }

            if (level >= DowngradeFromLevel)
            {
                newLevel = level - 1;
                return UpgradeOutcome.FailedDowngraded;
            }

            return UpgradeOutcome.Failed;
        }

        /// <summary>Nhãn hiển thị, ví dụ "+3" (rỗng nếu chưa cường hóa).</summary>
        public static string Label(int level)
        {
            return level > 0 ? "+" + level : string.Empty;
        }
    }
}
