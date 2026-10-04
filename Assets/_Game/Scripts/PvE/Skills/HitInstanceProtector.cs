using System.Collections.Generic;

namespace Game.PvE.Skills
{
    /// <summary>
    /// Chống 1 target bị trùng hit nhiều lần trong cùng 1 activation của cùng 1 hit window.
    /// Mỗi activation → mỗi windowIdx → mỗi target → tối đa 1 hit.
    /// Reset mỗi lần bắt đầu Technique mới (mỗi TryUseTechnique đổi activationId).
    /// </summary>
    public class HitInstanceProtector
    {
        private long _currentActivationId;
        private readonly HashSet<long> _applied = new HashSet<long>();

        public long BeginActivation()
        {
            _currentActivationId++;
            _applied.Clear();
            return _currentActivationId;
        }

        public long CurrentActivationId => _currentActivationId;

        /// <summary>Trả về true nếu được phép gây damage; false = đã trúng rồi, bỏ qua.</summary>
        public bool TryMarkHit(int windowIdx, int targetInstanceId)
        {
            long key = (_currentActivationId << 32) |
                       ((uint)windowIdx << 16) |
                       (uint)targetInstanceId;
            return _applied.Add(key);
        }

        public void Clear()
        {
            _applied.Clear();
        }
    }
}
