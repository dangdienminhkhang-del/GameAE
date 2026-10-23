using UnityEngine;

namespace Game.PvE.Enemy.Behaviors
{
    /// <summary>Đầu vào mỗi khung hình cho behavior của quái (dữ liệu thuần).</summary>
    public readonly struct BehaviorInput
    {
        public readonly Vector2 Self;
        public readonly Vector2 Target;
        public readonly float Distance;
        public readonly float DeltaTime;

        public BehaviorInput(Vector2 self, Vector2 target, float distance, float deltaTime)
        {
            Self = self;
            Target = target;
            Distance = distance;
            DeltaTime = deltaTime;
        }
    }

    /// <summary>Số liệu vai trò chép từ EnemyDefinition một lần khi tạo behavior (không đọc lại mỗi frame).</summary>
    public readonly struct EnemyRoleConfig
    {
        public readonly CombatRole Role;
        public readonly float AttackRange;
        public readonly float DetectionRange;
        public readonly float PreferredRange;
        public readonly float RetreatRange;
        public readonly float RetreatSeconds;
        public readonly float ShootRange;
        public readonly float ProjectileSpeed;
        public readonly float ProjectileLifetime;
        public readonly float ProjectileRadius;
        public readonly float ProjectileDamageScale;

        public EnemyRoleConfig(CombatRole role, float attackRange, float detectionRange, float preferredRange,
            float retreatRange, float retreatSeconds, float shootRange, float projectileSpeed,
            float projectileLifetime, float projectileRadius, float projectileDamageScale)
        {
            Role = role;
            AttackRange = attackRange;
            DetectionRange = detectionRange;
            PreferredRange = preferredRange;
            RetreatRange = retreatRange;
            RetreatSeconds = retreatSeconds;
            ShootRange = shootRange;
            ProjectileSpeed = projectileSpeed;
            ProjectileLifetime = projectileLifetime;
            ProjectileRadius = projectileRadius;
            ProjectileDamageScale = projectileDamageScale;
        }

        /// <summary>Chép từ definition; definition null thì dùng giá trị mặc định (Chaser).</summary>
        public static EnemyRoleConfig From(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return new EnemyRoleConfig(CombatRole.Chaser, 1.1f, 6f, 4.5f, 2.5f, 0.8f, 6f, 8f, 1.5f, 0.25f, 1f);
            }

            return new EnemyRoleConfig(
                definition.Role, definition.AttackRange, definition.DetectionRange, definition.PreferredRange,
                definition.RetreatRange, definition.RetreatSeconds, definition.ShootRange, definition.ProjectileSpeed,
                definition.ProjectileLifetime, definition.ProjectileRadius, definition.ProjectileDamageScale);
        }
    }

    /// <summary>
    /// Hành vi theo vai trò (Phase 9.8). EnemyController chỉ có ba điểm nối:
    /// (1) hướng di chuyển trong Chase, (2) điều kiện vào/ra Attack, (3) hành động tại thời điểm đánh.
    /// Mọi behavior được tạo MỘT lần khi quái được cấu hình, không cấp phát trong Update.
    /// </summary>
    public interface IEnemyBehavior
    {
        CombatRole Role { get; }

        /// <summary>Đưa về trạng thái đầu (quái hồi sinh từ pool).</summary>
        void Reset();

        /// <summary>Điểm nối 1: hướng đi trong Chase (vector đơn vị; zero = đứng yên). Gọi mỗi khung hình ở Chase.</summary>
        Vector2 ChaseDirection(in BehaviorInput input);

        /// <summary>Điểm nối 2a: có vào Attack không (Chase → Attack).</summary>
        bool ShouldEngage(in BehaviorInput input);

        /// <summary>Điểm nối 2b: có rời Attack không (Attack → Chase). Gọi mỗi khung hình ở Attack.</summary>
        bool ShouldDisengage(in BehaviorInput input);

        /// <summary>Điểm nối 3: ra đòn khi hết lấy đà. Trả true nếu đã đánh/bắn.</summary>
        bool Strike(EnemyController owner);
    }
}
