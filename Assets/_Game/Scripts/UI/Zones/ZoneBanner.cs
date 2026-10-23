using Game.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Zones
{
    /// <summary>
    /// Banner tên vùng (Phase 9.7a): hiện displayName của vùng người chơi vừa vào khoảng 2 giây rồi mờ dần.
    /// Không phải popup: không có nút, không chặn chạm (CanvasGroup không nhận raycast). Cùng một vùng không hiện lại trong 5 giây.
    /// Chạy bằng thời gian THẬT (unscaled) để Pause không làm banner kẹt. Chỉ hiển thị, không chứa logic game.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ZoneBanner : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private float visibleSeconds = ZoneBannerFade.DefaultVisibleSeconds;
        [SerializeField] private float fadeSeconds = ZoneBannerFade.DefaultFadeSeconds;
        [SerializeField] private float cooldownSeconds = ZoneBannerCooldown.DefaultCooldownSeconds;

        private CanvasGroup _group;
        private ZoneBannerCooldown _cooldown;
        private float _shownAt;
        private bool _showing;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _cooldown = new ZoneBannerCooldown(cooldownSeconds);
        }

        private void OnEnable()
        {
            GameEvents.OnZoneEntered += HandleZoneEntered;
        }

        private void OnDisable()
        {
            GameEvents.OnZoneEntered -= HandleZoneEntered;
            _showing = false;
            if (_group != null) _group.alpha = 0f;
        }

        private void HandleZoneEntered(object sender, ZoneEnteredArgs args)
        {
            if (args == null) return;
            float now = UnityEngine.Time.unscaledTime;
            if (!_cooldown.TryShow(args.ZoneId, now)) return;

            if (label != null) label.text = string.IsNullOrEmpty(args.DisplayName) ? args.ZoneId : args.DisplayName;
            _shownAt = now;
            _showing = true;
        }

        private void Update()
        {
            if (!_showing) return;

            float elapsed = UnityEngine.Time.unscaledTime - _shownAt;
            float alpha = ZoneBannerFade.Alpha(elapsed, visibleSeconds, fadeSeconds);
            _group.alpha = alpha;
            if (elapsed >= visibleSeconds) _showing = false;
        }
    }
}
