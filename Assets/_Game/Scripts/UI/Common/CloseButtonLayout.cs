using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Common
{
    /// <summary>
    /// Kiểu chung cho nút đóng (X) ở góc trên-phải của panel.
    /// Trước đây nút X bị phình 120x120 (theo MinButtonSize) nên đè lên tiêu đề và hàng đầu tiên của danh sách.
    /// Giờ hình hiển thị nhỏ gọn nằm gọn trong dải tiêu đề, còn vùng chạm vẫn được nới rộng sang trái/lên/phải
    /// (KHÔNG nới xuống dưới để không chặn nội dung bên dưới).
    /// </summary>
    public static class CloseButtonLayout
    {
        public const float VisibleSize = 76f;
        public const int LabelFontSize = 40;
        public static readonly Vector2 Inset = new Vector2(14f, 12f);

        /// <summary>Padding raycast (trái, dưới, phải, trên): giá trị ÂM = nới vùng chạm ra ngoài hình hiển thị.</summary>
        public static readonly Vector4 TouchPadding = new Vector4(-22f, 0f, -14f, -18f);

        public static void Apply(Button button)
        {
            if (button == null) return;
            Apply(button.transform as RectTransform, button.targetGraphic, button.GetComponentInChildren<Text>(true));
        }

        public static void Apply(RectTransform rect, Graphic graphic, Text label)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-Inset.x, -Inset.y);
            rect.sizeDelta = new Vector2(VisibleSize, VisibleSize);

            if (graphic != null) graphic.raycastPadding = TouchPadding;
            if (label != null) label.fontSize = LabelFontSize;
        }
    }
}
