using Game.UI.Controls;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class HudFeedbackCurvesTests
    {
        private const float Stiffness = 600f;
        private const float Damping = 22f;

        private static void Simulate(ref float value, ref float velocity, float target, float seconds, float dt)
        {
            for (float t = 0f; t < seconds; t += dt)
            {
                HudFeedbackCurves.SpringStep(ref value, ref velocity, target, Stiffness, Damping, dt);
            }
        }

        [Test]
        public void Spring_HoldingPress_SettlesAtPressedScale()
        {
            float value = 1f, velocity = 0f;
            Simulate(ref value, ref velocity, 0.9f, 1f, 1f / 60f);
            Assert.AreEqual(0.9f, value, 0.001f);
        }

        [Test]
        public void Spring_Release_OvershootsSlightlyThenSettles()
        {
            float value = 0.9f, velocity = 0f;
            float max = value;
            for (int i = 0; i < 120; i++)
            {
                HudFeedbackCurves.SpringStep(ref value, ref velocity, 1f, Stiffness, Damping, 1f / 60f);
                if (value > max) max = value;
            }
            Assert.Greater(max, 1.0f, "Nút phải nảy lố nhẹ qua 1.");
            Assert.Less(max, 1.06f, "Không được nảy quá đà.");
            Assert.AreEqual(1f, value, 0.001f);
        }

        [Test]
        public void Spring_PopImpulse_RaisesScaleThenReturns()
        {
            float value = 1f, velocity = 6f;
            float max = value;
            for (int i = 0; i < 180; i++)
            {
                HudFeedbackCurves.SpringStep(ref value, ref velocity, 1f, Stiffness, Damping, 1f / 60f);
                if (value > max) max = value;
            }
            Assert.Greater(max, 1.05f);
            Assert.Less(max, 1.3f);
            Assert.AreEqual(1f, value, 0.001f);
        }

        [Test]
        public void Spring_LargeFrameStep_StaysStable()
        {
            float value = 0.9f, velocity = 0f;
            Simulate(ref value, ref velocity, 1f, 2f, 0.05f);
            Assert.IsFalse(float.IsNaN(value) || float.IsInfinity(value));
            Assert.AreEqual(1f, value, 0.001f);
        }

        [Test]
        public void Spring_ZeroOrNegativeDt_ChangesNothing()
        {
            float value = 0.9f, velocity = 1f;
            HudFeedbackCurves.SpringStep(ref value, ref velocity, 1f, Stiffness, Damping, 0f);
            Assert.AreEqual(0.9f, value);
            Assert.AreEqual(1f, velocity);
        }

        [Test]
        public void Breath_StaysBetweenOneAndOnePlusAmount()
        {
            const float amount = 0.04f;
            Assert.AreEqual(1f, HudFeedbackCurves.Breath(0f, 1.4f, amount), 0.0001f);
            for (float t = 0f; t < 5f; t += 0.05f)
            {
                float v = HudFeedbackCurves.Breath(t, 1.4f, amount);
                Assert.GreaterOrEqual(v, 1f - 0.0001f);
                Assert.LessOrEqual(v, 1f + amount + 0.0001f);
            }
        }

        [Test]
        public void Breath_PeaksAtHalfPeriod()
        {
            Assert.AreEqual(1f, HudFeedbackCurves.BreathWave(0.7f, 1.4f), 0.0001f);
        }

        [Test]
        public void Breath_InvalidPeriod_IsFlat()
        {
            Assert.AreEqual(0f, HudFeedbackCurves.BreathWave(3f, 0f));
        }

        [Test]
        public void IsCooling_NeedsEnabledFillAboveEpsilon()
        {
            Assert.IsTrue(HudFeedbackCurves.IsCooling(true, 0.5f));
            Assert.IsFalse(HudFeedbackCurves.IsCooling(false, 0.5f));
            Assert.IsFalse(HudFeedbackCurves.IsCooling(true, 0f));
            Assert.IsFalse(HudFeedbackCurves.IsCooling(true, HudFeedbackCurves.CoolingEpsilon));
        }

        [Test]
        public void ShouldFlash_OnlyWhenCooldownJustEnded()
        {
            Assert.IsTrue(HudFeedbackCurves.ShouldFlash(true, false));
            Assert.IsFalse(HudFeedbackCurves.ShouldFlash(false, false));
            Assert.IsFalse(HudFeedbackCurves.ShouldFlash(true, true));
            Assert.IsFalse(HudFeedbackCurves.ShouldFlash(false, true));
        }

        [Test]
        public void FlashRing_GrowsAndFadesOverTime()
        {
            Assert.Less(HudFeedbackCurves.FlashRingScale(0f), HudFeedbackCurves.FlashRingScale(0.5f));
            Assert.Less(HudFeedbackCurves.FlashRingScale(0.5f), HudFeedbackCurves.FlashRingScale(1f));
            Assert.AreEqual(0.9f, HudFeedbackCurves.FlashRingAlpha(0f, 0.9f), 0.0001f);
            Assert.AreEqual(0f, HudFeedbackCurves.FlashRingAlpha(1f, 0.9f), 0.0001f);
            Assert.Greater(HudFeedbackCurves.FlashRingAlpha(0.2f, 0.9f), HudFeedbackCurves.FlashRingAlpha(0.6f, 0.9f));
        }

        [Test]
        public void FlashRing_ClampsOutOfRangeTime()
        {
            Assert.AreEqual(HudFeedbackCurves.FlashRingScale(1f), HudFeedbackCurves.FlashRingScale(5f), 0.0001f);
            Assert.AreEqual(0f, HudFeedbackCurves.FlashRingAlpha(3f, 0.9f), 0.0001f);
        }
    }
}
