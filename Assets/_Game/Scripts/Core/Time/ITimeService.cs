using System;
using System.Threading.Tasks;

namespace Game.Core.Time
{
    public interface ITimeService
    {
        double CurrentGameTimeHours { get; }
        TimeOfDay CurrentPhase { get; }
        bool IsInitialized { get; }
        WorldTimeSnapshot CurrentSnapshot { get; }
        bool IsAuthoritative { get; }
        int SnapshotAgeSeconds { get; }

        event EventHandler<TimeOfDay> OnPhaseChanged;
        event EventHandler<double> OnGameTimeAdvanced;
        event EventHandler<WorldTimeSnapshot> WorldTimeResynced;

        Task InitializeAsync();
        Task<bool> ResyncAsync();
        void SetGameTime(double gameTimeHours);
        void Tick(float deltaRealSeconds);
        TimeOfDay CalculatePhase(double gameTimeHours);
    }
}
