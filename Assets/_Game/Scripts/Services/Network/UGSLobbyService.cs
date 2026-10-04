using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Network;

namespace Game.Services.Network
{
    public class UGSLobbyService : ILobbyService
    {
        public bool IsInLobby => CurrentLobby != null;
        public LobbyInfo CurrentLobby { get; private set; }

        public virtual async Task<LobbyInfo> CreateLobbyAsync(string lobbyName, bool isChallenge = false)
        {
            await Task.CompletedTask;
            CurrentLobby = new LobbyInfo
            {
                LobbyId = System.Guid.NewGuid().ToString("N"),
                LobbyCode = "ABCD" + Random.Range(1000, 9999),
                HostPlayerId = "host",
                Players = new Dictionary<string, string> { { "host", "Host" } },
                IsChallenge = isChallenge
            };
            return CurrentLobby;
        }

        public virtual async Task<LobbyInfo> JoinLobbyByCodeAsync(string lobbyCode)
        {
            await Task.CompletedTask;
            CurrentLobby = new LobbyInfo
            {
                LobbyId = "joined_" + lobbyCode,
                LobbyCode = lobbyCode,
                HostPlayerId = "host",
                Players = new Dictionary<string, string>
                {
                    { "host", "Host" },
                    { "client", "Client" }
                },
                IsChallenge = false
            };
            return CurrentLobby;
        }

        public virtual async Task LeaveLobbyAsync()
        {
            await Task.CompletedTask;
            CurrentLobby = null;
        }

        public virtual async Task SetLobbyReadyAsync(bool isReady)
        {
            await Task.CompletedTask;
        }

        private static class Random
        {
            private static readonly System.Random _r = new System.Random();
            public static int Range(int min, int max) => _r.Next(min, max);
        }
    }
}
