using UnityEngine;

namespace Game.Core.Audio
{
    /// <summary>Luật âm lượng: khóa lưu, giá trị mặc định, kẹp 0..1, tính âm lượng cuối. Thuần logic.</summary>
    public static class AudioVolumes
    {
        public const string MusicKey = "settings.audio.music";
        public const string SfxKey = "settings.audio.sfx";

        public const float DefaultMusic = 0.7f;
        public const float DefaultSfx = 0.9f;

        /// <summary>Kẹp về 0..1. NaN hoặc vô cực (dữ liệu lưu hỏng) → <paramref name="fallback"/>.</summary>
        public static float Sanitize(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return Mathf.Clamp01(fallback);
            return Mathf.Clamp01(value);
        }

        /// <summary>Âm lượng SFX cuối = volume của entry × hệ số lượt phát × âm lượng SFX người chơi chọn.</summary>
        public static float SfxFinal(float entryVolume, float volumeScale, float sfxVolume)
        {
            return Mathf.Clamp01(Mathf.Clamp01(entryVolume) * Mathf.Max(0f, volumeScale) * Mathf.Clamp01(sfxVolume));
        }

        /// <summary>Âm lượng nhạc cuối = volume của entry × âm lượng nhạc người chơi chọn × trọng số crossfade (0..1).</summary>
        public static float MusicFinal(float entryVolume, float musicVolume, float fadeWeight)
        {
            return Mathf.Clamp01(Mathf.Clamp01(entryVolume) * Mathf.Clamp01(musicVolume) * Mathf.Clamp01(fadeWeight));
        }
    }
}
