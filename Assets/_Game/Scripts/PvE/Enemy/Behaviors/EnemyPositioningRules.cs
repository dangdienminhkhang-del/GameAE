using System;
using UnityEngine;

namespace Game.PvE.Enemy.Behaviors
{
    /// <summary>
    /// Luật di chuyển giữ khoảng cách (thuần logic, chỉ dùng Vector2/Mathf). Không NaN khi khoảng cách = 0.
    /// </summary>
    public static class EnemyPositioningRules
    {
        /// <summary>Quãng nhìn trước khi kiểm hướng lùi có bị chặn không (đơn vị Unity).</summary>
        public const float RetreatLookAhead = 0.6f;

        private const float Epsilon = 1e-6f;

        public enum RangeBand
        {
            TooClose = 0,
            InBand = 1,
            TooFar = 2
        }

        /// <summary>Gần hơn retreatRange = quá gần; xa hơn preferredRange = quá xa; còn lại = trong dải.</summary>
        public static RangeBand Classify(float distance, float retreatRange, float preferredRange)
        {
            if (distance < retreatRange) return RangeBand.TooClose;
            if (distance > preferredRange) return RangeBand.TooFar;
            return RangeBand.InBand;
        }

        /// <summary>Hướng đơn vị từ mục tiêu ra xa. Hai điểm trùng nhau thì chọn Vector2.right (ổn định, không NaN).</summary>
        public static Vector2 Away(Vector2 self, Vector2 target)
        {
            Vector2 diff = self - target;
            return diff.sqrMagnitude < Epsilon ? Vector2.right : diff.normalized;
        }

        /// <summary>Hướng đơn vị tới mục tiêu. Hai điểm trùng nhau thì trả zero (khớp code cũ MoveToward).</summary>
        public static Vector2 Toward(Vector2 self, Vector2 target)
        {
            Vector2 diff = target - self;
            return diff.sqrMagnitude < Epsilon ? Vector2.zero : diff.normalized;
        }

        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>
        /// Hướng lùi không chui vào vùng bị chặn (Safe Zone, tường...). Thử hướng thẳng ra xa, rồi lệch ±45°, ±90°.
        /// <paramref name="sidestepSign"/> khác 0 (đang bị kẹt) thì thử né sang bên trước.
        /// Trả zero nếu mọi hướng đều bị chặn (bị dồn vào góc).
        /// </summary>
        public static Vector2 RetreatDirection(Vector2 self, Vector2 target, Func<Vector2, bool> isBlockedPoint,
            float lookAhead = RetreatLookAhead, int sidestepSign = 0)
        {
            Vector2 away = Away(self, target);
            if (isBlockedPoint == null) return away;

            int sign = sidestepSign < 0 ? -1 : 1;

            if (sidestepSign != 0)
            {
                if (TryDirection(self, Rotate(away, 90f * sign), isBlockedPoint, lookAhead, out Vector2 d)) return d;
                if (TryDirection(self, Rotate(away, 45f * sign), isBlockedPoint, lookAhead, out d)) return d;
            }

            if (TryDirection(self, away, isBlockedPoint, lookAhead, out Vector2 straight)) return straight;
            if (TryDirection(self, Rotate(away, 45f * sign), isBlockedPoint, lookAhead, out Vector2 e)) return e;
            if (TryDirection(self, Rotate(away, -45f * sign), isBlockedPoint, lookAhead, out e)) return e;
            if (TryDirection(self, Rotate(away, 90f * sign), isBlockedPoint, lookAhead, out e)) return e;
            if (TryDirection(self, Rotate(away, -90f * sign), isBlockedPoint, lookAhead, out e)) return e;
            return Vector2.zero;
        }

        private static bool TryDirection(Vector2 self, Vector2 direction, Func<Vector2, bool> isBlockedPoint,
            float lookAhead, out Vector2 result)
        {
            result = direction;
            return !isBlockedPoint(self + direction * lookAhead);
        }

        /// <summary>
        /// Hướng đi của quái giữ khoảng cách: quá gần thì lùi (trừ khi bị dồn góc), quá xa thì áp sát, trong dải thì đứng yên.
        /// </summary>
        public static Vector2 RangedMove(Vector2 self, Vector2 target, float retreatRange, float preferredRange,
            Func<Vector2, bool> isBlockedPoint, bool cornered, int sidestepSign = 0)
        {
            float distance = Vector2.Distance(self, target);
            switch (Classify(distance, retreatRange, preferredRange))
            {
                case RangeBand.TooClose:
                    return cornered ? Vector2.zero : RetreatDirection(self, target, isBlockedPoint, RetreatLookAhead, sidestepSign);
                case RangeBand.TooFar:
                    return Toward(self, target);
                default:
                    return Vector2.zero;
            }
        }
    }

    /// <summary>
    /// Nhận ra "bị kẹt" khi cố di chuyển mà không tiến được (tường, góc). Kẹt thì giữ cờ trong <see cref="HoldSeconds"/>
    /// để quái không nhảy qua lại giữa lùi và đánh. Thuần logic.
    /// </summary>
    public sealed class StuckDetector
    {
        public const float DefaultWindowSeconds = 0.4f;
        public const float DefaultMinDisplacement = 0.1f;
        public const float DefaultHoldSeconds = 1.5f;

        private readonly float _window;
        private readonly float _minDisplacement;
        private readonly float _hold;

        private float _windowElapsed;
        private Vector2 _windowStart;
        private bool _hasStart;
        private float _holdRemaining;

        public StuckDetector(float windowSeconds = DefaultWindowSeconds, float minDisplacement = DefaultMinDisplacement,
            float holdSeconds = DefaultHoldSeconds)
        {
            _window = Mathf.Max(0.05f, windowSeconds);
            _minDisplacement = Mathf.Max(0f, minDisplacement);
            _hold = Mathf.Max(0f, holdSeconds);
        }

        /// <summary>Đang bị kẹt (cờ giữ vài giây sau khi phát hiện).</summary>
        public bool IsStuck => _holdRemaining > 0f;

        public void Reset()
        {
            _windowElapsed = 0f;
            _hasStart = false;
            _holdRemaining = 0f;
        }

        /// <summary>Đánh dấu kẹt ngay (mọi hướng lùi đều bị chặn).</summary>
        public void ForceStuck()
        {
            _holdRemaining = _hold;
        }

        /// <param name="trying">Quái đang CỐ di chuyển (ví dụ đang lùi). Không cố thì chỉ đếm lùi cờ giữ.</param>
        public void Tick(float deltaTime, Vector2 position, bool trying)
        {
            float dt = deltaTime < 0f ? 0f : deltaTime;
            if (_holdRemaining > 0f) _holdRemaining = Mathf.Max(0f, _holdRemaining - dt);

            if (!trying)
            {
                _windowElapsed = 0f;
                _hasStart = false;
                return;
            }

            if (!_hasStart)
            {
                _windowStart = position;
                _windowElapsed = 0f;
                _hasStart = true;
                return;
            }

            _windowElapsed += dt;
            if (_windowElapsed < _window) return;

            if ((position - _windowStart).magnitude < _minDisplacement) _holdRemaining = _hold;
            _windowStart = position;
            _windowElapsed = 0f;
        }
    }
}
