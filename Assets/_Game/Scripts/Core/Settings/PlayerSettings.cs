using System;

namespace Game.Core.Settings
{
    public enum JoystickMode
    {
        Fixed = 0,
        Floating = 1
    }

    /// <summary>Cài đặt người chơi (P9.6). Lưu local qua <see cref="ISettingsStore"/>, KHÔNG vào Player Data / cloud.</summary>
    public readonly struct PlayerSettings : IEquatable<PlayerSettings>
    {
        public readonly float MusicVolume;
        public readonly float SfxVolume;
        public readonly bool HapticsEnabled;
        public readonly JoystickMode Joystick;
        public readonly float DeadZone;
        public readonly float Sensitivity;

        public PlayerSettings(float musicVolume, float sfxVolume, bool hapticsEnabled, JoystickMode joystick, float deadZone, float sensitivity)
        {
            MusicVolume = musicVolume;
            SfxVolume = sfxVolume;
            HapticsEnabled = hapticsEnabled;
            Joystick = joystick;
            DeadZone = deadZone;
            Sensitivity = sensitivity;
        }

        public bool Equals(PlayerSettings o) =>
            MusicVolume == o.MusicVolume && SfxVolume == o.SfxVolume && HapticsEnabled == o.HapticsEnabled &&
            Joystick == o.Joystick && DeadZone == o.DeadZone && Sensitivity == o.Sensitivity;

        public override bool Equals(object obj) => obj is PlayerSettings o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(MusicVolume, SfxVolume, HapticsEnabled, Joystick, DeadZone, Sensitivity);
    }
}
