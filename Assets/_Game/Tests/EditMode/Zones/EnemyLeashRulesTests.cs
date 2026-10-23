using Game.PvE.Zones;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Zones
{
    /// <summary>Phase 9.7a — quái dừng đuổi khi mục tiêu vào Safe Zone, quay về, đuổi lại khi ra ngoài.</summary>
    public class EnemyLeashRulesTests
    {
        private const float Detection = 6f;
        private static readonly Vector2 Home = new Vector2(10f, 0f);

        [Test]
        public void TargetInSafeZone_IsDropped()
        {
            Assert.IsTrue(EnemyLeashRules.ShouldDropTarget(true));
            Assert.IsFalse(EnemyLeashRules.ShouldDropTarget(false));
        }

        [Test]
        public void ShouldChase_RequiresTargetOutsideSafeZoneAndInRange()
        {
            Assert.IsTrue(EnemyLeashRules.ShouldChase(true, false, 3f, Detection));
            Assert.IsTrue(EnemyLeashRules.ShouldChase(true, false, Detection, Detection), "Đúng biên vẫn tính là trong tầm.");

            Assert.IsFalse(EnemyLeashRules.ShouldChase(false, false, 3f, Detection), "Không có mục tiêu.");
            Assert.IsFalse(EnemyLeashRules.ShouldChase(true, true, 3f, Detection), "Mục tiêu trong Safe Zone.");
            Assert.IsFalse(EnemyLeashRules.ShouldChase(true, false, Detection + 0.1f, Detection), "Ngoài tầm phát hiện.");
        }

        [Test]
        public void HasArrivedHome_UsesRadius()
        {
            Assert.IsTrue(EnemyLeashRules.HasArrivedHome(Home, Home));
            Assert.IsTrue(EnemyLeashRules.HasArrivedHome(Home + new Vector2(0.2f, 0f), Home));
            Assert.IsFalse(EnemyLeashRules.HasArrivedHome(Home + new Vector2(1f, 0f), Home));
            Assert.IsTrue(EnemyLeashRules.HasArrivedHome(Home + new Vector2(1f, 0f), Home, 2f));
        }

        [Test]
        public void Idle_ChasesValidTarget()
        {
            var action = EnemyLeashRules.DecideIdle(true, false, 3f, Detection, false, Home, Home);
            Assert.AreEqual(EnemyLeashRules.IdleAction.Chase, action);
        }

        [Test]
        public void Idle_AfterDroppingTarget_ReturnsHome_ThenStays()
        {
            Vector2 away = Home + new Vector2(4f, 0f);

            Assert.AreEqual(EnemyLeashRules.IdleAction.ReturnHome,
                EnemyLeashRules.DecideIdle(true, true, 4f, Detection, true, away, Home));

            Assert.AreEqual(EnemyLeashRules.IdleAction.Stay,
                EnemyLeashRules.DecideIdle(true, true, 4f, Detection, true, Home, Home), "Đã về tới nơi.");
        }

        [Test]
        public void Idle_WhileReturning_ChasesAgainIfTargetLeavesSafeZoneInRange()
        {
            Vector2 away = Home + new Vector2(4f, 0f);

            var action = EnemyLeashRules.DecideIdle(true, false, 4f, Detection, true, away, Home);

            Assert.AreEqual(EnemyLeashRules.IdleAction.Chase, action);
        }

        [Test]
        public void Idle_WhileReturning_TargetLeavesSafeZoneButOutOfRange_KeepsReturning()
        {
            Vector2 away = Home + new Vector2(4f, 0f);

            var action = EnemyLeashRules.DecideIdle(true, false, Detection + 5f, Detection, true, away, Home);

            Assert.AreEqual(EnemyLeashRules.IdleAction.ReturnHome, action);
        }

        [Test]
        public void Idle_TargetStandingInSafeZoneEdge_NeverFlipsToChase()
        {
            // Người chơi đứng yên ở rìa Safe Zone, ngay trong tầm phát hiện: quái đứng yên ở nhà và KHÔNG bao giờ Chase.
            for (int i = 0; i < 50; i++)
            {
                var action = EnemyLeashRules.DecideIdle(true, true, 2f, Detection, false, Home, Home);
                Assert.AreEqual(EnemyLeashRules.IdleAction.Stay, action);
            }
        }

        [Test]
        public void ChaseThenSafeZone_FullCycle()
        {
            Vector2 position = Home + new Vector2(3f, 0f);

            // 1) Đang đuổi.
            Assert.IsTrue(EnemyLeashRules.ShouldChase(true, false, 3f, Detection));
            // 2) Người chơi vào Safe Zone: bỏ mục tiêu.
            Assert.IsTrue(EnemyLeashRules.ShouldDropTarget(true));
            // 3) Quay về nhà.
            Assert.AreEqual(EnemyLeashRules.IdleAction.ReturnHome,
                EnemyLeashRules.DecideIdle(true, true, 3f, Detection, true, position, Home));
            // 4) Về tới nơi thì đứng yên.
            Assert.AreEqual(EnemyLeashRules.IdleAction.Stay,
                EnemyLeashRules.DecideIdle(true, true, 8f, Detection, true, Home, Home));
            // 5) Người chơi ra ngoài và vào tầm: đuổi lại.
            Assert.AreEqual(EnemyLeashRules.IdleAction.Chase,
                EnemyLeashRules.DecideIdle(true, false, 5f, Detection, false, Home, Home));
        }
    }

    public class SafeZoneCheckThrottleTests
    {
        [Test]
        public void FirstTick_IsDue()
        {
            var throttle = new SafeZoneCheckThrottle();
            Assert.IsTrue(throttle.Tick(0.016f));
        }

        [Test]
        public void ShortTicks_AreNotDue_UntilIntervalPasses()
        {
            var throttle = new SafeZoneCheckThrottle(0.2f);
            throttle.Tick(0f); // lần đầu luôn đến hạn

            Assert.IsFalse(throttle.Tick(0.05f));
            Assert.IsFalse(throttle.Tick(0.05f));
            Assert.IsTrue(throttle.Tick(0.25f));
        }

        [Test]
        public void ChecksAtMostAboutFivePerSecond_At60Fps()
        {
            var throttle = new SafeZoneCheckThrottle();
            int checks = 0;
            for (int frame = 0; frame < 60; frame++)
            {
                if (throttle.Tick(1f / 60f)) checks++;
            }

            Assert.LessOrEqual(checks, 6, "Một giây chỉ kiểm khoảng 5 lần, không phải mỗi frame.");
            Assert.GreaterOrEqual(checks, 4);
        }

        [Test]
        public void NegativeDelta_IsTreatedAsZero()
        {
            var throttle = new SafeZoneCheckThrottle(0.2f);
            throttle.Tick(0f);

            Assert.IsFalse(throttle.Tick(-5f));
        }

        [Test]
        public void Reset_MakesNextTickDue()
        {
            var throttle = new SafeZoneCheckThrottle(0.2f);
            throttle.Tick(0f);
            Assert.IsFalse(throttle.Tick(0.01f));

            throttle.Reset();

            Assert.IsTrue(throttle.Tick(0.01f));
        }
    }
}
