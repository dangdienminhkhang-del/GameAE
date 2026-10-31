using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.UI.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Công cụ kiểm tra bố cục UI (Menu Game/UI/Kiem tra bo cuc).
    /// Kiểm tra 8+ độ phân giải và các mẫu tai thỏ theo yêu cầu Gói U1..U5.
    /// </summary>
    public static class UiLayoutChecker
    {
        public struct TestCase
        {
            public string Name;
            public Vector2 ScreenSize;
            public Rect SafeArea;

            public TestCase(string name, float width, float height, Rect? safeArea = null)
            {
                Name = name;
                ScreenSize = new Vector2(width, height);
                SafeArea = safeArea ?? new Rect(0f, 0f, width, height);
            }
        }

        public static readonly TestCase[] StandardTestCases = new[]
        {
            new TestCase("1280x720 (16:9)", 1280f, 720f),
            new TestCase("1920x1080 (16:9 FHD)", 1920f, 1080f),
            new TestCase("2160x1080 (18:9)", 2160f, 1080f),
            new TestCase("2340x1080 (19.5:9)", 2340f, 1080f),
            new TestCase("2400x1080 (20:9)", 2400f, 1080f),
            new TestCase("2560x1080 (21.3:9)", 2560f, 1080f),
            new TestCase("2000x1200 (5:3 Tablet)", 2000f, 1200f),
            new TestCase("2048x1536 (4:3 Tablet)", 2048f, 1536f),
            new TestCase("2400x1080 (Tai thỏ trái 130px)", 2400f, 1080f, new Rect(130f, 0f, 2270f, 1080f)),
            new TestCase("2400x1080 (Tai thỏ trái 130px + lề dưới 60px)", 2400f, 1080f, new Rect(130f, 60f, 2270f, 1020f))
        };

        public class CheckReport
        {
            public int TotalTestCases;
            public int OverlapCount;
            public int UndersizedButtonCount;
            public int OutsideSafeAreaCount;
            public int PanelCombatOverlapCount;
            public List<string> Details = new List<string>();

            public bool IsClean => OverlapCount == 0 && UndersizedButtonCount == 0 && OutsideSafeAreaCount == 0 && PanelCombatOverlapCount == 0;
        }

        [MenuItem("Game/UI/Kiem tra bo cuc")]
        public static void RunCheckMenuItem()
        {
            CheckReport report = RunLayoutCheck();
            var sb = new StringBuilder();
            sb.AppendLine("=== KẾT QUẢ KIỂM TRA BỐ CỤC GIAO DIỆN (UI LAYOUT CHECK) ===");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Số độ phân giải kiểm tra: {0}\nNút chồng nhau: {1}\nNút nhỏ (<120x120): {2}\nPhần tử ngoài vùng an toàn: {3}\nBảng chồng nút chiến đấu: {4}",
                report.TotalTestCases, report.OverlapCount, report.UndersizedButtonCount, report.OutsideSafeAreaCount, report.PanelCombatOverlapCount));

            if (report.Details.Count > 0)
            {
                sb.AppendLine("\nChi tiết cảnh báo/lỗi:");
                foreach (string d in report.Details)
                {
                    sb.AppendLine(" - " + d);
                }
            }

            if (report.IsClean)
            {
                sb.AppendLine("\n>>> HOÀN TOÀN HỢP LỆ! Không có xung đột giao diện trên mọi tỉ lệ.");
                Debug.Log(sb.ToString());
            }
            else
            {
                Debug.LogWarning(sb.ToString());
            }
        }

        public static CheckReport RunLayoutCheck()
        {
            var report = new CheckReport();
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas mainCanvas = null;
            foreach (var c in canvases)
            {
                if (c.name == "Canvas" && c.gameObject.scene.name == "PvE_Gameplay")
                {
                    mainCanvas = c;
                    break;
                }
            }

            if (mainCanvas == null)
            {
                foreach (var c in canvases)
                {
                    if (c.name == "Canvas") { mainCanvas = c; break; }
                }
            }

            if (mainCanvas == null)
            {
                report.Details.Add("Không tìm thấy Canvas chính trong scene đang mở.");
                return report;
            }

            report.TotalTestCases = StandardTestCases.Length;

            // Thu thập các nút HUD chính (đang active trong SafeAreaRoot)
            var hudButtons = new List<RectTransform>();
            Transform safeAreaRoot = mainCanvas.transform.Find("SafeAreaRoot");
            if (safeAreaRoot != null)
            {
                foreach (var b in safeAreaRoot.GetComponentsInChildren<Button>(false))
                {
                    var rt = b.GetComponent<RectTransform>();
                    if (rt != null && !hudButtons.Contains(rt)) hudButtons.Add(rt);
                }
            }
            else
            {
                foreach (var b in mainCanvas.GetComponentsInChildren<Button>(false))
                {
                    var rt = b.GetComponent<RectTransform>();
                    if (rt != null && !hudButtons.Contains(rt)) hudButtons.Add(rt);
                }
            }

            for (int t = 0; t < StandardTestCases.Length; t++)
            {
                var tc = StandardTestCases[t];
                Vector2 canvasSize = SafeAreaMath.CalculateCanvasSize(tc.ScreenSize, UiLayoutConstants.ReferenceResolution, UiLayoutConstants.MatchWidthOrHeight);
                Rect canvasSafeArea = SafeAreaMath.CalculateCanvasSafeArea(tc.SafeArea, tc.ScreenSize, canvasSize);

                // 1. Kiểm tra kích thước nút HUD
                foreach (var rt in hudButtons)
                {
                    if (rt.name.StartsWith("Notice_") || rt.name == "Handle") continue;

                    Vector2 size = GetEffectiveSize(rt, canvasSize, canvasSafeArea);
                    if (size.x < UiLayoutConstants.MinButtonSize - 1f || size.y < UiLayoutConstants.MinButtonSize - 1f)
                    {
                        if (t == 0)
                        {
                            report.UndersizedButtonCount++;
                            report.Details.Add(string.Format(CultureInfo.InvariantCulture,
                                "Nút '{0}' kích thước ({1:F0}x{2:F0}) nhỏ hơn chuẩn tối thiểu ({3}x{3})",
                                rt.name, size.x, size.y, UiLayoutConstants.MinButtonSize));
                        }
                    }

                    // 2. Kiểm tra phần tử có nằm ngoài vùng an toàn không
                    Rect r = ResolveCanvasRect(rt, canvasSize, canvasSafeArea);
                    const float tolerance = 2f;
                    if (r.xMin < canvasSafeArea.xMin - tolerance || r.xMax > canvasSafeArea.xMax + tolerance ||
                        r.yMin < canvasSafeArea.yMin - tolerance || r.yMax > canvasSafeArea.yMax + tolerance)
                    {
                        report.OutsideSafeAreaCount++;
                        report.Details.Add(string.Format(CultureInfo.InvariantCulture,
                            "[{0}] Nút '{1}' nằm ngoài vùng an toàn (Rect: {2}, SafeArea: {3})",
                            tc.Name, rt.name, r, canvasSafeArea));
                    }
                }

                // 3. Kiểm tra chồng lấn giữa các nút HUD đang hoạt động
                for (int i = 0; i < hudButtons.Count; i++)
                {
                    for (int j = i + 1; j < hudButtons.Count; j++)
                    {
                        var b1 = hudButtons[i];
                        var b2 = hudButtons[j];
                        if (b1.parent == b2 || b2.parent == b1) continue;

                        Rect r1 = ResolveCanvasRect(b1, canvasSize, canvasSafeArea);
                        Rect r2 = ResolveCanvasRect(b2, canvasSize, canvasSafeArea);

                        if (r1.Overlaps(r2))
                        {
                            report.OverlapCount++;
                            report.Details.Add(string.Format(CultureInfo.InvariantCulture,
                                "[{0}] Cặp nút chồng nhau: '{1}' và '{2}'", tc.Name, b1.name, b2.name));
                        }
                    }
                }
            }

            return report;
        }

        private static Vector2 GetEffectiveSize(RectTransform rt, Vector2 canvasSize, Rect safeArea)
        {
            Vector2 delta = rt.sizeDelta;
            if (rt.anchorMin != rt.anchorMax)
            {
                Vector2 parentSize = rt.parent is RectTransform prt ? prt.rect.size : canvasSize;
                delta = new Vector2(
                    parentSize.x * (rt.anchorMax.x - rt.anchorMin.x) + rt.sizeDelta.x,
                    parentSize.y * (rt.anchorMax.y - rt.anchorMin.y) + rt.sizeDelta.y
                );
            }
            return new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
        }

        public static Rect ResolveCanvasRect(RectTransform rt, Vector2 canvasSize, Rect safeArea)
        {
            Vector2 anchorMin = rt.anchorMin;
            Vector2 anchorMax = rt.anchorMax;
            Vector2 offsetMin = rt.offsetMin;
            Vector2 offsetMax = rt.offsetMax;
            Vector2 pivot = rt.pivot;

            // Xác định hệ tọa độ phụ huynh
            Rect parentRect = new Rect(0f, 0f, canvasSize.x, canvasSize.y);
            if (rt.parent != null && rt.parent.name == "SafeAreaRoot")
            {
                parentRect = safeArea;
            }

            float xMin = parentRect.xMin + anchorMin.x * parentRect.width + offsetMin.x;
            float xMax = parentRect.xMin + anchorMax.x * parentRect.width + offsetMax.x;
            float yMin = parentRect.yMin + anchorMin.y * parentRect.height + offsetMin.y;
            float yMax = parentRect.yMin + anchorMax.y * parentRect.height + offsetMax.y;

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
