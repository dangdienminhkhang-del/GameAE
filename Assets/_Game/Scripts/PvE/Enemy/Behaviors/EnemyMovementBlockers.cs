using System;
using Game.PvE.Enemy.Projectile;
using Game.PvE.SafeZone;
using UnityEngine;

namespace Game.PvE.Enemy.Behaviors
{
    /// <summary>
    /// Điểm nào quái KHÔNG được lùi vào: Safe Zone và vật cản (layer lấy từ EnemyProjectilePool.ObstacleMask).
    /// Delegate được tạo một lần, không cấp phát khi dùng.
    /// </summary>
    public static class EnemyMovementBlockers
    {
        public static readonly Func<Vector2, bool> IsBlockedPoint = IsBlocked;

        private static bool IsBlocked(Vector2 point)
        {
            if (SafeZoneProvider.GetOrCreate().IsSafeZoneAt(point)) return true;

            EnemyProjectilePool pool = EnemyProjectilePool.Current;
            if (pool != null && pool.ObstacleMask.value != 0)
            {
                return Physics2D.OverlapPoint(point, pool.ObstacleMask) != null;
            }
            return false;
        }
    }
}
