using System;
using Game.Core.Floor;
using Game.Core.World;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Floor
{
    /// <summary>
    /// Component gắn lên prefab nút tầng trong FloorSelectorUI.
    /// Hiển thị tên, trạng thái và xử lý nhấn.
    ///
    /// Gán qua Inspector trên prefab:
    ///   nameText, statusText, selectButton
    /// </summary>
    public class FloorButtonItem : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button selectButton;

        private FloorDefinition _floor;
        private Action<FloorDefinition> _onSelected;

        /// <summary>
        /// Khởi tạo button với thông tin tầng và callback.
        /// </summary>
        public void Setup(FloorDefinition floor, FloorStatus status, Action<FloorDefinition> onSelected)
        {
            _floor = floor;
            _onSelected = onSelected;

            if (nameText != null)
                nameText.text = floor.displayName;

            if (statusText != null)
                statusText.text = GetStatusLabel(status);

            if (selectButton != null)
            {
                selectButton.interactable = status != FloorStatus.Locked;
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnButtonClicked()
        {
            _onSelected?.Invoke(_floor);
        }

        private static string GetStatusLabel(FloorStatus status)
        {
            return status switch
            {
                FloorStatus.Locked    => "🔒 Bị khóa",
                FloorStatus.Available => "✅ Khả dụng",
                FloorStatus.Cleared   => "⭐ Đã xóa",
                _                     => "?"
            };
        }
    }
}
