using System.Collections.Generic;
using Game.Core.Events;
using Game.PvE.Boss;
using Game.Services;
using UnityEngine;

namespace Game.PvE.Zones
{
    /// <summary>Luật chọn nhạc theo vùng (thuần logic).</summary>
    public static class ZoneMusicRules
    {
        /// <summary>
        /// Trả ID nhạc cần phát, hoặc null nếu KHÔNG đổi nhạc: boss đang trong trận thì nhạc boss (P9.5) được ưu tiên,
        /// vùng không có musicId thì giữ nhạc hiện tại.
        /// </summary>
        public static string Choose(string zoneMusicId, int engagedBossCount)
        {
            if (engagedBossCount > 0) return null;
            return string.IsNullOrEmpty(zoneMusicId) ? null : zoneMusicId;
        }
    }

    /// <summary>
    /// Đổi nhạc nền theo vùng người chơi vừa vào (crossfade qua IAudioService, Phase 9.7a).
    /// Việc phát được dời tới LateUpdate để luôn chạy SAU CombatMusicRouter (P9.5) trong cùng khung hình:
    /// vào game → nhạc vùng thắng nhạc gameplay mặc định; boss rời trận → quay về nhạc vùng hiện tại.
    /// </summary>
    public sealed class ZoneMusicRouter : MonoBehaviour
    {
        [SerializeField] private float fadeSeconds = 1f;
        [Tooltip("Các boss cần theo dõi. Để trống = tự tìm mọi BossController trong scene (kể cả đang tắt) lúc Start.")]
        [SerializeField] private BossController[] bosses;

        private readonly HashSet<BossController> _engaged = new HashSet<BossController>();
        private string _zoneMusicId;
        private bool _dirty;
        private bool _started;

        private void Start()
        {
            if (bosses == null || bosses.Length == 0)
            {
                bosses = FindObjectsByType<BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }
            _started = true;
            SubscribeBosses();
        }

        private void OnEnable()
        {
            GameEvents.OnZoneEntered += HandleZoneEntered;
            if (_started) SubscribeBosses();
        }

        private void OnDisable()
        {
            GameEvents.OnZoneEntered -= HandleZoneEntered;
            UnsubscribeBosses();
        }

        private void SubscribeBosses()
        {
            if (bosses == null) return;
            foreach (BossController boss in bosses)
            {
                if (boss == null) continue;
                boss.EngagedChanged -= HandleEngagedChanged; // tránh đăng ký hai lần
                boss.EngagedChanged += HandleEngagedChanged;
            }
        }

        private void UnsubscribeBosses()
        {
            if (bosses == null) return;
            foreach (BossController boss in bosses)
            {
                if (boss != null) boss.EngagedChanged -= HandleEngagedChanged;
            }
        }

        private void HandleZoneEntered(object sender, ZoneEnteredArgs args)
        {
            if (args == null) return;
            ZoneVolume zone = ZoneVolume.Find(args.ZoneId);
            _zoneMusicId = zone != null ? zone.MusicId : null;
            _dirty = true;
        }

        private void HandleEngagedChanged(BossController boss, bool engaged)
        {
            if (engaged) _engaged.Add(boss); else _engaged.Remove(boss);
            // Boss rời trận: CombatMusicRouter đưa về nhạc gameplay; dời tới LateUpdate để trả lại nhạc vùng.
            if (_engaged.Count == 0) _dirty = true;
        }

        private void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;

            string id = ZoneMusicRules.Choose(_zoneMusicId, _engaged.Count);
            if (id != null) ServiceProvider.GetAudioService().PlayMusic(id, fadeSeconds);
        }
    }
}
