using UnityEngine;

namespace Game.PvE.Zones
{
    /// <summary>
    /// Luật "dây xích" của quái với Safe Zone (docs/02 §12, Phase 9.7a), thuần logic:
    /// mục tiêu đang trong Safe Zone thì KHÔNG hợp lệ (không bắt đầu đuổi, đang đuổi thì bỏ), quái quay về vị trí gốc.
    /// Chỉ áp dụng khi người chơi ở trong Safe Zone; mọi hành vi khác của quái giữ nguyên.
    /// </summary>
    public static class EnemyLeashRules
    {
        /// <summary>Kiểm Safe Zone theo chu kỳ này (không mỗi frame).</summary>
        public const float SafeZoneCheckIntervalSeconds = 0.2f;

        /// <summary>Tới gần vị trí gốc trong bán kính này thì coi là đã về.</summary>
        public const float HomeArriveRadius = 0.3f;

        public enum IdleAction
        {
            Stay = 0,
            ReturnHome = 1,
            Chase = 2
        }

        public static bool ShouldDropTarget(bool targetInSafeZone) => targetInSafeZone;

        /// <summary>Bắt đầu (hoặc tiếp tục) đuổi: có mục tiêu, mục tiêu ngoài Safe Zone và trong tầm phát hiện.</summary>
        public static bool ShouldChase(bool hasTarget, bool targetInSafeZone, float distanceToTarget, float detectionRange)
        {
            return hasTarget && !targetInSafeZone && distanceToTarget <= detectionRange;
        }

        public static bool HasArrivedHome(Vector2 position, Vector2 home, float radius = HomeArriveRadius)
        {
            return (position - home).sqrMagnitude <= radius * radius;
        }

        /// <summary>Quyết định của trạng thái Idle: đuổi, quay về, hoặc đứng yên.</summary>
        public static IdleAction DecideIdle(bool hasTarget, bool targetInSafeZone, float distanceToTarget,
            float detectionRange, bool returningHome, Vector2 position, Vector2 home)
        {
            if (ShouldChase(hasTarget, targetInSafeZone, distanceToTarget, detectionRange)) return IdleAction.Chase;
            if (returningHome && !HasArrivedHome(position, home)) return IdleAction.ReturnHome;
            return IdleAction.Stay;
        }
    }

    /// <summary>Đếm giờ để chỉ kiểm Safe Zone mỗi <see cref="EnemyLeashRules.SafeZoneCheckIntervalSeconds"/> giây.</summary>
    public sealed class SafeZoneCheckThrottle
    {
        private readonly float _interval;
        private float _remaining;

        public SafeZoneCheckThrottle(float intervalSeconds = EnemyLeashRules.SafeZoneCheckIntervalSeconds)
        {
            _interval = intervalSeconds < 0f ? 0f : intervalSeconds;
        }

        /// <summary>True khi đến lúc kiểm (lần gọi đầu tiên luôn true).</summary>
        public bool Tick(float deltaSeconds)
        {
            _remaining -= deltaSeconds < 0f ? 0f : deltaSeconds;
            if (_remaining > 0f) return false;
            _remaining = _interval;
            return true;
        }

        public void Reset() => _remaining = 0f;
    }
}
