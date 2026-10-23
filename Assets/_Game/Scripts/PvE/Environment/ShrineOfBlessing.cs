using System;
using System.Collections;
using Game.Core.Events;
using Game.PvE.Camera;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Environment
{
    public enum BlessingType
    {
        Swiftness, // Tăng tốc chạy & tốc đánh
        Aegis,     // Khiên hộ mệnh hấp thụ sát thương
        Vampirism  // Hút máu khi đánh trúng
    }

    /// <summary>
    /// Trụ Phước Lành Cổ Đại (Ancient Shrine):
    /// - Xuất hiện rải rác ngoài Vùng Farm.
    /// - Khi người chơi bước vào tế đàn, nhận được buff đặc biệt trong 30 giây.
    /// - Có hiệu ứng hào quang, hồi chiêu sau khi sử dụng.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ShrineOfBlessing : MonoBehaviour
    {
        [Header("Cấu hình Phước Lành")]
        [SerializeField] private BlessingType blessingType = BlessingType.Swiftness;
        [SerializeField] private float durationSeconds = 30f;
        [SerializeField] private float rechargeSeconds = 45f;

        [Header("Chỉ số Buff")]
        [SerializeField] private float moveSpeedBonus = 1.35f;
        [SerializeField] private double vampirismHealPerHit = 5.0;

        [Header("Hiển thị")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color activeColor = new Color(0.3f, 1f, 0.8f, 1f);
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

        private bool _isAvailable = true;
        private float _activeBuffTimer;
        private HealthComponent _activePlayerHealth;

        public bool IsAvailable => _isAvailable;
        public BlessingType Type => blessingType;

        public static event Action<BlessingType, float> OnBlessingGranted;

        private void Awake()
        {
            if (spriteRenderer == null) TryGetComponent(out spriteRenderer);
            UpdateVisual();
        }

        private void OnEnable()
        {
            GameEvents.OnHitResolved += HandleHitResolved;
        }

        private void OnDisable()
        {
            GameEvents.OnHitResolved -= HandleHitResolved;
        }

        private void Update()
        {
            if (_activeBuffTimer > 0f)
            {
                _activeBuffTimer -= Time.deltaTime;
                if (_activeBuffTimer <= 0f)
                {
                    EndBuff();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isAvailable || other == null) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            GrantBlessing(player);
        }

        private void GrantBlessing(PlayerController player)
        {
            _isAvailable = false;
            _activeBuffTimer = durationSeconds;
            _activePlayerHealth = player.GetComponent<HealthComponent>();

            CameraFollow.RequestShake(0.18f, 0.2f);
            UpdateVisual();

            OnBlessingGranted?.Invoke(blessingType, durationSeconds);

            StartCoroutine(RechargeRoutine());
        }

        private void EndBuff()
        {
            _activePlayerHealth = null;
        }

        private void HandleHitResolved(object sender, HitResolvedArgs args)
        {
            if (_activeBuffTimer <= 0f || blessingType != BlessingType.Vampirism) return;
            if (_activePlayerHealth == null || _activePlayerHealth.IsDead) return;

            // Hồi máu hút hồn
            _activePlayerHealth.Heal(vampirismHealPerHit);
        }

        private IEnumerator RechargeRoutine()
        {
            yield return new WaitForSeconds(rechargeSeconds);
            _isAvailable = true;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = _isAvailable ? activeColor : inactiveColor;
            }
        }
    }
}
