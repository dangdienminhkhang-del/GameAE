using System;
using Game.Core.AI;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Presentation;
using Game.PvE.Enemy.Behaviors;
using Game.PvE.Enemy.Projectile;
using Game.PvE.Presentation;
using Game.PvE.SafeZone;
using Game.PvE.Zones;
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
        private IEnemyBehavior _behavior;
        private EnemyDefinition _behaviorDefinition;
        // Dây xích với Safe Zone (Phase 9.7a): vị trí gốc, mục tiêu có đang trong Safe Zone không (kiểm theo chu kỳ), đang quay về.
        private Vector2 _home;
        private bool _targetInSafeZone;
        private bool _returningHome;
        private readonly SafeZoneCheckThrottle _safeZoneThrottle = new SafeZoneCheckThrottle();

        public event Action<EnemyController> Died;

        /// <summary>
        /// Máu đổi: (hiện tại, tối đa, delta). delta &lt; 0 = bị trúng; delta = 0 và đầy máu = vừa hồi sinh/reset.
        /// Thanh máu nghe sự kiện này thay vì poll mỗi frame (docs/05 Phase 9.5).
        /// </summary>
        public event Action<double, double, double> HealthChanged;

        /// <summary>
        /// Một quái vừa bắt đầu báo đòn (telegraph). Static để hệ hướng dẫn (Phase 9.9) nghe chung mọi quái;
        /// được xóa khi vào Play (Enter Play Mode Options có thể tắt Domain Reload).
        /// </summary>
        public static event Action<EnemyController> WindupStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            WindupStarted = null;
        }

        // Màu/nháy/tan của sprite: mọi thay đổi màu đi qua SpriteTint để nháy không phá màu báo đòn.
        private static readonly Color TelegraphColor = new Color(1f, 0.55f, 0.55f, 1f);
        private SpriteTint _tint;
        private float _deathDuration;

        private void EnsureTint()
        {
            if (_tint != null || spriteRenderer == null) return;
            _tint = new SpriteTint(spriteRenderer, transform);
        }

        public EnemyDefinition Definition => definition;
        private int _overrideLevel;
        public int Level => _overrideLevel > 0 ? _overrideLevel : (definition != null ? definition.Level : 1);
        public void SetLevel(int level) => _overrideLevel = Mathf.Max(1, level);

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

        /// <summary>Mục tiêu đang đứng trong Safe Zone (cập nhật mỗi ~0.2 giây). Khi true quái không đuổi và quay về.</summary>
        public bool TargetInSafeZone => _targetInSafeZone;

        /// <summary>Vị trí quái xuất hiện lần gần nhất (ghi nhận trong ResetForSpawn).</summary>
        public Vector2 HomePosition => _home;

        public bool IsReturningHome => _returningHome;
        public bool IsDead => _health <= 0.0;

        /// <summary>Quái bị khóa hành động (bị Choáng / Tê Liệt / Stun / Paralyze).</summary>
        public bool IsActionLocked { get; set; }

        /// <summary>
        /// Tầm phát hiện hiệu dụng của quái (tự động thu hẹp khi người chơi kích hoạt Ẩn Nấp - Hiding theo SAO).
        /// </summary>
        public float EffectiveDetectionRange => Game.Core.Skills.SearchingHidingRules.CalculateEffectiveDetectionRange(
            definition != null ? definition.DetectionRange : 5.0f,
            Skills.SearchingHidingController.CurrentPlayerHidingFactor);

        public double CurrentHealth => _health;
        public double MaxHealth => definition != null ? ScaleStat(definition.MaxHealth, _floorPower) : 1.0;
        public double Attack => definition != null ? ScaleStat(definition.Attack, _floorPower) : 0.0;
        public double Defense => definition != null ? ScaleStat(definition.Defense, _floorPower) : 0.0;

        /// <summary>EXP thật quái này cho khi chết: EXP gốc × hệ số EXP của tầng, làm tròn theo ScaleExperience.</summary>
        public int ExperienceReward => definition != null ? ScaleExperience(definition.ExperienceReward, _expMultiplier) : 0;

        /// <summary>
        /// Chỉ số sau khi nhân hệ số tầng, làm tròn về số nguyên: Math.Round + MidpointRounding.AwayFromZero (docs/07 §6).
        /// Không làm tròn thì số thực kiểu 50 × 1.6f = 80.0000012 khiến quái sống dư một đòn.
        /// </summary>
        public static double ScaleStat(double baseValue, float multiplier)
        {
            return Math.Round(baseValue * multiplier, MidpointRounding.AwayFromZero);
        }

        /// <summary>EXP sau khi nhân hệ số tầng, làm tròn cùng quy tắc với ScaleStat (docs/07 §6).</summary>
        public static int ScaleExperience(int baseExperience, float multiplier)
        {
            return (int)Math.Round(baseExperience * (double)multiplier, MidpointRounding.AwayFromZero);
        }

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
            WindupStarted?.Invoke(this);
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
            EnsureTint();
            if (_tint == null) return;
            _tint.SetStateTint(on ? TelegraphColor : Color.white, on ? TintKind.DangerTelegraph : TintKind.Base);
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.freezeRotation = true;
            if (spriteRenderer == null) TryGetComponent(out spriteRenderer);
            EnsureTint();

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
            _home = transform.position;
            _targetInSafeZone = false;
            _returningHome = false;
            _safeZoneThrottle.Reset();
            EnsureBehavior();
            _behavior.Reset();
            if (definition != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = definition.Sprite;
            }

            // Đưa màu, độ mờ, scale và nháy về trạng thái sạch (quái dùng lại từ pool không còn mờ/nhỏ/đang nháy).
            EnsureTint();
            if (_tint != null) _tint.ResetAll(Color.white);
            HealthChanged?.Invoke(_health, MaxHealth, 0.0);
            _machine.ChangeTo<IdleState>();
        }

        private void Update()
        {
            // Guard: object có thể tồn tại trong scene mà Awake chưa chạy (rác lưu nhầm trong scene).
            if (_machine == null) return;

            if (IsActionLocked)
            {
                if (_rigidbody != null) _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            AcquireTarget();
            TickTimers();
            UpdateSafeZoneCache();
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

        /// <summary>Kiểm mục tiêu có trong Safe Zone không, mỗi ~0.2 giây thay vì mỗi frame.</summary>
        private void UpdateSafeZoneCache()
        {
            if (!_safeZoneThrottle.Tick(Time.deltaTime)) return;
            _targetInSafeZone = _target != null && SafeZoneProvider.GetOrCreate().IsSafeZoneAt(_target.position);
        }

        /// <summary>Người chơi vào Safe Zone: bỏ mục tiêu, hủy báo đòn, quay về vị trí gốc rồi về Idle.</summary>
        private void DropTargetAndReturn()
        {
            _returningHome = true;
            EndWindup();
            _machine.ChangeTo<IdleState>();
        }

        private void StepReturnHome()
        {
            if (EnemyLeashRules.HasArrivedHome(transform.position, _home))
            {
                _returningHome = false;
                StopMoving();
                return;
            }
            MoveToward(_home);
        }

        private void TickTimers()
        {
            if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;
            if (_hurtTimer > 0f) _hurtTimer -= Time.deltaTime;
            // Nháy chạy bằng thời gian THẬT để dừng hình không kéo dài nháy.
            if (_tint != null) _tint.Tick(UnityEngine.Time.unscaledDeltaTime);
            if (IsDead && _deathTimer > 0f)
            {
                _deathTimer -= Time.deltaTime;
                if (_tint != null && _deathDuration > 0f)
                {
                    _tint.SetDeathProgress(1f - Mathf.Clamp01(_deathTimer / _deathDuration));
                }
                if (_deathTimer <= 0f) ReturnToPool();
            }
        }

        public void MoveToward(Vector2 destination)
        {
            MoveInDirection((destination - (Vector2)transform.position).normalized);
        }

        /// <summary>Di chuyển theo hướng đơn vị (zero = đứng yên). Dùng chung cho mọi vai trò.</summary>
        public void MoveInDirection(Vector2 direction)
        {
            if (definition == null) return;
            _rigidbody.linearVelocity = direction * definition.MoveSpeed;
            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.01f) spriteRenderer.flipX = direction.x < 0f;
        }

        // ── Điểm nối hành vi theo vai trò (Phase 9.8) ──

        private void EnsureBehavior()
        {
            if (_behavior != null && ReferenceEquals(_behaviorDefinition, definition)) return;
            _behaviorDefinition = definition;
            _behavior = EnemyBehaviorFactory.Create(EnemyRoleConfig.From(definition));
        }

        private IEnemyBehavior Behavior
        {
            get
            {
                EnsureBehavior();
                return _behavior;
            }
        }

        private BehaviorInput BuildInput()
        {
            Vector2 target = _target != null ? (Vector2)_target.position : (Vector2)transform.position;
            return new BehaviorInput(transform.position, target, DistanceToTarget, Time.deltaTime);
        }

        /// <summary>Đòn đánh khi hết lấy đà: cận chiến hay bắn đạn tùy vai trò.</summary>
        private void PerformStrike()
        {
            Behavior.Strike(this);
        }

        /// <summary>Bắt đầu hồi chiêu (behavior gọi sau khi bắn).</summary>
        public void StartAttackCooldown()
        {
            if (definition != null) _attackTimer = definition.AttackCooldownSeconds;
        }

        /// <summary>
        /// Tính damage lên mục tiêu hiện tại theo đúng công thức đòn cận chiến, nhân hệ số <paramref name="scale"/>
        /// (đạn: projectileDamageScale). Trả null nếu không có mục tiêu còn sống.
        /// </summary>
        public DamageInfo CreateAttackDamage(float scale)
        {
            if (definition == null || _target == null) return null;
            var damageable = _target.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead) return null;

            double defense = AttackExecutor.ResolveDefense(damageable);
            DamageInfo damage = AttackExecutor.ResolveDamage(this, defense, null, UnityEngine.Random.value, definition.Id);
            damage.Amount = ProjectileMath.ScaleDamage(damage.Amount, scale);
            return damage;
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

            double healthBefore = _health;
            _health = Math.Max(0.0, _health - damage.Amount);
            EndWindup();
            if (!IsDead)
            {
                EnsureTint();
                if (_tint != null) _tint.Flash();
            }
            HealthChanged?.Invoke(_health, MaxHealth, _health - healthBefore);
            if (logHits) Debug.Log($"[Enemy:{definition?.DisplayName}] nhận {damage.Amount:0.#} → {_health:0.#}", this);

            if (IsDead) _machine.ChangeTo<DeadState>();
            else _machine.ChangeTo<HurtState>();
        }

        public void Heal(double amount)
        {
            if (amount <= 0.0 || IsDead) return;
            double healthBefore = _health;
            _health = Math.Min(MaxHealth, _health + amount);
            if (_health > healthBefore) HealthChanged?.Invoke(_health, MaxHealth, _health - healthBefore);
        }

        public void SetMaxHealth(double maxHp, bool resetCurrent = false)
        {
            if (definition == null) return;
            if (resetCurrent) _health = definition.MaxHealth;
        }

        public void OnDeath()
        {
            _deathTimer = definition != null ? definition.DeathSeconds : 0.5f;
            _deathDuration = _deathTimer;
            EnsureTint();
            if (_tint != null)
            {
                // Xám rồi mờ dần + thu nhỏ trong deathSeconds (tiến độ do TickTimers đẩy); ResetForSpawn trả lại như mới.
                CombatFeelConfig feel = CombatFeelConfig.Active;
                Color gray = feel.DeadTint;
                _tint.SetDead(new Color(gray.r, gray.g, gray.b, feel.DeathStartAlpha), true);
            }
            StopMoving();

            GameEvents.RaiseEnemyDefeated(this);
            if (definition != null)
            {
                GameEvents.RaiseExperienceGained(this, ExperienceReward);
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
                bool shouldChase = EnemyLeashRules.ShouldChase(
                    _owner.Target != null, _owner._targetInSafeZone, _owner.DistanceToTarget, _owner.EffectiveDetectionRange);
                if (_owner._returningHome && !shouldChase)
                {
                    _owner.StepReturnHome();
                    return;
                }
                if (shouldChase)
                    _owner._machine.ChangeTo<ChaseState>();
            }
            public override void Exit() { }
        }

        private class ChaseState : StateBase
        {
            private readonly EnemyController _owner;
            public ChaseState(EnemyController owner) { _owner = owner; }

            public override void Enter() { _owner._returningHome = false; }
            public override void Tick()
            {
                if (_owner.Target == null) { _owner._machine.ChangeTo<IdleState>(); return; }
                if (EnemyLeashRules.ShouldDropTarget(_owner._targetInSafeZone)) { _owner.DropTargetAndReturn(); return; }
                float distance = _owner.DistanceToTarget;
                BehaviorInput input = _owner.BuildInput();
                IEnemyBehavior behavior = _owner.Behavior;
                if (behavior.ShouldEngage(input))
                {
                    _owner._machine.ChangeTo<AttackState>();
                    return;
                }
                if (distance > _owner.EffectiveDetectionRange * 1.5f)
                {
                    _owner._machine.ChangeTo<IdleState>();
                    return;
                }
                _owner.MoveInDirection(behavior.ChaseDirection(input));
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
                if (EnemyLeashRules.ShouldDropTarget(_owner._targetInSafeZone))
                {
                    _owner.DropTargetAndReturn();
                    return;
                }
                if (_owner.Behavior.ShouldDisengage(_owner.BuildInput()))
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

                if (_owner.TickWindup(UnityEngine.Time.deltaTime)) _owner.PerformStrike();
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
