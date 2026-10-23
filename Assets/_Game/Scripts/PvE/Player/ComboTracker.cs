using System;
using Game.Core.Combat;
using Game.Core.Events;
using UnityEngine;

namespace Game.PvE.Player
{
    /// <summary>
    /// Quản lý chuỗi Combo của người chơi:
    /// - Tăng combo mỗi khi gây sát thương thành công (OnHitResolved).
    /// - Tự động tụt sau comboDecaySeconds nếu không đánh tiếp.
    /// - Giảm/reset khi người chơi bị trúng đòn.
    /// - Cung cấp chỉ số buff theo Rank (D -> SSS).
    /// </summary>
    public class ComboTracker : MonoBehaviour
    {
        public static ComboTracker Instance { get; private set; }

        [Header("Cấu hình")]
        [SerializeField] private float comboDecaySeconds = 3.5f;
        [SerializeField] private bool penalizeOnPlayerHit = true;

        private int _comboCount;
        private ComboRank _currentRank = ComboRank.None;
        private float _decayTimer;
        private HealthComponent _playerHealth;

        public int ComboCount => _comboCount;
        public ComboRank CurrentRank => _currentRank;
        public float DecayTimerRemaining => _decayTimer;
        public float DecayRatio => comboDecaySeconds > 0f ? Mathf.Clamp01(_decayTimer / comboDecaySeconds) : 0f;
        public double DamageMultiplier => ComboRankMath.GetDamageMultiplier(_currentRank);
        public float MoveSpeedMultiplier => ComboRankMath.GetMoveSpeedBonus(_currentRank);

        public event Action<int, ComboRank> ComboChanged;
        public event Action<ComboRank> RankChanged;
        public event Action ComboReset;

        private void Awake()
        {
            Instance = this;
            TryGetComponent(out _playerHealth);
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.OnHitResolved += HandleHitResolved;
            GameEvents.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnHitResolved -= HandleHitResolved;
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            ResetCombo();
        }

        private void Update()
        {
            if (_comboCount <= 0) return;

            _decayTimer -= Time.deltaTime;
            if (_decayTimer <= 0f)
            {
                ResetCombo();
            }
        }

        public void AddHit()
        {
            _comboCount++;
            _decayTimer = comboDecaySeconds;

            ComboRank newRank = ComboRankMath.GetRank(_comboCount);
            bool rankUp = newRank != _currentRank;
            _currentRank = newRank;

            ComboChanged?.Invoke(_comboCount, _currentRank);
            if (rankUp)
            {
                RankChanged?.Invoke(_currentRank);
            }
        }

        public void ResetCombo()
        {
            if (_comboCount == 0 && _currentRank == ComboRank.None) return;

            _comboCount = 0;
            _currentRank = ComboRank.None;
            _decayTimer = 0f;

            ComboChanged?.Invoke(0, ComboRank.None);
            ComboReset?.Invoke();
        }

        private void HandleHitResolved(object sender, HitResolvedArgs args)
        {
            if (args == null || args.Damage == null) return;
            AddHit();
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs args)
        {
            if (!penalizeOnPlayerHit || args == null || args.Delta >= 0.0) return;
            if (_playerHealth != null && !ReferenceEquals(sender, _playerHealth)) return;

            // Bị trúng đòn: trừ 1/2 combo
            if (_comboCount > 0)
            {
                _comboCount = Mathf.Max(0, _comboCount / 2);
                _currentRank = ComboRankMath.GetRank(_comboCount);
                _decayTimer = Mathf.Min(_decayTimer, comboDecaySeconds * 0.5f);
                ComboChanged?.Invoke(_comboCount, _currentRank);
                if (_comboCount == 0) ComboReset?.Invoke();
            }
        }
    }
}
