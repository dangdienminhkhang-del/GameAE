using System;
using Game.Core.Combat;
using Game.PvE.Enemy.Projectile;
using UnityEngine;

namespace Game.PvE.Enemy.Behaviors
{
    /// <summary>
    /// Ranged: giữ khoảng cách [retreatRange, preferredRange], vào tầm bắn thì lấy đà (telegraph có sẵn) rồi bắn một viên
    /// theo hướng tính lúc bắn. Bị dồn góc không lùi được thì vẫn đánh bình thường. Không bắn vào Safe Zone.
    /// Damage đạn tính bằng EnemyController.CreateAttackDamage (cùng công thức đòn cận chiến) rồi nhân projectileDamageScale.
    /// </summary>
    public sealed class RangedBehavior : IEnemyBehavior
    {
        private static bool s_loggedMissingPool;

        private readonly EnemyRoleConfig _config;
        private readonly Func<Vector2, bool> _isBlockedPoint;
        private readonly StuckDetector _cornered = new StuckDetector();
        private bool _wasRetreating;

        public RangedBehavior(in EnemyRoleConfig config, Func<Vector2, bool> isBlockedPoint)
        {
            _config = config;
            _isBlockedPoint = isBlockedPoint;
        }

        public CombatRole Role => CombatRole.Ranged;

        /// <summary>Đang bị dồn góc (không lùi được): quái vẫn đánh dù người chơi quá gần.</summary>
        public bool IsCornered => _cornered.IsStuck;

        public void Reset()
        {
            _cornered.Reset();
            _wasRetreating = false;
        }

        public Vector2 ChaseDirection(in BehaviorInput input)
        {
            _cornered.Tick(input.DeltaTime, input.Self, _wasRetreating);

            Vector2 dir = EnemyPositioningRules.RangedMove(input.Self, input.Target, _config.RetreatRange,
                _config.PreferredRange, _isBlockedPoint, _cornered.IsStuck, _cornered.IsStuck ? 1 : 0);

            bool tooClose = input.Distance < _config.RetreatRange;
            _wasRetreating = tooClose && dir.sqrMagnitude > 0f;
            // Quá gần mà mọi hướng lùi đều bị chặn: đánh dấu bị dồn góc ngay.
            if (tooClose && !_cornered.IsStuck && dir.sqrMagnitude <= 0f) _cornered.ForceStuck();
            return dir;
        }

        public bool ShouldEngage(in BehaviorInput input)
        {
            return RangedAttackRules.ShouldEngage(input.Distance, _config.ShootRange, _config.RetreatRange, _cornered.IsStuck);
        }

        public bool ShouldDisengage(in BehaviorInput input)
        {
            _cornered.Tick(input.DeltaTime, input.Self, false);
            return RangedAttackRules.ShouldDisengage(input.Distance, _config.ShootRange, _config.RetreatRange, _cornered.IsStuck);
        }

        public bool Strike(EnemyController owner)
        {
            Transform target = owner.Target;
            if (!RangedAttackRules.CanFire(target != null, owner.TargetInSafeZone)) return false;

            // Hồi chiêu bắt đầu dù bắn được hay không: không có pool/hết đạn thì quái không báo đòn lặp từng khung hình.
            owner.StartAttackCooldown();

            EnemyProjectilePool pool = EnemyProjectilePool.Current;
            if (pool == null)
            {
                if (!s_loggedMissingPool)
                {
                    s_loggedMissingPool = true;
                    Debug.LogWarning("[Enemy] Không có EnemyProjectilePool trong scene: quái bắn xa không thể bắn.");
                }
                return false;
            }

            DamageInfo damage = owner.CreateAttackDamage(_config.ProjectileDamageScale);
            if (damage == null) return false;

            Vector2 self = owner.transform.position;
            Vector2 dir = RangedAttackRules.AimDirection(self, target.position, Vector2.right);
            return pool.TryFire(RangedAttackRules.Muzzle(self, dir), dir, _config.ProjectileSpeed,
                _config.ProjectileLifetime, _config.ProjectileRadius, damage, owner);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_loggedMissingPool = false;
        }
    }
}
