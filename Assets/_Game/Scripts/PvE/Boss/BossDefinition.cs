using System;
using System.Collections.Generic;
using Game.Core.Items;
using UnityEngine;

namespace Game.PvE.Boss
{
    /// <summary>
    /// Định nghĩa thông số Boss (docs/02 §13, docs/03 §2, docs/07 §5).
    /// Boss sở hữu: ID theo chuẩn boss.floor_NN, chỉ số máu/công/thủ,
    /// telegraph (báo hiệu đòn), punish window (khoảng hở phản công),
    /// first-clear bonus item và danh sách main drops cho rematch.
    /// </summary>
    [CreateAssetMenu(fileName = "BossDefinition", menuName = "Game/PvE/Boss Definition")]
    public class BossDefinition : ScriptableObject
    {
        [Header("Nhận dạng")]
        [Tooltip("ID ổn định theo chuẩn boss.floor_NN (docs/03 §2), ví dụ boss.floor_01")]
        [SerializeField] private string id = "boss.floor_01";
        [SerializeField] private string displayName = "Cỗ Máy Kéo Neo";
        [Tooltip("Floor ID mà boss này cai quản, ví dụ floor.01")]
        [SerializeField] private string floorId = "floor.01";
        [SerializeField] private Sprite sprite;

        [Header("Chỉ số")]
        [SerializeField] private double maxHealth = 250.0;
        [SerializeField] private double attack = 18.0;
        [SerializeField] private double defense = 4.0;
        [SerializeField] private float moveSpeed = 1.8f;

        [Header("Hành vi & Readability")]
        [SerializeField] private float detectionRange = 10.0f;
        [SerializeField] private float attackRange = 1.6f;
        [Tooltip("Thời gian telegraph (báo hiệu trước khi vung đòn) để người chơi né lướt.")]
        [SerializeField] private float attackWindupSeconds = 0.8f;
        [Tooltip("Punish window: thời gian boss khựng lại sau đòn đánh để người chơi phản công.")]
        [SerializeField] private float punishWindowSeconds = 1.2f;
        [SerializeField] private float attackCooldownSeconds = 2.0f;
        [Tooltip("Ngưỡng HP (0-1) kích hoạt Phase 2 Cuồng nộ (Enrage).")]
        [Range(0f, 1f)]
        [SerializeField] private float enrageHpThreshold = 0.35f;

        [Header("Vùng đánh & Pressure Window (docs/12 §4, docs/07 §5)")]
        [Tooltip("Bán kính vùng đánh thường quanh boss, kiểm TẠI THỜI ĐIỂM đánh. Người chơi lùi ra ngoài vùng trong lúc telegraph thì trượt.")]
        [SerializeField] private float attackHitRadius = DefaultAttackHitRadius;
        [Tooltip("Sau bao nhiêu đòn thường thì boss vào Pressure Window (lao có telegraph).")]
        [SerializeField] private int pressureEveryNAttacks = DefaultPressureEveryNAttacks;
        [Tooltip("Telegraph của Pressure (màu khác đòn thường). Hướng lao khóa khi telegraph kết thúc.")]
        [SerializeField] private float pressureWindupSeconds = DefaultPressureWindupSeconds;
        [Tooltip("Quãng đường lao (đơn vị Unity).")]
        [SerializeField] private float pressureDashDistance = DefaultPressureDashDistance;
        [Tooltip("Thời gian lao hết quãng đường.")]
        [SerializeField] private float pressureDashSeconds = DefaultPressureDashSeconds;
        [Tooltip("Bán kính hành lang lao: mục tiêu cách đường lao không quá bán kính này thì dính đòn (đúng một lần mỗi lần lao).")]
        [SerializeField] private float pressureHitRadius = DefaultPressureHitRadius;
        [Tooltip("Hệ số nhân lên damage đòn thường cho đòn lao.")]
        [SerializeField] private float pressureDamageMultiplier = DefaultPressureDamageMultiplier;
        [Tooltip("Recovery sau lao: Punish Window dài để người chơi phản công.")]
        [SerializeField] private float pressureRecoverySeconds = DefaultPressureRecoverySeconds;

        [Header("Thưởng kinh nghiệm")]
        [SerializeField] private int experienceReward = 150;

        [Header("First-Clear Bonus (nhận 1 lần cho mỗi player)")]
        [SerializeField] private ItemDefinition firstClearRewardItem;
        [SerializeField] private int firstClearRewardQuantity = 1;

        // Giữ chính xác tên serialized "drops" để asset cũ tự trở thành Main Drops.
        [Header("Main Drops (rơi mọi lần hạ boss / rematch)")]
        [SerializeField] private List<BossDrop> drops = new List<BossDrop>();

        [Header("Rare Drops")]
        [Tooltip("Rare drops rơi bình thường ở lần hạ World Boss; khi rematch, chance nhân hệ số bên dưới.")]
        [SerializeField] private List<BossDrop> rareDrops = new List<BossDrop>();
        [Tooltip("Hệ số chance rare drop khi rematch. 0 = không rơi, 1 = như World Boss.")]
        [Range(0f, 1f)]
        [SerializeField] private float rematchRareMultiplier = 0.05f;

        // Giá trị mặc định cho field mới. Asset cũ (YAML thiếu các field này) vẫn mở được:
        // Unity giữ giá trị khởi tạo của field khi YAML không có dòng tương ứng.
        public const float DefaultAttackHitRadius = 1.8f;
        public const int DefaultPressureEveryNAttacks = 2;
        public const float DefaultPressureWindupSeconds = 0.9f;
        public const float DefaultPressureDashDistance = 4.0f;
        public const float DefaultPressureDashSeconds = 0.35f;
        public const float DefaultPressureHitRadius = 1.4f;
        public const float DefaultPressureDamageMultiplier = 1.3f;
        public const float DefaultPressureRecoverySeconds = 1.6f;

        [Serializable]
        public class BossDrop
        {
            public ItemDefinition item;
            [Range(0f, 1f)] public float chance = 1f;
            public int minCount = 1;
            public int maxCount = 1;
        }

        public string Id => id;
        public string DisplayName => displayName;
        public string FloorId => floorId;
        public Sprite Sprite => sprite;
        public double MaxHealth => maxHealth;
        public double Attack => attack;
        public double Defense => defense;
        public float MoveSpeed => moveSpeed;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackWindupSeconds => attackWindupSeconds;
        public float PunishWindowSeconds => punishWindowSeconds;
        public float AttackCooldownSeconds => attackCooldownSeconds;
        public float EnrageHpThreshold => enrageHpThreshold;

        // Các getter dưới đây lùi về mặc định khi giá trị không dương (asset hỏng, hoặc chưa qua OnValidate),
        // để boss không bao giờ lao/đánh với bán kính hay thời lượng 0.
        public float AttackHitRadius => attackHitRadius > 0f ? attackHitRadius : DefaultAttackHitRadius;
        public int PressureEveryNAttacks => pressureEveryNAttacks > 0 ? pressureEveryNAttacks : DefaultPressureEveryNAttacks;
        public float PressureWindupSeconds => pressureWindupSeconds > 0f ? pressureWindupSeconds : DefaultPressureWindupSeconds;
        public float PressureDashDistance => pressureDashDistance > 0f ? pressureDashDistance : DefaultPressureDashDistance;
        public float PressureDashSeconds => pressureDashSeconds > 0f ? pressureDashSeconds : DefaultPressureDashSeconds;
        public float PressureHitRadius => pressureHitRadius > 0f ? pressureHitRadius : DefaultPressureHitRadius;
        public float PressureDamageMultiplier => pressureDamageMultiplier > 0f ? pressureDamageMultiplier : DefaultPressureDamageMultiplier;
        public float PressureRecoverySeconds => pressureRecoverySeconds > 0f ? pressureRecoverySeconds : DefaultPressureRecoverySeconds;

        /// <summary>
        /// Khoảng cách tối đa tới mục tiêu mà boss còn bắt đầu telegraph Pressure: tầm với của một cú lao
        /// (quãng lao + bán kính hành lang). Xa hơn thì boss tiếp tục đuổi.
        /// </summary>
        public float PressureTriggerRange => PressureDashDistance + PressureHitRadius;

        public int ExperienceReward => experienceReward;
        public ItemDefinition FirstClearRewardItem => firstClearRewardItem;
        public int FirstClearRewardQuantity => firstClearRewardQuantity;
        /// <summary>Main drops. Field serialized cũ <c>drops</c> được giữ để asset hiện có tương thích.</summary>
        public IReadOnlyList<BossDrop> MainDrops => drops;

        /// <summary>Rare drops; chance chỉ bị nhân <see cref="RematchRareMultiplier"/> khi rematch.</summary>
        public IReadOnlyList<BossDrop> RareDrops => rareDrops;

        /// <summary>Hệ số rare drop rematch, luôn được clamp trong [0, 1].</summary>
        public float RematchRareMultiplier => Mathf.Clamp01(rematchRareMultiplier);

        /// <summary>Alias tương thích code cũ: <see cref="MainDrops"/>.</summary>
        public IReadOnlyList<BossDrop> Drops => MainDrops;

        public bool HasValidId()
        {
            if (string.IsNullOrEmpty(id)) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(id, @"^boss\.floor_\d{2,}$");
        }

        private void OnValidate()
        {
            rematchRareMultiplier = Mathf.Clamp01(rematchRareMultiplier);

            attackHitRadius = RequirePositive(attackHitRadius, DefaultAttackHitRadius, nameof(attackHitRadius));
            pressureEveryNAttacks = RequirePositive(pressureEveryNAttacks, DefaultPressureEveryNAttacks, nameof(pressureEveryNAttacks));
            pressureWindupSeconds = RequirePositive(pressureWindupSeconds, DefaultPressureWindupSeconds, nameof(pressureWindupSeconds));
            pressureDashDistance = RequirePositive(pressureDashDistance, DefaultPressureDashDistance, nameof(pressureDashDistance));
            pressureDashSeconds = RequirePositive(pressureDashSeconds, DefaultPressureDashSeconds, nameof(pressureDashSeconds));
            pressureHitRadius = RequirePositive(pressureHitRadius, DefaultPressureHitRadius, nameof(pressureHitRadius));
            pressureDamageMultiplier = RequirePositive(pressureDamageMultiplier, DefaultPressureDamageMultiplier, nameof(pressureDamageMultiplier));
            pressureRecoverySeconds = RequirePositive(pressureRecoverySeconds, DefaultPressureRecoverySeconds, nameof(pressureRecoverySeconds));

            if (!string.IsNullOrEmpty(id) && !HasValidId())
            {
                Debug.LogWarning($"[BossDefinition] ID '{id}' không hợp lệ. Phải theo chuẩn boss.floor_NN (ví dụ: boss.floor_01).", this);
            }
        }

        private float RequirePositive(float value, float fallback, string fieldName)
        {
            if (value > 0f) return value;
            Debug.LogWarning($"[BossDefinition] '{fieldName}' phải > 0 (đang {value}); dùng mặc định {fallback}.", this);
            return fallback;
        }

        private int RequirePositive(int value, int fallback, string fieldName)
        {
            if (value > 0) return value;
            Debug.LogWarning($"[BossDefinition] '{fieldName}' phải > 0 (đang {value}); dùng mặc định {fallback}.", this);
            return fallback;
        }
    }
}
