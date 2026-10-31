using Game.Core.Events;
using UnityEngine;
using UnityEngine.UI;

#if TMP_PRESENT
using TMPro;
#endif

namespace Game.UI.HUD
{
    /// <summary>
    /// HUD CHỈ hiển thị. Mọi phép tính EXP/level nằm ở PlayerProgress (PvE) —
    /// HUD nghe OnExperienceChanged để vẽ thanh EXP + text level.
    /// Thanh máu theo phong cách SAO: xanh lá khi đầy, vàng khi dưới nửa, đỏ khi nguy kịch.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        // Màu thanh máu kiểu SAO (xanh lá -> vàng -> đỏ).
        private static readonly Color HpGreen = new Color(0.42f, 0.88f, 0.28f, 1f);
        private static readonly Color HpYellow = new Color(0.98f, 0.80f, 0.16f, 1f);
        private static readonly Color HpRed = new Color(0.92f, 0.22f, 0.16f, 1f);

        // Tốc độ thanh chạy tới giá trị mới (càng lớn càng nhanh).
        private const float BarSmoothSpeed = 9f;
        private const float SnapEpsilon = 0.0005f;

        [Header("Bars")]
        [SerializeField] private Image hpBarFill;
        [SerializeField] private Image expBarFill;

        [Header("Texts")]
        [SerializeField] private GameObject levelTextLegacy;
        [Tooltip("Text hiện số máu dạng 'current/max' (đặt trên thanh máu).")]
        [SerializeField] private Text hpText;
#if TMP_PRESENT
        [SerializeField] private TMP_Text levelTextTMP;
#endif

        [Tooltip("Chỉ nhận OnHealthChanged từ component này — tránh quái/bia làm nhảy thanh HP của player.")]
        [SerializeField] private MonoBehaviour healthSource;

        [Header("Cảnh báo")]
        [Tooltip("Viền đỏ khi máu thấp. Để trống = không cảnh báo.")]
        [SerializeField] private LowHealthWarning lowHealthWarning;

        private int _currentLevel = 1;

        private float _hpTarget = 1f;
        private float _hpShown = 1f;
        private bool _hpInitialized;

        private float _expTarget;
        private float _expShown;
        private bool _expInitialized;

        private void Start()
        {
            EnsureAdvancedCombatHUD();
        }

        private void EnsureAdvancedCombatHUD()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            Transform parent = canvas.transform.Find("SafeAreaRoot") ?? canvas.transform;

            if (FindFirstObjectByType<ComboHUD>() == null)
            {
                GameObject comboObj = new GameObject("ComboHUD_Auto", typeof(RectTransform));
                comboObj.transform.SetParent(parent, false);
                comboObj.AddComponent<ComboHUD>();
            }

            if (FindFirstObjectByType<Controls.OnScreenUltimateButton>() == null)
            {
                GameObject ultObj = new GameObject("UltimateButton_Auto", typeof(RectTransform));
                ultObj.transform.SetParent(parent, false);
                ultObj.AddComponent<Controls.OnScreenUltimateButton>();
            }

            if (FindFirstObjectByType<Combat.FloatingCombatNoticePresenter>() == null)
            {
                GameObject noticeObj = new GameObject("FloatingNotice_Auto", typeof(RectTransform));
                noticeObj.transform.SetParent(parent, false);
                noticeObj.AddComponent<Combat.FloatingCombatNoticePresenter>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnHealthChanged += HandleHealthChanged;
            GameEvents.OnExperienceChanged += HandleExperienceChanged;
            RefreshLevelText();
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            GameEvents.OnExperienceChanged -= HandleExperienceChanged;
        }

        private void Update()
        {
            float k = 1f - Mathf.Exp(-BarSmoothSpeed * UnityEngine.Time.unscaledDeltaTime);

            if (hpBarFill != null && !Mathf.Approximately(_hpShown, _hpTarget))
            {
                _hpShown = Mathf.Abs(_hpTarget - _hpShown) < SnapEpsilon ? _hpTarget : Mathf.Lerp(_hpShown, _hpTarget, k);
                ApplyHpBar();
            }

            if (expBarFill != null && !Mathf.Approximately(_expShown, _expTarget))
            {
                _expShown = Mathf.Abs(_expTarget - _expShown) < SnapEpsilon ? _expTarget : Mathf.Lerp(_expShown, _expTarget, k);
                expBarFill.fillAmount = _expShown;
            }
        }

        /// <summary>Do PvEBootstrap gọi — UI không tự đi tìm player.</summary>
        public void SetHealthSource(MonoBehaviour source)
        {
            healthSource = source;
        }

        public void SetInitialLevel(int level)
        {
            _currentLevel = level;
            RefreshLevelText();
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs e)
        {
            if (healthSource != null && !ReferenceEquals(sender, healthSource)) return;

            float ratio = e.Max > 0.0 ? Mathf.Clamp01((float)(e.Current / e.Max)) : 0f;

            _hpTarget = ratio;
            if (!_hpInitialized)
            {
                // Lần đầu: gán thẳng, không chạy hiệu ứng từ 100%.
                _hpInitialized = true;
                _hpShown = ratio;
                ApplyHpBar();
            }

            if (lowHealthWarning != null)
            {
                lowHealthWarning.SetHealthRatio(ratio);
            }

            // Hiện số máu: "46/100" (docs/07 §1 quy định HP theo level; số hiển thị là quyết định UI).
            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt((float)e.Current)}/{Mathf.CeilToInt((float)e.Max)}";
            }
        }

        private void HandleExperienceChanged(object sender, ExperienceChangedArgs e)
        {
            bool leveledUp = e.Level != _currentLevel;
            _currentLevel = e.Level;
            RefreshLevelText();

            if (expBarFill == null) return;
            float ratio = e.Required > 0 ? Mathf.Clamp01((float)e.Current / e.Required) : 0f;

            _expTarget = ratio;
            // Lần đầu, lên cấp hoặc EXP bị trừ: gán thẳng để thanh không chạy lùi.
            if (!_expInitialized || leveledUp || ratio < _expShown)
            {
                _expInitialized = true;
                _expShown = ratio;
                expBarFill.fillAmount = ratio;
            }
        }

        private void ApplyHpBar()
        {
            if (hpBarFill == null) return;
            hpBarFill.fillAmount = _hpShown;
            hpBarFill.color = EvaluateHpColor(_hpShown);
        }

        /// <summary>Xanh lá (&gt;50%) -> vàng (20–50%) -> đỏ (&lt;20%), chuyển màu mượt quanh các ngưỡng.</summary>
        private static Color EvaluateHpColor(float ratio)
        {
            if (ratio >= 0.55f) return HpGreen;
            if (ratio >= 0.45f) return Color.Lerp(HpYellow, HpGreen, (ratio - 0.45f) / 0.10f);
            if (ratio >= 0.25f) return HpYellow;
            if (ratio >= 0.15f) return Color.Lerp(HpRed, HpYellow, (ratio - 0.15f) / 0.10f);
            return HpRed;
        }

        private void RefreshLevelText()
        {
            string text = $"Lv.{_currentLevel}";
#if TMP_PRESENT
            if (levelTextTMP != null)
            {
                levelTextTMP.text = text;
                return;
            }
#endif
            if (levelTextLegacy != null)
            {
                Text legacy = levelTextLegacy.GetComponent<Text>();
                if (legacy != null) legacy.text = text;
            }
        }
    }
}
