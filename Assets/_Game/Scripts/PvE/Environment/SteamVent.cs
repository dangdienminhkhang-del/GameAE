using System.Collections.Generic;
using Game.Core.Combat;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Lỗ xả hơi: chu kỳ nghỉ -> báo trước (nhấp nháy) -> phun hơi gây sát thương một lần mỗi đợt cho người chơi đứng trong vùng.
    /// Người chơi luôn có thời gian báo trước để tránh. Sát thương = phần trăm máu tối đa (có mức tối thiểu).
    /// SourceId để trống để game phân loại cái chết là do bẫy (DeathReason.Trap).
    /// Vùng an toàn (Trụ Đèn) đã được HealthComponent tự bỏ qua.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SteamVent : MonoBehaviour
    {
        private enum Phase { Idle, Warning, Burst }

        [SerializeField] private SpriteRenderer ventRenderer;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private SpriteRenderer steamRenderer;
        [SerializeField] private Sprite[] steamFrames = new Sprite[0];

        [SerializeField, Min(0.5f)] private float idleSeconds = 2.6f;
        [SerializeField, Min(0.3f)] private float warningSeconds = 0.9f;
        [SerializeField, Min(0.2f)] private float burstSeconds = 0.7f;
        [SerializeField] private float phaseOffsetSeconds = 0f;
        [SerializeField, Range(0.01f, 0.5f)] private float damageFractionOfMaxHealth = 0.12f;
        [SerializeField, Min(1f)] private float minimumDamage = 4f;

        private readonly HashSet<HealthComponent> _inside = new HashSet<HealthComponent>();
        private readonly HashSet<HealthComponent> _hitThisBurst = new HashSet<HealthComponent>();
        private readonly List<HealthComponent> _scratch = new List<HealthComponent>();
        private Phase _phase = Phase.Idle;
        private float _cycle;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnEnable()
        {
            _cycle = idleSeconds + warningSeconds + burstSeconds;
            _inside.Clear();
            _hitThisBurst.Clear();
            ApplyVisual(Phase.Idle, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<HealthComponent>();
            if (health != null) _inside.Add(health);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var health = other.GetComponentInParent<HealthComponent>();
            if (health != null) _inside.Remove(health);
        }

        private void Update()
        {
            if (_cycle <= 0f) return;
            float t = Mathf.Repeat(Time.time + phaseOffsetSeconds, _cycle);
            Phase next = t < idleSeconds ? Phase.Idle : t < idleSeconds + warningSeconds ? Phase.Warning : Phase.Burst;
            float phaseTime = next == Phase.Idle ? t : next == Phase.Warning ? t - idleSeconds : t - idleSeconds - warningSeconds;

            if (next == Phase.Burst && _phase != Phase.Burst) _hitThisBurst.Clear();
            _phase = next;
            ApplyVisual(next, phaseTime);
            if (next == Phase.Burst) DamageInside();
        }

        private void DamageInside()
        {
            if (_inside.Count == 0) return;
            _scratch.Clear();
            _scratch.AddRange(_inside);
            for (int i = 0; i < _scratch.Count; i++)
            {
                var health = _scratch[i];
                if (health == null || health.IsDead || _hitThisBurst.Contains(health)) continue;
                _hitThisBurst.Add(health);
                double amount = System.Math.Max(minimumDamage, health.MaxHealth * damageFractionOfMaxHealth);
                health.TakeDamage(new DamageInfo(amount) { SourceId = null, Source = this });
            }
        }

        private void ApplyVisual(Phase phase, float phaseTime)
        {
            if (ventRenderer != null)
            {
                bool lit = phase == Phase.Burst || (phase == Phase.Warning && Mathf.FloorToInt(phaseTime * 10f) % 2 == 0);
                var sprite = lit ? activeSprite : idleSprite;
                if (sprite != null) ventRenderer.sprite = sprite;
            }
            if (steamRenderer != null)
            {
                bool show = phase == Phase.Burst && steamFrames != null && steamFrames.Length > 0;
                steamRenderer.enabled = show;
                if (show)
                {
                    int frame = Mathf.Min(steamFrames.Length - 1, Mathf.FloorToInt(phaseTime / burstSeconds * steamFrames.Length));
                    steamRenderer.sprite = steamFrames[frame];
                }
            }
        }
    }
}
