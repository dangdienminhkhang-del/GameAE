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
    /// Overload có thêm cổng chuyển tầng (Func&lt;FloorTravelRules.Result&gt;): chỉ cho đổi tầng khi cổng trả Ok
    /// (đứng trong Vùng An Toàn, còn sống). Không truyền cổng = hành vi cũ hoàn toàn.
    /// Overload có thêm callback <c>onFloorEntered</c>: gọi ở MỌI nhánh vào tầng thành công (kể cả nhánh "đang đứng đúng tầng"
    /// và nhánh EditMode), KHÔNG gọi khi Locked/NotAllowed/NotFound/Error. Callback ném lỗi chỉ bị log, không làm hỏng việc vào tầng.
    /// </summary>
    public class FloorLoader : IFloorLoader
    {
        private readonly IWorldStateService _worldState;
        private readonly FloorDatabase _database;
        private readonly bool _accountIsDead;
        private readonly System.Func<FloorTravelRules.Result> _travelGate;
        private readonly System.Action<FloorDefinition> _onFloorEntered;

        private string _currentFloorId;
        public string CurrentFloorId => _currentFloorId;

        /// <summary>Constructor dùng trong EditMode test (không có account context).</summary>
        public FloorLoader(IWorldStateService worldState, FloorDatabase database)
            : this(worldState, database, accountIsDead: false) { }

        /// <summary>Constructor đầy đủ (không có cổng chuyển tầng) — hành vi cũ.</summary>
        public FloorLoader(IWorldStateService worldState, FloorDatabase database, bool accountIsDead)
            : this(worldState, database, accountIsDead, null) { }

        /// <summary>
        /// Constructor có cổng chuyển tầng — inject từ PvEBootstrap.
        /// <paramref name="travelGate"/> được gọi LÚC CẦN (đọc vị trí/trạng thái sống chết tại thời điểm đó);
        /// null = không có cổng (hành vi cũ). Cổng trả khác Ok thì LoadFloorAsync trả NotAllowed và không đổi gì.
        /// </summary>
        public FloorLoader(IWorldStateService worldState, FloorDatabase database, bool accountIsDead,
                           System.Func<FloorTravelRules.Result> travelGate)
            : this(worldState, database, accountIsDead, travelGate, null) { }

        /// <summary>
        /// Constructor có cổng chuyển tầng và callback "đã vào tầng" (<paramref name="onFloorEntered"/>, có thể null).
        /// PvEBootstrap dùng callback này để ghi nhận tầng cao nhất đã vào.
        /// </summary>
        public FloorLoader(IWorldStateService worldState, FloorDatabase database, bool accountIsDead,
                           System.Func<FloorTravelRules.Result> travelGate,
                           System.Action<FloorDefinition> onFloorEntered)
        {
            _worldState = worldState;
            _database = database;
            _accountIsDead = accountIsDead;
            _travelGate = travelGate;
            _onFloorEntered = onFloorEntered;
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

        // ── Cổng chuyển tầng ─────────────────────────────────────────────────

        // Chữ hiển thị cho người chơi (loader quyết định nội dung; UI chỉ hiện nguyên văn).
        private const string NotInSafeZoneMessage = "Chỉ có thể chuyển tầng khi đang ở Vùng An Toàn.";
        private const string PlayerDeadMessage = "Nhân vật đã gục, không thể chuyển tầng.";
        private const string NotAllowedFallbackMessage = "Không thể chuyển tầng lúc này.";

        private static string DescribeTravelDenial(FloorTravelRules.Result verdict)
        {
            switch (verdict)
            {
                case FloorTravelRules.Result.NotInSafeZone: return NotInSafeZoneMessage;
                case FloorTravelRules.Result.PlayerDead: return PlayerDeadMessage;
                default: return NotAllowedFallbackMessage;
            }
        }

        /// <summary>
        /// Hỏi cổng chuyển tầng. Trả null nếu được phép (hoặc không có cổng); ngược lại trả NotAllowedResult.
        /// Không đổi bất kỳ trạng thái nào của loader.
        /// </summary>
        private FloorLoadResult CheckTravelGate(string floorId)
        {
            if (_travelGate == null) return null;

            var verdict = _travelGate();
            if (verdict == FloorTravelRules.Result.Ok) return null;

            Debug.Log($"[FloorLoader] Từ chối chuyển tầng '{floorId}': {verdict}.");
            return FloorLoadResult.NotAllowedResult(DescribeTravelDenial(verdict));
        }

        /// <summary>
        /// Kết quả thành công của một lần vào tầng: báo callback (nếu có) rồi trả Success.
        /// Callback ném lỗi → log, KHÔNG làm hỏng việc vào tầng.
        /// </summary>
        private FloorLoadResult Entered(FloorDefinition def)
        {
            if (_onFloorEntered != null)
            {
                try
                {
                    _onFloorEntered(def);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            return FloorLoadResult.Success(def);
        }

        // ── IFloorLoader ─────────────────────────────────────────────────────

        public async Task<FloorLoadResult> LoadFloorAsync(string floorId)
        {
            // 1. Account check
            if (_accountIsDead)
                return FloorLoadResult.ErrorResult(
                    "[FloorLoader] Account đã chết — không thể vào Floor.");

            // 1b. Cổng chuyển tầng (lần 1): từ chối sớm, khỏi đọc world state vô ích.
            var denied = CheckTravelGate(floorId);
            if (denied != null) return denied;

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

            // 4b. Cổng chuyển tầng (lần 2): người chơi có thể đã bước ra khỏi Vùng An Toàn trong lúc chờ world state.
            //     Kiểm NGAY TRƯỚC khi unload/nạp/kích hoạt bất cứ thứ gì — từ chối thì _currentFloorId không đổi.
            denied = CheckTravelGate(floorId);
            if (denied != null) return denied;

#if UNITY_EDITOR
            // Ngoài Play Mode (EditMode test): không đụng scene thật — chỉ trả Success
            if (!Application.isPlaying)
            {
                await UnloadCurrentFloorAsync();
                _currentFloorId = floorId;
                return Entered(def);
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
                // Đang đứng đúng tầng này: chỉ đảm bảo nội dung đang bật, KHÔNG dịch chuyển lại người chơi.
                FloorActivation.ActivateFloor(def, placePlayer: false);
                return Entered(def);
            }

            // 6. Unload floor cũ nếu có (không bao giờ unload scene active)
            await UnloadCurrentFloorAsync();

            if (hosted)
            {
                _currentFloorId = floorId;
                Debug.Log($"[FloorLoader] Tầng '{floorId}' đã nằm trong scene host '{def.sceneKey}' — " +
                          $"bỏ qua load (scene đang nạp: {loadedNames.Count}).");
                FloorActivation.ActivateFloor(def);
                return Entered(def);
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
            FloorActivation.ActivateFloor(def);
            return Entered(def);
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
