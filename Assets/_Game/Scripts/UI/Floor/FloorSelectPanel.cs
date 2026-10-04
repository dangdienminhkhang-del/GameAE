using System.Collections.Generic;
using Game.Core.Floor;
using Game.PvE.Floor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Floor
{
    /// <summary>
    /// Hiển thị danh sách tầng và cho player chọn — chỉ hiển thị + gửi ý định.
    /// Không chứa logic load scene hay check world state trực tiếp.
    /// Inject FloorSelector từ bootstrap.
    /// Lưu ý tạm dùng UnityEngine.UI.Text thay TMP_Text (package TextMeshPro chưa cài / asmdef Game.UI chưa ref Unity.TextMeshPro).
    /// </summary>
    public class FloorSelectPanel : MonoBehaviour
    {
        [Header("UI refs")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform listContainer;
        [SerializeField] private FloorSelectRow rowPrefab;

        [Header("Loading / Error")]
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private Text errorLabel;

        private FloorSelector _selector;
        private IFloorLoader _loader;
        private readonly List<FloorSelectRow> _rows = new List<FloorSelectRow>();

        // ── Inject ───────────────────────────────────────────────────────────

        public void Inject(FloorSelector selector, IFloorLoader loader)
        {
            _selector = selector;
            _loader = loader;
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        public void Show() => SetVisible(true);
        public void Hide() => SetVisible(false);

        private void SetVisible(bool visible)
        {
            if (panelRoot != null) panelRoot.SetActive(visible);
            if (visible) RefreshAsync();
        }

        // ── Refresh ──────────────────────────────────────────────────────────

        private async void RefreshAsync()
        {
            SetLoading(true);
            SetError(false, null);

            if (_selector == null)
            {
                SetLoading(false);
                SetError(true, "FloorSelector chưa được inject.");
                return;
            }

            List<FloorSelectEntry> entries;
            try
            {
                entries = await _selector.GetSelectableFloorsAsync();
            }
            catch (System.Exception ex)
            {
                SetLoading(false);
                SetError(true, $"Không đọc được danh sách tầng: {ex.Message}");
                return;
            }

            SetLoading(false);
            PopulateList(entries);
        }

        private void PopulateList(List<FloorSelectEntry> entries)
        {
            // Clear rows cũ
            foreach (var row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();

            if (listContainer == null || rowPrefab == null) return;

            foreach (var entry in entries)
            {
                var row = Instantiate(rowPrefab, listContainer);
                row.Bind(entry, OnRowSelected);
                _rows.Add(row);
            }
        }

        private async void OnRowSelected(FloorSelectEntry entry)
        {
            if (_loader == null) return;
            if (!entry.IsPlayable) return;

            SetLoading(true);
            SetError(false, null);

            var result = await _loader.LoadFloorAsync(entry.Definition.id);

            SetLoading(false);

            // FloorLoadStatus chỉ có: Loaded, Locked, NotFound, Error (định nghĩa trong FloorLoadResult.cs)
            if (result.Status != FloorLoadStatus.Loaded)
            {
                string msg = result.Status switch
                {
                    FloorLoadStatus.Locked   => "Tầng chưa mở.",
                    FloorLoadStatus.NotFound => "Tầng không tồn tại.",
                    FloorLoadStatus.Error    => result.ErrorMessage ?? "Lỗi không xác định.",
                    _                        => result.ErrorMessage ?? "Lỗi không xác định."
                };
                SetError(true, msg);
                return;
            }

            // Load thành công → đóng panel
            Hide();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

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
