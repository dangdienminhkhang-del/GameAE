using UnityEngine;

namespace Game.UI.Common
{
    /// <summary>
    /// Gắn lên SafeAreaRoot (con trực tiếp của Canvas).
    /// Đặt anchor của RectTransform theo Screen.safeArea và tự cập nhật khi xoay màn hình hoặc đổi độ phân giải.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2 _lastScreenSize;
        private bool _isApplying;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        private void Awake()
        {
            ApplySafeArea();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            Rect safeArea = Screen.safeArea;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (safeArea != _lastSafeArea || screenSize != _lastScreenSize)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (_isApplying) return;
            if (RectTransform == null) return;

            try
            {
                _isApplying = true;
                Rect safeArea = Screen.safeArea;
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);

                SafeAreaMath.CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

                if (_rectTransform.anchorMin != anchorMin) _rectTransform.anchorMin = anchorMin;
                if (_rectTransform.anchorMax != anchorMax) _rectTransform.anchorMax = anchorMax;
                if (_rectTransform.offsetMin != Vector2.zero) _rectTransform.offsetMin = Vector2.zero;
                if (_rectTransform.offsetMax != Vector2.zero) _rectTransform.offsetMax = Vector2.zero;

                _lastSafeArea = safeArea;
                _lastScreenSize = screenSize;
            }
            finally
            {
                _isApplying = false;
            }
        }
    }
}
