using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Guidance;
using Game.Core.Items;
using Game.Core.Skills;
using Game.PvE.Boss;
using Game.PvE.Dodge;
using Game.PvE.Enemy;
using Game.PvE.Items;
using Game.PvE.Player;
using Game.PvE.SafeZone;
using Game.PvE.Skills;
using Game.UI.Boss;
using UnityEngine;

namespace Game.UI.Guidance
{
    /// <summary>
    /// Cảm biến của hướng dẫn (Phase 9.9): chuyển sự kiện THẬT của game thành tín hiệu chung (<see cref="TutorialTrigger"/> và
    /// <see cref="TutorialComplete"/>) cho <see cref="TutorialTriggerRouter"/>. Chỉ ĐỌC; không sửa gì của PvE.
    /// - Phần lớn đi theo sự kiện (không polling). Phần cần đo (vị trí, Safe Zone, mở khóa kỹ thuật) chạy theo nhịp
    ///   <see cref="GuidanceConfig.SensorHz"/> (tối đa 4 lần/giây) bằng thời gian THẬT, tham chiếu người chơi được cache một lần.
    /// - Không Find mỗi frame: chưa thấy người chơi thì thử lại mỗi <see cref="PlayerScanIntervalSeconds"/> giây.
    /// Luật quyết định (cạnh, trễ, mở khóa...) nằm ở Core/Guidance (thuần, có test); lớp này chỉ nối dây.
    /// </summary>
    public sealed class TutorialSensors : MonoBehaviour
    {
        private const float PlayerScanIntervalSeconds = 1f;

        [Tooltip("Để trống = dùng giá trị mặc định có tên trong GuidanceConfig/code.")]
        [SerializeField] private GuidanceConfig config;

        public event Action<TutorialTrigger> TriggerRaised;
        public event Action<TutorialComplete> CompleteRaised;

        private readonly SafeZoneEdgeDetector _safeZoneEdge = new SafeZoneEdgeDetector();
        private readonly RisingEdgeDetector _techniqueUnlock = new RisingEdgeDetector();
        private readonly FloorUnlockGate _floorGate = new FloorUnlockGate();
        private readonly EquipChangeDetector _equipChange = new EquipChangeDetector();
        private readonly List<BossController> _watchedBosses = new List<BossController>(2);

        private MoveStreakDetector _move;
        private LowHealthGate _lowHealth;

        private Transform _player;
        private PlayerController _playerController;
        private HealthComponent _health;
        private DodgeController _dodge;
        private TechniqueController _technique;
        private PlayerInventory _inventory;
        private Equipment _equipment;

        private Vector2 _lastPosition;
        private bool _hasLastPosition;
        private float _lastTickAt;
        private float _nextTickAt;
        private float _nextPlayerScanAt;
        private bool _playerDead;
        private bool _hitReported;

        public bool HasPlayer => _player != null;
        public bool IsPlayerDead => _playerDead || (_health != null && _health.IsDead);

        public void SetConfig(GuidanceConfig value)
        {
            config = value;
            BuildDetectors();
        }

        private float TickInterval => config != null ? config.SensorIntervalSeconds : 1f / GuidanceConfig.MaxSensorHz;

        private void Awake()
        {
            BuildDetectors();
        }

        private void BuildDetectors()
        {
            _move = new MoveStreakDetector(config != null ? config.MoveConfirmTicks : 2, config != null ? config.MoveMinSpeed : 0.3f);
            _lowHealth = new LowHealthGate(config != null ? config.LowHealthRatio : 0.35f,
                config != null ? config.LowHealthRearmRatio : 0.5f);
        }

        private void OnEnable()
        {
            GameEvents.OnHitResolved += HandleHitResolved;
            GameEvents.OnHealthChanged += HandleHealthChanged;
            GameEvents.OnItemAcquiredWithPayload += HandleItemAcquired;
            GameEvents.OnBossDefeated += HandleBossDefeated;
            GameEvents.OnPlayerDied += HandlePlayerDied;
            EnemyController.WindupStarted += HandleWindupStarted;
            _nextTickAt = 0f;
            _nextPlayerScanAt = 0f;
        }

        private void OnDisable()
        {
            GameEvents.OnHitResolved -= HandleHitResolved;
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            GameEvents.OnItemAcquiredWithPayload -= HandleItemAcquired;
            GameEvents.OnBossDefeated -= HandleBossDefeated;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            EnemyController.WindupStarted -= HandleWindupStarted;
            ReleasePlayer();
            ReleaseBosses();
        }

        /// <summary>"Xem lại hướng dẫn": nạp lại các tín hiệu "lần đầu" để gợi ý có thể hiện lại.</summary>
        public void ResetOneShots()
        {
            _move.Reset();
            _lowHealth.Reset();
            _techniqueUnlock.Reset();
            _floorGate.Reset();
            _hitReported = false;
            _safeZoneEdge.Reset();
            _hasLastPosition = false;
        }

        // ── Nhịp đo ──────────────────────────────────────────────────────────

        private void Update()
        {
            float now = UnityEngine.Time.unscaledTime;
            if (now < _nextTickAt) return;
            _nextTickAt = now + TickInterval;

            if (_player == null && !TryAcquirePlayer(now)) return;

            float delta = _lastTickAt > 0f ? now - _lastTickAt : TickInterval;
            _lastTickAt = now;

            SampleMovement(delta);
            SampleSafeZone();
            SampleTechniqueUnlock();

            // floor_select: boss đã hạ + đã xác nhận + bảng báo boss đã đóng.
            if (_floorGate.Update(BossDefeatReportPanel.CurrentState == BossDefeatReportPanelState.Hidden))
                Raise(TutorialTrigger.FloorUnlocked);
        }

        private bool TryAcquirePlayer(float now)
        {
            if (now < _nextPlayerScanAt) return false;
            _nextPlayerScanAt = now + PlayerScanIntervalSeconds;

            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found == null) return false;

            _player = found.transform;
            _playerController = found.GetComponentInParent<PlayerController>();
            _health = found.GetComponentInParent<HealthComponent>();
            _dodge = found.GetComponentInParent<DodgeController>();
            _technique = found.GetComponentInParent<TechniqueController>();
            _inventory = found.GetComponentInParent<PlayerInventory>();
            if (_inventory == null) _inventory = FindAnyObjectByType<PlayerInventory>();

            if (_dodge != null) _dodge.OnDodgeStarted += HandleDodgeStarted;
            if (_technique != null) _technique.OnTechniqueStarted += HandleTechniqueStarted;
            if (_inventory != null)
            {
                _inventory.ConsumableUsed += HandleConsumableUsed;
                _equipment = _inventory.Equipment;
                if (_equipment != null)
                {
                    _equipChange.Update(_equipment.GetInstanceId(EquipmentSlot.Weapon), _equipment.GetInstanceId(EquipmentSlot.Armor));
                    _equipment.Changed += HandleEquipmentChanged;
                }
            }

            _hasLastPosition = false;
            _playerDead = false;
            Raise(TutorialTrigger.PlayerReady);
            return true;
        }

        private void ReleasePlayer()
        {
            if (_dodge != null) _dodge.OnDodgeStarted -= HandleDodgeStarted;
            if (_technique != null) _technique.OnTechniqueStarted -= HandleTechniqueStarted;
            if (_inventory != null) _inventory.ConsumableUsed -= HandleConsumableUsed;
            if (_equipment != null) _equipment.Changed -= HandleEquipmentChanged;

            _player = null;
            _playerController = null;
            _health = null;
            _dodge = null;
            _technique = null;
            _inventory = null;
            _equipment = null;
        }

        private void SampleMovement(float deltaSeconds)
        {
            Vector2 position = _player.position;
            if (_hasLastPosition && _move.Update(Vector2.Distance(position, _lastPosition), deltaSeconds))
                RaiseComplete(TutorialComplete.Moved);

            _lastPosition = position;
            _hasLastPosition = true;
        }

        private void SampleSafeZone()
        {
            bool inside = SafeZoneProvider.GetOrCreate().IsSafeZoneAt(_player.position);
            SafeZoneEdge edge = _safeZoneEdge.Update(inside);
            if (edge == SafeZoneEdge.Left) Raise(TutorialTrigger.LeftSafeZone);
            else if (edge == SafeZoneEdge.Entered) Raise(TutorialTrigger.EnteredSafeZone);
        }

        private void SampleTechniqueUnlock()
        {
            if (_technique == null) return;

            _technique.IsSlotReady(TechniqueInputSlot.Core, out TechniqueValidationError reason);
            if (!TechniqueUnlockRules.IsConclusive(reason)) return;

            if (_techniqueUnlock.Update(TechniqueUnlockRules.IsUnlocked(reason)))
                Raise(TutorialTrigger.TechniqueUnlocked);
        }

        // ── Sự kiện thật ─────────────────────────────────────────────────────

        private void HandleHitResolved(object sender, HitResolvedArgs args)
        {
            if (_hitReported || args == null || _playerController == null || args.Source == null) return;
            if (!ReferenceEquals(args.Source, _playerController.Stats)) return;

            _hitReported = true;
            RaiseComplete(TutorialComplete.HitLanded);
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs args)
        {
            if (args == null || _health == null || !ReferenceEquals(sender, _health)) return;
            if (_lowHealth.Update(args.Current, args.Max)) Raise(TutorialTrigger.LowHealth);
        }

        private void HandleItemAcquired(object sender, ItemAcquiredArgs args)
        {
            if (args == null) return;

            Raise(TutorialTrigger.ItemAcquired);

            ItemDefinition definition = _inventory != null && _inventory.Database != null ? _inventory.Database.Find(args.ItemId) : null;
            if (definition != null && definition.Slot != EquipmentSlot.None) Raise(TutorialTrigger.EquippableAcquired);
        }

        private void HandleBossDefeated(object sender, BossDefeatedArgs args)
        {
            _floorGate.MarkDefeated();

            // Xác nhận của máy chủ tới SAU (BossController.DefeatReportConfirmed): chỉ khi đó mới đủ điều kiện hint.floor_select.
            if (sender is BossController boss && !_watchedBosses.Contains(boss))
            {
                boss.DefeatReportConfirmed += HandleBossReportConfirmed;
                _watchedBosses.Add(boss);
            }
        }

        private void HandleBossReportConfirmed()
        {
            _floorGate.MarkConfirmed();
        }

        private void ReleaseBosses()
        {
            for (int i = 0; i < _watchedBosses.Count; i++)
            {
                // Event là field C# thuần nên gỡ được kể cả khi boss đã bị hủy.
                if (!ReferenceEquals(_watchedBosses[i], null)) _watchedBosses[i].DefeatReportConfirmed -= HandleBossReportConfirmed;
            }
            _watchedBosses.Clear();
        }

        private void HandlePlayerDied(object sender, Game.Core.Combat.DamageInfo lastHit)
        {
            _playerDead = true;
        }

        private void HandleWindupStarted(EnemyController enemy)
        {
            if (enemy == null || _player == null) return;

            Transform target = enemy.Target;
            if (target == null || (target != _player && !target.IsChildOf(_player))) return;

            Raise(TutorialTrigger.EnemyWindup);
        }

        private void HandleDodgeStarted(object sender, EventArgs args)
        {
            RaiseComplete(TutorialComplete.Dodged);
        }

        private void HandleTechniqueStarted(object sender, TechniqueInputSlot slot)
        {
            RaiseComplete(TutorialComplete.TechniqueUsed);
        }

        private void HandleConsumableUsed()
        {
            RaiseComplete(TutorialComplete.ConsumableUsed);
        }

        private void HandleEquipmentChanged()
        {
            if (_equipment == null) return;
            if (_equipChange.Update(_equipment.GetInstanceId(EquipmentSlot.Weapon), _equipment.GetInstanceId(EquipmentSlot.Armor)))
                RaiseComplete(TutorialComplete.Equipped);
        }

        private void Raise(TutorialTrigger trigger) => TriggerRaised?.Invoke(trigger);
        private void RaiseComplete(TutorialComplete kind) => CompleteRaised?.Invoke(kind);
    }
}
