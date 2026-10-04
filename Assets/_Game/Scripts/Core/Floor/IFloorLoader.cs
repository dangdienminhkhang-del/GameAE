using System.Threading.Tasks;

namespace Game.Core.Floor
{
    /// <summary>
    /// Contract load/unload floor (docs/02 §19, 13 §4.2).
    /// Cài đặt nằm ở Game.PvE/Floor/FloorLoader.cs.
    /// FloorLoadStatus / FloorLoadResult định nghĩa ở file FloorLoadResult.cs.
    /// </summary>
    public interface IFloorLoader
    {
        /// <summary>
        /// Load floor theo ID. Trả về kết quả tường minh — KHÔNG throw exception thầm lặng.
        /// </summary>
        Task<FloorLoadResult> LoadFloorAsync(string floorId);

        /// <summary>Unload floor hiện tại (nếu có).</summary>
        Task UnloadCurrentFloorAsync();

        /// <summary>ID floor đang được load, null nếu chưa có floor nào.</summary>
        string CurrentFloorId { get; }
    }
}
