using System.Collections.Generic;

namespace Game.Core.Guidance
{
    /// <summary>
    /// Kho cờ "đã xem" của hướng dẫn (Phase 9.9). Cờ là LOCAL, khóa theo playerId; không vào Player Data, không gọi server.
    /// Mọi hàm không được ném lỗi (kho hỏng thì hướng dẫn vẫn chạy, chỉ không nhớ được qua phiên).
    /// </summary>
    public interface ITutorialStore
    {
        bool HasSeen(string playerId, string hintId);

        /// <summary>Đánh dấu đã xem. Không làm gì nếu playerId hoặc hintId rỗng.</summary>
        void MarkSeen(string playerId, string hintId);

        /// <summary>
        /// Xóa cờ "đã xem" của playerId. CHỈ xóa khóa của gói hướng dẫn (không đụng khóa khác như settings.audio.*).
        /// <paramref name="knownHintIds"/> (tùy chọn) là danh mục hint để xóa chắc chắn cả khi chỉ mục bị mất.
        /// </summary>
        void ClearAll(string playerId, IEnumerable<string> knownHintIds);

        /// <summary>Công tắc "Gợi ý hướng dẫn". Mặc định bật.</summary>
        bool GetEnabled();

        void SetEnabled(bool enabled);
    }
}
