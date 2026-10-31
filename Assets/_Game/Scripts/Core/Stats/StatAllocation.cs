using System;

namespace Game.Core.Stats
{
    /// <summary>Hai chỉ số cộng điểm kiểu SAO.</summary>
    public enum AllocatableStat
    {
        Strength,
        Agility
    }

    /// <summary>
    /// Cộng điểm chỉ số khi lên cấp (SAO): mỗi level được PointsPerLevel điểm, người chơi tự chia vào STR hoặc AGI.
    /// - STR: tăng tấn công.
    /// - AGI: tăng tốc chạy và tỉ lệ chí mạng (có trần để không phá cân bằng).
    /// Điểm CHƯA DÙNG được suy ra từ level (tổng điểm đã nhận − điểm đã cộng) nên save chỉ cần lưu STR/AGI đã cộng;
    /// save cũ (STR = AGI = 0) tự có đủ điểm theo level hiện tại. Lớp thuần C#, không phụ thuộc Unity — dễ test.
    /// </summary>
    public sealed class StatAllocation
    {
        /// <summary>Số điểm nhận được mỗi lần lên cấp.</summary>
        public const int PointsPerLevel = 3;

        /// <summary>Mỗi điểm STR cộng bấy nhiêu tấn công.</summary>
        public const double AttackPerStrength = 1.0;

        /// <summary>Mỗi điểm AGI cộng bấy nhiêu phần trăm (dạng phân số) tốc chạy: 0.004 = 0.4%.</summary>
        public const double MoveSpeedFractionPerAgility = 0.004;

        /// <summary>Trần cộng tốc chạy từ AGI (0.30 = +30%).</summary>
        public const double MaxMoveSpeedFraction = 0.30;

        /// <summary>Mỗi điểm AGI cộng bấy nhiêu tỉ lệ chí mạng: 0.003 = 0.3%.</summary>
        public const double CritChancePerAgility = 0.003;

        /// <summary>Trần cộng chí mạng từ AGI (0.30 = +30%).</summary>
        public const double MaxCritChanceBonus = 0.30;

        public int Strength { get; private set; }
        public int Agility { get; private set; }

        /// <summary>Phát khi STR/AGI đổi (cộng điểm hoặc nạp save).</summary>
        public event Action Changed;

        /// <summary>Tổng điểm đã nhận được ở level này (level 1 = 0).</summary>
        public static int TotalPointsForLevel(int level)
        {
            int safeLevel = level < 1 ? 1 : level;
            return (safeLevel - 1) * PointsPerLevel;
        }

        public int Spent => Strength + Agility;

        /// <summary>Điểm còn có thể cộng ở level này.</summary>
        public int Unspent(int level)
        {
            int left = TotalPointsForLevel(level) - Spent;
            return left > 0 ? left : 0;
        }

        public int PointsIn(AllocatableStat stat) => stat == AllocatableStat.Strength ? Strength : Agility;

        /// <summary>Cộng <paramref name="points"/> điểm vào chỉ số. False nếu không đủ điểm hoặc tham số sai.</summary>
        public bool TryAllocate(AllocatableStat stat, int level, int points = 1)
        {
            if (points <= 0 || Unspent(level) < points) return false;

            if (stat == AllocatableStat.Strength) Strength += points;
            else Agility += points;

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Nạp từ save. Giá trị âm coi như 0; nếu tổng vượt số điểm của level (save bị sửa / level bị hạ)
        /// thì cắt AGI trước rồi tới STR để không có điểm "ảo".
        /// </summary>
        public void LoadFrom(int strength, int agility, int level)
        {
            int total = TotalPointsForLevel(level);
            int str = strength < 0 ? 0 : strength;
            int agi = agility < 0 ? 0 : agility;

            if (str > total) str = total;
            if (str + agi > total) agi = total - str;

            Strength = str;
            Agility = agi;
            Changed?.Invoke();
        }

        /// <summary>Xóa hết điểm đã cộng (dùng khi tạo nhân vật mới / reset).</summary>
        public void Clear()
        {
            Strength = 0;
            Agility = 0;
            Changed?.Invoke();
        }

        // ── Công thức hiệu ứng (thuần, không đổi trạng thái) ─────────────────

        public static double AttackBonus(int strength)
        {
            return strength <= 0 ? 0.0 : strength * AttackPerStrength;
        }

        public static double MoveSpeedFractionBonus(int agility)
        {
            if (agility <= 0) return 0.0;
            double value = agility * MoveSpeedFractionPerAgility;
            return value > MaxMoveSpeedFraction ? MaxMoveSpeedFraction : value;
        }

        public static double CritChanceBonus(int agility)
        {
            if (agility <= 0) return 0.0;
            double value = agility * CritChancePerAgility;
            return value > MaxCritChanceBonus ? MaxCritChanceBonus : value;
        }
    }
}
