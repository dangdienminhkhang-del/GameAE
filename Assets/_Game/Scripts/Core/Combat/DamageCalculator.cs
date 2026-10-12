using System;
using UnityEngine;

namespace Game.Core.Combat
{
    public static class DamageCalculator
    {
        /// <summary>
        /// Tính damage sau khi trừ defense. Docs/07 §4.2.
        /// defenseWeight mặc định 1.0 để test cũ không vỡ; PvE dùng 0.3 qua CombatBalance.
        /// </summary>
        public static double Calculate(double attackerAtk, double defenderDef, double skillMultiplier = 1.0, bool isCritical = false, double critMultiplier = 1.5, double defenseWeight = 1.0)
        {
            double rawDamage = attackerAtk * skillMultiplier - defenderDef * defenseWeight;
            double baseDamage = Math.Max(1.0, Math.Round(rawDamage, MidpointRounding.AwayFromZero));
            if (isCritical)
            {
                baseDamage *= critMultiplier;
            }
            return baseDamage;
        }
    }
}
