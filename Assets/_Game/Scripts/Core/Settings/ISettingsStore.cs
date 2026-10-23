namespace Game.Core.Settings
{
    /// <summary>Kho lưu cài đặt cục bộ (mặc định PlayerPrefs, test dùng bản trong bộ nhớ). Khóa có tiền tố "settings.".</summary>
    public interface ISettingsStore
    {
        bool TryGetFloat(string key, out float value);
        void SetFloat(string key, float value);
        bool TryGetInt(string key, out int value);
        void SetInt(string key, int value);
        void Save();
    }

    /// <summary>
    /// Đọc/ghi <see cref="PlayerSettings"/> qua <see cref="ISettingsStore"/>. Thiếu khóa hoặc giá trị rác → mặc định (SettingsRules).
    /// Khóa âm lượng dùng chung với AudioService (<see cref="Game.Core.Audio.AudioVolumes"/>) để hai nơi không lệch nhau.
    /// </summary>
    public static class SettingsRepository
    {
        public const string HapticsKey = "settings.haptics";
        public const string JoystickKey = "settings.joystick_mode";
        public const string DeadZoneKey = "settings.dead_zone";
        public const string SensitivityKey = "settings.sensitivity";

        public static PlayerSettings Load(ISettingsStore store)
        {
            PlayerSettings d = SettingsRules.Defaults;
            if (store == null) return d;

            float music = store.TryGetFloat(Game.Core.Audio.AudioVolumes.MusicKey, out float m) ? m : d.MusicVolume;
            float sfx = store.TryGetFloat(Game.Core.Audio.AudioVolumes.SfxKey, out float s) ? s : d.SfxVolume;
            bool haptics = store.TryGetInt(HapticsKey, out int h) ? h != 0 : d.HapticsEnabled;
            JoystickMode joystick = store.TryGetInt(JoystickKey, out int j) ? SettingsRules.ParseJoystick(j) : d.Joystick;
            float dead = store.TryGetFloat(DeadZoneKey, out float dz) ? dz : d.DeadZone;
            float sens = store.TryGetFloat(SensitivityKey, out float se) ? se : d.Sensitivity;

            return SettingsRules.Sanitize(new PlayerSettings(music, sfx, haptics, joystick, dead, sens));
        }

        public static void Save(ISettingsStore store, PlayerSettings settings)
        {
            if (store == null) return;
            PlayerSettings s = SettingsRules.Sanitize(settings);
            store.SetFloat(Game.Core.Audio.AudioVolumes.MusicKey, s.MusicVolume);
            store.SetFloat(Game.Core.Audio.AudioVolumes.SfxKey, s.SfxVolume);
            store.SetInt(HapticsKey, s.HapticsEnabled ? 1 : 0);
            store.SetInt(JoystickKey, (int)s.Joystick);
            store.SetFloat(DeadZoneKey, s.DeadZone);
            store.SetFloat(SensitivityKey, s.Sensitivity);
            store.Save();
        }
    }
}
