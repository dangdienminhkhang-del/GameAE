using Game.Core.Items;
using Game.PvE.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Controls
{
    /// <summary>
    /// Nút dùng Bình trên màn hình cảm ứng (P9.6). Một chạm gọi PlayerInventory.TryUseConsumable(itemId).
    /// UI CHỈ hiển thị và gửi ý định: luật dùng bình nằm ở ConsumableRules (không dùng được giữa trận), chữ lý do lấy
    /// từ ConsumableRules.DescribeResult qua <see cref="PotionButtonViewModel"/>. Không hard-code id vật phẩm: gán ở Inspector.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OnScreenPotionButton : MonoBehaviour
    {
        [Tooltip("Id vật phẩm bình máu (ví dụ id trong ItemDefinition của bình). KHÔNG hard-code trong code.")]
        [SerializeField] private string itemId = string.Empty;
        [SerializeField] private PlayerInventory inventory;
        [Tooltip("Nhóm canvas của nút để làm mờ khi chưa dùng được (không bắt buộc, nếu thiếu sẽ tự thêm).")]
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("Chữ số bình còn lại (không bắt buộc).")]
        [SerializeField] private Text countLabel;
        [Tooltip("Chữ trạng thái dưới nút: lý do + đếm ngược giây (không bắt buộc).")]
        [SerializeField] private Text statusLabel;
        [Tooltip("Chữ thông báo ngắn khi chạm vào nút mờ (không bắt buộc).")]
        [SerializeField] private Text toastLabel;
        [SerializeField] private float toastSeconds = 1.5f;

        private Button _button;
        private float _toastHideAt;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (canvasGroup == null && !TryGetComponent(out canvasGroup)) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            if (toastLabel != null) toastLabel.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        private void Update()
        {
            ResolveInventory();
            Refresh();

            // Toast tính theo thời gian THẬT để vẫn tự tắt khi Pause.
            if (toastLabel != null && toastLabel.gameObject.activeSelf && UnityEngine.Time.unscaledTime >= _toastHideAt)
                toastLabel.gameObject.SetActive(false);
        }

        private void ResolveInventory()
        {
            if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
        }

        private void Refresh()
        {
            PotionButtonViewModel vm = BuildViewModel();
            if (canvasGroup != null) canvasGroup.alpha = vm.Alpha;
            if (countLabel != null) countLabel.text = vm.CountText;
            if (statusLabel != null) statusLabel.text = vm.StatusText;
        }

        private PotionButtonViewModel BuildViewModel()
        {
            if (inventory == null || string.IsNullOrEmpty(itemId))
                return PotionButtonViewModel.From(ConsumableUseResult.NotConsumable, 0, 0f);

            ConsumableUseResult state = inventory.GetConsumableUseState(itemId);
            int quantity = inventory.Inventory.GetStack(itemId);
            return PotionButtonViewModel.From(state, quantity, SecondsRemaining(state));
        }

        private float SecondsRemaining(ConsumableUseResult state)
        {
            if (state == ConsumableUseResult.InCombat) return inventory.CombatLockRemainingSeconds;
            if (state == ConsumableUseResult.OnCooldown)
            {
                ItemDefinition definition = inventory.Database != null ? inventory.Database.Find(itemId) : null;
                if (definition == null) return 0f;
                float since = inventory.UseClock.SecondsSinceUse(itemId, UnityEngine.Time.time);
                return Mathf.Max(0f, definition.UseCooldownSeconds - since);
            }
            return 0f;
        }

        /// <summary>Chạm nút: gửi ý định dùng bình. Không dùng được thì hiện lý do bằng chữ của ConsumableRules.</summary>
        public void HandleClick()
        {
            ResolveInventory();
            if (inventory == null || string.IsNullOrEmpty(itemId)) return;

            ConsumableUseResult result = inventory.TryUseConsumable(itemId);
            if (result != ConsumableUseResult.Ok) ShowToast(ConsumableRules.DescribeResult(result));
            Refresh();
        }

        private void ShowToast(string message)
        {
            if (toastLabel == null || string.IsNullOrEmpty(message)) return;
            toastLabel.text = message;
            toastLabel.gameObject.SetActive(true);
            _toastHideAt = UnityEngine.Time.unscaledTime + toastSeconds;
        }
    }
}
