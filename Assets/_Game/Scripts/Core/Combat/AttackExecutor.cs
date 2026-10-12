using System.Collections.Generic;
using Game.Core.Events;

namespace Game.Core.Combat
{
    public static class AttackExecutor
    {
        public static DamageInfo ResolveDamage(
            ICombatStats attacker,
            double defenderDefense,
            AttackDefinition definition,
            double criticalRoll,
            string sourceId = null)
        {
            double multiplier = definition != null ? definition.DamageMultiplier : 1.0;
            double criticalChance = ResolveCriticalChance(attacker, definition);
            double criticalDamage = ResolveCriticalDamage(attacker, definition);
            bool isCritical = criticalRoll < criticalChance;

            double attackValue = attacker != null ? attacker.Attack : 0.0;
            double amount = DamageCalculator.Calculate(attackValue, defenderDefense, multiplier, isCritical, criticalDamage, CombatBalance.DefenseWeight);

            return new DamageInfo(amount)
            {
                IsCritical = isCritical,
                CriticalMultiplier = criticalDamage,
                SourceId = sourceId,
                Source = attacker
            };
        }

        public static double ResolveCriticalChance(ICombatStats attacker, AttackDefinition definition)
        {
            double chance = definition != null && definition.CriticalChanceOverride >= 0.0
                ? definition.CriticalChanceOverride
                : (attacker != null ? attacker.CriticalChance : 0.0);
            return Clamp01(chance);
        }

        public static double ResolveCriticalDamage(ICombatStats attacker, AttackDefinition definition)
        {
            if (definition != null && definition.CriticalDamageOverride >= 0.0)
            {
                return definition.CriticalDamageOverride;
            }
            return attacker != null ? attacker.CriticalDamage : 1.0;
        }

        public static double ResolveDefense(IDamageable target)
        {
            return target is ICombatStats stats ? stats.Defense : 0.0;
        }

        public static int ApplyDamage(IReadOnlyList<IDamageable> targets, DamageInfo damage)
        {
            if (targets == null || damage == null) return 0;

            int hits = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                IDamageable target = targets[i];
                if (target == null || target.IsDead) continue;
                ApplyHit(target, damage);
                hits++;
            }
            return hits;
        }

        /// <summary>
        /// Áp 1 hit lên 1 mục tiêu rồi phát GameEvents.OnHitResolved đúng một lần.
        /// Chỉ phát khi mục tiêu còn sống trước hit VÀ damage thật sự áp (máu giảm hoặc chết):
        /// mục tiêu đã chết, hoặc từ chối damage (Safe Zone...) thì không phát. Trả true nếu đã phát.
        /// </summary>
        public static bool ApplyHit(IDamageable target, DamageInfo damage)
        {
            if (target == null || damage == null || target.IsDead) return false;

            double before = target.CurrentHealth;
            target.TakeDamage(damage);
            if (!target.IsDead && target.CurrentHealth >= before) return false;

            GameEvents.RaiseHitResolved(damage.Source, new HitResolvedArgs
            {
                Source = damage.Source,
                Target = target,
                Damage = damage
            });
            return true;
        }

        private static double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }
    }
}
