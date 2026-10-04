using System;
using Game.Core.Time;

namespace Game.Core.Quest
{
    /// <summary>Kết quả kiểm tra intent nhận quest, dùng cho UI giải thích nhưng không tự đổi state.</summary>
    public enum QuestAcceptResult
    {
        Ok = 0,
        Locked = 1,
        WrongTime = 2,
        TimeUnavailable = 3,
        AlreadyActive = 4,
        AlreadyCompleted = 5
    }

    /// <summary>Luật quest thuần, không biết MonoBehaviour, UI, inventory hay save.</summary>
    public static class QuestRules
    {
        public static QuestAcceptResult CanAccept(
            QuestDefinition definition,
            QuestLog log,
            TimeOfDay phase,
            bool timeAuthoritative)
        {
            if (definition == null || log == null) return QuestAcceptResult.Locked;

            QuestState state = log.Get(definition.Id);
            if (state != null)
            {
                if (state.IsActive || state.IsReadyToTurnIn) return QuestAcceptResult.AlreadyActive;
                if (state.IsCompleted && !definition.Repeatable) return QuestAcceptResult.AlreadyCompleted;
            }

            foreach (string prerequisiteId in definition.PrerequisiteQuestIds)
            {
                if (!string.IsNullOrWhiteSpace(prerequisiteId) && !log.IsCompleted(prerequisiteId))
                    return QuestAcceptResult.Locked;
            }

            if (!definition.RequiresSpecificTime) return QuestAcceptResult.Ok;
            if (!timeAuthoritative) return QuestAcceptResult.TimeUnavailable;
            return phase == definition.RequiredTimeOfDay
                ? QuestAcceptResult.Ok
                : QuestAcceptResult.WrongTime;
        }

        /// <summary>Time-gated objectives may advance only while the trusted phase matches.</summary>
        public static bool CanAdvance(
            QuestDefinition definition,
            QuestLog log,
            QuestObjectiveType objectiveType,
            string targetId,
            TimeOfDay phase,
            bool timeAuthoritative)
        {
            if (definition == null || log == null || definition.ObjectiveType != objectiveType) return false;
            if (!log.IsActive(definition.Id) || !string.Equals(definition.TargetId, targetId, StringComparison.Ordinal)) return false;
            return !definition.RequiresSpecificTime ||
                   (timeAuthoritative && phase == definition.RequiredTimeOfDay);
        }

        /// <summary>Collect starts from committed stack quantity; non-positive values seed zero.</summary>
        public static int SeedCollectProgress(QuestDefinition definition, int ownedCount)
        {
            if (definition == null || definition.ObjectiveType != QuestObjectiveType.Collect) return 0;
            return Math.Min(definition.RequiredCount, Math.Max(0, ownedCount));
        }
    }
}
