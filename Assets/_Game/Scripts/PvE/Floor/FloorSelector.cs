using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Floor;
using Game.Core.World;
using UnityEngine;

namespace Game.PvE.Floor
{
    /// <summary>
    /// Cung cấp danh sách tầng có thể chọn cho UI (docs/02 §19).
    /// Đọc World State qua IWorldStateService — KHÔNG gọi UGS trực tiếp.
    /// UI chỉ gọi GetSelectableFloorsAsync() rồi hiển thị kết quả.
    /// </summary>
    public class FloorSelector
    {
        private readonly FloorDatabase _database;
        private readonly IWorldStateService _worldState;

        public FloorSelector(FloorDatabase database, IWorldStateService worldState)
        {
            _database = database;
            _worldState = worldState;
        }

        /// <summary>
        /// Trả danh sách tầng kèm trạng thái Available/Locked/Cleared.
        /// Sort theo floorNumber tăng dần.
        /// </summary>
        public async Task<List<FloorSelectEntry>> GetSelectableFloorsAsync()
        {
            var result = new List<FloorSelectEntry>();
            var sorted = _database.GetAllSorted();

            Dictionary<string, FloorStatus> statuses;
            try
            {
                statuses = await _worldState.GetAllFloorStatusesAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FloorSelector] Không đọc được World State: {ex.Message}");
                // Fail-safe: trả về floor.01 Available, phần còn lại Locked
                statuses = new Dictionary<string, FloorStatus>();
            }

            foreach (var def in sorted)
            {
                if (def == null || !def.HasValidId()) continue;

                FloorStatus status = FloorStatus.Locked;

                // floor.01 mở mặc định nếu world state không có entry
                if (string.IsNullOrEmpty(def.requiredBossDefeatedId))
                    status = FloorStatus.Available;

                if (statuses.TryGetValue(def.id, out var worldStatus))
                    status = worldStatus;

                result.Add(new FloorSelectEntry(def, status));
            }

            return result;
        }
    }

    /// <summary>Một dòng trong danh sách chọn tầng.</summary>
    public class FloorSelectEntry
    {
        public FloorDefinition Definition { get; }
        public FloorStatus Status { get; }
        public bool IsPlayable => Status == FloorStatus.Available || Status == FloorStatus.Cleared;

        public FloorSelectEntry(FloorDefinition definition, FloorStatus status)
        {
            Definition = definition;
            Status = status;
        }
    }
}
