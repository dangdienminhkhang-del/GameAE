using System;
using System.Threading.Tasks;
using Game.Core.Network;
using Game.Core.Time;

namespace Game.Services.Time
{
    /// <summary>
    /// ITimeService server-authoritative theo docs/08 §3.
    /// Giờ gốc đến từ snapshot của IServerFunctionService (Cloud Code).
    /// Client dự đoán presentation giữa các lần resync bằng monotonic elapsed time.
    /// Server unavailable hoặc snapshot quá cũ → IsAuthoritative=false; drift lớn → snap ngay; drift nhỏ → smooth correction.
    /// </summary>
    public class TimeService : ITimeService
    {
        private readonly TimeConfig _config;
        private readonly IServerFunctionService _serverFunctions;

        private WorldTimeSnapshot _snapshot;
        private long _snapshotReceivedAtUnix;
        private double _elapsedSinceSnapshot;
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
        public bool IsAuthoritative => _isAuthoritative && !IsSnapshotStaleInternal();

        public int SnapshotAgeSeconds
        {
            get
            {
                if (_snapshot == null) return int.MaxValue;
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long age = now - _snapshotReceivedAtUnix;
                return age > int.MaxValue ? int.MaxValue : (int)age;
            }
        }

        public event EventHandler<TimeOfDay> OnPhaseChanged;
        public event EventHandler<double> OnGameTimeAdvanced;
        public event EventHandler<WorldTimeSnapshot> WorldTimeResynced;

        private TimeOfDay _lastPhase = (TimeOfDay)(-1);

        public virtual async Task InitializeAsync()
        {
            _effectiveGameHours = _config != null ? _config.StartHour : 8.0;
            _lastPhase = CurrentPhase;
            IsInitialized = true;

            if (_serverFunctions != null)
            {
                await ResyncInternalAsync(calledFromInitialize: true);
            }
            else
            {
                _isAuthoritative = false;
            }
        }

        public async Task<bool> ResyncAsync()
        {
            if (!IsInitialized) return false;
            if (_serverFunctions == null) return false;
            return await ResyncInternalAsync(calledFromInitialize: false);
        }

        private async Task<bool> ResyncInternalAsync(bool calledFromInitialize)
        {
            WorldTimeSnapshot candidate;
            try
            {
                candidate = await _serverFunctions.GetWorldTimeSnapshotAsync();
            }
            catch
            {
                // Resync failed: giữ snapshot đã xác nhận nếu có.
                // IsAuthoritative false tự động khi snapshot quá maxSnapshotAgeSeconds (08 §5 hàng 2).
                if (!calledFromInitialize && _snapshot != null)
                {
                    return false;
                }
                _isAuthoritative = false;
                return false;
            }

            if (candidate == null)
            {
                // Server trả null = không có snapshot mới được xác nhận.
                // Giữ snapshot đã xác nhận gần nhất nếu có.
                if (!calledFromInitialize && _snapshot != null)
                {
                    return false;
                }
                _isAuthoritative = false;
                return false;
            }

            if (!WorldTimeSnapshot.IsNewerOrEquivalent(_snapshot, candidate))
            {
                return false;
            }

            ApplySnapshot(candidate, calledFromInitialize);
            return true;
        }

        private void ApplySnapshot(WorldTimeSnapshot candidate, bool isInitial)
        {
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double predicted = candidate.PredictGameHours(nowUnix);
            double driftHours = WrapDiff(_effectiveGameHours, predicted);
            double materialDrift = _config != null ? _config.MaterialDriftGameHours : 0.25f;

            if (!isInitial && Math.Abs(driftHours) <= materialDrift && Math.Abs(driftHours) > 0.0001)
            {
                _effectiveGameHours = Wrap(_effectiveGameHours + driftHours * 0.5);
            }
            else
            {
                _effectiveGameHours = Wrap(predicted);
            }

            _snapshot = candidate;
            _snapshotReceivedAtUnix = nowUnix;
            _elapsedSinceSnapshot = 0.0;
            _isAuthoritative = candidate.CycleConfigSupported;

            RaiseAdvanced();
            WorldTimeResynced?.Invoke(this, candidate);
        }

        public void SetGameTime(double hours)
        {
            _effectiveGameHours = Wrap(hours);
            RaiseAdvanced();
        }

        public virtual void Tick(float deltaRealSeconds)
        {
            if (!IsInitialized || deltaRealSeconds <= 0f) return;
            if (_config != null && !_config.DayNightCycleEnabled) return;

            double hoursPerSecond;
            if (_snapshot != null && _snapshot.realSecondsPerGameDay > 0)
            {
                hoursPerSecond = _snapshot.GameHoursPerRealSecondFromSnapshot;
            }
            else
            {
                hoursPerSecond = _config != null ? _config.GameHoursPerRealSecond : 24.0 / 1200.0;
            }

            _elapsedSinceSnapshot += deltaRealSeconds;
            _effectiveGameHours = Wrap(_effectiveGameHours + deltaRealSeconds * hoursPerSecond);
            RaiseAdvanced();
        }

        private bool IsSnapshotStaleInternal()
        {
            if (_snapshot == null) return true;
            int maxAge = _config != null ? _config.MaxSnapshotAgeSeconds : 900;
            if (maxAge <= 0) return false;
            return SnapshotAgeSeconds > maxAge;
        }

        private static double Wrap(double hours)
        {
            return ((hours % 24.0) + 24.0) % 24.0;
        }

        private static double WrapDiff(double from, double to)
        {
            double d = to - from;
            if (d > 12.0) return d - 24.0;
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
            if (h < 5.0) return TimeOfDay.Night;
            if (h < 7.0) return TimeOfDay.Dawn;
            if (h < 11.0) return TimeOfDay.Morning;
            if (h < 13.0) return TimeOfDay.Noon;
            if (h < 17.0) return TimeOfDay.Afternoon;
            if (h < 19.0) return TimeOfDay.Evening;
            return TimeOfDay.Night;
        }
    }
}
