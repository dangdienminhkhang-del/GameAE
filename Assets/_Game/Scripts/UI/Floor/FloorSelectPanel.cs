using System;
using System.Collections.Generic;
using Game.Core.Floor;
using Game.PvE.Bootstrap;
using Game.PvE.Floor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Floor
{
    /// <summary>
    /// Hiển thị danh sách tầng và cho player chọn — chỉ hiển thị + gửi ý định.
    /// Không chứa logic load scene hay check world state trực tiếp.
    ///
    /// Phụ thuộc được lấy theo hướng UI → PvE: lần Show() đầu tiên panel tìm PvEBootstrap
    /// và đọc FloorDatabase / WorldState / FloorLoader (PvE không biết gì về UI).
    /// Inject(...) vẫn public để test hoặc bootstrap khác dùng.
    ///
    /// Lưu ý: GameObject chứa component này phải ACTIVE (FindAnyObjectByType không thấy object inactive);
    /// chỉ panelRoot được tắt mặc định.
    /// Tạm dùng UnityEngine.UI.Text thay TMP_Text (asmdef Game.UI chưa ref Unity.TextMeshPro).
    /// </summary>
    public class FloorSelectPanel : MonoBehaviour
    {
        [Header("UI refs")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform listContainer;
        [SerializeField] private FloorSelectRow rowPrefab;
        [SerializeField] private Button closeButton;

        [Header("Loading / Error")]
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private Text errorLabel;

        private FloorSelector _selector;
        private IFloorLoader _loader;
        private readonly List<FloorSelectRow> _rows = new List<FloorSelectRow>();

        private bool _busy;          // đang load tầng — chặn bấm đúp
        private int _refreshVersion; // bỏ kết quả refresh cũ nếu panel đã đóng/mở lại

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        // ── Inject ───────────────────────────────────────────────────────────

        public void Inject(FloorSelector selector, IFloorLoader loader)
        {
            _selector = selector;
            _loader = loader;
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);
        }

        public void Show() => SetVisible(true);
        public void Hide() => SetVisible(false);
        public void Toggle() => SetVisible(!IsOpen);

        private void SetVisible(bool visible)
        {
            if (panelRoot != null) panelRoot.SetActive(visible);

            if (visible)
            {
                RefreshAsync();
            }
            else
            {
                _refreshVersion++;       // vô hiệu hóa refresh đang chờ
                SetLoading(false);
                SetError(false, null);
            }
        }

        // ── Phụ thuộc ────────────────────────────────────────────────────────

        private bool TryResolveDependencies(out string problem)
        {
            problem = null;
            if (_selector != null && _loader != null) return true;

            var bootstrap = FindFirstObjectByType<PvEBootstrap>();
            if (bootstrap == null)
            {
                problem = "Không tìm thấy PvEBootstrap trong scene.";
                return false;
            }

            if (bootstrap.FloorDatabase == null || bootstrap.WorldState == null || bootstrap.FloorLoader == null)
            {
                problem = "Chưa gán FloorDatabase trong PvEBootstrap.";
                return false;
            }

            _selector = new FloorSelector(bootstrap.FloorDatabase, bootstrap.WorldState);
            _loader = bootstrap.FloorLoader;
            return true;
        }

        // ── Refresh ──────────────────────────────────────────────────────────

        private async void RefreshAsync()
        {
            int version = ++_refreshVersion;
            SetLoading(true);
            SetError(false, null);

            if (!TryResolveDependencies(out string problem))
            {
                Debug.LogError($"[Floor] FloorSelectPanel: {problem}", this);
                ClearRows();
                SetLoading(false);
                SetError(true, problem);
                return;
            }

            List<FloorSelectEntry> entries;
            try
            {
                entries = await _selector.GetSelectableFloorsAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Floor] Không đọc được danh sách tầng: {ex}", this);
                if (this == null || version != _refreshVersion) return;
                SetLoading(false);
                SetError(true, $"Không đọc được danh sách tầng: {ex.Message}");
                return;
            }

            if (this == null || version != _refreshVersion) return;

            SetLoading(false);
            PopulateList(entries);
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();
        }

        private void PopulateList(List<FloorSelectEntry> entries)
        {
            ClearRows();

            if (listContainer == null || rowPrefab == null)
            {
                Debug.LogError("[Floor] FloorSelectPanel: thiếu listContainer hoặc rowPrefab.", this);
                SetError(true, "Giao diện chọn tầng chưa được cấu hình.");
                return;
            }

            foreach (var entry in entries)
            {
                var row = Instantiate(rowPrefab, listContainer);
                row.Bind(entry, OnRowSelected);
                _rows.Add(row);
            }
        }

        // ── Chọn tầng ────────────────────────────────────────────────────────

        private async void OnRowSelected(FloorSelectEntry entry)
        {
            if (_busy || _loader == null || entry == null || !entry.IsPlayable) return;

            _busy = true;
            SetLoading(true);
            SetError(false, null);

            FloorLoadResult result;
            try
            {
                result = await _loader.LoadFloorAsync(entry.Definition.id);
            }
            catch (Exception ex)
            {
                _busy = false;
                Debug.LogError($"[Floor] Lỗi vào tầng '{entry.Definition.id}': {ex}", this);
                if (this == null) return;
                SetLoading(false);
                SetError(true, $"Không vào được tầng: {ex.Message}");
                return;
            }

            _busy = false;
            if (this == null) return;
            SetLoading(false);

            // FloorLoadStatus chỉ có: Loaded, Locked, NotFound, Error (định nghĩa trong FloorLoadResult.cs)
            if (result.Status != FloorLoadStatus.Loaded)
            {
                string msg = result.Status switch
                {
                    FloorLoadStatus.Locked   => "Tầng chưa mở.",
                    FloorLoadStatus.NotFound => "Tầng không tồn tại.",
                    _                        => CleanMessage(result.ErrorMessage)
                };
                SetError(true, msg);
                return;
            }

            // Vào tầng thành công (hoặc đó chính là tầng đang đứng) → đóng panel
            Hide();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string CleanMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return "Lỗi không xác định.";
            const string prefix = "[FloorLoader] ";
            return message.StartsWith(prefix, StringComparison.Ordinal)
                ? message.Substring(prefix.Length)
                : message;
        }

        private void SetLoading(bool active)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(active);
        }

        private void SetError(bool active, string message)
        {
            if (errorPanel != null) errorPanel.SetActive(active);
            if (errorLabel != null) errorLabel.text = message ?? string.Empty;
        }
    }
}
