using Game.Core.Events;
using Game.Core.Presentation;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Nháy người chơi khi bị trúng đòn (docs/05 Phase 9.5, ADR-029). Chỉ NGHE OnHealthChanged đã lọc theo máu của người chơi
    /// (Delta &lt; 0), không sửa PlayerController hay HealthComponent. Đặt trên object Player.
    /// Nháy = lớp phủ trắng riêng (child "HitFlash" do SpriteTint/HitFlashOverlay tự dựng), không nhân màu vào sprite người chơi.
    /// Không scale punch cho người chơi (collider/vật lý) — chỉ có lớp phủ nháy.
    /// </summary>
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [Tooltip("Để trống = HealthComponent trên cùng object.")]
        [SerializeField] private HealthComponent playerHealth;
        [Tooltip("Để trống = SpriteRenderer đầu tiên trong object hoặc con.")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Để trống = dùng CombatFeelConfig đang active.")]
        [SerializeField] private CombatFeelConfig config;

        private SpriteTint _tint;

        private void Awake()
        {
            if (playerHealth == null) TryGetComponent(out playerHealth);
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) _tint = new SpriteTint(spriteRenderer, null, config);
        }

        private void OnEnable()
        {
            GameEvents.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= HandleHealthChanged;
        }

        private void Update()
        {
            if (_tint != null) _tint.Tick(UnityEngine.Time.unscaledDeltaTime);
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs args)
        {
            if (_tint == null || args == null || args.Delta >= 0.0) return;
            if (playerHealth == null || !ReferenceEquals(sender, playerHealth)) return;
            _tint.Flash();
        }
    }
}
