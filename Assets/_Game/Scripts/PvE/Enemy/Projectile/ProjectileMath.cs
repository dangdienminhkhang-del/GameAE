using System;
using UnityEngine;

namespace Game.PvE.Enemy.Projectile
{
    /// <summary>
    /// Toán thuần của đạn quái: bước đi, va chạm đoạn–tròn (không xuyên khi dt lớn), hết giờ, thời gian bay tối thiểu
    /// để né được, và làm tròn damage đạn. Chỉ dùng Vector2/Mathf, test được không cần scene.
    /// </summary>
    public static class ProjectileMath
    {
        /// <summary>
        /// Thời gian bay tối thiểu từ shootRange tới người chơi để né được trên điện thoại (giả định của package 9.8,
        /// product owner đổi được ở đây). Dodge không có i-frames nên né = di chuyển ra khỏi đường đạn.
        /// </summary>
        public const float MinDodgeFlightSeconds = 0.5f;

        /// <summary>Vị trí sau một bước đi thẳng. Tốc độ hoặc dt âm thì không đi lùi.</summary>
        public static Vector2 Step(Vector2 position, Vector2 direction, float speed, float deltaTime)
        {
            float s = speed > 0f ? speed : 0f;
            float dt = deltaTime > 0f ? deltaTime : 0f;
            return position + direction * (s * dt);
        }

        /// <summary>Đạn đã sống hết lifetime chưa (tuổi ≥ lifetime).</summary>
        public static bool IsExpired(float age, float lifetime)
        {
            return age >= lifetime;
        }

        /// <summary>
        /// Vị trí (0..1) trên đoạn a→b gần tâm <paramref name="center"/> nhất. Dùng để so thứ tự giữa tường và người chơi trên đường đạn.
        /// </summary>
        public static float ClosestApproachFraction(Vector2 a, Vector2 b, Vector2 center)
        {
            Vector2 ab = b - a;
            float lengthSqr = ab.sqrMagnitude;
            if (lengthSqr < 1e-12f) return 0f;
            float t = Vector2.Dot(center - a, ab) / lengthSqr;
            return t < 0f ? 0f : (t > 1f ? 1f : t);
        }

        public static float FlightSeconds(float distance, float speed)
        {
            if (speed <= 0f) return float.PositiveInfinity;
            return Mathf.Max(0f, distance) / speed;
        }

        /// <summary>Đạn bay từ shootRange tới người chơi có đủ chậm để né không (≥ MinDodgeFlightSeconds).</summary>
        public static bool IsDodgeable(float shootRange, float speed)
        {
            return FlightSeconds(shootRange, speed) >= MinDodgeFlightSeconds;
        }

        /// <summary>
        /// Đoạn a→b có chạm hình tròn (tâm center, bán kính radius) không. Kiểm CẢ ĐOẠN nên bước dài (dt lớn)
        /// không xuyên qua mục tiêu. Bán kính âm coi như 0. Đoạn dài 0 = kiểm một điểm.
        /// </summary>
        public static bool SegmentHitsCircle(Vector2 a, Vector2 b, Vector2 center, float radius)
        {
            float r = radius > 0f ? radius : 0f;
            Vector2 d = b - a;
            Vector2 f = a - center;

            float c = f.sqrMagnitude - r * r;
            if (c <= 0f) return true; // điểm đầu đã nằm trong hình tròn

            float aa = d.sqrMagnitude;
            if (aa < 1e-12f) return false; // không di chuyển và nằm ngoài

            float bb = 2f * Vector2.Dot(f, d);
            float disc = bb * bb - 4f * aa * c;
            if (disc < 0f) return false;

            float t = (-bb - Mathf.Sqrt(disc)) / (2f * aa);
            return t >= 0f && t <= 1f;
        }

        /// <summary>
        /// Damage đạn = damage quái × hệ số, làm tròn cùng quy tắc EnemyController.ScaleStat (Math.Round, AwayFromZero).
        /// Hệ số ≤ 0, damage ≤ 0 hoặc NaN thì 0; hệ số dương nhỏ vẫn cho tối thiểu 1 để đạn không vô hình.
        /// </summary>
        public static double ScaleDamage(double amount, float scale)
        {
            if (double.IsNaN(amount) || amount <= 0.0 || scale <= 0f || float.IsNaN(scale)) return 0.0;
            double scaled = Math.Round(amount * scale, MidpointRounding.AwayFromZero);
            return Math.Max(1.0, scaled);
        }
    }
}
