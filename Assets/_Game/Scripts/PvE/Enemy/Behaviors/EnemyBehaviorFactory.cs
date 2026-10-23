using System;
using UnityEngine;

namespace Game.PvE.Enemy.Behaviors
{
    /// <summary>
    /// Chọn behavior theo CombatRole. Gọi MỘT lần khi quái được cấu hình (không mỗi frame).
    /// Guard và Elite tạm dùng ChaserBehavior (chưa có thiết kế riêng).
    /// </summary>
    public static class EnemyBehaviorFactory
    {
        /// <summary>Tạo behavior; hướng lùi tránh Safe Zone và vật cản (EnemyMovementBlockers).</summary>
        public static IEnemyBehavior Create(EnemyRoleConfig config)
        {
            return Create(config, EnemyMovementBlockers.IsBlockedPoint);
        }

        /// <summary>Tạo behavior với hàm kiểm "điểm bị chặn" tự chọn (test dùng để không cần Safe Zone thật).</summary>
        public static IEnemyBehavior Create(EnemyRoleConfig config, Func<Vector2, bool> isBlockedPoint)
        {
            switch (config.Role)
            {
                case CombatRole.Skirmisher:
                    return new SkirmisherBehavior(config, isBlockedPoint);
                case CombatRole.Ranged:
                    return new RangedBehavior(config, isBlockedPoint);
                default:
                    // Chaser, Guard, Elite (và giá trị lạ) giữ hành vi cũ.
                    return new ChaserBehavior(config.Role, config.AttackRange);
            }
        }
    }
}
