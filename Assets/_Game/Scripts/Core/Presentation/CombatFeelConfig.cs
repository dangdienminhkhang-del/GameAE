using UnityEngine;

namespace Game.Core.Presentation
{
    /// <summary>
    /// Mọi con số của "cảm giác chiến đấu" (docs/05 Phase 9.5): dừng hình, nháy, tan khi chết,
    /// thanh máu, số sát thương nổi. Không có magic number trong code — giá trị mặc định ở ĐÂY.
    /// Tạo asset: Create → Game → Presentation → Combat Feel Config (gợi ý Assets/_Game/Data/Presentation/).
    /// Thành phần nào không được gán asset sẽ dùng <see cref="Active"/> (asset đã đăng ký) hoặc bản mặc định.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatFeelConfig", menuName = "Game/Presentation/Combat Feel Config")]
    public class CombatFeelConfig : ScriptableObject
    {
        [Header("Dừng hình (giây THỜI GIAN THỰC)")]
        [Tooltip("Dừng hình cho đòn thường của người chơi.")]
        [SerializeField] private float hitStopNormalSeconds = 0.04f;
        [Tooltip("Dừng hình cho đòn kết liễu (mục tiêu chết sau hit).")]
        [SerializeField] private float hitStopFinisherSeconds = 0.06f;
        [Tooltip("Dừng hình khi đánh trúng boss (chưa chết).")]
        [SerializeField] private float hitStopBossSeconds = 0.05f;
        [Tooltip("TRẦN tuyệt đối (docs/05: tối đa 0.06 giây). Mọi giá trị trên bị kẹp về trần này.")]
        [SerializeField] private float hitStopMaxSeconds = 0.06f;
        [Tooltip("Khoảng cách tối thiểu giữa hai lần bắt đầu dừng hình.")]
        [SerializeField] private float hitStopMinIntervalSeconds = 0.15f;
        [Tooltip("Time.timeScale trong lúc dừng hình (không phải 0 để quái và boss chỉ chậm lại).")]
        [SerializeField] private float hitStopTimeScale = 0.05f;

        [Header("Nháy khi trúng đòn")]
        [SerializeField] private float flashSeconds = 0.08f;
        [Tooltip("Màu lớp phủ nháy (child 'HitFlash', KHÔNG nhân vào màu sprite). Mặc định trắng; alpha của màu này bị bỏ qua, alpha lấy từ 'Flash Start Alpha'.")]
        [SerializeField] private Color flashColor = Color.white;
        [Tooltip("Alpha lớp phủ lúc bắt đầu nháy, giảm tuyến tính về 0 trong 'Flash Seconds' (thời gian thực).")]
        [SerializeField] private float flashStartAlpha = 0.85f;
        [Tooltip("Alpha TỐI ĐA của lớp phủ khi đang báo đòn nguy hiểm (quái windup, boss Telegraph/Pressure), để màu báo đòn vẫn đọc được.")]
        [SerializeField] private float flashMaxAlphaUnderDanger = 0.5f;
        [Tooltip("Scale punch 1 → giá trị này → 1 trong thời gian nháy (chỉ áp cho quái thường). 1 = tắt.")]
        [SerializeField] private float punchScale = 1.08f;

        [Header("Quái tan khi chết")]
        [Tooltip("Màu xám khi chết (RGB). Alpha lấy từ 'Death Start Alpha' rồi mờ dần về 0.")]
        [SerializeField] private Color deadTint = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private float deathStartAlpha = 0.9f;
        [Tooltip("Scale cuối của hiệu ứng tan (so với scale gốc).")]
        [SerializeField] private float deathEndScale = 0.85f;
        [Tooltip("Màu xám khi BOSS chết (boss không tan, giữ hành vi chết hiện tại).")]
        [SerializeField] private Color bossDeadTint = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        [Header("Thanh máu quái")]
        [Tooltip("Ẩn thanh máu sau chừng này giây không bị đánh.")]
        [SerializeField] private float enemyHealthBarHideSeconds = 3f;
        [SerializeField] private float enemyHealthBarWidth = 0.9f;
        [SerializeField] private float enemyHealthBarHeight = 0.12f;
        [Tooltip("Khoảng cách phía trên mép sprite (đơn vị thế giới).")]
        [SerializeField] private float enemyHealthBarOffsetY = 0.15f;
        [SerializeField] private Color healthBarBackColor = new Color(0f, 0f, 0f, 0.7f);
        [SerializeField] private Color healthBarFillColor = new Color(0.85f, 0.15f, 0.15f, 1f);

        [Header("Số sát thương nổi")]
        [SerializeField] private float damageNumberSeconds = 0.6f;
        [Tooltip("Số pixel (trong không gian Canvas) số bay lên.")]
        [SerializeField] private float damageNumberRisePixels = 70f;
        [Tooltip("Số trùng điểm thì dời lên thêm chừng này pixel mỗi số.")]
        [SerializeField] private float damageNumberStackStepPixels = 28f;
        [Tooltip("Hai số cách nhau nhỏ hơn bán kính này (pixel) được coi là trùng điểm.")]
        [SerializeField] private float damageNumberStackRadiusPixels = 40f;
        [SerializeField] private int damageNumberFontSize = 36;
        [Tooltip("Số người chơi gây ra.")]
        [SerializeField] private Color damageDealtColor = new Color(1f, 0.95f, 0.6f, 1f);
        [Tooltip("Số người chơi gây ra khi chí mạng.")]
        [SerializeField] private Color damageCriticalColor = new Color(1f, 0.6f, 0.1f, 1f);
        [Tooltip("Số người chơi NHẬN vào.")]
        [SerializeField] private Color damageReceivedColor = new Color(1f, 0.25f, 0.25f, 1f);

        [Header("Rung (P9.6)")]
        [Tooltip("Độ dài xung rung ngắn khi người chơi mất máu (mili-giây).")]
        [SerializeField] private int hapticHitMilliseconds = 40;
        [Tooltip("Độ dài mỗi xung trong cảnh báo HP thấp (mili-giây). Cảnh báo gồm hai xung.")]
        [SerializeField] private int hapticWarnPulseMilliseconds = 40;
        [Tooltip("Khoảng nghỉ giữa hai xung của cảnh báo HP thấp (mili-giây).")]
        [SerializeField] private int hapticWarnGapMilliseconds = 80;
        [Tooltip("Cảnh báo khi HP rơi dưới tỉ lệ này (0.30 = 30%).")]
        [SerializeField] private float hapticLowHpRatio = 0.30f;
        [Tooltip("Chỉ báo lại sau khi HP lên trên tỉ lệ này (độ trễ, 0.35 = 35%).")]
        [SerializeField] private float hapticRearmRatio = 0.35f;
        [Tooltip("Khoảng cách tối thiểu giữa hai xung rung (giây thời gian thật).")]
        [SerializeField] private float hapticMinIntervalSeconds = 0.08f;

        [Header("Màn tải (P9.6)")]
        [Tooltip("Thời gian hiện tối thiểu của màn tải để không chớp (giây).")]
        [SerializeField] private float loadingMinShowSeconds = 0.4f;

        public HitStopSettings HitStop => new HitStopSettings(
            hitStopNormalSeconds, hitStopFinisherSeconds, hitStopBossSeconds,
            hitStopMaxSeconds, hitStopMinIntervalSeconds, hitStopTimeScale);

        public float FlashSeconds => flashSeconds;
        public Color FlashColor => flashColor;
        public float FlashStartAlpha => flashStartAlpha;
        public float FlashMaxAlphaUnderDanger => flashMaxAlphaUnderDanger;
        public float PunchScale => punchScale;

        public Color DeadTint => deadTint;
        public float DeathStartAlpha => deathStartAlpha;
        public float DeathEndScale => deathEndScale;
        public Color BossDeadTint => bossDeadTint;

        public float EnemyHealthBarHideSeconds => enemyHealthBarHideSeconds;
        public float EnemyHealthBarWidth => enemyHealthBarWidth;
        public float EnemyHealthBarHeight => enemyHealthBarHeight;
        public float EnemyHealthBarOffsetY => enemyHealthBarOffsetY;
        public Color HealthBarBackColor => healthBarBackColor;
        public Color HealthBarFillColor => healthBarFillColor;

        public float DamageNumberSeconds => damageNumberSeconds;
        public float DamageNumberRisePixels => damageNumberRisePixels;
        public float DamageNumberStackStepPixels => damageNumberStackStepPixels;
        public float DamageNumberStackRadiusPixels => damageNumberStackRadiusPixels;
        public int DamageNumberFontSize => damageNumberFontSize;

        public int HapticHitMilliseconds => hapticHitMilliseconds;
        public int HapticWarnPulseMilliseconds => hapticWarnPulseMilliseconds;
        public int HapticWarnGapMilliseconds => hapticWarnGapMilliseconds;
        public float HapticLowHpRatio => hapticLowHpRatio;
        public float HapticRearmRatio => hapticRearmRatio;
        public float HapticMinIntervalSeconds => hapticMinIntervalSeconds;
        public float LoadingMinShowSeconds => loadingMinShowSeconds;

        public Color ColorFor(DamageNumberKind kind)
        {
            switch (kind)
            {
                case DamageNumberKind.Received: return damageReceivedColor;
                case DamageNumberKind.DealtCritical: return damageCriticalColor;
                default: return damageDealtColor;
            }
        }

        // ── Asset đang dùng ─────────────────────────────────────────────────

        private static CombatFeelConfig s_active;
        private static CombatFeelConfig s_defaults;

        /// <summary>
        /// Asset đang có hiệu lực (do PlaySpeedTicker đăng ký). Chưa đăng ký thì trả bản mặc định (giá trị khởi tạo ở trên),
        /// nên code luôn có số hợp lệ kể cả khi scene chưa gán asset.
        /// </summary>
        public static CombatFeelConfig Active
        {
            get
            {
                if (s_active != null) return s_active;
                if (s_defaults == null)
                {
                    s_defaults = CreateInstance<CombatFeelConfig>();
                    s_defaults.hideFlags = HideFlags.HideAndDontSave;
                    s_defaults.name = "CombatFeelConfig (defaults)";
                }
                return s_defaults;
            }
        }

        /// <summary>Trả chính <paramref name="assigned"/> nếu có, ngược lại <see cref="Active"/>.</summary>
        public static CombatFeelConfig Resolve(CombatFeelConfig assigned)
        {
            return assigned != null ? assigned : Active;
        }

        public static void SetActive(CombatFeelConfig config)
        {
            s_active = config;
        }

        /// <summary>Chỉ bỏ đăng ký nếu <paramref name="config"/> đúng là asset đang active (tránh gỡ nhầm của thành phần khác).</summary>
        public static void ClearActive(CombatFeelConfig config)
        {
            if (s_active == config) s_active = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_active = null;
            s_defaults = null;
        }

        private void OnValidate()
        {
            hitStopNormalSeconds = Mathf.Max(0f, hitStopNormalSeconds);
            hitStopFinisherSeconds = Mathf.Max(0f, hitStopFinisherSeconds);
            hitStopBossSeconds = Mathf.Max(0f, hitStopBossSeconds);
            hitStopMaxSeconds = Mathf.Max(0f, hitStopMaxSeconds);
            hitStopMinIntervalSeconds = Mathf.Max(0f, hitStopMinIntervalSeconds);
            hitStopTimeScale = Mathf.Clamp01(hitStopTimeScale);
            flashSeconds = Mathf.Max(0.01f, flashSeconds);
            flashStartAlpha = Mathf.Clamp01(flashStartAlpha);
            flashMaxAlphaUnderDanger = Mathf.Clamp01(flashMaxAlphaUnderDanger);
            punchScale = Mathf.Max(1f, punchScale);
            deathStartAlpha = Mathf.Clamp01(deathStartAlpha);
            deathEndScale = Mathf.Clamp(deathEndScale, 0.1f, 1f);
            enemyHealthBarHideSeconds = Mathf.Max(0.1f, enemyHealthBarHideSeconds);
            damageNumberSeconds = Mathf.Max(0.1f, damageNumberSeconds);
            hapticHitMilliseconds = Mathf.Max(1, hapticHitMilliseconds);
            hapticWarnPulseMilliseconds = Mathf.Max(1, hapticWarnPulseMilliseconds);
            hapticWarnGapMilliseconds = Mathf.Max(1, hapticWarnGapMilliseconds);
            hapticLowHpRatio = Mathf.Clamp01(hapticLowHpRatio);
            hapticRearmRatio = Mathf.Clamp(hapticRearmRatio, hapticLowHpRatio, 1f);
            hapticMinIntervalSeconds = Mathf.Max(0f, hapticMinIntervalSeconds);
            loadingMinShowSeconds = Mathf.Max(0f, loadingMinShowSeconds);
        }
    }
}
