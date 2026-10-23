using UnityEngine;

namespace Game.PvE.Enemy.Behaviors
{
    /// <summary>
    /// Luật của quái bắn xa (thuần logic): khi nào vào/rời Attack (có trễ), khi nào được bắn, hướng bắn.
    /// Hướng bắn tính ĐÚNG LÚC bắn (cuối lấy đà) rồi đạn bay thẳng, không bám theo: đó là điều làm đạn né được.
    /// </summary>
    public static class RangedAttackRules
    {
        /// <summary>Đang Attack thì chỉ rời khi khoảng cách vượt shootRange × hệ số này (trễ ở biên tầm bắn).</summary>
        public const float DisengageRangeFactor = 1.15f;

        /// <summary>Đang Attack thì chỉ rời để lùi khi khoảng cách nhỏ hơn retreatRange × hệ số này (trễ ở biên lùi).</summary>
        public const float DisengageCloseFactor = 0.8f;

        /// <summary>Đạn xuất hiện cách tâm quái chừng này theo hướng bắn.</summary>
        public const float MuzzleOffset = 0.4f;

        /// <summary>Chase → Attack: trong tầm bắn, và không quá gần (trừ khi bị dồn góc, lúc đó vẫn đánh).</summary>
        public static bool ShouldEngage(float distance, float shootRange, float retreatRange, bool cornered)
        {
            return distance <= shootRange && (cornered || distance >= retreatRange);
        }

        /// <summary>Attack → Chase: ra hẳn khỏi tầm bắn, hoặc người chơi áp sát quá (và quái còn đường lùi).</summary>
        public static bool ShouldDisengage(float distance, float shootRange, float retreatRange, bool cornered)
        {
            if (distance > shootRange * DisengageRangeFactor) return true;
            return !cornered && distance < retreatRange * DisengageCloseFactor;
        }

        /// <summary>Được phép bắn lúc hết lấy đà: có mục tiêu và mục tiêu KHÔNG đứng trong Safe Zone.</summary>
        public static bool CanFire(bool hasTarget, bool targetInSafeZone)
        {
            return hasTarget && !targetInSafeZone;
        }

        /// <summary>
        /// Hướng bắn (vector đơn vị) từ <paramref name="self"/> tới <paramref name="target"/>.
        /// Hai điểm trùng nhau thì dùng <paramref name="fallback"/> đã chuẩn hóa (hoặc Vector2.right nếu fallback cũng zero); không NaN.
        /// </summary>
        public static Vector2 AimDirection(Vector2 self, Vector2 target, Vector2 fallback)
        {
            Vector2 diff = target - self;
            if (diff.sqrMagnitude >= 1e-6f) return diff.normalized;
            return fallback.sqrMagnitude >= 1e-6f ? fallback.normalized : Vector2.right;
        }

        public static Vector2 Muzzle(Vector2 self, Vector2 direction)
        {
            return self + direction * MuzzleOffset;
        }
    }
}
