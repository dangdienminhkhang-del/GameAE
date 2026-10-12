using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Skills
{
    /// <summary>
    /// Definition tĩnh của Weapon Technique (docs/12 §6.3, docs/07 §4).
    /// Balance nằm toàn bộ ở đây. Core executor dùng chung không branch theo tên technique.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponTechniqueDefinition", menuName = "Game/Core/Skills/Weapon Technique Definition")]
    public class WeaponTechniqueDefinition : ScriptableObject
    {
        [Header("Nhận dạng")]
        [SerializeField] private string id = "technique.undefined";
        [SerializeField] private string displayName = "Technique";
        [SerializeField] private WeaponDiscipline weaponDiscipline = WeaponDiscipline.None;

        [Header("Yêu cầu")]
        [SerializeField] private TechniqueInputSlot inputSlot = TechniqueInputSlot.Core;
        [SerializeField] [Range(0, 100)] private int masteryRequired = 0;

        [Header("Thời gian & Balance")]
        [SerializeField] private float cooldownSeconds = 2.5f;
        [SerializeField] private float windUpSeconds = 0.10f;
        [SerializeField] private float recoverySeconds = 0.25f;
        [SerializeField] [Tooltip("Tổng nhân damage toàn bộ hit; không được nhỏ hơn tổng hitMultiplier của các window.")]
        private double totalMultiplier = 1.0;

        [Header("Active Windows (hit + movement)")]
        [SerializeField] private List<TechniqueHitWindow> activeWindows = new List<TechniqueHitWindow>();

        [Header("Hit Detection (kế thừa AttackDefinition layout)")]
        [SerializeField] private float hitboxRadius = 0.9f;
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.6f, 0.0f);
        [SerializeField] private LayerMask targetLayers;

        [Header("Override chí mạng — -1 nghĩa là dùng chỉ số nhân vật")]
        [SerializeField] private double criticalChanceOverride = -1.0;
        [SerializeField] private double criticalDamageOverride = -1.0;

        public string Id => id;
        public string DisplayName => displayName;
        public WeaponDiscipline WeaponDiscipline => weaponDiscipline;
        public TechniqueInputSlot InputSlot => inputSlot;
        public int MasteryRequired => masteryRequired;
        public float CooldownSeconds => cooldownSeconds;
        public float WindUpSeconds => windUpSeconds;
        public float RecoverySeconds => recoverySeconds;
        public double TotalMultiplier => totalMultiplier;
        public IReadOnlyList<TechniqueHitWindow> ActiveWindows => activeWindows;
        public float HitboxRadius => hitboxRadius;
        public Vector2 HitboxOffset => hitboxOffset;
        public LayerMask TargetLayers => targetLayers;
        public double CriticalChanceOverride => criticalChanceOverride;
        public double CriticalDamageOverride => criticalDamageOverride;

        /// <summary>Tổng hitMultiplier của tất cả window — dùng để validate không vượt TotalMultiplier.</summary>
        public double SumHitMultipliers()
        {
            double sum = 0.0;
            if (activeWindows == null) return sum;
            for (int i = 0; i < activeWindows.Count; i++) sum += activeWindows[i].hitMultiplier;
            return sum;
        }

        /// <summary>Validate khi authoring. Runtime vẫn phải gọi Validate(...) qua TechniqueExecutor.</summary>
        public bool IsDefinitionValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(id)) { error = "Id không được trống."; return false; }
            if (id == "technique.undefined")   { error = $"Id '{id}' chưa được đặt — thay bằng ID thực (vd: technique.blade_foundation)."; return false; }
            if (weaponDiscipline == WeaponDiscipline.None) { error = $"{id}: WeaponDiscipline chưa gán."; return false; }
            if (inputSlot == TechniqueInputSlot.None) { error = $"{id}: InputSlot chưa gán."; return false; }
            if (masteryRequired < 0 || masteryRequired > 100) { error = $"{id}: masteryRequired ngoài 0-100."; return false; }
            if (cooldownSeconds < 0f) { error = $"{id}: cooldownSeconds âm."; return false; }
            if (windUpSeconds < 0f) { error = $"{id}: windUpSeconds âm."; return false; }
            if (recoverySeconds < 0f) { error = $"{id}: recoverySeconds âm."; return false; }
            if (totalMultiplier <= 0.0) { error = $"{id}: totalMultiplier phải dương."; return false; }
            double sum = SumHitMultipliers();
            if (sum > totalMultiplier + 0.0001)
            {
                error = $"{id}: tổng hitMultiplier {sum:0.###} vượt totalMultiplier {totalMultiplier:0.###}.";
                return false;
            }
            if (activeWindows == null || activeWindows.Count == 0)
            {
                error = $"{id}: phải có ít nhất 1 ActiveWindow.";
                return false;
            }
            error = null;
            return true;
        }

        private void OnValidate()
        {
            // Cảnh báo targetLayers = 0 (Nothing) → Technique không trúng gì, mastery không tăng.
            if (targetLayers.value == 0)
                Debug.LogWarning(
                    $"[WeaponTechniqueDefinition] '{id}': targetLayers = Nothing. " +
                    "Gán layer Enemy (giống Attack_PlayerBasic) trong Inspector.", this);

            // Cảnh báo balance lệch 07 §4.1/§4.2
            if (inputSlot == TechniqueInputSlot.Traverse)
            {
                if (cooldownSeconds < 4f || cooldownSeconds > 6f)
                    Debug.LogWarning(
                        $"[WeaponTechniqueDefinition] '{id}': Traverse cooldown {cooldownSeconds:F1}s ngoài dải 4–6s (docs/07 §4.1).", this);
                double sumMult = SumHitMultipliers();
                if (sumMult > 1.25)
                    Debug.LogWarning(
                        $"[WeaponTechniqueDefinition] '{id}': Traverse tổng hitMultiplier {sumMult:0.##} vượt 1.25 (docs/07 §4.2).", this);
            }
        }

        private void Reset()
        {
            ApplyAuthoringDefaults();
        }

        public static WeaponTechniqueDefinition CreateDefault(string techniqueId, WeaponDiscipline discipline, TechniqueInputSlot slot, double totalMult)
        {
            WeaponTechniqueDefinition def = CreateInstance<WeaponTechniqueDefinition>();
            def.ApplyAuthoringDefaults();
            def.id = techniqueId;
            def.weaponDiscipline = discipline;
            def.inputSlot = slot;
            def.totalMultiplier = totalMult;
            return def;
        }

        private void ApplyAuthoringDefaults()
        {
            id = "technique.undefined";
            displayName = "Technique";
            weaponDiscipline = WeaponDiscipline.Blade;
            inputSlot = TechniqueInputSlot.Core;
            masteryRequired = 0;
            cooldownSeconds = 2.5f;
            windUpSeconds = 0.10f;
            recoverySeconds = 0.25f;
            totalMultiplier = 1.0;
            hitboxRadius = 0.9f;
            hitboxOffset = new Vector2(0.6f, 0.0f);
            criticalChanceOverride = -1.0;
            criticalDamageOverride = -1.0;
            if (activeWindows == null) activeWindows = new List<TechniqueHitWindow>();
            activeWindows.Clear();
            activeWindows.Add(new TechniqueHitWindow(0f, 0.18f, 1.0, MovementPolicy.None, 0f));
        }
    }
}
