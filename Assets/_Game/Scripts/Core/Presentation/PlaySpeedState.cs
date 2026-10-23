using System;
using UnityEngine;

namespace Game.Core.Presentation
{
    /// <summary>
    /// ĐIỂM DUY NHẤT được ghi Time.timeScale trong toàn dự án (docs/05 Phase 9.5, test TimeScaleWriteScanTests canh luật này).
    /// timeScale = 0 nếu đang pause; = scale dừng hình nếu đang dừng hình; = 1 nếu bình thường. Pause luôn thắng dừng hình.
    /// Pause (Phase 9.6) dùng lại <see cref="PushPause"/>/<see cref="PopPause"/>.
    /// Bộ đếm dừng hình chạy bằng thời gian THẬT (unscaled) nên luôn tự hết, không bao giờ kẹt ở scale thấp.
    /// </summary>
    public sealed class PlaySpeedState
    {
        private readonly Action<float> _applyScale;
        private int _pauseCount;
        private float _hitStopRemaining;
        private float _hitStopScale = 1f;
        private float _appliedScale = float.NaN;

        /// <param name="applyScale">Nơi thật sự ghi tốc độ. Test truyền hàm giả; <see cref="Shared"/> ghi vào Unity.</param>
        public PlaySpeedState(Action<float> applyScale)
        {
            _applyScale = applyScale ?? throw new ArgumentNullException(nameof(applyScale));
        }

        public bool IsPaused => _pauseCount > 0;
        public bool IsHitStopActive => _pauseCount == 0 && _hitStopRemaining > 0f;
        public int PauseDepth => _pauseCount;
        public float CurrentScale => ComputeScale();

        public void PushPause()
        {
            _pauseCount++;
            Apply();
        }

        public void PopPause()
        {
            if (_pauseCount > 0) _pauseCount--;
            Apply();
        }

        /// <summary>Yêu cầu dừng hình <paramref name="seconds"/> giây thật ở tốc độ <paramref name="scale"/>. Bỏ qua khi đang pause.</summary>
        public void RequestHitStop(float seconds, float scale)
        {
            if (seconds <= 0f || _pauseCount > 0) return;

            scale = Mathf.Clamp01(scale);
            _hitStopScale = IsHitStopActive ? Mathf.Min(_hitStopScale, scale) : scale;
            _hitStopRemaining = Mathf.Max(_hitStopRemaining, seconds);
            Apply();
        }

        /// <summary>Gọi mỗi frame với thời gian THẬT (Time.unscaledDeltaTime).</summary>
        public void Tick(float unscaledDeltaSeconds)
        {
            if (_hitStopRemaining > 0f)
            {
                _hitStopRemaining -= Mathf.Max(0f, unscaledDeltaSeconds);
                if (_hitStopRemaining <= 0f)
                {
                    _hitStopRemaining = 0f;
                    _hitStopScale = 1f;
                }
            }
            Apply();
        }

        /// <summary>Về trạng thái bình thường (rời scene, tắt ticker, thoát Play Mode): hết pause, hết dừng hình, tốc độ = 1.</summary>
        public void ForceReset()
        {
            _pauseCount = 0;
            _hitStopRemaining = 0f;
            _hitStopScale = 1f;
            Apply();
        }

        private float ComputeScale()
        {
            if (_pauseCount > 0) return 0f;
            return _hitStopRemaining > 0f ? _hitStopScale : 1f;
        }

        private void Apply()
        {
            float target = ComputeScale();
            if (Mathf.Approximately(_appliedScale, target)) return;
            _appliedScale = target;
            _applyScale(target);
        }

        // ── Bản dùng chung của game ─────────────────────────────────────────

        private static PlaySpeedState s_shared;

        public static PlaySpeedState Shared
        {
            get
            {
                if (s_shared == null) s_shared = new PlaySpeedState(ApplyToUnity);
                return s_shared;
            }
        }

        // Dòng DUY NHẤT ghi timeScale trong Scripts/. Dùng UnityEngine.Time vì Game.Core.Time là một namespace.
        private static void ApplyToUnity(float scale)
        {
            UnityEngine.Time.timeScale = scale;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            // Enter Play Mode Options có thể tắt Domain Reload: bỏ trạng thái cũ để Play lần sau luôn bắt đầu ở tốc độ 1.
            if (s_shared != null) s_shared.ForceReset();
            s_shared = null;
        }
    }
}
