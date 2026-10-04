using System;

namespace Game.Core.Character
{
    /// <summary>
    /// Dữ liệu permadeath, lưu ở Player Data key "death" (docs/03 §5, 09 §1.4).
    /// LƯU Ý: dùng kiểu KHÔNG nullable (0 = chưa có) vì JsonUtility không serialize int?/long?.
    /// </summary>
    [Serializable]
    public class DeathInfo
    {
        public bool isDead;
        public long deathTimeUnix;
        public string deathCause;
        public string deathSourceId;
        public int finalLevel;
        public int finalHighestFloor;
        public int totalPlayTimeSeconds;
    }
}
