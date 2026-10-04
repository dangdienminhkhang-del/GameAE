using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Floor;
using Game.Core.World;
using Game.PvE.Floor;
using Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Floor
{
    /// <summary>
    /// UI hiển thị danh sách tầng, trạng thái (Locked/Available/Cleared) và cho phép chọn tầng.
    ///
    /// Trách nhiệm:
    ///   - List floors từ FloorDatabase (sort theo floorNumber).
    ///   - Hiển thị trạng thái từ IWorldStateService.
    ///   - Khi player chọn: gọi FloorLoader.LoadFloorAsync, xử lý result.
    ///   - Nếu Loaded → gọi SceneLoader.Load(sceneKey).
    ///   - KHÔNG chứa logic unlock. KHÔNG gọi UGS trực tiếp.
    ///
    /// Gán qua Inspector:
    ///   floorDatabase, floorLoader, worldStateService, floorButtonContainer, floorButtonPrefab,
    ///   statusMessageText.
    /// </summary>
    public class FloorSelectorUI : MonoBehaviour
    {
        // ─── Inspector refs ───────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("FloorDatabase ScriptableObject asset")]
        [SerializeField] private FloorDatabase floorDatabase;

        [Header("Dependencies")]
        [Tooltip("IWorldStateService implementation (component trên cùng GameObject hoặc assign)")]
        [SerializeField] private WorldStateServiceProxy worldStateProxy;

        [Header("UI")]
        [Tooltip("Container (Vertical Layout Group) chứa các nút tầng")]
        [SerializeField] private Transform floorButtonContainer;

        [Tooltip("Prefab nút tầng — cần có FloorButtonItem component")]
        [SerializeField] private GameObject floorButtonPrefab;

        [Tooltip("Text hiển thị thông báo lỗi / trạng thái")]
        [SerializeField] private Text statusMessageText;

        // ─── Runtime ─────────────────────────────────────────────────────────────

        private FloorLoader _loader;
        private bool _isLoading;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            if (!ValidateRefs()) return;

            // FloorLoader dùng constructor injection — không FindObjectOfType
            _loader = new FloorLoader(worldStateProxy.Service, floorDatabase);

            _ = PopulateFloorListAsync();
        }

        // ─── Populate ─────────────────────────────────────────────────────────────

        /// <summary>Lấy trạng thái từng tầng và dựng UI list.</summary>
        private async Task PopulateFloorListAsync()
        {
            ClearButtons();
            SetStatus(string.Empty);

            IReadOnlyList<FloorDefinition> allFloors = floorDatabase.GetAllFloors();

            Dictionary<string, FloorStatus> statuses;
            try
            {
                statuses = await worldStateProxy.Service.GetAllFloorStatusesAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Floor] PopulateFloorListAsync: không lấy được floor statuses: {ex.Message}");
                SetStatus("Không thể tải trạng thái tầng. Thử lại sau.");
                return;
            }

            foreach (var floor in allFloors)
            {
                if (floor == null) continue;

                FloorStatus status = FloorStatus.Locked;
                if (statuses != null)
                    statuses.TryGetValue(floor.id, out status);

                SpawnFloorButton(floor, status);
            }
        }

        private void SpawnFloorButton(FloorDefinition floor, FloorStatus status)
        {
            if (floorButtonPrefab == null || floorButtonContainer == null) return;

            GameObject go = Instantiate(floorButtonPrefab, floorButtonContainer);
            var item = go.GetComponent<FloorButtonItem>();
            if (item == null)
            {
                Debug.LogWarning($"[Floor] floorButtonPrefab thiếu component FloorButtonItem.");
                return;
            }

            item.Setup(floor, status, OnFloorSelected);
        }

        private void ClearButtons()
        {
            if (floorButtonContainer == null) return;
            foreach (Transform child in floorButtonContainer)
                Destroy(child.gameObject);
        }

        // ─── Chọn tầng ───────────────────────────────────────────────────────────

        /// <summary>Callback khi player nhấn nút chọn tầng.</summary>
        private void OnFloorSelected(FloorDefinition floor)
        {
            if (_isLoading) return;
            _ = HandleFloorSelectedAsync(floor.id);
        }

        private async Task HandleFloorSelectedAsync(string floorId)
        {
            _isLoading = true;
            SetStatus("Đang kiểm tra...");

            FloorLoadResult result = await _loader.LoadFloorAsync(floorId);

            _isLoading = false;

            switch (result.Status)
            {
                case FloorLoadStatus.Loaded:
                    SetStatus(string.Empty);
                    // UI gọi SceneLoader — không chứa logic game
                    SceneLoader.Load(result.Floor.sceneKey);
                    break;

                case FloorLoadStatus.Locked:
                    SetStatus("Tầng này đang bị khóa.");
                    Debug.Log($"[Floor] Tầng '{floorId}' bị khóa — không load scene.");
                    break;

                case FloorLoadStatus.NotFound:
                    SetStatus("Tầng không tồn tại.");
                    Debug.LogWarning($"[Floor] Tầng '{floorId}' không tồn tại trong database.");
                    break;

                case FloorLoadStatus.Error:
                    SetStatus($"Lỗi: {result.ErrorMessage}");
                    Debug.LogError($"[Floor] Lỗi load tầng '{floorId}': {result.ErrorMessage}");
                    break;
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void SetStatus(string message)
        {
            if (statusMessageText != null)
                statusMessageText.text = message;
        }

        private bool ValidateRefs()
        {
            if (floorDatabase == null)
            {
                Debug.LogError("[Floor] FloorSelectorUI: floorDatabase chưa được assign!");
                return false;
            }
            if (worldStateProxy == null || worldStateProxy.Service == null)
            {
                Debug.LogError("[Floor] FloorSelectorUI: worldStateProxy/Service chưa được assign!");
                return false;
            }
            return true;
        }
    }
}
