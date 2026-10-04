using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Floor
{
    /// <summary>
    /// Đăng ký toàn bộ FloorDefinition trong project (docs/06 §2, docs/03 §4).
    /// Asset nằm ở Data/Databases/FloorDatabase.asset.
    /// Thêm tầng = thêm FloorDefinition asset + đăng ký vào mảng floors này.
    /// Core KHÔNG hard-code số tầng; số tối đa đọc từ GameConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "FloorDatabase", menuName = "Game/Floor/Floor Database")]
    public class FloorDatabase : ScriptableObject
    {
        [Tooltip("Danh sách tất cả tầng. Thứ tự trong danh sách không quan trọng — " +
                 "code sort theo floorNumber.")]
        public List<FloorDefinition> floors = new List<FloorDefinition>();

        // ── Lookup ──────────────────────────────────────────────────────────

        private Dictionary<string, FloorDefinition> _cache;

        /// <summary>Tìm FloorDefinition theo ID (floor.NN). Trả null nếu không tìm thấy.</summary>
        public FloorDefinition GetById(string floorId)
        {
            BuildCacheIfNeeded();
            _cache.TryGetValue(floorId, out var def);
            return def;
        }

        /// <summary>Alias của GetById — dùng trong test và UI.</summary>
        public FloorDefinition GetFloor(string floorId) => GetById(floorId);

        /// <summary>Trả danh sách tầng đã sort theo floorNumber (tăng dần).</summary>
        public List<FloorDefinition> GetAllSorted()
        {
            var result = new List<FloorDefinition>(floors);
            result.Sort((a, b) => a.floorNumber.CompareTo(b.floorNumber));
            return result;
        }

        /// <summary>Alias của GetAllSorted — dùng trong test.</summary>
        public List<FloorDefinition> GetAllFloors() => GetAllSorted();

        // ── Validation ──────────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra tất cả định nghĩa hợp lệ. Trả danh sách lỗi.
        /// Gọi trong OnValidate hoặc test.
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var seenIds = new HashSet<string>();

            foreach (var floor in floors)
            {
                if (floor == null) { errors.Add("Có entry null trong FloorDatabase."); continue; }
                if (!floor.HasValidId()) errors.Add($"FloorDefinition '{floor.name}': ID '{floor.id}' không hợp lệ.");
                if (!seenIds.Add(floor.id)) errors.Add($"Trùng ID: '{floor.id}'.");
                if (floor.floorNumber < 1) errors.Add($"FloorDefinition '{floor.id}': floorNumber < 1.");
        if (string.IsNullOrEmpty(floor.sceneKey)) errors.Add($"FloorDefinition '{floor.id}': sceneKey trống.");
            }
            return errors;
        }

        private void OnValidate()
        {
            _cache = null; // invalidate cache khi Inspector thay đổi
            var errors = Validate();
            foreach (var err in errors)
                Debug.LogWarning($"[FloorDatabase] {err}", this);
        }

        private void BuildCacheIfNeeded()
        {
            if (_cache != null) return;
            _cache = new Dictionary<string, FloorDefinition>(floors.Count);
            foreach (var f in floors)
            {
                if (f == null || string.IsNullOrEmpty(f.id)) continue;
                if (!_cache.TryAdd(f.id, f))
                    Debug.LogError($"[FloorDatabase] Trùng ID '{f.id}' khi build cache.", this);
            }
        }
    }
}
