using UnityEngine;

namespace Game.PvE.Enemy
{
    /// <summary>
    /// Dữ liệu balance của một loại quái (docs/07 §1). Tạo asset trong Data/Enemies.
    /// Không hard-code số trong EnemyController.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Game/PvE/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Nhận dạng")]
        [Tooltip("ID ổn định để quest/drop tham chiếu (docs/03 §2), ví dụ enemy.slime")]
        [SerializeField] private string id = "enemy.slime";
        [SerializeField] private string displayName = "Enemy";
        [SerializeField] private Sprite sprite;

        [Header("Chỉ số")]
        [SerializeField] private double maxHealth = 40.0;
        [SerializeField] private double attack = 6.0;
        [SerializeField] private double defense = 2.0;
        [SerializeField] private float moveSpeed = 2.0f;

        [Header("Hành vi")]
        [SerializeField] private float detectionRange = 6.0f;
        [SerializeField] private float attackRange = 1.1f;
        [SerializeField] private float attackCooldownSeconds = 1.2f;
        [Tooltip("Thời gian lấy đà (telegraph) trước khi ra đòn — quan trọng vì PvE permadeath, đừng để chết oan (FB-011).")]
        [SerializeField] private float attackWindupSeconds = 0.35f;
        [SerializeField] private float hurtSeconds = 0.25f;
        [SerializeField] private float deathSeconds = 0.6f;

        [Header("Thưởng")]
        [SerializeField] private int experienceReward = 60;

        [Header("Rơi đồ (Phase 5)")]
        [SerializeField] private System.Collections.Generic.List<EnemyDrop> drops =
            new System.Collections.Generic.List<EnemyDrop>();

        /// <summary>Một dòng trong bảng rơi đồ (docs/03 §3 DropTable).</summary>
        [System.Serializable]
        public class EnemyDrop
        {
            public Game.Core.Items.ItemDefinition item;
            [Range(0f, 1f)] public float chance = 1f;
            public int minCount = 1;
            public int maxCount = 1;
        }

        public System.Collections.Generic.IReadOnlyList<EnemyDrop> Drops => drops;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public double MaxHealth => maxHealth;
        public double Attack => attack;
        public double Defense => defense;
        public float MoveSpeed => moveSpeed;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackCooldownSeconds => attackCooldownSeconds;
        public float AttackWindupSeconds => attackWindupSeconds;
        public float HurtSeconds => hurtSeconds;
        public float DeathSeconds => deathSeconds;
        public int ExperienceReward => experienceReward;

        private void Reset()
        {
            displayName = "Enemy";
            maxHealth = 40.0;
            attack = 6.0;
            defense = 2.0;
            moveSpeed = 2.0f;
            detectionRange = 6.0f;
            attackRange = 1.1f;
            attackCooldownSeconds = 1.2f;
            hurtSeconds = 0.25f;
            deathSeconds = 0.6f;
            experienceReward = 60;
        }
    }
}
