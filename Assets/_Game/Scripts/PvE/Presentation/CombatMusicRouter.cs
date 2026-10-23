using Game.Core.Audio;
using Game.PvE.Boss;
using Game.Services;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Chọn nhạc nền cho scene gameplay (docs/05 Phase 9.5): nhạc gameplay khi vào scene; có boss vào trận thì chuyển nhạc boss,
    /// boss rời trận hoặc chết thì quay về nhạc gameplay (crossfade). Luật chọn nằm ở <see cref="MusicRules"/>.
    /// Chỉ nghe BossController.EngagedChanged, không đụng logic boss.
    /// </summary>
    public sealed class CombatMusicRouter : MonoBehaviour
    {
        [SerializeField] private string gameplayMusicId = AudioIds.MusicGameplay;
        [SerializeField] private string bossMusicId = AudioIds.MusicBoss;
        [SerializeField] private float fadeSeconds = 1f;
        [Tooltip("Các boss cần theo dõi. Để trống = tự tìm mọi BossController trong scene (kể cả đang tắt) lúc Start.")]
        [SerializeField] private BossController[] bosses;

        private int _engagedCount;
        private bool _started;
        private readonly System.Collections.Generic.HashSet<BossController> _engaged =
            new System.Collections.Generic.HashSet<BossController>();

        private void Start()
        {
            if (bosses == null || bosses.Length == 0)
            {
                bosses = FindObjectsByType<BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }

            _started = true;
            Subscribe();
            PlayCurrent();
        }

        private void OnEnable()
        {
            if (_started) Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (bosses == null) return;
            foreach (BossController boss in bosses)
            {
                if (boss == null) continue;
                boss.EngagedChanged -= HandleEngagedChanged; // tránh đăng ký hai lần
                boss.EngagedChanged += HandleEngagedChanged;
            }
        }

        private void Unsubscribe()
        {
            if (bosses == null) return;
            foreach (BossController boss in bosses)
            {
                if (boss != null) boss.EngagedChanged -= HandleEngagedChanged;
            }
        }

        private void HandleEngagedChanged(BossController boss, bool engaged)
        {
            if (engaged) _engaged.Add(boss); else _engaged.Remove(boss);
            _engagedCount = _engaged.Count;
            PlayCurrent();
        }

        private void PlayCurrent()
        {
            string id = MusicRules.ChooseTrack(_engagedCount, gameplayMusicId, bossMusicId);
            ServiceProvider.GetAudioService().PlayMusic(id, fadeSeconds);
        }
    }
}
