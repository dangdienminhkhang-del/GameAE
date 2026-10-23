using System;

namespace Game.Core.Presentation
{
    /// <summary>Luật thanh máu: tỉ lệ và khi nào hiện/ẩn. Thuần logic.</summary>
    public static class HealthBarRules
    {
        /// <summary>Tỉ lệ máu 0..1. Max không hợp lệ hoặc NaN → 0.</summary>
        public static float Ratio(double current, double max)
        {
            if (!(max > 0.0)) return 0f;
            double ratio = current / max;
            if (double.IsNaN(ratio)) return 0f;
            return (float)Math.Min(1.0, Math.Max(0.0, ratio));
        }

        /// <summary>
        /// Thanh máu quái: chỉ hiện sau khi bị trúng, ẩn khi chết hoặc sau <paramref name="hideAfterSeconds"/> giây không bị đánh.
        /// </summary>
        public static bool ShouldShowEnemyBar(bool hitSinceSpawn, bool isDead, float secondsSinceLastHit, float hideAfterSeconds)
        {
            return hitSinceSpawn && !isDead && secondsSinceLastHit < hideAfterSeconds;
        }

        /// <summary>Thanh máu boss trên HUD: chỉ hiện khi boss đã vào trận và chưa chết.</summary>
        public static bool ShouldShowBossBar(bool bossEngaged, bool isDead)
        {
            return bossEngaged && !isDead;
        }
    }
}
