using Game.Core.Skills;
using Game.PvE.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Skills
{
    /// <summary>
    /// HUD nhỏ hiển thị: tên Technique hiện tại theo slot + mastery hiện tại của Discipline.
    /// CHỈ đọc từ TechniqueController; không tự tính toán. (AGENTS.md #3)
    /// Người phát triển gắn Text + Slider con trong Unity Editor.
    /// </summary>
    public class TechniqueSlotHUD : MonoBehaviour
    {
        [Header("Slot")]
        [SerializeField] private TechniqueInputSlot slot = TechniqueInputSlot.Core;

        [Header("UI Refs")]
        [SerializeField] private Text techniqueNameLabel;
        [SerializeField] private Text masteryLabel;
        [SerializeField] private Slider masteryBar;

        [SerializeField] [Range(0.05f, 1f)] private float refreshIntervalSeconds = 0.2f;

        private TechniqueController _controller;
        private float _nextRefresh;

        private void OnEnable()
        {
            TryFindController();
        }

        private void Update()
        {
            if (_controller == null) TryFindController();
            if (_controller == null) return;
            if (!(UnityEngine.Time.time >= _nextRefresh)) return;
            _nextRefresh = UnityEngine.Time.time + refreshIntervalSeconds;
            Refresh();
        }

        private void Refresh()
        {
            IMasteryService mastery = _controller != null ? _controller.Mastery : null;
            WeaponDiscipline disc = ResolveDiscipline();
            int masteryVal = mastery != null ? mastery.GetMastery(disc) : 0;

            if (masteryLabel != null) masteryLabel.text = disc == WeaponDiscipline.None
                ? "Không có vũ khí"
                : $"{disc} Mastery {masteryVal}/100";
            if (masteryBar != null)
            {
                masteryBar.maxValue = 100f;
                masteryBar.minValue = 0f;
                masteryBar.value = Mathf.Clamp(masteryVal, 0, 100);
            }

            if (techniqueNameLabel != null)
            {
                // Technique hiện tại theo slot — lấy từ TechniqueDatabase (nếu controller reference)
                techniqueNameLabel.text = slot == TechniqueInputSlot.None ? "" : $"{slot} Technique";
            }
        }

        private WeaponDiscipline ResolveDiscipline()
        {
            // Mốc 1: mặc định Blade (vũ khí mặc định). Khi có nhiều Discipline,
            // người phát triển có thể expose Discipline property trên TechniqueController.
            return WeaponDiscipline.Blade;
        }

        private void TryFindController()
        {
            if (_controller != null) return;
            _controller = FindAnyObjectByType<TechniqueController>();
        }
    }
}
