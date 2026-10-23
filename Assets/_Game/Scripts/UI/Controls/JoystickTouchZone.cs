using Game.Core.Settings;
using Game.UI.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Controls
{
    /// <summary>
    /// Vùng cảm ứng trong suốt phủ NỬA TRÁI màn hình cho joystick nổi (Floating, P9.6).
    /// Chạm ở đâu thì nền joystick hiện ở đó, kéo để di chuyển, nhả thì nền về chỗ cũ.
    /// Chế độ Cố định: vùng này tự vô hiệu (không nhận chạm), joystick chạy như trước.
    /// Cách đặt trong Canvas: để vùng này NẰM DƯỚI các nút khác trong thứ tự vẽ (đặt trên cùng danh sách con = vẽ trước)
    /// để không chặn nút Đánh/Né/Technique; vùng không chứa logic gameplay.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class JoystickTouchZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        [SerializeField] private OnScreenJoystick joystick;
        [Tooltip("Ảnh trong suốt (alpha 0) của vùng. Chỉ bật Raycast Target khi chế độ Floating.")]
        [SerializeField] private Graphic raycastGraphic;

        private void Awake()
        {
            if (raycastGraphic == null) TryGetComponent(out raycastGraphic);
        }

        private void OnEnable()
        {
            Refresh(SettingsRuntime.Current);
            SettingsRuntime.Changed += Refresh;
        }

        private void OnDisable()
        {
            SettingsRuntime.Changed -= Refresh;
        }

        private void Refresh(PlayerSettings settings)
        {
            if (raycastGraphic != null) raycastGraphic.raycastTarget = settings.Joystick == JoystickMode.Floating;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // Joystick cần phản ứng ngay khi chạm, không đợi vượt ngưỡng kéo.
            eventData.useDragThreshold = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (joystick == null || !joystick.IsFloatingMode) return;
            joystick.BeginInteraction(eventData, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (joystick == null || !joystick.IsFloatingMode) return;
            joystick.OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (joystick == null) return;
            joystick.OnPointerUp(eventData);
        }
    }
}
