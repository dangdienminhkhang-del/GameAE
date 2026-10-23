using System;
using System.Collections.Generic;

namespace Game.Core.Guidance
{
    /// <summary>Dữ liệu một gợi ý đã tách khỏi ScriptableObject để luật thuần test được không cần asset.</summary>
    public readonly struct TutorialHint
    {
        public readonly string Id;
        public readonly string Text;
        public readonly string SpeakerLabel;
        public readonly int Priority;
        public readonly float DisplaySeconds;
        public readonly TutorialComplete CompleteKind;

        public TutorialHint(string id, string text, string speakerLabel, int priority, float displaySeconds, TutorialComplete completeKind)
        {
            Id = id;
            Text = text;
            SpeakerLabel = speakerLabel;
            Priority = priority;
            DisplaySeconds = displaySeconds;
            CompleteKind = completeKind;
        }
    }

    /// <summary>Nơi hiển thị toast. <see cref="TryShow"/> trả true CHỈ khi toast thật sự hiện (lúc đó hint mới được đánh dấu đã xem).</summary>
    public interface ITutorialToastSink
    {
        bool TryShow(TutorialHint hint);
        void Hide();
    }

    public enum TutorialRequestResult
    {
        Queued = 0,
        /// <summary>Công tắc đang tắt.</summary>
        Disabled = 1,
        /// <summary>Người chơi này đã xem gợi ý này rồi.</summary>
        AlreadySeen = 2,
        /// <summary>Đang hiện hoặc đang chờ.</summary>
        Duplicate = 3,
        /// <summary>Hàng đợi đầy và gợi ý này không quan trọng hơn cái nào đang chờ.</summary>
        Dropped = 4,
        /// <summary>ID rỗng.</summary>
        Invalid = 5
    }

    /// <summary>
    /// Luật hiển thị gợi ý (Phase 9.9), thuần logic: không biết Unity, UI hay PlayerPrefs.
    /// - Mỗi hint chỉ hiện MỘT lần cho mỗi playerId; chỉ được đánh dấu "đã xem" khi toast thật sự hiện.
    /// - Công tắc tắt: không hiện, không đánh dấu, xóa hàng đợi và ẩn toast đang hiện.
    /// - Hàng đợi sắp theo priority (số lớn trước) rồi thời điểm; không trùng ID; tối đa <see cref="TutorialRuleSettings.MaxQueued"/> chờ.
    /// - Một toast một lúc, cách nhau tối thiểu <see cref="TutorialRuleSettings.MinGapSeconds"/>.
    /// - Bị chặn (Pause, chết, bảng báo boss mở, chưa có playerId): giữ chờ tối đa <see cref="TutorialRuleSettings.BlockedTimeoutSeconds"/> giây rồi bỏ, KHÔNG đánh dấu.
    /// - Hint có completeKind tự ẩn sớm khi người chơi làm việc đó; nếu không thì ẩn sau displaySeconds.
    /// Thời gian do người gọi truyền vào (giây, thời gian thật), nên test không cần chờ.
    /// </summary>
    public sealed class TutorialRules
    {
        private struct Pending
        {
            public TutorialHint Hint;
            public double EnqueuedAt;
            public long Order;
        }

        private readonly ITutorialStore _store;
        private readonly TutorialRuleSettings _settings;
        private readonly List<Pending> _queue = new List<Pending>(4);

        private bool _enabled;
        private bool _hasActive;
        private TutorialHint _active;
        private double _shownAt;
        private double _lastHiddenAt = double.NegativeInfinity;
        private long _order;

        public TutorialRules(ITutorialStore store, TutorialRuleSettings settings)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _settings = settings;
            _enabled = store.GetEnabled();
        }

        /// <summary>Người chơi hiện tại. Rỗng = chưa biết → coi như bị chặn.</summary>
        public string PlayerId { get; set; }

        public bool Enabled => _enabled;
        public bool HasActive => _hasActive;
        public TutorialHint Active => _active;
        public int QueuedCount => _queue.Count;

        public bool IsQueued(string hintId)
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i].Hint.Id == hintId) return true;
            }
            return false;
        }

        /// <summary>Công tắc bật/tắt. KHÔNG ghi kho (TutorialRuntime ghi); tắt thì xóa hàng đợi, toast đang hiện bị ẩn ở lần Update kế tiếp.</summary>
        public void SetEnabled(bool value)
        {
            _enabled = value;
            if (!value) _queue.Clear();
        }

        public TutorialRequestResult Request(TutorialHint hint, double now)
        {
            if (string.IsNullOrEmpty(hint.Id)) return TutorialRequestResult.Invalid;
            if (!_enabled) return TutorialRequestResult.Disabled;
            if (!string.IsNullOrEmpty(PlayerId) && _store.HasSeen(PlayerId, hint.Id)) return TutorialRequestResult.AlreadySeen;
            if ((_hasActive && _active.Id == hint.Id) || IsQueued(hint.Id)) return TutorialRequestResult.Duplicate;

            var pending = new Pending { Hint = hint, EnqueuedAt = now, Order = _order++ };

            if (_queue.Count >= _settings.MaxQueued)
            {
                int worst = IndexOfLeastImportant();
                // Chỉ thay khi gợi ý mới QUAN TRỌNG HƠN hẳn (priority lớn hơn); bằng nhau thì giữ cái đến trước.
                if (worst < 0 || hint.Priority <= _queue[worst].Hint.Priority) return TutorialRequestResult.Dropped;
                _queue.RemoveAt(worst);
            }

            _queue.Add(pending);
            return TutorialRequestResult.Queued;
        }

        /// <summary>Người chơi vừa làm một việc: hint đang hiện cùng loại tự ẩn sớm; hint cùng loại đang chờ bị bỏ (không đánh dấu).</summary>
        public void Notify(TutorialComplete kind, double now, ITutorialToastSink sink)
        {
            if (kind == TutorialComplete.None) return;

            if (_hasActive && _active.CompleteKind == kind) HideActive(now, sink);

            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                if (_queue[i].Hint.CompleteKind == kind) _queue.RemoveAt(i);
            }
        }

        /// <summary>Gọi thường xuyên (mỗi frame được). <paramref name="blocked"/> = Pause / chết / bảng báo boss đang mở.</summary>
        public void Update(double now, bool blocked, ITutorialToastSink sink)
        {
            if (!_enabled)
            {
                _queue.Clear();
                if (_hasActive) HideActive(now, sink);
                return;
            }

            if (_hasActive && now - _shownAt >= _active.DisplaySeconds) HideActive(now, sink);

            bool effectivelyBlocked = blocked || string.IsNullOrEmpty(PlayerId);
            if (effectivelyBlocked)
            {
                for (int i = _queue.Count - 1; i >= 0; i--)
                {
                    if (now - _queue[i].EnqueuedAt >= _settings.BlockedTimeoutSeconds) _queue.RemoveAt(i);
                }
                return;
            }

            if (_hasActive) return;
            if (now - _lastHiddenAt < _settings.MinGapSeconds) return;

            while (_queue.Count > 0)
            {
                int best = IndexOfMostImportant();
                Pending next = _queue[best];
                _queue.RemoveAt(best);

                // Có thể đã được xem ở phiên/máy khác trong lúc chờ: bỏ, thử cái kế.
                if (_store.HasSeen(PlayerId, next.Hint.Id)) continue;

                if (sink == null || !sink.TryShow(next.Hint))
                {
                    // Chưa hiện được (UI chưa sẵn sàng): trả về hàng đợi, KHÔNG đánh dấu, thử lại lần Update sau.
                    _queue.Add(next);
                    return;
                }

                _store.MarkSeen(PlayerId, next.Hint.Id);
                _active = next.Hint;
                _hasActive = true;
                _shownAt = now;
                return;
            }
        }

        /// <summary>Về trạng thái sạch (đổi người chơi, "Xem lại hướng dẫn"): xóa hàng đợi và ẩn toast đang hiện.</summary>
        public void ResetState(double now, ITutorialToastSink sink)
        {
            _queue.Clear();
            if (_hasActive) HideActive(now, sink);
            _lastHiddenAt = double.NegativeInfinity;
        }

        private void HideActive(double now, ITutorialToastSink sink)
        {
            _hasActive = false;
            _lastHiddenAt = now;
            sink?.Hide();
        }

        private int IndexOfMostImportant()
        {
            int best = 0;
            for (int i = 1; i < _queue.Count; i++)
            {
                if (IsMoreImportant(_queue[i], _queue[best])) best = i;
            }
            return best;
        }

        private int IndexOfLeastImportant()
        {
            if (_queue.Count == 0) return -1;
            int worst = 0;
            for (int i = 1; i < _queue.Count; i++)
            {
                if (IsMoreImportant(_queue[worst], _queue[i])) worst = i;
            }
            return worst;
        }

        private static bool IsMoreImportant(Pending a, Pending b)
        {
            if (a.Hint.Priority != b.Hint.Priority) return a.Hint.Priority > b.Hint.Priority;
            if (a.EnqueuedAt != b.EnqueuedAt) return a.EnqueuedAt < b.EnqueuedAt;
            return a.Order < b.Order;
        }
    }
}
