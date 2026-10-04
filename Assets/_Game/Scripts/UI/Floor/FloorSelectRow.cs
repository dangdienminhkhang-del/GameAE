using System;
using Game.Core.Floor;
using Game.Core.World;
using Game.PvE.Floor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Floor
{
    /// <summary>
    /// Một dòng trong danh sách chọn tầng.
    /// UI chỉ hiển thị + gọi callback — không tự load scene.
    /// Lưu ý tạm dùng UnityEngine.UI.Text thay TMP_Text (cần asmdef Game.UI reference Unity.TextMeshPro).
    /// </summary>
    public class FloorSelectRow : MonoBehaviour
    {
        [SerializeField] private Text floorNameLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text levelRangeLabel;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject lockedOverlay;

        private FloorSelectEntry _entry;
        private Action<FloorSelectEntry> _onSelected;

        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(HandleClick);
        }

        public void Bind(FloorSelectEntry entry, Action<FloorSelectEntry> onSelected)
        {
            _entry = entry;
            _onSelected = onSelected;

            var def = entry.Definition;

            if (floorNameLabel != null)
                floorNameLabel.text = def.displayName;

            if (levelRangeLabel != null)
                levelRangeLabel.text = $"Lv {def.recommendedLevelMin}–{def.recommendedLevelMax}";

            if (statusLabel != null)
                statusLabel.text = entry.Status switch
                {
                    FloorStatus.Available => "Sẵn sàng",
                    FloorStatus.Cleared   => "Đã qua",
                    FloorStatus.Locked    => "Chưa mở",
                    _                     => string.Empty
                };

            bool locked = !entry.IsPlayable;
            if (selectButton != null) selectButton.interactable = !locked;
            if (lockedOverlay != null) lockedOverlay.SetActive(locked);
        }

        private void HandleClick()
        {
            if (_entry != null && _entry.IsPlayable)
                _onSelected?.Invoke(_entry);
        }
    }
}
