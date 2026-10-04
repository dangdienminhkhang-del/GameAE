using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Stats;
using UnityEngine;

namespace Game.PvE.Player
{
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private bool destroyOnDeath = false;

        private CharacterStats _stats;
        private double _currentHealth;
        private double _lastMaxHealth;

        public double CurrentHealth => _currentHealth;
        public double MaxHealth => _stats != null ? _stats.MaxHealth : 0.0;
        public bool IsDead => _stats != null && _currentHealth <= 0.0;

        private void Start()
        {
            if (playerController == null) TryGetComponent(out playerController);
            if (playerController != null)
            {
                _stats = playerController.Stats;
                _currentHealth = _stats.MaxHealth;
                _lastMaxHealth = _stats.MaxHealth;
                RaiseChanged(0.0);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnStatsChanged += HandleStatsChanged;
            GameEvents.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            GameEvents.OnStatsChanged -= HandleStatsChanged;
            GameEvents.OnLevelUp -= HandleLevelUp;
        }

        /// <summary>
        /// Lên LEVEL → máu hiện tại tăng đúng phần MaxHealth gốc tăng thêm (docs/07 §1 phụ lục).
        /// Dùng max GỐC theo level nên không dính phần cộng của trang bị.
        /// </summary>
        private void HandleLevelUp(object sender, int newLevel)
        {
            double delta = CharacterStats.BaseMaxHealthForLevel(newLevel) - CharacterStats.BaseMaxHealthForLevel(newLevel - 1);
            if (delta > 0.0 && !IsDead) _currentHealth = System.Math.Min(MaxHealth, _currentHealth + delta);
            RaiseChanged(delta);
        }

        /// <summary>
        /// Mặc/tháo trang bị: CHỈ cắt máu hiện tại nếu vượt max — KHÔNG hồi máu
        /// (nếu hồi thì mặc/tháo lặp lại = hồi máu miễn phí).
        /// </summary>
        private void HandleStatsChanged(object sender, System.EventArgs e)
        {
            double newMax = MaxHealth;
            if (newMax > 0.0 && _currentHealth > newMax) _currentHealth = newMax;
            _lastMaxHealth = newMax;
            RaiseChanged(0.0);
        }

        private void RaiseChanged(double delta)
        {
            GameEvents.RaiseHealthChanged(this, new HealthChangedArgs
            {
                Current = _currentHealth,
                Max = MaxHealth,
                Delta = delta
            });
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;

            // Vùng an toàn (Trụ Đèn): kiểm tra theo VỊ TRÍ lúc bị đánh — docs/09 §1.3, docs/11 §2.
            if (SafeZone.SafeZoneProvider.GetOrCreate().IsSafeZoneAt(transform.position)) return;

            double delta = -(double)info.Amount;
            _currentHealth = UnityEngine.Mathf.Max(0f, (float)(_currentHealth - info.Amount));
            GameEvents.RaiseHealthChanged(this, new HealthChangedArgs
            {
                Current = _currentHealth,
                Max = MaxHealth,
                Delta = delta
            });

            if (_currentHealth <= 0.0)
            {
                Die(info);
            }
        }

        public void Heal(double amount)
        {
            if (IsDead || _stats == null) return;
            double before = _currentHealth;
            _currentHealth = UnityEngine.Mathf.Min((float)_stats.MaxHealth, (float)(_currentHealth + amount));
            double delta = _currentHealth - before;
            if (delta > 0.0)
            {
                GameEvents.RaiseHealthChanged(this, new HealthChangedArgs
                {
                    Current = _currentHealth,
                    Max = MaxHealth,
                    Delta = delta
                });
            }
        }

        /// <summary>Khớp đúng ngữ nghĩa IDamageable.SetMaxHealth(..., resetCurrent): false = giữ tỉ lệ HP hiện tại.</summary>
        public void SetMaxHealth(double max, bool resetCurrent = false)
        {
            if (_stats == null) return;
            double oldMax = _stats.MaxHealth;
            double ratio = oldMax > 0.0 ? _currentHealth / oldMax : 1.0;
            _stats.SetMaxHealthOverride(max);
            _currentHealth = resetCurrent ? _stats.MaxHealth : _stats.MaxHealth * ratio;
            GameEvents.RaiseHealthChanged(this, new HealthChangedArgs
            {
                Current = _currentHealth,
                Max = MaxHealth,
                Delta = 0.0
            });
        }

        private void Die(DamageInfo lastHit)
        {
            GameEvents.RaisePlayerDied(this, lastHit);
            if (destroyOnDeath)
            {
                Destroy(gameObject, 0.1f);
            }
        }
    }
}
