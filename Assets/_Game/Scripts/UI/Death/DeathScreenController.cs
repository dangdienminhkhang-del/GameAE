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

        [Tooltip("Nút 'Thử lại' của màn 'BẠN ĐÃ BỊ HẠ GỤC' (chưa lưu được cái chết). Chưa gán thì bỏ qua + log cảnh báo.")]
        [SerializeField] private Button retryButton;

        private IAccountResetService _resetService;
        private bool _isResetting;

        private void Awake()
        {
            _resetService = ServiceProvider.CreateAccountResetService();

            if (panelRoot != null) panelRoot.SetActive(false);
            if (newAccountButton != null) newAccountButton.onClick.AddListener(HandleNewAccount);
            if (quitButton != null) quitButton.onClick.AddListener(HandleQuit);
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(HandleRetry);
                retryButton.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (newAccountButton != null) newAccountButton.onClick.RemoveListener(HandleNewAccount);
            if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuit);
            if (retryButton != null) retryButton.onClick.RemoveListener(HandleRetry);
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDeathRecorded += HandleDeathRecorded;
            GameEvents.OnDeathStateUnavailable += HandleDeathStateUnavailable;
            GameEvents.OnDeathRecordUnconfirmed += HandleDeathRecordUnconfirmed;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDeathRecorded -= HandleDeathRecorded;
            GameEvents.OnDeathStateUnavailable -= HandleDeathStateUnavailable;
            GameEvents.OnDeathRecordUnconfirmed -= HandleDeathRecordUnconfirmed;
        }

        private void HandleDeathRecorded(object sender, DeathInfo info)
        {
            Show(info);
        }

        private void HandleDeathStateUnavailable(object sender, EventArgs e)
        {
            ShowUnavailable();
        }

        private void HandleDeathRecordUnconfirmed(object sender, DeathInfo info)
        {
            ShowUnconfirmed(info);
        }

        public void Show(DeathInfo info)
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (titleText != null) titleText.text = "BẠN ĐÃ CHẾT";
            if (retryButton != null) retryButton.gameObject.SetActive(false); // đã xác nhận → không còn gì để thử lại

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
            if (retryButton != null) retryButton.gameObject.SetActive(false);
        }

        /// <summary>Tiêu đề màn "chưa lưu được cái chết" — nhân vật ĐÃ chết, chỉ là chưa ghi được lên máy chủ.</summary>
        public const string UnconfirmedTitle = "BẠN ĐÃ BỊ HẠ GỤC";

        /// <summary>Lời giải thích màn "chưa lưu được cái chết" (game tự thử lại, người chơi cũng có thể bấm Thử lại).</summary>
        public const string UnconfirmedDetails =
            "Chưa lưu được kết quả vì mất kết nối. Nhân vật vẫn tính là đã chết.\n" +
            "Hãy kết nối lại, game sẽ tự lưu.";

        /// <summary>
        /// Cái chết CHƯA được ghi xác nhận (docs/09 §1.3, §1.4): vẫn chặn chơi, chỉ có Thử lại / Thoát.
        /// KHÔNG có "Tạo tài khoản mới" và không có nút nào cho chơi tiếp: cái chết chưa chốt thì không được reset account,
        /// mất mạng cũng không phải lý do "tha chết". PvEBootstrap tự thử lại định kỳ; nút Thử lại gọi ngay.
        /// </summary>
        public void ShowUnconfirmed(DeathInfo info)
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (titleText != null) titleText.text = UnconfirmedTitle;
            if (detailsText != null) detailsText.text = UnconfirmedDetails;

            if (newAccountButton != null) newAccountButton.gameObject.SetActive(false);

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(true);
                retryButton.interactable = true;
            }
            else
            {
                Debug.LogWarning("[DeathScreen] Chưa gán retryButton — màn 'chưa lưu được cái chết' không có nút Thử lại (game vẫn tự thử lại).", this);
            }
        }

        /// <summary>UI chỉ gửi ý định "Thử lại"; việc ghi lại do Bootstrap/Services lo.</summary>
        private void HandleRetry()
        {
            if (retryButton != null) retryButton.interactable = false; // chặn bấm đúp; mở lại khi có kết quả Unconfirmed mới
            if (detailsText != null) detailsText.text = "Đang thử lưu lại...";
            GameEvents.RaiseDeathRetryRequested(this);
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
