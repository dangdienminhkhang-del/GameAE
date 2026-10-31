using System.Collections.Generic;
using Game.Core.Audio;
using UnityEngine;

namespace Game.Services.Audio
{
    /// <summary>
    /// Phát âm cho game (docs/05 Phase 9.5): pool AudioSource cho SFX, hai kênh nhạc để crossfade,
    /// chống spam theo id, âm lượng lưu PlayerPrefs, tạm dừng khi app ra nền.
    /// Đặt MỘT object trong scene khởi đầu (Boot); tự giữ qua các scene, bản thứ hai (nếu có) tự hủy.
    /// Id thiếu hoặc không có clip: im lặng và chỉ log một lần cho mỗi id — game không bao giờ crash vì thiếu âm thanh.
    /// Lưu ý tên: Game.Services.Time là namespace nên mọi chỗ dùng thời gian của Unity phải viết UnityEngine.Time.
    /// </summary>
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        private sealed class MusicChannel
        {
            public AudioSource Source;
            public float Weight;      // trọng số crossfade hiện tại 0..1
            public float Target;      // 1 = đang vào, 0 = đang ra
            public float RatePerSecond;
            public float EntryVolume;
            public string Id;
        }

        [SerializeField] private AudioCatalog catalog;
        [Tooltip("Số AudioSource trong pool SFX (số tiếng kêu đồng thời tối đa).")]
        [SerializeField] private int sfxVoices = SfxThrottleRules.DefaultMaxVoices;
        [Tooltip("Giữ object này qua các scene (bỏ chọn nếu để dưới một object đã DontDestroyOnLoad).")]
        [SerializeField] private bool persistAcrossScenes = true;

        private AudioSource[] _sfxSources;
        private readonly MusicChannel[] _music = new MusicChannel[2];
        private int _activeMusic = -1;
        private string _musicId;

        private readonly SfxThrottleTracker _throttle = new SfxThrottleTracker();
        private readonly HashSet<string> _warnedIds = new HashSet<string>();
        private IAudioVolumeStore _store;
        private float _musicVolume = AudioVolumes.DefaultMusic;
        private float _sfxVolume = AudioVolumes.DefaultSfx;
        private bool _backgrounded;
        private bool _initialized;

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = AudioVolumes.Sanitize(value, AudioVolumes.DefaultMusic);
                _store?.SaveMusic(_musicVolume);
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = AudioVolumes.Sanitize(value, AudioVolumes.DefaultSfx);
                _store?.SaveSfx(_sfxVolume);
            }
        }

        public AudioCatalog Catalog => catalog;

        /// <summary>Gán catalog bằng code (Bootstrap/test). Bình thường gán trong Inspector.</summary>
        public void SetCatalog(AudioCatalog value)
        {
            catalog = value;
        }

        private void Awake()
        {
            if (!AudioServiceLocator.TryClaim(this))
            {
                // Đã có AudioService khác (ví dụ quay lại scene Boot): bỏ bản này để không nhân đôi nhạc.
                Destroy(gameObject);
                return;
            }

            Initialize();
            if (persistAcrossScenes && transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        private void Initialize()
        {
            if (_initialized) return;

            _store = new PlayerPrefsAudioVolumeStore();
            _musicVolume = _store.LoadMusic();
            _sfxVolume = _store.LoadSfx();

            _sfxSources = new AudioSource[Mathf.Max(1, sfxVoices)];
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                _sfxSources[i] = CreateSource(false);
            }

            for (int i = 0; i < _music.Length; i++)
            {
                _music[i] = new MusicChannel { Source = CreateSource(true) };
            }

            _initialized = true;
        }

        private AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D: không dùng âm thanh 3D
            return source;
        }

        private void OnDestroy()
        {
            AudioServiceLocator.Release(this);
            if (_initialized) PlayerPrefs.Save();
        }

        // ── IAudioService ───────────────────────────────────────────────────

        public void PlaySfx(string id, float volumeScale = 1f)
        {
            if (!_initialized || _backgrounded || string.IsNullOrEmpty(id)) return;
            if (!TryGetEntry(id, out AudioCatalog.Entry entry)) return;

            AudioClip clip = PickClip(entry);
            if (clip == null)
            {
                WarnOnce(id, $"[AudioService] '{id}' chưa có clip trong catalog — bỏ qua (im lặng).");
                return;
            }

            double now = UnityEngine.Time.unscaledTimeAsDouble;
            if (_throttle.IsTooSoon(id, now, entry.minIntervalSeconds)) return;

            AudioSource voice = FindFreeVoice();
            if (voice == null) return; // hết voice: bỏ lượt phát này thay vì cắt tiếng đang kêu

            _throttle.Record(id, now);
            voice.clip = clip;
            voice.volume = AudioVolumes.SfxFinal(entry.volume, volumeScale, _sfxVolume);
            voice.pitch = 1f + (entry.pitchJitter > 0f ? UnityEngine.Random.Range(-entry.pitchJitter, entry.pitchJitter) : 0f);
            voice.Play();
        }

        public void PlayMusic(string id, float fadeSeconds = 1f)
        {
            if (!_initialized || string.IsNullOrEmpty(id)) return;
            if (_musicId == id && _activeMusic >= 0 && _music[_activeMusic].Target > 0f) return;

            AudioClip clip = null;
            AudioCatalog.Entry entry = null;
            if (TryGetEntry(id, out entry))
            {
                clip = entry.clips != null && entry.clips.Length > 0 ? entry.clips[0] : null;
                if (clip == null) WarnOnce(id, $"[AudioService] '{id}' chưa có clip trong catalog — bỏ qua (im lặng).");
            }

            // Bài cũ luôn được fade ra; bài mới (nếu có clip) fade vào ở kênh còn lại.
            if (_activeMusic >= 0) BeginFade(_music[_activeMusic], 0f, fadeSeconds);
            _musicId = id;

            if (clip == null)
            {
                _activeMusic = -1;
                return;
            }

            int next = _activeMusic < 0 ? 0 : 1 - _activeMusic;
            MusicChannel channel = _music[next];
            channel.Source.Stop();
            channel.Source.clip = clip;
            channel.Source.loop = true;
            channel.EntryVolume = entry.volume;
            channel.Id = id;
            channel.Weight = fadeSeconds > 0f ? 0f : 1f;
            channel.Source.volume = AudioVolumes.MusicFinal(channel.EntryVolume, _musicVolume, channel.Weight);
            BeginFade(channel, 1f, fadeSeconds);
            if (!_backgrounded) channel.Source.Play();
            _activeMusic = next;
        }

        public void StopMusic(float fadeSeconds = 1f)
        {
            if (!_initialized) return;
            if (_activeMusic >= 0) BeginFade(_music[_activeMusic], 0f, fadeSeconds);
            _activeMusic = -1;
            _musicId = null;
        }

        // ── Nội bộ ──────────────────────────────────────────────────────────

        private static void BeginFade(MusicChannel channel, float target, float fadeSeconds)
        {
            channel.Target = target;
            if (fadeSeconds <= 0f)
            {
                channel.Weight = target;
                channel.RatePerSecond = 1f;
            }
            else
            {
                channel.RatePerSecond = 1f / fadeSeconds;
            }
        }

        private void Update()
        {
            if (!_initialized) return;

            // Fade chạy bằng thời gian THẬT: dừng hình/pause (timeScale thấp hoặc 0) không làm nhạc đứng giữa chừng.
            float dt = UnityEngine.Time.unscaledDeltaTime;
            for (int i = 0; i < _music.Length; i++)
            {
                MusicChannel channel = _music[i];
                if (channel == null || channel.Source.clip == null) continue;

                if (!Mathf.Approximately(channel.Weight, channel.Target))
                {
                    channel.Weight = Mathf.MoveTowards(channel.Weight, channel.Target, channel.RatePerSecond * dt);
                }

                channel.Source.volume = AudioVolumes.MusicFinal(channel.EntryVolume, _musicVolume, channel.Weight);

                if (channel.Target <= 0f && channel.Weight <= 0f)
                {
                    channel.Source.Stop();
                    channel.Source.clip = null;
                    channel.Id = null;
                }
            }
        }

        private bool TryGetEntry(string id, out AudioCatalog.Entry entry)
        {
            entry = null;
            if (catalog == null)
            {
                WarnOnce("<catalog>", "[AudioService] Chưa gán AudioCatalog — mọi âm thanh sẽ im lặng.");
                return false;
            }
            if (!catalog.TryGet(id, out entry))
            {
                WarnOnce(id, $"[AudioService] Id âm thanh '{id}' không có trong AudioCatalog — bỏ qua (im lặng).");
                return false;
            }
            return true;
        }

        private static AudioClip PickClip(AudioCatalog.Entry entry)
        {
            AudioClip[] clips = entry.clips;
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        private AudioSource FindFreeVoice()
        {
            int active = 0;
            AudioSource free = null;
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                if (_sfxSources[i].isPlaying) active++;
                else if (free == null) free = _sfxSources[i];
            }
            return SfxThrottleRules.HasFreeVoice(active, _sfxSources.Length) ? free : null;
        }

        private void WarnOnce(string key, string message)
        {
            if (_warnedIds.Add(key)) Debug.LogWarning(message, this);
        }

        // ── Ra nền / vào lại app ────────────────────────────────────────────

        private void OnApplicationFocus(bool hasFocus)
        {
            SetBackgrounded(!hasFocus);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _initialized) PlayerPrefs.Save();
            SetBackgrounded(paused);
        }

        private void SetBackgrounded(bool value)
        {
            if (!_initialized || _backgrounded == value) return;
            _backgrounded = value;

            // Khi thoát Play Mode / đóng scene, AudioSource có thể đã bị Unity huỷ trước khi
            // OnApplicationFocus được gọi → bỏ qua nguồn đã mất để không ném NullReference.
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                if (_sfxSources[i] == null) continue;
                if (value) _sfxSources[i].Pause(); else _sfxSources[i].UnPause();
            }
            for (int i = 0; i < _music.Length; i++)
            {
                if (_music[i] == null || _music[i].Source == null || _music[i].Source.clip == null) continue;
                if (value) _music[i].Source.Pause(); else _music[i].Source.UnPause();
            }
        }
    }
}
