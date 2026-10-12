using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Game.Core.Network;
using Game.Core.Time;

namespace Game.Services.Time
{
    /// <summary>
    /// ITimeService server-authoritative theo docs/08 §3.
    ///
    /// Nguyên tắc (08 §3.3):
    ///   - Server re-anchor snapshot tại thời điểm trả lời → client dùng gameTimeHoursAtAnchor trực tiếp.
    ///   - KHÔNG dùng DateTimeOffset/giờ thiết bị để tính giờ game (tránh clock-skew nhân lên).
    ///   - Tuổi snapshot đo bằng Stopwatch monotonic (không bị đồng hồ máy ảnh hưởng).
    ///   - Half-RTT correction: cộng (rttSeconds / 2) × GameHoursPerRealSecondFromSnapshot
    ///     để bù thời gian snapshot đi từ server đến client.
    /// </summary>
    public class TimeService : ITimeService
    {
        private readonly TimeConfig _config;
        private readonly IServerFunctionService _serverFunctions;

        private WorldTimeSnapshot _snapshot;

        // Tuổi snapshot theo Stopwatch monotonic — không phụ thuộc đồng hồ máy.
        private long _snapshotTimestamp;   // Stopwatch.GetTimestamp() lúc ApplySnapshot

        private bool _isAuthoritative;
        private double _effectiveGameHours;

        public TimeService(TimeConfig config = null, IServerFunctionService serverFunctions = null)
        {
            _config = config;
            _serverFunctions = serverFunctions;
        }

        public double CurrentGameTimeHours => _effectiveGameHours;
        public TimeOfDay CurrentPhase => CalculatePhase(_effectiveGameHours);
        public bool IsInitialized { get; private set; }
        public WorldTimeSnapshot CurrentSnapshot => _snapshot;

        /// <summary>
        /// IsAuthoritative = có snapshot được xác nhận AND CycleConfig hỗ trợ AND chưa stale.
        /// Không bao giờ set false khi resync thất bại nếu snapshot cũ còn hợp lệ (08 §5 hàng 2).
        /// </summary>
        public bool IsAuthoritative => _isAuthoritative && !IsSnapshotStaleInternal();

        /// <summary>Tuổi snapshot tính bằng Stopwatch — không phụ thuộc giờ thiết bị.</summary>
        public int SnapshotAgeSeconds
        {
            get
            {
                if (_snapshot == null) return int.MaxValue;
                long elapsed = Stopwatch.GetTimestamp() - _snapshotTimestamp;
                double seconds = (double)elapsed / Stopwatch.Frequency;
                return seconds > int.MaxValue ? int.MaxValue : (int)seconds;
            }
        }

        public event EventHandler<TimeOfDay> OnPhaseChanged;
        public event EventHandler<double> OnGameTimeAdvanced;
        public event EventHandler<WorldTimeSnapshot> WorldTimeResynced;

        private TimeOfDay _lastPhase = (TimeOfDay)(-1);

        // ── Lifecycle ──────────────────────────────────────────────

        public virtual async Task InitializeAsync()
        {
            _effectiveGameHours = _config != null ? _config.StartHour : 8.0;
            _lastPhase = CurrentPhase;
            IsInitialized = true;

            if (_serverFunctions != null)
                await ResyncInternalAsync(calledFromInitialize: true);
            else
                _isAuthoritative = false;
        }

        public async Task<bool> ResyncAsync()
        {
            if (!IsInitialized) return false;
            if (_serverFunctions == null) return false;
            return await ResyncInternalAsync(calledFromInitialize: false);
        }

        // ── Resync ─────────────────────────────────────────────────

        private async Task<bool> ResyncInternalAsync(bool calledFromInitialize)
        {
            WorldTimeSnapshot candidate;
            double rttSeconds = 0.0;

            // Đo RTT bằng Stopwatch để tính half-RTT correction
            long t0 = Stopwatch.GetTimestamp();
            try
            {
                candidate = await _serverFunctions.GetWorldTimeSnapshotAsync();
                rttSeconds = (double)(Stopwatch.GetTimestamp() - t0) / Stopwatch.Frequency;
            }
            catch
            {
                // Resync thất bại: giữ snapshot cũ nếu có — IsAuthoritative tự suy từ IsSnapshotStaleInternal.
                if (calledFromInitialize) _isAuthoritative = false;
                return false;
            }

            if (candidate == null)
            {
                if (calledFromInitialize) _isAuthoritative = false;
                return false;
            }

            if (!WorldTimeSnapshot.IsNewerOrEquivalent(_snapshot, candidate))
                return false;

            ApplySnapshot(candidate, calledFromInitialize, rttSeconds);
            return true;
        }

        // ── ApplySnapshot ──────────────────────────────────────────

        /// <summary>
        /// Server đã re-anchor snapshot tại lúc trả lời → dùng gameTimeHoursAtAnchor trực tiếp.
        /// KHÔNG gọi PredictGameHours(DateTimeOffset.UtcNow) — tránh clock-skew nhân tốc độ game.
        /// Half-RTT correction: cộng (rtt/2) × hoursPerSecond để bù transit time.
        /// </summary>
        private void ApplySnapshot(WorldTimeSnapshot candidate, bool isInitial, double rttSeconds)
        {
            // Điểm khởi đầu: giờ server đã gắn anchor tại thời điểm trả lời
            double serverHours = candidate.gameTimeHoursAtAnchor;

            // Cộng half-RTT nếu đo được (bù thời gian gói tin đi từ server → client)
            if (rttSeconds > 0.0 && candidate.realSecondsPerGameDay > 0)
            {
                double hoursPerSec = candidate.GameHoursPerRealSecondFromSnapshot;
                serverHours = Wrap(serverHours + (rttSeconds * 0.5) * hoursPerSec);
            }

            double materialDrift = _config != null ? _config.MaterialDriftGameHours : 0.25f;

            if (!isInitial && _snapshot != null)
            {
                // Smooth correction nếu drift nhỏ
                double driftHours = WrapDiff(_effectiveGameHours, serverHours);
                if (Math.Abs(driftHours) <= materialDrift && Math.Abs(driftHours) > 0.0001)
                    _effectiveGameHours = Wrap(_effectiveGameHours + driftHours * 0.5);
                else
                    _effectiveGameHours = serverHours;
            }
            else
            {
                _effectiveGameHours = serverHours;
            }

            _snapshot = candidate;
            _snapshotTimestamp = Stopwatch.GetTimestamp();   // monotonic, không phụ thuộc giờ máy
            _isAuthoritative = candidate.CycleConfigSupported;

            RaiseAdvanced();
            WorldTimeResynced?.Invoke(this, candidate);
        }

        // ── Tick ───────────────────────────────────────────────────

        public void SetGameTime(double hours)
        {
            _effectiveGameHours = Wrap(hours);
            RaiseAdvanced();
        }

        public virtual void Tick(float deltaRealSeconds)
        {
            if (!IsInitialized || deltaRealSeconds <= 0f) return;
            if (_config != null && !_config.DayNightCycleEnabled) return;

            double hoursPerSecond = _snapshot != null && _snapshot.realSecondsPerGameDay > 0
                ? _snapshot.GameHoursPerRealSecondFromSnapshot
                : (_config != null ? _config.GameHoursPerRealSecond : 24.0 / 1200.0);

            _effectiveGameHours = Wrap(_effectiveGameHours + deltaRealSeconds * hoursPerSecond);
            RaiseAdvanced();
        }

        // ── Helpers ────────────────────────────────────────────────

        private bool IsSnapshotStaleInternal()
        {
            if (_snapshot == null) return true;
            int maxAge = _config != null ? _config.MaxSnapshotAgeSeconds : 900;
            if (maxAge <= 0) return false;
            return SnapshotAgeSeconds > maxAge;
        }

        private static double Wrap(double hours) => ((hours % 24.0) + 24.0) % 24.0;

        private static double WrapDiff(double from, double to)
        {
            double d = to - from;
            if (d >  12.0) return d - 24.0;
            if (d < -12.0) return d + 24.0;
            return d;
        }

        private void RaiseAdvanced()
        {
            OnGameTimeAdvanced?.Invoke(this, _effectiveGameHours);
            TimeOfDay phase = CurrentPhase;
            if (phase == _lastPhase) return;
            _lastPhase = phase;
            OnPhaseChanged?.Invoke(this, phase);
        }

        public TimeOfDay CalculatePhase(double gameTimeHours)
        {
            double h = Wrap(gameTimeHours);
            if (h <  5.0) return TimeOfDay.Night;
            if (h <  7.0) return TimeOfDay.Dawn;
            if (h < 11.0) return TimeOfDay.Morning;
            if (h < 13.0) return TimeOfDay.Noon;
            if (h < 17.0) return TimeOfDay.Afternoon;
            if (h < 19.0) return TimeOfDay.Evening;
            return TimeOfDay.Night;
        }
    }
}
