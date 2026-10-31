using UnityEngine;

namespace Game.UI.Common
{
    /// <summary>
    /// Các hằng số chuẩn hóa bố cục UI toàn bộ game (Gói U1..U5).
    /// Chuẩn hóa tham chiếu 1920x1080, Match = 1 (chiều cao cố định 1080 trên mọi màn hình ngang).
    /// </summary>
    public static class UiLayoutConstants
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public static readonly Vector2 ReferenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        public const float MatchWidthOrHeight = 1.0f; // 1 = Match Height

        /// <summary>Lề tối thiểu từ mép vùng an toàn cho các phần tử bám mép (canvas px).</summary>
        public const float EdgeMargin = 24f;

        /// <summary>Kích thước tối thiểu cho mọi nút tương tác (canvas px, ~48 dp).</summary>
        public const float MinButtonSize = 120f;

        /// <summary>Kích thước nút kỹ năng, né, tuyệt kỹ (canvas px, 48 art px * 3).</summary>
        public const float CombatButtonSize = 144f;

        /// <summary>Kích thước nút đánh chính (canvas px, 64 art px * 3).</summary>
        public const float AttackButtonSize = 192f;

        /// <summary>Khoảng cách tối đa từ góc dưới phải tới tâm mọi nút chiến đấu (canvas px).</summary>
        public const float CombatClusterMaxRadius = 620f;

        /// <summary>Khoảng hở tối thiểu giữa hai nút bất kỳ (canvas px).</summary>
        public const float MinButtonSpacing = 16f;

        /// <summary>Cỡ chữ tối thiểu cho các thành phần UI.</summary>
        public const int MinFontSize = 24;
        public const int ButtonLabelFontSize = 30;
        public const int BodyFontSize = 28;
        public const int TitleFontSize = 44;
        public const int HudNumberFontSize = 28;

        /// <summary>Tỷ lệ tối đa của bảng popup so với vùng an toàn (92%).</summary>
        public const float MaxPanelScreenRatio = 0.92f;
        public const float MaxPanelWidth = 900f;
    }
}
