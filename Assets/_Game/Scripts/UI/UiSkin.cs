using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Gắn sprite UI pixel art (khung thép + đồng) lên các Image do code tạo lúc chạy (Kho đồ, Cửa hàng...).
    /// Sprite nằm ở Resources/UISkin. Nếu thiếu sprite thì mọi hàm đều không làm gì (UI giữ kiểu ô màu phẳng cũ).
    /// Nút và hàng dùng sprite xám trung tính: màu cũ của Image vẫn nhuộm lên nên ý nghĩa màu được giữ.
    /// </summary>
    public static class UiSkin
    {
        public const int ArtPixel = 3;

        private const string Folder = "UISkin/";
        private static bool _loaded;
        private static Sprite _panel;
        private static Sprite _button;
        private static Sprite _frame;
        private static Sprite _slot;
        private static Sprite _row;
        private static Sprite _header;
        private static Sprite _toast;
        private static Sprite _iconButton;
        private static Sprite _coin;
        private static Font _defaultFont;
        private static Font _boldFont;

        /// <summary>true nếu bộ sprite cơ bản đã sẵn sàng.</summary>
        public static bool Available
        {
            get { Load(); return _panel != null && _button != null && _frame != null; }
        }

        /// <summary>Sprite biểu tượng đồng tiền Col.</summary>
        public static Sprite CoinIcon
        {
            get { Load(); return _coin; }
        }

        /// <summary>Phông chữ tiếng Việt chuẩn Be Vietnam Pro (SemiBold).</summary>
        public static Font DefaultFont
        {
            get { Load(); return _defaultFont; }
        }

        /// <summary>Phông chữ tiếng Việt chuẩn Be Vietnam Pro (Bold) cho tiêu đề/sát thương.</summary>
        public static Font BoldFont
        {
            get { Load(); return _boldFont; }
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _panel = Resources.Load<Sprite>(Folder + "UI_Panel");
            _button = Resources.Load<Sprite>(Folder + "UI_Button");
            _frame = Resources.Load<Sprite>(Folder + "UI_BarFrame");
            _slot = Resources.Load<Sprite>(Folder + "UI_Slot");
            _row = Resources.Load<Sprite>(Folder + "UI_Row");
            _header = Resources.Load<Sprite>(Folder + "UI_Header");
            _toast = Resources.Load<Sprite>(Folder + "UI_Toast");
            _iconButton = Resources.Load<Sprite>(Folder + "UI_IconButton");
            _coin = Resources.Load<Sprite>(Folder + "Icon_Coin");
            _defaultFont = Resources.Load<Font>(Folder + "Fonts/BeVietnamPro-SemiBold") 
                        ?? Resources.Load<Font>(Folder + "Fonts/BeVietnamPro-Regular");
            _boldFont = Resources.Load<Font>(Folder + "Fonts/BeVietnamPro-Bold") ?? _defaultFont;
        }

        /// <summary>Áp dụng phông chữ tiếng Việt chuẩn Be Vietnam Pro cho Text. Trả về true nếu đã áp dụng.</summary>
        public static bool ApplyFont(Text text, bool bold = false)
        {
            Load();
            Font f = bold ? _boldFont : _defaultFont;
            if (text == null || f == null) return false;
            text.font = f;
            return true;
        }

        private static void Slice(Image img, Sprite sprite, int artPixel)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(1, artPixel);
        }

        /// <summary>Khung bảng 9-slice (màu riêng, đặt màu trắng). Trả về true nếu đã áp dụng.</summary>
        public static bool Panel(Image img, int artPixel = ArtPixel)
        {
            Load();
            if (img == null || _panel == null) return false;
            Slice(img, _panel, artPixel);
            img.color = Color.white;
            return true;
        }

        /// <summary>Nút/hàng nổi. Giữ tông màu của Image, chỉ làm sáng nếu màu quá tối để thấy được vân.</summary>
        public static bool Button(Image img, int artPixel = ArtPixel)
        {
            Load();
            if (img == null || _button == null) return false;
            Color c = img.color;
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            Slice(img, _button, artPixel);
            if (m > 0.01f && m < 0.6f)
            {
                float f = Mathf.Min(3f, 0.6f / m);
                img.color = new Color(Mathf.Min(1f, c.r * f), Mathf.Min(1f, c.g * f), Mathf.Min(1f, c.b * f), c.a);
            }
            return true;
        }

        /// <summary>Ô trũng tối (nền hàng, nền thanh).</summary>
        public static bool Inset(Image img, int artPixel = ArtPixel)
        {
            Load();
            if (img == null || _frame == null) return false;
            Slice(img, _frame, artPixel);
            img.color = new Color(1f, 1f, 1f, 0.9f);
            return true;
        }

        /// <summary>Ô chứa vật phẩm / slot kho đồ 9-slice.</summary>
        public static bool Slot(Image img, int artPixel = ArtPixel)
        {
            Load();
            if (img == null || _slot == null) return false;
            Slice(img, _slot, artPixel);
            img.color = Color.white;
            return true;
        }

        /// <summary>Hàng danh sách 9-slice.</summary>
        public static bool Row(Image img, int artPixel = ArtPixel)
        {
            Load();
            if (img == null || _row == null) return false;
            Color c = img.color;
            Slice(img, _row, artPixel);
            // Giữ độ trong suốt nếu có, hoặc màu sáng nếu tối
            if (c.a < 0.05f) img.color = Color.white;
            else img.color = c;
            return true;
        }

        /// <summary>Dải tiêu đề / header 9-slice.</summary>
        public static bool Header(Image img, int artPixel = ArtPixel)
        {
            Load();
            if (img == null || _header == null) return false;
            Slice(img, _header, artPixel);
            img.color = Color.white;
            return true;
        }

        /// <summary>Khung thông báo nổi / tấm nhãn nhỏ 9-slice (UI_Toast).</summary>
        public static bool Toast(Image img, int artPixel = ArtPixel)
        {
            Load();
            if (img == null || _toast == null) return false;
            Slice(img, _toast, artPixel);
            img.color = Color.white;
            return true;
        }

        /// <summary>Nút icon huy hiệu (viền đồng + nền xanh than) 9-slice. Trả về true nếu đã áp dụng.</summary>
        public static bool IconButton(Image img, int artPixel = ArtPixel)
        {
            Load();
            if (img == null || _iconButton == null) return false;
            Slice(img, _iconButton, artPixel);
            img.color = Color.white;
            return true;
        }
    }
}
