using System;
using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Input;
using Game.Core.Items;
using Game.Core.Skills;
using Game.Core.Stats;
using Game.PvE.Dodge;
using Game.PvE.Items;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Skills
{
    /// <summary>
    /// Component MỚI — người phát triển gắn vào Player prefab trong Unity Editor.
    /// KHÔNG sửa PlayerAttack.cs, PlayerController.cs hay PvEBootstrap.cs hiện có.
    /// Quản lý 2 input slot Core và Traverse (Mốc 1); chạy state machine WindUp → Active → Recovery;
    /// validate mastery/discipline/cooldown; gây damage theo từng hit window; chống duplicate hit;
    /// cộng mastery credit Technique (per-enemy cap 4). Persistence qua IMasteryService.
    ///
    /// Wave 2A fixes:
    ///   - Log error một lần nếu targetLayers rỗng (BUG chặn).
    ///   - playerInventory: GetComponent → FindAnyObjectByType → log error.
    ///   - Dirty flag + 2s delay save; flush khi pause/quit/disable.
    ///   - TryUseTechnique bị từ chối khi DodgeController đang Dodge.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class TechniqueController : MonoBehaviour
    {
        [Header("Refs (tự tìm nếu bỏ trống)")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private TechniqueDatabase techniqueDatabase;
        [SerializeField] private ItemDatabase itemDatabase;

        [Header("Presentation (tùy chọn)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string triggerCoreTechnique = "";
        [SerializeField] private string triggerTraverseTechnique = "";

        [Header("Debug")]
        [SerializeField] private bool logHits = false;

        // ── Services / helpers ──
        private IMasteryService _mastery;
        private readonly MasteryCreditTracker _creditTracker = new MasteryCreditTracker();
        private readonly HitDetection _hitDetection = new HitDetection();
        private readonly HitInstanceProtector _hitProtector = new HitInstanceProtector();
        private readonly Dictionary<TechniqueInputSlot, CooldownSystem> _slotCooldown = new Dictionary<TechniqueInputSlot, CooldownSystem>();

        // ── Dodge coordination ──
        private DodgeController _dodgeController;

        // ── Active execution state ──
        private WeaponTechniqueDefinition _activeDefinition;
        private TechniqueExecutionState _state = TechniqueExecutionState.Idle;
        private float _stateEnterTime;
        private long _activeActivationId;
        private WeaponDiscipline _lastKnownDiscipline = WeaponDiscipline.None;
        private int _lastKnownMastery;

        // ── Dirty save (delay 2s) ──
        private bool _masteryDirty;
        private float _dirtySince;
        private float _nextSaveAttemptAt;
        private bool _saveInFlight;
        private bool _masteryEventsSubscribed;
        private const float SaveDelaySeconds = 2f;

        // ── One-time error flags ──
        private bool _loggedTargetLayersError;
        private bool _loggedInventoryError;

        // ── Events ──
        public event EventHandler<TechniqueValidationError> OnValidationFailed;
        public event EventHandler<TechniqueInputSlot> OnTechniqueStarted;
        public event EventHandler<TechniqueInputSlot> OnTechniqueFinished;

        public TechniqueExecutionState State => _state;
        public IMasteryService Mastery => _mastery;

        // ── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            // PlayerController
            if (playerController == null) TryGetComponent(out playerController);

            // PlayerInventory: GetComponent → FindAnyObjectByType → log error
            if (playerInventory == null) TryGetComponent(out playerInventory);
            if (playerInventory == null) playerInventory = FindAnyObjectByType<PlayerInventory>();
            if (playerInventory == null && !_loggedInventoryError)
            {
                _loggedInventoryError = true;
                Debug.LogError(
                    "[TechniqueController] Không tìm thấy PlayerInventory — " +
                    "ResolveEquippedDiscipline sẽ luôn trả None. " +
                    "Gán PlayerInventory vào field hoặc đảm bảo nó có trên cùng GameObject.", this);
            }

            // DodgeController — cùng GameObject (điều phối trạng thái)
            TryGetComponent(out _dodgeController);

            _slotCooldown[TechniqueInputSlot.Core]      = new CooldownSystem();
            _slotCooldown[TechniqueInputSlot.Traverse]  = new CooldownSystem();
            _slotCooldown[TechniqueInputSlot.Control]   = new CooldownSystem();
            _slotCooldown[TechniqueInputSlot.Signature] = new CooldownSystem();
            if (_mastery == null) _mastery = new PlayerMasteryService();
        }

        private async void Start()
        {
            try { await _mastery.LoadAsync(); }
            catch { /* không block gameplay nếu save lỗi */ }
        }

        private void OnEnable() => SubscribeMasteryEvents();

        private void OnMasteryChanged(object sender, TechniqueMasteryChangedArgs args)
        {
            _masteryDirty = true;
            _dirtySince = UnityEngine.Time.unscaledTime;
            _nextSaveAttemptAt = _dirtySince + SaveDelaySeconds;
        }

        private void Update()
        {
            // Dirty-save: flush sau 2s không có thay đổi mới
            if (_masteryDirty && !_saveInFlight && UnityEngine.Time.unscaledTime >= _nextSaveAttemptAt)
            {
                _ = FlushSaveAsync();
            }

            switch (_state)
            {
                case TechniqueExecutionState.WindUp:    TickWindUp();    break;
                case TechniqueExecutionState.Active:    TickActive();    break;
                case TechniqueExecutionState.Recovery:  TickRecovery();  break;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) _ = FlushSaveAsync();
        }

        private void OnDisable()
        {
            _ = FlushSaveAsync();
            UnsubscribeMasteryEvents();
        }

        private async void OnDestroy()
        {
            while (_saveInFlight) await System.Threading.Tasks.Task.Yield();
            await FlushSaveAsync();
            (_mastery as IDisposable)?.Dispose();
        }

        public void AssignMasteryService(IMasteryService service)
        {
            UnsubscribeMasteryEvents();
            _mastery = service;
            if (isActiveAndEnabled) SubscribeMasteryEvents();
        }

        private void SubscribeMasteryEvents()
        {
            if (_mastery == null || _masteryEventsSubscribed) return;
            _mastery.TechniqueMasteryChanged += OnMasteryChanged;
            _masteryEventsSubscribed = true;
        }

        private void UnsubscribeMasteryEvents()
        {
            if (_mastery == null || !_masteryEventsSubscribed) return;
            _mastery.TechniqueMasteryChanged -= OnMasteryChanged;
            _masteryEventsSubscribed = false;
        }

        // ── Public API ─────────────────────────────────────────────

        /// <summary>UI bấm nút Technique. Trả true nếu bắt đầu WindUp.</summary>
        public bool TryUseTechnique(TechniqueInputSlot slot)
        {
            if (_state != TechniqueExecutionState.Idle)
            {
                OnValidationFailed?.Invoke(this, TechniqueValidationError.CombatBusy);
                return false;
            }

            // Từ chối khi đang Dodge
            if (_dodgeController != null && _dodgeController.IsDodging)
            {
                OnValidationFailed?.Invoke(this, TechniqueValidationError.CombatBusy);
                return false;
            }

            WeaponDiscipline equipped = ResolveEquippedDiscipline();
            int mastery = _mastery != null ? _mastery.GetMastery(equipped) : 0;
            WeaponTechniqueDefinition def = FindTechnique(equipped, slot);

            // Log one-time nếu targetLayers rỗng
            if (def != null && def.TargetLayers.value == 0 && !_loggedTargetLayersError)
            {
                _loggedTargetLayersError = true;
                Debug.LogError(
                    $"[TechniqueController] Technique '{def.Id}' có targetLayers = Nothing. " +
                    "Technique sẽ không trúng gì — gán layer Enemy trong Inspector asset.", this);
            }

            bool cooldownReady = _slotCooldown.TryGetValue(slot, out CooldownSystem cd) && cd.IsReady;
            TechniqueValidationResult validation = TechniqueExecutor.Validate(def, equipped, mastery, cooldownReady);
            if (!validation.IsValid)
            {
                OnValidationFailed?.Invoke(this, validation.Error);
                if (logHits) Debug.Log($"[TechniqueController] {slot} thất bại: {validation.Message}");
                return false;
            }

            _lastKnownDiscipline = equipped;
            _lastKnownMastery = mastery;
            BeginExecution(def, slot);
            return true;
        }

        public bool IsSlotReady(TechniqueInputSlot slot, out TechniqueValidationError reason)
        {
            reason = TechniqueValidationError.None;
            if (_state != TechniqueExecutionState.Idle)          { reason = TechniqueValidationError.CombatBusy;        return false; }
            if (_dodgeController != null && _dodgeController.IsDodging) { reason = TechniqueValidationError.CombatBusy; return false; }
            WeaponDiscipline equipped = ResolveEquippedDiscipline();
            if (equipped == WeaponDiscipline.None)                { reason = TechniqueValidationError.NoWeaponEquipped;  return false; }
            WeaponTechniqueDefinition def = FindTechnique(equipped, slot);
            if (def == null)                                      { reason = TechniqueValidationError.DefinitionMissing; return false; }
            int mastery = _mastery != null ? _mastery.GetMastery(equipped) : 0;
            if (mastery < def.MasteryRequired)                    { reason = TechniqueValidationError.LockedMastery;     return false; }
            if (_slotCooldown.TryGetValue(slot, out CooldownSystem cd) && !cd.IsReady)
                                                                  { reason = TechniqueValidationError.Cooldown;          return false; }
            return true;
        }

        public float GetCooldownRemaining(TechniqueInputSlot slot)
            => _slotCooldown.TryGetValue(slot, out CooldownSystem cd) ? cd.Remaining : 0f;

        public float GetCooldownTotal(TechniqueInputSlot slot)
        {
            WeaponDiscipline equipped = ResolveEquippedDiscipline();
            WeaponTechniqueDefinition def = FindTechnique(equipped, slot);
            return def != null ? def.CooldownSeconds : 0f;
        }

        // ── State machine ──────────────────────────────────────────

        private void BeginExecution(WeaponTechniqueDefinition def, TechniqueInputSlot slot)
        {
            _activeDefinition = def;
            _state = TechniqueExecutionState.WindUp;
            _stateEnterTime = UnityEngine.Time.time;
            _activeActivationId = _hitProtector.BeginActivation();
            _windowsProcessedThisActivation.Clear();
            _movementApplied.Clear();
            _slotCooldown[slot]?.StartCooldown(def.CooldownSeconds);

            OnTechniqueStarted?.Invoke(this, slot);
            FireAnimatorTrigger(slot);
            if (logHits) Debug.Log($"[TechniqueController] Bắt đầu {def.Id} — WindUp {def.WindUpSeconds:F2}s");
        }

        private void TickWindUp()
        {
            if (_activeDefinition == null) { EndIdle(); return; }
            float elapsed = UnityEngine.Time.time - _stateEnterTime;
            if (elapsed >= UnityEngine.Mathf.Max(0f, _activeDefinition.WindUpSeconds))
            {
                _state = TechniqueExecutionState.Active;
                _stateEnterTime = UnityEngine.Time.time;
                if (logHits) Debug.Log("[TechniqueController] Kích hoạt Active window");
            }
        }

        private void TickActive()
        {
            if (_activeDefinition == null) { EndIdle(); return; }
            float elapsedInActive = UnityEngine.Time.time - _stateEnterTime;
            float totalActiveDuration = ComputeTotalActiveDuration(_activeDefinition);

            for (int w = 0; w < _activeDefinition.ActiveWindows.Count; w++)
            {
                TechniqueHitWindow window = _activeDefinition.ActiveWindows[w];
                if (elapsedInActive >= window.startTimeOffset &&
                    elapsedInActive < window.startTimeOffset + window.duration + 0.0001f)
                {
                    ProcessWindowDamageOnce(window, w);
                }
            }

            if (elapsedInActive >= totalActiveDuration)
            {
                _state = TechniqueExecutionState.Recovery;
                _stateEnterTime = UnityEngine.Time.time;
                if (logHits) Debug.Log($"[TechniqueController] Sang Recovery {_activeDefinition.RecoverySeconds:F2}s");
            }
        }

        private void FixedUpdate()
        {
            if (_state != TechniqueExecutionState.Active || _activeDefinition == null) return;

            float elapsedInActive = UnityEngine.Time.time - _stateEnterTime;
            for (int w = 0; w < _activeDefinition.ActiveWindows.Count; w++)
            {
                TechniqueHitWindow window = _activeDefinition.ActiveWindows[w];
                if (elapsedInActive >= window.startTimeOffset &&
                    elapsedInActive < window.startTimeOffset + window.duration + 0.0001f)
                {
                    ApplyWindowMovementOnce(window, w);
                }
            }
        }

        private readonly HashSet<int> _windowsProcessedThisActivation = new HashSet<int>();

        private void ProcessWindowDamageOnce(TechniqueHitWindow window, int windowIdx)
        {
            if (_activeDefinition == null) return;
            if (_windowsProcessedThisActivation.Contains(windowIdx)) return;
            _windowsProcessedThisActivation.Add(windowIdx);

            Vector2 center = ResolveHitboxCenter(_activeDefinition);
            IReadOnlyList<IDamageable> targets = _hitDetection.FindTargets(center, _activeDefinition.HitboxRadius, _activeDefinition.TargetLayers);
            ICombatStats attacker = playerController != null ? playerController.Stats : null;
            if (attacker == null) return;

            for (int t = 0; t < targets.Count; t++)
            {
                IDamageable target = targets[t];
                if (target == null || target.IsDead) continue;

                int targetInstanceId = (target as UnityEngine.Object) != null
                    ? (target as UnityEngine.Object).GetInstanceID()
                    : target.GetHashCode();

                if (!_hitProtector.TryMarkHit(windowIdx, targetInstanceId))
                {
                    if (logHits) Debug.Log($"[TechniqueController] Window {windowIdx} duplicate hit bị bỏ qua.");
                    continue;
                }

                double defense  = AttackExecutor.ResolveDefense(target);
                double critRoll = UnityEngine.Random.value;
                DamageInfo dmg  = CalculateSingleWindow(attacker, defense, _activeDefinition, window, critRoll, gameObject.name);
                // ApplyHit = TakeDamage + phát GameEvents.OnHitResolved (túi đồ dùng để chặn uống thuốc khi đang đánh).
                AttackExecutor.ApplyHit(target, dmg);

                AwardMasteryCredit(targetInstanceId, isTechniqueHit: true);

                if (logHits) Debug.Log(
                    $"[TechniqueController] {_activeDefinition.Id} w{windowIdx}: {dmg.Amount:0.#} (crit={dmg.IsCritical}) → {target}");
            }
        }

        private readonly HashSet<int> _movementApplied = new HashSet<int>();

        private void ApplyWindowMovementOnce(TechniqueHitWindow window, int windowIdx)
        {
            if (_movementApplied.Contains(windowIdx)) return;
            _movementApplied.Add(windowIdx);
            if (playerController == null || window.movementPolicy == MovementPolicy.None) return;

            float distance = window.moveDistance;
            if (distance <= 0f)
            {
                switch (window.movementPolicy)
                {
                    case MovementPolicy.Step:   distance = 0.8f; break;
                    case MovementPolicy.Dash:   distance = 1.8f; break;
                    case MovementPolicy.Commit: distance = 2.8f; break;
                }
            }

            // Dùng Rigidbody2D.MovePosition thay vì ghi thẳng transform.position
            // để tôn trọng collision và không xuyên tường.
            var rb = playerController.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                int facing = playerController.Facing;
                float safeDistance = distance;

                var hits = new RaycastHit2D[4];
                var filter = new ContactFilter2D { useTriggers = false };
                filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
                Vector2 dir = new Vector2(facing, 0f);
                int hitCount = rb.Cast(dir, filter, hits, distance);
                if (hitCount > 0 && hits[0].collider != null)
                    safeDistance = UnityEngine.Mathf.Max(0f, hits[0].distance - 0.05f);

                rb.MovePosition(rb.position + dir * safeDistance);
            }
            else
            {
                // Fallback nếu không có Rigidbody2D (EditMode test)
                int facing = playerController.Facing;
                Vector3 pos = playerController.transform.position;
                pos.x += facing * distance;
                playerController.transform.position = pos;
            }
        }

        private void TickRecovery()
        {
            if (_activeDefinition == null) { EndIdle(); return; }
            float elapsed = UnityEngine.Time.time - _stateEnterTime;
            if (elapsed >= UnityEngine.Mathf.Max(0f, _activeDefinition.RecoverySeconds))
            {
                TechniqueInputSlot slot = _activeDefinition.InputSlot;
                EndIdle();
                OnTechniqueFinished?.Invoke(this, slot);
                // KHÔNG flush save sau mỗi Technique — dirty flag + 2s delay (Update)
            }
        }

        private void EndIdle()
        {
            _activeDefinition = null;
            _state = TechniqueExecutionState.Idle;
            _windowsProcessedThisActivation.Clear();
            _movementApplied.Clear();
        }

        // ── Helpers ────────────────────────────────────────────────

        private DamageInfo CalculateSingleWindow(ICombatStats attacker, double defense,
            WeaponTechniqueDefinition def, TechniqueHitWindow window, double critRoll, string sourceId)
        {
            double critChance = def.CriticalChanceOverride >= 0.0 ? def.CriticalChanceOverride : (attacker != null ? attacker.CriticalChance : 0.0);
            if (critChance < 0.0) critChance = 0.0;
            if (critChance > 1.0) critChance = 1.0;
            double critDmg = def.CriticalDamageOverride >= 0.0 ? def.CriticalDamageOverride : (attacker != null ? attacker.CriticalDamage : 1.5);
            bool isCrit = critRoll < critChance;
            double final = DamageCalculator.Calculate(
                attacker != null ? attacker.Attack : 0.0, defense, window.hitMultiplier, isCrit, critDmg, Game.Core.Combat.CombatBalance.DefenseWeight);
            return new DamageInfo(final)
            {
                IsCritical        = isCrit,
                CriticalMultiplier = critDmg,
                SourceId          = sourceId,
                Source            = attacker
            };
        }

        private void AwardMasteryCredit(int enemyInstanceId, bool isTechniqueHit)
        {
            if (_mastery == null || _lastKnownDiscipline == WeaponDiscipline.None) return;
            int requested = TechniqueExecutor.GetCreditPerConfirmedHit(isTechniqueHit);
            int actual    = _creditTracker.TryConsume(enemyInstanceId, requested);
            if (actual > 0) _ = _mastery.AddMastery(_lastKnownDiscipline, actual);
        }

        private WeaponDiscipline ResolveEquippedDiscipline()
        {
            if (playerInventory == null) return WeaponDiscipline.None;
            Equipment equip  = playerInventory.Equipment;
            ItemDatabase db  = itemDatabase != null ? itemDatabase : playerInventory.Database;
            if (equip == null || db == null) return WeaponDiscipline.None;

            string instId = equip.GetInstanceId(EquipmentSlot.Weapon);
            if (string.IsNullOrEmpty(instId)) return WeaponDiscipline.None;
            ItemInstance inst = playerInventory.Inventory.FindInstance(instId);
            if (inst == null) return WeaponDiscipline.None;
            ItemDefinition def = db.Find(inst.itemId);
            if (def == null || def.Type != ItemType.Weapon) return WeaponDiscipline.None;
            // Mốc 1: chỉ có Blade. WeaponType → Discipline mapping sẽ mở rộng ở Mốc 2.
            return WeaponDiscipline.Blade;
        }

        private WeaponTechniqueDefinition FindTechnique(WeaponDiscipline discipline, TechniqueInputSlot slot)
            => techniqueDatabase != null ? techniqueDatabase.FindBySlot(discipline, slot) : null;

        private static float ComputeTotalActiveDuration(WeaponTechniqueDefinition def)
        {
            if (def == null || def.ActiveWindows == null || def.ActiveWindows.Count == 0) return 0f;
            float maxEnd = 0f;
            for (int i = 0; i < def.ActiveWindows.Count; i++)
            {
                TechniqueHitWindow w = def.ActiveWindows[i];
                float end = w.startTimeOffset + w.duration;
                if (end > maxEnd) maxEnd = end;
            }
            return maxEnd;
        }

        private Vector2 ResolveHitboxCenter(WeaponTechniqueDefinition def)
        {
            int facing     = playerController != null ? playerController.Facing : 1;
            Vector3 pos    = transform.position;
            Vector2 offset = def != null ? def.HitboxOffset : Vector2.zero;
            return new Vector2(pos.x + offset.x * facing, pos.y + offset.y);
        }

        private void FireAnimatorTrigger(TechniqueInputSlot slot)
        {
            if (animator == null) return;
            string trigger = slot switch
            {
                TechniqueInputSlot.Core     => triggerCoreTechnique,
                TechniqueInputSlot.Traverse => triggerTraverseTechnique,
                _                           => null
            };
            if (!string.IsNullOrEmpty(trigger)) animator.SetTrigger(trigger);
        }

        private async System.Threading.Tasks.Task FlushSaveAsync()
        {
            if (_mastery == null || _saveInFlight) return;

            _saveInFlight = true;
            try
            {
                bool saved = await _mastery.SaveAsync();
                if (saved) _masteryDirty = false;
                else _nextSaveAttemptAt = UnityEngine.Time.unscaledTime + 5f;
            }
            catch { _nextSaveAttemptAt = UnityEngine.Time.unscaledTime + 5f; }
            finally { _saveInFlight = false; }
        }
    }
}
