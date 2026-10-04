namespace Game.Core.Stats
{
    /// <summary>
    /// Tiến trình level thuần logic (test được, không cần scene).
    /// Quy tắc: EXP dư sau khi lên level được giữ lại cho level kế (docs/07 §1).
    /// </summary>
    public class LevelProgression
    {
        public int Level { get; private set; }
        public int CurrentExp { get; private set; }
        public int RequiredExp => CharacterStats.ExpRequiredForNextLevel(Level);

        public LevelProgression(int level = 1, int currentExp = 0)
        {
            Level = level < 1 ? 1 : level;
            CurrentExp = currentExp < 0 ? 0 : currentExp;
        }

        /// <summary>Đặt lại tiến trình (dùng khi spawn/reset nhân vật).</summary>
        public void ResetTo(int level, int currentExp = 0)
        {
            Level = level < 1 ? 1 : level;
            CurrentExp = currentExp < 0 ? 0 : currentExp;
        }

        /// <summary>Cộng EXP, trả về số level đã lên.</summary>
        public int AddExperience(int amount)
        {
            if (amount <= 0) return 0;

            CurrentExp += amount;
            int levelUps = 0;
            int required = RequiredExp;
            while (CurrentExp >= required && levelUps < MaxLevelUpsPerCall)
            {
                CurrentExp -= required;
                Level++;
                levelUps++;
                required = RequiredExp;
                if (required <= 0) break;
            }
            return levelUps;
        }

        private const int MaxLevelUpsPerCall = 100;
    }
}
