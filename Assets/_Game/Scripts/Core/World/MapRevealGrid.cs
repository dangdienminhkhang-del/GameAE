using System;

namespace Game.Core.World
{
    /// <summary>
    /// Lưới "sương chiến tranh" của một tầng: chia thế giới thành các khối <see cref="BlockSize"/>×<see cref="BlockSize"/> ô,
    /// mỗi khối một bit (đã khám phá hay chưa). Lớp thuần (không biết scene/Unity), tuần tự hoá thành base64.
    /// Khối được coi là đã khám phá khi nó giao với hình tròn quanh người chơi.
    /// </summary>
    public sealed class MapRevealGrid
    {
        public const int BlockSize = 8;

        private readonly byte[] _bits;

        public MapRevealGrid(float minX, float minY, float width, float height)
        {
            MinX = minX;
            MinY = minY;
            BlocksX = Math.Max(1, (int)Math.Ceiling(Math.Max(1f, width) / BlockSize));
            BlocksY = Math.Max(1, (int)Math.Ceiling(Math.Max(1f, height) / BlockSize));
            _bits = new byte[(BlocksX * BlocksY + 7) / 8];
        }

        public float MinX { get; }
        public float MinY { get; }
        public int BlocksX { get; }
        public int BlocksY { get; }
        public int TotalBlocks => BlocksX * BlocksY;
        public int RevealedCount { get; private set; }

        /// <summary>Tăng mỗi khi lưới đổi (UI so sánh để biết có cần vẽ lại).</summary>
        public int Version { get; private set; }

        /// <summary>Tỉ lệ đã khám phá, 0..1.</summary>
        public float Fraction => TotalBlocks == 0 ? 0f : (float)RevealedCount / TotalBlocks;

        public bool IsRevealed(int bx, int by)
        {
            if (bx < 0 || by < 0 || bx >= BlocksX || by >= BlocksY) return false;
            int i = by * BlocksX + bx;
            return (_bits[i >> 3] & (1 << (i & 7))) != 0;
        }

        public bool IsRevealedAt(float x, float y)
        {
            return TryGetBlock(x, y, out int bx, out int by) && IsRevealed(bx, by);
        }

        public bool TryGetBlock(float x, float y, out int bx, out int by)
        {
            bx = (int)Math.Floor((x - MinX) / BlockSize);
            by = (int)Math.Floor((y - MinY) / BlockSize);
            return bx >= 0 && by >= 0 && bx < BlocksX && by < BlocksY;
        }

        private bool SetBit(int bx, int by)
        {
            int i = by * BlocksX + bx;
            byte mask = (byte)(1 << (i & 7));
            if ((_bits[i >> 3] & mask) != 0) return false;
            _bits[i >> 3] |= mask;
            RevealedCount++;
            return true;
        }

        private static float Clamp(float v, float lo, float hi) { return v < lo ? lo : (v > hi ? hi : v); }

        /// <summary>Vẽ mọi khối giao với hình tròn (x, y, radius). Trả về true nếu có khối MỚI.</summary>
        public bool RevealCircle(float x, float y, float radius)
        {
            if (radius <= 0f) return false;
            int bx0 = Math.Max(0, (int)Math.Floor((x - radius - MinX) / BlockSize));
            int bx1 = Math.Min(BlocksX - 1, (int)Math.Floor((x + radius - MinX) / BlockSize));
            int by0 = Math.Max(0, (int)Math.Floor((y - radius - MinY) / BlockSize));
            int by1 = Math.Min(BlocksY - 1, (int)Math.Floor((y + radius - MinY) / BlockSize));
            float r2 = radius * radius;
            bool changed = false;
            for (int by = by0; by <= by1; by++)
            {
                for (int bx = bx0; bx <= bx1; bx++)
                {
                    float x0 = MinX + bx * BlockSize, y0 = MinY + by * BlockSize;
                    float dx = x - Clamp(x, x0, x0 + BlockSize);
                    float dy = y - Clamp(y, y0, y0 + BlockSize);
                    if (dx * dx + dy * dy <= r2 && SetBit(bx, by)) changed = true;
                }
            }
            if (changed) Version++;
            return changed;
        }

        public string ToBase64()
        {
            return Convert.ToBase64String(_bits);
        }

        /// <summary>
        /// Gộp (OR) bản đã lưu vào lưới hiện tại — không bao giờ làm mất phần đã vẽ. Sai kích thước/hỏng thì bỏ qua và trả false.
        /// </summary>
        public bool MergeBase64(string base64, int blocksX, int blocksY)
        {
            if (string.IsNullOrEmpty(base64) || blocksX != BlocksX || blocksY != BlocksY) return false;
            byte[] data;
            try { data = Convert.FromBase64String(base64); }
            catch (FormatException) { return false; }
            if (data.Length != _bits.Length) return false;

            bool changed = false;
            for (int i = 0; i < data.Length; i++)
            {
                byte merged = (byte)(_bits[i] | data[i]);
                if (merged != _bits[i]) { _bits[i] = merged; changed = true; }
            }
            if (!changed) return false;

            int count = 0;
            for (int i = 0; i < TotalBlocks; i++)
                if ((_bits[i >> 3] & (1 << (i & 7))) != 0) count++;
            RevealedCount = count;
            Version++;
            return true;
        }
    }
}
