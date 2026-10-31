using System;
using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Facing;
using Game.Core.Input;
using Game.Core.Stats;
using UnityEngine;

namespace Game.PvE.Player
{
    /// <summary>
    /// Đòn đánh thường của player: input → cooldown → truy vấn hitbox → tính damage → áp lên IDamageable.
    /// Không chứa công thức balance (nằm ở AttackDefinition) và không tự xử lý death/thưởng.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private AttackDefinition attackDefinition;

        [Tooltip("Điểm gốc của vùng đánh (thường là con của Player, phía trước nhân vật). Bỏ trống = dùng vị trí Player + HitboxOffset theo hướng mặt.")]
        [SerializeField] private Transform attackOrigin;

        [SerializeField] private Animator animator;
        [Tooltip("Tên trigger trong Animator. Để trống nếu Animator chưa có state Attack.")]
        [SerializeField] private string attackTriggerName = "";

        [SerializeField] private bool logHits = false;

        [Header("Tự nhắm (tùy chọn, mặc định TẮT)")]
        [Tooltip("Bật: khi bấm đánh, quay về phía mục tiêu còn sống gần nhất trong bán kính rồi mới tính vùng đánh. Không có mục tiêu thì dùng hướng đang nhìn.")]
        [SerializeField] private bool autoAim = false;
        [SerializeField] private float autoAimRadius = 2.5f;

        [Header("Combo String (Chuỗi đòn 3 nhịp)")]
        [SerializeField] private bool enableComboString = true;
        [SerializeField] private float comboResetTime = 0.85f;
        [SerializeField] private float combo2DamageMultiplier = 1.25f;
        [SerializeField] private float combo3DamageMultiplier = 1.75f;
        [SerializeField] private float combo3RadiusMultiplier = 1.35f;
        [SerializeField] private float comboStep1Cooldown = 0.22f;
        [SerializeField] private float comboStep2Cooldown = 0.25f;
        [SerializeField] private float comboStep3Cooldown = 0.45f;
        [SerializeField] private float comboForwardLunge = 0.35f;

        private int _currentComboStep = 0;
        private float _lastAttackTime = -999f;

        private readonly HitDetection _hitDetection = new HitDetection();

        // Buffer tái dùng cho tự nhắm (không cấp phát mỗi đòn).
        private readonly List<Collider2D> _aimColliders = new List<Collider2D>(16);
        private readonly List<IDamageable> _aimTargets = new List<IDamageable>(8);
        private readonly List<AimCandidate> _aimCandidates = new List<AimCandidate>(8);
        private readonly List<Vector2> _aimPositions = new List<Vector2>(8);
        private ContactFilter2D _aimFilter;
        private bool _aimFilterReady;
        private CooldownSystem _cooldown;
        private IInputReader _inputReader;
        private CharacterStats _stats;

        /// <summary>
        /// Lấy Stats muộn (không cache ở Awake) vì thứ tự Awake giữa các component trên cùng
        /// GameObject là không xác định — PlayerController có thể tạo Stats SAU PlayerAttack.
        /// </summary>
        private CharacterStats Stats
        {
            get
            {
                if (_stats == null && playerController != null) _stats = playerController.Stats;
                return _stats;
            }
        }

        public AttackDefinition Definition => attackDefinition;
        public bool IsReady => _cooldown != null && _cooldown.IsReady && (playerController == null || !playerController.IsActionLocked);
        public int CurrentComboStep => _currentComboStep;

        /// <summary>
        /// Kỹ năng Độc Nhất Nhị Đao Lưu (Dual Wielding / Dual Blades theo SAO):
        /// Cầm 2 kiếm tăng 25% tốc đánh và tăng sát thương combo 3 nhịp.
        /// </summary>
        public bool IsDualWielding { get; set; }

        public static float ExternalDamageMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// Phát mỗi khi một đòn đánh thường được thực hiện (kể cả khi không trúng ai) — đúng lúc vung đòn, trước các OnHitResolved.
        /// Dùng cho âm thanh/hiệu ứng (docs/05 Phase 9.5); không đổi luật chiến đấu.
        /// </summary>
        public event EventHandler AttackPerformed;

        /// <summary>Phát bước combo hiện tại (0 = nhịp 1, 1 = nhịp 2, 2 = kết liễu).</summary>
        public event Action<int> ComboStepPerformed;

        /// <summary>Phát khi đòn đánh kích hoạt từ Thế Phản Đòn (Counter Strike).</summary>
        public event Action CounterStrikeTriggered;

        private bool _isCounterStanceActive;
        public bool IsCounterStanceActive => _isCounterStanceActive;

        public void ActivateCounterStance(float duration)
        {
            _isCounterStanceActive = true;
            CancelInvoke(nameof(DeactivateCounterStance));
            Invoke(nameof(DeactivateCounterStance), duration);
        }

        public void DeactivateCounterStance()
        {
            _isCounterStanceActive = false;
        }

        private void Awake()
        {
            if (playerController == null) TryGetComponent(out playerController);
            _cooldown = new CooldownSystem(this);
        }

        private void Start()
        {
            SyncDualWieldFromInventory();
        }

        /// <summary>
        /// Đồng bộ trạng thái Nhị Đao Lưu từ trang bị nếu có PlayerInventory.
        /// </summary>
        public void SyncDualWieldFromInventory()
        {
            if (TryGetComponent(out Items.PlayerInventory inv) && inv.Equipment != null)
            {
                bool hasMain = !string.IsNullOrEmpty(inv.Equipment.GetInstanceId(Game.Core.Items.EquipmentSlot.Weapon));
                bool hasOff = !string.IsNullOrEmpty(inv.Equipment.GetInstanceId(Game.Core.Items.EquipmentSlot.OffHand));
                IsDualWielding = DualWieldRules.IsDualWielding(hasMain, hasOff);
            }
        }

        private void OnEnable()
        {
            if (_inputReader != null) _inputReader.OnAttackStarted += HandleAttackStarted;
        }

        private void OnDisable()
        {
            if (_inputReader != null) _inputReader.OnAttackStarted -= HandleAttackStarted;
        }

        /// <summary>Do PvEBootstrap gọi — UI/joystick không tham chiếu trực tiếp vào đây.</summary>
        public void AssignInputReader(IInputReader reader)
        {
            if (_inputReader != null) _inputReader.OnAttackStarted -= HandleAttackStarted;
            _inputReader = reader;
            if (isActiveAndEnabled && _inputReader != null) _inputReader.OnAttackStarted += HandleAttackStarted;
        }

        private void HandleAttackStarted(object sender, EventArgs e)
        {
            TryAttack();
        }

        public bool TryAttack()
        {
            if (!IsReady) return false;
            if (attackDefinition == null)
            {
                Debug.LogWarning("[PlayerAttack] Chưa gán AttackDefinition — bỏ qua đòn đánh.", this);
                return false;
            }
            CharacterStats stats = Stats;
            if (stats == null) return false;

            if (enableComboString)
            {
                if (UnityEngine.Time.time - _lastAttackTime <= comboResetTime)
                {
                    _currentComboStep = (_currentComboStep + 1) % 3;
                }
                else
                {
                    _currentComboStep = 0;
                }
                _lastAttackTime = UnityEngine.Time.time;
            }
            else
            {
                _currentComboStep = 0;
            }

            AttackPerformed?.Invoke(this, EventArgs.Empty);
            ComboStepPerformed?.Invoke(_currentComboStep);

            ApplyAutoAim();

            double stepMultiplier = 1.0;
            float stepCooldown = attackDefinition.CooldownSeconds;
            float hitboxRadius = attackDefinition.HitboxRadius;

            if (enableComboString)
            {
                switch (_currentComboStep)
                {
                    case 0:
                        stepMultiplier = IsDualWielding ? DualWieldRules.GetComboMultiplier(0, true) : 1.0;
                        stepCooldown = DualWieldRules.CalculateAttackCooldown(comboStep1Cooldown, IsDualWielding);
                        ApplyForwardLunge(comboForwardLunge * (IsDualWielding ? 0.6f : 0.4f));
                        break;
                    case 1:
                        stepMultiplier = IsDualWielding ? DualWieldRules.GetComboMultiplier(1, true) : combo2DamageMultiplier;
                        stepCooldown = DualWieldRules.CalculateAttackCooldown(comboStep2Cooldown, IsDualWielding);
                        ApplyForwardLunge(comboForwardLunge * (IsDualWielding ? 1.0f : 0.8f));
                        break;
                    case 2:
                        stepMultiplier = IsDualWielding ? DualWieldRules.GetComboMultiplier(2, true) : combo3DamageMultiplier;
                        stepCooldown = DualWieldRules.CalculateAttackCooldown(comboStep3Cooldown, IsDualWielding);
                        hitboxRadius *= combo3RadiusMultiplier * (IsDualWielding ? 1.15f : 1.0f);
                        ApplyForwardLunge(comboForwardLunge * (IsDualWielding ? 1.5f : 1.2f));
                        Camera.CameraFollow.RequestShake(IsDualWielding ? 0.22f : 0.16f, IsDualWielding ? 0.25f : 0.18f);
                        break;
                }
            }

            bool wasCounter = _isCounterStanceActive;
            if (wasCounter)
            {
                _isCounterStanceActive = false;
                stepMultiplier *= 2.0;
                CounterStrikeTriggered?.Invoke();
                Camera.CameraFollow.RequestShake(0.18f, 0.22f);
            }

            Vector2 center = ResolveHitboxCenter();
            var targets = _hitDetection.FindTargets(center, hitboxRadius, attackDefinition.TargetLayers);
            for (int i = 0; i < targets.Count; i++)
            {
                IDamageable target = targets[i];
                double defense = AttackExecutor.ResolveDefense(target);
                DamageInfo damage = AttackExecutor.ResolveDamage(
                    stats, defense, attackDefinition, UnityEngine.Random.value, gameObject.name);

                damage.Amount *= stepMultiplier * ExternalDamageMultiplier;

                if (damage.IsCritical)
                {
                    Camera.CameraFollow.RequestShake(0.12f, 0.14f);
                }

                // ApplyHit = TakeDamage + phát GameEvents.OnHitResolved (túi đồ dùng để chặn uống thuốc khi đang đánh).
                AttackExecutor.ApplyHit(target, damage);

                if (logHits)
                {
                    Debug.Log($"[PlayerAttack] {damage.Amount:0.#} dmg (crit={damage.IsCritical}, combo={_currentComboStep}) → {target}", target as UnityEngine.Object);
                }
            }

            _cooldown.StartCooldown(stepCooldown);
            if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
            {
                animator.SetTrigger(attackTriggerName);
            }
            return true;
        }

        private void ApplyForwardLunge(float distance)
        {
            if (distance <= 0f || playerController == null) return;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;

            Vector2 dir = playerController.FacingDirection;
            var hits = new RaycastHit2D[2];
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            int count = rb.Cast(dir, filter, hits, distance);
            float safeDist = distance;
            if (count > 0 && hits[0].collider != null)
            {
                safeDist = Mathf.Max(0f, hits[0].distance - 0.05f);
            }
            if (safeDist > 0.01f)
            {
                rb.MovePosition(rb.position + dir * safeDist);
            }
        }

        private Vector2 ResolveHitboxCenter()
        {
            if (attackOrigin != null) return attackOrigin.position;

            Vector2 facingDirection = playerController != null ? playerController.FacingDirection : Vector2.right;
            Vector2 offset = attackDefinition != null ? attackDefinition.HitboxOffset : Vector2.zero;
            return HitboxMath.Center(transform.position, facingDirection, offset);
        }

        /// <summary>
        /// Tự nhắm (nếu bật): quay hướng nhìn về mục tiêu còn sống gần nhất trong bán kính. IDamageable không có vị trí
        /// nên ứng viên lấy qua collider theo TargetLayers (cùng cách HitDetection lọc).
        /// </summary>
        private void ApplyAutoAim()
        {
            if (!autoAim || playerController == null || attackDefinition == null) return;

            if (!_aimFilterReady)
            {
                _aimFilter = new ContactFilter2D { useTriggers = true };
                _aimFilterReady = true;
            }
            _aimFilter.SetLayerMask(attackDefinition.TargetLayers);

            Vector2 origin = transform.position;
            _aimColliders.Clear();
            _aimTargets.Clear();
            _aimCandidates.Clear();
            _aimPositions.Clear();

            int count = Physics2D.OverlapCircle(origin, autoAimRadius, _aimFilter, _aimColliders);
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = _aimColliders[i];
                if (collider == null) continue;

                IDamageable target = collider.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead || _aimTargets.Contains(target)) continue;

                _aimTargets.Add(target);
                Vector2 position = target is Component component ? (Vector2)component.transform.position : (Vector2)collider.bounds.center;
                _aimPositions.Add(position);
                _aimCandidates.Add(new AimCandidate(position, true));
            }

            int best = AimAssist.PickNearest(origin, _aimCandidates, autoAimRadius);
            if (best < 0) return;
            if (!AimAssist.TryDirection(origin, _aimPositions[best], out Vector2 direction)) return;

            playerController.SetFacingDirection(direction);
        }

        private void OnValidate()
        {
            autoAimRadius = Mathf.Max(0f, autoAimRadius);
        }
    }
}
