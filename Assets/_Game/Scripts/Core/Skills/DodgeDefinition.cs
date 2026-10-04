using UnityEngine;

namespace Game.Core.Skills
{
    /// <summary>
    /// Dodge là hành động chung, không phải Technique — không cần mastery (ADR-025, docs/07 §4.4).
    /// Không i-frames, không xuyên tường. PvP dùng profile riêng (sẽ thêm trong PvP RuleSet sau), không modify đây.
    /// </summary>
    [CreateAssetMenu(fileName = "DodgeDefinition", menuName = "Game/Core/Skills/Dodge Definition")]
    public class DodgeDefinition : ScriptableObject
    {
        [Header("Thời gian")]
        [SerializeField] private float dodgeDurationSeconds = 0.18f;
        [SerializeField] private float cooldownSeconds = 1.2f;
        [SerializeField] private float recoverySeconds = 0.20f;

        [Header("Di chuyển")]
        [SerializeField] private float dashDistance = 2.0f;

        public float DodgeDurationSeconds => dodgeDurationSeconds;
        public float CooldownSeconds => cooldownSeconds;
        public float RecoverySeconds => recoverySeconds;
        public float DashDistance => dashDistance;

        private void Reset()
        {
            dodgeDurationSeconds = 0.18f;
            cooldownSeconds = 1.2f;
            recoverySeconds = 0.20f;
            dashDistance = 2.0f;
        }

        public static DodgeDefinition CreateDefault()
        {
            DodgeDefinition def = CreateInstance<DodgeDefinition>();
            def.Reset();
            return def;
        }
    }
}
