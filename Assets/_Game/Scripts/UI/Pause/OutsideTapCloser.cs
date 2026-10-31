using System.Collections.Generic;
using Game.UI.Map;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI.Pause
{
    /// <summary>
    /// Panel cho biết object gốc của nó để <see cref="OutsideTapCloser"/> xác định vùng cửa sổ.
    /// Gốc có thể là chính khung cửa sổ, hoặc lớp nền mờ phủ cả màn hình (khi đó cửa sổ thật là các object con).
    /// </summary>
    public interface IPanelRoot
    {
        GameObject PanelRoot { get; }
    }

    /// <summary>Luật thuần của "bấm ra ngoài để tắt" (có test, không cần Unity).</summary>
    public static class OutsideTapRules
    {
        /// <summary>
        /// Chỉ đóng khi: panel đã mở ổn định từ khung hình trước (tránh đóng ngay lúc vừa mở), có một lần bấm xuống,
        /// điểm bấm nằm ngoài cửa sổ, và không rơi vào nút/điều khiển khác (nút đó tự xử lý — ví dụ nút bật/tắt chính panel này,
        /// hay nút X nhô ra khỏi viền cửa sổ).
        /// </summary>
        public static bool ShouldClose(bool stableTarget, bool pressedThisFrame, bool insideWindow, bool overInteractive)
        {
            return stableTarget && pressedThisFrame && !insideWindow && !overInteractive;
        }

        /// <summary>Gốc panel phủ gần hết màn hình (lớp nền mờ) → vùng cửa sổ thật là các object con của nó.</summary>
        public static bool IsBackdrop(float rootWidth, float rootHeight, float screenWidth, float screenHeight)
        {
            return rootWidth >= screenWidth * 0.9f && rootHeight >= screenHeight * 0.9f;
        }
    }

    /// <summary>
    /// Bấm vào vùng trống bên ngoài panel đang mở trên cùng thì đóng panel đó (giống nút Back/nút X).
    /// Dùng lại <see cref="BackKeyRouter"/>: mọi panel đã đăng ký IBackHandler + IPanelRoot đều được hỗ trợ.
    /// Khi không còn panel nào mở: menu Pause, rồi minimap của bản đồ (không nằm trong router) được xử lý riêng.
    /// Tự khởi tạo một lần khi vào game (DontDestroyOnLoad), không cần đặt vào scene.
    /// </summary>
    public sealed class OutsideTapCloser : MonoBehaviour
    {
        private static OutsideTapCloser _instance;
        private static readonly Vector3[] Corners = new Vector3[4];

        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private object _prevTarget;
        private PauseController _pause;
        private float _nextPauseSearch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("OutsideTapCloser");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<OutsideTapCloser>();
        }

        private void Update()
        {
            ResolveTarget(out object key, out GameObject root, out System.Action close, out bool strict);

            // Chỉ phản ứng nếu cùng một mục tiêu đã mở từ khung hình trước.
            bool stable = key != null && ReferenceEquals(key, _prevTarget);
            _prevTarget = key;
            if (!stable || root == null || close == null) return;

            if (!TryGetPress(out Vector2 pos)) return;

            bool inside = IsInsideWindow(root, pos);
            bool blocked = !inside && (strict ? IsOverAnyUi(pos) : IsOverInteractive(pos));
            if (OutsideTapRules.ShouldClose(stable, true, inside, blocked))
            {
                close();
            }
        }

        /// <param name="strict">
        /// true = chỉ tính là "ra ngoài" khi dưới ngón tay hoàn toàn không có UI nào (dùng cho minimap:
        /// chạm joystick/nút kỹ năng để chơi không được làm minimap biến mất).
        /// false = chỉ cần không phải nút/điều khiển tương tác (dùng cho panel).
        /// </param>
        private void ResolveTarget(out object key, out GameObject root, out System.Action close, out bool strict)
        {
            key = null;
            root = null;
            close = null;
            strict = false;

            IBackHandler top = BackKeyRouterHost.Router.TopOpen();
            if (top != null)
            {
                if (top is IPanelRoot provider && provider.PanelRoot != null && provider.PanelRoot.activeInHierarchy)
                {
                    key = top;
                    root = provider.PanelRoot;
                    close = top.CloseFromBack;
                }
                return;
            }

            // Không còn panel nào trong router → xét menu Pause.
            if (_pause == null && UnityEngine.Time.unscaledTime >= _nextPauseSearch)
            {
                _pause = FindFirstObjectByType<PauseController>();
                _nextPauseSearch = UnityEngine.Time.unscaledTime + 1f;
            }

            if (_pause != null && _pause.IsPaused && _pause.PausePanelRoot != null && _pause.PausePanelRoot.activeInHierarchy)
            {
                key = _pause;
                root = _pause.PausePanelRoot;
                close = _pause.ClosePause;
                return;
            }

            // Cuối cùng: minimap của bản đồ (bản đồ toàn màn hình đã tự thu khi chạm vùng trống).
            MapHudController map = MapHudController.Active;
            if (map != null && map.CollapseOnOutsideTap)
            {
                GameObject mini = map.MinimapRoot;
                if (mini != null && mini.activeInHierarchy)
                {
                    key = map;
                    root = mini;
                    close = map.CollapseFromOutsideTap;
                    strict = true;
                }
            }
        }

        private static bool TryGetPress(out Vector2 position)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touches = touchscreen.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].press.wasPressedThisFrame)
                    {
                        position = touches[i].position.ReadValue();
                        return true;
                    }
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                position = mouse.position.ReadValue();
                return true;
            }

            position = default;
            return false;
        }

        private static bool IsInsideWindow(GameObject root, Vector2 position)
        {
            var rt = root.transform as RectTransform;
            if (rt == null) return false;

            Canvas canvas = rt.GetComponentInParent<Canvas>();
            Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
            Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;

            // Gốc là lớp nền mờ phủ cả màn hình → chỉ các object con mới là cửa sổ thật.
            // Ngược lại gốc chính là cửa sổ; vẫn tính thêm các object con vì có panel có khung con lớn hơn gốc
            // (ví dụ Cài đặt) hoặc nút nhô ra ngoài viền.
            if (!IsBackdrop(rt, cam) && RectTransformUtility.RectangleContainsScreenPoint(rt, position, cam))
            {
                return true;
            }

            for (int i = 0; i < rt.childCount; i++)
            {
                var child = rt.GetChild(i) as RectTransform;
                if (child != null && child.gameObject.activeInHierarchy
                    && RectTransformUtility.RectangleContainsScreenPoint(child, position, cam))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBackdrop(RectTransform rt, Camera cam)
        {
            rt.GetWorldCorners(Corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, Corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, Corners[2]);
            return OutsideTapRules.IsBackdrop(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Screen.width, Screen.height);
        }

        private bool RaycastUi(Vector2 position)
        {
            EventSystem es = EventSystem.current;
            _hits.Clear();
            if (es == null) return false;

            var data = new PointerEventData(es) { position = position };
            es.RaycastAll(data, _hits);
            return _hits.Count > 0;
        }

        private bool IsOverInteractive(Vector2 position)
        {
            if (!RaycastUi(position)) return false;

            // Chỉ xét phần tử trên cùng: lớp nền mờ chặn các nút nằm dưới nó, nên bấm lên nền mờ vẫn tính là "ra ngoài".
            Selectable selectable = _hits[0].gameObject.GetComponentInParent<Selectable>();
            return selectable != null && selectable.IsInteractable();
        }

        private bool IsOverAnyUi(Vector2 position)
        {
            return RaycastUi(position);
        }
    }
}
