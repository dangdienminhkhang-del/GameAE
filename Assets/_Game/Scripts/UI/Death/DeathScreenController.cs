using System;
using Game.Core.Account;
using Game.Core.Character;
using Game.Core.Events;
using Game.Services;
using Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Death
{
    /// <summary>
    /// Death Screen (docs/05 Phase 5.5, docs/09 §1): hiện khi player chết vĩnh viễn.
    /// Hiển thị nguyên nhân / level / tầng cao nhất / thời gian chơi.
    /// Guest → nút "Tạo tài khoản mới"; Official → chỉ "Thoát".
    /// Cũng dùng để chặn vào game khi KHÔNG xác minh được trạng thái tài khoản (fail-closed, docs/03 §12).
    /// UI chỉ hiển thị + gửi ý định; logic death/reset nằm ở Core/Services.
    /// </summary>
    public class DeathScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Button newAccountButton;
        [SerializeField] private Button quitButton;

        private IAccountResetService _resetService;
        private bool _isResetting;

        private void Awake()
        {
            _resetService = ServiceProvider.CreateAccountResetService();

            if (panelRoot != null) panelRoot.SetActive(false);
            if (newAccountButton != null) newAccountButton.onClick.AddListener(HandleNewAccount);
            if (quitButton != null) quitButton.onClick.AddListener(HandleQuit);
        }

        private void OnDestroy()
        {
            if (newAccountButton != null) newAccountButton.onClick.RemoveListener(HandleNewAccount);
            if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuit);
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDeathRecorded += HandleDeathRecorded;
            GameEvents.OnDeathStateUnavailable += HandleDeathStateUnavailable;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDeathRecorded -= HandleDeathRecorded;
            GameEvents.OnDeathStateUnavailable -= HandleDeathStateUnavailable;
        }

        private void HandleDeathRecorded(object sender, DeathInfo info)
        {
            Show(info);
        }

        private void HandleDeathStateUnavailable(object sender, EventArgs e)
        {
            ShowUnavailable();
        }

        public void Show(DeathInfo info)
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (titleText != null) titleText.text = "BẠN ĐÃ CHẾT";

            if (detailsText != null)
            {
                string cause = info != null ? info.deathCause : "?";
                int level = info != null && info.finalLevel > 0 ? info.finalLevel : 1;
                int floor = info != null && info.finalHighestFloor > 0 ? info.finalHighestFloor : 1;
                int seconds = info != null ? info.totalPlayTimeSeconds : 0;

                detailsText.text =
                    $"Nguyên nhân: {TranslateCause(cause)}\n" +
                    $"Level cuối: {level}\n" +
                    $"Tầng cao nhất: {floor}\n" +
                    $"Thời gian chơi: {seconds / 60:00}:{seconds % 60:00}";
            }

            bool canReset = _resetService == null || _resetService.CanResetCurrentAccount();
            if (newAccountButton != null)
            {
                newAccountButton.gameObject.SetActive(canReset);
                newAccountButton.interactable = true;
            }

            if (!canReset && detailsText != null)
            {
                detailsText.text += "\n\n(Tài khoản Official hoặc đã liên kết — không thể tạo tài khoản mới)";
            }
        }

        /// <summary>
        /// Không xác minh được tài khoản đã chết hay chưa → KHÔNG cho chơi (không mặc định "còn sống").
        /// Không có nút "Tạo tài khoản mới" ở đây: đây không phải cái chết. Người chơi kiểm tra mạng rồi mở lại app.
        /// </summary>
        public void ShowUnavailable()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (titleText != null) titleText.text = "KHÔNG THỂ XÁC MINH TÀI KHOẢN";
            if (detailsText != null)
            {
                detailsText.text = "Không đọc được dữ liệu tài khoản.\nHãy kiểm tra kết nối mạng rồi mở lại game.";
            }
            if (newAccountButton != null) newAccountButton.gameObject.SetActive(false);
        }

        private static string TranslateCause(string cause)
        {
            if (cause == DeathReason.Enemy.ToString()) return "Quái vật";
            if (cause == DeathReason.Boss.ToString()) return "Boss";
            if (cause == DeathReason.Trap.ToString()) return "Bẫy";
            return cause;
        }

        public async void HandleNewAccount()
        {
            // Chặn bấm 2 lần (mỗi lần tạo một Player ID mới) và chặn Official/đã link:
            // HandleNewAccount là public nên không thể chỉ tin vào việc nút bị ẩn.
            if (_isResetting) return;
            if (_resetService == null || !_resetService.CanResetCurrentAccount()) return;

            _isResetting = true;
            if (newAccountButton != null) newAccountButton.interactable = false;

            try
            {
                // ResetGuestAccountAsync: đăng xuất → đăng nhập ẩn danh mới → xóa MỌI Player Data cũ
                // (kể cả trạng thái chết) để nhân vật mới bắt đầu từ đầu.
                bool ok = await _resetService.ResetGuestAccountAsync();
                if (ok)
                {
                    SceneLoader.Load(SceneLoader.BootSceneName);
                    return;
                }

                Debug.LogError("[DeathScreen] Tạo tài khoản mới thất bại.");
                if (detailsText != null) detailsText.text += "\n\nTạo tài khoản mới thất bại. Hãy kiểm tra mạng rồi thử lại.";
            }
            catch (Exception error)
            {
                Debug.LogError($"[DeathScreen] Tạo tài khoản mới lỗi: {error}");
                if (detailsText != null) detailsText.text += "\n\nCó lỗi khi tạo tài khoản mới. Hãy thử lại.";
            }

            // Chỉ tới đây khi thất bại → cho bấm lại.
            _isResetting = false;
            if (newAccountButton != null) newAccountButton.interactable = true;
        }

        public void HandleQuit()
        {
            Application.Quit();
        }
    }
}
