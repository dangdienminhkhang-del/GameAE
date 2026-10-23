using UnityEngine;

namespace Game.Core.Facing
{
    /// <summary>
    /// Giữ hướng nhìn hiện tại (thuần C#, không dùng đối tượng scene).
    /// - Chỉ đổi hướng khi đầu vào đủ lớn (vượt ngưỡng chết); thả joystick (vector gần 0) thì GIỮ hướng cũ.
    /// - Hướng luôn được làm tròn về một trong 8 hướng (vector đơn vị), nên đòn đánh và Né đi đúng một trong 8 hướng.
    /// - Có độ trễ nhỏ ở ranh giới giữa hai hướng để joystick rung nhẹ không làm hướng nhảy qua lại.
    ///
    /// NGƯỠNG CHẾT Ở ĐÂY CHỈ LÀ BỘ LỌC NHIỄU NHỎ, không phải dead zone joystick. Dead zone người chơi chỉnh được là việc của
    /// P9.6 (JoystickMath, ở đầu ra của joystick); khi đó FacingTracker nhận vector ĐÃ qua dead zone và chỉ lọc nhiễu số.
    /// </summary>
    public sealed class FacingTracker
    {
        /// <summary>Ngưỡng chết mặc định (P9.6): chỉ lọc nhiễu số, nhỏ hơn dead zone nhỏ nhất của Settings (SettingsRules.MinDeadZone). Dead zone thật do JoystickMath quyết.</summary>
        public const float DefaultDeadZone = 0.02f;

        /// <summary>Độ trễ mặc định (độ) cộng thêm vào nửa bước 22.5° trước khi đổi sang hướng khác.</summary>
        public const float DefaultHysteresisDegrees = 5f;

        private readonly float _deadZone;
        private readonly float _hysteresisDegrees;
        private Direction8 _direction;

        public FacingTracker(Direction8 initial = Direction8.East, float deadZone = DefaultDeadZone, float hysteresisDegrees = DefaultHysteresisDegrees)
        {
            _direction = initial;
            _deadZone = SanitizeDeadZone(deadZone);
            _hysteresisDegrees = Mathf.Clamp(float.IsNaN(hysteresisDegrees) ? 0f : hysteresisDegrees, 0f, FacingMath.HalfStepDegrees);
        }

        public float DeadZone => _deadZone;

        /// <summary>Hướng nhìn hiện tại (một trong 8 hướng).</summary>
        public Direction8 Direction => _direction;

        /// <summary>Vector hướng nhìn đã chuẩn hóa (độ dài 1). Mặc định (1, 0).</summary>
        public Vector2 Vector => FacingMath.ToVector(_direction);

        /// <summary>
        /// Đưa đầu vào di chuyển (joystick/bàn phím) vào. Trả true nếu hướng đổi.
        /// Độ lớn dưới ngưỡng chết, NaN hoặc vô cực thì bỏ qua (giữ hướng cũ).
        /// </summary>
        public bool Update(Vector2 input)
        {
            if (!FacingMath.IsUsable(input)) return false;
            if (input.magnitude < _deadZone) return false;

            Direction8 candidate = FacingMath.FromVector(input, _direction);
            if (candidate == _direction) return false;

            // Độ trễ: vẫn còn trong "vùng của hướng cũ" cộng thêm một chút thì chưa đổi.
            float offAxis = FacingMath.AngularDistance(FacingMath.AngleDegrees(input), FacingMath.AngleDegrees(_direction));
            if (offAxis <= FacingMath.HalfStepDegrees + _hysteresisDegrees) return false;

            _direction = candidate;
            return true;
        }

        /// <summary>
        /// Đặt hướng trực tiếp (ví dụ tự nhắm). Vector không dùng được thì bỏ qua. Không áp ngưỡng chết hay độ trễ.
        /// Trả true nếu hướng đổi.
        /// </summary>
        public bool Set(Vector2 direction)
        {
            if (!FacingMath.IsUsable(direction)) return false;

            Direction8 candidate = FacingMath.FromVector(direction, _direction);
            if (candidate == _direction) return false;

            _direction = candidate;
            return true;
        }

        private static float SanitizeDeadZone(float value)
        {
            if (float.IsNaN(value) || value < 0f) return 0f;
            return Mathf.Min(value, 0.9f);
        }
    }
}
