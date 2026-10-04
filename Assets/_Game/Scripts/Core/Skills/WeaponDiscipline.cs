namespace Game.Core.Skills
{
    /// <summary>
    /// Họ vũ khí — xác định nhịp chơi và Technique khả dụng (docs/12 §6.1).
    /// Blade là Discipline duy nhất được triển khai ở Mốc 1.
    /// </summary>
    public enum WeaponDiscipline
    {
        None = 0,
        Blade = 1,
        Knives = 2,
        Needle = 3,
        Greatblade = 4,
        Maul = 5,
        Cleaver = 6
    }
}
