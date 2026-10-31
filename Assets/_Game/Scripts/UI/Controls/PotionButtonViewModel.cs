using Game.Core.Items;

namespace Game.UI.Controls
{
    /// <summary>
    /// Trạng thái hiển thị của nút Bình (thuần logic): UI chỉ đọc, không tự suy luận luật dùng bình.
    /// Lý do khóa lấy từ <see cref="ConsumableRules.DescribeResult"/>, không dùng chuỗi tự chế.
    /// </summary>
    public readonly struct PotionButtonViewModel
    {
        public const float DimAlpha = 0.75f;
        /// <summary>Sáng (dùng được) hay mờ.</summary>
        public readonly bool Available;
        public readonly float Alpha;
        /// <summary>Chữ trạng thái dưới nút (rỗng khi sáng). Đang giao chiến/đang chờ có đếm ngược giây.</summary>
        public readonly string StatusText;
        /// <summary>Chữ số lượng còn lại.</summary>
        public readonly string CountText;
        /// <summary>Chữ toast khi chạm vào nút mờ (rỗng khi sáng).</summary>
        public readonly string ToastText;

        private PotionButtonViewModel(bool available, string status, string count, string toast)
        {
            Available = available;
            Alpha = available ? 1f : DimAlpha;
            StatusText = status;
            CountText = count;
            ToastText = toast;
        }

        public static PotionButtonViewModel From(ConsumableUseResult result, int quantity, float secondsRemaining)
        {
            string count = quantity < 0 ? "0" : quantity.ToString();
            if (result == ConsumableUseResult.Ok) return new PotionButtonViewModel(true, string.Empty, count, string.Empty);

            string reason = ConsumableRules.DescribeResult(result);
            string status = reason;
            if ((result == ConsumableUseResult.InCombat || result == ConsumableUseResult.OnCooldown) && secondsRemaining > 0f)
            {
                int whole = (int)System.Math.Ceiling(secondsRemaining);
                status = reason + " " + whole + "s";
            }
            return new PotionButtonViewModel(false, status, count, reason);
        }
    }
}
