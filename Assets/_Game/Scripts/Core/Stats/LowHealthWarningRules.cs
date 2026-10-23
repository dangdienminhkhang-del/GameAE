using UnityEngine;

namespace Game.Core.Stats
{
    /// <summary>
    /// Quy tắc cảnh báo máu thấp (thuần, không biết gì về scene hay UI) — UI chỉ việc hỏi "độ đậm bao nhiêu".
    /// Chạm ngưỡng cảnh báo → viền đỏ hiện nhẹ; càng gần 0 càng đậm; dưới ngưỡng nguy kịch thì nhịp nhanh hơn.
    /// Nhịp luôn dưới 3 Hz (an toàn cho người nhạy cảm với ánh nháy). Chết (máu = 0) thì không cảnh báo
    /// vì đã có màn hình chết.
    /// </summary>
    public static class LowHealthWarningRules
    {
        /// <summary>Bắt đầu cảnh báo khi máu còn từ mức này trở xuống (tỉ lệ 0..1).</summary>
        public const float WarnRatio = 0.30f;

        /// <summary>Dưới mức này là nguy kịch: nhịp nhanh hơn.</summary>
        public const float CriticalRatio = 0.15f;

        /// <summary>Độ đậm tối đa của viền (alpha), để không che mất màn chơi.</summary>
        public const float MaxIntensity = 0.60f;

        public const float WarnPulseHz = 1.0f;
        public const float CriticalPulseHz = 2.0f;

        private const float MinIntensityAtThreshold = 0.25f;

        /// <summary>Có đang ở vùng máu thấp không (còn sống và ≤ ngưỡng).</summary>
        public static bool IsLow(float healthRatio)
        {
            return !float.IsNaN(healthRatio) && healthRatio > 0f && healthRatio <= WarnRatio;
        }

        /// <summary>Độ đậm nền (chưa nhấp nháy): 0 khi máu ổn hoặc đã chết; tăng dần khi máu giảm.</summary>
        public static float BaseIntensity(float healthRatio)
        {
            if (!IsLow(healthRatio)) return 0f;
            float t = 1f - healthRatio / WarnRatio; // 0 ở ngưỡng → gần 1 khi sắp chết
            return Mathf.Lerp(MinIntensityAtThreshold, MaxIntensity, t);
        }

        /// <summary>Độ đậm tại thời điểm <paramref name="timeSeconds"/> (đã nhấp nháy).</summary>
        public static float Intensity(float healthRatio, float timeSeconds)
        {
            float baseValue = BaseIntensity(healthRatio);
            if (baseValue <= 0f) return 0f;

            float hz = healthRatio <= CriticalRatio ? CriticalPulseHz : WarnPulseHz;
            float pulse = 0.65f + 0.35f * Mathf.Sin(timeSeconds * hz * 2f * Mathf.PI);
            return Mathf.Clamp(baseValue * pulse, 0f, MaxIntensity);
        }
    }
}
