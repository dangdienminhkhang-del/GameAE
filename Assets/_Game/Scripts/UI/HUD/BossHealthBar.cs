using Game.Core.Combat;
using Game.Core.Presentation;
using Game.PvE.Boss;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.HUD
{
    /// <summary>
    /// Thanh máu boss trên HUD (docs/05 Phase 9.5 & SAO Multi-bar HP):
    /// - Chỉ hiện khi boss đã vào trận (EngagedChanged = true) và chưa chết.
    /// - Hiển thị nhiều thanh máu theo phong cách SAO (mặc định 4 thanh: Xanh lá -> Vàng -> Cam -> Đỏ thanh cuối).
    /// - Hiển thị nhãn số thanh còn lại (x4, x3, x2, x1).
    /// - Lớp màu bên dưới hiển thị màu thanh tiếp theo khi thanh hiện tại tụt dần.
    /// - Tự gắn runtime các thành phần phụ trợ (UnderlyingFill, BarsCountLabel, BossNameLabel) mà không làm bẩn scene.
    /// </summary>
    public sealed class BossHealthBar : MonoBehaviour
    {
        [Tooltip("Boss cần theo dõi (tham chiếu scene). Gán lại bằng code qua SetBoss nếu boss sinh động.")]
        [SerializeField] private BossController boss;
        [Tooltip("Phần hiển thị cần bật/tắt (thường là con chứa nền + thanh + tên). KHÔNG được là chính object chứa component này.")]
        [SerializeField] private GameObject visualRoot;
        [Tooltip("Image thanh máu. Type=Filled thì dùng fillAmount; ngược lại co giãn theo anchorMax.x (cần anchor căng kín cha).")]
        [SerializeField] private Image fill;
        [Tooltip("Tên boss (tuỳ chọn).")]
        [SerializeField] private Text nameLabel;

        private Image _underlyingFill;
        private Text _barsCountLabel;
        private bool _engaged;
        private bool _dead;
        private bool _subscribed;

        public void SetBoss(BossController value)
        {
            Unsubscribe();
            boss = value;
            if (isActiveAndEnabled) Subscribe();
        }

        private void Awake()
        {
            EnsureRuntimeElements();
            // Ẩn ngay từ đầu (trước cả khi boss kịp báo).
            SetVisible(false);
        }

        private void OnEnable()
        {
            EnsureRuntimeElements();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void EnsureRuntimeElements()
        {
            if (visualRoot == null) return;

            // 1. Underlying Fill (hiển thị màu thanh kế tiếp bên dưới fill chính)
            if (_underlyingFill == null && fill != null)
            {
                Transform existing = visualRoot.transform.Find("UnderlyingFill");
                if (existing != null)
                {
                    _underlyingFill = existing.GetComponent<Image>();
                }
                else
                {
                    GameObject bgObj = new GameObject("UnderlyingFill", typeof(RectTransform), typeof(Image));
                    bgObj.transform.SetParent(fill.transform.parent, false);
                    bgObj.transform.SetSiblingIndex(fill.transform.GetSiblingIndex()); // Nằm ngay sau lưng fill

                    RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                    RectTransform fillRect = fill.rectTransform;
                    bgRect.anchorMin = fillRect.anchorMin;
                    bgRect.anchorMax = fillRect.anchorMax;
                    bgRect.anchoredPosition = fillRect.anchoredPosition;
                    bgRect.sizeDelta = fillRect.sizeDelta;
                    bgRect.pivot = fillRect.pivot;

                    _underlyingFill = bgObj.GetComponent<Image>();
                    _underlyingFill.sprite = fill.sprite;
                    _underlyingFill.type = fill.type;
                    if (_underlyingFill.type == Image.Type.Filled)
                    {
                        _underlyingFill.fillAmount = 1.0f;
                    }
                }
            }

            // 2. Bar Count Label (hiển thị x4, x3, x2, x1 theo SAO)
            if (_barsCountLabel == null)
            {
                Transform existing = visualRoot.transform.Find("BarsCountLabel");
                if (existing != null)
                {
                    _barsCountLabel = existing.GetComponent<Text>();
                }
                else
                {
                    GameObject labelObj = new GameObject("BarsCountLabel", typeof(RectTransform), typeof(Text), typeof(Outline));
                    labelObj.transform.SetParent(visualRoot.transform, false);

                    RectTransform rt = labelObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 0.5f);
                    rt.anchorMax = new Vector2(1f, 0.5f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(12f, 0f);
                    rt.sizeDelta = new Vector2(80f, 36f);

                    _barsCountLabel = labelObj.GetComponent<Text>();
                    _barsCountLabel.alignment = TextAnchor.MiddleLeft;
                    _barsCountLabel.fontSize = 24;
                    UiSkin.ApplyFont(_barsCountLabel, true);
                    _barsCountLabel.color = Color.white;

                    Outline outline = labelObj.GetComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            // 3. Name Label (nếu chưa gán trong Inspector)
            if (nameLabel == null)
            {
                Transform existing = visualRoot.transform.Find("BossNameLabel");
                if (existing != null)
                {
                    nameLabel = existing.GetComponent<Text>();
                }
                else
                {
                    GameObject nameObj = new GameObject("BossNameLabel", typeof(RectTransform), typeof(Text), typeof(Outline));
                    nameObj.transform.SetParent(visualRoot.transform, false);

                    RectTransform rt = nameObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(0f, 6f);
                    rt.sizeDelta = new Vector2(400f, 30f);

                    nameLabel = nameObj.GetComponent<Text>();
                    nameLabel.alignment = TextAnchor.MiddleCenter;
                    nameLabel.fontSize = 22;
                    UiSkin.ApplyFont(nameLabel, true);
                    nameLabel.color = new Color(0.95f, 0.95f, 0.95f, 1f);

                    Outline outline = nameObj.GetComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
        }

        private void Subscribe()
        {
            if (_subscribed || boss == null) return;
            _subscribed = true;

            boss.HealthChanged += HandleHealthChanged;
            boss.EngagedChanged += HandleEngagedChanged;

            _engaged = boss.IsEngaged;
            _dead = boss.IsDead;
            if (nameLabel != null && boss.Definition != null) nameLabel.text = boss.Definition.DisplayName;
            ApplyMultiBarHealth(boss.CurrentHealth, boss.MaxHealth);
            RefreshVisibility();
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            if (boss != null)
            {
                boss.HealthChanged -= HandleHealthChanged;
                boss.EngagedChanged -= HandleEngagedChanged;
            }
        }

        private void HandleHealthChanged(double current, double max, double delta)
        {
            _dead = current <= 0.0;
            ApplyMultiBarHealth(current, max);
            RefreshVisibility();
        }

        private void HandleEngagedChanged(BossController sender, bool engaged)
        {
            _engaged = engaged;
            // Boss được reset (rematch) thì máu đầy lại → không còn "chết".
            if (engaged && boss != null) _dead = boss.IsDead;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            SetVisible(HealthBarRules.ShouldShowBossBar(_engaged, _dead));
        }

        private void SetVisible(bool visible)
        {
            if (visualRoot != null && visualRoot != gameObject) visualRoot.SetActive(visible);
        }

        private void ApplyMultiBarHealth(double current, double max)
        {
            int totalBars = boss != null && boss.Definition != null
                ? boss.Definition.HealthBarCount
                : BossHealthBarRules.DefaultBarCount;

            BossHealthBarRules.BarState state = BossHealthBarRules.Calculate(current, max, totalBars);

            // Cập nhật màu và tỉ lệ fill thanh chính
            if (fill != null)
            {
                fill.color = BossHealthBarRules.GetBarColor(state.RemainingBars);
                ApplyRatio(state.CurrentBarFill);
            }

            // Cập nhật màu thanh phía dưới
            if (_underlyingFill != null)
            {
                _underlyingFill.color = BossHealthBarRules.GetUnderlyingBarColor(state.RemainingBars);
                _underlyingFill.enabled = state.RemainingBars > 0;
            }

            // Cập nhật nhãn x4, x3, x2, x1
            if (_barsCountLabel != null)
            {
                if (state.RemainingBars > 0)
                {
                    _barsCountLabel.gameObject.SetActive(true);
                    _barsCountLabel.text = $"x{state.RemainingBars}";
                    _barsCountLabel.color = state.IsFinalBar
                        ? new Color(1f, 0.35f, 0.35f, 1f)
                        : Color.white;
                }
                else
                {
                    _barsCountLabel.gameObject.SetActive(false);
                }
            }

            // Cập nhật tên boss
            if (nameLabel != null && boss != null && boss.Definition != null)
            {
                nameLabel.text = boss.Definition.DisplayName;
            }
        }

        private void ApplyRatio(float ratio)
        {
            if (fill == null) return;

            if (fill.type == Image.Type.Filled)
            {
                fill.fillAmount = ratio;
            }
            else
            {
                RectTransform rect = fill.rectTransform;
                rect.anchorMax = new Vector2(ratio, rect.anchorMax.y);
            }
        }
    }
}
