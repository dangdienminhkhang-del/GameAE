using System;
using Game.Core.Combat;
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

        private readonly HitDetection _hitDetection = new HitDetection();
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
        public bool IsReady => _cooldown != null && _cooldown.IsReady;

        private void Awake()
        {
            if (playerController == null) TryGetComponent(out playerController);
            _cooldown = new CooldownSystem(this);
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

            Vector2 center = ResolveHitboxCenter();
            var targets = _hitDetection.FindTargets(center, attackDefinition.HitboxRadius, attackDefinition.TargetLayers);
            for (int i = 0; i < targets.Count; i++)
            {
                IDamageable target = targets[i];
                double defense = AttackExecutor.ResolveDefense(target);
                DamageInfo damage = AttackExecutor.ResolveDamage(
                    stats, defense, attackDefinition, UnityEngine.Random.value, gameObject.name);
                // ApplyHit = TakeDamage + phát GameEvents.OnHitResolved (túi đồ dùng để chặn uống thuốc khi đang đánh).
                AttackExecutor.ApplyHit(target, damage);

                if (logHits)
                {
                    Debug.Log($"[PlayerAttack] {damage.Amount:0.#} dmg (crit={damage.IsCritical}) → {target}", target as UnityEngine.Object);
                }
            }

            _cooldown.StartCooldown(attackDefinition.CooldownSeconds);
            if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
            {
                animator.SetTrigger(attackTriggerName);
            }
            return true;
        }

        private Vector2 ResolveHitboxCenter()
        {
            if (attackOrigin != null) return attackOrigin.position;

            int facing = playerController != null ? playerController.Facing : 1;
            Vector3 position = transform.position;
            Vector2 offset = attackDefinition != null ? attackDefinition.HitboxOffset : Vector2.zero;
            return new Vector2(position.x + offset.x * facing, position.y + offset.y);
        }
    }
}
