using Game.PvE.Dodge;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Skills
{
    /// <summary>
    /// Nút cảm ứng Dodge. CHỨA UI logic ONLY — gọi DodgeController.TryDodge().
    /// Không tự tính cooldown; chỉ đọc từ controller. (AGENTS.md #3)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OnScreenDodgeButton : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private Image cooldownFill;
        [SerializeField] private GameObject disabledOverlay;
        [SerializeField] [Range(0.01f, 0.5f)] private float uiRefreshIntervalSeconds = 0.1f;

        private Button _button;
        private DodgeController _dodge;
        private float _nextRefresh;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
            TryFindDodge();
        }

        private void OnEnable()
        {
            TryFindDodge();
        }

        private void Update()
        {
            if (_dodge == null) TryFindDodge();
            if (_dodge == null) return;
            if (!(UnityEngine.Time.time >= _nextRefresh)) return;
            _nextRefresh = UnityEngine.Time.time + uiRefreshIntervalSeconds;
            RefreshUI();
        }

        private void HandleClick()
        {
            if (_dodge == null) TryFindDodge();
            if (_dodge == null) return;
            _dodge.TryDodge();
        }

        private void RefreshUI()
        {
            if (_dodge == null) return;

            float remaining = _dodge.CooldownRemaining;
            float total = _dodge.CooldownTotal;
            if (cooldownFill != null && total > 0.001f)
            {
                cooldownFill.fillAmount = Mathf.Clamp01(remaining / total);
                cooldownFill.enabled = remaining > 0.01f;
            }
            bool ready = _dodge.IsReady;
            if (_button != null) _button.interactable = ready;
            if (disabledOverlay != null) disabledOverlay.SetActive(!ready);
        }

        private void TryFindDodge()
        {
            if (_dodge != null) return;
            _dodge = FindAnyObjectByType<DodgeController>();
        }
    }
}
