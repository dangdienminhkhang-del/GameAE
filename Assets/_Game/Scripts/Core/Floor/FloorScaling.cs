using UnityEngine;

namespace Game.Core.Floor
{
    /// <summary>
    /// Hệ số sức mạnh theo tầng (docs/07 §6, chốt ở Task B2). Quái/boss mạnh dần theo tầng bằng HỆ SỐ,
    /// KHÔNG sửa số gốc trong EnemyDefinition — thêm tầng chỉ thêm data.
    /// Tăng đều (tuyến tính), không tăng theo lũy thừa:
    ///   enemyPowerMultiplier(N) = 1 + 0.6  × (N − 1)   → HP, ATK, DEF của quái nhân hệ số này
    ///   expMultiplier(N)        = 1 + 0.75 × (N − 1)   → EXP quái rơi ra nhân hệ số này
    /// Ví dụ: Tầng 2 = 1.6 / 1.75; Tầng 5 = 3.4 / 4.0; Tầng 10 = 6.4 / 7.75.
    /// </summary>
    public static class FloorScaling
    {
        /// <summary>Mỗi tầng sau tầng 1 cộng thêm bấy nhiêu vào hệ số sức mạnh quái (docs/07 §6).</summary>
        public const float EnemyPowerIncreasePerFloor = 0.6f;

        /// <summary>Mỗi tầng sau tầng 1 cộng thêm bấy nhiêu vào hệ số EXP (docs/07 §6).</summary>
        public const float ExpIncreasePerFloor = 0.75f;

        public static float EnemyPowerMultiplier(int floorNumber)
        {
            int floor = Mathf.Max(1, floorNumber);
            return 1f + EnemyPowerIncreasePerFloor * (floor - 1);
        }

        public static float ExpMultiplier(int floorNumber)
        {
            int floor = Mathf.Max(1, floorNumber);
            return 1f + ExpIncreasePerFloor * (floor - 1);
        }
    }
}
