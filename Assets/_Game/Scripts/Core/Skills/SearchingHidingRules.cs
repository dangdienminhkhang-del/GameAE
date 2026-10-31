using System;
using UnityEngine;

namespace Game.Core.Skills
{
    /// <summary>
    /// Quy tắc Kỹ năng Phụ (Extra Skills): Dò Tìm & Ẩn Nấp (Searching & Hiding) chuẩn Sword Art Online (SAO).
    /// - Searching: Mở rộng bán kính tự động phát hiện quái, hiển thị con trỏ cấp độ / độ nguy hiểm
    ///   của quái vật trước khi giao chiến.
    /// - Hiding: Cho phép người chơi hòa mình vào môi trường khi đứng yên hoặc di chuyển chậm,
    ///   giảm đáng kể bán kính phát hiện của quái vật (50-60%). Bị phá vỡ khi tấn công hoặc dính đòn.
    /// Pure logic — độc lập khỏi scene, 100% testable trong EditMode.
    /// </summary>
    public static class SearchingHidingRules
    {
        // ── Searching Presets ──
        /// <summary>Bán kính dò tìm quái mặc định của người chơi (mét).</summary>
        public const float DefaultSearchingRadius = 7.5f;

        /// <summary>Bán kính dò tìm tối thiểu.</summary>
        public const float MinSearchingRadius = 4.0f;

        /// <summary>Bán kính dò tìm tối đa khi max skill Searching.</summary>
        public const float MaxSearchingRadius = 16.0f;

        // ── Hiding Presets ──
        /// <summary>Thời gian đứng yên liên tục để kích hoạt thế Ẩn Nấp (giây).</summary>
        public const float HidingActivationTimeSeconds = 1.20f;

        /// <summary>Tỉ lệ giảm tầm phát hiện của quái khi đứng yên ẩn nấp (60%).</summary>
        public const float HidingReductionStanding = 0.60f;

        /// <summary>Tỉ lệ giảm tầm phát hiện khi di chuyển chậm (30%).</summary>
        public const float HidingReductionMoving = 0.30f;

        /// <summary>Độ trong suốt của nhân vật khi đang ẩn nấp (alpha).</summary>
        public const float StealthAlpha = 0.55f;

        /// <summary>
        /// Tính toán bán kính dò tìm quái vật dựa trên cấp độ kỹ năng Searching (0 - 100).
        /// </summary>
        public static float CalculateSearchingRadius(int searchingLevel)
        {
            if (searchingLevel <= 0) return DefaultSearchingRadius;
            float bonus = searchingLevel * 0.10f; // Mỗi level searching tăng 0.1m
            return Mathf.Clamp(DefaultSearchingRadius + bonus, MinSearchingRadius, MaxSearchingRadius);
        }

        /// <summary>
        /// Kiểm tra xem mục tiêu có nằm trong tầm dò tìm hay không.
        /// </summary>
        public static bool IsWithinSearchingRange(Vector2 playerPos, Vector2 targetPos, float searchingRadius)
        {
            if (searchingRadius <= 0f) return false;
            float sqrDist = (playerPos - targetPos).sqrMagnitude;
            return sqrDist <= searchingRadius * searchingRadius;
        }

        /// <summary>
        /// Tính toán tầm phát hiện hiệu dụng của quái vật khi người chơi đang Ẩn Nấp.
        /// </summary>
        public static float CalculateEffectiveDetectionRange(float baseDetectionRange, float hidingFactor)
        {
            if (baseDetectionRange <= 0f) return 0f;
            float factor = Mathf.Clamp01(hidingFactor);
            float effective = baseDetectionRange * (1.0f - factor);
            return Mathf.Max(1.0f, effective); // Quái luôn có tối thiểu 1.0m tầm phát hiện cận chiến
        }

        /// <summary>
        /// Kiểm tra xem trạng thái Ẩn Nấp có bị phá vỡ hay không.
        /// </summary>
        public static bool ShouldBreakHiding(bool didAttack, bool didTakeDamage, bool isUsingSkill)
        {
            return didAttack || didTakeDamage || isUsingSkill;
        }

        /// <summary>
        /// Xác định hệ số ẩn nấp dựa trên trạng thái di chuyển và thời gian đứng yên.
        /// </summary>
        public static float EvaluateHidingFactor(float stillDuration, bool isMoving, bool isStealthBroken)
        {
            if (isStealthBroken) return 0f;

            if (!isMoving && stillDuration >= HidingActivationTimeSeconds)
            {
                return HidingReductionStanding;
            }

            if (isMoving && stillDuration >= HidingActivationTimeSeconds)
            {
                return HidingReductionMoving;
            }

            return 0f;
        }
    }
}
