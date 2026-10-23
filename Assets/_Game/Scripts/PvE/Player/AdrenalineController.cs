using System;
using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Presentation;
using Game.Core.Stats;
using Game.PvE.Camera;
using Game.PvE.Dodge;
using UnityEngine;

namespace Game.PvE.Player
{
    /// <summary>
    /// Quản lý Thanh Nộ Khí (Adrenaline Gauge) và Tuyệt Kỹ "Vũ Điệu Cuồng Nộ" (Tidal Blade Tempest):
    /// - Tích lũy nộ khi đánh trúng quái (+4) hoặc Né Hoàn Hảo (+30).
    /// - Khi đầy 100 nộ, có thể kích hoạt Tuyệt Kỹ:
    ///   + Miễn nhiễm sát thương trong 0.8 giây.
    ///   + Quét bão kiếm 3.2m quanh người, gây 350% sát thương diện rộng.
    ///   + Hồi 15% Max HP.
    ///   + Rung giật camera và làm chậm thời gian cực đã!
    /// </summary>
    public class AdrenalineController : MonoBehaviour
    {
        public static AdrenalineController Instance { get; private set; }

        public const float MaxAdrenaline = 100f;

        [Header("Tích lũy Nộ")]
        [SerializeField] private float adrenalinePerHit = 4f;
        [SerializeField] private float adrenalineOnPerfectDodge = 30f;
        [SerializeField] private float adrenalineOnFinisher = 8f;

        [Header("Tuyệt Kỹ Cấu hình")]
        [SerializeField] private float ultimateRadius = 3.2f;
        [SerializeField] private double ultimateDamageMultiplier = 3.5;
        [SerializeField] private float ultimateHealPercent = 0.15f;
        [SerializeField] private float invulnerabilityDuration = 0.8f;
        [SerializeField] private LayerMask targetLayers;

        private float _currentAdrenaline;
        private PlayerController _playerController;
        private HealthComponent _healthComponent;
        private PerfectDodgeDetector _dodgeDetector;
        private PlayerAttack _playerAttack;
        private readonly HitDetection _hitDetection = new HitDetection();

        public float CurrentAdrenaline => _currentAdrenaline;
        public float AdrenalineRatio => Mathf.Clamp01(_currentAdrenaline / MaxAdrenaline);
        public bool IsReady => _currentAdrenaline >= MaxAdrenaline;

        public event Action<float> AdrenalineChanged;
        public event Action UltimateReady;
        public event Action UltimateExecuted;

        private void Awake()
        {
            Instance = this;
            TryGetComponent(out _playerController);
            TryGetComponent(out _healthComponent);
            TryGetComponent(out _dodgeDetector);
            TryGetComponent(out _playerAttack);

            if (targetLayers == 0)
            {
                targetLayers = LayerMask.GetMask("Enemy", "Default");
            }
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.OnHitResolved += HandleHitResolved;
            if (_dodgeDetector != null)
            {
                _dodgeDetector.PerfectDodgeTriggered += HandlePerfectDodge;
            }
            if (_playerAttack != null)
            {
                _playerAttack.ComboStepPerformed += HandleComboStep;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnHitResolved -= HandleHitResolved;
            if (_dodgeDetector != null)
            {
                _dodgeDetector.PerfectDodgeTriggered -= HandlePerfectDodge;
            }
            if (_playerAttack != null)
            {
                _playerAttack.ComboStepPerformed -= HandleComboStep;
            }
        }

        public void AddAdrenaline(float amount)
        {
            if (amount <= 0f) return;

            float prev = _currentAdrenaline;
            _currentAdrenaline = Mathf.Clamp(_currentAdrenaline + amount, 0f, MaxAdrenaline);

            AdrenalineChanged?.Invoke(_currentAdrenaline);

            if (prev < MaxAdrenaline && _currentAdrenaline >= MaxAdrenaline)
            {
                UltimateReady?.Invoke();
            }
        }

        private void HandleHitResolved(object sender, HitResolvedArgs args)
        {
            if (args == null || args.Damage == null) return;
            AddAdrenaline(adrenalinePerHit);
        }

        private void HandleComboStep(int step)
        {
            if (step == 2) // Finisher
            {
                AddAdrenaline(adrenalineOnFinisher);
            }
        }

        private void HandlePerfectDodge()
        {
            AddAdrenaline(adrenalineOnPerfectDodge);
        }

        public bool TryExecuteUltimate()
        {
            if (!IsReady) return false;

            CharacterStats stats = _playerController != null ? _playerController.Stats : null;
            if (stats == null) return false;

            _currentAdrenaline = 0f;
            AdrenalineChanged?.Invoke(0f);

            // 1. Bất tử ngắn
            if (_healthComponent != null)
            {
                _healthComponent.IsInvulnerable = true;
                CancelInvoke(nameof(EndInvulnerability));
                Invoke(nameof(EndInvulnerability), invulnerabilityDuration);

                // Hồi 15% Max HP
                double healAmount = _healthComponent.MaxHealth * ultimateHealPercent;
                _healthComponent.Heal(healAmount);
            }

            // 2. Hiệu ứng làm chậm + giật rung camera
            PlaySpeedState.Shared.RequestHitStop(0.12f, 0.05f);
            CameraFollow.RequestShake(0.35f, 0.35f);

            // 3. Quét sát thương diện rộng
            Vector2 origin = transform.position;
            var targets = _hitDetection.FindTargets(origin, ultimateRadius, targetLayers);
            for (int i = 0; i < targets.Count; i++)
            {
                IDamageable target = targets[i];
                if (target == null || target.IsDead) continue;

                double defense = AttackExecutor.ResolveDefense(target);
                // Sát thương 350%
                DamageInfo dmg = new DamageInfo(stats.Attack * ultimateDamageMultiplier)
                {
                    IsCritical = true,
                    CriticalMultiplier = 1.5,
                    SourceId = gameObject.name,
                    Source = this
                };

                AttackExecutor.ApplyHit(target, dmg);
            }

            UltimateExecuted?.Invoke();
            return true;
        }

        private void EndInvulnerability()
        {
            if (_healthComponent != null)
            {
                _healthComponent.IsInvulnerable = false;
            }
        }
    }
}
