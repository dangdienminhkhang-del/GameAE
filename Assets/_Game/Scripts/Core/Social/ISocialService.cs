using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Core.Social
{
    /// <summary>
    /// Giao diện dịch vụ Xã hội (Tổ đội, Bang hội, Bạn bè) theo Luật Backend Abstraction (Luật 9).
    /// Gameplay hoàn toàn không gọi trực tiếp UGS Friends / Lobby SDK.
    /// </summary>
    public interface ISocialService
    {
        Task<bool> SendPartyInviteAsync(string targetPlayerId);
        Task<bool> LeavePartyAsync();
        Task<List<FriendEntry>> GetFriendsListAsync();
        Task<GuildData> GetGuildInfoAsync();
    }
}
