namespace Game.Core.Facing
{
    /// <summary>
    /// 8 hướng nhìn, bước 45°, đếm từ trục +X ngược chiều kim đồng hồ
    /// (Đông 0°, Đông-Bắc 45°, Bắc 90°, Tây-Bắc 135°, Tây 180°, Tây-Nam 225°, Nam 270°, Đông-Nam 315°).
    /// Giá trị số (0..7) là chỉ số hướng; đừng đổi thứ tự (FacingMath dựa vào nó).
    /// </summary>
    public enum Direction8
    {
        East = 0,
        NorthEast = 1,
        North = 2,
        NorthWest = 3,
        West = 4,
        SouthWest = 5,
        South = 6,
        SouthEast = 7
    }

    /// <summary>
    /// 5 khe hình khi vẽ nhân vật 8 hướng (docs/14 §4.1): Nam, Đông-Nam, Đông, Đông-Bắc, Bắc.
    /// Ba hướng phía Tây là bản lật ngang của ba khe phía Đông.
    /// </summary>
    public enum ArtSlot
    {
        South = 0,
        SouthEast = 1,
        East = 2,
        NorthEast = 3,
        North = 4
    }

    /// <summary>Khe hình cần dùng cho một hướng, kèm cờ lật ngang.</summary>
    public readonly struct ArtFacing
    {
        public readonly ArtSlot Slot;
        public readonly bool FlipX;

        public ArtFacing(ArtSlot slot, bool flipX)
        {
            Slot = slot;
            FlipX = flipX;
        }
    }
}
