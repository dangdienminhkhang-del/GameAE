namespace Game.PvE.Enemy
{
    /// <summary>
    /// Vai trò chiến đấu của quái (docs/12 §7.1, Phase 9.8). Chỉ là nhãn dữ liệu trong EnemyDefinition.
    /// Chaser PHẢI là giá trị 0 để asset cũ (chưa có field này) mặc định đúng hành vi cũ.
    /// Giá trị số được serialize trong asset nên KHÔNG đổi thứ tự hay số.
    /// </summary>
    public enum CombatRole
    {
        /// <summary>Đuổi, báo đòn, cận chiến (hành vi cũ).</summary>
        Chaser = 0,
        /// <summary>Áp sát, đánh, rồi lùi ra, rồi áp sát lại.</summary>
        Skirmisher = 1,
        /// <summary>Tạm dùng hành vi Chaser (chưa có thiết kế riêng).</summary>
        Guard = 2,
        /// <summary>Giữ khoảng cách, báo đòn, bắn đạn thẳng né được.</summary>
        Ranged = 3,
        /// <summary>Tạm dùng hành vi Chaser (chưa có thiết kế riêng).</summary>
        Elite = 4
    }
}
