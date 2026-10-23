using System.Collections.Generic;
using Game.Core.Guidance;
using Game.Core.Presentation;
using Game.UI.Boss;
using UnityEngine;

namespace Game.UI.Guidance
{
    /// <summary>
    /// Bộ định tuyến hướng dẫn (Phase 9.9): nhận tín hiệu từ <see cref="TutorialSensors"/>, chọn hint trong danh mục
    /// <see cref="GuidanceConfig"/> theo trigger, đưa vào <see cref="TutorialRules"/> và cho <see cref="TutorialToastPresenter"/> hiển thị.
    /// Không chứa luật: hàng đợi, "một lần", khoảng cách, thời hạn chờ đều ở TutorialRules; ở đây chỉ nối dây.
    /// Bị chặn (Pause, người chơi chết, bảng báo boss đang mở, chưa có playerId) thì hint giữ chờ rồi bỏ, KHÔNG đánh dấu đã xem.
    /// Đặt MỘT GameObject (luôn bật) mang router; cảm biến và toast tự được thêm vào cùng GameObject nếu chưa gán.
    /// </summary>
    public sealed class TutorialTriggerRouter : MonoBehaviour
    {
        private const float PlayerIdRefreshSeconds = 1f;

        [SerializeField] private GuidanceConfig config;
        [Tooltip("Để trống = tự lấy/thêm trên cùng GameObject.")]
        [SerializeField] private TutorialSensors sensors;
        [Tooltip("Để trống = tự lấy/thêm trên cùng GameObject.")]
        [SerializeField] private TutorialToastPresenter presenter;

        private readonly List<TutorialHint> _scratch = new List<TutorialHint>(4);
        private TutorialRules _rules;
        private float _nextPlayerIdRefreshAt;

        public TutorialRules Rules => _rules;

        private void Awake()
        {
            if (sensors == null && !TryGetComponent(out sensors)) sensors = gameObject.AddComponent<TutorialSensors>();
            if (presenter == null && !TryGetComponent(out presenter)) presenter = gameObject.AddComponent<TutorialToastPresenter>();

            if (config != null)
            {
                sensors.SetConfig(config);
                presenter.SetConfig(config);
            }
        }

        private void OnEnable()
        {
            _rules = new TutorialRules(TutorialRuntime.Store, config != null ? config.Rules : TutorialRuleSettings.Default);
            _rules.PlayerId = TutorialRuntime.CurrentPlayerId;
            if (config != null) TutorialRuntime.RegisterKnownHintIds(config.HintIds());

            sensors.TriggerRaised += HandleTrigger;
            sensors.CompleteRaised += HandleComplete;
            TutorialRuntime.EnabledChanged += HandleEnabledChanged;
            TutorialRuntime.SeenReset += HandleSeenReset;
        }

        private void OnDisable()
        {
            if (sensors != null)
            {
                sensors.TriggerRaised -= HandleTrigger;
                sensors.CompleteRaised -= HandleComplete;
            }
            TutorialRuntime.EnabledChanged -= HandleEnabledChanged;
            TutorialRuntime.SeenReset -= HandleSeenReset;

            _rules?.ResetState(UnityEngine.Time.unscaledTimeAsDouble, presenter);
            _rules = null;
        }

        private void Update()
        {
            if (_rules == null) return;

            double now = UnityEngine.Time.unscaledTimeAsDouble;
            if (now >= _nextPlayerIdRefreshAt)
            {
                _nextPlayerIdRefreshAt = (float)now + PlayerIdRefreshSeconds;
                _rules.PlayerId = TutorialRuntime.CurrentPlayerId;
            }

            bool blocked = PlaySpeedState.Shared.IsPaused ||
                           (sensors != null && sensors.IsPlayerDead) ||
                           BossDefeatReportPanel.CurrentState != BossDefeatReportPanelState.Hidden;
            _rules.Update(now, blocked, presenter);
        }

        private void HandleTrigger(TutorialTrigger trigger)
        {
            if (_rules == null || config == null) return;

            _scratch.Clear();
            TutorialTriggerMap.Collect(config.Hints, trigger, _scratch);

            double now = UnityEngine.Time.unscaledTimeAsDouble;
            for (int i = 0; i < _scratch.Count; i++) _rules.Request(_scratch[i], now);
        }

        private void HandleComplete(TutorialComplete kind)
        {
            _rules?.Notify(kind, UnityEngine.Time.unscaledTimeAsDouble, presenter);
        }

        private void HandleEnabledChanged()
        {
            _rules?.SetEnabled(TutorialRuntime.Enabled);
        }

        private void HandleSeenReset()
        {
            if (_rules == null) return;

            _rules.ResetState(UnityEngine.Time.unscaledTimeAsDouble, presenter);
            if (sensors != null) sensors.ResetOneShots();

            // Gợi ý "sẵn sàng" (di chuyển) chỉ phát một lần mỗi phiên: phát lại để nút "Xem lại hướng dẫn" có tác dụng ngay.
            HandleTrigger(TutorialTrigger.PlayerReady);
        }
    }
}
