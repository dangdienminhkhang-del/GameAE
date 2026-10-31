using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Controls
{
    /// <summary>
    /// Hiệu ứng "sống" cho nút cảm ứng trên HUD:
    /// lún khi bấm rồi nảy lại, thở nhẹ + quầng sáng khi sẵn sàng (tuỳ chọn),
    /// lóe vòng + nảy khi hồi chiêu xong, làm tối icon khi nút không dùng được.
    /// CHỈ HIỂN THỊ: không gọi gameplay, không đổi interactable hay cooldown (luật #3).
    /// Dùng thời gian không phụ thuộc timeScale nên nút vẫn nảy khi game đang tạm dừng.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HudButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const string FlashRingName = "ReadyFlash";
        private const string IconChildName = "Icon";

        // Độ cứng / độ giảm chấn của lò xo: damping ~ 0.45 của mức tới hạn, nảy lố khoảng 2-3%.
        private const float SpringStiffness = 600f;
        private const float SpringDamping = 22f;
        private const float MaxFrameStep = 0.05f;
        private const float BreathFadeSpeed = 2.5f;
        private const float GlowScale = 1.08f;

        [Header("Bấm")]
        [Tooltip("Tỉ lệ nút co lại khi đang giữ ngón tay (1 = không co).")]
        [SerializeField, Range(0.7f, 1f)] private float pressedScale = 0.85f;

        [Header("Hồi chiêu (tuỳ chọn)")]
        [Tooltip("Vòng đếm ngược của nút. Bỏ trống thì tự tìm con tên CooldownFill.")]
        [SerializeField] private Image cooldownFill;
        [Tooltip("Lóe vòng sáng và nảy nút một lần khi vòng đếm ngược vừa hết.")]
        [SerializeField] private bool flashWhenCooldownEnds;
        [SerializeField, Min(0.1f)] private float flashSeconds = 0.4f;
        [SerializeField, Range(0f, 40f)] private float popImpulse = 6f;
        [SerializeField] private Color flashColor = new Color(0.55f, 0.92f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float flashPeakAlpha = 0.9f;

        [Header("Sẵn sàng (thường chỉ bật cho nút kỹ thuật)")]
        [Tooltip("Khi nút dùng được: thở nhẹ và có quầng sáng nhấp nháy.")]
        [SerializeField] private bool readyBreathing;
        [SerializeField, Range(0f, 0.1f)] private float breathAmount = 0.035f;
        [SerializeField, Min(0.3f)] private float breathPeriodSeconds = 1.4f;
        [SerializeField, Range(0f, 1f)] private float glowMaxAlpha = 0.35f;

        [Header("Không dùng được")]
        [Tooltip("Làm tối icon (con tên Icon) khi nút bị khoá hoặc đang hồi chiêu.")]
        [SerializeField] private bool dimIconWhenDisabled = true;
        [SerializeField] private Color disabledIconTint = new Color(0.5f, 0.5f, 0.5f, 0.75f);

        private RectTransform _rect;
        private Selectable _selectable;
        private Image _icon;
        private Image _ring;

        private Vector3 _baseScale = Vector3.one;
        private Vector2 _appliedOffset;
        private float _lastMul = 1f;

        private float _scale = 1f;
        private float _velocity;
        private bool _held;

        private float _breathAmp;
        private bool _wasCooling;

        private bool _flashing;
        private float _flashT;

        private bool _dimmed;
        private Color _iconBaseColor;
        private Color _iconDimmedColor;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _selectable = GetComponent<Selectable>();

            Transform iconTf = transform.Find(IconChildName);
            if (iconTf != null) _icon = iconTf.GetComponent<Image>();

            if (cooldownFill == null)
            {
                Transform fillTf = transform.Find("CooldownFill");
                if (fillTf != null) cooldownFill = fillTf.GetComponent<Image>();
            }
            PrepareCooldownFill();
        }

        private void OnEnable()
        {
            _baseScale = _rect.localScale;
            _appliedOffset = Vector2.zero;
            _lastMul = 1f;
            _scale = 1f;
            _velocity = 0f;
            _held = false;
            _breathAmp = 0f;
            _flashing = false;
            _wasCooling = IsCoolingNow();
            if (_ring != null) _ring.enabled = false;
        }

        private void OnDisable()
        {
            if (_rect == null) return;
            _rect.localScale = _baseScale;
            _rect.anchoredPosition -= _appliedOffset;
            _appliedOffset = Vector2.zero;
            _lastMul = 1f;
            ApplyDim(false);
            if (_ring != null) _ring.enabled = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Nút đang khoá thì không lún: lún nghĩa là "đã nhận lệnh".
            if (_selectable == null || _selectable.IsInteractable()) _held = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _held = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _held = false;
        }

        private void Update()
        {
            float dt = Mathf.Min(UnityEngine.Time.unscaledDeltaTime, MaxFrameStep);
            bool interactable = _selectable == null || _selectable.IsInteractable();
            bool cooling = IsCoolingNow();

            if (flashWhenCooldownEnds && HudFeedbackCurves.ShouldFlash(_wasCooling, cooling))
            {
                TriggerReadyFlash();
            }
            _wasCooling = cooling;

            ApplyDim(dimIconWhenDisabled && !interactable);

            bool ready = interactable && !cooling;
            float breathTarget = (readyBreathing && ready && !_held) ? 1f : 0f;
            _breathAmp = Mathf.MoveTowards(_breathAmp, breathTarget, dt * BreathFadeSpeed);

            float target = _held ? pressedScale : 1f;
            HudFeedbackCurves.SpringStep(ref _scale, ref _velocity, target, SpringStiffness, SpringDamping, dt);

            float wave = HudFeedbackCurves.BreathWave(UnityEngine.Time.unscaledTime, breathPeriodSeconds);
            float mul = _scale * (1f + breathAmount * wave * _breathAmp);
            ApplyTransform(mul);

            UpdateRing(dt, wave);
        }

        private bool IsCoolingNow()
        {
            return cooldownFill != null && HudFeedbackCurves.IsCooling(cooldownFill.enabled, cooldownFill.fillAmount);
        }

        private void TriggerReadyFlash()
        {
            EnsureRing();
            _flashing = true;
            _flashT = 0f;
            _velocity += popImpulse;
        }

        private void UpdateRing(float dt, float wave)
        {
            if (!readyBreathing && !_flashing) return;
            EnsureRing();
            if (_ring == null) return;

            float alpha = glowMaxAlpha * _breathAmp * wave;
            float scale = GlowScale;

            if (_flashing)
            {
                _flashT += dt / flashSeconds;
                if (_flashT >= 1f)
                {
                    _flashing = false;
                }
                else
                {
                    float flashAlpha = HudFeedbackCurves.FlashRingAlpha(_flashT, flashPeakAlpha);
                    if (flashAlpha > alpha)
                    {
                        alpha = flashAlpha;
                        scale = HudFeedbackCurves.FlashRingScale(_flashT);
                    }
                }
            }

            _ring.enabled = alpha > 0.003f;
            if (!_ring.enabled) return;

            Color c = flashColor;
            c.a = alpha;
            _ring.color = c;
            _ring.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private void ApplyTransform(float mul)
        {
            if (Mathf.Abs(mul - _lastMul) < 0.00005f) return;
            _lastMul = mul;

            _rect.localScale = _baseScale * mul;

            // Giữ TÂM nút đứng yên dù pivot nằm ở góc (nút menu góc trên phải có pivot 1,1).
            Vector2 centerShift = (_rect.pivot - new Vector2(0.5f, 0.5f)) * (_rect.rect.size * _baseScale.x) * (mul - 1f);
            _rect.anchoredPosition += centerShift - _appliedOffset;
            _appliedOffset = centerShift;
        }

        private void ApplyDim(bool dim)
        {
            if (_icon == null) return;
            if (dim && !_dimmed)
            {
                _iconBaseColor = _icon.color;
                _iconDimmedColor = new Color(
                    _iconBaseColor.r * disabledIconTint.r,
                    _iconBaseColor.g * disabledIconTint.g,
                    _iconBaseColor.b * disabledIconTint.b,
                    _iconBaseColor.a);
                _icon.color = _iconDimmedColor;
                _dimmed = true;
            }
            else if (!dim && _dimmed)
            {
                // Chỉ trả màu cũ nếu không ai khác đã đổi màu icon trong lúc đó.
                if (_icon.color == _iconDimmedColor) _icon.color = _iconBaseColor;
                _dimmed = false;
            }
        }

        private void PrepareCooldownFill()
        {
            if (cooldownFill == null) return;
            if (cooldownFill.sprite == null) cooldownFill.sprite = GeneratedSprites.Disc;
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = (int)Image.Origin360.Top;
            cooldownFill.fillClockwise = false;
            cooldownFill.raycastTarget = false;
        }

        private void EnsureRing()
        {
            if (_ring != null) return;

            Transform existing = transform.Find(FlashRingName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(FlashRingName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (existing == null) go.transform.SetParent(transform, false);

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            _ring = go.GetComponent<Image>();
            _ring.sprite = GeneratedSprites.Ring;
            _ring.raycastTarget = false;
            _ring.enabled = false;
        }

        /// <summary>Sprite tròn / vòng tạo bằng code: không cần file ảnh, không phụ thuộc phong cách icon.</summary>
        private static class GeneratedSprites
        {
            private const int Size = 128;
            private static Sprite _disc;
            private static Sprite _ring;

            public static Sprite Disc
            {
                get
                {
                    if (_disc == null) _disc = Build(false);
                    return _disc;
                }
            }

            public static Sprite Ring
            {
                get
                {
                    if (_ring == null) _ring = Build(true);
                    return _ring;
                }
            }

            private static Sprite Build(bool ring)
            {
                var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
                {
                    name = ring ? "HudGeneratedRing" : "HudGeneratedDisc",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };

                var pixels = new Color32[Size * Size];
                float center = (Size - 1) * 0.5f;
                float outer = Size * 0.5f - 1f;
                float inner = outer - Size * 0.09f;

                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(outer - d + 0.5f);
                        if (ring) a = Mathf.Min(a, Mathf.Clamp01(d - inner + 0.5f));
                        pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply(false, true);

                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = tex.name;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }
        }
    }
}
