using UnityEngine;

namespace Game.Core.Guidance
{
    public readonly struct QuestArrowResult
    {
        /// <summary>Mũi tên có hiện không (false = mục tiêu đã nằm trong khung nhìn hoặc dữ liệu không hợp lệ).</summary>
        public readonly bool Visible;
        /// <summary>Vị trí mũi tên theo pixel màn hình (gốc ở góc dưới trái, như Camera.WorldToScreenPoint), kẹp trong vùng an toàn trừ lề.</summary>
        public readonly Vector2 ScreenPosition;
        /// <summary>Góc xoay độ, 0 = chỉ sang phải (+X), tăng theo chiều ngược kim đồng hồ.</summary>
        public readonly float AngleDegrees;

        public QuestArrowResult(bool visible, Vector2 screenPosition, float angleDegrees)
        {
            Visible = visible;
            ScreenPosition = screenPosition;
            AngleDegrees = angleDegrees;
        }

        public static QuestArrowResult Hidden => new QuestArrowResult(false, Vector2.zero, 0f);
    }

    /// <summary>
    /// Toán thuần cho mũi tên quest bám mép màn hình (Phase 9.9). Không biết Camera hay UI: nhận vị trí mục tiêu đã đổi sang
    /// pixel màn hình và vùng an toàn (Screen.safeArea) rồi trả vị trí + góc. Đúng ở mọi tỉ lệ khung (16:9, 20:9...) vì chỉ dùng hình học.
    /// </summary>
    public static class QuestArrowMath
    {
        private const float Epsilon = 1e-6f;

        /// <param name="targetScreen">Vị trí mục tiêu theo pixel màn hình.</param>
        /// <param name="safeArea">Vùng an toàn của màn hình (pixel).</param>
        /// <param name="edgeMarginPixels">Mũi tên nằm cách mép vùng an toàn chừng này.</param>
        /// <param name="viewMarginPixels">Mục tiêu nằm trong vùng an toàn nới thêm chừng này thì ẩn mũi tên.</param>
        public static QuestArrowResult Compute(Vector2 targetScreen, Rect safeArea, float edgeMarginPixels, float viewMarginPixels)
        {
            if (!IsFinite(targetScreen) || !IsFinite(safeArea.center) || safeArea.width <= 0f || safeArea.height <= 0f)
                return QuestArrowResult.Hidden;

            float edge = edgeMarginPixels < 0f ? 0f : edgeMarginPixels;
            float view = viewMarginPixels < 0f ? 0f : viewMarginPixels;

            // Mục tiêu nằm trong khung nhìn (cộng lề) thì không cần mũi tên.
            var inflated = new Rect(safeArea.xMin - view, safeArea.yMin - view, safeArea.width + 2f * view, safeArea.height + 2f * view);
            if (inflated.Contains(targetScreen)) return QuestArrowResult.Hidden;

            Vector2 center = safeArea.center;
            Vector2 direction = targetScreen - center;
            if (direction.sqrMagnitude < Epsilon) return QuestArrowResult.Hidden;

            float halfX = Mathf.Max(0f, safeArea.width * 0.5f - edge);
            float halfY = Mathf.Max(0f, safeArea.height * 0.5f - edge);

            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);
            float scaleX = absX > Epsilon ? halfX / absX : float.PositiveInfinity;
            float scaleY = absY > Epsilon ? halfY / absY : float.PositiveInfinity;
            float scale = Mathf.Min(scaleX, scaleY);
            if (float.IsInfinity(scale) || float.IsNaN(scale)) return QuestArrowResult.Hidden;

            Vector2 position = center + direction * scale;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return new QuestArrowResult(true, position, angle);
        }

        /// <summary>Người chơi đã tới gần mục tiêu (trong bán kính, đơn vị thế giới) thì mũi tên ẩn.</summary>
        public static bool IsNear(Vector2 playerWorld, Vector2 targetWorld, float arrivalRadius)
        {
            if (!IsFinite(playerWorld) || !IsFinite(targetWorld)) return false;
            float radius = arrivalRadius < 0f ? 0f : arrivalRadius;
            return (targetWorld - playerWorld).sqrMagnitude <= radius * radius;
        }

        private static bool IsFinite(Vector2 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y);
        }
    }
}
