namespace Game.Core.Floor
{
    /// <summary>
    /// Trạng thái kết quả khi load một tầng.
    /// </summary>
    public enum FloorLoadStatus
    {
        /// <summary>Load thành công — Floor sẵn sàng chơi.</summary>
        Loaded = 0,

        /// <summary>Tầng bị khóa theo World State.</summary>
        Locked = 1,

        /// <summary>Không tìm thấy tầng trong database.</summary>
        NotFound = 2,

        /// <summary>Lỗi khi load (network, exception, v.v.).</summary>
        Error = 3
    }

    /// <summary>
    /// Kết quả trả về từ FloorLoader.LoadFloorAsync().
    /// Luôn explicit — không dùng null thay cho trạng thái lỗi.
    /// </summary>
    public sealed class FloorLoadResult
    {
        /// <summary>Trạng thái load.</summary>
        public FloorLoadStatus Status { get; }

        /// <summary>
        /// Định nghĩa tầng đã load.
        /// Chỉ có giá trị khi Status == Loaded; null trong các trường hợp khác.
        /// </summary>
        public FloorDefinition Floor { get; }

        /// <summary>
        /// Thông báo lỗi hoặc mô tả lý do.
        /// Luôn có giá trị khi Status != Loaded.
        /// </summary>
        public string ErrorMessage { get; }

        private FloorLoadResult(FloorLoadStatus status, FloorDefinition floor, string errorMessage)
        {
            Status = status;
            Floor = floor;
            ErrorMessage = errorMessage;
        }

        // ─── Factory methods ──────────────────────────────────────────────────────

        /// <summary>Tầng load thành công.</summary>
        public static FloorLoadResult Success(FloorDefinition floor) =>
            new FloorLoadResult(FloorLoadStatus.Loaded, floor, null);

        /// <summary>Tầng bị khóa (server chưa mở).</summary>
        public static FloorLoadResult LockedResult(string floorId) =>
            new FloorLoadResult(FloorLoadStatus.Locked, null, $"[Floor] Tầng '{floorId}' đang bị khóa.");

        /// <summary>Không tìm thấy tầng trong database.</summary>
        public static FloorLoadResult NotFoundResult(string floorId) =>
            new FloorLoadResult(FloorLoadStatus.NotFound, null, $"[Floor] Không tìm thấy tầng '{floorId}' trong database.");

        /// <summary>Lỗi khi load.</summary>
        public static FloorLoadResult ErrorResult(string message) =>
            new FloorLoadResult(FloorLoadStatus.Error, null, $"[Floor] Lỗi: {message}");
    }
}
