using System.Collections.Generic;

namespace Game.PvE.Skills
{
    /// <summary>
    /// Track mastery credit MỖI enemy instance (docs/07 §3, docs/12 §6.2):
    /// Mỗi enemy instance → tối đa 4 credit tổng (kể cả basic hit + technique hit).
    /// Đây là policy chống farm 1 enemy vô hạn.
    /// Gắn cùng GameObject với TechniqueController; tự clear khi chết hoặc reset scene.
    /// </summary>
    public class MasteryCreditTracker
    {
        /// <summary>Key: enemy instance id (GetInstanceID()), Value: credit đã cộng cho enemy này.</summary>
        private readonly Dictionary<int, int> _creditByEnemy = new Dictionary<int, int>();

        public const int MaxCreditPerEnemy = 4;

        public int GetRemaining(int enemyInstanceId)
        {
            if (_creditByEnemy.TryGetValue(enemyInstanceId, out int used))
                return Mathf.Clamp(MaxCreditPerEnemy - used, 0, MaxCreditPerEnemy);
            return MaxCreditPerEnemy;
        }

        /// <summary>Trả về lượng credit thực tế được cộng (0 nếu vượt cap).</summary>
        public int TryConsume(int enemyInstanceId, int requestedCredit)
        {
            if (requestedCredit <= 0) return 0;
            int remaining = GetRemaining(enemyInstanceId);
            int actual = Mathf.Min(remaining, requestedCredit);
            if (actual <= 0) return 0;

            _creditByEnemy.TryGetValue(enemyInstanceId, out int old);
            _creditByEnemy[enemyInstanceId] = old + actual;
            return actual;
        }

        public void Clear()
        {
            _creditByEnemy.Clear();
        }
    }

    internal static class Mathf
    {
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int Min(int a, int b) => a < b ? a : b;
    }
}
