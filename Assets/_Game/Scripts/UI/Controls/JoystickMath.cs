using UnityEngine;

namespace Game.UI.Controls
{
    /// <summary>
    /// Tính đầu ra joystick (thuần logic, test được). Đây là NGUỒN DUY NHẤT của dead zone (P9.6 mục 3b):
    /// FacingTracker chỉ lọc nhiễu số trên vector ĐÃ qua đây (ngưỡng 0.02, nhỏ hơn mọi dead zone cho phép).
    /// </summary>
    public static class JoystickMath
    {
        public const float MaxDeadZone = 0.95f;

        /// <summary>
        /// <paramref name="delta"/>: vị trí kéo (đơn vị pixel) tính từ tâm. <paramref name="range"/>: bán kính kéo hết cỡ.
        /// <paramref name="sensitivity"/>: &gt;1 thì kéo ít hơn vẫn tới tối đa (range hiệu dụng = range / sensitivity).
        /// Dưới dead zone → (0,0); trên ngưỡng thì co giãn lại (len − dz) / (1 − dz) để không nhảy vọt; độ dài ≤ 1; hướng giữ nguyên.
        /// </summary>
        public static Vector2 Apply(Vector2 delta, float range, float deadZone, float sensitivity = 1f)
        {
            if (float.IsNaN(delta.x) || float.IsNaN(delta.y) || float.IsInfinity(delta.x) || float.IsInfinity(delta.y)) return Vector2.zero;
            if (float.IsNaN(range) || range <= 0f) return Vector2.zero;
            if (float.IsNaN(sensitivity) || sensitivity <= 0f) sensitivity = 1f;
            if (float.IsNaN(deadZone) || deadZone < 0f) deadZone = 0f;
            if (deadZone > MaxDeadZone) deadZone = MaxDeadZone;

            Vector2 normalized = delta / (range / sensitivity);
            float length = normalized.magnitude;
            if (length < deadZone || length <= 0f) return Vector2.zero;

            float clamped = Mathf.Min(length, 1f);
            float scaled = (clamped - deadZone) / (1f - deadZone);
            return normalized / length * scaled;
        }
    }
}
