using System.Collections.Generic;

namespace Game.Core.Audio
{
    /// <summary>Luật chống spam SFX: khoảng cách tối thiểu cho mỗi id và trần số voice đồng thời. Thuần logic.</summary>
    public static class SfxThrottleRules
    {
        /// <summary>Số AudioSource SFX mặc định trong pool (docs/05: pool AudioSource).</summary>
        public const int DefaultMaxVoices = 8;

        /// <summary>True nếu lượt phát này đến quá sớm sau lần phát trước của cùng id. minInterval ≤ 0 = không giới hạn.</summary>
        public static bool IsTooSoon(double now, double lastPlayedAt, float minIntervalSeconds)
        {
            return minIntervalSeconds > 0f && (now - lastPlayedAt) < minIntervalSeconds;
        }

        public static bool HasFreeVoice(int activeVoices, int maxVoices)
        {
            return activeVoices < maxVoices;
        }
    }

    /// <summary>Ghi nhận thời điểm phát gần nhất của từng id để áp <see cref="SfxThrottleRules.IsTooSoon"/>.</summary>
    public sealed class SfxThrottleTracker
    {
        private readonly Dictionary<string, double> _lastPlayedAt = new Dictionary<string, double>();

        public bool IsTooSoon(string id, double now, float minIntervalSeconds)
        {
            if (id == null) return false;
            return _lastPlayedAt.TryGetValue(id, out double last)
                && SfxThrottleRules.IsTooSoon(now, last, minIntervalSeconds);
        }

        public void Record(string id, double now)
        {
            if (id != null) _lastPlayedAt[id] = now;
        }

        public void Clear()
        {
            _lastPlayedAt.Clear();
        }
    }
}
