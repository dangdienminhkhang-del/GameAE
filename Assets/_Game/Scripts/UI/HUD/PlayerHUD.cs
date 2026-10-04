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
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
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

        private int _currentLevel = 1;

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

            if (hpBarFill != null)
            {
                float ratio = e.Max > 0.0 ? (float)(e.Current / e.Max) : 0f;
                hpBarFill.fillAmount = Mathf.Clamp01(ratio);
            }

            // Hiện số máu: "46/100" (docs/07 §1 quy định HP theo level; số hiển thị là quyết định UI).
            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt((float)e.Current)}/{Mathf.CeilToInt((float)e.Max)}";
            }
        }

        private void HandleExperienceChanged(object sender, ExperienceChangedArgs e)
        {
            _currentLevel = e.Level;
            RefreshLevelText();

            if (expBarFill == null) return;
            float ratio = e.Required > 0 ? (float)e.Current / e.Required : 0f;
            expBarFill.fillAmount = Mathf.Clamp01(ratio);
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
