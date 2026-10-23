namespace Game.Core.Stats
{
    /// <summary>
    /// Luật hồi máu tự nhiên của người chơi. Hàm thuần: nhận tham số, trả số máu hồi — dễ test.
    /// Cố ý để chậm để bình thuốc vẫn có giá trị: ngoài giao chiến hồi chậm, trong Vùng an toàn (Trụ Đèn) hồi nhanh.
    /// </summary>
    public static class HealthRegenRules
    {
        /// <summary>Tỉ lệ MaxHealth hồi mỗi giây khi ngoài giao chiến (0.5%/s → ~200 giây đầy máu).</summary>
        public const double OutOfCombatFractionPerSecond = 0.005;

        /// <summary>Tỉ lệ MaxHealth hồi mỗi giây khi đứng trong Vùng an toàn (3%/s → ~33 giây đầy máu).</summary>
        public const double SafeZoneFractionPerSecond = 0.03;

        /// <summary>Tỉ lệ hồi/giây theo hoàn cảnh. Đang giao chiến ngoài vùng an toàn = 0.</summary>
        public static double FractionPerSecond(bool inCombat, bool inSafeZone)
        {
            if (inSafeZone) return SafeZoneFractionPerSecond;
            return inCombat ? 0.0 : OutOfCombatFractionPerSecond;
        }

        /// <summary>
        /// Số máu hồi trong deltaSeconds giây. Không vượt MaxHealth, không hồi khi đã chết / đầy máu.
        /// </summary>
        public static double ComputeHeal(double current, double max, double deltaSeconds, bool inCombat, bool inSafeZone, bool isDead)
        {
            if (isDead || max <= 0.0 || deltaSeconds <= 0.0 || current >= max) return 0.0;

            double heal = max * FractionPerSecond(inCombat, inSafeZone) * deltaSeconds;
            double room = max - current;
            return heal > room ? room : (heal < 0.0 ? 0.0 : heal);
        }
    }
}
