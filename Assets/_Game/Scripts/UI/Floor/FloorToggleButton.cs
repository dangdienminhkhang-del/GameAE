using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Floor
{
    /// <summary>
    /// Nút trên HUD để mở/đóng bảng Chọn Tầng (Android dùng chạm).
    /// Chỉ gọi FloorSelectPanel.Toggle() — không chứa logic Floor.
    /// Lưu ý: GameObject chứa FloorSelectPanel phải ACTIVE để FindAnyObjectByType tìm thấy.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class FloorToggleButton : MonoBehaviour
    {
        [SerializeField] private FloorSelectPanel target;

        private void Awake()
        {
            if (target == null) target = FindAnyObjectByType<FloorSelectPanel>();
            Button button = GetComponent<Button>();
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            Button button = GetComponent<Button>();
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            if (target == null) target = FindAnyObjectByType<FloorSelectPanel>();
            if (target != null) target.Toggle();
            else Debug.LogWarning("[Floor] FloorToggleButton: không tìm thấy FloorSelectPanel trong scene.", this);
        }
    }
}
