using System.Collections.Generic;
using Game.Core.Quest;
using UnityEngine;

namespace Game.Core.Guidance
{
    public enum QuestTargetKind
    {
        None = 0,
        /// <summary>Một NPC theo npcId (Talk, hoặc nơi trả quest).</summary>
        Npc = 1,
        /// <summary>Một vùng theo zoneId (Reach).</summary>
        Zone = 2,
        /// <summary>Vùng Farm gần người chơi nhất của tầng hiện tại (Kill, Collect).</summary>
        NearestFarm = 3
    }

    public readonly struct QuestTarget
    {
        public readonly QuestTargetKind Kind;
        /// <summary>npcId hoặc zoneId tùy <see cref="Kind"/>; rỗng với NearestFarm và None.</summary>
        public readonly string Id;

        public QuestTarget(QuestTargetKind kind, string id)
        {
            Kind = kind;
            Id = id;
        }

        public static QuestTarget None => new QuestTarget(QuestTargetKind.None, null);
    }

    /// <summary>Thông tin một quest để chọn mục tiêu (đã tách khỏi PlayerQuestLog và asset để test thuần).</summary>
    public readonly struct QuestTargetCandidate
    {
        public readonly string QuestId;
        public readonly QuestStatus Status;
        public readonly QuestObjectiveType Objective;
        public readonly string ObjectiveTargetId;
        /// <summary>npcId của NPC nhận trả quest này; rỗng nếu quest tự hoàn thành (không cần trả).</summary>
        public readonly string TurnInNpcId;

        public QuestTargetCandidate(string questId, QuestStatus status, QuestObjectiveType objective, string objectiveTargetId, string turnInNpcId)
        {
            QuestId = questId;
            Status = status;
            Objective = objective;
            ObjectiveTargetId = objectiveTargetId;
            TurnInNpcId = turnInNpcId;
        }
    }

    /// <summary>
    /// Chọn mục tiêu cho mũi tên quest (Phase 9.9), thuần logic.
    /// Quest được chọn: quest ĐẦU TIÊN (theo thứ tự danh sách quest của người chơi) đang Active hoặc ReadyToTurnIn và cho ra một mục tiêu.
    /// Active: Talk → NPC theo npcId; Reach → vùng theo zoneId; Kill/Collect → Vùng Farm gần nhất.
    /// ReadyToTurnIn: chỉ tới NPC nhận trả quest; quest không cần trả ở NPC thì bỏ qua (mũi tên ẩn nếu không còn quest nào khác).
    /// </summary>
    public static class QuestTargetRules
    {
        public static QuestTarget Select(IReadOnlyList<QuestTargetCandidate> candidates)
        {
            if (candidates == null) return QuestTarget.None;

            for (int i = 0; i < candidates.Count; i++)
            {
                QuestTarget target = ForCandidate(candidates[i]);
                if (target.Kind != QuestTargetKind.None) return target;
            }
            return QuestTarget.None;
        }

        public static QuestTarget ForCandidate(QuestTargetCandidate quest)
        {
            if (quest.Status == QuestStatus.ReadyToTurnIn)
            {
                return string.IsNullOrEmpty(quest.TurnInNpcId)
                    ? QuestTarget.None
                    : new QuestTarget(QuestTargetKind.Npc, quest.TurnInNpcId);
            }

            if (quest.Status != QuestStatus.Active) return QuestTarget.None;

            switch (quest.Objective)
            {
                case QuestObjectiveType.Talk:
                    return string.IsNullOrEmpty(quest.ObjectiveTargetId)
                        ? QuestTarget.None
                        : new QuestTarget(QuestTargetKind.Npc, quest.ObjectiveTargetId);
                case QuestObjectiveType.Reach:
                    return string.IsNullOrEmpty(quest.ObjectiveTargetId)
                        ? QuestTarget.None
                        : new QuestTarget(QuestTargetKind.Zone, quest.ObjectiveTargetId);
                case QuestObjectiveType.Kill:
                case QuestObjectiveType.Collect:
                    return new QuestTarget(QuestTargetKind.NearestFarm, null);
                default:
                    return QuestTarget.None;
            }
        }

        /// <summary>Chỉ số điểm gần <paramref name="from"/> nhất; -1 nếu danh sách rỗng.</summary>
        public static int NearestIndex(Vector2 from, IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count == 0) return -1;

            int best = -1;
            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                float sqr = (points[i] - from).sqrMagnitude;
                if (float.IsNaN(sqr)) continue;
                if (best < 0 || sqr < bestSqr)
                {
                    best = i;
                    bestSqr = sqr;
                }
            }
            return best;
        }
    }
}
