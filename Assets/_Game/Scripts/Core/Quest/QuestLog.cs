using System.Collections.Generic;

namespace Game.Core.Quest
{
    /// <summary>Trạng thái 1 nhiệm vụ của người chơi (docs/02: LOCKED → AVAILABLE → ACTIVE → COMPLETED).</summary>
    public class QuestState
    {
        public string questId;
        public QuestStatus status = QuestStatus.Available;
        public int progress;

        public QuestState() { }

        public QuestState(string questId, QuestStatus status = QuestStatus.Available, int progress = 0)
        {
            this.questId = questId;
            this.status = status;
            this.progress = progress;
        }

        public bool IsActive => status == QuestStatus.Active;
        public bool IsReadyToTurnIn => status == QuestStatus.ReadyToTurnIn;
        public bool IsCompleted => status == QuestStatus.Completed;
    }

    /// <summary>Sổ nhiệm vụ thuần logic — không biết UI, không biết save.</summary>
    public class QuestLog
    {
        private readonly Dictionary<string, QuestState> _states = new Dictionary<string, QuestState>();

        public IReadOnlyDictionary<string, QuestState> States => _states;

        public QuestState Get(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            return _states.TryGetValue(questId, out QuestState state) ? state : null;
        }

        public QuestState GetOrCreate(string questId)
        {
            QuestState state = Get(questId);
            if (state != null) return state;

            state = new QuestState(questId);
            _states[questId] = state;
            return state;
        }

        /// <summary>
        /// Cộng tiến độ; trả về true nếu vừa đủ objective (chuyển sang ReadyToTurnIn).
        /// Reward chỉ được cấp sau khi TurnIn() được gọi.
        /// </summary>
        public bool AddProgress(string questId, int requiredCount, int amount = 1)
        {
            QuestState state = Get(questId);
            if (state == null || !state.IsActive) return false;

            state.progress += amount;
            if (state.progress < requiredCount) return false;

            state.progress = requiredCount;
            state.status = QuestStatus.ReadyToTurnIn;
            return true;
        }

        /// <summary>
        /// Player đã nói chuyện NPC hợp lệ → chuyển ReadyToTurnIn sang Completed.
        /// Trả về true nếu thành công (gọi một lần duy nhất).
        /// </summary>
        public bool TurnIn(string questId)
        {
            QuestState state = Get(questId);
            if (state == null || state.status != QuestStatus.ReadyToTurnIn) return false;
            state.status = QuestStatus.Completed;
            return true;
        }

        public void Accept(string questId)
        {
            QuestState state = GetOrCreate(questId);
            // Không hạ về Active nếu đã Completed hoặc đang ReadyToTurnIn
            if (state.IsCompleted || state.IsReadyToTurnIn) return;
            state.status = QuestStatus.Active;
        }

        public bool IsActive(string questId)
        {
            QuestState state = Get(questId);
            return state != null && state.IsActive;
        }

        public bool IsReadyToTurnIn(string questId)
        {
            QuestState state = Get(questId);
            return state != null && state.IsReadyToTurnIn;
        }

        public bool IsCompleted(string questId)
        {
            QuestState state = Get(questId);
            return state != null && state.IsCompleted;
        }

        public void LoadFrom(IEnumerable<QuestState> states)
        {
            _states.Clear();
            if (states == null) return;
            foreach (QuestState state in states)
            {
                if (state != null && !string.IsNullOrEmpty(state.questId)) _states[state.questId] = state;
            }
        }
    }
}
