using System.Collections.Generic;

namespace Game.Core.Quest
{
    /// <summary>
    /// Sổ thưởng quest chờ cấp — thuần logic, không biết Unity/save/UI.
    ///
    /// Quy tắc (Phase 6 tiêu chí 3): thưởng chỉ được cấp SAU KHI lần lưu chứa trạng thái Completed
    /// thành công. Nếu app bị đóng giữa lúc item/EXP đã lưu mà quest chưa lưu, lần mở sau quest vẫn
    /// ReadyToTurnIn và người chơi sẽ nhận thưởng lần hai — sổ này chặn tình huống đó.
    ///
    /// Vòng đời một lần lưu (writer lưu tuần tự, chỉ một lần lưu tại một thời điểm):
    ///   Enqueue(quest)   — lúc turn-in, đánh dấu Completed trong bộ nhớ
    ///   BeginSave()      — ngay khi chụp snapshot: các thưởng đang chờ thuộc về snapshot này
    ///   CommitSave()     — lưu thành công → trả danh sách thưởng được phép cấp
    ///   RollbackSave()   — lưu lỗi → thưởng quay lại hàng chờ, không mất, không cấp
    ///   Clear()          — chết / khóa lưu / hủy → bỏ toàn bộ thưởng đang chờ
    ///
    /// Thưởng được Enqueue SAU BeginSave (trong lúc lần lưu trước còn đang chạy) KHÔNG nằm trong lần
    /// CommitSave đó, vì snapshot của lần lưu ấy chưa chứa trạng thái Completed của nó.
    /// </summary>
    public sealed class QuestRewardLedger
    {
        private readonly List<QuestDefinition> _pending = new List<QuestDefinition>();
        private readonly List<QuestDefinition> _inFlight = new List<QuestDefinition>();

        /// <summary>Số thưởng đang chờ lần lưu kế tiếp.</summary>
        public int PendingCount => _pending.Count;

        /// <summary>Số thưởng thuộc lần lưu đang chạy (chưa biết thành công hay lỗi).</summary>
        public int InFlightCount => _inFlight.Count;

        public void Enqueue(QuestDefinition quest)
        {
            if (quest == null) return;
            _pending.Add(quest);
        }

        /// <summary>Gọi ngay khi chụp snapshot. Chuyển thưởng đang chờ sang trạng thái "đang lưu".</summary>
        public void BeginSave()
        {
            if (_pending.Count == 0) return;
            _inFlight.AddRange(_pending);
            _pending.Clear();
        }

        /// <summary>Lưu thành công: trả (và xóa) danh sách thưởng được phép cấp.</summary>
        public IReadOnlyList<QuestDefinition> CommitSave()
        {
            if (_inFlight.Count == 0) return System.Array.Empty<QuestDefinition>();
            var granted = new List<QuestDefinition>(_inFlight);
            _inFlight.Clear();
            return granted;
        }

        /// <summary>Lưu lỗi: thưởng quay lại ĐẦU hàng chờ (giữ đúng thứ tự), không cấp.</summary>
        public void RollbackSave()
        {
            if (_inFlight.Count == 0) return;
            _pending.InsertRange(0, _inFlight);
            _inFlight.Clear();
        }

        /// <summary>Bỏ toàn bộ thưởng đang chờ và đang lưu (chết, khóa lưu, hủy).</summary>
        public void Clear()
        {
            _pending.Clear();
            _inFlight.Clear();
        }
    }
}
