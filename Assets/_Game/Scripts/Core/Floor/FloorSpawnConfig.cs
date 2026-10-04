using System;

namespace Game.Core.Floor
{
    /// <summary>
    /// Cấu hình spawn quái trên một tầng.
    /// Là plain C# class (không phải ScriptableObject) để serialize trong List bên trong FloorDefinition.
    /// </summary>
    [Serializable]
    public class FloorSpawnConfig
    {
        /// <summary>ID của EnemyDefinition (ví dụ: "enemy.goblin_01").</summary>
        public string enemyDefinitionId;

        /// <summary>Số lượng quái spawn theo config này.</summary>
        public int spawnCount;

        /// <summary>
        /// Bitmask pha thời gian được phép spawn.
        /// 0 = spawn ở mọi pha.
        /// Các bit tương ứng với TimeOfDay enum:
        ///   Bit 0 = Dawn, Bit 1 = Morning, Bit 2 = Afternoon,
        ///   Bit 3 = Evening, Bit 4 = Night
        /// </summary>
        public int allowedTimePhases;
    }
}
