using System;
using Game.Core.Settings;
using Game.Services.Audio;
using Game.Services.Settings;
using UnityEngine;

namespace Game.UI.Settings
{
    /// <summary>
    /// Giữ cài đặt người chơi đang có hiệu lực (P9.6). Joystick, rung, panel Settings đều đọc từ đây
    /// và nghe <see cref="Changed"/> để đổi ngay, không phải khởi động lại. Chỉ nối dây: luật nằm ở Core/Settings.
    /// Lưu local bằng PlayerPrefs qua ISettingsStore, KHÔNG vào Player Data / cloud.
    /// </summary>
    public static class SettingsRuntime
    {
        private static ISettingsStore s_store;
        private static PlayerSettings s_current;
        private static bool s_loaded;

        /// <summary>Báo khi cài đặt đã đổi (đã kẹp về khoảng hợp lệ và đã lưu).</summary>
        public static event Action<PlayerSettings> Changed;

        public static PlayerSettings Current
        {
            get
            {
                EnsureLoaded();
                return s_current;
            }
        }

        /// <summary>Cho test hoặc nơi khác thay kho lưu (mặc định PlayerPrefs). Truyền null để quay về mặc định.</summary>
        public static void UseStore(ISettingsStore store)
        {
            s_store = store;
            s_loaded = false;
        }

        /// <summary>Kẹp, lưu, đẩy âm lượng sang AudioService rồi báo cho người nghe.</summary>
        public static void Apply(PlayerSettings settings)
        {
            EnsureLoaded();
            s_current = SettingsRules.Sanitize(settings);
            SettingsRepository.Save(s_store, s_current);
            PushVolumes(s_current);
            Changed?.Invoke(s_current);
        }

        /// <summary>Đẩy âm lượng đã lưu sang AudioService (gọi khi vào scene có AudioService mới).</summary>
        public static void ApplyVolumesToAudio()
        {
            EnsureLoaded();
            PushVolumes(s_current);
        }

        private static void EnsureLoaded()
        {
            if (s_loaded) return;
            if (s_store == null) s_store = new PlayerPrefsSettingsStore();
            s_current = SettingsRepository.Load(s_store);
            s_loaded = true;
        }

        private static void PushVolumes(PlayerSettings s)
        {
            var audio = AudioServiceLocator.Current;
            if (audio == null) return;
            audio.MusicVolume = s.MusicVolume;
            audio.SfxVolume = s.SfxVolume;
        }

        // Static: phải reset khi vào Play (Enter Play Mode Options có thể tắt Domain Reload).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            s_store = null;
            s_loaded = false;
            Changed = null;
        }
    }
}
