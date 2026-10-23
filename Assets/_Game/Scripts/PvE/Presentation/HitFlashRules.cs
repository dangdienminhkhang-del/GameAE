using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Luật thuần của lớp phủ nháy "HitFlash" (ADR-029): alpha theo thời gian, giới hạn khi đang báo đòn nguy hiểm.
    /// Không dùng đối tượng Unity nào ngoài <see cref="Mathf"/> nên test được trong EditMode.
    /// Đảm bảo: kết quả luôn nằm trong [0, 1], không bao giờ âm hoặc NaN, kể cả khi đầu vào hỏng.
    /// </summary>
    public static class HitFlashRules
    {
        /// <summary>
        /// Alpha lớp phủ tại thời điểm <paramref name="elapsedSeconds"/> kể từ lúc bắt đầu nháy.
        /// Giảm tuyến tính từ alpha đầu về 0 trong <paramref name="durationSeconds"/>. Đang báo đòn nguy hiểm thì alpha đầu
        /// bị kẹp xuống tối đa <paramref name="maxAlphaUnderDanger"/> (đường giảm vẫn mượt, không có bước nhảy).
        /// </summary>
        public static float Alpha(float elapsedSeconds, float durationSeconds, float startAlpha, bool dangerActive, float maxAlphaUnderDanger)
        {
            if (!IsFinitePositive(durationSeconds)) return 0f;
            if (float.IsNaN(elapsedSeconds)) return 0f;

            float peak = PeakAlpha(startAlpha, dangerActive, maxAlphaUnderDanger);
            float progress = Mathf.Clamp01(Mathf.Max(0f, elapsedSeconds) / durationSeconds);
            float alpha = peak * (1f - progress);
            return float.IsNaN(alpha) ? 0f : Mathf.Clamp01(alpha);
        }

        /// <summary>Alpha cao nhất của lớp phủ (tại t = 0) sau khi áp giới hạn báo đòn.</summary>
        public static float PeakAlpha(float startAlpha, bool dangerActive, float maxAlphaUnderDanger)
        {
            float peak = Sanitize01(startAlpha);
            if (dangerActive) peak = Mathf.Min(peak, Sanitize01(maxAlphaUnderDanger));
            return peak;
        }

        /// <summary>Lớp phủ còn đang nháy không (chưa hết <paramref name="durationSeconds"/>).</summary>
        public static bool IsActive(float elapsedSeconds, float durationSeconds)
        {
            if (!IsFinitePositive(durationSeconds)) return false;
            if (float.IsNaN(elapsedSeconds)) return false;
            return elapsedSeconds < durationSeconds;
        }

        /// <summary>Loại màu nào được coi là báo đòn nguy hiểm (bị giới hạn alpha khi nháy). Chỉ <see cref="TintKind.DangerTelegraph"/>.</summary>
        public static bool IsDanger(TintKind kind)
        {
            return kind == TintKind.DangerTelegraph;
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsInfinity(value) && !float.IsNaN(value);
        }

        private static float Sanitize01(float value)
        {
            return float.IsNaN(value) ? 0f : Mathf.Clamp01(value);
        }
    }
}
