using System;
using System.Collections.Generic;

namespace Game.Core.Social
{
    [Serializable]
    public class PartyMember
    {
        public string playerId;
        public string playerName;
        public int level;
        public double currentHp;
        public double maxHp;
        public bool isLeader;
        public bool isDead;

        public PartyMember(string id, string name, int level, double maxHp, bool isLeader = false)
        {
            this.playerId = id;
            this.playerName = name;
            this.level = level;
            this.maxHp = maxHp;
            this.currentHp = maxHp;
            this.isLeader = isLeader;
            this.isDead = false;
        }
    }

    [Serializable]
    public class GuildData
    {
        public string guildId;
        public string guildName;
        public string leaderPlayerId;
        public List<string> memberNames = new List<string>();
        public int maxMembers = 30;
    }

    [Serializable]
    public class FriendEntry
    {
        public string friendId;
        public string friendName;
        public bool isOnline;
        public int currentFloor;
    }

    /// <summary>
    /// Quy tắc Tổ Đội (Party), Bang Hội (Guild) và Đột Kích (Raid) theo Sword Art Online (docs/16 & prompt).
    /// - 1 Party tiêu chuẩn SAO tối đa 6 thành viên (Kirito, Asuna, Klein, Agil, Silica, Lisbeth).
    /// - 1 Raid săn Boss gồm nhiều Party phối hợp (4-8 party = 24-48 người, config tuỳ chỉnh).
    /// - Có thưởng EXP cộng dồn khi đánh quái theo đội.
    /// Logic C# thuần, dễ kiểm thử.
    /// </summary>
    public static class PartyRules
    {
        /// <summary>Số thành viên tối đa trong 1 party theo chuẩn SAO.</summary>
        public const int MaxPartySize = 6;

        /// <summary>Số party tối đa trong 1 Raid săn Boss (giả định 8 party = 48 người theo SAO).</summary>
        public const int MaxRaidParties = 8;

        /// <summary>
        /// Kiểm tra xem có thể thêm thành viên vào party không.
        /// </summary>
        public static bool CanAddMember(int currentCount, string newMemberId, List<PartyMember> existingMembers)
        {
            if (string.IsNullOrEmpty(newMemberId)) return false;
            if (currentCount >= MaxPartySize) return false;
            if (existingMembers == null) return false;

            for (int i = 0; i < existingMembers.Count; i++)
            {
                if (existingMembers[i] != null && existingMembers[i].playerId == newMemberId)
                {
                    return false; // Đã có trong party
                }
            }

            return true;
        }

        /// <summary>
        /// Tính hệ số thưởng EXP tổng khi đánh quái theo nhóm.
        /// Càng đông người càng được cộng thêm % EXP tổng:
        /// 1 người = 1.0, 2 người = 1.1, 3 người = 1.2 ... 6 người = 1.5.
        /// </summary>
        public static double GetPartyExpMultiplier(int memberCount)
        {
            if (memberCount <= 1) return 1.0;
            int count = Math.Min(memberCount, MaxPartySize);
            return 1.0 + (count - 1) * 0.1;
        }

        /// <summary>
        /// Chia đều EXP quái hạ gục cho các thành viên trong đội còn sống.
        /// </summary>
        public static int CalculateExpPerMember(int baseEnemyExp, int aliveMemberCount)
        {
            if (aliveMemberCount <= 0 || baseEnemyExp <= 0) return 0;
            double totalExp = baseEnemyExp * GetPartyExpMultiplier(aliveMemberCount);
            return Math.Max(1, (int)Math.Round(totalExp / aliveMemberCount));
        }

        /// <summary>
        /// Kiểm tra xem số lượng party có hợp lệ để thành lập một Raid săn Boss không.
        /// </summary>
        public static bool IsValidRaid(int partyCount)
        {
            return partyCount >= 2 && partyCount <= MaxRaidParties;
        }
    }
}
