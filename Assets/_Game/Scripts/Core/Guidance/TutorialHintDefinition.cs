using UnityEngine;

namespace Game.Core.Guidance
{
    /// <summary>
    /// Một gợi ý hướng dẫn (Phase 9.9): chữ ngắn "giọng hệ thống", hiện một lần cho mỗi người chơi.
    /// Dữ liệu thuần: tên hint, chữ, độ ưu tiên và tín hiệu đều nằm ở asset, không hard-code trong code.
    /// Chữ do dự án tự đặt, tối đa <see cref="MaxTextLength"/> ký tự, product owner duyệt trước khi chốt.
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialHint", menuName = "Game/Core/Tutorial Hint")]
    public sealed class TutorialHintDefinition : ScriptableObject
    {
        public const string DefaultSpeakerLabel = "Hệ thống";
        public const float DefaultDisplaySeconds = 4f;
        public const float MinDisplaySeconds = 0.5f;
        public const int MaxTextLength = 70;

        [Tooltip("ID ổn định, dùng làm khóa cờ 'đã xem' (ví dụ hint.move). Đổi ID = người chơi thấy lại gợi ý.")]
        [SerializeField] private string hintId = "hint.new";
        [TextArea] [SerializeField] private string text = "";
        [SerializeField] private string speakerLabel = DefaultSpeakerLabel;
        [Tooltip("Số LỚN hơn = quan trọng hơn (hiện trước khi nhiều gợi ý cùng chờ).")]
        [SerializeField] private int priority;
        [SerializeField] private float displaySeconds = DefaultDisplaySeconds;
        [SerializeField] private TutorialTrigger trigger = TutorialTrigger.None;
        [Tooltip("Người chơi làm việc này thì gợi ý tự ẩn sớm. None = chỉ ẩn sau displaySeconds.")]
        [SerializeField] private TutorialComplete completeKind = TutorialComplete.None;

        public string HintId => hintId;
        public string Text => text;
        public string SpeakerLabel => string.IsNullOrEmpty(speakerLabel) ? DefaultSpeakerLabel : speakerLabel;
        public int Priority => priority;
        public float DisplaySeconds => displaySeconds < MinDisplaySeconds ? MinDisplaySeconds : displaySeconds;
        public TutorialTrigger Trigger => trigger;
        public TutorialComplete CompleteKind => completeKind;

        public TutorialHint ToHint()
        {
            return new TutorialHint(HintId, Text, SpeakerLabel, Priority, DisplaySeconds, CompleteKind);
        }

        /// <summary>Gán dữ liệu bằng code (test hoặc công cụ Editor). Asset thật gán qua Inspector.</summary>
        public void Configure(string id, string hintText, int hintPriority, TutorialTrigger hintTrigger, TutorialComplete hintComplete,
            float seconds = DefaultDisplaySeconds, string speaker = DefaultSpeakerLabel)
        {
            hintId = id;
            text = hintText;
            priority = hintPriority;
            trigger = hintTrigger;
            completeKind = hintComplete;
            displaySeconds = seconds;
            speakerLabel = speaker;
        }

        private void Reset()
        {
            hintId = "hint.new";
            speakerLabel = DefaultSpeakerLabel;
            displaySeconds = DefaultDisplaySeconds;
        }
    }
}
