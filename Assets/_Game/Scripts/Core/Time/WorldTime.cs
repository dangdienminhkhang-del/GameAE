using System;

namespace Game.Core.Time
{
    [Serializable]
    public class WorldTime
    {
        public double gameTimeHours;
        public long lastSyncRealTimeUnix;
    }
}
