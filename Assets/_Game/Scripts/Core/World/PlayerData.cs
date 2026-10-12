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
        public const string RewardClaims = "rewardClaims";

        /// <summary>
        /// Mọi key của MỘT nhân vật. Reset tài khoản Guest phải xóa hết để nhân vật mới
        /// không thừa hưởng level/đồ/quest của nhân vật đã chết. Thêm key mới → thêm vào đây.
        /// </summary>
        public static readonly string[] All =
        {
            Profile, Progress, Inventory, Equipment, Death, Quests, RewardClaims
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
        /// <summary>
        /// HP hiện tại khi lưu. 0 = chưa có dữ liệu (lần đầu, hoặc save cũ chưa có field này)
        /// → HealthComponent sẽ dùng MaxHealth mặc định thay thế.
        /// </summary>
        public double currentHp = 0;
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

    /// <summary>Một claim thưởng một lần (docs/03 §5 — Wave 3).</summary>
    [Serializable]
    public class RewardClaimEntry
    {
        public string type; // "FirstClear"
        public string bossId;
        public long claimedAtUnixSeconds;
    }

    /// <summary>Bản ghi danh sách các phần thưởng đã nhận (docs/03 §5).</summary>
    [Serializable]
    public class RewardClaimsData
    {
        public int schemaVersion = 1;
        public List<RewardClaimEntry> claims = new List<RewardClaimEntry>();

        public bool HasClaim(string type, string bossId)
        {
            if (claims == null) return false;
            return claims.Exists(c => c != null && c.type == type && c.bossId == bossId);
        }

        public void AddClaim(string type, string bossId, long timestamp)
        {
            if (claims == null) claims = new List<RewardClaimEntry>();
            if (!HasClaim(type, bossId))
            {
                claims.Add(new RewardClaimEntry
                {
                    type = type,
                    bossId = bossId,
                    claimedAtUnixSeconds = timestamp
                });
            }
        }
    }
}
