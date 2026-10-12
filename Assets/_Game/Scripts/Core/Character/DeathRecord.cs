namespace Game.Core.Character
{
    /// <summary>
    /// Bản ghi cái chết BẤT BIẾN, chụp tại thời điểm chết (docs/09 §1.3, ADR-021).
    /// Đưa vào <see cref="IDeathService.RecordDeathAsync"/>; service ánh xạ sang <see cref="DeathInfo"/>
    /// (dữ liệu lưu ở Player Data key "death") bằng <see cref="ToDeathInfo"/>.
    /// </summary>
    public sealed class DeathRecord
    {
        public DeathReason Reason { get; }
        public string SourceId { get; }
        public int FinalLevel { get; }

        /// <summary>Tầng người chơi đang ở lúc chết. 0 = không xác định được (không đoán).</summary>
        public int FinalHighestFloor { get; }

        public int TotalPlayTimeSeconds { get; }

        /// <summary>Thời điểm chết, Unix giây (UTC).</summary>
        public long DeathTimeUnix { get; }

        public DeathRecord(
            DeathReason reason,
            string sourceId,
            int finalLevel,
            int finalHighestFloor,
            int totalPlayTimeSeconds,
            long deathTimeUnix)
        {
            Reason = reason;
            SourceId = sourceId;
            FinalLevel = finalLevel;
            FinalHighestFloor = finalHighestFloor;
            TotalPlayTimeSeconds = totalPlayTimeSeconds;
            DeathTimeUnix = deathTimeUnix;
        }

        /// <summary>Ánh xạ sang dữ liệu lưu "death": isDead luôn true.</summary>
        public DeathInfo ToDeathInfo()
        {
            return new DeathInfo
            {
                isDead = true,
                deathCause = Reason.ToString(),
                deathSourceId = SourceId,
                deathTimeUnix = DeathTimeUnix,
                finalLevel = FinalLevel,
                finalHighestFloor = FinalHighestFloor,
                totalPlayTimeSeconds = TotalPlayTimeSeconds
            };
        }
    }
}
