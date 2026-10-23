using Game.Core.Settings;
using UnityEngine;

namespace Game.Services.Settings
{
    /// <summary>Cài đặt người chơi lưu bằng PlayerPrefs (cục bộ, không đồng bộ lên cloud).</summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public bool TryGetFloat(string key, out float value)
        {
            value = 0f;
            if (!PlayerPrefs.HasKey(key)) return false;
            value = PlayerPrefs.GetFloat(key, 0f);
            return true;
        }

        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);

        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            if (!PlayerPrefs.HasKey(key)) return false;
            value = PlayerPrefs.GetInt(key, 0);
            return true;
        }

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);

        public void Save() => PlayerPrefs.Save();
    }
}
