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

        [Header("Vai trò chiến đấu (Phase 9.8)")]
        [Tooltip("Chaser = hành vi cũ (mặc định). Skirmisher: áp sát, đánh rồi lùi. Ranged: giữ khoảng cách và bắn đạn.")]
        [SerializeField] private CombatRole role = CombatRole.Chaser;
        [Tooltip("Ranged: khoảng cách ưa thích tới người chơi. Phải > retreatRange và ≤ shootRange.")]
        [SerializeField] private float preferredRange = 4.5f;
        [Tooltip("Người chơi lại gần hơn khoảng này thì quái lùi (Ranged) hoặc lùi tới khoảng này (Skirmisher).")]
        [SerializeField] private float retreatRange = 2.5f;
        [Tooltip("Skirmisher: thời gian lùi tối đa sau mỗi đòn (hết giờ là quay lại áp sát dù bị chặn).")]
        [SerializeField] private float retreatSeconds = 0.8f;
        [Tooltip("Ranged: trong tầm này thì báo đòn rồi bắn. Phải ≤ detectionRange.")]
        [SerializeField] private float shootRange = 6.0f;
        [Tooltip("Đạn phải đủ chậm để né được: thời gian bay từ shootRange tới người chơi >= 0.5 giây.")]
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float projectileLifetime = 1.5f;
        [SerializeField] private float projectileRadius = 0.25f;
        [Tooltip("Sát thương đạn = sát thương của quái x hệ số này.")]
        [SerializeField] private float projectileDamageScale = 1f;

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

        // Vai trò chiến đấu (Phase 9.8). Asset cũ thiếu field này sẽ dùng mặc định: role = Chaser.
        public CombatRole Role => role;
        public float PreferredRange => preferredRange;
        public float RetreatRange => retreatRange;
        public float RetreatSeconds => retreatSeconds;
        public float ShootRange => shootRange;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public float ProjectileRadius => projectileRadius;
        public float ProjectileDamageScale => projectileDamageScale;

        /// <summary>
        /// Kẹp các field vai trò: retreatRange &lt; preferredRange ≤ shootRange ≤ detectionRange và các số dương.
        /// OnValidate gọi hàm này; test gọi trực tiếp.
        /// </summary>
        public void ClampRoleFields()
        {
            Behaviors.EnemyRangeRules.Clamp(detectionRange, ref retreatRange, ref preferredRange, ref shootRange);
            retreatSeconds = Mathf.Max(0.1f, retreatSeconds);
            projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
            projectileLifetime = Mathf.Max(0.1f, projectileLifetime);
            projectileRadius = Mathf.Max(0.01f, projectileRadius);
            projectileDamageScale = Mathf.Max(0f, projectileDamageScale);
        }

        private void OnValidate()
        {
            ClampRoleFields();
        }

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
