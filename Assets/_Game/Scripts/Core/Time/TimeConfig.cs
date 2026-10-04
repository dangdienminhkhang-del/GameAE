using UnityEngine;

namespace Game.Core.Time
{
    /// <summary>
    /// Cấu hình chu kỳ ngày/đêm (docs/08 §2.2). Balance nằm ở đây, không hard-code trong code.
    /// Khuyến nghị: 1 ngày game = 20 phút thực → realSecondsPerGameDay = 1200.
    /// </summary>
    [CreateAssetMenu(fileName = "TimeConfig", menuName = "Game/Core/Time Config")]
    public class TimeConfig : ScriptableObject
    {
        [Header("Chu kỳ")]
        [Tooltip("Số giây THỰC cho một ngày game (1200 = 20 phút). Server rate được ưu tiên nếu có snapshot.")]
        [SerializeField] private float realSecondsPerGameDay = 1200f;

        [Tooltip("Giờ game lúc bắt đầu (0-24). Chỉ dùng khi server chưa có snapshot (bootstrap).")]
        [SerializeField] private float startHour = 8f;

        [SerializeField] private bool dayNightCycleEnabled = true;

        [Header("Authoritative Sync (docs/08 §3)")]
        [Tooltip("Số giây giữa hai lần resync tự động (mặc định 300 = 5 phút).")]
        [SerializeField] private int resyncIntervalSeconds = 300;

        [Tooltip("Snapshot cũ hơn giá trị này (giây) coi là stale (mặc định 900 = 15 phút).")]
        [SerializeField] private int maxSnapshotAgeSeconds = 900;

        [Tooltip("Độ lệch giờ (giờ game) lớn hơn giá trị này thì snap ngay, không smooth correction.")]
        [SerializeField] private float materialDriftGameHours = 0.25f;

        [Header("Màu ánh sáng theo pha (URP 2D)")]
        [SerializeField] private Color nightColor = new Color(0.25f, 0.3f, 0.55f, 1f);
        [SerializeField] private Color dawnColor = new Color(0.85f, 0.6f, 0.5f, 1f);
        [SerializeField] private Color dayColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color eveningColor = new Color(0.8f, 0.55f, 0.45f, 1f);

        public float RealSecondsPerGameDay => realSecondsPerGameDay < 1f ? 1f : realSecondsPerGameDay;
        public float StartHour => Mathf.Repeat(startHour, 24f);
        public bool DayNightCycleEnabled => dayNightCycleEnabled;
        public int ResyncIntervalSeconds => resyncIntervalSeconds < 10 ? 10 : resyncIntervalSeconds;
        public int MaxSnapshotAgeSeconds => maxSnapshotAgeSeconds < 0 ? 0 : maxSnapshotAgeSeconds;
        public float MaterialDriftGameHours => materialDriftGameHours < 0.01f ? 0.01f : materialDriftGameHours;

        /// <summary>Giờ game tiến thêm mỗi giây thực.</summary>
        public double GameHoursPerRealSecond => 24.0 / RealSecondsPerGameDay;

        public Color ColorForPhase(TimeOfDay phase)
        {
            switch (phase)
            {
                case TimeOfDay.Night: return nightColor;
                case TimeOfDay.Dawn: return dawnColor;
                case TimeOfDay.Evening: return eveningColor;
                default: return dayColor;
            }
        }

        private void Reset()
        {
            realSecondsPerGameDay = 1200f;
            startHour = 8f;
            dayNightCycleEnabled = true;
            resyncIntervalSeconds = 300;
            maxSnapshotAgeSeconds = 900;
            materialDriftGameHours = 0.25f;
        }
    }
}
