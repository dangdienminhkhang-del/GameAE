using System;
using Game.Core.Character;
using Game.Core.Events;
using Game.Core.Presentation;
using Game.PvE.Items;
using Game.UI.Common;
using Game.UI.Loading;
using Game.UI.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Pause
{
    /// <summary>
    /// Pause và nút Back (P9.6). Pause dùng PlaySpeedState.PushPause/PopPause (KHÔNG ghi Time.timeScale).
    /// Panel Pause có: Tiếp tục, Cài đặt, Về Main Menu. "Về Main Menu" bị khóa khi đang giao chiến để Pause không thành nút
    /// thoát hiểm (docs/09 §1.2). Đã vào Death flow thì KHÔNG pause nữa và Back không làm gì. Back không bao giờ thoát app.
    /// Luật nằm ở PauseRules/PauseGate/MainMenuQuitGate/BackKeyRouter (thuần, có test); lớp này chỉ nối dây.
    /// Thời gian thế giới (server) không bị đụng ở đây.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [Tooltip("Nút Pause trên màn hình cảm ứng (góc HUD). Không bắt buộc nhưng cần để người dùng cảm ứng mở Pause.")]
        [SerializeField] private Button pauseButton;
        [Tooltip("Chữ trạng thái dưới nút 'Về Main Menu' (hiện lý do bị khóa khi đang giao chiến).")]
        [SerializeField] private Text mainMenuStatusText;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private LoadingOverlay loadingOverlay;

        private readonly PauseGate _gate = new PauseGate();
        private bool _pushed;

        public bool IsPaused => _pushed;
        /// <summary>Object gốc của menu Pause (cho OutsideTapCloser xác định vùng cửa sổ).</summary>
        public GameObject PausePanelRoot => pausePanel;

        private void Awake()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resumeButton != null) resumeButton.onClick.AddListener(ClosePause);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
            if (pauseButton != null) pauseButton.onClick.AddListener(OpenPause);
            _gate.Reset();
        }

        private void OnDestroy()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(ClosePause);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            if (pauseButton != null) pauseButton.onClick.RemoveListener(OpenPause);
            ReleasePause();
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnPlayerDeathRecorded += HandleDeathRecorded;
            GameEvents.OnDeathRecordUnconfirmed += HandleDeathUnconfirmed;
            GameEvents.OnDeathStateUnavailable += HandleDeathUnavailable;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnPlayerDeathRecorded -= HandleDeathRecorded;
            GameEvents.OnDeathRecordUnconfirmed -= HandleDeathUnconfirmed;
            GameEvents.OnDeathStateUnavailable -= HandleDeathUnavailable;
            ReleasePause();
        }

        private void Update()
        {
            if (pauseButton != null) pauseButton.interactable = _gate.CanPause || _pushed;

            // Android: nút Back được Input System map thành Escape (cần xác minh trên máy thật).
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) HandleBack();
        }

        /// <summary>Back: đóng panel trên cùng, hoặc mở/đóng Pause. KHÔNG BAO GIỜ thoát app.</summary>
        public void HandleBack()
        {
            BackAction action = BackKeyRouterHost.Router.Handle(
                deathScreenShowing: _gate.DeathFlowStarted,
                isPauseOpen: _pushed,
                canPause: _gate.CanPause);

            if (action == BackAction.OpenPause) OpenPause();
            else if (action == BackAction.ClosePause) ClosePause();
        }

        public void OpenPause()
        {
            if (_pushed || !_gate.CanPause) return;

            PlaySpeedState.Shared.PushPause();
            _pushed = true;
            if (pausePanel != null) pausePanel.SetActive(true);
            RefreshMainMenuLock();
        }

        public void ClosePause()
        {
            if (settingsPanel != null && settingsPanel.IsOpen) settingsPanel.Close();
            if (pausePanel != null) pausePanel.SetActive(false);
            ReleasePause();
        }

        private void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.Open();
        }

        private void RefreshMainMenuLock()
        {
            ResolveInventory();
            bool inCombat = inventory != null && inventory.IsInCombat;
            if (mainMenuButton != null) mainMenuButton.interactable = MainMenuQuitGate.CanQuit(inCombat);
            if (mainMenuStatusText != null) mainMenuStatusText.text = MainMenuQuitGate.LabelFor(inCombat);
        }

        private void GoToMainMenu()
        {
            ResolveInventory();
            bool inCombat = inventory != null && inventory.IsInCombat;
            if (!MainMenuQuitGate.CanQuit(inCombat)) return;

            // Lưu tiến trình trước khi đổi scene, rồi trả tốc độ về 1 (không để timeScale kẹt ở 0 sang scene khác).
            if (inventory != null) inventory.FlushNow();
            _gate.IsLoading = true;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null && settingsPanel.IsOpen) settingsPanel.Close();
            ReleasePause();

            if (loadingOverlay != null)
            {
                loadingOverlay.Show();
                StartCoroutine(SceneLoader.LoadSceneAsync(SceneLoader.MainMenuSceneName, LoadSceneMode.Single, loadingOverlay));
            }
            else
            {
                SceneLoader.Load(SceneLoader.MainMenuSceneName);
            }
        }

        private void ResolveInventory()
        {
            if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
        }

        /// <summary>Trả đúng một lần PushPause đã đẩy (không bao giờ PopPause thừa).</summary>
        private void ReleasePause()
        {
            if (!_pushed) return;
            _pushed = false;
            PlaySpeedState.Shared.PopPause();
        }

        // ── Death flow: một khi đã chết thì KHÔNG pause nữa trong scene này ──

        private void HandlePlayerDied(object sender, Game.Core.Combat.DamageInfo lastHit) => EnterDeathFlow();
        private void HandleDeathRecorded(object sender, DeathInfo info) => EnterDeathFlow();
        private void HandleDeathUnconfirmed(object sender, DeathInfo info) => EnterDeathFlow();
        private void HandleDeathUnavailable(object sender, EventArgs e) => EnterDeathFlow();

        private void EnterDeathFlow()
        {
            _gate.MarkDeathFlow();
            if (_pushed) ClosePause();
        }
    }
}
