using Game.PvE.Zones;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Zones
{
    /// <summary>Phase 9.7a — toán kẹp camera vào bounds vùng (thuần logic).</summary>
    public class CameraBoundsMathTests
    {
        [Test]
        public void ClampAxis_InsideRange_Unchanged()
        {
            Assert.AreEqual(5f, CameraBoundsMath.ClampAxis(5f, 0f, 20f, 3f), 0.0001f);
        }

        [Test]
        public void ClampAxis_BelowRange_ClampsToMinPlusHalf()
        {
            Assert.AreEqual(3f, CameraBoundsMath.ClampAxis(-10f, 0f, 20f, 3f), 0.0001f);
        }

        [Test]
        public void ClampAxis_AboveRange_ClampsToMaxMinusHalf()
        {
            Assert.AreEqual(17f, CameraBoundsMath.ClampAxis(99f, 0f, 20f, 3f), 0.0001f);
        }

        [Test]
        public void ClampAxis_ZoneNarrowerThanView_CentersThatAxis()
        {
            // Vùng rộng 4, khung nhìn rộng 10 (half = 5): min + half > max - half.
            float result = CameraBoundsMath.ClampAxis(100f, 0f, 4f, 5f);
            Assert.AreEqual(2f, result, 0.0001f);

            Assert.AreEqual(2f, CameraBoundsMath.ClampAxis(-100f, 0f, 4f, 5f), 0.0001f,
                "Vùng hẹp: dù camera ở đâu cũng căn giữa, không nhảy lung tung.");
        }

        [Test]
        public void ClampAxis_ZoneExactlyViewSize_PinsToCenter()
        {
            Assert.AreEqual(5f, CameraBoundsMath.ClampAxis(0f, 0f, 10f, 5f), 0.0001f);
        }

        [Test]
        public void ClampAxis_BadInputs_NeverReturnNaN()
        {
            Assert.AreEqual(0f, CameraBoundsMath.ClampAxis(float.NaN, 0f, 10f, 2f));
            Assert.AreEqual(0f, CameraBoundsMath.ClampAxis(float.PositiveInfinity, 0f, 10f, 2f));
            Assert.AreEqual(4f, CameraBoundsMath.ClampAxis(4f, float.NaN, 10f, 2f), "Bounds hỏng: giữ nguyên vị trí.");
            Assert.AreEqual(4f, CameraBoundsMath.ClampAxis(4f, 0f, float.PositiveInfinity, 2f));
            Assert.AreEqual(4f, CameraBoundsMath.ClampAxis(4f, 0f, 10f, float.NaN));
        }

        [Test]
        public void Clamp_AxesAreIndependent()
        {
            // X nằm trong vùng, Y vùng hẹp nên căn giữa.
            Vector2 result = CameraBoundsMath.Clamp(new Vector2(10f, 50f), new Vector2(0f, 0f), new Vector2(30f, 4f), 5f, 5f);

            Assert.AreEqual(10f, result.x, 0.0001f);
            Assert.AreEqual(2f, result.y, 0.0001f);
        }

        [Test]
        public void ShouldSnap_NoPreviousBounds_IsTrue()
        {
            Assert.IsTrue(CameraBoundsMath.ShouldSnap(false, default, default, Vector2.zero, Vector2.one));
        }

        [Test]
        public void ShouldSnap_OverlappingBounds_IsFalse()
        {
            Assert.IsFalse(CameraBoundsMath.ShouldSnap(true,
                new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(8f, 0f), new Vector2(20f, 10f)));
        }

        [Test]
        public void ShouldSnap_TouchingBounds_IsFalse()
        {
            Assert.IsFalse(CameraBoundsMath.ShouldSnap(true,
                new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(10f, 0f), new Vector2(20f, 10f)));
        }

        [Test]
        public void ShouldSnap_DisjointBounds_IsTrue()
        {
            Assert.IsTrue(CameraBoundsMath.ShouldSnap(true,
                new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(100f, 100f), new Vector2(120f, 120f)));
        }

        [Test]
        public void Lerp_EndsAndMiddle()
        {
            CameraBoundsMath.Lerp(Vector2.zero, new Vector2(10f, 10f), new Vector2(20f, 20f), new Vector2(40f, 40f), 0f,
                out Vector2 min0, out Vector2 max0);
            Assert.AreEqual(Vector2.zero, min0);
            Assert.AreEqual(new Vector2(10f, 10f), max0);

            CameraBoundsMath.Lerp(Vector2.zero, new Vector2(10f, 10f), new Vector2(20f, 20f), new Vector2(40f, 40f), 1f,
                out Vector2 min1, out Vector2 max1);
            Assert.AreEqual(new Vector2(20f, 20f), min1);
            Assert.AreEqual(new Vector2(40f, 40f), max1);

            CameraBoundsMath.Lerp(Vector2.zero, new Vector2(10f, 10f), new Vector2(20f, 20f), new Vector2(40f, 40f), 0.5f,
                out Vector2 minH, out Vector2 maxH);
            Assert.AreEqual(new Vector2(10f, 10f), minH);
            Assert.AreEqual(new Vector2(25f, 25f), maxH);
        }

        [Test]
        public void Lerp_ClampsTOutsideZeroOne()
        {
            CameraBoundsMath.Lerp(Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(11f, 11f), 7f,
                out Vector2 min, out _);
            Assert.AreEqual(new Vector2(10f, 10f), min);
        }

        [Test]
        public void SmoothStep_EndsMiddleAndClamp()
        {
            Assert.AreEqual(0f, CameraBoundsMath.SmoothStep(-1f), 0.0001f);
            Assert.AreEqual(0f, CameraBoundsMath.SmoothStep(0f), 0.0001f);
            Assert.AreEqual(0.5f, CameraBoundsMath.SmoothStep(0.5f), 0.0001f);
            Assert.AreEqual(1f, CameraBoundsMath.SmoothStep(1f), 0.0001f);
            Assert.AreEqual(1f, CameraBoundsMath.SmoothStep(3f), 0.0001f);
        }
    }
}
