using System;
using System.Collections.Generic;

namespace Game.UI.Zones
{
    /// <summary>
    /// Luật hiện banner tên vùng (thuần logic, có test): mỗi vùng không hiện lại trong <see cref="CooldownSeconds"/> giây
    /// kể từ lần hiện gần nhất (chống chớp khi đứng ở biên hoặc vào lại tầng).
    /// </summary>
    public sealed class ZoneBannerCooldown
    {
        /// <summary>Không hiện lại cùng một vùng trong chừng này giây.</summary>
        public const float DefaultCooldownSeconds = 5f;

        private readonly float _cooldownSeconds;
        private readonly Dictionary<string, float> _lastShownAt = new Dictionary<string, float>(StringComparer.Ordinal);

        public ZoneBannerCooldown(float cooldownSeconds = DefaultCooldownSeconds)
        {
            _cooldownSeconds = cooldownSeconds < 0f ? 0f : cooldownSeconds;
        }

        /// <summary>
        /// True nếu được hiện banner cho vùng này ở thời điểm <paramref name="nowSeconds"/> (và ghi nhận đã hiện).
        /// ID rỗng thì không hiện.
        /// </summary>
        public bool TryShow(string zoneId, float nowSeconds)
        {
            if (string.IsNullOrEmpty(zoneId)) return false;

            if (_lastShownAt.TryGetValue(zoneId, out float last) && nowSeconds - last < _cooldownSeconds)
                return false;

            _lastShownAt[zoneId] = nowSeconds;
            return true;
        }

        public void Clear() => _lastShownAt.Clear();
    }

    /// <summary>Độ mờ của banner theo thời gian đã hiện (thuần logic): giữ rõ rồi mờ dần ở cuối.</summary>
    public static class ZoneBannerFade
    {
        public const float DefaultVisibleSeconds = 2f;
        public const float DefaultFadeSeconds = 0.5f;

        /// <summary>Alpha 0..1 tại <paramref name="elapsed"/> giây kể từ lúc hiện; 0 khi hết thời gian hiện.</summary>
        public static float Alpha(float elapsed, float visibleSeconds = DefaultVisibleSeconds, float fadeSeconds = DefaultFadeSeconds)
        {
            if (elapsed < 0f || visibleSeconds <= 0f) return 0f;
            if (elapsed >= visibleSeconds) return 0f;

            if (fadeSeconds <= 0f) return 1f;
            float fadeStart = visibleSeconds - fadeSeconds;
            if (fadeStart < 0f) fadeStart = 0f;
            if (elapsed <= fadeStart) return 1f;

            float t = (elapsed - fadeStart) / (visibleSeconds - fadeStart);
            return 1f - (t < 0f ? 0f : (t > 1f ? 1f : t));
        }
    }
}
