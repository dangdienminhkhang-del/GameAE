using System;

namespace Game.UI.Loading
{
    /// <summary>
    /// Đổi tiến độ thô của AsyncOperation (chạy 0 → 0.9 rồi mới tới 1 khi kích hoạt) thành thanh 0 → 1:
    /// đơn điệu tăng (không bao giờ lùi), và có thời gian hiện tối thiểu để overlay không chớp. Thuần logic.
    /// </summary>
    public sealed class LoadingProgressMapper
    {
        /// <summary>AsyncOperation.progress dừng ở 0.9 cho tới khi cho phép kích hoạt.</summary>
        public const float RawReadyThreshold = 0.9f;
        public const float DefaultMinShowSeconds = 0.4f;

        private readonly float _minShowSeconds;
        private float _last;

        public LoadingProgressMapper(float minShowSeconds = DefaultMinShowSeconds)
        {
            _minShowSeconds = minShowSeconds < 0f ? 0f : minShowSeconds;
        }

        public float MinShowSeconds => _minShowSeconds;

        /// <summary>Tiến độ hiển thị (0..1, đơn điệu) tại thời điểm <paramref name="elapsedSeconds"/> kể từ lúc bắt đầu tải.</summary>
        public float Map(float rawProgress, float elapsedSeconds)
        {
            if (float.IsNaN(rawProgress)) rawProgress = 0f;
            float mapped = Clamp01(rawProgress / RawReadyThreshold);
            float timeCap = _minShowSeconds <= 0f ? 1f : Clamp01(elapsedSeconds / _minShowSeconds);
            float value = Math.Min(mapped, timeCap);
            if (value > _last) _last = value;
            return _last;
        }

        /// <summary>Xong khi tải đã tới ngưỡng sẵn sàng VÀ đã hiện đủ thời gian tối thiểu.</summary>
        public bool IsComplete(float rawProgress, float elapsedSeconds)
        {
            return !float.IsNaN(rawProgress) && rawProgress >= RawReadyThreshold && elapsedSeconds >= _minShowSeconds;
        }

        public void Reset() => _last = 0f;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
