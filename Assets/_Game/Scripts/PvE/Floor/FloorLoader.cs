using System.Threading.Tasks;
using Game.Core.Floor;
using Game.Core.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.PvE.Floor
{
    /// <summary>
    /// Cài đặt IFloorLoader cho PvE (docs/02 §19, 13 §4.2).
    /// Kiểm tra world state rồi load scene additive.
    /// Không gọi UGS trực tiếp — chỉ gọi qua IWorldStateService.
    ///
    /// Constructor: FloorLoader(IWorldStateService, FloorDatabase)
    ///   — để tạo mà không cần account state, dùng overload có accountIsDead.
    /// </summary>
    public class FloorLoader : IFloorLoader
    {
        private readonly IWorldStateService _worldState;
        private readonly FloorDatabase _database;
        private readonly bool _accountIsDead;

        private string _currentFloorId;
        public string CurrentFloorId => _currentFloorId;

        /// <summary>Constructor dùng trong EditMode test (không có account context).</summary>
        public FloorLoader(IWorldStateService worldState, FloorDatabase database)
            : this(worldState, database, accountIsDead: false) { }

        /// <summary>Constructor đầy đủ — inject từ PvEBootstrap.</summary>
        public FloorLoader(IWorldStateService worldState, FloorDatabase database, bool accountIsDead)
        {
            _worldState = worldState;
            _database = database;
            _accountIsDead = accountIsDead;
        }

        // ── IFloorLoader ─────────────────────────────────────────────────────

        public async Task<FloorLoadResult> LoadFloorAsync(string floorId)
        {
            // 1. Account check
            if (_accountIsDead)
                return FloorLoadResult.ErrorResult(
                    "[FloorLoader] Account đã chết — không thể vào Floor.");

            // 2. Database lookup
            var def = _database?.GetFloor(floorId);
            if (def == null)
                return FloorLoadResult.NotFoundResult(floorId);

            // 3. World State unlock check — chỉ check khi floor có requiredBossDefeatedId
            if (!string.IsNullOrEmpty(def.requiredBossDefeatedId))
            {
                FloorStatus status;
                try
                {
                    status = await _worldState.GetFloorStatusAsync(floorId);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[FloorLoader] Lỗi đọc world state: {ex.Message}");
                    return FloorLoadResult.ErrorResult(
                        $"[FloorLoader] Không đọc được world state: {ex.Message}");
                }

                if (status == FloorStatus.Locked)
                    return FloorLoadResult.LockedResult(floorId);
            }
            else
            {
                // floor mở mặc định (không có requiredBossDefeatedId) — vẫn check world state
                // để tôn trọng trạng thái Locked nếu có override từ server
                try
                {
                    var status = await _worldState.GetFloorStatusAsync(floorId);
                    if (status == FloorStatus.Locked)
                        return FloorLoadResult.LockedResult(floorId);
                }
                catch
                {
                    // floor mặc định không require boss — tiếp tục nếu world state unavailable
                }
            }

            // 4. Unload floor cũ nếu có
            await UnloadCurrentFloorAsync();

            // 5. Load scene additive (EditMode test: scene không tồn tại → trả Loaded với def)
            if (string.IsNullOrEmpty(def.sceneKey))
                return FloorLoadResult.ErrorResult(
                    $"[FloorLoader] FloorDefinition '{floorId}' không có sceneKey.");

#if UNITY_EDITOR
            // Trong EditMode test, không load scene thật — chỉ trả Success
            if (!Application.isPlaying)
            {
                _currentFloorId = floorId;
                return FloorLoadResult.Success(def);
            }
#endif

            var op = SceneManager.LoadSceneAsync(def.sceneKey, LoadSceneMode.Additive);
            if (op == null)
                return FloorLoadResult.ErrorResult(
                    $"[FloorLoader] LoadSceneAsync trả null cho scene '{def.sceneKey}'. " +
                    "Kiểm tra scene có trong Build Settings không.");

            var tcs = new TaskCompletionSource<bool>();
            op.completed += _ => tcs.TrySetResult(op.isDone);
            await tcs.Task;

            if (!op.isDone)
                return FloorLoadResult.ErrorResult(
                    $"[FloorLoader] Scene '{def.sceneKey}' load không thành công.");

            _currentFloorId = floorId;
            Debug.Log($"[FloorLoader] Đã load tầng '{floorId}' (scene: {def.sceneKey}).");
            return FloorLoadResult.Success(def);
        }

        public async Task UnloadCurrentFloorAsync()
        {
            if (string.IsNullOrEmpty(_currentFloorId)) return;

            var def = _database?.GetFloor(_currentFloorId);
            if (def == null || string.IsNullOrEmpty(def.sceneKey))
            {
                _currentFloorId = null;
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _currentFloorId = null;
                return;
            }
#endif

            var scene = SceneManager.GetSceneByName(def.sceneKey);
            if (scene.IsValid() && scene.isLoaded)
            {
                var op = SceneManager.UnloadSceneAsync(def.sceneKey);
                if (op != null)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    op.completed += _ => tcs.TrySetResult(true);
                    await tcs.Task;
                }
            }

            Debug.Log($"[FloorLoader] Đã unload tầng '{_currentFloorId}'.");
            _currentFloorId = null;
        }
    }
}
