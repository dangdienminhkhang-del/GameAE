using Game.Core.Combat;
using Game.PvE.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.HUD
{
    /// <summary>
    /// Hiển thị chuỗi Combo và Xếp Hạng Đẳng Cấp (D -> SSS) trên màn hình:
    /// - Tự động tìm ComboTracker hoặc dựng UI động nếu chưa có sẵn trong Canvas.
    /// - Hiệu ứng nảy chữ (scale punch) mỗi khi combo tăng.
    /// - Màu sắc rực rỡ theo từng bậc Rank.
    /// </summary>
    public class ComboHUD : MonoBehaviour
    {
        [Header("Giao diện (Tự dựng nếu để trống)")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Text comboCountText;
        [SerializeField] private Text rankBadgeText;
        [SerializeField] private Image decayFillBar;

        [Header("Hiệu ứng")]
        [SerializeField] private float punchScale = 1.3f;
        [SerializeField] private float punchReturnSpeed = 6f;

        private ComboTracker _tracker;
        private CanvasGroup _canvasGroup;
        private float _currentScale = 1f;

        private static readonly Color ColorD = new Color(0.8f, 0.8f, 0.8f, 1f);
        private static readonly Color ColorC = new Color(0.4f, 1f, 0.4f, 1f);
        private static readonly Color ColorB = new Color(0.3f, 0.8f, 1f, 1f);
        private static readonly Color ColorA = new Color(0.85f, 0.4f, 1f, 1f);
        private static readonly Color ColorS = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color ColorSSS = new Color(1f, 0.25f, 0.25f, 1f);

        private void Awake()
        {
            if (rootPanel == null)
            {
                BuildRuntimeUI();
            }

            if (rootPanel != null)
            {
                _canvasGroup = rootPanel.GetComponent<CanvasGroup>();
                if (_canvasGroup == null) _canvasGroup = rootPanel.AddComponent<CanvasGroup>();
                _canvasGroup.alpha = 0f;
            }
        }

        private void Start()
        {
            _tracker = ComboTracker.Instance ?? FindFirstObjectByType<ComboTracker>();
            if (_tracker != null)
            {
                _tracker.ComboChanged += HandleComboChanged;
                _tracker.ComboReset += HandleComboReset;
            }
        }

        private void OnDestroy()
        {
            if (_tracker != null)
            {
                _tracker.ComboChanged -= HandleComboChanged;
                _tracker.ComboReset -= HandleComboReset;
            }
        }

        private void Update()
        {
            if (_tracker == null)
            {
                _tracker = ComboTracker.Instance ?? FindFirstObjectByType<ComboTracker>();
                if (_tracker != null)
                {
                    _tracker.ComboChanged += HandleComboChanged;
                    _tracker.ComboReset += HandleComboReset;
                }
                return;
            }

            if (_tracker.ComboCount > 1)
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, 1f, UnityEngine.Time.deltaTime * 8f);
                }

                if (decayFillBar != null)
                {
                    decayFillBar.fillAmount = _tracker.DecayRatio;
                }
            }
            else
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, 0f, UnityEngine.Time.deltaTime * 4f);
                }
            }

            // Scale punch return
            if (_currentScale > 1f && rootPanel != null)
            {
                _currentScale = Mathf.MoveTowards(_currentScale, 1f, UnityEngine.Time.deltaTime * punchReturnSpeed);
                rootPanel.transform.localScale = Vector3.one * _currentScale;
            }
        }

        private void HandleComboChanged(int count, ComboRank rank)
        {
            if (count <= 1) return;

            if (comboCountText != null)
            {
                comboCountText.text = $"{count}x";
            }

            if (rankBadgeText != null)
            {
                rankBadgeText.text = rank != ComboRank.None ? $"RANK {rank}" : "";
                rankBadgeText.color = GetRankColor(rank);
            }

            // Punch scale
            _currentScale = punchScale;
            if (rootPanel != null)
            {
                rootPanel.transform.localScale = Vector3.one * _currentScale;
            }
        }

        private void HandleComboReset()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private Color GetRankColor(ComboRank rank)
        {
            switch (rank)
            {
                case ComboRank.SSS: return ColorSSS;
                case ComboRank.S:   return ColorS;
                case ComboRank.A:   return ColorA;
                case ComboRank.B:   return ColorB;
                case ComboRank.C:   return ColorC;
                default:            return ColorD;
            }
        }

        private void BuildRuntimeUI()
        {
            GameObject container = new GameObject("ComboHUD_Auto", typeof(RectTransform));
            container.transform.SetParent(transform, false);

            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.7f);
            rt.anchorMax = new Vector2(1f, 0.7f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-40f, 0f);
            rt.sizeDelta = new Vector2(160f, 90f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Text Combo
            GameObject comboObj = new GameObject("Text_Combo", typeof(RectTransform), typeof(Text));
            comboObj.transform.SetParent(container.transform, false);
            RectTransform comboRt = comboObj.GetComponent<RectTransform>();
            comboRt.anchorMin = new Vector2(0f, 0.4f);
            comboRt.anchorMax = new Vector2(1f, 1f);
            comboRt.offsetMin = Vector2.zero;
            comboRt.offsetMax = Vector2.zero;
            comboCountText = comboObj.GetComponent<Text>();
            comboCountText.font = font;
            comboCountText.fontSize = 32;
            comboCountText.fontStyle = FontStyle.Bold;
            comboCountText.alignment = TextAnchor.MiddleRight;
            comboCountText.color = Color.white;

            // Text Rank
            GameObject rankObj = new GameObject("Text_Rank", typeof(RectTransform), typeof(Text));
            rankObj.transform.SetParent(container.transform, false);
            RectTransform rankRt = rankObj.GetComponent<RectTransform>();
            rankRt.anchorMin = new Vector2(0f, 0f);
            rankRt.anchorMax = new Vector2(1f, 0.4f);
            rankRt.offsetMin = Vector2.zero;
            rankRt.offsetMax = Vector2.zero;
            rankBadgeText = rankObj.GetComponent<Text>();
            rankBadgeText.font = font;
            rankBadgeText.fontSize = 20;
            rankBadgeText.fontStyle = FontStyle.Bold;
            rankBadgeText.alignment = TextAnchor.MiddleRight;
            rankBadgeText.color = ColorS;

            rootPanel = container;
        }
    }
}
