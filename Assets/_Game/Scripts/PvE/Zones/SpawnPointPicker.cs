using System;
using UnityEngine;

namespace Game.PvE.Zones
{
    /// <summary>
    /// Chọn điểm spawn KHÔNG bị chặn (thuần logic: hàm kiểm "bị chặn" và nguồn ngẫu nhiên do người gọi đưa vào).
    /// "Bị chặn" thường là: nằm trong Safe Zone, hoặc nằm ngoài ZoneVolume gán cho spawner.
    /// </summary>
    public static class SpawnPointPicker
    {
        public const int DefaultAttempts = 12;

        public readonly struct Result
        {
            public readonly Vector2 Position;
            /// <summary>True nếu thử hết số lần mà vẫn bị chặn nên trả về tâm spawn (đã được kiểm bố cục bằng ZoneLayoutValidator).</summary>
            public readonly bool UsedFallback;
            public readonly int Attempts;

            public Result(Vector2 position, bool usedFallback, int attempts)
            {
                Position = position;
                UsedFallback = usedFallback;
                Attempts = attempts;
            }
        }

        /// <param name="center">Tâm spawn.</param>
        /// <param name="radius">Bán kính lấy mẫu quanh tâm.</param>
        /// <param name="isBlocked">Trả true nếu điểm KHÔNG được phép spawn. Null = không có điểm nào bị chặn.</param>
        /// <param name="nextUnitCircle">Nguồn ngẫu nhiên: mỗi lần gọi trả một điểm trong hình tròn đơn vị.</param>
        /// <param name="maxAttempts">Số lần thử tối đa (tối thiểu 1).</param>
        public static Result Pick(Vector2 center, float radius, Func<Vector2, bool> isBlocked,
            Func<Vector2> nextUnitCircle, int maxAttempts = DefaultAttempts)
        {
            if (nextUnitCircle == null) return new Result(center, true, 0);
            if (float.IsNaN(radius) || radius < 0f) radius = 0f;
            int attempts = maxAttempts < 1 ? 1 : maxAttempts;

            for (int i = 1; i <= attempts; i++)
            {
                Vector2 point = center + nextUnitCircle() * radius;
                if (isBlocked == null || !isBlocked(point)) return new Result(point, false, i);
            }
            return new Result(center, true, attempts);
        }
    }
}
