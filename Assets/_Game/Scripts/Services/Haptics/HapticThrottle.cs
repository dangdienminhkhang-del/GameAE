namespace Game.Services.Haptics
{
    /// <summary>
    /// Giãn cách tối thiểu giữa hai xung rung (thuần logic). Dùng thời gian THẬT (unscaled) nên vẫn đúng khi dừng hình.
    /// </summary>
    public sealed class HapticThrottle
    {
        /// <summary>Khoảng cách tối thiểu mặc định: 80 ms.</summary>
        public const float DefaultMinIntervalSeconds = 0.08f;

        private readonly float _minInterval;
        private float _lastAt = float.NegativeInfinity;

        public HapticThrottle(float minIntervalSeconds = DefaultMinIntervalSeconds)
        {
            _minInterval = minIntervalSeconds < 0f ? 0f : minIntervalSeconds;
        }

        /// <summary>True nếu được phép rung ở thời điểm <paramref name="nowUnscaled"/> (và ghi nhận là đã rung).</summary>
        public bool TryAcquire(float nowUnscaled)
        {
            if (nowUnscaled - _lastAt < _minInterval) return false;
            _lastAt = nowUnscaled;
            return true;
        }

        public void Reset() => _lastAt = float.NegativeInfinity;
    }

    /// <summary>
    /// Cảnh báo HP thấp có độ trễ: báo MỖI LẦN HP rơi xuống dưới ngưỡng thấp; chỉ báo lại sau khi HP đã lên trên ngưỡng nạp lại.
    /// Đã chết (HP ≤ 0) thì không báo. Thuần logic.
    /// </summary>
    public sealed class LowHpWarning
    {
        public const float DefaultLowRatio = 0.30f;
        public const float DefaultRearmRatio = 0.35f;

        private readonly float _low;
        private readonly float _rearm;
        private bool _armed = true;

        public LowHpWarning(float lowRatio = DefaultLowRatio, float rearmRatio = DefaultRearmRatio)
        {
            _low = lowRatio;
            _rearm = rearmRatio < lowRatio ? lowRatio : rearmRatio;
        }

        /// <summary>Gọi mỗi khi HP đổi. Trả true đúng một lần khi HP vừa rơi xuống dưới ngưỡng thấp.</summary>
        public bool ShouldWarn(double hp, double maxHp)
        {
            if (maxHp <= 0.0) return false;
            if (hp <= 0.0) return false;

            double ratio = hp / maxHp;
            if (ratio > _rearm)
            {
                _armed = true;
                return false;
            }
            if (ratio < _low && _armed)
            {
                _armed = false;
                return true;
            }
            return false;
        }

        public void Reset() => _armed = true;
    }
}
