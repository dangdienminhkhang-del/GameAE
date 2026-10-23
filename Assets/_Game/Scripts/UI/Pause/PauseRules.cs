namespace Game.UI.Pause
{
    /// <summary>Luật pause (thuần logic). Không pause khi đã chết, đang trong Death flow hoặc đang tải scene.</summary>
    public static class PauseRules
    {
        public static bool CanPause(bool deathFlowStarted, bool isLoading) => !deathFlowStarted && !isLoading;
    }

    /// <summary>
    /// Giữ cờ "đã vào Death flow" (đóng chốt: một khi đã chết thì KHÔNG BAO GIỜ cho pause lại trong scene này)
    /// và cờ "đang tải". Pause/Back đọc <see cref="CanPause"/>.
    /// </summary>
    public sealed class PauseGate
    {
        public bool DeathFlowStarted { get; private set; }
        public bool IsLoading { get; set; }

        /// <summary>Gọi khi nhận OnPlayerDied / OnPlayerDeathRecorded / OnDeathRecordUnconfirmed.</summary>
        public void MarkDeathFlow() => DeathFlowStarted = true;

        public bool CanPause => PauseRules.CanPause(DeathFlowStarted, IsLoading);

        public void Reset()
        {
            DeathFlowStarted = false;
            IsLoading = false;
        }
    }

    /// <summary>
    /// "Về Main Menu" bị khóa khi đang giao chiến (docs/09 §1.2): Pause không được thành nút thoát hiểm.
    /// Time.time dừng khi Pause nên KHÔNG hiện đếm ngược, chỉ hiện chữ khóa.
    /// </summary>
    public static class MainMenuQuitGate
    {
        public const string LockedLabel = "Đang giao chiến — tiếp tục chơi cho tới khi hết giao chiến";

        public static bool CanQuit(bool inCombat) => !inCombat;

        /// <summary>Chữ trạng thái cho nút (rỗng = không khóa).</summary>
        public static string LabelFor(bool inCombat) => inCombat ? LockedLabel : string.Empty;
    }
}
