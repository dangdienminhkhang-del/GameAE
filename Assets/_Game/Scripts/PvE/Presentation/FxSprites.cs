using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>Sprite hiệu ứng nhỏ tạo bằng code (chấm, vòng, tia) — pixel-art, filter Point, PPU 32.</summary>
    public static class FxSprites
    {
        private static Sprite _dot, _ring, _spark;

        public static Sprite Dot => _dot != null ? _dot : (_dot = Build(8, (x, y, c) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            return d <= 3.5f ? (d <= 2f ? 1f : 0.6f) : 0f;
        }));

        public static Sprite Ring => _ring != null ? _ring : (_ring = Build(32, (x, y, c) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            return d >= 13f && d <= 15.5f ? 1f : (d >= 11.5f && d < 13f ? 0.35f : 0f);
        }));

        public static Sprite Spark => _spark != null ? _spark : (_spark = Build(8, (x, y, c) =>
        {
            int dx = Mathf.Abs(x - 3), dy = Mathf.Abs(y - 3);
            return (dx == 0 && dy <= 3) || (dy == 0 && dx <= 3) || (dx <= 1 && dy <= 1) ? 1f : 0f;
        }));

        private static Sprite Build(int size, System.Func<int, int, float, float> alphaAt)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            float center = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alphaAt(x, y, center)));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
