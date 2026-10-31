using System;

namespace Game.Core.PvP
{
    public enum DuelMode
    {
        FirstStrike = 0, // Đòn đánh đầu tiên trúng đích sẽ giành chiến thắng
        HalfLoss = 1,    // Bên nào còn <= 50% HP thì người kia thắng (Chuẩn an toàn SAO)
        TotalLoss = 2    // Hết 100% HP thì thua (Không kích hoạt Permadeath)
    }

    public enum DuelState
    {
        None = 0,
        Countdown = 1,
        InProgress = 2,
        Finished = 3
    }

    /// <summary>
    /// Quy tắc Đấu Tay Đôi (Duel PvP) theo Sword Art Online (docs/16 & prompt).
    /// Hỗ trợ 3 chế độ truyền thống của SAO: First-Strike, Half-Loss, Total-Loss.
    /// Luật bất thành văn: Total-Loss trong khuôn khổ Duel được hệ thống bảo hộ,
    /// không bao giờ gây cái chết thực sự (không kích hoạt Permadeath).
    /// Logic thuần C#, dễ kiểm thử.
    /// </summary>
    public static class DuelRules
    {
        public const float CountdownSeconds = 3.0f;

        /// <summary>
        /// Kiểm tra xem trận Duel đã kết thúc hay chưa.
        /// winnerIndex: 1 nếu Người 1 thắng, 2 nếu Người 2 thắng, 0 nếu Hoà, -1 nếu Chưa kết thúc.
        /// </summary>
        public static bool CheckDuelEnd(
            DuelMode mode,
            double p1Hp, double p1MaxHp,
            double p2Hp, double p2MaxHp,
            int p1HitsDealt, int p2HitsDealt,
            out int winnerIndex)
        {
            winnerIndex = -1;

            switch (mode)
            {
                case DuelMode.FirstStrike:
                    if (p1HitsDealt > 0 && p2HitsDealt == 0)
                    {
                        winnerIndex = 1;
                        return true;
                    }
                    if (p2HitsDealt > 0 && p1HitsDealt == 0)
                    {
                        winnerIndex = 2;
                        return true;
                    }
                    if (p1HitsDealt > 0 && p2HitsDealt > 0)
                    {
                        winnerIndex = 0; // Hoà
                        return true;
                    }
                    return false;

                case DuelMode.HalfLoss:
                    bool p1HalfLost = p1Hp <= p1MaxHp * 0.5;
                    bool p2HalfLost = p2Hp <= p2MaxHp * 0.5;

                    if (p1HalfLost && p2HalfLost)
                    {
                        winnerIndex = 0; // Hoà
                        return true;
                    }
                    if (p1HalfLost)
                    {
                        winnerIndex = 2;
                        return true;
                    }
                    if (p2HalfLost)
                    {
                        winnerIndex = 1;
                        return true;
                    }
                    return false;

                case DuelMode.TotalLoss:
                    bool p1Dead = p1Hp <= 0.0;
                    bool p2Dead = p2Hp <= 0.0;

                    if (p1Dead && p2Dead)
                    {
                        winnerIndex = 0; // Hoà
                        return true;
                    }
                    if (p1Dead)
                    {
                        winnerIndex = 2;
                        return true;
                    }
                    if (p2Dead)
                    {
                        winnerIndex = 1;
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Lấy tên tiếng Việt của chế độ Duel theo SAO.
        /// </summary>
        public static string GetModeName(DuelMode mode)
        {
            switch (mode)
            {
                case DuelMode.FirstStrike:
                    return "Đòn Đầu Tiên (First-Strike)";
                case DuelMode.HalfLoss:
                    return "Nửa Máu (Half-Loss)";
                case DuelMode.TotalLoss:
                    return "Toàn Lực (Total-Loss)";
                default:
                    return mode.ToString();
            }
        }
    }
}
