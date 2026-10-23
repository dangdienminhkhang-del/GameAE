using Game.Core.Presentation;
using Game.PvE.Boss;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.HUD
{
    /// <summary>
    /// Thanh máu boss trên HUD (docs/05 Phase 9.5): chỉ hiện khi boss đã vào trận (EngagedChanged = true) và chưa chết;
    /// ẩn lúc đầu, khi boss về Idle/chết/rematch. Cập nhật bằng sự kiện HealthChanged, KHÔNG poll trong Update.
    /// Component này nằm trên object LUÔN BẬT; phần hiển thị là <c>visualRoot</c> (con) được bật/tắt — nếu tự tắt chính object này
    /// thì sẽ mất luôn việc nghe sự kiện.
    /// </summary>
    public sealed class BossHealthBar : MonoBehaviour
    {
        [Tooltip("Boss cần theo dõi (tham chiếu scene). Gán lại bằng code qua SetBoss nếu boss sinh động.")]
        [SerializeField] private BossController boss;
        [Tooltip("Phần hiển thị cần bật/tắt (thường là con chứa nền + thanh + tên). KHÔNG được là chính object chứa component này.")]
        [SerializeField] private GameObject visualRoot;
        [Tooltip("Image thanh máu. Type=Filled thì dùng fillAmount; ngược lại co giãn theo anchorMax.x (cần anchor căng kín cha).")]
        [SerializeField] private Image fill;
        [Tooltip("Tên boss (tuỳ chọn).")]
        [SerializeField] private Text nameLabel;

        private bool _engaged;
        private bool _dead;
        private bool _subscribed;

        public void SetBoss(BossController value)
        {
            Unsubscribe();
            boss = value;
            if (isActiveAndEnabled) Subscribe();
        }

        private void Awake()
        {
            // Ẩn ngay từ đầu (trước cả khi boss kịp báo).
            SetVisible(false);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || boss == null) return;
            _subscribed = true;

            boss.HealthChanged += HandleHealthChanged;
            boss.EngagedChanged += HandleEngagedChanged;

            _engaged = boss.IsEngaged;
            _dead = boss.IsDead;
            if (nameLabel != null && boss.Definition != null) nameLabel.text = boss.Definition.DisplayName;
            ApplyRatio(HealthBarRules.Ratio(boss.CurrentHealth, boss.MaxHealth));
            RefreshVisibility();
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            if (boss != null)
            {
                boss.HealthChanged -= HandleHealthChanged;
                boss.EngagedChanged -= HandleEngagedChanged;
            }
        }

        private void HandleHealthChanged(double current, double max, double delta)
        {
            _dead = current <= 0.0;
            ApplyRatio(HealthBarRules.Ratio(current, max));
            RefreshVisibility();
        }

        private void HandleEngagedChanged(BossController sender, bool engaged)
        {
            _engaged = engaged;
            // Boss được reset (rematch) thì máu đầy lại → không còn "chết".
            if (engaged && boss != null) _dead = boss.IsDead;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            SetVisible(HealthBarRules.ShouldShowBossBar(_engaged, _dead));
        }

        private void SetVisible(bool visible)
        {
            if (visualRoot != null && visualRoot != gameObject) visualRoot.SetActive(visible);
        }

        private void ApplyRatio(float ratio)
        {
            if (fill == null) return;

            if (fill.type == Image.Type.Filled)
            {
                fill.fillAmount = ratio;
            }
            else
            {
                RectTransform rect = fill.rectTransform;
                rect.anchorMax = new Vector2(ratio, rect.anchorMax.y);
            }
        }
    }
}
