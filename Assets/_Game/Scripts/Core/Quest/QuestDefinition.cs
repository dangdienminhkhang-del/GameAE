using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Quest
{
    public enum QuestObjectiveType { Kill = 0, Collect = 1, Talk = 2, Reach = 3 }

    /// <summary>
    /// Nhiệm vụ tĩnh (docs/02 §? / 03 §3, time-aware theo docs/08 §3.4).
    /// Balance/thưởng nằm ở asset, không hard-code.
    /// </summary>
    [UnityEngine.CreateAssetMenu(fileName = "QuestDefinition", menuName = "Game/Core/Quest Definition")]
    public class QuestDefinition : UnityEngine.ScriptableObject
    {
        [Header("Nhận dạng")]
        [SerializeField] private string id = "quest.new";
        [SerializeField] private string title = "Nhiệm vụ";
        [SerializeField] private string description = "";

        [Header("Mục tiêu")]
        [SerializeField] private QuestObjectiveType objectiveType = QuestObjectiveType.Kill;
        [Tooltip("Kill: id quái (enemy.slime) — Collect: chỉ hỗ trợ item stackable theo id item — Talk: id NPC — Reach: id vùng")]
        [SerializeField] private string targetId = "enemy.slime";
        [SerializeField] private int requiredCount = 3;

        [Header("Tiên quyết")]
        [Tooltip("ID quest phải hoàn thành trước khi có thể nhận quest này.")]
        [SerializeField] private string[] prerequisiteQuestIds = Array.Empty<string>();

        [Header("Thưởng")]
        [SerializeField] private int experienceReward = 150;
        [SerializeField] private Game.Core.Items.ItemDefinition itemReward;
        [SerializeField] private int itemRewardCount = 1;

        [Header("Điều kiện (time-aware — docs/08 §3.4)")]
        [SerializeField] private bool requiresSpecificTime;
        [SerializeField] private Game.Core.Time.TimeOfDay requiredTimeOfDay = Game.Core.Time.TimeOfDay.Night;

        [Tooltip("Quest lặp lại được (làm lại sau khi hoàn thành) — tiện cho nhiệm vụ demo/hằng ngày.")]
        [SerializeField] private bool repeatable;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public QuestObjectiveType ObjectiveType => objectiveType;
        public string TargetId => targetId;
        public int RequiredCount => requiredCount < 1 ? 1 : requiredCount;
        public IReadOnlyList<string> PrerequisiteQuestIds => prerequisiteQuestIds ?? Array.Empty<string>();
        public int ExperienceReward => experienceReward;
        public Game.Core.Items.ItemDefinition ItemReward => itemReward;
        public int ItemRewardCount => itemRewardCount < 1 ? 1 : itemRewardCount;
        public bool RequiresSpecificTime => requiresSpecificTime;
        public Game.Core.Time.TimeOfDay RequiredTimeOfDay => requiredTimeOfDay;
        public bool Repeatable => repeatable;

        public string ObjectiveText(int progress)
        {
            string verb;
            switch (objectiveType)
            {
                case QuestObjectiveType.Kill: verb = "Hạ"; break;
                case QuestObjectiveType.Collect: verb = "Thu thập"; break;
                case QuestObjectiveType.Talk: verb = "Nói chuyện với"; break;
                default: verb = "Đi tới"; break;
            }
            return $"{verb} {targetId} ({progress}/{RequiredCount})";
        }

        private void Reset()
        {
            id = "quest.new";
            title = "Nhiệm vụ";
            objectiveType = QuestObjectiveType.Kill;
            targetId = "enemy.slime";
            requiredCount = 3;
            experienceReward = 150;
        }
    }
}
