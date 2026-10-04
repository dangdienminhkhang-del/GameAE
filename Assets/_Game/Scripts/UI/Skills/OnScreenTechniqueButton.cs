using Game.Core.Skills;
using Game.PvE.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Skills
{
    /// <summary>
    /// Nút cảm ứng Technique (Core / Traverse).
    /// CHỨA UI logic ONLY: gọi public API TechniqueController, hiển thị cooldown + lý do khóa.
    /// Không chứa logic damage / mastery / cooldown calculation (quy luật AGENTS.md #3).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OnScreenTechniqueButton : MonoBehaviour
    {
        [Header("Slot")]
        [SerializeField] private TechniqueInputSlot slot = TechniqueInputSlot.Core;

        [Header("UI Refs (tự tạo Image con nếu bỏ trống)")]
        [SerializeField] private Image cooldownFill;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Text feedbackLabel;

        [Header("Polling (không sửa TechniqueController.cs — UI chỉ đọc)")]
        [SerializeField] private float uiRefreshIntervalSeconds = 0.1f;

        private Button _button;
        private TechniqueController _controller;
        private float _nextRefresh;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
            TryFindController();
        }

        private void OnEnable()
        {
            TryFindController();
            if (_controller != null)
            {
                _controller.OnValidationFailed += HandleValidationFailed;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.OnValidationFailed -= HandleValidationFailed;
            }
        }

        private void Update()
        {
            if (_controller == null) TryFindController();
            if (_controller == null) return;
            if (!(UnityEngine.Time.time >= _nextRefresh)) return;
            _nextRefresh = UnityEngine.Time.time + uiRefreshIntervalSeconds;
            RefreshUI();
        }

        private void HandleClick()
        {
            if (_controller == null) TryFindController();
            if (_controller == null)
            {
                SetFeedback("TechniqueController chưa gắn vào Player");
                return;
            }
            _controller.TryUseTechnique(slot);
        }

        private void RefreshUI()
        {
            if (_controller == null) return;

            float remaining = _controller.GetCooldownRemaining(slot);
            float total = _controller.GetCooldownTotal(slot);
            if (cooldownFill != null && total > 0.001f)
            {
                cooldownFill.fillAmount = Mathf.Clamp01(remaining / total);
                cooldownFill.enabled = remaining > 0.01f;
            }

            bool usable = _controller.IsSlotReady(slot, out TechniqueValidationError reason);
            if (_button != null) _button.interactable = usable;
            if (lockOverlay != null) lockOverlay.SetActive(!usable);

            if (!usable)
            {
                SetFeedback(FeedbackTextFor(reason));
            }
            else
            {
                SetFeedback(null);
            }
        }

        private static string FeedbackTextFor(TechniqueValidationError reason)
        {
            switch (reason)
            {
                case TechniqueValidationError.LockedMastery: return "Mastery chưa đủ";
                case TechniqueValidationError.WrongDiscipline: return "Vũ khí không khớp";
                case TechniqueValidationError.Cooldown: return "Hồi chiêu";
                case TechniqueValidationError.CombatBusy: return "Đang đánh";
                case TechniqueValidationError.NoWeaponEquipped: return "Chưa có vũ khí";
                case TechniqueValidationError.DefinitionMissing: return "Chưa có technique";
                default: return null;
            }
        }

        private void SetFeedback(string msg)
        {
            if (feedbackLabel != null) feedbackLabel.text = msg;
        }

        private void HandleValidationFailed(object sender, TechniqueValidationError reason)
        {
            // Chỉ hiển thị feedback ngắn gọn của nút này (nếu lý do là slot khác, bỏ qua).
            // TechniqueExecutor không phân biệt slot → hiển thị tạm cho tất cả.
            SetFeedback(FeedbackTextFor(reason));
        }

        private void TryFindController()
        {
            if (_controller != null) return;
            _controller = FindAnyObjectByType<TechniqueController>();
            if (_controller != null)
            {
                _controller.OnValidationFailed += HandleValidationFailed;
            }
        }
    }
}
