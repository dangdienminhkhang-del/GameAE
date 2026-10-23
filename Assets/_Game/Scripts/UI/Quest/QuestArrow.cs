using Game.Core.Events;
using Game.Core.Guidance;
using Game.Core.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Quest
{
    /// <summary>
    /// Mũi tên quest ở mép màn hình (Phase 9.9). Toán nằm ở <see cref="QuestArrowMath"/> (thuần, có test, đúng ở mọi tỉ lệ khung vì chỉ dùng
    /// hình học trên Screen.safeArea). Đích lấy từ <see cref="QuestTargetResolver"/> (đã cache, không Find mỗi frame).
    /// ẨN khi: không có quest/đích, mục tiêu đang trong khung nhìn (cộng lề), người chơi đã tới gần, đang Pause, người chơi đã chết.
    /// Mũi tên KHÔNG chặn cảm ứng, không có số khoảng cách, không bắt buộc làm theo. Tự dựng UI bằng code trên Canvas riêng.
    /// Hình mũi tên mặc định vẽ bằng code (tam giác chỉ sang phải); có thể gán Sprite riêng (phải vẽ chỉ sang PHẢI vì góc 0 = +X).
    /// </summary>
    public sealed class QuestArrow : MonoBehaviour
    {
        private const int CanvasSortingOrder = 800;
        private const int GeneratedSpriteSize = 64;
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private const float ReferenceHeight = 1080f;

        [SerializeField] private GuidanceConfig config;
        [SerializeField] private QuestTargetResolver resolver;
        [Tooltip("Sprite mũi tên chỉ sang phải. Để trống = tự vẽ một tam giác.")]
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private Camera worldCamera;

        private RectTransform _arrowRect;
        private Image _arrowImage;
        private GameObject _canvasRoot;
        private Sprite _generatedSprite;
        private bool _playerDead;

        /// <summary>Mũi tên đang hiện (test và công cụ chỉ đọc).</summary>
        public bool IsShowing => _arrowImage != null && _arrowImage.enabled;

        private void Awake()
        {
            if (resolver == null && !TryGetComponent(out resolver)) resolver = FindAnyObjectByType<QuestTargetResolver>();
            BuildUi();
            SetShown(false);
        }

        private void OnEnable()
        {
            _playerDead = false;
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            SetShown(false);
        }

        private void OnDestroy()
        {
            if (_generatedSprite != null)
            {
                Destroy(_generatedSprite.texture);
                Destroy(_generatedSprite);
            }
        }

        private void HandlePlayerDied(object sender, Game.Core.Combat.DamageInfo lastHit) => _playerDead = true;

        private void LateUpdate()
        {
            if (_arrowRect == null) return;

            if (resolver == null || !resolver.HasTarget || _playerDead || PlaySpeedState.Shared.IsPaused)
            {
                SetShown(false);
                return;
            }

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null)
            {
                SetShown(false);
                return;
            }

            QuestArrowSettings settings = config != null ? config.Arrow : QuestArrowSettings.Default;

            // Đã tới gần: ẩn.
            Transform player = resolver.PlayerTransform;
            if (player != null && QuestArrowMath.IsNear(player.position, resolver.TargetPosition, settings.ArrivalRadius))
            {
                SetShown(false);
                return;
            }

            // Lề cấu hình theo pixel của độ phân giải tham chiếu 1920x1080: co giãn theo chiều cao màn hình thật.
            float scale = Screen.height / ReferenceHeight;
            Vector3 screen = cam.WorldToScreenPoint(resolver.TargetPosition);
            QuestArrowResult result = QuestArrowMath.Compute(
                screen, Screen.safeArea, settings.EdgeMarginPixels * scale, settings.ViewMarginPixels * scale);

            if (!result.Visible)
            {
                SetShown(false);
                return;
            }

            float size = (config != null ? config.ArrowSizePixels : 96f) * scale;
            _arrowRect.sizeDelta = new Vector2(size, size);
            // Canvas Overlay: position tính bằng pixel màn hình, không phụ thuộc CanvasScaler.
            _arrowRect.position = new Vector3(result.ScreenPosition.x, result.ScreenPosition.y, 0f);
            _arrowRect.rotation = Quaternion.Euler(0f, 0f, result.AngleDegrees);
            SetShown(true);
        }

        private void SetShown(bool shown)
        {
            if (_arrowImage != null && _arrowImage.enabled != shown) _arrowImage.enabled = shown;
        }

        // ── Dựng UI bằng code ───────────────────────────────────────────────────────

        private void BuildUi()
        {
            _canvasRoot = new GameObject("QuestArrowCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _canvasRoot.transform.SetParent(transform, false);

            var canvas = _canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;
            // Không thêm GraphicRaycaster: mũi tên không bao giờ nhận chạm.

            var scaler = _canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrow.transform.SetParent(_canvasRoot.transform, false);
            _arrowRect = (RectTransform)arrow.transform;
            _arrowRect.anchorMin = Vector2.zero;
            _arrowRect.anchorMax = Vector2.zero;
            _arrowRect.pivot = new Vector2(0.5f, 0.5f);

            _arrowImage = arrow.GetComponent<Image>();
            _arrowImage.raycastTarget = false;
            _arrowImage.color = config != null ? config.ArrowColor : new Color(1f, 0.85f, 0.2f, 0.95f);
            _arrowImage.sprite = arrowSprite != null ? arrowSprite : GetGeneratedSprite();
            _arrowImage.preserveAspect = true;
        }

        private Sprite GetGeneratedSprite()
        {
            if (_generatedSprite != null) return _generatedSprite;

            int n = GeneratedSpriteSize;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "QuestArrowGenerated"
            };

            // Tam giác đặc chỉ sang phải: đỉnh ở cạnh phải giữa, đáy ở cạnh trái.
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                float halfSpan = (1f - (float)0) * 0f; // giữ biến cục bộ rõ ràng, tính ngay dưới
                for (int x = 0; x < n; x++)
                {
                    float t = x / (float)(n - 1);                  // 0 ở trái, 1 ở phải
                    halfSpan = (1f - t) * 0.5f;                    // nửa bề rộng (theo chiều cao) giảm dần về đỉnh
                    float v = Mathf.Abs(y / (float)(n - 1) - 0.5f);
                    pixels[y * n + x] = v <= halfSpan ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            _generatedSprite = Sprite.Create(texture, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
            return _generatedSprite;
        }
    }
}
