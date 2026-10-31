using System;
using Game.Core.Skills;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Skills
{
    /// <summary>
    /// Điều khiển thời gian khựng chiêu / bất động (Post-Motion Freeze) theo chuẩn SAO.
    /// Tự động gắn vào Player lúc runtime qua PlayerController.EnsureAdvancedCombatComponents().
    /// - Khi dùng Weapon Technique hoặc Ultimate, kích hoạt trạng thái Freeze.
    /// - Khi đang Freeze: khóa di chuyển PlayerController, khóa đánh thường PlayerAttack, khóa né DodgeController, khóa kỹ năng tiếp theo.
    /// - Hiệu ứng thị giác: làm chậm/tint nhẹ màu sprite nhân vật khi khựng chiêu.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PostSkillDelayController : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private TechniqueController techniqueController;
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private Dodge.DodgeController dodgeController;
        [SerializeField] private AdrenalineController adrenalineController;

        private float _freezeEndTime = -1f;
        private bool _isFrozen;
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor = Color.white;
        private bool _colorCaptured;

        public bool IsFrozen => _isFrozen;
        public float FreezeEndTime => _freezeEndTime;
        public float RemainingFreezeTime => PostSkillDelayRules.GetRemainingDelay(_freezeEndTime, Time.time);

        public event Action<bool> FreezeStateChanged;

        private void Awake()
        {
            if (playerController == null) TryGetComponent(out playerController);
            if (techniqueController == null) TryGetComponent(out techniqueController);
            if (playerAttack == null) TryGetComponent(out playerAttack);
            if (dodgeController == null) TryGetComponent(out dodgeController);
            if (adrenalineController == null) TryGetComponent(out adrenalineController);

            CaptureRenderer();
        }

        private void CaptureRenderer()
        {
            if (_colorCaptured) return;
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
                _colorCaptured = true;
            }
        }

        private void OnEnable()
        {
            if (techniqueController != null)
            {
                techniqueController.OnTechniqueStarted += HandleTechniqueStarted;
                techniqueController.OnTechniqueFinished += HandleTechniqueFinished;
            }

            if (adrenalineController != null)
            {
                adrenalineController.UltimateExecuted += HandleUltimateExecuted;
            }
        }

        private void OnDisable()
        {
            if (techniqueController != null)
            {
                techniqueController.OnTechniqueStarted -= HandleTechniqueStarted;
                techniqueController.OnTechniqueFinished -= HandleTechniqueFinished;
            }

            if (adrenalineController != null)
            {
                adrenalineController.UltimateExecuted -= HandleUltimateExecuted;
            }

            SetFrozen(false);
        }

        private void HandleTechniqueStarted(object sender, TechniqueInputSlot slot)
        {
            // Khóa hành động trong suốt thời gian Technique (WindUp + Active + Recovery)
            SetFrozen(true);
        }

        private void HandleTechniqueFinished(object sender, TechniqueInputSlot slot)
        {
            // Khi Technique hoàn thành xong Recovery
            SetFrozen(false);
        }

        private void HandleUltimateExecuted()
        {
            ApplyFreeze(PostSkillDelayRules.DefaultUltimateFreezeDuration, "Ultimate");
        }

        /// <summary>
        /// Áp dụng thời gian khựng chiêu (post-motion freeze) trong khoảng duration giây.
        /// </summary>
        public void ApplyFreeze(float duration, string reason = "")
        {
            if (duration <= 0f) return;
            float newEnd = Time.time + duration;
            if (newEnd > _freezeEndTime)
            {
                _freezeEndTime = newEnd;
            }
            SetFrozen(true);
        }

        private void Update()
        {
            // Kiểm tra hết thời gian freeze nếu không phải do TechniqueController đang chạy
            if (_isFrozen)
            {
                bool techniqueRunning = techniqueController != null &&
                                        techniqueController.State != TechniqueExecutionState.Idle;

                if (!techniqueRunning && !PostSkillDelayRules.IsDelayActive(_freezeEndTime, Time.time))
                {
                    SetFrozen(false);
                }
            }
        }

        private void SetFrozen(bool frozen)
        {
            if (_isFrozen == frozen) return;
            _isFrozen = frozen;

            if (playerController == null) TryGetComponent(out playerController);
            if (playerController != null)
            {
                playerController.IsActionLocked = frozen;
            }

            // Visual feedback: Đổi tint nhẹ khi khựng chiêu (chuẩn SAO post-motion freeze)
            CaptureRenderer();
            if (_spriteRenderer != null)
            {
                if (frozen)
                {
                    _spriteRenderer.color = new Color(0.82f, 0.88f, 1f, 1f); // Xanh lam thép dịu khi khựng
                }
                else
                {
                    _spriteRenderer.color = _originalColor;
                }
            }

            FreezeStateChanged?.Invoke(frozen);
        }
    }
}
