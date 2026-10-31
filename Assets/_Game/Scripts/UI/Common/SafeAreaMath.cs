using UnityEngine;

namespace Game.UI.Common
{
    /// <summary>
    /// Toán học vùng an toàn (Safe Area) thuần túy không phụ thuộc Scene/GameObject.
    /// Giúp tính toán neo chuẩn hóa (normalized anchors) và kích thước canvas tương thích mọi tỷ lệ màn hình (4:3 đến 21:9).
    /// </summary>
    public static class SafeAreaMath
    {
        /// <summary>
        /// Tính anchorMin và anchorMax chuẩn hóa (0..1) từ safeArea và screenSize.
        /// Rơi về toàn màn hình (0,0)-(1,1) nếu safeArea lỗi (NaN, âm, rỗng, lớn hơn màn hình).
        /// </summary>
        public static void CalculateNormalizedAnchors(Rect safeArea, Vector2 screenSize, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f ||
                float.IsNaN(screenSize.x) || float.IsNaN(screenSize.y) ||
                float.IsInfinity(screenSize.x) || float.IsInfinity(screenSize.y))
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            if (float.IsNaN(safeArea.x) || float.IsNaN(safeArea.y) ||
                float.IsNaN(safeArea.width) || float.IsNaN(safeArea.height) ||
                float.IsInfinity(safeArea.x) || float.IsInfinity(safeArea.y) ||
                float.IsInfinity(safeArea.width) || float.IsInfinity(safeArea.height))
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            // Nếu safeArea vượt quá giới hạn màn hình (lớn hơn màn hình)
            const float epsilon = 0.05f;
            if (safeArea.xMin < -epsilon || safeArea.yMin < -epsilon ||
                safeArea.xMax > screenSize.x + epsilon || safeArea.yMax > screenSize.y + epsilon ||
                safeArea.width > screenSize.x + epsilon || safeArea.height > screenSize.y + epsilon)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            float minX = Mathf.Clamp01(safeArea.xMin / screenSize.x);
            float minY = Mathf.Clamp01(safeArea.yMin / screenSize.y);
            float maxX = Mathf.Clamp01(safeArea.xMax / screenSize.x);
            float maxY = Mathf.Clamp01(safeArea.yMax / screenSize.y);

            if (minX >= maxX || minY >= maxY)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            anchorMin = new Vector2(minX, minY);
            anchorMax = new Vector2(maxX, maxY);
        }

        /// <summary>
        /// Tính kích thước canvas thực tế theo thuật toán CanvasScaler (ScaleWithScreenSize, MatchWidthOrHeight).
        /// </summary>
        public static Vector2 CalculateCanvasSize(Vector2 screenSize, Vector2 referenceResolution, float match = 1f)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f) return referenceResolution;
            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f) return screenSize;

            match = Mathf.Clamp01(match);
            float logW = Mathf.Log(screenSize.x / referenceResolution.x, 2f);
            float logH = Mathf.Log(screenSize.y / referenceResolution.y, 2f);
            float logWeightedScale = Mathf.Lerp(logW, logH, match);
            float scaleFactor = Mathf.Pow(2f, logWeightedScale);

            return new Vector2(screenSize.x / scaleFactor, screenSize.y / scaleFactor);
        }

        /// <summary>
        /// Tính vùng an toàn theo đơn vị tọa độ điểm ảnh Canvas (0..canvasWidth, 0..canvasHeight).
        /// </summary>
        public static Rect CalculateCanvasSafeArea(Rect safeArea, Vector2 screenSize, Vector2 canvasSize)
        {
            CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);
            return new Rect(
                anchorMin.x * canvasSize.x,
                anchorMin.y * canvasSize.y,
                (anchorMax.x - anchorMin.x) * canvasSize.x,
                (anchorMax.y - anchorMin.y) * canvasSize.y
            );
        }
    }
}
