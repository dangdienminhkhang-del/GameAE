namespace Game.Core.World
{
    /// <summary>
    /// Luật dịch chuyển nhanh giữa các Đèn Hiệu (docs/17 §6.2). Hàm thuần, test được không cần scene.
    /// • Chỉ tới Đèn Hiệu ĐÃ THẮP, cùng tầng (không đổi tầng: đổi tầng vẫn theo luật Vùng An Toàn của FloorLoader).
    /// • Không dùng khi đã chết hoặc trong 5 giây sau lần nhận sát thương cuối.
    /// • Kênh 3 giây; bị huỷ nếu trúng đòn hoặc người chơi tự di chuyển xa khỏi chỗ bắt đầu.
    /// </summary>
    public static class FastTravelRules
    {
        public const float ChannelSeconds = 3f;
        public const float CombatLockSeconds = 5f;
        public const float MoveCancelDistance = 0.75f;
        /// <summary>Đã đứng gần đích hơn mức này thì không cần dịch chuyển.</summary>
        public const float AlreadyThereDistance = 3f;

        public enum Result
        {
            Ok = 0,
            PlayerDead,
            InCombat,
            DestinationLocked,
            WrongFloor,
            AlreadyThere
        }

        public static Result Check(bool playerDead, float secondsSinceLastDamage, bool destinationLit, bool sameFloor, float distanceToDestination)
        {
            if (playerDead) return Result.PlayerDead;
            if (secondsSinceLastDamage < CombatLockSeconds) return Result.InCombat;
            if (!sameFloor) return Result.WrongFloor;
            if (!destinationLit) return Result.DestinationLocked;
            if (distanceToDestination < AlreadyThereDistance) return Result.AlreadyThere;
            return Result.Ok;
        }

        public static string Describe(Result r)
        {
            switch (r)
            {
                case Result.PlayerDead: return "Không thể dịch chuyển khi đã chết.";
                case Result.InCombat: return "Không thể dịch chuyển khi đang giao chiến.";
                case Result.DestinationLocked: return "Đèn Hiệu này chưa được thắp.";
                case Result.WrongFloor: return "Đèn Hiệu nằm ở tầng khác.";
                case Result.AlreadyThere: return "Bạn đang ở ngay cạnh Đèn Hiệu này.";
                default: return "";
            }
        }

        /// <summary>Kênh bị huỷ nếu người chơi đã rời chỗ bắt đầu quá <see cref="MoveCancelDistance"/>.</summary>
        public static bool ChannelBrokenByMovement(float distanceMoved)
        {
            return distanceMoved > MoveCancelDistance;
        }
    }
}
