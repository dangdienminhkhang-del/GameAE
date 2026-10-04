using Game.Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Controls
{
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

        private void Awake()
        {
            _bgRect = background != null ? background.rectTransform : GetComponent<RectTransform>();
            _handleRect = handle != null ? handle.rectTransform : null;
            _currentInput = Vector2.zero;

            if (inputReaderComponent is IInputReader reader)
            {
                _inputReader = reader;
            }
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
            ReleaseJoystick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_bgRect == null) return;
            _uiCamera = GetUICamera(eventData);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_bgRect == null) return;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _bgRect, eventData.position, GetUICamera(eventData), out localPoint);

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
            if (_handleRect != null)
            {
                _handleRect.anchoredPosition = clamped;
            }
            _currentInput = new Vector2(
                clamped.x / handleRange,
                clamped.y / handleRange);
            _inputReader?.SetMoveInput(_currentInput);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ReleaseJoystick();
        }

        private void ReleaseJoystick()
        {
            if (_handleRect != null) _handleRect.anchoredPosition = Vector2.zero;
            _currentInput = Vector2.zero;
            _inputReader?.SetMoveInput(Vector2.zero);
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
