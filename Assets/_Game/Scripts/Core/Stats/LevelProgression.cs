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
            ApplyPendingLevelUps();
        }

        /// <summary>
        /// Đặt lại tiến trình (dùng khi spawn/reset nhân vật và khi nạp save).
        /// Save cũ có EXP ≥ EXPNext theo công thức mới (docs/07 §1, Task B2) thì lên level tiếp
        /// bằng EXP dư — không mất tiến độ, không clamp EXP.
        /// </summary>
        public void ResetTo(int level, int currentExp = 0)
        {
            Level = level < 1 ? 1 : level;
            CurrentExp = currentExp < 0 ? 0 : currentExp;
            ApplyPendingLevelUps();
        }

        /// <summary>Cộng EXP, trả về số level đã lên.</summary>
        public int AddExperience(int amount)
        {
            if (amount <= 0) return 0;

            CurrentExp += amount;
            return ApplyPendingLevelUps();
        }

        /// <summary>
        /// Trừ EXP dư để lên level cho tới khi EXP &lt; EXPNext. Trả về số level đã lên.
        /// Không giới hạn số level mỗi lần: EXPNext tăng đều và luôn &gt; 0 nên vòng lặp luôn dừng.
        /// </summary>
        private int ApplyPendingLevelUps()
        {
            int levelUps = 0;
            int required = RequiredExp;
            while (CurrentExp >= required)
            {
                CurrentExp -= required;
                Level++;
                levelUps++;
                required = RequiredExp;
            }
            return levelUps;
        }
    }
}
