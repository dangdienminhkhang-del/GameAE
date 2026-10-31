using UnityEngine;

namespace Game.UI.Controls
{
    /// <summary>
    /// Công thức thuần (không đụng scene) cho hiệu ứng nút HUD: lò xo nảy, nhịp thở, vòng lóe.
    /// Tách riêng để test EditMode được. CHỈ hiển thị — không chứa logic gameplay (luật #3).
    /// </summary>
    public static class HudFeedbackCurves
    {
        /// <summary>Phần cooldown còn lại nhỏ hơn mức này coi như đã hồi xong.</summary>
        public const float CoolingEpsilon = 0.01f;

        private const float MaxSubStep = 0.004f;

        /// <summary>
        /// Một bước lò xo giảm chấn: value kéo về target, velocity là vận tốc hiện tại.
        /// Chia nhỏ bước nên ổn định cả khi dt lớn (khung hình giật).
        /// </summary>
        public static void SpringStep(ref float value, ref float velocity, float target,
            float stiffness, float damping, float dt)
        {
            if (dt <= 0f) return;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / MaxSubStep));
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float accel = -stiffness * (value - target) - damping * velocity;
                velocity += accel * h;
                value += velocity * h;
            }
        }

        /// <summary>Sóng thở 0..1: bắt đầu ở 0, lên 1 ở nửa chu kỳ rồi về 0.</summary>
        public static float BreathWave(float time, float periodSeconds)
        {
            if (periodSeconds <= 0.0001f) return 0f;
            return 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * time / periodSeconds);
        }

        /// <summary>Hệ số phóng to khi "thở": từ 1 đến 1 + amount.</summary>
        public static float Breath(float time, float periodSeconds, float amount)
        {
            return 1f + amount * BreathWave(time, periodSeconds);
        }

        /// <summary>Đang hồi chiêu nếu vòng đếm ngược bật và còn một phần đáng kể.</summary>
        public static bool IsCooling(bool fillEnabled, float fillAmount)
        {
            return fillEnabled && fillAmount > CoolingEpsilon;
        }

        /// <summary>Lóe đúng một lần: lúc chuyển từ đang hồi sang hồi xong.</summary>
        public static bool ShouldFlash(bool wasCooling, bool isCooling)
        {
            return wasCooling && !isCooling;
        }

        /// <summary>Vòng lóe nở dần ra ngoài (t từ 0 đến 1), nhanh lúc đầu rồi chậm lại.</summary>
        public static float FlashRingScale(float t01)
        {
            float t = Mathf.Clamp01(t01);
            float ease = 1f - (1f - t) * (1f - t);
            return Mathf.Lerp(0.9f, 1.7f, ease);
        }

        /// <summary>Vòng lóe mờ dần từ peakAlpha về 0.</summary>
        public static float FlashRingAlpha(float t01, float peakAlpha)
        {
            float t = Mathf.Clamp01(t01);
            return peakAlpha * (1f - t) * (1f - t);
        }
    }
}
