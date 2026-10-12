using Game.Core.Account;
using Game.Core.Character;
using Game.Core.Network;
using Game.Core.Time;
using Game.Core.World;
using Game.Services.Config;
using UnityEngine;

namespace Game.Services
{
    public static class ServiceProvider
    {
        private static BackendConfig _config;
        private static IAccountService _accountService;

        public static void SetConfig(BackendConfig config)
        {
            _config = config;
        }

        private static string BackendType => _config != null ? _config.BackendType : "UGS";

        /// <summary>
        /// Một instance IAccountService dùng chung cho cả app: các service khác (reset/link)
        /// phải thấy cùng trạng thái đăng nhập, nếu không CanResetCurrentAccount() luôn sai.
        /// </summary>
        public static IAccountService GetAccountService()
        {
            if (_accountService == null) _accountService = CreateAccountService();
            return _accountService;
        }

        public static IAccountService CreateAccountService()
        {
            return BackendType switch
            {
#if UGS_AUTH_PRESENT
                "UGS" => new Account.UGSAccountService(),
#else
                "UGS" => new Account.AccountService(),
#endif
                "Spring" => new Account.AccountService(),
                "Hybrid" => new Account.AccountService(),
                _ => new Account.AccountService()
            };
        }

        public static IAccountResetService CreateAccountResetService()
        {
            return BackendType switch
            {
                "UGS" => new Account.AccountResetService(GetAccountService(), CreateSaveService()),
                "Spring" => new Account.AccountResetService(GetAccountService(), CreateSaveService()),
                "Hybrid" => new Account.AccountResetService(GetAccountService(), CreateSaveService()),
                _ => new Account.AccountResetService(GetAccountService(), CreateSaveService())
            };
        }

        public static IAccountLinkService CreateAccountLinkService()
        {
            return BackendType switch
            {
                "UGS" => new Account.AccountLinkService(),
                "Spring" => new Account.AccountLinkService(),
                "Hybrid" => new Account.AccountLinkService(),
                _ => new Account.AccountLinkService()
            };
        }

        public static ISaveService CreateSaveService()
        {
            return BackendType switch
            {
                "UGS" => new World.CloudPlayerSaveService(),
                "Spring" => new World.PlayerSaveService(),
                "Hybrid" => new World.CloudPlayerSaveService(),
                _ => new World.PlayerSaveService()
            };
        }

        public static IWorldStateService CreateWorldStateService()
        {
            var serverFunctions = CreateServerFunctionService();
            return BackendType switch
            {
                "UGS" => new World.WorldStateService(serverFunctions),
                "Spring" => new World.WorldStateService(serverFunctions),
                "Hybrid" => new World.WorldStateService(serverFunctions),
                _ => new World.WorldStateService(serverFunctions)
            };
        }

        public static ITimeService CreateTimeService(Game.Core.Time.TimeConfig config = null)
        {
            var serverFunctions = CreateServerFunctionService();
            return BackendType switch
            {
                "UGS" => new Time.TimeService(config, serverFunctions),
                "Spring" => new Time.TimeService(config, serverFunctions),
                "Hybrid" => new Time.TimeService(config, serverFunctions),
                _ => new Time.TimeService(config, serverFunctions)
            };
        }

        public static IDeathService CreateDeathService()
        {
            return BackendType switch
            {
                "UGS" => new Character.DeathPersistenceService(CreateSaveService()),
                "Spring" => new Character.DeathPersistenceService(CreateSaveService()),
                "Hybrid" => new Character.DeathPersistenceService(CreateSaveService()),
                _ => new Character.DeathPersistenceService()
            };
        }

        public static ILobbyService CreateLobbyService()
        {
            var provider = _config != null ? _config.LobbyProvider : "UGS";
            return provider switch
            {
                "UGS" => new Network.UGSLobbyService(),
                "Custom" => new Network.UGSLobbyService(),
                _ => new Network.UGSLobbyService()
            };
        }

        public static IRelayService CreateRelayService()
        {
            var provider = _config != null ? _config.RelayProvider : "UGS";
            return provider switch
            {
                "UGS" => new Network.UGSRelayService(),
                "Custom" => new Network.UGSRelayService(),
                _ => new Network.UGSRelayService()
            };
        }

        public static INetworkTransport CreateNetworkTransport()
        {
            return new Network.UGSNetworkTransport();
        }

        public static IServerFunctionService CreateServerFunctionService()
        {
            return BackendType switch
            {
                "UGS" => new Network.UGSServerFunctionService(),
                "Spring" => new Network.UGSServerFunctionService(),
                "Hybrid" => new Network.UGSServerFunctionService(),
                _ => new Network.UGSServerFunctionService()
            };
        }

        public static SafeZoneService CreateSafeZoneService()
        {
            return new SafeZoneService();
        }

        // Tạm comment để compile. VI PHẠM LUẬT #2: Game.Services không được tham chiếu Game.PvE
        // (chiều phụ thuộc: PvE → Services → Core). AI Wave 1B di chuyển hàm này vào
        // PvEBootstrap.cs (nơi Game.PvE namespace), dùng ServiceProvider.CreateWorldStateService()
        // rồi new Game.PvE.Floor.FloorLoader(...) ở đó thay cho gọi ở đây.
        // public static Game.Core.Floor.IFloorLoader CreateFloorLoader(
        //     Game.Core.Floor.FloorDatabase database, bool accountIsDead = false)
        // {
        //     return new Game.PvE.Floor.FloorLoader(
        //         CreateWorldStateService(), database, accountIsDead);
        // }
    }
}
