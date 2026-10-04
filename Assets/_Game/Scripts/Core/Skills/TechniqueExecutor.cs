using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Items;

namespace Game.Core.Skills
{
    /// <summary>
    /// Executor dùng chung cho mọi Weapon Technique.
    /// KHÔNG viết script riêng cho từng technique. Mọi rule lấy từ WeaponTechniqueDefinition.
    /// Pure logic — test được không cần scene. (docs/02 §10, docs/12 §4.2)
    /// </summary>
    public static class TechniqueExecutor
    {
        /// <summary>
        /// Validate: Definition tồn tại, Discipline khớp vũ khí đang trang bị, mastery đủ, cooldown.
        /// Không validate CombatBusy (do TechniqueController quyết định runtime state).
        /// </summary>
        public static TechniqueValidationResult Validate(
            WeaponTechniqueDefinition definition,
            WeaponDiscipline equippedDiscipline,
            int currentMastery,
            bool isCooldownReady)
        {
            if (definition == null)
                return TechniqueValidationResult.Fail(TechniqueValidationError.DefinitionMissing, "Không tìm thấy Technique definition.");

            if (equippedDiscipline == WeaponDiscipline.None)
                return TechniqueValidationResult.Fail(TechniqueValidationError.NoWeaponEquipped, "Chưa trang bị vũ khí.");

            if (definition.WeaponDiscipline != equippedDiscipline)
                return TechniqueValidationResult.Fail(TechniqueValidationError.WrongDiscipline, $"Technique thuộc {definition.WeaponDiscipline}, vũ khí đang {equippedDiscipline}.");

            if (currentMastery < definition.MasteryRequired)
                return TechniqueValidationResult.Fail(TechniqueValidationError.LockedMastery, $"Cần Mastery {definition.MasteryRequired} ({definition.WeaponDiscipline}), hiện tại {currentMastery}.");

            if (!isCooldownReady)
                return TechniqueValidationResult.Fail(TechniqueValidationError.Cooldown, "Technique đang hồi chiêu.");

            return TechniqueValidationResult.Ok();
        }

        /// <summary>
        /// Tính tổng damage (đa hit) cho một mục tiêu.
        /// Để test duplicate hit prevention: mỗi target trong cùng một activation chỉ nhận 1 hit mỗi hit window.
        /// Trả về list DamageInfo (mỗi element là damage của 1 window) → caller xử lý.
        /// </summary>
        public static List<DamageInfo> CalculateHitsForTarget(
            ICombatStats attacker,
            double defenderDefense,
            WeaponTechniqueDefinition definition,
            IReadOnlyList<double> criticalRolls,
            string sourceId = null)
        {
            var results = new List<DamageInfo>();
            if (attacker == null || definition == null || definition.ActiveWindows == null) return results;

            int windowCount = definition.ActiveWindows.Count;
            for (int w = 0; w < windowCount; w++)
            {
                TechniqueHitWindow window = definition.ActiveWindows[w];
                double critRoll = 0.0;
                if (criticalRolls != null && w < criticalRolls.Count) critRoll = criticalRolls[w];

                double criticalChance = ResolveCriticalChance(attacker, definition);
                double criticalDamage = ResolveCriticalDamage(attacker, definition);
                bool isCrit = critRoll < criticalChance;

                double finalDamage = DamageCalculator.Calculate(
                    attacker.Attack,
                    defenderDefense,
                    window.hitMultiplier,
                    isCrit,
                    criticalDamage);

                results.Add(new DamageInfo(finalDamage)
                {
                    IsCritical = isCrit,
                    CriticalMultiplier = criticalDamage,
                    SourceId = sourceId,
                    Source = attacker
                });
            }
            return results;
        }

        /// <summary>Tổng damage dự kiến (tổng tất cả hit) — dùng cho unit test và HUD dự đoán.</summary>
        public static double CalculateTotalMultiplier(WeaponTechniqueDefinition definition)
        {
            if (definition == null || definition.ActiveWindows == null) return 0.0;
            double sum = 0.0;
            for (int w = 0; w < definition.ActiveWindows.Count; w++)
            {
                sum += definition.ActiveWindows[w].hitMultiplier;
            }
            return sum;
        }

        private static double ResolveCriticalChance(ICombatStats attacker, WeaponTechniqueDefinition def)
        {
            double chance = def != null && def.CriticalChanceOverride >= 0.0
                ? def.CriticalChanceOverride
                : (attacker != null ? attacker.CriticalChance : 0.0);
            if (chance < 0.0) return 0.0;
            if (chance > 1.0) return 1.0;
            return chance;
        }

        private static double ResolveCriticalDamage(ICombatStats attacker, WeaponTechniqueDefinition def)
        {
            if (def != null && def.CriticalDamageOverride >= 0.0)
                return def.CriticalDamageOverride;
            return attacker != null ? attacker.CriticalDamage : 1.0;
        }

        /// <summary>
        /// Credit nhận được cho 1 hit đã xác nhận (docs/07 §3).
        /// Basic hit: 1; Technique hit: 2.
        /// Caller (TechniqueController) phải giữ creditPerEnemy cap 4.
        /// </summary>
        public static int GetCreditPerConfirmedHit(bool isTechniqueHit)
        {
            return isTechniqueHit ? 2 : 1;
        }
    }
}
