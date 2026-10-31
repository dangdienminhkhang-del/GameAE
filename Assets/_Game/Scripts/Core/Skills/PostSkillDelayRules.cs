using System;
using UnityEngine;

namespace Game.Core.Skills
{
    /// <summary>
    /// Quy tắc độ trễ / khựng chiêu (Post-Skill Delay / Post-Motion Freeze) theo chuẩn Sword Art Online (SAO).
    /// - Trong SAO, sau khi xả Sword Skill, nhân vật bị khựng một khoảng thời gian không thể di chuyển,
    ///   không tung đòn thường hoặc kỹ năng khác được, tạo sơ hở cho đối thủ phản công.
    /// - Chiêu càng mạnh (totalMultiplier cao) hoặc nhiều đòn đánh liên tiếp (multi-hit combo)
    ///   thì thời gian khựng càng lâu.
    /// - Tuyệt kỹ tối thượng (Ultimate) có thời gian khựng đặc trưng để cân bằng sức mạnh.
    /// Pure logic — dễ dàng unit test EditMode không phụ thuộc scene.
    /// </summary>
    public static class PostSkillDelayRules
    {
        /// <summary>Thời gian khựng tối thiểu sau bất kỳ kỹ năng nào (giây).</summary>
        public const float MinDelaySeconds = 0.20f;

        /// <summary>Thời gian khựng tối đa cho phép để tránh làm nhân vật bất động quá lâu (giây).</summary>
        public const float MaxDelaySeconds = 2.50f;

        /// <summary>Thời gian khựng cộng thêm cho mỗi nhịp đòn đánh ngoài nhịp đầu tiên (giây/hit).</summary>
        public const float DelayPerExtraHit = 0.08f;

        /// <summary>Ngưỡng sát thương bắt đầu tính thêm khựng (hệ số nhân tổng sát thương).</summary>
        public const double HighMultiplierThreshold = 1.5;

        /// <summary>Thời gian khựng cộng thêm theo mỗi 1.0 hệ số sát thương vượt ngưỡng.</summary>
        public const float DelayPerMultiplierOverThreshold = 0.06f;

        /// <summary>Thời gian khựng mặc định của Tuyệt Kỹ (Ultimate - Vũ Điệu Cuồng Nộ).</summary>
        public const float DefaultUltimateFreezeDuration = 0.85f;

        /// <summary>
        /// Tính toán thời gian khựng (post-motion freeze) của một kỹ năng dựa trên thông số cơ bản.
        /// </summary>
        public static float CalculateDelay(float baseRecovery, int hitCount = 1, double totalMultiplier = 1.0)
        {
            float delay = Mathf.Max(0.10f, baseRecovery);

            // Thêm delay theo số lượng hit (multi-hit combo)
            if (hitCount > 1)
            {
                delay += (hitCount - 1) * DelayPerExtraHit;
            }

            // Thêm delay nếu tổng sát thương lớn
            if (totalMultiplier > HighMultiplierThreshold)
            {
                float excess = (float)(totalMultiplier - HighMultiplierThreshold);
                delay += excess * DelayPerMultiplierOverThreshold;
            }

            return Mathf.Clamp(delay, MinDelaySeconds, MaxDelaySeconds);
        }

        /// <summary>
        /// Tính toán thời gian khựng từ WeaponTechniqueDefinition.
        /// </summary>
        public static float CalculateDelay(WeaponTechniqueDefinition definition)
        {
            if (definition == null) return MinDelaySeconds;

            int hitCount = definition.ActiveWindows != null ? definition.ActiveWindows.Count : 1;
            return CalculateDelay(definition.RecoverySeconds, hitCount, definition.TotalMultiplier);
        }

        /// <summary>
        /// Kiểm tra xem người chơi còn đang trong thời gian khựng chiêu hay không.
        /// </summary>
        public static bool IsDelayActive(float freezeEndTime, float currentTime)
        {
            return currentTime < freezeEndTime;
        }

        /// <summary>
        /// Thời gian khựng còn lại (giây). Trả về 0 nếu đã hết khựng.
        /// </summary>
        public static float GetRemainingDelay(float freezeEndTime, float currentTime)
        {
            return Mathf.Max(0f, freezeEndTime - currentTime);
        }

        /// <summary>
        /// Người chơi có thể thực hiện hành động (di chuyển, tấn công, né, dùng kỹ năng) hay không.
        /// </summary>
        public static bool CanAct(float freezeEndTime, float currentTime)
        {
            return !IsDelayActive(freezeEndTime, currentTime);
        }
    }
}
