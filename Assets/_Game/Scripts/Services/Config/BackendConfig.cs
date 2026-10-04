using UnityEngine;

namespace Game.Services.Config
{
    [CreateAssetMenu(fileName = "BackendConfig", menuName = "Game/Config/Backend Config")]
    public class BackendConfig : ScriptableObject
    {
        [SerializeField] private string backendType = "UGS";
        [SerializeField] private string lobbyProvider = "UGS";
        [SerializeField] private string relayProvider = "UGS";
        [SerializeField] private string saveProvider = "UGS";
        [SerializeField] private string worldStateProvider = "UGS";
        [SerializeField] private string timeProvider = "UGS";
        [SerializeField] private string deathProvider = "UGS";

        public string BackendType => backendType;
        public string LobbyProvider => lobbyProvider;
        public string RelayProvider => relayProvider;
        public string SaveProvider => saveProvider;
        public string WorldStateProvider => worldStateProvider;
        public string TimeProvider => timeProvider;
        public string DeathProvider => deathProvider;
    }
}
