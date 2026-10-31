using Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Guidance
{
    /// <summary>
    /// Dựng Canvas Screen Space Overlay riêng cho hướng dẫn (toast và mũi tên) bằng code, nên scene không cần kéo thêm gì.
    /// Thấp hơn bảng báo boss (sortingOrder 1000) để bảng đó luôn nằm trên cùng.
    /// </summary>
    internal static class GuidanceCanvasFactory
    {
        public const int ToastSortingOrder = 900;
        public const int ArrowSortingOrder = 800;
        public static readonly Vector2 ReferenceResolution = UiLayoutConstants.ReferenceResolution;

        public static Canvas Create(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiLayoutConstants.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = UiLayoutConstants.MatchWidthOrHeight;

            // Không có GraphicRaycaster: hướng dẫn không bao giờ nhận hay chặn cảm ứng.
            return canvas;
        }

        public static Font DefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
