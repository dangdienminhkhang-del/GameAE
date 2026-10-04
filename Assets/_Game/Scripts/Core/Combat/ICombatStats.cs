namespace Game.Core.Combat
{
    /// <summary>
    /// Chỉ số chiến đấu tối thiểu mà damage pipeline cần.
    /// Cài đặt bởi CharacterStats (player/enemy) hoặc bất kỳ mục tiêu nào có chỉ số.
    /// </summary>
    public interface ICombatStats
    {
        double Attack { get; }
        double Defense { get; }
        double CriticalChance { get; }
        double CriticalDamage { get; }
    }
}
