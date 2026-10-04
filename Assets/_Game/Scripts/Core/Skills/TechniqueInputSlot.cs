namespace Game.Core.Skills
{
    /// <summary>
    /// Vị trí input Technique — quyết định cooldown/recovery target (docs/07 §4.1).
    /// Mốc 1 chỉ có Core và Traverse.
    /// </summary>
    public enum TechniqueInputSlot
    {
        None = 0,
        Core = 1,
        Traverse = 2,
        Control = 3,
        Signature = 4
    }
}
