using System;
using UnityEngine;

namespace Game.PvE.Boss
{
    /// <summary>
    /// Luật trúng đòn của Boss (docs/12 §4, docs/07 §5). Thuần hình học + số học: không đọc scene,
    /// không đọc Time, không có trạng thái nên test được trong EditMode.
    /// <see cref="BossController"/> chỉ gọi các hàm này tại THỜI ĐIỂM đánh để telegraph có nghĩa:
    /// người chơi lùi ra ngoài vùng hoặc lệch khỏi hành lang lao trước lúc đó thì trượt.
    /// </summary>
    public static class BossHitRules
    {
        /// <summary>Mục tiêu có nằm trong vòng tròn tâm boss, bán kính <paramref name="radius"/> (biên tính là trúng).</summary>
        public static bool IsInsideHitArea(Vector2 bossPosition, Vector2 targetPosition, float radius)
        {
            if (radius < 0f) return false;
            return (targetPosition - bossPosition).sqrMagnitude <= radius * radius;
        }

        /// <summary>
        /// Mục tiêu có nằm trong hành lang lao: khoảng cách từ mục tiêu tới đoạn
        /// <paramref name="dashStart"/> → <paramref name="dashEnd"/> không vượt <paramref name="radius"/>.
        /// Đoạn suy biến (đầu = cuối) coi như một điểm.
        /// </summary>
        public static bool IsInsideDashCorridor(Vector2 dashStart, Vector2 dashEnd, Vector2 targetPosition, float radius)
        {
            if (radius < 0f) return false;
            return DistanceToSegment(dashStart, dashEnd, targetPosition) <= radius;
        }

        /// <summary>Khoảng cách ngắn nhất từ <paramref name="point"/> tới đoạn thẳng start → end.</summary>
        public static float DistanceToSegment(Vector2 segmentStart, Vector2 segmentEnd, Vector2 point)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return Vector2.Distance(segmentStart, point);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / lengthSquared);
            Vector2 closest = segmentStart + segment * t;
            return Vector2.Distance(closest, point);
        }

        /// <summary>
        /// Hướng lao (vector đơn vị) từ <paramref name="from"/> tới <paramref name="to"/>.
        /// Hai điểm trùng nhau thì dùng <paramref name="fallback"/> (chuẩn hóa); fallback cũng rỗng thì lao sang phải.
        /// </summary>
        public static Vector2 ResolveDashDirection(Vector2 from, Vector2 to, Vector2 fallback)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude > Mathf.Epsilon) return direction.normalized;
            if (fallback.sqrMagnitude > Mathf.Epsilon) return fallback.normalized;
            return Vector2.right;
        }

        /// <summary>Điểm cuối của đường lao dài <paramref name="distance"/> theo <paramref name="direction"/> (đã chuẩn hóa).</summary>
        public static Vector2 ResolveDashEnd(Vector2 dashStart, Vector2 direction, float distance)
        {
            return dashStart + direction * Mathf.Max(0f, distance);
        }

        /// <summary>
        /// Damage của đòn Pressure = damage đòn thường (đã trừ giáp, theo công thức chung) × hệ số,
        /// làm tròn về số nguyên như <see cref="Game.Core.Combat.DamageCalculator"/> và không dưới 1.
        /// Dùng chung cho BossController và BossBalanceModel để hai bên không lệch nhau.
        /// </summary>
        public static double ResolvePressureDamage(double normalHitDamage, double multiplier)
        {
            // Hệ số thường là float (1.3f = 1.29999995...): làm tròn 4 chữ số thập phân trước để 25 × 1.3 = 32.4999988
            // không bị rơi xuống 32 thay vì 33.
            double product = Math.Round(normalHitDamage * multiplier, ProductNoiseDigits);
            double scaled = Math.Round(product, MidpointRounding.AwayFromZero);
            return Math.Max(1.0, scaled);
        }

        private const int ProductNoiseDigits = 4;
    }

    /// <summary>
    /// Cổng "mỗi lần lao chỉ gây damage một lần". BossController gọi <see cref="TryHit"/> mỗi frame
    /// trong lúc lao; chỉ lần đầu mục tiêu lọt vào hành lang đã quét mới trả true.
    /// </summary>
    public sealed class BossDashHitGate
    {
        public bool HasHit { get; private set; }

        /// <summary>Gọi khi bắt đầu một lần lao mới.</summary>
        public void Reset()
        {
            HasHit = false;
        }

        /// <summary>
        /// Hành lang là đoạn từ điểm xuất phát tới vị trí hiện tại của boss (phần đã quét).
        /// Trả true đúng một lần cho mỗi lần lao, khi mục tiêu nằm trong hành lang.
        /// </summary>
        public bool TryHit(Vector2 dashStart, Vector2 dashCurrent, Vector2 targetPosition, float radius)
        {
            if (HasHit) return false;
            if (!BossHitRules.IsInsideDashCorridor(dashStart, dashCurrent, targetPosition, radius)) return false;

            HasHit = true;
            return true;
        }
    }
}
