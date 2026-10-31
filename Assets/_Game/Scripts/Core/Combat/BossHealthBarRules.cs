using System;
using UnityEngine;

namespace Game.Core.Combat
{
    /// <summary>
    /// Quy tắc tính toán nhiều thanh máu Boss và hệ số nhân máu Boss theo phong cách Sword Art Online (SAO).
    /// - Boss có nhiều thanh máu (mặc định 4 thanh như Illfang Tầng 1).
    /// - Cung cấp hệ số BossHealthMultiplier (mặc định ×10, dễ dàng đổi sang ×100).
    /// - Khi vào thanh máu cuối (thanh đỏ), Boss kích hoạt Cuồng nộ (Enrage) tăng công và tốc đánh.
    /// </summary>
    public static class BossHealthBarRules
    {
        /// <summary>Số thanh máu mặc định của Boss (boss Tầng 1 Illfang có 4 thanh máu).</summary>
        public const int DefaultBarCount = 4;

        /// <summary>Hệ số nhân máu Boss mặc định (×10). Dễ dàng chuyển sang ×100.</summary>
        public const double DefaultBossHealthMultiplier = 10.0;

        /// <summary>
        /// Hệ số nhân máu hiện tại của toàn bộ Boss trong game.
        /// Cho phép tuỳ biến khi test hoặc nạp từ cấu hình runtime.
        /// </summary>
        public static double BossHealthMultiplier { get; set; } = DefaultBossHealthMultiplier;

        /// <summary>Hệ số tăng sát thương khi Boss vào thanh máu cuối (Cuồng nộ / Enrage).</summary>
        public const double DefaultEnrageAttackMultiplier = 1.25;

        /// <summary>Hệ số tăng tốc độ di chuyển khi Cuồng nộ.</summary>
        public const float DefaultEnrageSpeedMultiplier = 1.20f;

        /// <summary>Hệ số giảm thời gian hồi chiêu khi Cuồng nộ (tấn công nhanh hơn 25%).</summary>
        public const float DefaultEnrageCooldownMultiplier = 0.75f;

        /// <summary>
        /// Trạng thái thanh máu nhiều lớp của Boss.
        /// </summary>
        public readonly struct BarState
        {
            /// <summary>Số thanh máu còn lại (vd: 4, 3, 2, 1, 0).</summary>
            public readonly int RemainingBars;

            /// <summary>Tỉ lệ đầy của thanh hiện tại trong khoảng [0, 1].</summary>
            public readonly float CurrentBarFill;

            /// <summary>Tổng số thanh máu ban đầu.</summary>
            public readonly int TotalBars;

            /// <summary>Boss đang ở thanh máu cuối cùng (thanh đỏ) hay không.</summary>
            public readonly bool IsFinalBar;

            public BarState(int remainingBars, float currentBarFill, int totalBars, bool isFinalBar)
            {
                RemainingBars = remainingBars;
                CurrentBarFill = currentBarFill;
                TotalBars = totalBars;
                IsFinalBar = isFinalBar;
            }
        }

        /// <summary>
        /// Tính toán số thanh máu còn lại và tỉ lệ fill của thanh hiện tại.
        /// Thuần logic toán học, dễ unit test.
        /// </summary>
        public static BarState Calculate(double currentHealth, double maxHealth, int totalBars = DefaultBarCount)
        {
            if (totalBars < 1) totalBars = 1;
            if (maxHealth <= 0.0 || currentHealth <= 0.0)
            {
                return new BarState(0, 0f, totalBars, isFinalBar: false);
            }

            if (currentHealth >= maxHealth)
            {
                return new BarState(totalBars, 1.0f, totalBars, isFinalBar: totalBars == 1);
            }

            double barSlice = maxHealth / totalBars;
            int fullBarsBelow = (int)Math.Floor(currentHealth / barSlice);
            double remainder = currentHealth - (fullBarsBelow * barSlice);

            int remainingBars;
            float fill;

            if (remainder <= 1e-9)
            {
                remainingBars = fullBarsBelow;
                fill = 1.0f;
            }
            else
            {
                remainingBars = fullBarsBelow + 1;
                fill = (float)(remainder / barSlice);
            }

            if (remainingBars > totalBars)
            {
                remainingBars = totalBars;
                fill = 1.0f;
            }

            bool isFinalBar = (remainingBars == 1);
            return new BarState(remainingBars, Mathf.Clamp01(fill), totalBars, isFinalBar);
        }

        /// <summary>
        /// Màu của thanh máu hiện tại theo chuẩn SAO:
        /// 4: Xanh lá (Green), 3: Vàng (Yellow), 2: Cam (Orange), 1: Đỏ (Red - Thanh cuối).
        /// </summary>
        public static Color GetBarColor(int remainingBars)
        {
            switch (remainingBars)
            {
                case 4:
                    return new Color(0.18f, 0.82f, 0.35f, 1f); // Xanh lục SAO
                case 3:
                    return new Color(0.96f, 0.82f, 0.18f, 1f); // Vàng SAO
                case 2:
                    return new Color(0.98f, 0.52f, 0.12f, 1f); // Cam SAO
                case 1:
                    return new Color(0.92f, 0.18f, 0.18f, 1f); // Đỏ rực thanh cuối
                default:
                    if (remainingBars > 4)
                        return new Color(0.2f, 0.65f, 0.95f, 1f); // Xanh dương cho boss có > 4 thanh
                    return new Color(0.92f, 0.18f, 0.18f, 1f);
            }
        }

        /// <summary>
        /// Màu của thanh bên dưới (lộ ra khi thanh trên cạn dần).
        /// </summary>
        public static Color GetUnderlyingBarColor(int remainingBars)
        {
            if (remainingBars <= 1)
            {
                return new Color(0.12f, 0.12f, 0.14f, 0.85f); // Khung nền tối khi ở thanh cuối
            }
            return GetBarColor(remainingBars - 1);
        }
    }
}
