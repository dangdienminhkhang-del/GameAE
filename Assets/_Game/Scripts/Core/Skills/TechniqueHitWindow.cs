using System;

namespace Game.Core.Skills
{
    /// <summary>
    /// Một cửa sổ active của Technique: từ startTimeOffset (sau WindUp) kéo dài duration giây.
    /// Mỗi cửa sổ có multiplier riêng và policy di chuyển.
    /// Definition chia totalMultiplier thành nhiều hit profile rời rạc (docs/05 §4.2, docs/12 §5).
    /// </summary>
    [Serializable]
    public struct TechniqueHitWindow
    {
        /// <summary>Thời điểm bắt đầu cửa sổ tính từ lúc hết WindUp (giây).</summary>
        public float startTimeOffset;

        /// <summary>Thời gian tồn tại cửa sổ (giây).</summary>
        public float duration;

        /// <summary>Nhân sát thương của cửa sổ này so với Base Attack. Tổng các hit không được vượt totalMultiplier.</summary>
        public double hitMultiplier;

        /// <summary>Người chơi được phép di chuyển như thế nào trong cửa sổ này.</summary>
        public MovementPolicy movementPolicy;

        /// <summary>Khoảng cách di chuyển (đơn vị Unity) khi dùng Step/Dash/Commit. 0 = dùng mặc định controller.</summary>
        public float moveDistance;

        public TechniqueHitWindow(
            float startTimeOffset,
            float duration,
            double hitMultiplier,
            MovementPolicy movementPolicy = MovementPolicy.None,
            float moveDistance = 0f)
        {
            this.startTimeOffset = startTimeOffset;
            this.duration = duration;
            this.hitMultiplier = hitMultiplier;
            this.movementPolicy = movementPolicy;
            this.moveDistance = moveDistance;
        }
    }
}
