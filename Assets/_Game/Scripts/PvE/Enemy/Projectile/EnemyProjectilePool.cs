using System;
using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Events;
using UnityEngine;

namespace Game.PvE.Enemy.Projectile
{
    /// <summary>
    /// Pool đạn của quái (Phase 9.8). Một pool cho cả scene PvE, dựng sẵn số lượng cố định lúc Awake:
    /// KHÔNG Instantiate trong combat. Hết chỗ thì bỏ phát bắn đó và chỉ log MỘT lần.
    /// Đăng ký vào <see cref="Current"/> khi bật (quái lấy một lần rồi cache, không Find mỗi lần bắn).
    /// </summary>
    public sealed class EnemyProjectilePool : MonoBehaviour
    {
        public const int DefaultPoolSize = 16;
        public const float DefaultPlayerHitRadius = 0.4f;

        [Tooltip("Prefab đạn: GameObject có component EnemyProjectile (sprite tròn nhỏ làm placeholder).")]
        [SerializeField] private EnemyProjectile projectilePrefab;
        [Tooltip("Số đạn dựng sẵn. Hết chỗ thì bỏ phát bắn đó (log một lần).")]
        [SerializeField] private int poolSize = DefaultPoolSize;
        [Tooltip("Layer của tường/vật cản chặn đạn. Để trống (Nothing) nếu scene chưa có vật cản.")]
        [SerializeField] private LayerMask obstacleMask;
        [Tooltip("Bán kính trúng của người chơi (cộng với bán kính đạn).")]
        [SerializeField] private float playerHitRadius = DefaultPlayerHitRadius;

        private readonly Stack<EnemyProjectile> _free = new Stack<EnemyProjectile>();
        private readonly List<EnemyProjectile> _active = new List<EnemyProjectile>();
        private bool _prewarmed;
        private bool _loggedExhausted;
        private Transform _playerTransform;
        private IDamageable _playerDamageable;

        /// <summary>Pool đang bật trong scene (null nếu chưa có). Quái cache tham chiếu này một lần.</summary>
        public static EnemyProjectilePool Current { get; private set; }

        /// <summary>Báo mỗi lần một viên đạn được bắn đi (SFX nghe cái này). Xóa khi vào Play.</summary>
        public static event Action Fired;

        public int Capacity { get; private set; }
        public int ActiveCount => _active.Count;
        public int FreeCount => _free.Count;

        /// <summary>Số lần Instantiate đã làm (chỉ tăng lúc dựng sẵn). Test dùng để chứng minh không Instantiate khi chiến đấu.</summary>
        public int InstantiateCount { get; private set; }

        public LayerMask ObstacleMask
        {
            get => obstacleMask;
            set => obstacleMask = value;
        }

        public float PlayerHitRadius
        {
            get => playerHitRadius;
            set => playerHitRadius = Mathf.Max(0f, value);
        }

        public Transform PlayerTransform => _playerTransform;
        public IDamageable PlayerDamageable => _playerDamageable;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Fired = null;
        }

        private void Awake()
        {
            if (projectilePrefab != null) Prewarm(projectilePrefab, poolSize);
        }

        private void OnEnable()
        {
            Current = this;
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            if (ReferenceEquals(Current, this)) Current = null;
            ReleaseAll();
        }

        /// <summary>Dựng sẵn <paramref name="size"/> viên đạn từ prefab (chỉ một lần).</summary>
        public void Prewarm(EnemyProjectile prefab, int size)
        {
            if (_prewarmed || prefab == null) return;
            _prewarmed = true;

            Capacity = Mathf.Max(1, size);
            for (int i = 0; i < Capacity; i++)
            {
                EnemyProjectile projectile = Instantiate(prefab, transform);
                InstantiateCount++;
                projectile.name = prefab.name + "_" + i;
                projectile.Bind(this);
                projectile.Deactivate();
                _free.Push(projectile);
            }
        }

        /// <summary>Gắn người chơi (lấy từ mục tiêu của quái). Chỉ đổi khi khác tham chiếu cũ.</summary>
        public void BindPlayer(Transform player)
        {
            if (player == null || ReferenceEquals(player, _playerTransform)) return;
            _playerTransform = player;
            _playerDamageable = player.GetComponentInParent<IDamageable>();
        }

        /// <summary>
        /// Bắn một viên. Trả false nếu hết chỗ (bỏ phát bắn, log một lần) hoặc chưa dựng sẵn.
        /// Hướng <paramref name="direction"/> là vector đơn vị đã KHÓA, đạn không đổi hướng sau đó.
        /// </summary>
        public bool TryFire(Vector2 origin, Vector2 direction, float speed, float lifetime, float radius,
            DamageInfo damage, EnemyController owner)
        {
            if (damage == null) return false;

            if (owner != null) BindPlayer(owner.Target);

            if (_free.Count == 0)
            {
                if (!_loggedExhausted)
                {
                    _loggedExhausted = true;
                    Debug.LogWarning($"[EnemyProjectilePool] Hết đạn dựng sẵn ({Capacity}). Phát bắn này bị bỏ; tăng poolSize nếu cần.", this);
                }
                return false;
            }

            EnemyProjectile projectile = _free.Pop();
            _active.Add(projectile);
            projectile.Launch(origin, direction, speed, lifetime, radius, damage, owner);
            Fired?.Invoke();
            return true;
        }

        /// <summary>Trả đạn về pool (an toàn khi gọi hai lần).</summary>
        public void Release(EnemyProjectile projectile)
        {
            if (projectile == null) return;
            if (!_active.Remove(projectile)) return;
            projectile.Deactivate();
            _free.Push(projectile);
        }

        /// <summary>Thu hồi mọi đạn đang bay (người chơi chết, tầng tắt, pool bị tắt).</summary>
        public void ReleaseAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                EnemyProjectile projectile = _active[i];
                _active.RemoveAt(i);
                if (projectile == null) continue;
                projectile.Deactivate();
                _free.Push(projectile);
            }
        }

        private void HandlePlayerDied(object sender, DamageInfo lastHit)
        {
            ReleaseAll();
        }
    }
}
