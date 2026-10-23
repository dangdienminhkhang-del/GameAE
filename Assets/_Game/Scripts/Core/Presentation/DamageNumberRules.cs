using System;
using System.Globalization;
using UnityEngine;

namespace Game.Core.Presentation
{
    public enum DamageNumberKind
    {
        /// <summary>Người chơi gây ra.</summary>
        Dealt,
        /// <summary>Người chơi gây ra, chí mạng.</summary>
        DealtCritical,
        /// <summary>Người chơi nhận vào.</summary>
        Received
    }

    /// <summary>Luật hiển thị số sát thương nổi: làm tròn, phân loại màu, chống chồng chữ, chuỗi cache. Thuần logic.</summary>
    public static class DamageNumberRules
    {
        private const int CachedMax = 999;
        private static readonly string[] s_textCache = BuildCache();

        /// <summary>Số nguyên hiển thị: làm tròn (AwayFromZero), tối thiểu 1 để đòn trúng nào cũng thấy số.</summary>
        public static int ToDisplayValue(double amount)
        {
            if (double.IsNaN(amount)) return 1;
            if (amount >= int.MaxValue) return int.MaxValue;
            int value = (int)Math.Round(amount, MidpointRounding.AwayFromZero);
            return value < 1 ? 1 : value;
        }

        public static DamageNumberKind Classify(bool receivedByPlayer, bool isCritical)
        {
            if (receivedByPlayer) return DamageNumberKind.Received;
            return isCritical ? DamageNumberKind.DealtCritical : DamageNumberKind.Dealt;
        }

        /// <summary>Chuỗi số đã cache sẵn cho 0..999 → không cấp phát mỗi hit.</summary>
        public static string GetText(int value)
        {
            if (value >= 0 && value <= CachedMax) return s_textCache[value];
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Đếm số điểm đang hoạt động nằm trong <paramref name="radius"/> quanh <paramref name="point"/>.</summary>
        public static int CountNear(Vector2[] activePoints, int count, Vector2 point, float radius)
        {
            if (activePoints == null || count <= 0) return 0;

            int limit = Math.Min(count, activePoints.Length);
            float radiusSqr = radius * radius;
            int near = 0;
            for (int i = 0; i < limit; i++)
            {
                if ((activePoints[i] - point).sqrMagnitude <= radiusSqr) near++;
            }
            return near;
        }

        /// <summary>Dời lên theo số lượng số đã có ở gần để chữ không chồng nhau.</summary>
        public static Vector2 StackOffset(int stackIndex, float stepPixels)
        {
            if (stackIndex <= 0) return Vector2.zero;
            return new Vector2(0f, stackIndex * stepPixels);
        }

        private static string[] BuildCache()
        {
            var cache = new string[CachedMax + 1];
            for (int i = 0; i < cache.Length; i++) cache[i] = i.ToString(CultureInfo.InvariantCulture);
            return cache;
        }
    }
}
