using System;
using Game.Core.Audio;

namespace Game.Core.Settings
{
    /// <summary>
    /// Luật cho cài đặt người chơi (P9.6) — thuần C#, test được: giá trị mặc định, kẹp khoảng, bỏ giá trị rác.
    /// </summary>
    public static class SettingsRules
    {
        public const bool DefaultHaptics = true;
        public const JoystickMode DefaultJoystick = JoystickMode.Fixed;

        /// <summary>Dead zone nhỏ nhất cho phép. PHẢI lớn hơn ngưỡng lọc nhiễu của FacingTracker (0.02), xem mục 3b của P9.6.</summary>
        public const float MinDeadZone = 0.10f;
        public const float DefaultDeadZone = 0.15f;
        public const float MaxDeadZone = 0.35f;

        /// <summary>Độ nhạy: càng cao thì kéo ít hơn vẫn tới tốc độ tối đa (handleRange hiệu dụng = handleRange / độ nhạy).</summary>
        public const float MinSensitivity = 0.8f;
        public const float DefaultSensitivity = 1f;
        public const float MaxSensitivity = 1.3f;

        public static PlayerSettings Defaults => new PlayerSettings(
            AudioVolumes.DefaultMusic, AudioVolumes.DefaultSfx, DefaultHaptics, DefaultJoystick, DefaultDeadZone, DefaultSensitivity);

        /// <summary>Kẹp vào khoảng [min, max]; NaN/vô cực thì trả <paramref name="fallback"/>.</summary>
        public static float ClampOrDefault(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            return Math.Max(min, Math.Min(max, value));
        }

        public static JoystickMode ParseJoystick(int raw)
        {
            return raw == (int)JoystickMode.Floating ? JoystickMode.Floating
                 : raw == (int)JoystickMode.Fixed ? JoystickMode.Fixed
                 : DefaultJoystick;
        }

        /// <summary>Đưa mọi trường về khoảng hợp lệ (dùng sau khi đọc từ kho lưu hoặc từ UI).</summary>
        public static PlayerSettings Sanitize(PlayerSettings s)
        {
            return new PlayerSettings(
                ClampOrDefault(s.MusicVolume, 0f, 1f, AudioVolumes.DefaultMusic),
                ClampOrDefault(s.SfxVolume, 0f, 1f, AudioVolumes.DefaultSfx),
                s.HapticsEnabled,
                ParseJoystick((int)s.Joystick),
                ClampOrDefault(s.DeadZone, MinDeadZone, MaxDeadZone, DefaultDeadZone),
                ClampOrDefault(s.Sensitivity, MinSensitivity, MaxSensitivity, DefaultSensitivity));
        }
    }
}
