using System;

namespace Game.Core.Time
{
    [Serializable]
    public class WorldTimeSnapshot
    {
        public int schemaVersion;
        public double gameTimeHoursAtAnchor;
        public long anchorUnixSeconds;
        public long revision;
        public int cycleConfigVersion;
        public double realSecondsPerGameDay;

        public const int SupportedCycleConfigVersion = 1;
        public const int CurrentSchemaVersion = 3;

        public bool CycleConfigSupported => cycleConfigVersion <= SupportedCycleConfigVersion;

        public double GameHoursPerRealSecondFromSnapshot
        {
            get
            {
                double rate = realSecondsPerGameDay > 0 ? realSecondsPerGameDay : 1200.0;
                return 24.0 / rate;
            }
        }

        public double PredictGameHours(long atUnixSeconds)
        {
            if (atUnixSeconds < anchorUnixSeconds) return gameTimeHoursAtAnchor;
            double elapsed = atUnixSeconds - anchorUnixSeconds;
            double hours = gameTimeHoursAtAnchor + elapsed * GameHoursPerRealSecondFromSnapshot;
            return ((hours % 24.0) + 24.0) % 24.0;
        }

        public bool IsStale(long currentUnixSeconds, int maxAgeSeconds)
        {
            if (maxAgeSeconds <= 0) return false;
            return currentUnixSeconds - anchorUnixSeconds > maxAgeSeconds;
        }

        public static bool IsNewerOrEquivalent(WorldTimeSnapshot current, WorldTimeSnapshot candidate)
        {
            if (candidate == null) return false;
            if (current == null) return true;
            if (candidate.revision > current.revision) return true;
            if (candidate.revision == current.revision && candidate.anchorUnixSeconds >= current.anchorUnixSeconds) return true;
            return false;
        }
    }
}
