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
                    _bgImage.color = new Color(0.6f, 0.65f, 0.7f, 0.55f);
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
                    labelText.fontSize = 13;
                }
                else if (ratio > 0.05f)
                {
                    labelText.text = $"{Mathf.FloorToInt(_controller.CurrentAdrenaline)}%";
                    labelText.color = new Color(0.85f, 0.9f, 1f, 0.85f);
                    labelText.fontSize = 11;
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

            // Đặt cân đối phía trên vòng cung kỹ năng (không che khuất bình thuốc hay joystick)
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-205f, 215f);
            rt.sizeDelta = new Vector2(48f, 48f);

            button = gameObject.AddComponent<Button>();
            _bgImage = gameObject.AddComponent<Image>();

            // Nạp sprite icon Tuyệt Kỹ hình tròn pixel-art
            Sprite ultSprite = Resources.Load<Sprite>("UISkin/Icon_Ultimate") ?? Resources.Load<Sprite>("UISkin/UI_Button");
            if (ultSprite != null)
            {
                _bgImage.sprite = ultSprite;
            }
            _bgImage.color = new Color(0.6f, 0.65f, 0.7f, 0.55f);
            glowGraphic = _bgImage;

            // Vòng tiến độ xoay tròn bao quanh nút (Circular Progress Ring)
            GameObject fillObj = new GameObject("FillRing", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(transform, false);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(-3f, -3f);
            fillRt.offsetMax = new Vector2(3f, 3f);
            fillImage = fillObj.GetComponent<Image>();

            Sprite ringSprite = Resources.Load<Sprite>("UISkin/Icon_UltimateRing");
            if (ringSprite != null)
            {
                fillImage.sprite = ringSprite;
            }
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillClockwise = true;
            fillImage.fillAmount = 0f;
            fillImage.color = chargingColor;
            fillImage.raycastTarget = false;
            fillImage.enabled = false;

            // Nhãn % nộ khí nhỏ gọn đặt phía dưới nút
            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, -0.4f);
            textRt.anchorMax = new Vector2(1f, 0.1f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            labelText = textObj.GetComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 11;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.85f, 0.9f, 1f, 0.85f);
            labelText.raycastTarget = false;
            labelText.text = "";
        }
    }
}
