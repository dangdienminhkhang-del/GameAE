using Game.Core.Combat;
using Game.PvE.SafeZone;
using UnityEngine;

namespace Game.PvE.Enemy.Projectile
{
    /// <summary>
    /// Một viên đạn của quái Ranged (Phase 9.8). Bay thẳng theo hướng đã khóa lúc bắn (không bám theo),
    /// di chuyển bằng transform với Time.deltaTime (có scale: dừng hình/Pause làm đạn chậm theo, cùng quy ước với quái).
    /// Mỗi bước kiểm va chạm theo ĐOẠN (không xuyên khi dt lớn). Damage lên người chơi đi qua IDamageable.TakeDamage
    /// như đòn cận chiến, nên hiệu ứng nháy/số sát thương/âm thanh bị đánh tự chạy từ OnHealthChanged.
    /// Thu hồi khi: hết lifetime, trúng người chơi, chạm vật cản, vào Safe Zone, người chơi chết, tầng bị tắt.
    /// </summary>
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private EnemyProjectilePool _pool;
        private EnemyController _owner;
        private DamageInfo _damage;
        private Vector2 _direction;
        private float _speed;
        private float _lifetime;
        private float _radius;
        private float _age;
        private bool _active;

        public bool IsActive => _active;
        public Vector2 Direction => _direction;
        public float Age => _age;

        internal void Bind(EnemyProjectilePool pool)
        {
            _pool = pool;
        }

        /// <summary>Kích hoạt đạn tại <paramref name="origin"/> theo hướng đơn vị <paramref name="direction"/>.</summary>
        internal void Launch(Vector2 origin, Vector2 direction, float speed, float lifetime, float radius,
            DamageInfo damage, EnemyController owner)
        {
            _direction = direction;
            _speed = speed;
            _lifetime = lifetime;
            _radius = radius;
            _damage = damage;
            _owner = owner;
            _age = 0f;
            _active = true;

            transform.position = new Vector3(origin.x, origin.y, transform.position.z);
            if (direction.sqrMagnitude > 1e-6f) transform.right = new Vector3(direction.x, direction.y, 0f);
            gameObject.SetActive(true);
        }

        internal void Deactivate()
        {
            _active = false;
            _owner = null;
            _damage = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            Tick(UnityEngine.Time.deltaTime);
        }

        /// <summary>Một bước mô phỏng. Public để test điều khiển được dt.</summary>
        public void Tick(float deltaTime)
        {
            if (!_active) return;

            float dt = deltaTime < 0f ? 0f : deltaTime;
            _age += dt;
            if (ProjectileMath.IsExpired(_age, _lifetime)) { Release(); return; }

            // Tầng bị tắt: quái bắn ra đạn này đang bật riêng nhưng cha đã tắt. Quái chỉ chết/về pool (activeSelf = false) thì đạn vẫn bay.
            if (_owner != null && _owner.gameObject.activeSelf && !_owner.gameObject.activeInHierarchy)
            {
                Release();
                return;
            }

            Vector2 start = transform.position;
            Vector2 end = ProjectileMath.Step(start, _direction, _speed, dt);

            // Vật cản trên đoạn này (Linecast không cấp phát). Tính trước để tường chắn đạn trước khi tới người chơi.
            bool obstacleHit = false;
            float obstacleFraction = 1f;
            if (_pool != null && _pool.ObstacleMask.value != 0)
            {
                RaycastHit2D wall = Physics2D.Linecast(start, end, _pool.ObstacleMask);
                if (wall.collider != null)
                {
                    obstacleHit = true;
                    obstacleFraction = wall.fraction;
                }
            }

            // 1) Người chơi: kiểm theo đoạn start→end.
            Transform playerTransform = _pool != null ? _pool.PlayerTransform : null;
            IDamageable player = _pool != null ? _pool.PlayerDamageable : null;
            if (player != null && player.IsDead) { Release(); return; }

            if (playerTransform != null && player != null)
            {
                float hitRadius = _radius + _pool.PlayerHitRadius;
                if (ProjectileMath.SegmentHitsCircle(start, end, playerTransform.position, hitRadius))
                {
                    // Tường nằm trước người chơi trên đường đạn: đạn tắt tại tường, không gây damage.
                    if (obstacleHit && obstacleFraction <= ProjectileMath.ClosestApproachFraction(start, end, playerTransform.position))
                    {
                        Release();
                        return;
                    }

                    // Người chơi đứng trong Safe Zone thì đạn tan, không gây damage (IDamageable cũng chặn, đây là lớp phòng thủ thứ hai).
                    bool inSafeZone = SafeZoneProvider.GetOrCreate().IsSafeZoneAt(playerTransform.position);
                    if (!inSafeZone) player.TakeDamage(_damage);
                    Release();
                    return;
                }
            }

            // 2) Safe Zone: đạn không bay vào vùng an toàn.
            if (SafeZoneProvider.GetOrCreate().IsSafeZoneAt(end)) { Release(); return; }

            // 3) Vật cản (không có người chơi trên đường đạn).
            if (obstacleHit) { Release(); return; }

            transform.position = new Vector3(end.x, end.y, transform.position.z);
        }

        private void Release()
        {
            if (_pool != null) _pool.Release(this);
            else Deactivate();
        }
    }
}
