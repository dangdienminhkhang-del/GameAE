using System.Collections.Generic;

namespace Game.Core.Combat
{
    /// <summary>
    /// Ghép (attacker stats + AttackDefinition + target) thành DamageInfo rồi áp lên IDamageable.
    /// Thuần logic, không biết PvE/PvP — test được không cần scene.
    /// </summary>
    public static class AttackExecutor
    {
        /// <summary>criticalRoll là [0,1) — truyền vào để test tất định, gameplay truyền Random.value.</summary>
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
            double amount = DamageCalculator.Calculate(attackValue, defenderDefense, multiplier, isCritical, criticalDamage);

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

        /// <summary>Mục tiêu không có chỉ số (ví dụ bia) coi như DEF = 0.</summary>
        public static double ResolveDefense(IDamageable target)
        {
            return target is ICombatStats stats ? stats.Defense : 0.0;
        }

        /// <summary>Áp cùng một DamageInfo lên nhiều mục tiêu (đòn diện rộng). Trả về số mục tiêu trúng.</summary>
        public static int ApplyDamage(IReadOnlyList<IDamageable> targets, DamageInfo damage)
        {
            if (targets == null || damage == null) return 0;

            int hits = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                IDamageable target = targets[i];
                if (target == null || target.IsDead) continue;
                target.TakeDamage(damage);
                hits++;
            }
            return hits;
        }

        private static double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }
    }
}
