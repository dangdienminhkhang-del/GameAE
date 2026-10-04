using Game.Core.Events;
using Game.Core.Time;
using Game.Services;
using UnityEngine;

namespace Game.PvE.Clock
{
    /// <summary>
    /// Đồng hồ thế giới phía client: tạo ITimeService theo TimeConfig, tick mỗi frame và
    /// phát GameEvents.OnTimeChanged / OnTimePhaseChanged để ánh sáng, HUD, quái, NPC nghe theo.
    /// Thực hiện resync tự động theo interval (config.ResyncIntervalSeconds) và khi app resume.
    /// Namespace đặt là Game.PvE.Clock (không phải ...Time) để không che UnityEngine.Time.
    /// </summary>
    public class WorldClock : MonoBehaviour
    {
        [SerializeField] private TimeConfig config;

        private ITimeService _clock;
        private int _day = 1;
        private double _lastHours = -1.0;
        private TimeOfDay _lastPhase = (TimeOfDay)(-1);
        private float _secondsSinceLastResync;

        public ITimeService Clock => _clock;
        public TimeConfig Config => config;
        public int Day => _day;
        public double GameHours => _clock != null ? _clock.CurrentGameTimeHours : 0.0;
        public TimeOfDay Phase => _clock != null ? _clock.CurrentPhase : TimeOfDay.Morning;
        public bool IsTimeAuthoritative => _clock != null && _clock.IsAuthoritative;

        private async void Awake()
        {
            _clock = ServiceProvider.CreateTimeService(config);
            await _clock.InitializeAsync();
            SubscribeClock();
            Publish(true);
        }

        private void SubscribeClock()
        {
            if (_clock == null) return;
            _clock.WorldTimeResynced += OnWorldTimeResynced;
        }

        private void OnDestroy()
        {
            if (_clock != null) _clock.WorldTimeResynced -= OnWorldTimeResynced;
        }

        private void OnWorldTimeResynced(object sender, WorldTimeSnapshot snapshot)
        {
            _secondsSinceLastResync = 0f;
            Publish(false);
        }

        private void Update()
        {
            if (_clock == null || !_clock.IsInitialized) return;

            float dt = UnityEngine.Time.deltaTime;
            _clock.Tick(dt);
            Publish(false);

            _secondsSinceLastResync += dt;
            int interval = config != null ? config.ResyncIntervalSeconds : 300;
            if (_secondsSinceLastResync >= interval)
            {
                _secondsSinceLastResync = 0f;
                _ = _clock.ResyncAsync();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && _clock != null && _clock.IsInitialized)
            {
                _secondsSinceLastResync = 0f;
                _ = _clock.ResyncAsync();
            }
        }

        public void SetTime(double gameHours)
        {
            if (_clock == null) return;
            _clock.SetGameTime(gameHours);
            Publish(false);
        }

        private void Publish(bool force)
        {
            if (_clock == null) return;

            double hours = _clock.CurrentGameTimeHours;
            TimeOfDay phase = _clock.CurrentPhase;

            if (!force && Mathf.Abs((float)(hours - _lastHours)) < 0.0001f && phase == _lastPhase) return;

            if (_lastHours >= 0.0 && hours < _lastHours) _day++;

            _lastHours = hours;
            var args = new TimeChangedArgs { GameHours = hours, Day = _day, Phase = phase };
            GameEvents.RaiseTimeChanged(this, args);

            if (phase != _lastPhase)
            {
                _lastPhase = phase;
                GameEvents.RaiseTimePhaseChanged(this, args);
            }
        }
    }
}
