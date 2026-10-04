using System;
using Game.Core.AI;
using Game.Core.Combat;
using Game.Core.Events;
using UnityEngine;

namespace Game.PvE.Enemy
{
    /// <summary>
    /// Quái PvE: Idle → Chase → Attack, Hurt khi trúng đòn, Dead khi HP = 0.
    /// Chỉ số lấy từ EnemyDefinition; không chứa công thức balance.
    /// Chết → GameEvents.EnemyDefeated + ExperienceGained (PlayerProgress xử lý level).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour, IDamageable, ICombatStats
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool logHits;

        private Rigidbody2D _rigidbody;
        private StateMachine _machine;
        private Transform _target;
        private double _health;
        private float _attackTimer;
        private float _hurtTimer;
        private float _deathTimer;
        private bool _returned;

        public event Action<EnemyController> Died;

        public EnemyDefinition Definition => definition;

        /// <summary>Gán loại quái lúc runtime (spawner dùng một prefab cho mọi loại).</summary>
        public void SetDefinition(EnemyDefinition value)
        {
            definition = value;
            if (spriteRenderer != null && definition != null) spriteRenderer.sprite = definition.Sprite;
        }

        private float _floorPower = 1f;
        private float _expMultiplier = 1f;

        /// <summary>
        /// Áp hệ số theo tầng (docs/07 §5): stat thật = base × multiplier. Không sửa EnemyDefinition.
        /// </summary>
        public void SetFloor(int floorNumber)
        {
            _floorPower = Game.Core.Floor.FloorScaling.EnemyPowerMultiplier(floorNumber);
            _expMultiplier = Game.Core.Floor.FloorScaling.ExpMultiplier(floorNumber);
        }

        public float FloorPower => _floorPower;
        public Transform Target => _target;
        public bool IsDead => _health <= 0.0;
        public double CurrentHealth => _health;
        public double MaxHealth => definition != null ? definition.MaxHealth * _floorPower : 1.0;
        public double Attack => definition != null ? definition.Attack * _floorPower : 0.0;
        public double Defense => definition != null ? definition.Defense * _floorPower : 0.0;
        public double CriticalChance => 0.0;
        public double CriticalDamage => 1.0;
        public bool IsHurt => _hurtTimer > 0f;
        public bool IsReadyToAttack => _attackTimer <= 0f;
        public bool IsWindingUp => _windupActive;
        public float DistanceToTarget => _target != null ? Vector2.Distance(transform.position, _target.position) : float.MaxValue;

        private float _windupTimer;
        private bool _windupActive;
        private float _targetScanTimer;

        /// <summary>Bắt đầu lấy đà — có dấu hiệu báo trước để người chơi kịp né (FB-011, permadeath).</summary>
        public void BeginWindup()
        {
            if (_windupActive) return;
            _windupActive = true;
            _windupTimer = definition != null ? Mathf.Max(0f, definition.AttackWindupSeconds) : 0.35f;
            StopMoving();
            SetTelegraph(true);
        }

        public void EndWindup()
        {
            if (!_windupActive) return;
            _windupActive = false;
            _windupTimer = 0f;
            SetTelegraph(false);
        }

        /// <summary>True khi vừa hết lấy đà (được phép ra đòn).</summary>
        public bool TickWindup(float deltaTime)
        {
            if (!_windupActive) return false;
            _windupTimer -= deltaTime;
            if (_windupTimer > 0f) return false;
            EndWindup();
            return true;
        }

        private void SetTelegraph(bool on)
        {
            if (spriteRenderer == null) return;
            spriteRenderer.color = on ? new Color(1f, 0.55f, 0.55f, 1f) : Color.white;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.freezeRotation = true;
            if (spriteRenderer == null) TryGetComponent(out spriteRenderer);

            _machine = new StateMachine();
            _machine.Register(new IdleState(this));
            _machine.Register(new ChaseState(this));
            _machine.Register(new AttackState(this));
            _machine.Register(new HurtState(this));
            _machine.Register(new DeadState(this));
        }

        private void OnEnable()
        {
            ResetForSpawn();
        }

        private void OnDisable()
        {
            if (_rigidbody != null) _rigidbody.linearVelocity = Vector2.zero;
        }

        /// <summary>Dùng lại được cho pooling: hồi đầy máu và về Idle.</summary>
        public void ResetForSpawn()
        {
            _health = MaxHealth;
            _attackTimer = 0f;
            _hurtTimer = 0f;
            _deathTimer = 0f;
            _returned = false;
            if (definition != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = definition.Sprite;
                spriteRenderer.color = Color.white;
            }
            _machine.ChangeTo<IdleState>();
        }

        private void Update()
        {
            // Guard: object có thể tồn tại trong scene mà Awake chưa chạy (rác lưu nhầm trong scene).
            if (_machine == null) return;

            AcquireTarget();
            TickTimers();
            _machine.Tick();
        }

        private void AcquireTarget()
        {
            if (_target != null) return;

            // Không gọi FindGameObjectWithTag mỗi frame: quét lại mỗi 1 giây là đủ.
            _targetScanTimer -= UnityEngine.Time.deltaTime;
            if (_targetScanTimer > 0f) return;
            _targetScanTimer = 1f;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _target = player.transform;
        }

        private void TickTimers()
        {
            if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;
            if (_hurtTimer > 0f) _hurtTimer -= Time.deltaTime;
            if (IsDead && _deathTimer > 0f)
            {
                _deathTimer -= Time.deltaTime;
                if (_deathTimer <= 0f) ReturnToPool();
            }
        }

        public void MoveToward(Vector2 destination)
        {
            if (definition == null) return;
            Vector2 direction = (destination - (Vector2)transform.position).normalized;
            _rigidbody.linearVelocity = direction * definition.MoveSpeed;
            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.01f) spriteRenderer.flipX = direction.x < 0f;
        }

        public void StopMoving()
        {
            if (_rigidbody != null) _rigidbody.linearVelocity = Vector2.zero;
        }

        /// <summary>Đánh mục tiêu hiện tại nếu trong tầm — gọi từ AttackState.</summary>
        public bool TryAttackTarget()
        {
            if (!IsReadyToAttack || definition == null || _target == null) return false;

            _attackTimer = definition.AttackCooldownSeconds;
            var damageable = _target.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead) return false;

            double defense = AttackExecutor.ResolveDefense(damageable);
            // sourceId ổn định ("enemy.slime") — không dùng gameObject.name vì sẽ thành "Slime(Clone)" trong dữ liệu permadeath.
            string sourceId = definition.Id;
            DamageInfo damage = AttackExecutor.ResolveDamage(this, defense, null, UnityEngine.Random.value, sourceId);
            damageable.TakeDamage(damage);

            if (logHits) Debug.Log($"[Enemy:{definition.DisplayName}] {damage.Amount:0.#} dmg → {damageable}", this);
            return true;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (damage == null || IsDead) return;

            _health = Math.Max(0.0, _health - damage.Amount);
            EndWindup();
            if (logHits) Debug.Log($"[Enemy:{definition?.DisplayName}] nhận {damage.Amount:0.#} → {_health:0.#}", this);

            if (IsDead) _machine.ChangeTo<DeadState>();
            else _machine.ChangeTo<HurtState>();
        }

        public void Heal(double amount)
        {
            if (amount <= 0.0 || IsDead) return;
            _health = Math.Min(MaxHealth, _health + amount);
        }

        public void SetMaxHealth(double maxHp, bool resetCurrent = false)
        {
            if (definition == null) return;
            if (resetCurrent) _health = definition.MaxHealth;
        }

        public void OnDeath()
        {
            _deathTimer = definition != null ? definition.DeathSeconds : 0.5f;
            if (spriteRenderer != null) spriteRenderer.color = new Color(0.4f, 0.4f, 0.4f, 0.9f);
            StopMoving();

            GameEvents.RaiseEnemyDefeated(this);
            if (definition != null)
            {
                int reward = UnityEngine.Mathf.RoundToInt(definition.ExperienceReward * _expMultiplier);
                GameEvents.RaiseExperienceGained(this, reward);
            }
            Died?.Invoke(this);
        }

        private void ReturnToPool()
        {
            if (_returned) return;
            _returned = true;
            gameObject.SetActive(false);
        }

        // ── States ────────────────────────────────────────────────

        private class IdleState : StateBase
        {
            private readonly EnemyController _owner;
            public IdleState(EnemyController owner) { _owner = owner; }

            public override void Enter() => _owner.StopMoving();
            public override void Tick()
            {
                if (_owner.Target != null && _owner.DistanceToTarget <= _owner.Definition.DetectionRange)
                    _owner._machine.ChangeTo<ChaseState>();
            }
            public override void Exit() { }
        }

        private class ChaseState : StateBase
        {
            private readonly EnemyController _owner;
            public ChaseState(EnemyController owner) { _owner = owner; }

            public override void Enter() { }
            public override void Tick()
            {
                if (_owner.Target == null) { _owner._machine.ChangeTo<IdleState>(); return; }
                float distance = _owner.DistanceToTarget;
                if (distance <= _owner.Definition.AttackRange)
                {
                    _owner._machine.ChangeTo<AttackState>();
                    return;
                }
                if (distance > _owner.Definition.DetectionRange * 1.5f)
                {
                    _owner._machine.ChangeTo<IdleState>();
                    return;
                }
                _owner.MoveToward(_owner.Target.position);
            }
            public override void Exit() => _owner.StopMoving();
        }

        private class AttackState : StateBase
        {
            private readonly EnemyController _owner;
            public AttackState(EnemyController owner) { _owner = owner; }

            public override void Enter() => _owner.StopMoving();
            public override void Tick()
            {
                if (_owner.Target == null)
                {
                    _owner.EndWindup();
                    _owner._machine.ChangeTo<IdleState>();
                    return;
                }
                if (_owner.DistanceToTarget > _owner.Definition.AttackRange)
                {
                    _owner.EndWindup();
                    _owner._machine.ChangeTo<ChaseState>();
                    return;
                }

                // Đang hồi chiêu thì không lấy đà (tránh nhấp nháy báo động liên tục).
                if (!_owner.IsReadyToAttack)
                {
                    _owner.EndWindup();
                    return;
                }

                // Chưa lấy đà → bắt đầu lấy đà (telegraph) rồi mới ra đòn ở các frame sau.
                if (!_owner.IsWindingUp)
                {
                    _owner.BeginWindup();
                    return;
                }

                if (_owner.TickWindup(UnityEngine.Time.deltaTime)) _owner.TryAttackTarget();
            }
            public override void Exit() => _owner.EndWindup();
        }

        private class HurtState : StateBase
        {
            private readonly EnemyController _owner;
            public HurtState(EnemyController owner) { _owner = owner; }

            public override void Enter()
            {
                _owner._hurtTimer = _owner.Definition.HurtSeconds;
                _owner.StopMoving();
            }
            public override void Tick()
            {
                if (_owner.IsDead) { _owner._machine.ChangeTo<DeadState>(); return; }
                if (!_owner.IsHurt) _owner._machine.ChangeTo<ChaseState>();
            }
            public override void Exit() { }
        }

        private class DeadState : StateBase
        {
            private readonly EnemyController _owner;
            public DeadState(EnemyController owner) { _owner = owner; }

            public override void Enter() => _owner.OnDeath();
            public override void Tick()
            {
                if (_owner._deathTimer <= 0f) _owner.ReturnToPool();
            }
            public override void Exit() { }
        }
    }
}
