using System.Reflection;
using Game.Core.Stats;
using Game.PvE.Player;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Dựng người chơi thật (HealthComponent của game) cho test đạn/quái. Không thể dùng MonoBehaviour giả viết trong assembly test:
    /// Unity không cho AddComponent script nằm trong assembly Editor-only. HealthComponent là đường nhận damage thật nên cũng
    /// chứng minh đạn đi đúng đường chung (OnHealthChanged phát qua GameEvents như đòn cận chiến).
    /// Start() không chạy trong EditMode nên gán thẳng CharacterStats và máu bằng reflection.
    /// </summary>
    public static class ProjectileTestPlayer
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        public static HealthComponent Create(GameObject go)
        {
            HealthComponent health = go.AddComponent<HealthComponent>();
            var stats = new CharacterStats(1);
            typeof(HealthComponent).GetField("_stats", Private).SetValue(health, stats);
            typeof(HealthComponent).GetField("_currentHealth", Private).SetValue(health, stats.MaxHealth);
            return health;
        }

        public static void Kill(HealthComponent health)
        {
            typeof(HealthComponent).GetField("_currentHealth", Private).SetValue(health, 0.0);
        }
    }
}
