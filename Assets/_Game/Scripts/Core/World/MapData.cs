using System;
using System.Collections.Generic;

namespace Game.Core.World
{
    /// <summary>Sương chiến tranh của MỘT tầng, đã mã hoá base64 (xem <see cref="MapRevealGrid"/>).</summary>
    [Serializable]
    public class MapFloorEntry
    {
        public string floorId;
        public int blocksX;
        public int blocksY;
        public string bits;
    }

    /// <summary>Player Data key "map": bản đồ tự vẽ của mọi tầng (Sổ Hải Đồ).</summary>
    [Serializable]
    public class MapData
    {
        public int schemaVersion = 1;
        public List<MapFloorEntry> floors = new List<MapFloorEntry>();
    }

    /// <summary>Player Data key "beacons": các Đèn Hiệu đã thắp.</summary>
    [Serializable]
    public class BeaconData
    {
        public int schemaVersion = 1;
        public List<string> lit = new List<string>();
    }
}
