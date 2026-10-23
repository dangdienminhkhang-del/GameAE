using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Facing
{
    /// <summary>Một ứng viên cho tự nhắm: vị trí và còn dùng được hay không (còn sống, không null).</summary>
    public readonly struct AimCandidate
    {
        public readonly Vector2 Position;
        public readonly bool IsValid;

        public AimCandidate(Vector2 position, bool isValid)
        {
            Position = position;
            IsValid = isValid;
        }
    }

    /// <summary>
    /// Luật thuần của tự nhắm (docs/13 Wave 9.5a): chọn mục tiêu còn dùng được GẦN NHẤT trong bán kính.
    /// Hai mục tiêu cùng khoảng cách thì chọn mục tiêu đứng TRƯỚC trong danh sách (thứ tự ổn định).
    /// </summary>
    public static class AimAssist
    {
        /// <summary>Trả chỉ số ứng viên được chọn, hoặc -1 nếu không có ai trong bán kính.</summary>
        public static int PickNearest(Vector2 origin, IReadOnlyList<AimCandidate> candidates, float radius)
        {
            if (candidates == null) return -1;
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f) return -1;

            float radiusSqr = radius * radius;
            int best = -1;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                AimCandidate c = candidates[i];
                if (!c.IsValid) continue;
                if (float.IsNaN(c.Position.x) || float.IsNaN(c.Position.y)) continue;
                if (float.IsInfinity(c.Position.x) || float.IsInfinity(c.Position.y)) continue;

                float sqr = (c.Position - origin).sqrMagnitude;
                if (sqr > radiusSqr) continue;
                if (sqr < bestSqr)
                {
                    best = i;
                    bestSqr = sqr;
                }
            }
            return best;
        }

        /// <summary>
        /// Hướng từ <paramref name="origin"/> tới <paramref name="target"/> (đã chuẩn hóa). Trùng điểm thì trả false
        /// (người gọi giữ hướng đang nhìn).
        /// </summary>
        public static bool TryDirection(Vector2 origin, Vector2 target, out Vector2 direction)
        {
            Vector2 delta = target - origin;
            if (!FacingMath.IsUsable(delta))
            {
                direction = Vector2.zero;
                return false;
            }
            direction = delta.normalized;
            return true;
        }
    }
}
