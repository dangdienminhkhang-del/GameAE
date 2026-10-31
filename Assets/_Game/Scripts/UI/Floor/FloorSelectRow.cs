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
    /// Trạng thái có màu riêng (xanh = sẵn sàng, vàng = đã qua, xám = chưa mở) để nhìn qua là biết.
    /// </summary>
    public class FloorSelectRow : MonoBehaviour
    {
        private static readonly Color StatusReady = new Color(0.50f, 0.92f, 0.60f);
        private static readonly Color StatusCleared = new Color(0.96f, 0.82f, 0.42f);
        private static readonly Color StatusLocked = new Color(0.62f, 0.64f, 0.70f);
        private static readonly Color RangeColor = new Color(0.72f, 0.80f, 0.92f);

        [SerializeField] private Text floorNameLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text levelRangeLabel;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject lockedOverlay;

        private FloorSelectEntry _entry;
        private Action<FloorSelectEntry> _onSelected;

        private void Awake()
        {
            var rowImg = GetComponent<Image>();
            if (rowImg != null && UiSkin.Row(rowImg))
            {
                // Sprite hàng đã có màu thép riêng → để màu gần trắng, không nhuộm tối như trước.
                rowImg.color = new Color(1f, 1f, 1f, 0.96f);
            }

            if (selectButton != null)
            {
                selectButton.onClick.AddListener(HandleClick);
                var btnImg = selectButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = new Color(0.20f, 0.58f, 0.86f, 1f);
                    UiSkin.Button(btnImg, 2);
                }

                ColorBlock cb = selectButton.colors;
                cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
                cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
                cb.disabledColor = new Color(0.55f, 0.55f, 0.60f, 0.8f);
                selectButton.colors = cb;

                Text btnText = selectButton.GetComponentInChildren<Text>();
                if (btnText != null) UiSkin.ApplyFont(btnText, true);
            }

            if (floorNameLabel != null) UiSkin.ApplyFont(floorNameLabel, true);
            if (levelRangeLabel != null)
            {
                UiSkin.ApplyFont(levelRangeLabel);
                levelRangeLabel.color = RangeColor;
            }
            if (statusLabel != null) UiSkin.ApplyFont(statusLabel, true);
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
            {
                statusLabel.text = entry.Status switch
                {
                    FloorStatus.Available => "Sẵn sàng",
                    FloorStatus.Cleared   => "Đã qua",
                    FloorStatus.Locked    => "Chưa mở",
                    _                     => string.Empty
                };
                statusLabel.color = entry.Status switch
                {
                    FloorStatus.Available => StatusReady,
                    FloorStatus.Cleared   => StatusCleared,
                    _                     => StatusLocked
                };
            }

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
