using System.Collections.Generic;
using Game.Core.Quest;
using Game.Core.Time;
using UnityEngine;

namespace Game.Core.NPC
{
    /// <summary>
    /// NPC tĩnh (docs/03 §3, 08 §3.3): hội thoại tối thiểu + nhiệm vụ giao + chỉ xuất hiện ở một pha thời gian.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCDefinition", menuName = "Game/Core/NPC Definition")]
    public class NPCDefinition : ScriptableObject
    {
        [Header("Nhận dạng")]
        [SerializeField] private string id = "npc.new";
        [SerializeField] private string displayName = "NPC";

        [Header("Hội thoại")]
        [TextArea] [SerializeField] private List<string> dialogueLines = new List<string>();
        [TextArea] [SerializeField] private string questOfferLine = "Ngươi có muốn giúp ta không?";
        [TextArea] [SerializeField] private string questInProgressLine = "Xong việc rồi hãy quay lại.";
        [TextArea] [SerializeField] private string questCompletedLine = "Tốt lắm. Phần thưởng của ngươi đây.";

        [Header("Nhiệm vụ giao")]
        [SerializeField] private QuestDefinition offeredQuest;

        [Header("Thời gian hoạt động (docs/08 §3.3)")]
        [SerializeField] private bool onlyAtSpecificTime;
        [SerializeField] private TimeOfDay activeTimeOfDay = TimeOfDay.Night;

        public string Id => id;
        public string DisplayName => displayName;
        public IReadOnlyList<string> DialogueLines => dialogueLines;
        public string QuestOfferLine => questOfferLine;
        public string QuestInProgressLine => questInProgressLine;
        public string QuestCompletedLine => questCompletedLine;
        public QuestDefinition OfferedQuest => offeredQuest;
        public bool OnlyAtSpecificTime => onlyAtSpecificTime;
        public TimeOfDay ActiveTimeOfDay => activeTimeOfDay;

        public bool IsAvailableAt(TimeOfDay phase)
        {
            return !onlyAtSpecificTime || phase == activeTimeOfDay;
        }

        private void Reset()
        {
            id = "npc.new";
            displayName = "NPC";
            onlyAtSpecificTime = false;
        }
    }
}
