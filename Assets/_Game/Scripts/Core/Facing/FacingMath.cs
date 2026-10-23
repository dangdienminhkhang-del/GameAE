using UnityEngine;

namespace Game.Core.Facing
{
    /// <summary>
    /// Toán thuần cho hướng nhìn 8 hướng (docs/13 Wave 9.5a, docs/14 §4.1). Không phụ thuộc scene nên test được trong EditMode.
    /// </summary>
    public static class FacingMath
    {
        public const int DirectionCount = 8;
        public const float StepDegrees = 45f;
        public const float HalfStepDegrees = 22.5f;

        private const float Diagonal = 0.70710678f;
        private const float MinSqrMagnitude = 1e-12f;

        /// <summary>Vector dùng được (không NaN, không vô cực, độ dài khác 0).</summary>
        public static bool IsUsable(Vector2 v)
        {
            if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y)) return false;
            return v.sqrMagnitude > MinSqrMagnitude;
        }

        /// <summary>
        /// Làm tròn một vector tới hướng gần nhất trong 8 hướng. Vector không dùng được (gần 0, NaN) thì trả
        /// <paramref name="fallback"/> do người gọi truyền vào (thường là hướng đang nhìn).
        /// </summary>
        public static Direction8 FromVector(Vector2 v, Direction8 fallback)
        {
            if (!IsUsable(v)) return fallback;

            float degrees = AngleDegrees(v);
            int index = Mathf.FloorToInt(degrees / StepDegrees + 0.5f) % DirectionCount;
            return (Direction8)index;
        }

        /// <summary>Góc của vector trong [0, 360), đếm từ +X ngược chiều kim đồng hồ.</summary>
        public static float AngleDegrees(Vector2 v)
        {
            float degrees = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (degrees < 0f) degrees += 360f;
            if (degrees >= 360f) degrees -= 360f;
            return degrees;
        }

        /// <summary>Góc của một hướng, trong [0, 360).</summary>
        public static float AngleDegrees(Direction8 direction)
        {
            return ((int)direction % DirectionCount) * StepDegrees;
        }

        /// <summary>Độ lệch góc nhỏ nhất giữa hai góc (độ), luôn trong [0, 180].</summary>
        public static float AngularDistance(float degreesA, float degreesB)
        {
            return Mathf.Abs(Mathf.DeltaAngle(degreesA, degreesB));
        }

        /// <summary>
        /// Vector đơn vị của một hướng. Dùng giá trị ghi sẵn (không tính bằng sin/cos) nên trục Đông/Tây/Bắc/Nam
        /// ra đúng (±1, 0) hoặc (0, ±1), không có nhiễu số.
        /// </summary>
        public static Vector2 ToVector(Direction8 direction)
        {
            switch (direction)
            {
                case Direction8.East: return new Vector2(1f, 0f);
                case Direction8.NorthEast: return new Vector2(Diagonal, Diagonal);
                case Direction8.North: return new Vector2(0f, 1f);
                case Direction8.NorthWest: return new Vector2(-Diagonal, Diagonal);
                case Direction8.West: return new Vector2(-1f, 0f);
                case Direction8.SouthWest: return new Vector2(-Diagonal, -Diagonal);
                case Direction8.South: return new Vector2(0f, -1f);
                case Direction8.SouthEast: return new Vector2(Diagonal, -Diagonal);
                default: return new Vector2(1f, 0f);
            }
        }

        /// <summary>
        /// Khe hình (5 khe: Nam, Đông-Nam, Đông, Đông-Bắc, Bắc) và cờ lật ngang cho một hướng.
        /// Tây-Nam, Tây, Tây-Bắc dùng khe Đông-Nam, Đông, Đông-Bắc rồi lật ngang.
        /// </summary>
        public static ArtFacing ToArtSlot(Direction8 direction)
        {
            switch (direction)
            {
                case Direction8.East: return new ArtFacing(ArtSlot.East, false);
                case Direction8.NorthEast: return new ArtFacing(ArtSlot.NorthEast, false);
                case Direction8.North: return new ArtFacing(ArtSlot.North, false);
                case Direction8.NorthWest: return new ArtFacing(ArtSlot.NorthEast, true);
                case Direction8.West: return new ArtFacing(ArtSlot.East, true);
                case Direction8.SouthWest: return new ArtFacing(ArtSlot.SouthEast, true);
                case Direction8.South: return new ArtFacing(ArtSlot.South, false);
                case Direction8.SouthEast: return new ArtFacing(ArtSlot.SouthEast, false);
                default: return new ArtFacing(ArtSlot.East, false);
            }
        }

        /// <summary>
        /// Giá trị gửi cho Animator: FaceX là GIÁ TRỊ TUYỆT ĐỐI của thành phần ngang (phía Tây đã được lật bằng flipX),
        /// FaceY giữ dấu. Quy ước (chốt ở gói 9.5a): trục = 1 hoặc 0, chéo = 0.7071 (vector đơn vị), nên một Blend Tree
        /// 2D đặt ngưỡng ở S (0,-1), SE (0.71,-0.71), E (1,0), NE (0.71,0.71), N (0,1) khớp docs/14 §4.1.
        /// </summary>
        public static void ToAnimatorParams(Direction8 direction, out float faceX, out float faceY)
        {
            Vector2 v = ToVector(direction);
            faceX = Mathf.Abs(v.x);
            faceY = v.y;
        }
    }
}
