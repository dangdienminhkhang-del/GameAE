using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Core.Network
{
    public class LobbyInfo
    {
        public string LobbyId;
        public string LobbyCode;
        public string HostPlayerId;
        public Dictionary<string, string> Players;
        public bool IsChallenge;
        public int MaxPlayers = 2;
    }

    public interface ILobbyService
    {
        bool IsInLobby { get; }
        LobbyInfo CurrentLobby { get; }

        Task<LobbyInfo> CreateLobbyAsync(string lobbyName, bool isChallenge = false);
        Task<LobbyInfo> JoinLobbyByCodeAsync(string lobbyCode);
        Task LeaveLobbyAsync();
        Task SetLobbyReadyAsync(bool isReady);
    }
}
