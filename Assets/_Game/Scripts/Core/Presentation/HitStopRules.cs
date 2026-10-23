using System;

namespace Game.Core.Presentation
{
    /// <summary>Các số dừng hình, lấy từ <see cref="CombatFeelConfig"/>. Struct thuần để test không cần ScriptableObject.</summary>
    public readonly struct HitStopSettings
    {
        public readonly float NormalSeconds;
        public readonly float FinisherSeconds;
        public readonly float BossSeconds;
        public readonly float MaxSeconds;
        public readonly float MinIntervalSeconds;
        public readonly float TimeScale;

        public HitStopSettings(float normalSeconds, float finisherSeconds, float bossSeconds,
            float maxSeconds, float minIntervalSeconds, float timeScale)
        {
            NormalSeconds = normalSeconds;
            FinisherSeconds = finisherSeconds;
            BossSeconds = bossSeconds;
            MaxSeconds = maxSeconds;
            MinIntervalSeconds = minIntervalSeconds;
            TimeScale = timeScale;
        }
    }

    /// <summary>Hoàn cảnh của một hit để quyết định có dừng hình không.</summary>
    public readonly struct HitStopContext
    {
        /// <summary>Hit do NGƯỜI CHƠI gây ra (đòn xác nhận). Quái đánh người chơi = false → không bao giờ dừng hình.</summary>
        public readonly bool IsPlayerHit;
        public readonly bool IsPaused;
        public readonly bool PlayerIsDead;
        /// <summary>Mục tiêu đã chết sau hit này (đòn kết liễu).</summary>
        public readonly bool TargetKilled;
        public readonly bool TargetIsBoss;
        /// <summary>Thời gian thật hiện tại (giây).</summary>
        public readonly double Now;
        /// <summary>Lúc bắt đầu lần dừng hình trước; <see cref="HitStopRules.NeverStopped"/> nếu chưa có.</summary>
        public readonly double LastHitStopAt;

        public HitStopContext(bool isPlayerHit, bool isPaused, bool playerIsDead, bool targetKilled,
            bool targetIsBoss, double now, double lastHitStopAt)
        {
            IsPlayerHit = isPlayerHit;
            IsPaused = isPaused;
            PlayerIsDead = playerIsDead;
            TargetKilled = targetKilled;
            TargetIsBoss = targetIsBoss;
            Now = now;
            LastHitStopAt = lastHitStopAt;
        }
    }

    public readonly struct HitStopDecision
    {
        public readonly bool Apply;
        public readonly float Seconds;
        public readonly float TimeScale;

        public HitStopDecision(bool apply, float seconds, float timeScale)
        {
            Apply = apply;
            Seconds = seconds;
            TimeScale = timeScale;
        }

        public static HitStopDecision None => new HitStopDecision(false, 0f, 1f);
    }

    /// <summary>
    /// Luật dừng hình (docs/05 Phase 9.5): chỉ cho đòn của người chơi, có giới hạn tần suất,
    /// không dừng khi đang pause hoặc người chơi đã chết, độ dài kẹp về trần. Thuần logic, test được.
    /// </summary>
    public static class HitStopRules
    {
        public const double NeverStopped = double.NegativeInfinity;

        public static HitStopDecision Evaluate(in HitStopContext context, in HitStopSettings settings)
        {
            if (!context.IsPlayerHit) return HitStopDecision.None;
            if (context.IsPaused || context.PlayerIsDead) return HitStopDecision.None;
            if (context.Now - context.LastHitStopAt < settings.MinIntervalSeconds) return HitStopDecision.None;

            float seconds = context.TargetKilled
                ? settings.FinisherSeconds
                : (context.TargetIsBoss ? settings.BossSeconds : settings.NormalSeconds);

            seconds = Math.Min(seconds, settings.MaxSeconds);
            if (seconds <= 0f) return HitStopDecision.None;

            return new HitStopDecision(true, seconds, settings.TimeScale);
        }
    }
}
