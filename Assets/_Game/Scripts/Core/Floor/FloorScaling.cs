using UnityEngine;

namespace Game.Core.Floor
{
    /// <summary>
    /// Hệ số sức mạnh theo tầng (docs/07 §5). Quái/boss mạnh dần theo tầng bằng HỆ SỐ,
    /// KHÔNG sửa số gốc trong EnemyDefinition — thêm tầng chỉ thêm data.
    ///   enemyPowerMultiplier(floor) = 1.15 ^ (floor - 1)
    ///   expMultiplier(floor)        = 1.20 ^ (floor - 1)
    /// </summary>
    public static class FloorScaling
    {
        public const float EnemyPowerPerFloor = 1.15f;
        public const float ExpPerFloor = 1.20f;

        public static float EnemyPowerMultiplier(int floorNumber)
        {
            int floor = Mathf.Max(1, floorNumber);
            return Mathf.Pow(EnemyPowerPerFloor, floor - 1);
        }

        public static float ExpMultiplier(int floorNumber)
        {
            int floor = Mathf.Max(1, floorNumber);
            return Mathf.Pow(ExpPerFloor, floor - 1);
        }
    }
}
