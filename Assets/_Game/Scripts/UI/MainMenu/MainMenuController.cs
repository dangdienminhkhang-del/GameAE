using Game.UI.Common;
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
            // Phase 6.5: nút Play PvE vào thẳng scene gameplay (trước đây chỉ Debug.Log placeholder).
            SceneManager.LoadScene("PvE_Gameplay");
        }

        private void HandlePlayPvPClicked()
        {
            Debug.Log("[MainMenu] Play PvP clicked (placeholder - Phase 10 implement)");
        }

        private void HandleSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings clicked (placeholder)");
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
