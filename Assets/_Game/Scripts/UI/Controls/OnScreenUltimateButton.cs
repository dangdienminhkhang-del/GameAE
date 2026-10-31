using Game.PvE.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Controls
{
    /// <summary>
    /// Nút bấm trên màn hình cảm ứng để kích hoạt Tuyệt Kỹ Nộ (Ultimate Skill):
    /// - Hiển thị vòng tròn tiến độ nạp Nộ Khí (0% -> 100%).
    /// - Khi đầy 100%, nút phát sáng vàng rực và đập nhịp báo hiệu sẵn sàng.
    /// - Chạm vào là tung ngay Tuyệt Kỹ "Vũ Điệu Cuồng Nộ"!
    /// </summary>
    public class OnScreenUltimateButton : MonoBehaviour
    {
        [Header("Giao diện")]
        [SerializeField] private Button button;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text labelText;
        [SerializeField] private Graphic glowGraphic;

        [Header("Màu sắc")]
        [SerializeField] private Color chargingColor = new Color(0.2f, 0.7f, 1f, 0.9f);
        [SerializeField] private Color readyColor = new Color(1f, 0.88f, 0.25f, 1f);

        private AdrenalineController _controller;
        private float _pulseTimer;
        private Image _bgImage;

        private void Awake()
        {
            if (button == null) TryGetComponent(out button);
            if (button == null)
            {
                BuildRuntimeUI();
            }

            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void Start()
        {
            _controller = AdrenalineController.Instance ?? FindFirstObjectByType<AdrenalineController>();
            if (_controller != null)
            {
                _controller.AdrenalineChanged += HandleAdrenalineChanged;
            }
            HandleAdrenalineChanged(0f);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
            if (_controller != null)
            {
                _controller.AdrenalineChanged -= HandleAdrenalineChanged;
            }
        }

        private void Update()
        {
            if (_controller == null)
            {
                _controller = AdrenalineController.Instance ?? FindFirstObjectByType<AdrenalineController>();
                if (_controller != null)
                {
                    _controller.AdrenalineChanged += HandleAdrenalineChanged;
                }
                return;
            }

            if (_controller.IsReady)
            {
                _pulseTimer += UnityEngine.Time.deltaTime * 4f;
                float scale = 1f + Mathf.PingPong(_pulseTimer, 0.12f);
                transform.localScale = Vector3.one * scale;

                if (_bgImage != null)
                {
                    _bgImage.color = Color.white;
                }
            }
            else
            {
                transform.localScale = Vector3.one;
                if (_bgImage != null)
                {
                    // Khi chưa đầy nộ: hiển thị mờ dịu mắt, không chói lóa
                    _bgImage.color = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                }
            }
        }

        private void HandleAdrenalineChanged(float val)
        {
            float ratio = _controller != null ? _controller.AdrenalineRatio : 0f;

            if (fillImage != null)
            {
                fillImage.enabled = ratio > 0.01f;
                fillImage.fillAmount = ratio;
                fillImage.color = _controller != null && _controller.IsReady ? readyColor : chargingColor;
            }

            if (labelText != null && _controller != null)
            {
                if (_controller.IsReady)
                {
                    labelText.text = "NỘ!";
                    labelText.color = new Color(1f, 0.95f, 0.4f, 1f);
                    labelText.fontSize = 28;
                }
                else if (ratio > 0.05f)
                {
                    labelText.text = $"{Mathf.FloorToInt(_controller.CurrentAdrenaline)}%";
                    labelText.color = new Color(0.85f, 0.9f, 1f, 0.85f);
                    labelText.fontSize = 24;
                }
                else
                {
                    labelText.text = "";
                }
            }
        }

        private void HandleClick()
        {
            if (_controller == null)
            {
                _controller = AdrenalineController.Instance ?? FindFirstObjectByType<AdrenalineController>();
            }

            if (_controller != null)
            {
                _controller.TryExecuteUltimate();
            }
        }

                private void BuildRuntimeUI()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // Đặt cân đối trong cụm chiến đấu (Gói U2: >= 140x140, cách góc <= 620px)
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-465f, 290f);
            rt.sizeDelta = new Vector2(144f, 144f);

            button = gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
            button.colors = colors;

            _bgImage = gameObject.AddComponent<Image>();

            // Nạp khung vòng hành động ActionRing_M cho Ultimate (kích thước 144)
            Sprite ringSprite = Resources.Load<Sprite>("UISkin/UI_ActionRing_M") ?? Resources.Load<Sprite>("UISkin/UI_Button");
            if (ringSprite != null)
            {
                _bgImage.sprite = ringSprite;
            }
            _bgImage.color = new Color(0.5f, 0.5f, 0.5f, 0.75f);
            glowGraphic = _bgImage;

            // Icon con Tuyệt Kỹ nằm TRÊN vòng, kích thước 96x96
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(96f, 96f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = Resources.Load<Sprite>("UISkin/Icon_Ultimate");
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Vòng tiến độ xoay tròn bao quanh nút (Circular Progress Ring)
            GameObject fillObj = new GameObject("FillRing", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(transform, false);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillImage = fillObj.GetComponent<Image>();

            Sprite fillSprite = Resources.Load<Sprite>("UISkin/Icon_UltimateRing") ?? Resources.Load<Sprite>("UISkin/UI_Cooldown");
            if (fillSprite != null)
            {
                fillImage.sprite = fillSprite;
            }
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillClockwise = true;
            fillImage.fillAmount = 0f;
            fillImage.color = chargingColor;
            fillImage.raycastTarget = false;
            fillImage.enabled = false;

            // Nhãn % nộ khí rõ nét đặt trên nút
            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            labelText = textObj.GetComponent<Text>();
            UiSkin.ApplyFont(labelText, bold: true);
            labelText.fontSize = 24;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.85f, 0.9f, 1f, 0.85f);
            labelText.raycastTarget = false;
            labelText.text = "";
        }
    }
}
