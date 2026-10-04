namespace Game.Core.Quest
{
    public enum QuestStatus
    {
        Locked = 0,
        Available = 1,
        Active = 2,
        /// <summary>Objective đủ — chờ player nói chuyện NPC để turn-in và nhận thưởng.</summary>
        ReadyToTurnIn = 3,
        Completed = 4
    }
}
