using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;

namespace Game.PvE.Zones
{
    /// <summary>
    /// Theo dõi người chơi đang ở vùng nào và phát GameEvents.ZoneEntered khi đổi vùng (Phase 9.7a).
    /// Kiểm khoảng 10 lần/giây bằng Collider2D.OverlapPoint (không cấp phát, không Find mỗi frame).
    /// Luật chọn vùng và chống chớp ở biên nằm ở <see cref="ZoneTrackerLogic"/> (thuần logic, có test).
    /// Đặt MỘT component này ở ngoài nội dung tầng (luôn bật).
    /// </summary>
    public sealed class ZoneTracker : MonoBehaviour
    {
        private static int s_immediateVersion;

        [Tooltip("Người chơi. Để trống = tự tìm theo tag Player.")]
        [SerializeField] private Transform player;
        [Tooltip("Khoảng cách giữa hai lần kiểm (giây, thời gian thật).")]
        [SerializeField] private float tickSeconds = 0.1f;
        [Tooltip("Sang vùng khác phải ở đó đủ chừng này nhịp liên tiếp mới đổi (chống chớp ở biên).")]
        [SerializeField] private int dwellTicks = ZoneTrackerLogic.DefaultDwellTicks;

        private ZoneTrackerLogic _logic;
        private readonly List<ZoneCandidate> _candidates = new List<ZoneCandidate>(8);
        private float _nextTickAt;
        private float _nextPlayerScanAt;
        private int _seenImmediateVersion;

        /// <summary>ID vùng người chơi đang ở; null nếu chưa xác định.</summary>
        public string CurrentZoneId => _logic != null ? _logic.Current : null;

        /// <summary>Người chơi có đang ở vùng này không (đã qua trễ chống chớp). P9.7b dùng để kích hoạt boss theo phòng.</summary>
        public bool IsInZone(string zoneId)
        {
            return !string.IsNullOrEmpty(zoneId) && string.Equals(CurrentZoneId, zoneId, System.StringComparison.Ordinal);
        }

        /// <summary>Yêu cầu mọi tracker đánh giá lại ngay và phát ZoneEntered dù vùng không đổi (vừa vào tầng).</summary>
        public static void RequestImmediateEvaluation()
        {
            s_immediateVersion++;
        }

        private void Awake()
        {
            _logic = new ZoneTrackerLogic(dwellTicks);
            _seenImmediateVersion = s_immediateVersion;
        }

        private void Update()
        {
            float now = UnityEngine.Time.unscaledTime;
            if (now < _nextTickAt) return;
            _nextTickAt = now + Mathf.Max(0.02f, tickSeconds);

            if (player == null)
            {
                if (now < _nextPlayerScanAt) return;
                _nextPlayerScanAt = now + 1f;
                GameObject found = GameObject.FindGameObjectWithTag("Player");
                if (found == null) return;
                player = found.transform;
            }

            Evaluate(player.position);
        }

        private void Evaluate(Vector2 position)
        {
            bool immediate = false;
            if (_seenImmediateVersion != s_immediateVersion)
            {
                _seenImmediateVersion = s_immediateVersion;
                immediate = true;
            }

            // Vùng cũ đã biến mất (tầng bị tắt): quên đi để vùng đầu tiên của tầng mới được phát ngay.
            if (_logic.Current != null && ZoneVolume.Find(_logic.Current) == null) _logic.Reset();

            _candidates.Clear();
            IReadOnlyList<ZoneVolume> all = ZoneVolume.All;
            for (int i = 0; i < all.Count; i++)
            {
                ZoneVolume volume = all[i];
                if (volume == null || !volume.isActiveAndEnabled || !volume.Contains(position)) continue;
                _candidates.Add(new ZoneCandidate(volume.ZoneId, volume.Priority, volume.Area));
            }

            if (!_logic.Update(_candidates, immediate, out string enteredId)) return;

            ZoneVolume entered = ZoneVolume.Find(enteredId);
            GameEvents.RaiseZoneEntered(this, new ZoneEnteredArgs
            {
                ZoneId = enteredId,
                DisplayName = entered != null ? entered.DisplayName : enteredId
            });
        }

        // Enter Play Mode Options có thể tắt Domain Reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_immediateVersion = 0;
        }
    }
}
