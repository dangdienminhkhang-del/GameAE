namespace Game.Core.Audio
{
    /// <summary>
    /// Dịch vụ phát âm (docs/05 Phase 9.5). Gameplay chỉ biết interface này, không biết AudioSource hay catalog.
    /// Lấy bản đang chạy qua ServiceProvider.GetAudioService() (chưa có thì là bản im lặng, không bao giờ null).
    /// </summary>
    public interface IAudioService
    {
        /// <summary>Phát một hiệu ứng theo id (xem <see cref="AudioIds"/>). Id lạ hoặc thiếu clip: im lặng, chỉ log một lần.</summary>
        void PlaySfx(string id, float volumeScale = 1f);

        /// <summary>Phát nhạc nền theo id, crossfade từ bài đang phát. Gọi lại đúng bài đang phát thì không làm gì.</summary>
        void PlayMusic(string id, float fadeSeconds = 1f);

        void StopMusic(float fadeSeconds = 1f);

        /// <summary>Âm lượng nhạc 0..1 (lưu local bằng PlayerPrefs). Màn Settings làm ở Phase 9.6.</summary>
        float MusicVolume { get; set; }

        /// <summary>Âm lượng SFX 0..1 (lưu local bằng PlayerPrefs).</summary>
        float SfxVolume { get; set; }
    }

    /// <summary>Bản im lặng: dùng khi chưa có AudioService trong scene, để gọi code âm thanh không cần kiểm null.</summary>
    public sealed class NullAudioService : IAudioService
    {
        public static readonly NullAudioService Instance = new NullAudioService();

        private NullAudioService() { }

        public void PlaySfx(string id, float volumeScale = 1f) { }
        public void PlayMusic(string id, float fadeSeconds = 1f) { }
        public void StopMusic(float fadeSeconds = 1f) { }
        public float MusicVolume { get; set; } = AudioVolumes.DefaultMusic;
        public float SfxVolume { get; set; } = AudioVolumes.DefaultSfx;
    }
}
