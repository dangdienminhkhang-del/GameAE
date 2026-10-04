using System.Threading.Tasks;
using Game.Core.Network;

namespace Game.Core.Network
{
    public interface INetworkTransport
    {
        bool IsConnected { get; }
        bool IsHost { get; }

        Task StartHostAsync(RelayAllocation relay);
        Task StartClientAsync(RelayAllocation relay);
        Task Shutdown();
    }
}
