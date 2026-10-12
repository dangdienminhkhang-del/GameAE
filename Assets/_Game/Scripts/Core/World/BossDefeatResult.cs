namespace Game.Core.World
{
    /// <summary>
    /// Kết quả SERVER đã xác nhận cho yêu cầu "boss đã bị hạ" (docs/02 §22, docs/03 §6).
    /// Chỉ được tạo từ response hợp lệ của server function; client không tự dựng để "đoán" kết quả.
    /// Khớp response cố định của cloudcode/ReportBossDefeated.js:
    /// <c>{ ok, alreadyDefeated, bossId, floorId, bossStatus:"DEFEATED", floorStatus:"CLEARED", nextFloorId }</c>.
    /// </summary>
    public sealed class BossDefeatResult
    {
        /// <summary>True nếu boss đã DEFEATED từ trước (lần gọi lại); server vẫn đối chiếu/sửa world_floors.</summary>
        public bool AlreadyDefeated { get; }

        /// <summary>Boss progression ID, dạng <c>boss.floor_NN</c> (docs/03 §2).</summary>
        public string BossId { get; }

        /// <summary>Tầng chứa boss, dạng <c>floor.NN</c>. Server đặt thành CLEARED.</summary>
        public string FloorId { get; }

        /// <summary>Tầng kế tiếp do SERVER chỉ định (dạng <c>floor.NN</c>). Server đặt thành AVAILABLE.</summary>
        public string NextFloorId { get; }

        public BossDefeatResult(bool alreadyDefeated, string bossId, string floorId, string nextFloorId)
        {
            AlreadyDefeated = alreadyDefeated;
            BossId = bossId;
            FloorId = floorId;
            NextFloorId = nextFloorId;
        }
    }
}
