using Game.Core.Settings;
using Game.UI.Guidance;
using Game.UI.Pause;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Settings
{
    /// <summary>
    /// Panel Cài đặt (P9.6): âm lượng nhạc và hiệu ứng, bật/tắt rung, joystick nổi, dead zone, độ nhạy.
    /// Mở được từ Pause và từ Main Menu. Thay đổi có hiệu lực ngay và được lưu local (PlayerPrefs) qua <see cref="SettingsRuntime"/>.
    /// TUYỆT ĐỐI không có nút Reset Guest, xóa tài khoản hay đăng xuất ở đây (AGENTS.md luật 10).
    /// Nút Back đóng panel này trước (priority cao nhất trong <see cref="BackKeyRouter"/>).
    /// </summary>
    public class SettingsPanel : MonoBehaviour, IBackHandler
    {
        /// <summary>Settings nằm trên cùng: Back đóng nó trước Túi, Quest, Chọn Tầng.</summary>
        public const int Priority = 40;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle hapticsToggle;
        [Tooltip("Bật = joystick nổi (Floating); tắt = cố định.")]
        [SerializeField] private Toggle floatingJoystickToggle;
        [SerializeField] private Slider deadZoneSlider;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Button closeButton;

        [Header("Hướng dẫn (Phase 9.9) - có thể để trống, scene cũ không vỡ")]
        [Tooltip("Công tắt 'Gợi ý hướng dẫn'. Tắt có hiệu lực ngay.")]
        [SerializeField] private Toggle tutorialHintsToggle;
        [Tooltip("Nút 'Xem lại hướng dẫn': chỉ xóa cờ gợi ý local của người chơi hiện tại.")]
        [SerializeField] private Button replayTutorialButton;
        [Tooltip("Dòng chữ xác nhận ngắn hiện sau khi bấm 'Xem lại hướng dẫn'.")]
        [SerializeField] private Text replayConfirmLabel;
        [SerializeField] private float replayConfirmSeconds = 2f;

        /// <summary>Chữ xác nhận sau khi đặt lại gợi ý.</summary>
        public const string ReplayConfirmMessage = "Đã đặt lại gợi ý";

        private float _replayConfirmHideAt;

        private bool _refreshing;

        public int BackPriority => Priority;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;

            // Khoảng giá trị lấy từ SettingsRules (không có số trong UI).
            ConfigureSlider(musicSlider, 0f, 1f);
            ConfigureSlider(sfxSlider, 0f, 1f);
            ConfigureSlider(deadZoneSlider, SettingsRules.MinDeadZone, SettingsRules.MaxDeadZone);
            ConfigureSlider(sensitivitySlider, SettingsRules.MinSensitivity, SettingsRules.MaxSensitivity);

            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnAnyChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnAnyChanged);
            if (deadZoneSlider != null) deadZoneSlider.onValueChanged.AddListener(OnAnyChanged);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnAnyChanged);
            if (hapticsToggle != null) hapticsToggle.onValueChanged.AddListener(OnAnyChanged);
            if (floatingJoystickToggle != null) floatingJoystickToggle.onValueChanged.AddListener(OnAnyChanged);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (tutorialHintsToggle != null) tutorialHintsToggle.onValueChanged.AddListener(OnTutorialHintsChanged);
            if (replayTutorialButton != null) replayTutorialButton.onClick.AddListener(OnReplayTutorialClicked);
            if (replayConfirmLabel != null) replayConfirmLabel.gameObject.SetActive(false);

            panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnAnyChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnAnyChanged);
            if (deadZoneSlider != null) deadZoneSlider.onValueChanged.RemoveListener(OnAnyChanged);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveListener(OnAnyChanged);
            if (hapticsToggle != null) hapticsToggle.onValueChanged.RemoveListener(OnAnyChanged);
            if (floatingJoystickToggle != null) floatingJoystickToggle.onValueChanged.RemoveListener(OnAnyChanged);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (tutorialHintsToggle != null) tutorialHintsToggle.onValueChanged.RemoveListener(OnTutorialHintsChanged);
            if (replayTutorialButton != null) replayTutorialButton.onClick.RemoveListener(OnReplayTutorialClicked);
        }

        private void OnEnable()
        {
            BackKeyRouterHost.Router.Register(this);
        }

        private void OnDisable()
        {
            BackKeyRouterHost.Router.Unregister(this);
        }

        public void Open()
        {
            if (panelRoot == null) return;
            Refresh(SettingsRuntime.Current);
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void CloseFromBack() => Close();

        private static void ConfigureSlider(Slider slider, float min, float max)
        {
            if (slider == null) return;
            slider.wholeNumbers = false;
            slider.minValue = min;
            slider.maxValue = max;
        }

        /// <summary>Đưa giá trị hiện tại lên các điều khiển mà không kích hoạt lưu lại.</summary>
        private void Refresh(PlayerSettings s)
        {
            _refreshing = true;
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(s.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(s.SfxVolume);
            if (deadZoneSlider != null) deadZoneSlider.SetValueWithoutNotify(s.DeadZone);
            if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(s.Sensitivity);
            if (hapticsToggle != null) hapticsToggle.SetIsOnWithoutNotify(s.HapticsEnabled);
            if (floatingJoystickToggle != null) floatingJoystickToggle.SetIsOnWithoutNotify(s.Joystick == JoystickMode.Floating);
            if (tutorialHintsToggle != null) tutorialHintsToggle.SetIsOnWithoutNotify(TutorialRuntime.Enabled);
            _refreshing = false;
        }

        private void OnTutorialHintsChanged(bool enabled)
        {
            if (_refreshing) return;
            // Công tắt có hiệu lực ngay: TutorialRuntime ghi kho local và báo cho router ẩn toast đang hiện.
            TutorialRuntime.SetEnabled(enabled);
        }

        private void OnReplayTutorialClicked()
        {
            // CHỈ xóa cờ gợi ý local của playerId hiện tại (không đụng âm lượng, joystick hay tài khoản).
            TutorialRuntime.ResetSeen();
            if (replayConfirmLabel == null) return;
            replayConfirmLabel.text = ReplayConfirmMessage;
            replayConfirmLabel.gameObject.SetActive(true);
            _replayConfirmHideAt = UnityEngine.Time.unscaledTime + replayConfirmSeconds;
        }

        private void Update()
        {
            if (replayConfirmLabel != null && replayConfirmLabel.gameObject.activeSelf &&
                UnityEngine.Time.unscaledTime >= _replayConfirmHideAt)
            {
                replayConfirmLabel.gameObject.SetActive(false);
            }
        }

        private void OnAnyChanged(float _) => PushToRuntime();
        private void OnAnyChanged(bool _) => PushToRuntime();

        private void PushToRuntime()
        {
            if (_refreshing) return;
            PlayerSettings current = SettingsRuntime.Current;
            var updated = new PlayerSettings(
                musicSlider != null ? musicSlider.value : current.MusicVolume,
                sfxSlider != null ? sfxSlider.value : current.SfxVolume,
                hapticsToggle != null ? hapticsToggle.isOn : current.HapticsEnabled,
                floatingJoystickToggle != null
                    ? (floatingJoystickToggle.isOn ? JoystickMode.Floating : JoystickMode.Fixed)
                    : current.Joystick,
                deadZoneSlider != null ? deadZoneSlider.value : current.DeadZone,
                sensitivitySlider != null ? sensitivitySlider.value : current.Sensitivity);

            SettingsRuntime.Apply(updated);
        }
    }
}
