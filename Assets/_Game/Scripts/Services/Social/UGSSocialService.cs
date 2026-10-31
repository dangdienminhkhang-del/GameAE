using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Social;
using UnityEngine;

namespace Game.Services.Social
{
    /// <summary>
    /// Triển khai dịch vụ Xã hội qua Unity Gaming Services (Lobby / Friends / Cloud Code).
    /// Tuân thủ Luật Backend Abstraction số 9: đặt trong Services, không gọi trực tiếp từ gameplay.
    ///
    /// CHÚ Ý (ONLINE SERVICES):
    /// Cần kích hoạt và cấu hình dịch vụ "Lobby" và "Friends" trên Unity Gaming Services Dashboard của bạn:
    /// - UGS Dashboard -> Multiplayer -> Lobby & Relay -> Create Configuration
    /// - UGS Dashboard -> Player Engagement -> Friends -> Enable
    /// Hiện tại trả về dữ liệu mẫu (Mock/Fallback) khi chưa có kết nối thật từ dashboard.
    /// </summary>
    public class UGSSocialService : ISocialService
    {
        public Task<bool> SendPartyInviteAsync(string targetPlayerId)
        {
            // TODO: Kết nối với Unity.Services.Lobby hoặc Unity Friends SDK
            Debug.Log($"[UGSSocialService] Gửi lời mời tổ đội tới player: {targetPlayerId} (Cần cấu hình UGS Lobby Dashboard)");
            return Task.FromResult(true);
        }

        public Task<bool> LeavePartyAsync()
        {
            // TODO: Rời khỏi Lobby hiện tại trên UGS
            Debug.Log("[UGSSocialService] Rời khỏi tổ đội hiện tại");
            return Task.FromResult(true);
        }

        public Task<List<FriendEntry>> GetFriendsListAsync()
        {
            // TODO: Gọi Unity.Services.Friends.FriendsService.Instance.GetFriendsAsync()
            var list = new List<FriendEntry>
            {
                new FriendEntry { friendId = "f1", friendName = "Klein", isOnline = true, currentFloor = 1 },
                new FriendEntry { friendId = "f2", friendName = "Agil", isOnline = true, currentFloor = 1 },
                new FriendEntry { friendId = "f3", friendName = "Silica", isOnline = false, currentFloor = 2 },
                new FriendEntry { friendId = "f4", friendName = "Lisbeth", isOnline = true, currentFloor = 2 }
            };
            return Task.FromResult(list);
        }

        public Task<GuildData> GetGuildInfoAsync()
        {
            // TODO: Gọi Cloud Code hoặc UGS Groups SDK
            var guild = new GuildData
            {
                guildId = "guild.kob",
                guildName = "Huyết Kỵ Đoàn (Knights of the Blood)",
                leaderPlayerId = "Heathcliff",
                memberNames = new List<string> { "Heathcliff (Bang Chủ)", "Asuna (Phó Bang Chủ)", "Daizen", "Godfree" },
                maxMembers = 30
            };
            return Task.FromResult(guild);
        }
    }
}
