using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Inventory
{
    /// <summary>
    /// Nút trên màn hình để bật/tắt túi đồ (Android dùng chạm, không có bàn phím).
    /// Chỉ gọi InventoryPanel.Toggle() — không chứa logic inventory.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class InventoryToggleButton : MonoBehaviour
    {
        [SerializeField] private InventoryPanel target;

        private void Awake()
        {
            if (target == null) target = FindAnyObjectByType<InventoryPanel>();
            Button button = GetComponent<Button>();
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (target != null) target.Toggle();
        }
    }
}
