using UnityEngine;

namespace Game.Core.Combat
{
    /// <summary>
    /// Dữ liệu balance của MỘT loại đòn đánh (đòn thường, chiêu, đòn quái...).
    /// Không hard-code số trong code gameplay — tạo asset trong Data/Config và gán ở Inspector.
    /// Dùng chung được cho PvE và PvP (Core không biết ai đang đánh).
    /// </summary>
    [CreateAssetMenu(fileName = "AttackDefinition", menuName = "Game/Core/Attack Definition")]
    public class AttackDefinition : ScriptableObject
    {
        [Header("Sát thương")]
        [SerializeField] private double damageMultiplier = 1.0;

        [Header("Thời gian")]
        [SerializeField] private float cooldownSeconds = 0.45f;

        [Header("Vùng đánh (Hit Detection)")]
        [SerializeField] private float hitboxRadius = 0.9f;
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.6f, 0.0f);
        [SerializeField] private LayerMask targetLayers;

        [Header("Chí mạng — để -1 nghĩa là dùng chỉ số nhân vật")]
        [SerializeField] private double criticalChanceOverride = -1.0;
        [SerializeField] private double criticalDamageOverride = -1.0;

        public double DamageMultiplier => damageMultiplier;
        public float CooldownSeconds => cooldownSeconds;
        public float HitboxRadius => hitboxRadius;
        public Vector2 HitboxOffset => hitboxOffset;
        public LayerMask TargetLayers => targetLayers;
        public double CriticalChanceOverride => criticalChanceOverride;
        public double CriticalDamageOverride => criticalDamageOverride;

        /// <summary>Giá trị mặc định khi tạo asset mới trong Editor (Reset) hoặc khi cần bản mặc định lúc chạy.</summary>
        private void Reset()
        {
            ApplyAuthoringDefaults();
        }

        public static AttackDefinition CreateDefault()
        {
            AttackDefinition definition = CreateInstance<AttackDefinition>();
            definition.ApplyAuthoringDefaults();
            return definition;
        }

        private void ApplyAuthoringDefaults()
        {
            damageMultiplier = 1.0;
            cooldownSeconds = 0.45f;
            hitboxRadius = 0.9f;
            hitboxOffset = new Vector2(0.6f, 0.0f);
            criticalChanceOverride = -1.0;
            criticalDamageOverride = -1.0;
        }
    }
}
