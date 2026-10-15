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

        /// <summary>Tầng CAO NHẤT đã vào (= personalHighestFloor lúc chết, docs/03). 0 = chưa có.</summary>
        public int finalHighestFloor;

        /// <summary>
        /// Tầng người chơi đang đứng lúc chết. 0 = không xác định / bản ghi cũ chưa có field này
        /// (thiếu field khi đọc JSON → 0, nên bản ghi cũ vẫn đọc được).
        /// </summary>
        public int finalFloor;

        public int totalPlayTimeSeconds;
    }
}
