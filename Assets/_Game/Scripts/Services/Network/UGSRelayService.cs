using System.Threading.Tasks;
using Game.Core.Network;

namespace Game.Services.Network
{
    public class UGSRelayService : IRelayService
    {
        public virtual async Task<RelayAllocation> AllocateRelayAsync(int maxConnections = 2)
        {
            await Task.CompletedTask;
            return new RelayAllocation
            {
                JoinCode = "RELAY" + System.Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                AllocationIdBytes = new byte[16],
                Key = new byte[16],
                Ip = "127.0.0.1",
                Port = 7777,
                IsHost = true
            };
        }

        public virtual async Task<RelayAllocation> JoinRelayAsync(string joinCode)
        {
            await Task.CompletedTask;
            return new RelayAllocation
            {
                JoinCode = joinCode,
                AllocationIdBytes = new byte[16],
                Key = new byte[16],
                Ip = "127.0.0.1",
                Port = 7777,
                IsHost = false
            };
        }
    }
}
