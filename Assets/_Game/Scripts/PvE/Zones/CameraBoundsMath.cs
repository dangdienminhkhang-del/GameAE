using UnityEngine;

namespace Game.PvE.Zones
{
    /// <summary>
    /// Toán kẹp camera vào bounds của vùng (thuần logic, test được).
    /// Vùng HẸP hơn khung nhìn: trục đó căn giữa vùng (thay vì Mathf.Clamp với min &gt; max làm camera nhảy lung tung).
    /// </summary>
    public static class CameraBoundsMath
    {
        /// <summary>
        /// Kẹp tâm camera theo một trục. <paramref name="half"/> là nửa kích thước khung nhìn trên trục đó.
        /// Giá trị NaN/vô cực: giữ nguyên <paramref name="value"/> (hoặc 0 nếu chính value hỏng).
        /// </summary>
        public static float ClampAxis(float value, float min, float max, float half)
        {
            if (IsBad(value)) return 0f;
            if (IsBad(min) || IsBad(max) || IsBad(half)) return value;

            float lo = min + half;
            float hi = max - half;
            if (lo > hi) return (min + max) * 0.5f;
            return value < lo ? lo : (value > hi ? hi : value);
        }

        public static Vector2 Clamp(Vector2 position, Vector2 min, Vector2 max, float halfWidth, float halfHeight)
        {
            return new Vector2(
                ClampAxis(position.x, min.x, max.x, halfWidth),
                ClampAxis(position.y, min.y, max.y, halfHeight));
        }

        /// <summary>Nội suy hai bounds (t đã kẹp 0..1).</summary>
        public static void Lerp(Vector2 fromMin, Vector2 fromMax, Vector2 toMin, Vector2 toMax, float t,
            out Vector2 min, out Vector2 max)
        {
            t = Mathf.Clamp01(t);
            min = Vector2.LerpUnclamped(fromMin, toMin, t);
            max = Vector2.LerpUnclamped(fromMax, toMax, t);
        }

        public static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Hai bounds có giao hoặc chạm nhau không.</summary>
        public static bool Overlaps(Vector2 minA, Vector2 maxA, Vector2 minB, Vector2 maxB)
        {
            return minA.x <= maxB.x && maxA.x >= minB.x && minA.y <= maxB.y && maxA.y >= minB.y;
        }

        /// <summary>
        /// Có nên đặt camera NGAY (không chuyển mượt) khi sang bounds mới không.
        /// Đúng khi chưa có bounds trước, hoặc hai bounds rời nhau (người chơi vừa bị dịch chuyển sang tầng khác):
        /// nội suy qua khoảng trống giữa hai tầng sẽ làm camera quét qua cả thế giới.
        /// </summary>
        public static bool ShouldSnap(bool hasPrevious, Vector2 prevMin, Vector2 prevMax, Vector2 newMin, Vector2 newMax)
        {
            return !hasPrevious || !Overlaps(prevMin, prevMax, newMin, newMax);
        }

        private static bool IsBad(float v) => float.IsNaN(v) || float.IsInfinity(v);
    }
}
