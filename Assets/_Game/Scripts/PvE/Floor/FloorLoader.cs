using System.Collections.Generic;
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
    /// Scene host: Floor 1 hiện nằm ngay trong scene gameplay đang chạy (PvE_Gameplay).
    /// Nếu sceneKey của tầng đã được nạp thì KHÔNG nạp lại (tránh nhân đôi người chơi/Canvas/EventSystem),
    /// và KHÔNG bao giờ unload scene đang active (nó chứa người chơi + HUD + bootstrap).
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

        // ── Logic thuần (test được trong EditMode) ───────────────────────────

        /// <summary>
        /// True nếu scene của tầng đã nằm trong danh sách scene đang nạp
        /// (so sánh ordinal). sceneKey rỗng/null hoặc danh sách null → false.
        /// </summary>
        public static bool IsSceneAlreadyHosted(string sceneKey, IEnumerable<string> loadedSceneNames)
        {
            if (string.IsNullOrEmpty(sceneKey) || loadedSceneNames == null) return false;
            foreach (var name in loadedSceneNames)
                if (string.Equals(name, sceneKey, System.StringComparison.Ordinal))
                    return true;
            return false;
        }

        private static List<string> GetLoadedSceneNames()
        {
            var names = new List<string>(SceneManager.sceneCount);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) names.Add(scene.name);
            }
            return names;
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

            // 4. sceneKey bắt buộc (kiểm tra trước khi unload để lỗi không làm mất tầng cũ)
            if (string.IsNullOrEmpty(def.sceneKey))
                return FloorLoadResult.ErrorResult(
                    $"[FloorLoader] FloorDefinition '{floorId}' không có sceneKey.");

#if UNITY_EDITOR
            // Ngoài Play Mode (EditMode test): không đụng scene thật — chỉ trả Success
            if (!Application.isPlaying)
            {
                await UnloadCurrentFloorAsync();
                _currentFloorId = floorId;
                return FloorLoadResult.Success(def);
            }
#endif

            // 5. Scene của tầng đã được nạp (vd. Floor 1 nằm trong scene host)?
            var loadedNames = GetLoadedSceneNames();
            bool hosted = IsSceneAlreadyHosted(def.sceneKey, loadedNames);

            // Chọn đúng tầng đang đứng → không làm gì
            if (hosted && _currentFloorId == floorId)
            {
                Debug.Log($"[FloorLoader] Tầng '{floorId}' đang là tầng hiện tại — không load lại " +
                          $"(scene đang nạp: {loadedNames.Count}).");
                return FloorLoadResult.Success(def);
            }

            // 6. Unload floor cũ nếu có (không bao giờ unload scene active)
            await UnloadCurrentFloorAsync();

            if (hosted)
            {
                _currentFloorId = floorId;
                Debug.Log($"[FloorLoader] Tầng '{floorId}' đã nằm trong scene host '{def.sceneKey}' — " +
                          $"bỏ qua load (scene đang nạp: {loadedNames.Count}).");
                return FloorLoadResult.Success(def);
            }

            // 7. Load scene additive
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
                // Scene active = scene host (người chơi + HUD + bootstrap) → tuyệt đối không unload.
                if (scene == SceneManager.GetActiveScene())
                {
                    Debug.Log($"[FloorLoader] Tầng '{_currentFloorId}' nằm trong scene host '{def.sceneKey}' — không unload.");
                    _currentFloorId = null;
                    return;
                }

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
