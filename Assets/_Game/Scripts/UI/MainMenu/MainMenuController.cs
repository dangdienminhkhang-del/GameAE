using Game.UI.Common;
using Game.UI.Loading;
using Game.UI.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playPvEButton;
        [SerializeField] private Button playPvPButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [Tooltip("Panel Cài đặt (P9.6). Thiếu thì nút Cài đặt không làm gì.")]
        [SerializeField] private SettingsPanel settingsPanel;
        [Tooltip("Màn tải nhẹ (P9.6). Thiếu thì vào game bằng cách cũ (không có tiến độ).")]
        [SerializeField] private LoadingOverlay loadingOverlay;

        private bool _loading;

        private void Awake()
        {
            if (playPvEButton != null) playPvEButton.onClick.AddListener(HandlePlayPvEClicked);
            if (playPvPButton != null) playPvPButton.onClick.AddListener(HandlePlayPvPClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(HandleSettingsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(HandleQuitClicked);
        }

        private void OnDestroy()
        {
            if (playPvEButton != null) playPvEButton.onClick.RemoveListener(HandlePlayPvEClicked);
            if (playPvPButton != null) playPvPButton.onClick.RemoveListener(HandlePlayPvPClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(HandleSettingsClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuitClicked);
        }

        private void HandlePlayPvEClicked()
        {
            // Phase 6.5: nút Play PvE vào thẳng scene gameplay. Dùng hằng số chung để không lệch tên scene.
            if (_loading) return;
            if (loadingOverlay == null)
            {
                SceneLoader.Load(SceneLoader.PvESceneName);
                return;
            }

            _loading = true;
            loadingOverlay.Show();
            StartCoroutine(SceneLoader.LoadSceneAsync(SceneLoader.PvESceneName, LoadSceneMode.Single, loadingOverlay));
        }

        private void HandlePlayPvPClicked()
        {
            Debug.Log("[MainMenu] Play PvP clicked (placeholder - Phase 10 implement)");
        }

        private void HandleSettingsClicked()
        {
            if (settingsPanel != null) settingsPanel.Open();
        }

        private void HandleQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
