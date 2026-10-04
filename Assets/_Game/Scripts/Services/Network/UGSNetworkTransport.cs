using System.Threading.Tasks;
using Game.Core.Network;

namespace Game.Services.Network
{
    public class UGSNetworkTransport : INetworkTransport
    {
        public bool IsConnected { get; private set; }
        public bool IsHost { get; private set; }

        public virtual async Task StartHostAsync(RelayAllocation relay)
        {
            await Task.CompletedTask;
            IsHost = true;
            IsConnected = true;
        }

        public virtual async Task StartClientAsync(RelayAllocation relay)
        {
            await Task.CompletedTask;
            IsHost = false;
            IsConnected = true;
        }

        public virtual async Task Shutdown()
        {
            await Task.CompletedTask;
            IsConnected = false;
            IsHost = false;
        }
    }
}
