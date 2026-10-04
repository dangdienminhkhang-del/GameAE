namespace Game.Core.Skills
{
    /// <summary>
    /// Kiểu di chuyển của người chơi trong từng cửa sổ active (docs/12 §6.3).
    /// None = đứng yên đánh; Step = dịch chuyển ngắn 1 bước; Dash = lao nhanh; Commit = di chuyển dài, hở nhiều.
    /// </summary>
    public enum MovementPolicy
    {
        None = 0,
        Step = 1,
        Dash = 2,
        Commit = 3
    }
}
