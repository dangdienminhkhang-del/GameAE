using Game.Core.Presentation;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Luật dừng hình (docs/05 Phase 9.5): chỉ đòn người chơi, có trần, có khoảng cách tối thiểu, không dừng khi pause/chết.</summary>
    public class HitStopRulesTests
    {
        private static readonly HitStopSettings Settings = new HitStopSettings(
            normalSeconds: 0.04f, finisherSeconds: 0.06f, bossSeconds: 0.05f,
            maxSeconds: 0.06f, minIntervalSeconds: 0.15f, timeScale: 0.05f);

        private static HitStopContext Ctx(bool playerHit = true, bool paused = false, bool playerDead = false,
            bool killed = false, bool boss = false, double now = 10.0, double last = HitStopRules.NeverStopped)
        {
            return new HitStopContext(playerHit, paused, playerDead, killed, boss, now, last);
        }

        [Test]
        public void NormalPlayerHit_UsesNormalSecondsAndConfiguredScale()
        {
            HitStopDecision d = HitStopRules.Evaluate(Ctx(), Settings);
            Assert.IsTrue(d.Apply);
            Assert.AreEqual(0.04f, d.Seconds, 1e-5f);
            Assert.AreEqual(0.05f, d.TimeScale, 1e-5f);
        }

        [Test]
        public void FinisherAndBoss_AreLongerThanNormal_ButNeverAboveCap()
        {
            HitStopDecision normal = HitStopRules.Evaluate(Ctx(), Settings);
            HitStopDecision finisher = HitStopRules.Evaluate(Ctx(killed: true), Settings);
            HitStopDecision boss = HitStopRules.Evaluate(Ctx(boss: true), Settings);

            Assert.Greater(finisher.Seconds, normal.Seconds);
            Assert.Greater(boss.Seconds, normal.Seconds);
            Assert.LessOrEqual(finisher.Seconds, Settings.MaxSeconds);
            Assert.LessOrEqual(boss.Seconds, Settings.MaxSeconds);
        }

        [Test]
        public void ConfiguredValueAboveCap_IsClampedToCap()
        {
            var greedy = new HitStopSettings(0.5f, 0.9f, 0.7f, 0.06f, 0.15f, 0.05f);
            Assert.AreEqual(0.06f, HitStopRules.Evaluate(Ctx(), greedy).Seconds, 1e-5f);
            Assert.AreEqual(0.06f, HitStopRules.Evaluate(Ctx(killed: true), greedy).Seconds, 1e-5f);
            Assert.AreEqual(0.06f, HitStopRules.Evaluate(Ctx(boss: true), greedy).Seconds, 1e-5f);
        }

        [Test]
        public void KilledBoss_UsesFinisherNotBossSeconds()
        {
            HitStopDecision d = HitStopRules.Evaluate(Ctx(killed: true, boss: true), Settings);
            Assert.AreEqual(Settings.FinisherSeconds, d.Seconds, 1e-5f);
        }

        [Test]
        public void HitNotFromPlayer_NeverStops()
        {
            Assert.IsFalse(HitStopRules.Evaluate(Ctx(playerHit: false), Settings).Apply);
            Assert.IsFalse(HitStopRules.Evaluate(Ctx(playerHit: false, killed: true), Settings).Apply);
        }

        [Test]
        public void WhilePaused_NeverStops()
        {
            Assert.IsFalse(HitStopRules.Evaluate(Ctx(paused: true), Settings).Apply);
        }

        [Test]
        public void WhenPlayerIsDead_NeverStops()
        {
            Assert.IsFalse(HitStopRules.Evaluate(Ctx(playerDead: true), Settings).Apply);
        }

        [Test]
        public void SecondHitWithinMinInterval_IsIgnored_ButAllowedAfterIt()
        {
            Assert.IsFalse(HitStopRules.Evaluate(Ctx(now: 10.10, last: 10.0), Settings).Apply, "0.10s < 0.15s phải bị chặn");
            Assert.IsTrue(HitStopRules.Evaluate(Ctx(now: 10.20, last: 10.0), Settings).Apply, "0.20s > 0.15s phải cho phép");
        }

        [Test]
        public void FirstEverHit_IsAllowed()
        {
            Assert.IsTrue(HitStopRules.Evaluate(Ctx(now: 0.0, last: HitStopRules.NeverStopped), Settings).Apply);
        }

        [Test]
        public void ZeroConfiguredSeconds_DoesNothing()
        {
            var off = new HitStopSettings(0f, 0f, 0f, 0.06f, 0.15f, 0.05f);
            Assert.IsFalse(HitStopRules.Evaluate(Ctx(), off).Apply);
        }
    }
}
