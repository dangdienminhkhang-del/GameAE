using Game.Core.Combat;
using Game.Core.Events;
using UnityEngine;

namespace Game.PvE.Player
{
    /// <summary>
    /// Chỉ phát animation phản ứng (bị đánh / chết) cho Player — không chứa logic game.
    /// Lắng nghe GameEvents của đúng HealthComponent trên object này.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerAnimationReactions : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string hurtTrigger = "Hurt";
        [SerializeField] private string dieTrigger = "Die";

        private HealthComponent _health;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            GameEvents.OnHealthChanged += HandleHealthChanged;
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs e)
        {
            if (!ReferenceEquals(sender, _health) || animator == null) return;
            if (e.Delta < 0.0 && e.Current > 0.0 && !string.IsNullOrEmpty(hurtTrigger))
                animator.SetTrigger(hurtTrigger);
        }

        private void HandlePlayerDied(object sender, DamageInfo lastHit)
        {
            if (!ReferenceEquals(sender, _health) || animator == null) return;
            if (!string.IsNullOrEmpty(dieTrigger)) animator.SetTrigger(dieTrigger);
        }
    }
}
