using UnityEngine;

namespace Game.UI.Common
{
    /// <summary>
    /// Tự động giới hạn kích thước panel ở giữa màn hình:
    /// size = min(thiết kế, 92% vùng an toàn) cả ngang lẫn dọc, tối đa rộng maxWidth khi có thể.
    /// Giúp các hộp 940x960 (Cài đặt) và 760x760 (Tạm dừng) không bao giờ bị tràn trên máy màn dài hoặc hẹp (Gói U3).
    ///
    /// Hỗ trợ cả neo "co giãn" (anchorMin != anchorMax): sizeDelta được bù theo kích thước của cha để KÍCH THƯỚC CUỐI
    /// đúng bằng giá trị đã kẹp. Trước đây sizeDelta bị cộng thêm vào phần neo co giãn nên Cửa hàng, Nấu ăn, Nhà trọ... phình to quá màn hình.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class PanelSizeClamp : MonoBehaviour
    {
        [SerializeField] private Vector2 designSize = new Vector2(860f, 760f);
        [SerializeField] private float maxRatio = UiLayoutConstants.MaxPanelScreenRatio;
        [SerializeField] private float maxWidth = UiLayoutConstants.MaxPanelWidth;

        private RectTransform _rectTransform;

        public Vector2 DesignSize
        {
            get => designSize;
            set { designSize = value; ApplyClamp(); }
        }

        public float MaxWidth
        {
            get => maxWidth;
            set { maxWidth = value; ApplyClamp(); }
        }

        /// <summary>Đặt cùng lúc kích thước thiết kế và bề rộng tối đa (panel rộng như Nhân vật, Cửa hàng).</summary>
        public void Configure(Vector2 size, float maxPanelWidth)
        {
            designSize = size;
            maxWidth = maxPanelWidth;
            ApplyClamp();
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (designSize == Vector2.zero && _rectTransform != null)
            {
                designSize = _rectTransform.sizeDelta;
            }
            ApplyClamp();
        }

        private void OnEnable()
        {
            ApplyClamp();
        }

        private void Update()
        {
            ApplyClamp();
        }

        public void ApplyClamp()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) return;

            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 canvasSize = SafeAreaMath.CalculateCanvasSize(screenSize, UiLayoutConstants.ReferenceResolution, UiLayoutConstants.MatchWidthOrHeight);
            Rect canvasSafeArea = SafeAreaMath.CalculateCanvasSafeArea(Screen.safeArea, screenSize, canvasSize);

            float availW = canvasSafeArea.width * maxRatio;
            float availH = canvasSafeArea.height * maxRatio;

            float clampedW = Mathf.Min(designSize.x, availW);
            if (maxWidth > 0f) clampedW = Mathf.Min(clampedW, maxWidth);
            float clampedH = Mathf.Min(designSize.y, availH);

            Vector2 clamped = new Vector2(Mathf.Round(clampedW), Mathf.Round(clampedH));

            // Bù phần neo co giãn: kích thước thật = parentSize * (anchorMax - anchorMin) + sizeDelta.
            Vector2 anchorSpan = _rectTransform.anchorMax - _rectTransform.anchorMin;
            Vector2 target = clamped;
            if ((anchorSpan.x != 0f || anchorSpan.y != 0f) && _rectTransform.parent is RectTransform parent)
            {
                Vector2 parentSize = parent.rect.size;
                target = new Vector2(
                    Mathf.Round(clamped.x - parentSize.x * anchorSpan.x),
                    Mathf.Round(clamped.y - parentSize.y * anchorSpan.y));
            }

            if (_rectTransform.sizeDelta != target)
            {
                _rectTransform.sizeDelta = target;
            }
        }
    }
}
