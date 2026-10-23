using Game.Core.Audio;
using UnityEngine;

namespace Game.Services.Audio
{
    /// <summary>Lưu âm lượng nhạc/SFX cục bộ (docs/05: PlayerPrefs, không vào Player Data).</summary>
    public interface IAudioVolumeStore
    {
        float LoadMusic();
        float LoadSfx();
        void SaveMusic(float value);
        void SaveSfx(float value);
    }

    public sealed class PlayerPrefsAudioVolumeStore : IAudioVolumeStore
    {
        public float LoadMusic()
        {
            return AudioVolumes.Sanitize(PlayerPrefs.GetFloat(AudioVolumes.MusicKey, AudioVolumes.DefaultMusic), AudioVolumes.DefaultMusic);
        }

        public float LoadSfx()
        {
            return AudioVolumes.Sanitize(PlayerPrefs.GetFloat(AudioVolumes.SfxKey, AudioVolumes.DefaultSfx), AudioVolumes.DefaultSfx);
        }

        // Chỉ SetFloat (nằm trong bộ nhớ); AudioService gọi PlayerPrefs.Save() khi app ra nền hoặc bị hủy.
        public void SaveMusic(float value)
        {
            PlayerPrefs.SetFloat(AudioVolumes.MusicKey, AudioVolumes.Sanitize(value, AudioVolumes.DefaultMusic));
        }

        public void SaveSfx(float value)
        {
            PlayerPrefs.SetFloat(AudioVolumes.SfxKey, AudioVolumes.Sanitize(value, AudioVolumes.DefaultSfx));
        }
    }
}
