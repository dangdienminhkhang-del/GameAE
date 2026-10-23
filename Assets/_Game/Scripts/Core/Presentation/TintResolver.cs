using UnityEngine;

namespace Game.Core.Presentation
{
    /// <summary>
    /// Các lớp MÀU NHÂN của một sprite: màu trạng thái (báo đòn, Punish, Cuồng nộ, gốc...) và màu chết.
    /// Nháy khi trúng KHÔNG nằm ở đây: nó là lớp phủ trắng riêng (child "HitFlash", xem HitFlashOverlay) nên
    /// không bao giờ làm đổi màu trạng thái (ADR-029, docs/14 §2).
    /// </summary>
    public readonly struct TintState
    {
        public readonly Color StateTint;
        public readonly bool IsDead;
        public readonly Color DeadTint;

        public TintState(Color stateTint, bool isDead, Color deadTint)
        {
            StateTint = stateTint;
            IsDead = isDead;
            DeadTint = deadTint;
        }
    }

    /// <summary>
    /// Giải màu nhân cuối cùng của sprite. Thứ tự ưu tiên: chết &gt; màu trạng thái hiện tại (gồm màu gốc = trắng).
    /// Màu trạng thái được trả NGUYÊN VẸN, kể cả alpha; việc "nháy khi trúng" không đi qua hàm này.
    /// </summary>
    public static class TintResolver
    {
        public static Color Resolve(in TintState state)
        {
            return state.IsDead ? state.DeadTint : state.StateTint;
        }
    }
}
