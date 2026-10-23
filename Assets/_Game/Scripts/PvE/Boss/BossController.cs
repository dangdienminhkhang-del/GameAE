using System;
using System.Threading.Tasks;
using Game.Core.AI;
using Game.Core.Character;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Floor;
using Game.Core.Presentation;
using Game.Core.World;
using Game.PvE.Presentation;
using Game.PvE.Zones;
using Game.Services;
using UnityEngine;

namespace Game.PvE.Boss
{
    /// <summary>
    /// Điều khiển Boss (docs/02 §13):
    /// - Quản lý máu, đòn đánh, Telegraph (báo hiệu), Punish Window (khựng đòn), Pressure Window (lao có telegraph) và Enrage.
    /// - Chu kỳ: Observe (chase + telegraph) → đánh → Punish → ... sau mỗi pressureEveryNAttacks đòn thường:
    ///   Telegraph Pressure → Lao → Recovery (Punish Window dài). Mọi đòn kiểm vùng trúng TẠI THỜI ĐIỂM đánh (BossHitRules).
    /// - Khi chết: phát GameEvents, gọi IWorldStateService để cập nhật tiến trình thế giới,
    ///   và xử lý thưởng First-Clear / Main Drops.
    /// - Lethal damage lên người chơi mang sourceId là Boss ID, kích hoạt Permadeath đúng chuẩn.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossController : MonoBehaviour, IDamageable, ICombatStats, IFloorActivationListener
    {
        [SerializeField] private BossDefinition definition;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool logBossEvents;

        [Tooltip("Phòng boss (ZoneVolume loại BossRoom). Đặt thì boss chỉ vào trận khi người chơi Ở TRONG phòng và kết thúc trận (về nhà, giữ nguyên máu/enrage) khi người chơi rời phòng. " +
                 "Để trống = hành vi cũ: chỉ theo DetectionRange.")]
        [SerializeField] private ZoneVolume encounterZone;
        private Rigidbody2D _rigidbody;
        private StateMachine _machine;
        private Transform _target;
        private double _health;
        private float _attackTimer;
        private float _stateTimer;
        private bool _isEnraged;
        private bool _isDefeatedProcessed;

        // Pressure Window
        private static readonly Color PressureTelegraphColor = new Color(0.75f, 0.2f, 1f, 1f); // tím đậm: khác vàng (đòn thường), xanh nhạt (punish), hồng (enrage)
        private static readonly Color AttackTelegraphColor = new Color(1f, 0.8f, 0.2f, 1f); // vàng/cam: báo đòn thường
        private static readonly Color PunishColor = new Color(0.7f, 0.7f, 1f, 1f);          // xanh nhạt: khựng lại (Punish/Recovery)
        private static readonly Color EnragedColor = new Color(1f, 0.5f, 0.5f, 1f);         // hồng: cuồng nộ
        private int _normalAttacksSincePressure;
        private Vector2 _pressureDirection = Vector2.right;
        private Vector2 _pressureDashStart;
        private readonly BossDashHitGate _pressureHitGate = new BossDashHitGate();

        private IWorldStateService _worldState;
        private ISaveService _saveService;
        private IDeathService _deathService;
        private Items.PlayerInventory _playerInventory;
        private BossDefeatFlow _defeatFlow;
        private bool _hasPendingDefeatReport;

        // Vị trí ban đầu để đặt lại khi vào lại tầng (rematch).
        private Vector3 _homePosition;
        private bool _homeCaptured;

        // Phòng boss (P9.7b): lọc "người chơi có trong phòng không" để không bắt đầu/kết thúc trận liên tục ở biên.
        private readonly ZonePresenceFilter _presence = new ZonePresenceFilter();
        private bool _playerInEncounterZone;
        private bool _returnInterruptedPressure;
        public event Action<BossController> Died;

        /// <summary>Server chưa xác nhận được World State; UI P8-5 có thể hiện nút thử lại.</summary>
        public event Action<Exception> DefeatReportFailed;

        /// <summary>Server đã xác nhận report boss, hoặc retry đã xác nhận thành công.</summary>
        public event Action DefeatReportConfirmed;

        /// <summary>
        /// Máu đổi: (hiện tại, tối đa, delta). delta &lt; 0 = bị trúng; delta = 0 = reset/đầy máu.
        /// Thanh máu boss trên HUD nghe sự kiện này (docs/05 Phase 9.5).
        /// </summary>
        public event Action<double, double, double> HealthChanged;

        /// <summary>
        /// Boss vào trận (true: đã rời Idle và đang đuổi/đánh) hay hết trận (false: về Idle, chết, hoặc bị tắt).
        /// Thanh máu boss và nhạc boss nghe sự kiện này. Chỉ báo khi trạng thái thật sự đổi.
        /// </summary>
        public event Action<BossController, bool> EngagedChanged;

        /// <summary>
        /// Một boss vừa bắt đầu báo đòn (telegraph): đòn thường hoặc đòn lao. Static để mọi boss (kể cả boss ở scene tầng nạp sau)
        /// dùng chung một đường nghe; SFX báo đòn nghe ở đây. Được xóa khi vào Play (Domain Reload có thể tắt).
        /// </summary>
        public static event Action<BossController> TelegraphStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            TelegraphStarted = null;
        }

        // Màu/nháy của sprite: mọi thay đổi màu đi qua SpriteTint để nháy không phá màu telegraph/enrage.
        private SpriteTint _tint;
        private bool _engaged;

        public bool IsEngaged => _engaged;

        private void EnsureTint()
        {
            if (_tint != null || spriteRenderer == null) return;
            _tint = new SpriteTint(spriteRenderer, null);
        }

        /// <summary>Đặt màu trạng thái (telegraph, punish, enrage...) kèm LOẠI màu. Nháy là lớp phủ trắng riêng nên không bao giờ đổi màu này; chỉ DangerTelegraph làm lớp phủ nhạt đi.</summary>
        private void SetTint(Color color, TintKind kind)
        {
            EnsureTint();
            if (_tint != null) _tint.SetStateTint(color, kind);
        }

        private void SetEngaged(bool value)
        {
            if (_engaged == value) return;
            _engaged = value;
            EngagedChanged?.Invoke(this, value);
        }

        public BossDefinition Definition => definition;
        public bool IsDead => _health <= 0.0;
        public double CurrentHealth => _health;
        public double MaxHealth => definition != null ? definition.MaxHealth : 100.0;
        public double Attack => definition != null ? (definition.Attack * (_isEnraged ? 1.2 : 1.0)) : 10.0;
        public double Defense => definition != null ? definition.Defense : 0.0;
        public double CriticalChance => 0.0;
        public double CriticalDamage => 1.0;
        public bool IsEnraged => _isEnraged;

        /// <summary>Boss đang trong trạng thái báo đòn hoặc lao tấn công (Telegraph / Pressure).</summary>
        public bool IsTelegraphing => _machine != null && (_machine.Current is TelegraphState || _machine.Current is PressureTelegraphState || _machine.Current is PressureDashState);

        /// <summary>Đã đủ số đòn thường để lần tấn công kế tiếp là Pressure.</summary>
        public bool IsPressureDue => definition != null && _normalAttacksSincePressure >= definition.PressureEveryNAttacks;

        /// <summary>Số đòn thường đã đánh kể từ Pressure gần nhất.</summary>
        public int NormalAttacksSincePressure => _normalAttacksSincePressure;

        public Transform Target => _target;
        public float DistanceToTarget => _target != null
            ? Vector2.Distance(transform.position, _target.position)
            : float.MaxValue;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            EnsureTint();
            CaptureHomeIfNeeded();

            InitializeStateMachine();
            ResetHealth();
        }

        private void Start()
        {
            AcquireServices();
            FindPlayerTarget();
        }

        private void Update()
        {
            if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;
            // Nháy chạy bằng thời gian THẬT để dừng hình không kéo dài nháy.
            if (_tint != null) _tint.Tick(UnityEngine.Time.unscaledDeltaTime);
            StepEncounter(Time.deltaTime);
        }

        /// <summary>Một nhịp logic: cập nhật "người chơi có trong phòng", áp luật phòng boss (chỉ khi có encounterZone), rồi chạy state hiện tại.</summary>
        private void StepEncounter(float deltaSeconds)
        {
            UpdatePlayerPresence(deltaSeconds);
            EvaluateEncounter();
            _machine?.Tick();
        }

        private void UpdatePlayerPresence(float deltaSeconds)
        {
            if (encounterZone == null || _target == null)
            {
                _presence.Reset();
                _playerInEncounterZone = false;
                return;
            }

            // Dùng ZoneVolume.Contains trực tiếp (không dùng ZoneTracker.CurrentZoneId: tracker GIỮ vùng cũ khi người chơi
            // đứng ngoài mọi vùng, nên không bao giờ báo "đã rời phòng").
            _playerInEncounterZone = _presence.Update(encounterZone.Contains(_target.position), deltaSeconds);
        }

        private BossEncounterPhase CurrentEncounterPhase()
        {
            Type state = _machine != null ? _machine.CurrentStateType : null;
            if (state == null || state == typeof(IdleState)) return BossEncounterPhase.Idle;
            if (state == typeof(DeadState)) return BossEncounterPhase.Dead;
            if (state == typeof(ReturnHomeState)) return BossEncounterPhase.ReturningHome;
            return BossEncounterPhase.Combat;
        }

        /// <summary>Áp luật phòng boss cho các state đang trong trận hoặc đang về nhà. Idle tự quyết định trong IdleState.Tick.</summary>
        private void EvaluateEncounter()
        {
            if (encounterZone == null || _machine == null || _target == null) return;

            BossEncounterPhase phase = CurrentEncounterPhase();
            if (phase == BossEncounterPhase.Idle || phase == BossEncounterPhase.Dead) return;

            BossEncounterDecision decision = BossEncounterRules.Decide(phase, true, _playerInEncounterZone, true);
            if (decision == BossEncounterDecision.ReturnHome)
            {
                Type state = _machine.CurrentStateType;
                _returnInterruptedPressure = state == typeof(PressureTelegraphState) || state == typeof(PressureDashState);
                _machine.ChangeTo<ReturnHomeState>();
            }
            else if (decision == BossEncounterDecision.Engage)
            {
                _machine.ChangeTo<ChaseState>();
            }
        }

        /// <summary>
        /// Người chơi rời phòng giữa trận: hủy mọi thứ đang chạy (không còn damage nào), về nhà, GIỮ NGUYÊN máu và enrage.
        /// KHÔNG gọi ResetHealth/ResetEncounter.
        /// </summary>
        private void BeginReturnHome()
        {
            StopMoving();
            _pressureHitGate.Reset();
            _stateTimer = 0f;

            if (definition != null)
            {
                _normalAttacksSincePressure = BossEncounterRules.PressureCounterAfterInterrupt(
                    _normalAttacksSincePressure, definition.PressureEveryNAttacks, _returnInterruptedPressure);
                _attackTimer = BossEncounterRules.ReengageCooldown(_attackTimer, CurrentAttackCooldown());
            }
            _returnInterruptedPressure = false;

            RestoreBaseColor(); // giữ màu enrage nếu đang enrage
            SetEngaged(false);   // trận kết thúc: thanh máu và nhạc boss tắt
        }

        private void OnDisable()
        {
            // Tầng bị tắt giữa trận: không để nhạc boss/thanh máu boss kẹt ở trạng thái "đang giao chiến".
            SetEngaged(false);
        }

        public void SetServices(
            IWorldStateService worldState,
            ISaveService saveService,
            Items.PlayerInventory inventory = null,
            IDeathService deathService = null)
        {
            _worldState = worldState;
            _saveService = saveService;
            _playerInventory = inventory;
            _deathService = deathService;
            RebuildDefeatFlow();
        }

        /// <summary>Injection seam cho bootstrap/scene: boss không tự tạo death service riêng.</summary>
        public void SetDeathService(IDeathService deathService)
        {
            _deathService = deathService;
        }

        private void AcquireServices()
        {
            if (_worldState == null) _worldState = ServiceProvider.CreateWorldStateService();
            if (_saveService == null) _saveService = ServiceProvider.CreateSaveService();
            if (_playerInventory == null) _playerInventory = FindFirstObjectByType<Items.PlayerInventory>();
            RebuildDefeatFlow();
        }

        private void RebuildDefeatFlow()
        {
            _defeatFlow = new BossDefeatFlow(_worldState, _saveService);
        }

        private void FindPlayerTarget()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _target = player.transform;
        }

        public void SetDefinition(BossDefinition def)
        {
            definition = def;
            if (spriteRenderer != null && definition != null && definition.Sprite != null)
            {
                spriteRenderer.sprite = definition.Sprite;
            }
            ResetHealth();
        }

        private void ResetHealth()
        {
            _health = MaxHealth;
            _isEnraged = false;
            _isDefeatedProcessed = false;
            _normalAttacksSincePressure = 0;
            HealthChanged?.Invoke(_health, MaxHealth, 0.0);
        }

        private void InitializeStateMachine()
        {
            _machine = new StateMachine();
            _machine.Register(new IdleState(this));
            _machine.Register(new ChaseState(this));
            _machine.Register(new TelegraphState(this));
            _machine.Register(new AttackState(this));
            _machine.Register(new PunishWindowState(this));
            _machine.Register(new PressureTelegraphState(this));
            _machine.Register(new PressureDashState(this));
            _machine.Register(new PressureRecoveryState(this));
            _machine.Register(new ReturnHomeState(this));
            _machine.Register(new DeadState(this));

            _machine.ChangeTo<IdleState>();
        }

        public void MoveToward(Vector2 destination)
        {
            if (definition == null || _rigidbody == null) return;
            Vector2 direction = (destination - (Vector2)transform.position).normalized;
            float speed = definition.MoveSpeed * (_isEnraged ? 1.25f : 1.0f);
            _rigidbody.linearVelocity = direction * speed;
            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.01f)
            {
                spriteRenderer.flipX = direction.x < 0f;
            }
        }

        public void StopMoving()
        {
            if (_rigidbody != null) _rigidbody.linearVelocity = Vector2.zero;
        }

        private void FaceTarget()
        {
            if (_target == null || spriteRenderer == null) return;
            float dx = _target.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f) spriteRenderer.flipX = dx < 0f;
        }

        private void RestoreBaseColor()
        {
            SetTint(_isEnraged ? EnragedColor : Color.white, _isEnraged ? TintKind.Enrage : TintKind.Base);
        }

        /// <summary>Vào lại tầng (rematch): màu, nháy, trạng thái chết của sprite về sạch.</summary>
        private void ResetVisuals()
        {
            EnsureTint();
            if (_tint != null) _tint.ResetAll(_isEnraged ? EnragedColor : Color.white, _isEnraged ? TintKind.Enrage : TintKind.Base);
        }

        private float CurrentAttackCooldown()
        {
            return definition.AttackCooldownSeconds * (_isEnraged ? 0.7f : 1.0f);
        }

        // ── IDamageable ──────────────────────────────────────────────────────────

        public void TakeDamage(DamageInfo damage)
        {
            if (damage == null || IsDead) return;

            double healthBefore = _health;
            _health = Math.Max(0.0, _health - damage.Amount);

            // Kiểm tra ngưỡng Enrage
            if (!_isEnraged && definition != null && (_health / MaxHealth) <= definition.EnrageHpThreshold)
            {
                _isEnraged = true;
                SetTint(EnragedColor, TintKind.Enrage);
                if (logBossEvents) Debug.Log($"[Boss:{definition.DisplayName}] KÍCH HOẠT CUỒNG NỘ (Enrage)!", this);
            }

            if (logBossEvents) Debug.Log($"[Boss:{definition?.DisplayName}] Nhận {damage.Amount:0.#} dmg → Còn {_health:0.#}/{MaxHealth}", this);

            if (!IsDead)
            {
                EnsureTint();
                if (_tint != null) _tint.Flash();
            }
            HealthChanged?.Invoke(_health, MaxHealth, _health - healthBefore);

            if (IsDead)
            {
                _machine.ChangeTo<DeadState>();
            }
        }

        // ── IFloorActivationListener (rematch) ──────────────────────────────────────

        /// <summary>Vào (hoặc vào lại) tầng: reset trận đấu để đánh lại được (docs/12 §7.2).</summary>
        public void OnFloorActivated(FloorDefinition def)
        {
            ResetEncounter();
        }

        /// <summary>Boss đứng yên khi tầng bị tắt; không cần dọn thêm vì reset xảy ra lúc vào lại.</summary>
        public void OnFloorDeactivated() { }

        /// <summary>
        /// Đưa boss về trạng thái ban đầu: máu đầy, hết cuồng nộ, về vị trí gốc, về Idle, bật lại nếu đang tắt.
        /// KHÔNG đụng luồng báo server: cờ retry báo cáo đang chờ (_hasPendingDefeatReport) được giữ nguyên.
        /// </summary>
        public void ResetEncounter()
        {
            CaptureHomeIfNeeded();

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            ResetHealth();
            _attackTimer = 0f;
            _stateTimer = 0f;
            _pressureHitGate.Reset();
            _presence.Reset();
            _playerInEncounterZone = false;
            _returnInterruptedPressure = false;

            transform.position = _homePosition;
            StopMoving();
            ResetVisuals();

            _machine?.ChangeTo<IdleState>();
        }

        private void CaptureHomeIfNeeded()
        {
            if (_homeCaptured) return;
            _homePosition = transform.position;
            _homeCaptured = true;
        }

        public void Heal(double amount)
        {
            if (amount <= 0.0 || IsDead) return;
            _health = Math.Min(MaxHealth, _health + amount);
        }

        public void SetMaxHealth(double max, bool resetCurrent = false)
        {
            if (resetCurrent) _health = max;
        }

        // ── Boss Actions ─────────────────────────────────────────────────────────

        /// <summary>
        /// Đòn thường. Vùng đánh được kiểm TẠI THỜI ĐIỂM đánh (không phải lúc bắt đầu telegraph):
        /// người chơi đã lùi/né ra ngoài <see cref="BossDefinition.AttackHitRadius"/> thì trượt.
        /// </summary>
        public void PerformAttack()
        {
            if (definition == null || _target == null) return;

            _normalAttacksSincePressure++;

            if (BossHitRules.IsInsideHitArea(transform.position, _target.position, definition.AttackHitRadius))
            {
                DealDamageToTarget(1.0, "Đánh trúng");
            }
            else if (logBossEvents)
            {
                Debug.Log($"[Boss:{definition.DisplayName}] Đòn thường TRƯỢT: mục tiêu đã ra ngoài vùng đánh ({definition.AttackHitRadius:0.##}).", this);
            }

            _attackTimer = CurrentAttackCooldown();
        }

        /// <summary>
        /// Gây damage lên mục tiêu (nếu còn sống). <paramref name="damageMultiplier"/> khác 1 dùng cho Pressure.
        /// Luôn truyền sourceId = Boss ID để permadeath ghi đúng lý do (docs/09 §1.3).
        /// </summary>
        private bool DealDamageToTarget(double damageMultiplier, string logLabel)
        {
            if (definition == null || _target == null) return false;

            var damageable = _target.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead) return false;

            double defense = AttackExecutor.ResolveDefense(damageable);
            string sourceId = definition.Id;
            DamageInfo damage = AttackExecutor.ResolveDamage(this, defense, null, UnityEngine.Random.value, sourceId);
            if (Math.Abs(damageMultiplier - 1.0) > double.Epsilon)
            {
                damage.Amount = BossHitRules.ResolvePressureDamage(damage.Amount, damageMultiplier);
            }
            damageable.TakeDamage(damage);

            if (logBossEvents) Debug.Log($"[Boss:{definition.DisplayName}] {logLabel} {damage.Amount:0.#} dmg → {damageable}", this);
            return true;
        }

        // ── Pressure Window ──────────────────────────────────────────────────────

        /// <summary>Khóa hướng lao theo vị trí mục tiêu NGAY LÚC telegraph kết thúc.</summary>
        private void LockPressureDirection()
        {
            Vector2 fallback = spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right;
            Vector2 origin = transform.position;
            Vector2 targetPosition = _target != null ? (Vector2)_target.position : origin + fallback;
            _pressureDirection = BossHitRules.ResolveDashDirection(origin, targetPosition, fallback);

            if (spriteRenderer != null && Mathf.Abs(_pressureDirection.x) > 0.01f)
            {
                spriteRenderer.flipX = _pressureDirection.x < 0f;
            }
        }

        private void BeginPressureDash()
        {
            _pressureDashStart = transform.position;
            _pressureHitGate.Reset();
            _attackTimer = CurrentAttackCooldown();

            float speed = definition.PressureDashDistance / definition.PressureDashSeconds;
            if (_rigidbody != null) _rigidbody.linearVelocity = _pressureDirection * speed;

            if (logBossEvents)
            {
                Vector2 end = BossHitRules.ResolveDashEnd(_pressureDashStart, _pressureDirection, definition.PressureDashDistance);
                Debug.Log($"[Boss:{definition.DisplayName}] LAO {_pressureDashStart} → {end}", this);
            }
        }

        /// <summary>Mỗi frame trong lúc lao: mục tiêu lọt vào hành lang đã quét thì dính đòn, đúng một lần mỗi lần lao.</summary>
        private void ResolvePressureHit()
        {
            if (definition == null || _target == null) return;
            if (!_pressureHitGate.TryHit(_pressureDashStart, transform.position, _target.position, definition.PressureHitRadius)) return;

            DealDamageToTarget(definition.PressureDamageMultiplier, "Đòn lao trúng");
        }

        /// <summary>
        /// Vào luồng boss chết. Trả <see cref="Task"/> để không nuốt lỗi như <c>async void</c>;
        /// DeadState chỉ fire-and-forget vì StateMachine không có async contract.
        /// </summary>
        public Task HandleDefeatAsync()
        {
            if (_isDefeatedProcessed) return Task.CompletedTask;
            _isDefeatedProcessed = true;
            return HandleDefeatInternalAsync();
        }

        private async Task HandleDefeatInternalAsync()
        {

            StopMoving();
            EnsureTint();
            if (_tint != null) _tint.SetDead(CombatFeelConfig.Active.BossDeadTint, false);

            // 1. Domain Events (phát ngay, không chờ World State)
            GameEvents.RaiseEnemyDefeated(this);
            if (definition != null)
            {
                GameEvents.RaiseBossDefeated(this, new BossDefeatedArgs
                {
                    BossId = definition.Id,
                    FloorId = definition.FloorId
                });
            }

            Died?.Invoke(this);

            if (definition == null) return;

            // Chốt definition: SetDefinition có thể đổi trong lúc đang await.
            BossDefinition defeated = definition;

            if (_defeatFlow == null) RebuildDefeatFlow();

            // 2. Rematch? Đọc MỘT lần; EXP, đồ hiếm và báo cáo dùng cùng giá trị này.
            //    Đọc lỗi → false (không-rematch, như hành vi cũ) nên vẫn có EXP.
            bool isRematch = await _defeatFlow.ResolveIsRematchAsync(defeated.Id);

            // Rematch KHÔNG cho EXP (docs/02 §24): chỉ đồ chính và đồ hiếm.
            if (!isRematch)
            {
                GameEvents.RaiseExperienceGained(this, defeated.ExperienceReward);
            }

            BossDefeatFlowResult result;
            try
            {
                result = await _defeatFlow.HandleDefeatAsync(
                    defeated,
                    _deathService != null && _deathService.IsPlayerDead,
                    _playerInventory != null ? new Func<Game.Core.Items.ItemDefinition, int, int>(GrantItem) : null,
                    knownRematch: isRematch);
            }
            catch (Exception error)
            {
                // Không để Task bị fault khi DeadState fire-and-forget. Đây không phải xác nhận report;
                // event failure cho UI chỉ thuộc trường hợp report trả lỗi có retry được.
                Debug.LogError($"[BossController] Lỗi xử lý boss chết: {error}", this);
                return;
            }

            if (result.ReportStatus == BossDefeatReportStatus.Confirmed)
            {
                _hasPendingDefeatReport = false;
                DefeatReportConfirmed?.Invoke();
            }
            else if (result.ReportStatus == BossDefeatReportStatus.Failed)
            {
                _hasPendingDefeatReport = true;
                Debug.LogWarning($"[BossController] Server chưa xác nhận boss '{definition.Id}': {result.ReportError?.Message}", this);
                DefeatReportFailed?.Invoke(result.ReportError);
            }

            if (result.FirstClearResult == FirstClearClaimResult.Unconfirmed)
            {
                Debug.LogWarning("[BossController] First-Clear chưa xác nhận; không cấp bonus và không ghi đè RewardClaims.", this);
            }
            else if (result.FirstClearResult == FirstClearClaimResult.Claimed && logBossEvents)
            {
                Debug.Log($"[BossController] Người chơi nhận thưởng First-Clear: {definition.FirstClearRewardItem.DisplayName} x{definition.FirstClearRewardQuantity}");
            }
        }

        /// <summary>
        /// UI P8-5 gọi sau <see cref="DefeatReportFailed"/>. Chỉ gửi lại idempotent World State
        /// report; không phát EXP/event, không claim và không roll drop lần nữa.
        /// </summary>
        public async Task RetryDefeatReportAsync()
        {
            if (!_hasPendingDefeatReport || definition == null) return;
            if (_defeatFlow == null) RebuildDefeatFlow();

            var retry = await _defeatFlow.RetryDefeatReportAsync(definition);
            if (retry.status == BossDefeatReportStatus.Confirmed)
            {
                _hasPendingDefeatReport = false;
                DefeatReportConfirmed?.Invoke();
                return;
            }

            _hasPendingDefeatReport = true;
            Debug.LogWarning($"[BossController] Retry boss report chưa xác nhận: {retry.error?.Message}", this);
            DefeatReportFailed?.Invoke(retry.error);
        }

        private int GrantItem(Game.Core.Items.ItemDefinition item, int quantity)
        {
            return _playerInventory != null ? _playerInventory.TryAddItem(item, quantity) : 0;
        }

        // ── States ───────────────────────────────────────────────────────────────

        private class IdleState : StateBase
        {
            private readonly BossController _owner;
            public IdleState(BossController owner) { _owner = owner; }

            public override void Enter()
            {
                _owner.StopMoving();
                _owner.SetEngaged(false);
            }

            public override void Tick()
            {
                if (_owner._target == null) _owner.FindPlayerTarget();
                if (_owner._target == null) return;

                bool hasZone = _owner.encounterZone != null;
                bool inRange = _owner.DistanceToTarget <= _owner.definition.DetectionRange;
                BossEncounterDecision decision = BossEncounterRules.Decide(
                    BossEncounterPhase.Idle, hasZone, _owner._playerInEncounterZone, inRange);
                if (decision == BossEncounterDecision.Engage)
                {
                    _owner._machine.ChangeTo<ChaseState>();
                }
            }

            public override void Exit() { }
        }

        private class ChaseState : StateBase
        {
            private readonly BossController _owner;
            public ChaseState(BossController owner) { _owner = owner; }

            public override void Enter() { }

            public override void Tick()
            {
                if (_owner._target == null)
                {
                    _owner._machine.ChangeTo<IdleState>();
                    return;
                }

                _owner.SetEngaged(true);

                float dist = _owner.DistanceToTarget;
                if (_owner._attackTimer <= 0f)
                {
                    if (_owner.IsPressureDue)
                    {
                        // Đến lượt Pressure: bắt đầu telegraph khi mục tiêu trong tầm với của một cú lao.
                        if (dist <= _owner.definition.PressureTriggerRange)
                        {
                            _owner._machine.ChangeTo<PressureTelegraphState>();
                            return;
                        }
                    }
                    else if (dist <= _owner.definition.AttackRange)
                    {
                        _owner._machine.ChangeTo<TelegraphState>();
                        return;
                    }
                }

                if (dist <= _owner.definition.AttackRange)
                {
                    _owner.StopMoving();
                }
                else
                {
                    _owner.MoveToward(_owner._target.position);
                }
            }

            public override void Exit() => _owner.StopMoving();
        }

        private class TelegraphState : StateBase
        {
            private readonly BossController _owner;
            public TelegraphState(BossController owner) { _owner = owner; }

            public override void Enter()
            {
                _owner.StopMoving();
                _owner._stateTimer = _owner.definition.AttackWindupSeconds;
                TelegraphStarted?.Invoke(_owner);
                _owner.SetTint(AttackTelegraphColor, TintKind.DangerTelegraph); // Cảnh báo vàng/cam
            }

            public override void Tick()
            {
                _owner._stateTimer -= Time.deltaTime;
                if (_owner._stateTimer <= 0f)
                {
                    _owner._machine.ChangeTo<AttackState>();
                }
            }

            public override void Exit() => _owner.RestoreBaseColor();
        }

        private class AttackState : StateBase
        {
            private readonly BossController _owner;
            public AttackState(BossController owner) { _owner = owner; }

            public override void Enter()
            {
                _owner.StopMoving();
                _owner.PerformAttack();
                _owner._machine.ChangeTo<PunishWindowState>();
            }

            public override void Tick() { }
            public override void Exit() { }
        }

        private class PunishWindowState : StateBase
        {
            private readonly BossController _owner;
            public PunishWindowState(BossController owner) { _owner = owner; }

            public override void Enter()
            {
                _owner.StopMoving();
                _owner._stateTimer = _owner.definition.PunishWindowSeconds;
                _owner.SetTint(PunishColor, TintKind.Window); // Màu khựng lại (xanh nhạt)
            }

            public override void Tick()
            {
                _owner._stateTimer -= Time.deltaTime;
                if (_owner._stateTimer <= 0f)
                {
                    _owner._machine.ChangeTo<ChaseState>();
                }
            }

            public override void Exit() => _owner.RestoreBaseColor();
        }

        /// <summary>Telegraph Pressure: đứng im, nhìn theo mục tiêu, màu tím; hướng lao khóa khi hết giờ.</summary>
        private class PressureTelegraphState : StateBase
        {
            private readonly BossController _owner;
            public PressureTelegraphState(BossController owner) { _owner = owner; }

            public override void Enter()
            {
                _owner.StopMoving();
                _owner._normalAttacksSincePressure = 0;
                _owner._stateTimer = _owner.definition.PressureWindupSeconds;
                TelegraphStarted?.Invoke(_owner);
                _owner.SetTint(PressureTelegraphColor, TintKind.DangerTelegraph);
                if (_owner.logBossEvents) Debug.Log($"[Boss:{_owner.definition.DisplayName}] TELEGRAPH PRESSURE ({_owner.definition.PressureWindupSeconds:0.##}s)", _owner);
            }

            public override void Tick()
            {
                _owner.FaceTarget();
                _owner._stateTimer -= Time.deltaTime;
                if (_owner._stateTimer <= 0f)
                {
                    _owner.LockPressureDirection();
                    _owner._machine.ChangeTo<PressureDashState>();
                }
            }

            public override void Exit() => _owner.RestoreBaseColor();
        }

        /// <summary>Lao theo hướng đã khóa bằng Rigidbody2D.linearVelocity; dừng khi hết giờ. Một lần lao = tối đa một lần damage.</summary>
        private class PressureDashState : StateBase
        {
            private readonly BossController _owner;
            public PressureDashState(BossController owner) { _owner = owner; }

            public override void Enter()
            {
                _owner._stateTimer = _owner.definition.PressureDashSeconds;
                _owner.SetTint(PressureTelegraphColor, TintKind.DangerTelegraph);
                _owner.BeginPressureDash();
            }

            public override void Tick()
            {
                _owner._stateTimer -= Time.deltaTime;
                _owner.ResolvePressureHit();
                if (_owner._stateTimer <= 0f)
                {
                    _owner._machine.ChangeTo<PressureRecoveryState>();
                }
            }

            public override void Exit()
            {
                _owner.StopMoving();
                _owner.RestoreBaseColor();
            }
        }

        /// <summary>Recovery sau lao = Punish Window dài (cùng màu xanh nhạt với Punish thường).</summary>
        private class PressureRecoveryState : StateBase
        {
            private readonly BossController _owner;
            public PressureRecoveryState(BossController owner) { _owner = owner; }

            public override void Enter()
            {
                _owner.StopMoving();
                _owner._stateTimer = _owner.definition.PressureRecoverySeconds;
                _owner.SetTint(PunishColor, TintKind.Window);
            }

            public override void Tick()
            {
                _owner._stateTimer -= Time.deltaTime;
                if (_owner._stateTimer <= 0f)
                {
                    _owner._machine.ChangeTo<ChaseState>();
                }
            }

            public override void Exit() => _owner.RestoreBaseColor();
        }

        /// <summary>
        /// Người chơi đã rời phòng boss: đi về vị trí gốc rồi về Idle. Máu và enrage giữ nguyên. Người chơi vào lại phòng
        /// thì EvaluateEncounter chuyển sang Chase ngay (không đợi về tới nhà).
        /// </summary>
        private class ReturnHomeState : StateBase
        {
            private readonly BossController _owner;
            public ReturnHomeState(BossController owner) { _owner = owner; }

            public override void Enter() => _owner.BeginReturnHome();

            public override void Tick()
            {
                _owner._stateTimer += Time.deltaTime;

                float distanceToHome = Vector2.Distance(_owner.transform.position, _owner._homePosition);
                if (BossEncounterRules.HasArrivedHome(distanceToHome) || BossEncounterRules.ReturnTimedOut(_owner._stateTimer))
                {
                    _owner._machine.ChangeTo<IdleState>();
                    return;
                }

                _owner.MoveToward(_owner._homePosition);
            }

            public override void Exit() => _owner.StopMoving();
        }

        private class DeadState : StateBase
        {
            private readonly BossController _owner;
            public DeadState(BossController owner) { _owner = owner; }

            public override void Enter()
            {
                _owner.SetEngaged(false);
                _ = _owner.HandleDefeatAsync();
            }

            public override void Tick() { }
            public override void Exit() { }
        }
    }
}
