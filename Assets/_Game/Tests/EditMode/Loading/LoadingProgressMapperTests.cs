using Game.UI.Loading;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class LoadingProgressMapperTests
    {
        [Test]
        public void RawProgressZeroPointNine_MapsToOne_AfterMinShowTime()
        {
            var m = new LoadingProgressMapper(0.4f);
            Assert.AreEqual(1f, m.Map(0.9f, 1f), 1e-5f);
            Assert.IsTrue(m.IsComplete(0.9f, 1f));
        }

        [Test]
        public void MinShowTime_HoldsBarBackSoOverlayDoesNotFlash()
        {
            var m = new LoadingProgressMapper(0.4f);
            float early = m.Map(0.9f, 0.1f);
            Assert.Less(early, 1f);
            Assert.IsFalse(m.IsComplete(0.9f, 0.1f));
            Assert.AreEqual(1f, m.Map(0.9f, 0.4f), 1e-5f);
            Assert.IsTrue(m.IsComplete(0.9f, 0.4f));
        }

        [Test]
        public void Map_IsMonotonic_EvenIfRawProgressDrops()
        {
            var m = new LoadingProgressMapper(0f);
            float a = m.Map(0.5f, 0.2f);
            float b = m.Map(0.3f, 0.3f);
            float c = m.Map(0.7f, 0.4f);
            Assert.GreaterOrEqual(b, a);
            Assert.GreaterOrEqual(c, b);
        }

        [Test]
        public void NotComplete_WhileRawBelowThreshold()
        {
            var m = new LoadingProgressMapper(0.4f);
            Assert.IsFalse(m.IsComplete(0.5f, 5f));
            Assert.IsFalse(m.IsComplete(float.NaN, 5f));
        }

        [Test]
        public void Output_IsAlwaysWithinZeroToOne()
        {
            var m = new LoadingProgressMapper(0.4f);
            foreach (float raw in new[] { -1f, 0f, 0.45f, 0.9f, 1f, 5f, float.NaN })
            {
                float v = m.Map(raw, 10f);
                Assert.That(v, Is.InRange(0f, 1f), raw.ToString());
            }
        }

        [Test]
        public void ZeroMinShow_DoesNotDelay()
        {
            var m = new LoadingProgressMapper(0f);
            Assert.AreEqual(1f, m.Map(0.9f, 0f), 1e-5f);
            Assert.IsTrue(m.IsComplete(0.9f, 0f));
        }

        [Test]
        public void Reset_StartsOver()
        {
            var m = new LoadingProgressMapper(0f);
            m.Map(0.9f, 1f);
            m.Reset();
            Assert.AreEqual(0f, m.Map(0f, 0f));
        }
    }
}
