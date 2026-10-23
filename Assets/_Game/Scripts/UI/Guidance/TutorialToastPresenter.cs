using Game.Core.Guidance;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Guidance
{
    /// <summary>
    /// Toast chữ ngắn của hướng dẫn (Phase 9.9). Tự dựng UI bằng code (uGUI Text + CanvasGroup mờ vào/ra), nằm ở trên giữa màn hình,
    /// không che nút điều khiển, không nhận cảm ứng (raycastTarget tắt), chạy bằng thời gian THẬT (unscaled) nên Pause không làm kẹt.
    /// Tách hẳn khỏi bảng báo boss P8-5: Canvas riêng, đối tượng riêng, không đụng chữ của bảng đó.
    /// Chỉ hiển thị: việc chọn hint và đánh dấu "đã xem" nằm ở <see cref="TutorialRules"/>.
    /// </summary>
    public sealed class TutorialToastPresenter : MonoBehaviour, ITutorialToastSink
    {
        private const float PaddingPixels = 24f;
        private static readonly Color BackgroundColor = new Color(0.05f, 0.07f, 0.12f, 0.82f);
        private static readonly Color TextColor = Color.white;

        [Tooltip("Để trống = dùng giá trị mặc định có tên trong GuidanceConfig/code.")]
        [SerializeField] private GuidanceConfig config;

        private CanvasGroup _group;
        private Text _label;
        private float _targetAlpha;
        private float _fadeSeconds = 0.25f;
        private bool _built;

        public bool IsShowing => _targetAlpha > 0f;

        public void SetConfig(GuidanceConfig value)
        {
            config = value;
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnDisable()
        {
            // Tắt component thì ẩn ngay, không để toast kẹt trên màn hình.
            _targetAlpha = 0f;
            if (_group != null) _group.alpha = 0f;
        }

        private void Update()
        {
            if (_group == null) return;

            float step = UnityEngine.Time.unscaledDeltaTime / Mathf.Max(0.01f, _fadeSeconds);
            _group.alpha = Mathf.MoveTowards(_group.alpha, _targetAlpha, step);
        }

        public bool TryShow(TutorialHint hint)
        {
            if (!isActiveAndEnabled) return false;
            EnsureBuilt();
            if (_label == null) return false;

            _label.text = string.IsNullOrEmpty(hint.SpeakerLabel) ? hint.Text : hint.SpeakerLabel + ": " + hint.Text;
            _targetAlpha = 1f;
            return true;
        }

        public void Hide()
        {
            _targetAlpha = 0f;
        }

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            float width = config != null ? config.ToastWidth : 1100f;
            float height = config != null ? config.ToastHeight : 90f;
            float topOffset = config != null ? config.ToastTopOffset : 220f;
            int fontSize = config != null ? config.ToastFontSize : 36;
            _fadeSeconds = config != null ? config.ToastFadeSeconds : 0.25f;

            Canvas canvas = GuidanceCanvasFactory.Create(transform, "TutorialToastCanvas", GuidanceCanvasFactory.ToastSortingOrder);

            var root = new GameObject("Toast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);

            var rt = (RectTransform)root.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -topOffset);
            rt.sizeDelta = new Vector2(width, height);

            var background = root.GetComponent<Image>();
            background.color = BackgroundColor; if (UiSkin.Panel(background, 2)) background.color = new Color(1f, 1f, 1f, 0.95f);
            background.raycastTarget = false;

            _group = root.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(root.transform, false);

            _label = textGo.GetComponent<Text>();
            _label.font = GuidanceCanvasFactory.DefaultFont();
            _label.fontSize = fontSize;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = TextColor;
            _label.raycastTarget = false;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Truncate;

            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(PaddingPixels, 0f);
            textRt.offsetMax = new Vector2(-PaddingPixels, 0f);
        }
    }
}
