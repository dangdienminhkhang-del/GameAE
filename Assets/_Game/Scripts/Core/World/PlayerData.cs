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
        public const string Map = "map";
        public const string Beacons = "beacons";

        /// <summary>
        /// Mọi key của MỘT nhân vật. Reset tài khoản Guest phải xóa hết để nhân vật mới
        /// không thừa hưởng level/đồ/quest của nhân vật đã chết. Thêm key mới → thêm vào đây.
        /// </summary>
        public static readonly string[] All =
        {
            Profile, Progress, Inventory, Equipment, Death, Quests, RewardClaims, Map, Beacons
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
        /// <summary>Số Col trong ví khi lưu. -1 = chưa có dữ liệu (save cũ) → dùng Col khởi điểm.</summary>
        public int col = -1;
        /// <summary>Số điểm đã cộng vào STR. Save cũ không có field này → 0 (xem StatAllocation).</summary>
        public int strPoints = 0;
        /// <summary>Số điểm đã cộng vào AGI. Save cũ không có field này → 0.</summary>
        public int agiPoints = 0;
        /// <summary>Điểm kỹ năng đời sống: Câu cá (Fishing Proficiency theo SAO). Mặc định 0.</summary>
        public int fishingProficiency = 0;
        /// <summary>Điểm kỹ năng đời sống: Nấu ăn (Cooking Proficiency theo SAO). Mặc định 0.</summary>
        public int cookingProficiency = 0;
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
        /// <summary>Độ bền hiện tại. -1 = chưa thiết lập = đầy (save cũ không có field này). Xem DurabilityRules.</summary>
        public int durability = -1;
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
        /// <summary>Tay phụ (Dual Wielding). Save cũ không có → null = trống.</summary>
        public string offHand;
        /// <summary>Phụ kiện ô 1. Save cũ không có → null = trống.</summary>
        public string accessory;
        /// <summary>Phụ kiện ô 2. Save cũ không có → null = trống.</summary>
        public string accessory2;
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
