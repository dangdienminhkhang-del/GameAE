using System;
using Game.Core.Combat;
using Game.Core.Stats;

namespace Game.PvE.Boss
{
    /// <summary>
    /// Mô hình ước lượng cân bằng Boss (docs/07 §5, §9). Thuần số học: nhận BossDefinition, defenseWeight,
    /// CharacterStats và AttackDefinition thật, KHÔNG đọc scene/Time. Chỉ là ƯỚC LƯỢNG để định số ban đầu;
    /// bắt buộc thử lại trên điện thoại (docs/07 §9, Device PvE).
    ///
    /// Giả định (đều ghi trong docs/07):
    /// - Người chơi chỉ đánh trong Punish Window (sau đòn thường) và Recovery (sau lao); trong Observe/Telegraph/lao
    ///   người chơi đang tiếp cận hoặc né nên không gây damage.
    /// - Mỗi window: vung đầu tiên ngay khi window mở, rồi mỗi <c>cooldown</c> một lần.
    /// - Chỉ <see cref="PlayerHitEfficiency"/> số đòn trúng boss (né, lệch hướng, thao tác trên cảm ứng).
    /// - Mỗi chu kỳ mất <see cref="ApproachSecondsPerCycle"/> để tiếp cận/đổi vị trí (Observe), nhưng không ngắn hơn
    ///   phần cooldown còn lại sau Punish (boss chưa được phép đánh).
    /// - Đòn người chơi không chí mạng (giống BalanceSandboxTests); Enrage không được mô hình hóa
    ///   (cooldown ×0.7, tốc độ ×1.25 chỉ rút ngắn Observe nhẹ ở 35% HP cuối, nên ước lượng hơi dài, thiên về an toàn).
    /// - Chu kỳ lặp: <c>pressureEveryNAttacks</c> đòn thường rồi một Pressure.
    /// </summary>
    public static class BossBalanceModel
    {
        /// <summary>Tỉ lệ đòn người chơi thực sự trúng boss trong các window.</summary>
        public const double PlayerHitEfficiency = 0.7;

        /// <summary>Thời gian tiếp cận/đổi vị trí mỗi chu kỳ (giây), tính trước Telegraph.</summary>
        public const double ApproachSecondsPerCycle = 1.5;

        // Sai số số thực khi chia float (vd. 1.35f / 0.45f = 2.9999999) để không mất một đòn oan.
        private const double WindowEpsilon = 1e-6;

        public readonly struct Result
        {
            public readonly int PlayerLevel;
            public readonly double PlayerMaxHealth;
            public readonly double PlayerHitDamage;          // đòn thường người chơi lên boss
            public readonly double BossNormalHitDamage;      // đòn thường boss lên người chơi
            public readonly double BossPressureHitDamage;    // đòn lao boss lên người chơi
            public readonly int NormalHitsToKillPlayer;      // người chơi đứng yên
            public readonly int PressureHitsToKillPlayer;
            public readonly int HitsPerNormalWindow;
            public readonly int HitsPerPressureWindow;
            public readonly double NormalCycleSeconds;
            public readonly double PressureCycleSeconds;
            public readonly double SuperCycleSeconds;        // N đòn thường + 1 Pressure
            public readonly int HitsPerSuperCycle;
            public readonly double ExpectedDamagePerSuperCycle;
            public readonly double TimeToKillSeconds;

            public Result(
                int playerLevel, double playerMaxHealth, double playerHitDamage,
                double bossNormalHitDamage, double bossPressureHitDamage,
                int normalHitsToKillPlayer, int pressureHitsToKillPlayer,
                int hitsPerNormalWindow, int hitsPerPressureWindow,
                double normalCycleSeconds, double pressureCycleSeconds, double superCycleSeconds,
                int hitsPerSuperCycle, double expectedDamagePerSuperCycle, double timeToKillSeconds)
            {
                PlayerLevel = playerLevel;
                PlayerMaxHealth = playerMaxHealth;
                PlayerHitDamage = playerHitDamage;
                BossNormalHitDamage = bossNormalHitDamage;
                BossPressureHitDamage = bossPressureHitDamage;
                NormalHitsToKillPlayer = normalHitsToKillPlayer;
                PressureHitsToKillPlayer = pressureHitsToKillPlayer;
                HitsPerNormalWindow = hitsPerNormalWindow;
                HitsPerPressureWindow = hitsPerPressureWindow;
                NormalCycleSeconds = normalCycleSeconds;
                PressureCycleSeconds = pressureCycleSeconds;
                SuperCycleSeconds = superCycleSeconds;
                HitsPerSuperCycle = hitsPerSuperCycle;
                ExpectedDamagePerSuperCycle = expectedDamagePerSuperCycle;
                TimeToKillSeconds = timeToKillSeconds;
            }
        }

        /// <param name="defenseWeight">Lấy từ CombatBalanceConfig thật (Mốc 1: 0.3).</param>
        public static Result Evaluate(BossDefinition boss, double defenseWeight, CharacterStats player, AttackDefinition basicAttack)
        {
            if (boss == null) throw new ArgumentNullException(nameof(boss));
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (basicAttack == null) throw new ArgumentNullException(nameof(basicAttack));

            double cooldown = basicAttack.CooldownSeconds;

            // Damage: đúng công thức chung của game (DamageCalculator), không chí mạng.
            double playerHit = DamageCalculator.Calculate(
                player.Attack, boss.Defense, basicAttack.DamageMultiplier, false, player.CriticalDamage, defenseWeight);
            double bossNormalHit = DamageCalculator.Calculate(
                boss.Attack, player.Defense, 1.0, false, 1.0, defenseWeight);
            double bossPressureHit = BossHitRules.ResolvePressureDamage(bossNormalHit, boss.PressureDamageMultiplier);

            // Độ dài chu kỳ lấy từ các thời lượng trong definition.
            double observe = ObserveSeconds(boss);
            double normalWindow = boss.PunishWindowSeconds;
            double pressureWindow = boss.PressureRecoverySeconds;
            double normalCycle = observe + boss.AttackWindupSeconds + normalWindow;
            double pressureCycle = observe + boss.PressureWindupSeconds + boss.PressureDashSeconds + pressureWindow;

            int normalCount = boss.PressureEveryNAttacks;
            int hitsNormal = HitsPerWindow(normalWindow, cooldown);
            int hitsPressure = HitsPerWindow(pressureWindow, cooldown);

            double superCycle = normalCount * normalCycle + pressureCycle;
            int hitsPerSuperCycle = normalCount * hitsNormal + hitsPressure;
            double expectedDamage = hitsPerSuperCycle * playerHit * PlayerHitEfficiency;
            double timeToKill = expectedDamage > 0.0 ? boss.MaxHealth / expectedDamage * superCycle : double.PositiveInfinity;

            return new Result(
                player.Level, player.MaxHealth, playerHit,
                bossNormalHit, bossPressureHit,
                (int)Math.Ceiling(player.MaxHealth / bossNormalHit),
                (int)Math.Ceiling(player.MaxHealth / bossPressureHit),
                hitsNormal, hitsPressure,
                normalCycle, pressureCycle, superCycle,
                hitsPerSuperCycle, expectedDamage, timeToKill);
        }

        /// <summary>
        /// Số đòn người chơi vung được trong một window: vung đầu ngay khi window mở, rồi mỗi
        /// <paramref name="cooldownSeconds"/> một lần. Window hoặc cooldown không dương thì 0.
        /// </summary>
        public static int HitsPerWindow(double windowSeconds, double cooldownSeconds)
        {
            if (windowSeconds <= 0.0 || cooldownSeconds <= 0.0) return 0;
            return 1 + (int)Math.Floor(windowSeconds / cooldownSeconds + WindowEpsilon);
        }

        /// <summary>
        /// Thời gian Observe mỗi chu kỳ: tiếp cận <see cref="ApproachSecondsPerCycle"/>, nhưng boss cũng chưa
        /// được đánh khi cooldown chưa hết (cooldown tính từ lúc đánh, trong đó đã gồm Punish Window).
        /// </summary>
        public static double ObserveSeconds(BossDefinition boss)
        {
            if (boss == null) throw new ArgumentNullException(nameof(boss));
            double cooldownLeftAfterPunish = Math.Max(0.0, boss.AttackCooldownSeconds - boss.PunishWindowSeconds);
            return Math.Max(ApproachSecondsPerCycle, cooldownLeftAfterPunish);
        }
    }
}
