using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Game.UI.Controls;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Chống tái phát hai lỗi điều khiển cảm ứng:
    ///  1. Chạm hay bấm chuột ở BẤT CỨ ĐÂU đều đánh thường (action Attack gắn Mouse/Touchscreen).
    ///  2. Joystick nhảy vào giữa màn hình sau khi nhả tay (vị trí "nhà" lưu theo localPosition lúc Canvas chưa co giãn).
    /// </summary>
    public class TouchControlsRegressionTests
    {
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        private readonly List<Object> _temp = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _temp) if (o != null) Object.DestroyImmediate(o);
            _temp.Clear();
        }

        [Test]
        public void AttackAction_IsNotBoundToMouseOrTouchscreen_SoTouchingAnywhereDoesNotAttack()
        {
            string json = File.ReadAllText(InputActionsPath);
            var paths = new List<string>();
            foreach (Match m in Regex.Matches(json, "\"path\":\\s*\"([^\"]+)\"[^}]*?\"action\":\\s*\"Attack\""))
                paths.Add(m.Groups[1].Value);

            Assert.IsNotEmpty(paths, "Không tìm thấy binding nào của action Attack (đổi định dạng file?).");
            foreach (string path in paths)
            {
                StringAssert.DoesNotStartWith("<Mouse>", path, "Attack không được gắn chuột: bấm đâu cũng đánh.");
                StringAssert.DoesNotStartWith("<Touchscreen>", path, "Attack không được gắn chạm màn hình: chạm đâu cũng đánh. Chỉ nút Đánh trên màn hình mới đánh.");
                StringAssert.DoesNotStartWith("<Pointer>", path, "Attack không được gắn con trỏ.");
            }
        }

        [Test]
        public void Joystick_ReturnsToItsAnchoredHome_EvenWhenTheCanvasChangesSizeAfterAwake()
        {
            // Mô phỏng: lúc Awake vùng cha còn là kích thước pixel thô (820x400), sau đó CanvasScaler co giãn thành 2214x1080.
            var parent = NewRect("Parent", null);
            parent.sizeDelta = new Vector2(820f, 400f);

            var panel = NewRect("JoystickPanel", parent);
            panel.anchorMin = panel.anchorMax = Vector2.zero;
            panel.sizeDelta = new Vector2(200f, 200f);
            panel.anchoredPosition = new Vector2(150f, 150f);

            var joystick = panel.gameObject.AddComponent<OnScreenJoystick>();
            Invoke(joystick, "Awake");

            parent.sizeDelta = new Vector2(2214f, 1080f); // Canvas co giãn sau Awake

            panel.anchoredPosition = new Vector2(846f, 490f); // bị kéo đi (Floating) hoặc lệch
            Invoke(joystick, "ResetBackgroundToHome");

            Assert.AreEqual(150f, panel.anchoredPosition.x, 0.01f, "Joystick phải về đúng chỗ neo (góc dưới trái), không nhảy vào giữa.");
            Assert.AreEqual(150f, panel.anchoredPosition.y, 0.01f);
        }

        private RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            _temp.Add(go);
            if (parent != null) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Invoke(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(m, "Không tìm thấy hàm " + method);
            m.Invoke(target, null);
        }
    }
}
