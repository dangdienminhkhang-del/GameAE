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
        private const string Folder = "UISkin/";
        private static bool _loaded;
        private static Sprite _panel;
        private static Sprite _button;
        private static Sprite _frame;

        /// <summary>true nếu bộ sprite đã sẵn sàng.</summary>
        public static bool Available
        {
            get { Load(); return _panel != null && _button != null && _frame != null; }
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _panel = Resources.Load<Sprite>(Folder + "UI_Panel");
            _button = Resources.Load<Sprite>(Folder + "UI_Button");
            _frame = Resources.Load<Sprite>(Folder + "UI_BarFrame");
        }

        private static void Slice(Image img, Sprite sprite, int artPixel)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(1, artPixel);
        }

        /// <summary>Khung bảng 9-slice (màu riêng, đặt màu trắng). Trả về true nếu đã áp dụng.</summary>
        public static bool Panel(Image img, int artPixel = 3)
        {
            Load();
            if (img == null || _panel == null) return false;
            Slice(img, _panel, artPixel);
            img.color = Color.white;
            return true;
        }

        /// <summary>Nút/hàng nổi. Giữ tông màu của Image, chỉ làm sáng nếu màu quá tối để thấy được vân.</summary>
        public static bool Button(Image img, int artPixel = 2)
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
        public static bool Inset(Image img, int artPixel = 1)
        {
            Load();
            if (img == null || _frame == null) return false;
            Slice(img, _frame, artPixel);
            img.color = new Color(1f, 1f, 1f, 0.9f);
            return true;
        }
    }
}
