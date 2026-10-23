using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Guidance
{
    /// <summary>Luật hàng đợi gợi ý (đều có tên, đổi ở <see cref="GuidanceConfig"/>).</summary>
    public readonly struct TutorialRuleSettings
    {
        public const int DefaultMaxQueued = 2;
        public const float DefaultMinGapSeconds = 1.5f;
        public const float DefaultBlockedTimeoutSeconds = 10f;

        /// <summary>Số gợi ý tối đa được CHỜ (không tính cái đang hiện).</summary>
        public readonly int MaxQueued;
        /// <summary>Khoảng cách tối thiểu giữa hai toast (giây).</summary>
        public readonly float MinGapSeconds;
        /// <summary>Đang bị chặn (Pause, chết, bảng báo boss mở, chưa có playerId) thì giữ chờ tối đa chừng này giây rồi bỏ.</summary>
        public readonly float BlockedTimeoutSeconds;

        public TutorialRuleSettings(int maxQueued, float minGapSeconds, float blockedTimeoutSeconds)
        {
            MaxQueued = maxQueued < 1 ? 1 : maxQueued;
            MinGapSeconds = minGapSeconds < 0f ? 0f : minGapSeconds;
            BlockedTimeoutSeconds = blockedTimeoutSeconds < 0f ? 0f : blockedTimeoutSeconds;
        }

        public static TutorialRuleSettings Default =>
            new TutorialRuleSettings(DefaultMaxQueued, DefaultMinGapSeconds, DefaultBlockedTimeoutSeconds);
    }

    /// <summary>Số liệu của mũi tên quest (đều có tên, đổi ở <see cref="GuidanceConfig"/>).</summary>
    public readonly struct QuestArrowSettings
    {
        public const float DefaultEdgeMarginPixels = 96f;
        public const float DefaultViewMarginPixels = 48f;
        public const float DefaultArrivalRadius = 3f;

        /// <summary>Mũi tên cách mép vùng an toàn của màn hình chừng này pixel.</summary>
        public readonly float EdgeMarginPixels;
        /// <summary>Mục tiêu nằm trong khung nhìn cộng lề này thì mũi tên ẩn.</summary>
        public readonly float ViewMarginPixels;
        /// <summary>Người chơi cách mục tiêu không quá bán kính này (đơn vị thế giới) thì coi là đã tới và ẩn mũi tên.</summary>
        public readonly float ArrivalRadius;

        public QuestArrowSettings(float edgeMarginPixels, float viewMarginPixels, float arrivalRadius)
        {
            EdgeMarginPixels = edgeMarginPixels < 0f ? 0f : edgeMarginPixels;
            ViewMarginPixels = viewMarginPixels < 0f ? 0f : viewMarginPixels;
            ArrivalRadius = arrivalRadius < 0f ? 0f : arrivalRadius;
        }

        public static QuestArrowSettings Default =>
            new QuestArrowSettings(DefaultEdgeMarginPixels, DefaultViewMarginPixels, DefaultArrivalRadius);
    }

    /// <summary>
    /// Cấu hình hệ hướng dẫn (Phase 9.9): danh mục hint, hằng số hàng đợi, cảm biến, toast và mũi tên.
    /// Không có số nào nằm trong code ngoài giá trị mặc định có tên.
    /// </summary>
    [CreateAssetMenu(fileName = "GuidanceConfig", menuName = "Game/Core/Guidance Config")]
    public sealed class GuidanceConfig : ScriptableObject
    {
        public const float MaxSensorHz = 4f;

        [Header("Danh mục gợi ý")]
        [SerializeField] private List<TutorialHintDefinition> hints = new List<TutorialHintDefinition>();

        [Header("Hàng đợi")]
        [SerializeField] private int maxQueued = TutorialRuleSettings.DefaultMaxQueued;
        [SerializeField] private float minGapSeconds = TutorialRuleSettings.DefaultMinGapSeconds;
        [SerializeField] private float blockedTimeoutSeconds = TutorialRuleSettings.DefaultBlockedTimeoutSeconds;

        [Header("Cảm biến (chạy theo nhịp, không mỗi frame)")]
        [Tooltip("Số lần kiểm mỗi giây, tối đa 4.")]
        [SerializeField] private float sensorHz = 4f;
        [Tooltip("Số nhịp liên tiếp phải đang di chuyển mới tính là 'đã di chuyển' (2 nhịp ở 4 lần/giây = khoảng 0.5 giây).")]
        [SerializeField] private int moveConfirmTicks = 2;
        [Tooltip("Tốc độ tối thiểu (đơn vị thế giới/giây) để tính là đang di chuyển.")]
        [SerializeField] private float moveMinSpeed = 0.3f;
        [Tooltip("Máu thấp: tỉ lệ máu (0-1) tại hoặc dưới đó thì bắn gợi ý.")]
        [Range(0.05f, 0.9f)] [SerializeField] private float lowHealthRatio = 0.35f;
        [Tooltip("Máu thấp: phải hồi lên tới tỉ lệ này mới được bắn lại (trễ chống bắn lặp).")]
        [Range(0.1f, 1f)] [SerializeField] private float lowHealthRearmRatio = 0.5f;

        [Header("Toast")]
        [SerializeField] private float toastFadeSeconds = 0.25f;
        [Tooltip("Khoảng cách từ mép trên màn hình (pixel theo độ phân giải tham chiếu 1920x1080).")]
        [SerializeField] private float toastTopOffset = 220f;
        [SerializeField] private float toastWidth = 1100f;
        [SerializeField] private float toastHeight = 90f;
        [SerializeField] private int toastFontSize = 36;

        [Header("Mũi tên quest")]
        [SerializeField] private float arrowEdgeMarginPixels = QuestArrowSettings.DefaultEdgeMarginPixels;
        [SerializeField] private float arrowViewMarginPixels = QuestArrowSettings.DefaultViewMarginPixels;
        [SerializeField] private float arrowArrivalRadius = QuestArrowSettings.DefaultArrivalRadius;
        [SerializeField] private float arrowSizePixels = 96f;
        [SerializeField] private Color arrowColor = new Color(1f, 0.85f, 0.2f, 0.95f);

        public IReadOnlyList<TutorialHintDefinition> Hints => hints;
        public TutorialRuleSettings Rules => new TutorialRuleSettings(maxQueued, minGapSeconds, blockedTimeoutSeconds);
        public QuestArrowSettings Arrow => new QuestArrowSettings(arrowEdgeMarginPixels, arrowViewMarginPixels, arrowArrivalRadius);

        public float SensorHz => Mathf.Clamp(sensorHz, 0.5f, MaxSensorHz);
        public float SensorIntervalSeconds => 1f / SensorHz;
        public int MoveConfirmTicks => moveConfirmTicks < 1 ? 1 : moveConfirmTicks;
        public float MoveMinSpeed => moveMinSpeed < 0f ? 0f : moveMinSpeed;
        public float LowHealthRatio => Mathf.Clamp(lowHealthRatio, 0.05f, 0.9f);
        public float LowHealthRearmRatio => Mathf.Max(LowHealthRatio, Mathf.Clamp(lowHealthRearmRatio, 0.1f, 1f));

        public float ToastFadeSeconds => toastFadeSeconds < 0.01f ? 0.01f : toastFadeSeconds;
        public float ToastTopOffset => toastTopOffset;
        public float ToastWidth => toastWidth;
        public float ToastHeight => toastHeight;
        public int ToastFontSize => toastFontSize;

        public float ArrowSizePixels => arrowSizePixels;
        public Color ArrowColor => arrowColor;

        /// <summary>ID mọi hint trong danh mục (dùng khi xóa cờ "đã xem").</summary>
        public List<string> HintIds()
        {
            var ids = new List<string>(hints.Count);
            for (int i = 0; i < hints.Count; i++)
            {
                if (hints[i] != null && !string.IsNullOrEmpty(hints[i].HintId)) ids.Add(hints[i].HintId);
            }
            return ids;
        }
    }
}
