using System;
using System.Collections.Generic;

namespace Game.Core.World
{
    /// <summary>Key Player Data trên Cloud Save (docs/03 §5).</summary>
    public static class SaveKeys
    {
        public const string Profile = "profile";
        public const string Progress = "progress";
        public const string Inventory = "inventory";
        public const string Equipment = "equipment";
        public const string Death = "death";
        public const string Quests = "quests";

        /// <summary>
        /// Mọi key của MỘT nhân vật. Reset tài khoản Guest phải xóa hết để nhân vật mới
        /// không thừa hưởng level/đồ/quest của nhân vật đã chết. Thêm key mới → thêm vào đây.
        /// </summary>
        public static readonly string[] All =
        {
            Profile, Progress, Inventory, Equipment, Death, Quests
        };
    }

    [Serializable]
    public class QuestEntryData
    {
        public string questId;
        public int status;
        public int progress;
    }

    [Serializable]
    public class QuestData
    {
        public List<QuestEntryData> quests = new List<QuestEntryData>();
    }

    [Serializable]
    public class ProfileData
    {
        public int saveVersion = 1;
        public string displayName = "Player";
        public int level = 1;
        public int exp = 0;
        public string accountType = "Guest";
        public bool isLinked = false;
        public string linkedProvider = null;
    }

    [Serializable]
    public class StackEntry
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class InstanceEntry
    {
        public string instanceId;
        public string itemId;
        public int upgradeLevel;
        public double rolledAttack;
        public double rolledDefense;
        public double rolledMaxHealth;
    }

    [Serializable]
    public class InventoryData
    {
        public List<StackEntry> stacks = new List<StackEntry>();
        public List<InstanceEntry> instances = new List<InstanceEntry>();
    }

    [Serializable]
    public class EquipmentData
    {
        public string weapon;
        public string armor;
    }

    [Serializable]
    public class ProgressData
    {
        public int personalHighestFloor = 1;
        public string currentFloorId = "floor.01";
    }
}
