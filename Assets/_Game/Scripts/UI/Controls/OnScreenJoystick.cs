using Game.Core.Input;
using Game.Core.Settings;
using Game.UI.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Controls
{
    /// <summary>
    /// Joystick cảm ứng. P9.6: có dead zone và độ nhạy từ Settings (qua <see cref="JoystickMath"/>, nguồn dead zone DUY NHẤT),
    /// và hỗ trợ chế độ Floating khi có <see cref="JoystickTouchZone"/> điều khiển. Giữ API cũ (AssignInputReader).
    /// Chỉ một pointer tại một thời điểm, nên dùng được cùng lúc với nút Đánh/Né/Technique.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class OnScreenJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Image handle;
        [SerializeField] private float handleRange = 70f;
        [SerializeField] private MonoBehaviour inputReaderComponent;

        private RectTransform _bgRect;
        private RectTransform _handleRect;
        private IInputReader _inputReader;
        private Vector2 _currentInput;
        private Camera _uiCamera;

        private Vector2 _homeAnchoredPosition;
        private bool _active;
        private int _activePointerId;
        private float _deadZone = SettingsRules.DefaultDeadZone;
        private float _sensitivity = SettingsRules.DefaultSensitivity;
        private JoystickMode _mode = SettingsRules.DefaultJoystick;

        /// <summary>Kiểu joystick đang chọn trong Settings.</summary>
        public JoystickMode Mode => _mode;
        public bool IsFloatingMode => _mode == JoystickMode.Floating;
        public bool IsActive => _active;

        private void Awake()
        {
            _bgRect = background != null ? background.rectTransform : GetComponent<RectTransform>();
            _handleRect = handle != null ? handle.rectTransform : null;
            // Lưu theo anchoredPosition (tính từ điểm neo), KHÔNG dùng localPosition: localPosition tính từ tâm vùng cha và vùng cha
            // còn chưa được CanvasScaler co giãn lúc Awake, nên sẽ sai khi Canvas đổi kích thước (lỗi joystick nhảy vào giữa màn hình).
            _homeAnchoredPosition = _bgRect.anchoredPosition;
            _currentInput = Vector2.zero;

            if (inputReaderComponent is IInputReader reader)
            {
                _inputReader = reader;
            }
        }

        private void OnEnable()
        {
            ApplySettings(SettingsRuntime.Current);
            SettingsRuntime.Changed += ApplySettings;
        }

        public void AssignInputReader(IInputReader reader)
        {
            _inputReader = reader;
            if (_inputReader != null)
            {
                _inputReader.SetMoveInput(_currentInput);
            }
        }

        private void OnDisable()
        {
            SettingsRuntime.Changed -= ApplySettings;
            ReleaseJoystick();
        }

        private void ApplySettings(PlayerSettings settings)
        {
            _deadZone = settings.DeadZone;
            _sensitivity = settings.Sensitivity;
            _mode = settings.Joystick;
            // Đổi sang kiểu Cố định khi đang rảnh thì đưa nền về vị trí gốc.
            if (!IsFloatingMode && !_active) ResetBackgroundToHome();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            BeginInteraction(eventData, false);
        }

        /// <summary>
        /// Bắt đầu kéo với pointer này. <paramref name="moveToTouch"/> = true (Floating): đưa nền joystick tới điểm chạm.
        /// Trả false nếu đang bận với pointer khác (joystick chỉ nhận MỘT pointer).
        /// </summary>
        public bool BeginInteraction(PointerEventData eventData, bool moveToTouch)
        {
            if (_bgRect == null || eventData == null) return false;
            if (_active && eventData.pointerId != _activePointerId) return false;

            _active = true;
            _activePointerId = eventData.pointerId;
            _uiCamera = GetUICamera(eventData);

            if (moveToTouch) MoveBackgroundToScreenPoint(eventData.position, _uiCamera);

            UpdateFromPointer(eventData);
            return true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_bgRect == null || !_active || eventData.pointerId != _activePointerId) return;
            UpdateFromPointer(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_active || eventData == null || eventData.pointerId != _activePointerId) return;
            ReleaseJoystick();
        }

        private void UpdateFromPointer(PointerEventData eventData)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _bgRect, eventData.position, GetUICamera(eventData), out localPoint);

            // Tay cầm chỉ là hình ảnh: luôn kẹp trong handleRange. Đầu ra đi qua JoystickMath (dead zone + độ nhạy).
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
            if (_handleRect != null)
            {
                _handleRect.anchoredPosition = clamped;
            }
            _currentInput = JoystickMath.Apply(localPoint, handleRange, _deadZone, _sensitivity);
            _inputReader?.SetMoveInput(_currentInput);
        }

        private void MoveBackgroundToScreenPoint(Vector2 screenPoint, Camera cam)
        {
            var parent = _bgRect.parent as RectTransform;
            if (parent == null) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, cam, out local)) return;

            // Kẹp để nền joystick không tràn ra ngoài vùng cha (chừa nửa bề rộng nền).
            Rect rect = parent.rect;
            Vector2 half = _bgRect.rect.size * 0.5f;
            float marginX = Mathf.Min(half.x, rect.width * 0.5f);
            float marginY = Mathf.Min(half.y, rect.height * 0.5f);
            local.x = Mathf.Clamp(local.x, rect.xMin + marginX, rect.xMax - marginX);
            local.y = Mathf.Clamp(local.y, rect.yMin + marginY, rect.yMax - marginY);

            _bgRect.localPosition = new Vector3(local.x, local.y, _bgRect.localPosition.z);
        }

        private void ResetBackgroundToHome()
        {
            if (_bgRect != null) _bgRect.anchoredPosition = _homeAnchoredPosition;
        }

        private void ReleaseJoystick()
        {
            _active = false;
            if (_handleRect != null) _handleRect.anchoredPosition = Vector2.zero;
            _currentInput = Vector2.zero;
            _inputReader?.SetMoveInput(Vector2.zero);
            // Floating: nhả thì nền về vị trí gốc (đúng chỗ người chơi thấy lúc đầu).
            ResetBackgroundToHome();
        }

        private Camera GetUICamera(PointerEventData data)
        {
            if (data != null && data.pressEventCamera != null) return data.pressEventCamera;
            if (_uiCamera != null) return _uiCamera;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return canvas.worldCamera;
            }
            return null;
        }
    }
}
