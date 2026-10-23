namespace Game.Core.Combat
{
    /// <summary>
    /// Xếp hạng đẳng cấp combo (Style Ranking):
    /// Càng duy trì chuỗi đòn đánh, người chơi càng nhận nhiều buff tốc độ, sát thương và điểm thưởng.
    /// </summary>
    public enum ComboRank
    {
        None = 0,
        D = 1,
        C = 2,
        B = 3,
        A = 4,
        S = 5,
        SSS = 6
    }

    public static class ComboRankMath
    {
        public static ComboRank GetRank(int count)
        {
            if (count >= 50) return ComboRank.SSS;
            if (count >= 35) return ComboRank.S;
            if (count >= 20) return ComboRank.A;
            if (count >= 10) return ComboRank.B;
            if (count >= 5)  return ComboRank.C;
            if (count >= 1)  return ComboRank.D;
            return ComboRank.None;
        }

        public static double GetDamageMultiplier(ComboRank rank)
        {
            switch (rank)
            {
                case ComboRank.SSS: return 1.35;
                case ComboRank.S:   return 1.25;
                case ComboRank.A:   return 1.15;
                case ComboRank.B:   return 1.08;
                case ComboRank.C:   return 1.04;
                default:            return 1.00;
            }
        }

        public static float GetMoveSpeedBonus(ComboRank rank)
        {
            switch (rank)
            {
                case ComboRank.SSS: return 1.25f;
                case ComboRank.S:   return 1.20f;
                case ComboRank.A:   return 1.15f;
                case ComboRank.B:   return 1.10f;
                case ComboRank.C:   return 1.05f;
                default:            return 1.00f;
            }
        }
    }
}
