using System;
using Game.Core.Presentation;
using Game.PvE.Boss;
using Game.PvE.Camera;
using Game.PvE.Enemy;
using Game.PvE.Enemy.Projectile;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Dodge
{
    /// <summary>
    /// Phát hiện và kích hoạt Cơ Chế Né Hoàn Hảo (Perfect Dodge):
    /// - Khi người chơi bấm Né đúng thời điểm quái/boss đang báo đòn (Windup/Telegraph) hoặc đạn đang bay tới gần.
    /// - Hiệu ứng: Bullet-time làm chậm thời gian, rung màn hình nhẹ, bóng dư ảnh.
    /// - Kích hoạt "Thế Phản Đòn" (Counter Stance): đòn đánh kế tiếp gây sát thương chí mạng 200%!
    /// </summary>
    [RequireComponent(typeof(DodgeController))]
    public class PerfectDodgeDetector : MonoBehaviour
    {
        public static PerfectDodgeDetector Instance { get; private set; }

        [Header("Cấu hình")]
        [SerializeField] private float dangerRadius = 2.4f;
        [SerializeField] private float slowMotionSeconds = 0.14f;
        [SerializeField] private float slowMotionTimeScale = 0.05f;
        [SerializeField] private float counterStanceDuration = 2.5f;

        [Header("Hiệu ứng Dư ảnh (Afterimage)")]
        [SerializeField] private bool enableGhostTrail = true;
        [SerializeField] private Color ghostColor = new Color(0.3f, 0.8f, 1f, 0.6f);

        private DodgeController _dodgeController;
        private PlayerAttack _playerAttack;
        private SpriteRenderer _spriteRenderer;
        private bool _isCounterStanceActive;
        private float _counterStanceTimer;

        public bool IsCounterStanceActive => _isCounterStanceActive;
        public event Action PerfectDodgeTriggered;
        public event Action CounterStanceActivated;
        public event Action CounterStanceExpired;

        private void Awake()
        {
            Instance = this;
            _dodgeController = GetComponent<DodgeController>();
            TryGetComponent(out _playerAttack);
            TryGetComponent(out _spriteRenderer);
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
        }

        private void OnEnable()
        {
            if (_dodgeController != null)
            {
                _dodgeController.OnDodgeStarted += HandleDodgeStarted;
            }
        }

        private void OnDisable()
        {
            if (_dodgeController != null)
            {
                _dodgeController.OnDodgeStarted -= HandleDodgeStarted;
            }
            _isCounterStanceActive = false;
        }

        private void Update()
        {
            if (!_isCounterStanceActive) return;

            _counterStanceTimer -= Time.deltaTime;
            if (_counterStanceTimer <= 0f)
            {
                _isCounterStanceActive = false;
                CounterStanceExpired?.Invoke();
            }
        }

        private void HandleDodgeStarted(object sender, EventArgs e)
        {
            if (CheckThreatNearby())
            {
                TriggerPerfectDodge();
            }
        }

        /// <summary>Kiểm tra xem có mối đe dọa nào đang tấn công người chơi trong bán kính hay không.</summary>
        public bool CheckThreatNearby()
        {
            Vector2 playerPos = transform.position;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(playerPos, dangerRadius);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col == null || col.transform.IsChildOf(transform)) continue;

                // 1. Quái thường đang lấy đà (Windup)
                EnemyController enemy = col.GetComponentInParent<EnemyController>();
                if (enemy != null && !enemy.IsDead && enemy.IsWindingUp)
                {
                    return true;
                }

                // 2. Boss đang báo đòn (Telegraph / Pressure Dash)
                BossController boss = col.GetComponentInParent<BossController>();
                if (boss != null && !boss.IsDead && boss.IsTelegraphing)
                {
                    return true;
                }

                // 3. Đạn quái bay tới gần
                EnemyProjectile projectile = col.GetComponentInParent<EnemyProjectile>();
                if (projectile != null && projectile.IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        private void TriggerPerfectDodge()
        {
            // 1. Bullet-time làm chậm qua PlaySpeedState
            PlaySpeedState.Shared.RequestHitStop(slowMotionSeconds, slowMotionTimeScale);

            // 2. Camera shake giật nhẹ
            CameraFollow.RequestShake(0.14f, 0.18f);

            // 3. Kích hoạt Thế Phản Đòn
            _isCounterStanceActive = true;
            _counterStanceTimer = counterStanceDuration;
            if (_playerAttack != null)
            {
                _playerAttack.ActivateCounterStance(counterStanceDuration);
            }

            // 4. Cộng điểm combo
            if (ComboTracker.Instance != null)
            {
                ComboTracker.Instance.AddHit();
            }

            // 5. Spawn bóng dư ảnh (Ghost)
            if (enableGhostTrail && _spriteRenderer != null && _spriteRenderer.sprite != null)
            {
                SpawnGhostAfterimage();
            }

            PerfectDodgeTriggered?.Invoke();
            CounterStanceActivated?.Invoke();
        }

        private void SpawnGhostAfterimage()
        {
            GameObject ghost = new GameObject("Ghost_Afterimage", typeof(SpriteRenderer));
            ghost.transform.position = transform.position;
            ghost.transform.rotation = transform.rotation;
            ghost.transform.localScale = transform.localScale;

            SpriteRenderer sr = ghost.GetComponent<SpriteRenderer>();
            sr.sprite = _spriteRenderer.sprite;
            sr.flipX = _spriteRenderer.flipX;
            sr.flipY = _spriteRenderer.flipY;
            sr.color = ghostColor;
            sr.sortingLayerID = _spriteRenderer.sortingLayerID;
            sr.sortingOrder = _spriteRenderer.sortingOrder - 1;

            Destroy(ghost, 0.35f);
        }
    }
}
