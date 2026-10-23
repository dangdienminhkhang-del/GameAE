namespace Game.Core.Audio
{
    /// <summary>Chọn bài nhạc nền: có boss đang giao chiến thì nhạc boss, ngược lại nhạc gameplay. Thuần logic.</summary>
    public static class MusicRules
    {
        public static string ChooseTrack(int engagedBossCount, string gameplayId, string bossId)
        {
            return engagedBossCount > 0 ? bossId : gameplayId;
        }
    }
}
