using System;
using Game.Core.Events;
using Game.Core.Skills;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Skills
{
    /// <summary>
    /// Điều khiển Kỹ năng Phụ Dò Tìm & Ẩn Nấp (Searching & Hiding) của người chơi theo chuẩn SAO.
    /// Tự động gắn runtime lên Player qua PlayerController.EnsureAdvancedCombatComponents().
    /// - Searching: Duy trì tọa độ và bán kính dò tìm toàn cục, giúp EnemyHealthBar tự động hiện
    ///   con trỏ / cấp độ / độ nguy hiểm của quái vật trong phạm vi trước khi đánh.
    /// - Hiding: Đứng yên >= 1.2s kích hoạt Ẩn Nấp (giảm 60% tầm phát hiện của quái, mờ sprite 55%).
    ///   Tấn công hoặc dính đòn sẽ phá vỡ thế Ẩn Nấp ngay lập tức.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class SearchingHidingController : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private TechniqueController techniqueController;
        [SerializeField] private int searchingSkillLevel = 15; // Cấp kỹ năng Searching ban đầu

        private SpriteRenderer _spriteRenderer;
        private Color _originalColor = Color.white;
        private bool _colorCaptured;

        private float _stillTimer;
        private bool _isHiding;
        private bool _stealthBroken;
        private Vector2 _lastPosition;

        /// <summary>Hệ số giảm tầm phát hiện quái của người chơi (0 = không ẩn nấp, 0.6 = giảm 60%).</summary>
        public static float CurrentPlayerHidingFactor { get; private set; }

        /// <summary>Bán kính dò tìm quái hiện tại của người chơi (mét).</summary>
        public static float CurrentPlayerSearchingRadius { get; private set; } = SearchingHidingRules.DefaultSearchingRadius;

        /// <summary>Tọa độ thế giới hiện tại của người chơi.</summary>
        public static Vector2 CurrentPlayerPosition { get; private set; }

        public bool IsHiding => _isHiding;
        public int SearchingSkillLevel => searchingSkillLevel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            CurrentPlayerHidingFactor = 0f;
            CurrentPlayerSearchingRadius = SearchingHidingRules.DefaultSearchingRadius;
            CurrentPlayerPosition = Vector2.zero;
        }

        private void Awake()
        {
            if (playerController == null) TryGetComponent(out playerController);
            if (playerAttack == null) TryGetComponent(out playerAttack);
            if (techniqueController == null) TryGetComponent(out techniqueController);

            CaptureRenderer();
            UpdateSearchingRadius();
            _lastPosition = transform.position;
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
            if (playerAttack != null)
            {
                playerAttack.AttackPerformed += HandleAttackPerformed;
            }

            if (techniqueController != null)
            {
                techniqueController.OnTechniqueStarted += HandleTechniqueStarted;
            }

            GameEvents.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (playerAttack != null)
            {
                playerAttack.AttackPerformed -= HandleAttackPerformed;
            }

            if (techniqueController != null)
            {
                techniqueController.OnTechniqueStarted -= HandleTechniqueStarted;
            }

            GameEvents.OnHealthChanged -= HandleHealthChanged;

            BreakHiding(silent: true);
        }

        private void HandleAttackPerformed(object sender, EventArgs e)
        {
            BreakHiding();
        }

        private void HandleTechniqueStarted(object sender, TechniqueInputSlot slot)
        {
            BreakHiding();
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs args)
        {
            if (args != null && args.Delta < 0.0)
            {
                BreakHiding();
            }
        }

        public void SetSearchingSkillLevel(int level)
        {
            searchingSkillLevel = Mathf.Clamp(level, 0, 100);
            UpdateSearchingRadius();
        }

        private void UpdateSearchingRadius()
        {
            CurrentPlayerSearchingRadius = SearchingHidingRules.CalculateSearchingRadius(searchingSkillLevel);
        }

        private void Update()
        {
            CurrentPlayerPosition = transform.position;

            Vector2 currentPos = transform.position;
            float movementDelta = (currentPos - _lastPosition).magnitude;
            bool isMoving = movementDelta > 0.01f;
            _lastPosition = currentPos;

            if (isMoving)
            {
                // Nếu đang di chuyển, giảm tốc độ tích lũy đứng yên
                if (!_isHiding)
                {
                    _stillTimer = 0f;
                }
            }
            else
            {
                _stillTimer += Time.deltaTime;
                // Đủ thời gian đứng yên -> kích hoạt Ẩn Nấp
                if (!_isHiding && _stillTimer >= SearchingHidingRules.HidingActivationTimeSeconds && !_stealthBroken)
                {
                    EnterHiding();
                }
            }

            // Cập nhật hệ số ẩn nấp thời gian thực
            if (_isHiding)
            {
                CurrentPlayerHidingFactor = isMoving
                    ? SearchingHidingRules.HidingReductionMoving
                    : SearchingHidingRules.HidingReductionStanding;
            }
            else
            {
                CurrentPlayerHidingFactor = 0f;
            }

            // Khi di chuyển một khoảng xa hoặc hồi phục, reset trạng thái bị lộ
            if (_stealthBroken && !isMoving && _stillTimer >= SearchingHidingRules.HidingActivationTimeSeconds)
            {
                _stealthBroken = false;
            }
        }

        public void EnterHiding()
        {
            if (_isHiding) return;
            _isHiding = true;
            _stealthBroken = false;
            CurrentPlayerHidingFactor = SearchingHidingRules.HidingReductionStanding;

            CaptureRenderer();
            if (_spriteRenderer != null)
            {
                Color c = _originalColor;
                c.a = SearchingHidingRules.StealthAlpha;
                _spriteRenderer.color = c;
            }

            GameEvents.RaiseCombatNotice("🌫 ĐANG ẨN NẤP (Hiding)", new Color(0.7f, 0.9f, 1f));
        }

        public void BreakHiding(bool silent = false)
        {
            if (!_isHiding && !_stealthBroken) return;

            bool wasHiding = _isHiding;
            _isHiding = false;
            _stealthBroken = true;
            _stillTimer = 0f;
            CurrentPlayerHidingFactor = 0f;

            CaptureRenderer();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }

            if (wasHiding && !silent)
            {
                GameEvents.RaiseCombatNotice("💨 LỘ DIỆN!", Color.gray);
            }
        }
    }
}
