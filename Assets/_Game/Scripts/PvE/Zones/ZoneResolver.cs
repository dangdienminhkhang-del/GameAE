using System.Collections.Generic;

namespace Game.PvE.Zones
{
    /// <summary>Một vùng đang chứa người chơi (dữ liệu thuần, không phụ thuộc Unity).</summary>
    public readonly struct ZoneCandidate
    {
        public readonly string Id;
        public readonly int Priority;
        /// <summary>Diện tích vùng; vùng chồng lấn thì vùng NHỎ hơn thắng khi cùng priority.</summary>
        public readonly float Area;

        public ZoneCandidate(string id, int priority, float area)
        {
            Id = id;
            Priority = priority;
            Area = area;
        }
    }

    /// <summary>Chọn vùng khi nhiều vùng chồng nhau (thuần logic).</summary>
    public static class ZoneResolver
    {
        /// <summary>
        /// Trả chỉ số vùng thắng trong danh sách các vùng đang CHỨA người chơi, hoặc -1 nếu danh sách rỗng.
        /// Thứ tự: priority cao hơn thắng → diện tích nhỏ hơn thắng → ID nhỏ hơn (so sánh ordinal, để kết quả ổn định).
        /// </summary>
        public static int Pick(IReadOnlyList<ZoneCandidate> containing)
        {
            if (containing == null) return -1;
            int best = -1;
            for (int i = 0; i < containing.Count; i++)
            {
                if (best < 0 || IsBetter(containing[i], containing[best])) best = i;
            }
            return best;
        }

        private static bool IsBetter(ZoneCandidate a, ZoneCandidate b)
        {
            if (a.Priority != b.Priority) return a.Priority > b.Priority;
            if (a.Area != b.Area) return a.Area < b.Area;
            return string.CompareOrdinal(a.Id, b.Id) < 0;
        }
    }

    /// <summary>
    /// Trạng thái theo dõi vùng của người chơi (thuần logic, ZoneTracker gọi mỗi nhịp ~0.1 giây).
    /// - Không vùng nào chứa người chơi → GIỮ vùng cũ, không phát sự kiện.
    /// - Sang vùng khác phải ở đó đủ <see cref="DefaultDwellTicks"/> nhịp liên tiếp mới đổi (chống chớp ở biên).
    /// - Chưa có vùng nào, hoặc yêu cầu "ngay" (vừa vào tầng): đổi ngay, và phát lại kể cả khi vùng không đổi.
    /// </summary>
    public sealed class ZoneTrackerLogic
    {
        public const int DefaultDwellTicks = 2;

        private readonly int _dwellTicks;
        private string _current;
        private string _pending;
        private int _pendingTicks;

        public ZoneTrackerLogic(int dwellTicks = DefaultDwellTicks)
        {
            _dwellTicks = dwellTicks < 1 ? 1 : dwellTicks;
        }

        /// <summary>ID vùng hiện tại; null nếu chưa có.</summary>
        public string Current => _current;

        /// <summary>
        /// Cập nhật một nhịp. Trả true và gán <paramref name="enteredId"/> khi người chơi vừa vào một vùng
        /// (cần phát ZoneEntered); trả false nếu không có gì mới.
        /// </summary>
        public bool Update(IReadOnlyList<ZoneCandidate> containing, bool immediate, out string enteredId)
        {
            enteredId = null;
            int index = ZoneResolver.Pick(containing);
            if (index < 0)
            {
                ClearPending();
                return false;
            }

            string bestId = containing[index].Id;

            if (immediate || _current == null)
            {
                _current = bestId;
                ClearPending();
                enteredId = bestId;
                return true;
            }

            if (string.Equals(bestId, _current, System.StringComparison.Ordinal))
            {
                ClearPending();
                return false;
            }

            if (string.Equals(bestId, _pending, System.StringComparison.Ordinal)) _pendingTicks++;
            else
            {
                _pending = bestId;
                _pendingTicks = 1;
            }

            if (_pendingTicks < _dwellTicks) return false;

            _current = bestId;
            ClearPending();
            enteredId = bestId;
            return true;
        }

        /// <summary>Quên vùng hiện tại (vùng cũ không còn tồn tại, ví dụ tầng bị tắt).</summary>
        public void Reset()
        {
            _current = null;
            ClearPending();
        }

        private void ClearPending()
        {
            _pending = null;
            _pendingTicks = 0;
        }
    }
}
