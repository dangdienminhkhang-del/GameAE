using System;

namespace Game.UI.Audio
{
    /// <summary>Luật chọn âm cho nút UI theo tên nút (quy ước đặt tên). Thuần logic, test được.</summary>
    public static class UiSfxRules
    {
        private static readonly string[] BackFragments = { "back", "close", "cancel" };

        /// <summary>Nút có tên chứa back/close/cancel (không phân biệt hoa thường) → âm quay lại; còn lại → âm bấm.</summary>
        public static string PickSfxId(string buttonName)
        {
            if (!string.IsNullOrEmpty(buttonName))
            {
                for (int i = 0; i < BackFragments.Length; i++)
                {
                    if (buttonName.IndexOf(BackFragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return Game.Core.Audio.AudioIds.SfxUiBack;
                    }
                }
            }
            return Game.Core.Audio.AudioIds.SfxUiClick;
        }

        /// <summary>True nếu tên nút chứa một trong các mảnh cần bỏ qua (ví dụ nút hành động chiến đấu đã có SFX riêng).</summary>
        public static bool ShouldSkip(string buttonName, string[] skipFragments)
        {
            if (string.IsNullOrEmpty(buttonName) || skipFragments == null) return false;
            for (int i = 0; i < skipFragments.Length; i++)
            {
                string fragment = skipFragments[i];
                if (string.IsNullOrEmpty(fragment)) continue;
                if (buttonName.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }
}
