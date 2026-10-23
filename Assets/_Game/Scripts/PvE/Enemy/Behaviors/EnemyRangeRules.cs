using UnityEngine;

namespace Game.PvE.Enemy.Behaviors
{
    /// <summary>
    /// Kẹp các khoảng cách vai trò của EnemyDefinition (thuần logic):
    /// retreatRange &lt; preferredRange ≤ shootRange ≤ detectionRange.
    /// </summary>
    public static class EnemyRangeRules
    {
        public const float MinRange = 0.3f;
        public const float MinGapRetreatToPreferred = 0.1f;

        public static void Clamp(float detectionRange, ref float retreatRange, ref float preferredRange, ref float shootRange)
        {
            float detection = float.IsNaN(detectionRange) ? MinRange : Mathf.Max(detectionRange, MinRange);

            shootRange = float.IsNaN(shootRange) ? detection : Mathf.Clamp(shootRange, MinRange, detection);
            preferredRange = float.IsNaN(preferredRange) ? shootRange : Mathf.Clamp(preferredRange, MinRange * 0.5f, shootRange);

            float maxRetreat = Mathf.Max(0f, preferredRange - MinGapRetreatToPreferred);
            retreatRange = float.IsNaN(retreatRange) ? 0f : Mathf.Clamp(retreatRange, 0f, maxRetreat);
        }
    }
}
