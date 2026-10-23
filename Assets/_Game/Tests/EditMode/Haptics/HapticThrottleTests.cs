using Game.Services.Haptics;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class HapticThrottleTests
    {
        [Test]
        public void FirstPulse_IsAllowed()
        {
            Assert.IsTrue(new HapticThrottle().TryAcquire(0f));
        }

        [Test]
        public void PulsesCloserThanMinInterval_AreRejected()
        {
            var t = new HapticThrottle(0.08f);
            Assert.IsTrue(t.TryAcquire(10f));
            Assert.IsFalse(t.TryAcquire(10.05f));
            Assert.IsTrue(t.TryAcquire(10.09f));
        }

        [Test]
        public void RejectedPulse_DoesNotExtendTheWindow()
        {
            var t = new HapticThrottle(0.08f);
            Assert.IsTrue(t.TryAcquire(1f));
            Assert.IsFalse(t.TryAcquire(1.07f));
            Assert.IsTrue(t.TryAcquire(1.081f), "tính từ xung ĐÃ RUNG gần nhất, không phải từ lần bị từ chối");
        }

        [Test]
        public void DefaultInterval_Is80Ms()
        {
            Assert.AreEqual(0.08f, HapticThrottle.DefaultMinIntervalSeconds, 1e-6f);
        }

        [Test]
        public void Reset_AllowsImmediately()
        {
            var t = new HapticThrottle();
            t.TryAcquire(5f);
            t.Reset();
            Assert.IsTrue(t.TryAcquire(5f));
        }
    }

    public class LowHpWarningTests
    {
        [Test]
        public void FiresOnceWhenDroppingBelowThreshold()
        {
            var w = new LowHpWarning(0.30f, 0.35f);
            Assert.IsFalse(w.ShouldWarn(80, 100));
            Assert.IsFalse(w.ShouldWarn(31, 100));
            Assert.IsTrue(w.ShouldWarn(29, 100));
            Assert.IsFalse(w.ShouldWarn(20, 100), "đã báo, chưa nạp lại");
            Assert.IsFalse(w.ShouldWarn(10, 100));
        }

        [Test]
        public void Hysteresis_RearmsOnlyAboveRearmRatio()
        {
            var w = new LowHpWarning(0.30f, 0.35f);
            Assert.IsTrue(w.ShouldWarn(25, 100));
            Assert.IsFalse(w.ShouldWarn(33, 100), "33% chưa vượt 35%: chưa nạp lại");
            Assert.IsFalse(w.ShouldWarn(28, 100), "rơi lại xuống dưới 30% nhưng chưa nạp lại thì không báo");
            Assert.IsFalse(w.ShouldWarn(36, 100));
            Assert.IsTrue(w.ShouldWarn(20, 100), "đã lên trên 35% rồi rơi lại: báo lại");
        }

        [Test]
        public void Dead_NeverWarns()
        {
            var w = new LowHpWarning();
            Assert.IsFalse(w.ShouldWarn(0, 100));
            Assert.IsFalse(w.ShouldWarn(-5, 100));
        }

        [Test]
        public void InvalidMaxHp_NeverWarns()
        {
            var w = new LowHpWarning();
            Assert.IsFalse(w.ShouldWarn(10, 0));
            Assert.IsFalse(w.ShouldWarn(10, -1));
        }

        [Test]
        public void RearmRatioBelowLow_IsRaisedToLow()
        {
            var w = new LowHpWarning(0.30f, 0.10f);
            Assert.IsTrue(w.ShouldWarn(20, 100));
            Assert.IsFalse(w.ShouldWarn(25, 100));
            Assert.IsFalse(w.ShouldWarn(29, 100));
            Assert.IsFalse(w.ShouldWarn(31, 100)); // vừa vượt 30% nhưng ngưỡng nạp lại = 30%: nạp lại khi > 30%
            Assert.IsTrue(w.ShouldWarn(20, 100));
        }
    }
}
