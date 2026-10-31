using System;

namespace Game.Core.Items
{
    /// <summary>
    /// Luật độ bền trang bị (SAO: vũ khí/giáp hao mòn khi dùng, thợ rèn sửa bằng Col). Thuần, dễ test.
    /// Độ bền lưu trên ItemInstance.durability: -1 = chưa thiết lập = đầy. ItemDefinition.MaxDurability = 0 nghĩa là
    /// món không hao mòn (phụ kiện, đồ cũ chưa khai báo). Độ bền về 0 = HỎNG: không cộng chỉ số cho tới khi sửa.
    /// </summary>
    public static class DurabilityRules
    {
        /// <summary>Giá trị lưu nghĩa là "chưa thiết lập" → coi như đầy.</summary>
        public const int UnsetDurability = -1;

        /// <summary>Tỉ lệ độ bền còn lại ≤ ngưỡng này thì cảnh báo "sắp hỏng".</summary>
        public const double LowFraction = 0.2;

        /// <summary>Mỗi lần trúng đòn (vũ khí) hoặc bị đánh (giáp) mất bao nhiêu độ bền.</summary>
        public const int WearPerHit = 1;

        /// <summary>Chi phí sửa đầy tối thiểu (Col).</summary>
        public const int MinFullRepairCost = 10;

        /// <summary>Chi phí sửa đầy = SellValue × hệ số này (× thêm mỗi cấp cường hóa).</summary>
        public const double RepairCostPerSellValue = 0.5;

        /// <summary>Mỗi cấp cường hóa làm phí sửa đắt thêm bao nhiêu (0.15 = +15%).</summary>
        public const double RepairCostPerUpgradeLevel = 0.15;

        /// <summary>Độ bền hiện tại thật: chưa thiết lập hoặc vượt tối đa → đầy; món không hao mòn → 0.</summary>
        public static int Resolve(int stored, int maxDurability)
        {
            if (maxDurability <= 0) return 0;
            if (stored < 0 || stored > maxDurability) return maxDurability;
            return stored;
        }

        /// <summary>Món có hao mòn (MaxDurability &gt; 0)?</summary>
        public static bool Wears(int maxDurability)
        {
            return maxDurability > 0;
        }

        public static bool IsBroken(int stored, int maxDurability)
        {
            return maxDurability > 0 && Resolve(stored, maxDurability) <= 0;
        }

        /// <summary>Sắp hỏng: còn ≤ 20% (và chưa hỏng).</summary>
        public static bool IsLow(int stored, int maxDurability)
        {
            if (maxDurability <= 0 || IsBroken(stored, maxDurability)) return false;
            return Resolve(stored, maxDurability) <= (int)Math.Ceiling(maxDurability * LowFraction);
        }

        /// <summary>Độ bền sau khi hao <paramref name="amount"/> (không xuống dưới 0). Món không hao mòn → giữ nguyên.</summary>
        public static int ApplyWear(int stored, int maxDurability, int amount)
        {
            if (maxDurability <= 0) return stored;
            int current = Resolve(stored, maxDurability);
            if (amount <= 0) return current;
            int next = current - amount;
            return next < 0 ? 0 : next;
        }

        /// <summary>Số điểm độ bền đang thiếu so với tối đa.</summary>
        public static int Missing(int stored, int maxDurability)
        {
            if (maxDurability <= 0) return 0;
            return maxDurability - Resolve(stored, maxDurability);
        }

        /// <summary>Phí sửa đầy từ 0 → tối đa (Col).</summary>
        public static int FullRepairCost(int sellValue, int upgradeLevel)
        {
            double raw = (sellValue < 0 ? 0 : sellValue) * RepairCostPerSellValue
                         * (1.0 + RepairCostPerUpgradeLevel * (upgradeLevel < 0 ? 0 : upgradeLevel));
            int cost = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
            return cost < MinFullRepairCost ? MinFullRepairCost : cost;
        }

        /// <summary>Phí sửa phần đang thiếu (tỉ lệ theo số điểm thiếu, làm tròn lên, tối thiểu 1 nếu có thiếu).</summary>
        public static int RepairCost(int stored, int maxDurability, int sellValue, int upgradeLevel)
        {
            int missing = Missing(stored, maxDurability);
            if (missing <= 0) return 0;
            int full = FullRepairCost(sellValue, upgradeLevel);
            int cost = (int)Math.Ceiling(full * (double)missing / maxDurability);
            return cost < 1 ? 1 : cost;
        }

        /// <summary>Văn bản hiển thị, ví dụ "85/100" (rỗng nếu món không hao mòn).</summary>
        public static string Label(int stored, int maxDurability)
        {
            if (maxDurability <= 0) return string.Empty;
            return $"{Resolve(stored, maxDurability)}/{maxDurability}";
        }
    }
}
