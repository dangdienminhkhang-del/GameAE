using System;
using UnityEngine;

namespace Game.Core.Combat
{
    /// <summary>
    /// Quy tắc Kỹ năng Độc Nhất Nhị Đao Lưu (Unique Skill: Dual Wielding / Dual Blades) chuẩn Sword Art Online (SAO).
    /// - Kirito là người chơi duy nhất sở hữu kỹ năng Nhị Đao Lưu nhờ tốc độ phản xạ cao nhất game.
    /// - Khi kích hoạt Nhị Đao Lưu:
    ///   + Tăng tốc độ tấn công (giảm thời gian hồi đòn đánh 25%).
    ///   + Sát thương kết hợp từ cả 2 thanh kiếm (100% công tay chính + 60% công tay phụ).
    ///   + Chuỗi combo 3 nhịp song kiếm liên hoàn với hệ số sát thương lớn hơn (1.15×, 1.45×, 2.20×).
    /// Pure logic — độc lập khỏi scene, 100% testable trong EditMode.
    /// </summary>
    public static class DualWieldRules
    {
        /// <summary>Hệ số đóng góp sát thương của vũ khí tay phụ (60% công).</summary>
        public const double OffHandAttackContribution = 0.60;

        /// <summary>Hệ số giảm thời gian hồi chiêu đòn đánh thường khi cầm 2 kiếm (đánh nhanh hơn 25%).</summary>
        public const float AttackCooldownReduction = 0.75f;

        /// <summary>Hệ số tăng tốc độ combo khi cầm 2 kiếm.</summary>
        public const float ComboSpeedMultiplier = 1.30f;

        /// <summary>Hệ số sát thương combo 3 nhịp của Nhị Đao Lưu.</summary>
        public static readonly double[] DualWieldComboMultipliers = new double[] { 1.15, 1.45, 2.20 };

        /// <summary>
        /// Kiểm tra xem trạng thái Nhị Đao Lưu có được kích hoạt hay không.
        /// Cần cả vũ khí tay chính và tay phụ, hoặc cờ mở khóa kỹ năng độc nhất.
        /// </summary>
        public static bool IsDualWielding(bool hasMainHandWeapon, bool hasOffHandWeapon, bool isSkillUnlocked = true)
        {
            return isSkillUnlocked && hasMainHandWeapon && hasOffHandWeapon;
        }

        /// <summary>
        /// Tính toán tổng lực tấn công khi sử dụng Nhị Đao Lưu.
        /// </summary>
        public static double CalculateTotalAttack(double baseAttack, double mainWeaponAttack, double offWeaponAttack, bool isDualWielding)
        {
            if (!isDualWielding)
            {
                return baseAttack + mainWeaponAttack;
            }

            // Sát thương = Công cơ bản + 100% kiếm chính + 60% kiếm phụ
            return baseAttack + mainWeaponAttack + (offWeaponAttack * OffHandAttackContribution);
        }

        /// <summary>
        /// Thời gian hồi chiêu đòn đánh (cooldown) sau khi áp dụng hệ số Nhị Đao Lưu.
        /// </summary>
        public static float CalculateAttackCooldown(float baseCooldown, bool isDualWielding)
        {
            if (!isDualWielding) return baseCooldown;
            return Mathf.Max(0.10f, baseCooldown * AttackCooldownReduction);
        }

        /// <summary>
        /// Lấy hệ số sát thương cho nhịp combo của Nhị Đao Lưu (0, 1, 2).
        /// </summary>
        public static double GetComboMultiplier(int step, bool isDualWielding)
        {
            if (!isDualWielding)
            {
                if (step == 1) return 1.25;
                if (step == 2) return 1.75;
                return 1.0;
            }

            int clamped = Mathf.Clamp(step, 0, DualWieldComboMultipliers.Length - 1);
            return DualWieldComboMultipliers[clamped];
        }
    }
}
