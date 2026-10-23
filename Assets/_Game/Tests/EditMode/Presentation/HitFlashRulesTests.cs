using Game.PvE.Presentation;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Luật alpha của lớp phủ nháy "HitFlash" (ADR-029): 0.85 → 0 trong 0.08 s, tối đa 0.5 khi đang báo đòn nguy hiểm.</summary>
    public class HitFlashRulesTests
    {
        private const float Start = 0.85f;
        private const float Duration = 0.08f;
        private const float DangerCap = 0.5f;

        // ── Đường giảm bình thường ───────────────────────────────────────────

        [Test]
        public void Alpha_StartsAtStartAlpha_AndEndsAtZero()
        {
            Assert.AreEqual(Start, HitFlashRules.Alpha(0f, Duration, Start, false, DangerCap), 1e-6f);
            Assert.AreEqual(0f, HitFlashRules.Alpha(Duration, Duration, Start, false, DangerCap), 1e-6f);
        }

        [Test]
        public void Alpha_IsLinear_HalfwayIsHalf()
        {
            Assert.AreEqual(Start * 0.5f, HitFlashRules.Alpha(Duration * 0.5f, Duration, Start, false, DangerCap), 1e-6f);
            Assert.AreEqual(Start * 0.25f, HitFlashRules.Alpha(Duration * 0.75f, Duration, Start, false, DangerCap), 1e-6f);
        }

        [Test]
        public void Alpha_StaysZero_AfterDuration()
        {
            Assert.AreEqual(0f, HitFlashRules.Alpha(Duration * 2f, Duration, Start, false, DangerCap), 1e-6f);
            Assert.AreEqual(0f, HitFlashRules.Alpha(1000f, Duration, Start, false, DangerCap), 1e-6f);
        }

        [Test]
        public void Alpha_NeverIncreases_AsTimePasses()
        {
            float previous = float.MaxValue;
            for (int i = 0; i <= 100; i++)
            {
                float alpha = HitFlashRules.Alpha(Duration * i / 80f, Duration, Start, false, DangerCap);
                Assert.LessOrEqual(alpha, previous + 1e-6f, "bước " + i);
                previous = alpha;
            }
        }

        [Test]
        public void NormalHit_IgnoresDangerCap()
        {
            // Không báo đòn: lớp phủ đậm hơn giới hạn báo đòn (nháy trắng rõ).
            Assert.Greater(HitFlashRules.Alpha(0f, Duration, Start, false, DangerCap), DangerCap);
        }

        // ── Giới hạn khi đang báo đòn nguy hiểm ───────────────────────────────

        [Test]
        public void UnderDanger_PeakIsCappedAtMaxAlpha()
        {
            Assert.AreEqual(DangerCap, HitFlashRules.Alpha(0f, Duration, Start, true, DangerCap), 1e-6f);
            Assert.AreEqual(DangerCap, HitFlashRules.PeakAlpha(Start, true, DangerCap), 1e-6f);
        }

        [Test]
        public void UnderDanger_NeverExceedsCap_AtAnyTime()
        {
            for (int i = -5; i <= 120; i++)
            {
                float alpha = HitFlashRules.Alpha(Duration * i / 100f, Duration, Start, true, DangerCap);
                Assert.LessOrEqual(alpha, DangerCap + 1e-6f, "bước " + i);
                Assert.GreaterOrEqual(alpha, 0f);
            }
        }

        [Test]
        public void UnderDanger_StillDecaysToZero()
        {
            Assert.AreEqual(DangerCap * 0.5f, HitFlashRules.Alpha(Duration * 0.5f, Duration, Start, true, DangerCap), 1e-6f);
            Assert.AreEqual(0f, HitFlashRules.Alpha(Duration, Duration, Start, true, DangerCap), 1e-6f);
        }

        [Test]
        public void UnderDanger_WhenStartIsBelowCap_StartIsKept()
        {
            // Giới hạn chỉ làm nhạt đi, không bao giờ làm đậm lên.
            Assert.AreEqual(0.3f, HitFlashRules.Alpha(0f, Duration, 0.3f, true, DangerCap), 1e-6f);
        }

        [Test]
        public void UnderDanger_IsNeverDarkerThanNormal()
        {
            for (int i = 0; i <= 20; i++)
            {
                float t = Duration * i / 20f;
                Assert.LessOrEqual(
                    HitFlashRules.Alpha(t, Duration, Start, true, DangerCap),
                    HitFlashRules.Alpha(t, Duration, Start, false, DangerCap) + 1e-6f);
            }
        }

        // ── Không âm, không NaN ───────────────────────────────────────────────

        [Test]
        public void NegativeElapsed_ClampsToPeak_NotAboveIt()
        {
            Assert.AreEqual(Start, HitFlashRules.Alpha(-5f, Duration, Start, false, DangerCap), 1e-6f);
            Assert.AreEqual(DangerCap, HitFlashRules.Alpha(-5f, Duration, Start, true, DangerCap), 1e-6f);
        }

        [Test]
        public void BadDuration_GivesZero()
        {
            Assert.AreEqual(0f, HitFlashRules.Alpha(0f, 0f, Start, false, DangerCap));
            Assert.AreEqual(0f, HitFlashRules.Alpha(0f, -1f, Start, false, DangerCap));
            Assert.AreEqual(0f, HitFlashRules.Alpha(0f, float.NaN, Start, false, DangerCap));
            Assert.AreEqual(0f, HitFlashRules.Alpha(0f, float.PositiveInfinity, Start, false, DangerCap));
        }

        [Test]
        public void BadStartAlpha_IsSanitized()
        {
            Assert.AreEqual(1f, HitFlashRules.Alpha(0f, Duration, 5f, false, DangerCap), 1e-6f);
            Assert.AreEqual(0f, HitFlashRules.Alpha(0f, Duration, -3f, false, DangerCap), 1e-6f);
            Assert.AreEqual(0f, HitFlashRules.Alpha(0f, Duration, float.NaN, false, DangerCap), 1e-6f);
        }

        [Test]
        public void BadDangerCap_IsSanitized_AndOnlyMattersUnderDanger()
        {
            Assert.AreEqual(0f, HitFlashRules.Alpha(0f, Duration, Start, true, float.NaN), 1e-6f);
            Assert.AreEqual(0f, HitFlashRules.Alpha(0f, Duration, Start, true, -1f), 1e-6f);
            Assert.AreEqual(Start, HitFlashRules.Alpha(0f, Duration, Start, true, 7f), 1e-6f);
            Assert.AreEqual(Start, HitFlashRules.Alpha(0f, Duration, Start, false, float.NaN), 1e-6f);
        }

        [Test]
        public void Alpha_NeverNegativeNeverNaN_ForHostileInputs()
        {
            float[] values =
            {
                0f, 1f, -1f, 0.08f, 0.85f, 1e-9f, 1e9f,
                float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MaxValue, float.MinValue
            };

            foreach (float elapsed in values)
            foreach (float duration in values)
            foreach (float start in values)
            foreach (float cap in values)
            foreach (bool danger in new[] { false, true })
            {
                float alpha = HitFlashRules.Alpha(elapsed, duration, start, danger, cap);
                Assert.IsFalse(float.IsNaN(alpha), $"NaN: e={elapsed} d={duration} s={start} c={cap} danger={danger}");
                Assert.GreaterOrEqual(alpha, 0f, $"âm: e={elapsed} d={duration} s={start} c={cap} danger={danger}");
                Assert.LessOrEqual(alpha, 1f, $"> 1: e={elapsed} d={duration} s={start} c={cap} danger={danger}");
            }
        }

        // ── IsActive / IsDanger ───────────────────────────────────────────────

        [Test]
        public void IsActive_TrueOnlyWhileInsideDuration()
        {
            Assert.IsTrue(HitFlashRules.IsActive(0f, Duration));
            Assert.IsTrue(HitFlashRules.IsActive(Duration * 0.99f, Duration));
            Assert.IsFalse(HitFlashRules.IsActive(Duration, Duration));
            Assert.IsFalse(HitFlashRules.IsActive(Duration * 2f, Duration));
        }

        [Test]
        public void IsActive_FalseForBadInput()
        {
            Assert.IsFalse(HitFlashRules.IsActive(0f, 0f));
            Assert.IsFalse(HitFlashRules.IsActive(0f, -1f));
            Assert.IsFalse(HitFlashRules.IsActive(0f, float.NaN));
            Assert.IsFalse(HitFlashRules.IsActive(float.NaN, Duration));
            Assert.IsFalse(HitFlashRules.IsActive(0f, float.PositiveInfinity));
        }

        [Test]
        public void IsDanger_OnlyForDangerTelegraph()
        {
            Assert.IsTrue(HitFlashRules.IsDanger(TintKind.DangerTelegraph));
            Assert.IsFalse(HitFlashRules.IsDanger(TintKind.Base));
            Assert.IsFalse(HitFlashRules.IsDanger(TintKind.Window));
            Assert.IsFalse(HitFlashRules.IsDanger(TintKind.Enrage));
        }
    }
}
