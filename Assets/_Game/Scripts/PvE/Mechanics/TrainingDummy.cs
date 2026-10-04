using System;
using Game.Core.Combat;
using UnityEngine;

namespace Game.PvE.Mechanics
{
    /// <summary>
    /// Bia tập (Phase 3): mục tiêu đứng yên để test damage pipeline.
    /// Cố ý KHÔNG raise GameEvents.OnHealthChanged — vì PlayerHUD đang nghe event toàn cục,
    /// nếu bia raise thì thanh HP của player sẽ nhảy theo. Xem báo cáo Known issues.
    /// </summary>
    public class TrainingDummy : MonoBehaviour, IDamageable, ICombatStats
    {
        [Header("Chỉ số")]
        [SerializeField] private double maxHealth = 500.0;
        [SerializeField] private double defense = 5.0;

        [Header("Hồi lại")]
        [SerializeField] private float resetDelaySeconds = 3f;

        [Header("Hiển thị")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hitColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color deadColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private const float FlashSeconds = 0.15f;

        private double _currentHealth;
        private Color _baseColor = Color.white;
        private float _flashTimer;
        private Coroutine _resetRoutine;

        /// <summary>Cho UI/log nghe tại chỗ, không dùng event toàn cục.</summary>
        public event Action<TrainingDummy, DamageInfo> Damaged;

        public double CurrentHealth => _currentHealth;
        public double MaxHealth => maxHealth;
        public bool IsDead => _currentHealth <= 0.0;
        public DamageInfo LastHit { get; private set; }

        // ICombatStats — bia không đánh trả
        public double Attack => 0.0;
        public double Defense => defense;
        public double CriticalChance => 0.0;
        public double CriticalDamage => 1.0;

        private void Awake()
        {
            if (spriteRenderer == null) TryGetComponent(out spriteRenderer);
            if (spriteRenderer != null) _baseColor = spriteRenderer.color;
            _currentHealth = maxHealth;
        }

        private void Update()
        {
            if (spriteRenderer == null || _flashTimer <= 0f) return;

            _flashTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(1f - _flashTimer / FlashSeconds); // 0 = vừa trúng, 1 = trở lại bình thường
            Color target = IsDead ? deadColor : _baseColor;
            spriteRenderer.color = Color.Lerp(hitColor, target, t);
            if (_flashTimer <= 0f) spriteRenderer.color = target;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (damage == null || IsDead) return;

            LastHit = damage;
            _currentHealth = Math.Max(0.0, _currentHealth - damage.Amount);
            _flashTimer = FlashSeconds;
            if (spriteRenderer != null) spriteRenderer.color = hitColor;

            Damaged?.Invoke(this, damage);

            if (IsDead)
            {
                if (spriteRenderer != null) spriteRenderer.color = deadColor;
                if (_resetRoutine != null) StopCoroutine(_resetRoutine);
                _resetRoutine = StartCoroutine(ResetAfterDelay());
            }
        }

        public void Heal(double amount)
        {
            if (amount <= 0.0 || IsDead) return;
            _currentHealth = Math.Min(maxHealth, _currentHealth + amount);
        }

        public void SetMaxHealth(double maxHp, bool resetCurrent = false)
        {
            double ratio = maxHealth > 0.0 ? _currentHealth / maxHealth : 1.0;
            maxHealth = Math.Max(1.0, maxHp);
            _currentHealth = resetCurrent ? maxHealth : maxHealth * ratio;
        }

        /// <summary>Hồi đầy bia (dùng cho nút debug hoặc sau khi chết).</summary>
        public void ResetDummy()
        {
            _currentHealth = maxHealth;
            _flashTimer = 0f;
            if (spriteRenderer != null) spriteRenderer.color = _baseColor;
        }

        private System.Collections.IEnumerator ResetAfterDelay()
        {
            yield return new WaitForSeconds(resetDelaySeconds);
            _resetRoutine = null;
            ResetDummy();
        }
    }
}
