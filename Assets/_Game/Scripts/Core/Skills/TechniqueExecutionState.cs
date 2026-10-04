namespace Game.Core.Skills
{
    /// <summary>
    /// Trạng thái của một lần thực thi Technique (docs/12 §4.2).
    /// Chỉ Active mới được tạo hitbox.
    /// </summary>
    public enum TechniqueExecutionState
    {
        Idle = 0,
        WindUp = 1,
        Active = 2,
        Recovery = 3
    }
}
