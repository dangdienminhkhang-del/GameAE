using System.Threading.Tasks;

namespace Game.Core.Network
{
    public class RelayAllocation
    {
        public string JoinCode;
        public byte[] AllocationIdBytes;
        public byte[] Key;
        public string Ip;
        public int Port;
        public bool IsHost;
    }

    public interface IRelayService
    {
        Task<RelayAllocation> AllocateRelayAsync(int maxConnections = 2);
        Task<RelayAllocation> JoinRelayAsync(string joinCode);
    }
}
