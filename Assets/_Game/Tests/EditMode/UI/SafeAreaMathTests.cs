using Game.UI.Common;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.UI
{
    [TestFixture]
    public class SafeAreaMathTests
    {
        private const float Epsilon = 0.0001f;

        [Test]
        public void NoNotch_ReturnsFullAnchors()
        {
            Vector2 screenSize = new Vector2(1920f, 1080f);
            Rect safeArea = new Rect(0f, 0f, 1920f, 1080f);

            SafeAreaMath.CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

            Assert.AreEqual(0f, anchorMin.x, Epsilon);
            Assert.AreEqual(0f, anchorMin.y, Epsilon);
            Assert.AreEqual(1f, anchorMax.x, Epsilon);
            Assert.AreEqual(1f, anchorMax.y, Epsilon);
        }

        [Test]
        public void SingleSideNotch_Left_ReturnsCorrectAnchors()
        {
            Vector2 screenSize = new Vector2(2400f, 1080f);
            // Left notch 130px
            Rect safeArea = new Rect(130f, 0f, 2270f, 1080f);

            SafeAreaMath.CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

            Assert.AreEqual(130f / 2400f, anchorMin.x, Epsilon);
            Assert.AreEqual(0f, anchorMin.y, Epsilon);
            Assert.AreEqual(1f, anchorMax.x, Epsilon);
            Assert.AreEqual(1f, anchorMax.y, Epsilon);
        }

        [Test]
        public void TwoSideNotch_LeftAndRight_ReturnsCorrectAnchors()
        {
            Vector2 screenSize = new Vector2(2400f, 1080f);
            // Left notch 130px, right notch 130px
            Rect safeArea = new Rect(130f, 0f, 2140f, 1080f);

            SafeAreaMath.CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

            Assert.AreEqual(130f / 2400f, anchorMin.x, Epsilon);
            Assert.AreEqual(0f, anchorMin.y, Epsilon);
            Assert.AreEqual((2400f - 130f) / 2400f, anchorMax.x, Epsilon);
            Assert.AreEqual(1f, anchorMax.y, Epsilon);
        }

        [Test]
        public void NotchAndBottomBar_ReturnsCorrectAnchors()
        {
            Vector2 screenSize = new Vector2(2400f, 1080f);
            // Left notch 130px, bottom bar 60px
            Rect safeArea = new Rect(130f, 60f, 2270f, 1020f);

            SafeAreaMath.CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

            Assert.AreEqual(130f / 2400f, anchorMin.x, Epsilon);
            Assert.AreEqual(60f / 1080f, anchorMin.y, Epsilon);
            Assert.AreEqual(1f, anchorMax.x, Epsilon);
            Assert.AreEqual(1f, anchorMax.y, Epsilon);
        }

        [Test]
        public void SafeAreaLargerThanScreen_FallsBackToFullScreen()
        {
            Vector2 screenSize = new Vector2(1920f, 1080f);
            // safeArea larger than screen
            Rect safeArea = new Rect(-50f, -20f, 2000f, 1200f);

            SafeAreaMath.CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

            Assert.AreEqual(Vector2.zero, anchorMin);
            Assert.AreEqual(Vector2.one, anchorMax);
        }

        [Test]
        public void NaNSafeArea_FallsBackToFullScreen()
        {
            Vector2 screenSize = new Vector2(1920f, 1080f);
            Rect safeArea = new Rect(float.NaN, 0f, 1920f, 1080f);

            SafeAreaMath.CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

            Assert.AreEqual(Vector2.zero, anchorMin);
            Assert.AreEqual(Vector2.one, anchorMax);
        }

        [Test]
        public void InfinitySafeArea_FallsBackToFullScreen()
        {
            Vector2 screenSize = new Vector2(1920f, 1080f);
            Rect safeArea = new Rect(0f, 0f, float.PositiveInfinity, 1080f);

            SafeAreaMath.CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

            Assert.AreEqual(Vector2.zero, anchorMin);
            Assert.AreEqual(Vector2.one, anchorMax);
        }

        [Test]
        public void ZeroOrNegativeScreenSize_FallsBackToFullScreen()
        {
            Vector2 screenSize = Vector2.zero;
            Rect safeArea = new Rect(0f, 0f, 100f, 100f);

            SafeAreaMath.CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

            Assert.AreEqual(Vector2.zero, anchorMin);
            Assert.AreEqual(Vector2.one, anchorMax);
        }

        [Test]
        public void ZeroOrNegativeSafeArea_FallsBackToFullScreen()
        {
            Vector2 screenSize = new Vector2(1920f, 1080f);
            Rect safeArea = new Rect(100f, 100f, 0f, 0f);

            SafeAreaMath.CalculateNormalizedAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

            Assert.AreEqual(Vector2.zero, anchorMin);
            Assert.AreEqual(Vector2.one, anchorMax);
        }

        [Test]
        public void CalculateCanvasSize_MatchHeight_HeightIsAlways1080()
        {
            Vector2 refRes = new Vector2(1920f, 1080f);

            // 16:9
            Vector2 c1 = SafeAreaMath.CalculateCanvasSize(new Vector2(1920f, 1080f), refRes, 1f);
            Assert.AreEqual(1080f, c1.y, Epsilon);
            Assert.AreEqual(1920f, c1.x, Epsilon);

            // 20:9 (2400x1080)
            Vector2 c2 = SafeAreaMath.CalculateCanvasSize(new Vector2(2400f, 1080f), refRes, 1f);
            Assert.AreEqual(1080f, c2.y, Epsilon);
            Assert.AreEqual(2400f, c2.x, Epsilon);

            // 4:3 (2048x1536 -> ratio 4/3 -> canvas width 1440, height 1080)
            Vector2 c3 = SafeAreaMath.CalculateCanvasSize(new Vector2(2048f, 1536f), refRes, 1f);
            Assert.AreEqual(1080f, c3.y, Epsilon);
            Assert.AreEqual(1440f, c3.x, Epsilon);
        }
    }
}
