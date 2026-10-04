using UnityEngine;

namespace Game.Core.Combat
{
    public static class DamageCalculator
    {
        public static double Calculate(double attackerAtk, double defenderDef, double skillMultiplier = 1.0, bool isCritical = false, double critMultiplier = 1.5)
        {
            double raw = attackerAtk * skillMultiplier - defenderDef;
            double baseDamage = UnityEngine.Mathf.Max(1, (float)raw);
            if (isCritical)
            {
                baseDamage *= critMultiplier;
            }
            return baseDamage;
        }
    }
}
