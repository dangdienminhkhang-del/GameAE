using UnityEngine;

namespace Game.Core.Facing
{
    /// <summary>
    /// Tâm vùng đánh theo hướng nhìn: vị trí + hướng * offset.x + (0, offset.y).
    /// Offset.x là khoảng cách "phía trước" (đi theo hướng nhìn, độ dài giữ nguyên ở mọi hướng); offset.y là độ lệch
    /// dọc theo trục Y thế giới và KHÔNG xoay theo hướng nhìn (giữ nghĩa như trước 9.5a).
    /// Khi hướng là (+1, 0) hoặc (-1, 0), kết quả TRÙNG TỪNG CON SỐ với công thức cũ (x + offset.x * facing, y + offset.y).
    /// </summary>
    public static class HitboxMath
    {
        public static Vector2 Center(Vector2 position, Vector2 facingDirection, Vector2 offset)
        {
            float x = position.x + facingDirection.x * offset.x;
            float y = position.y + facingDirection.y * offset.x + offset.y;
            return new Vector2(x, y);
        }
    }
}
