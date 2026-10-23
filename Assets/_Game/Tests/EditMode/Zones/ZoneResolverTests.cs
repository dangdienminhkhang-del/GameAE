using System.Collections.Generic;
using Game.PvE.Zones;
using NUnit.Framework;

namespace Game.Tests.EditMode.Zones
{
    /// <summary>Phase 9.7a — chọn vùng khi chồng lấn và chống chớp ở biên (thuần logic).</summary>
    public class ZoneResolverTests
    {
        private static List<ZoneCandidate> List(params ZoneCandidate[] items) => new List<ZoneCandidate>(items);

        [Test]
        public void Pick_EmptyOrNull_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, ZoneResolver.Pick(null));
            Assert.AreEqual(-1, ZoneResolver.Pick(new List<ZoneCandidate>()));
        }

        [Test]
        public void Pick_SingleCandidate_ReturnsIt()
        {
            Assert.AreEqual(0, ZoneResolver.Pick(List(new ZoneCandidate("a", 0, 10f))));
        }

        [Test]
        public void Pick_HigherPriorityWins_EvenIfLarger()
        {
            var zones = List(new ZoneCandidate("small_low", 0, 1f), new ZoneCandidate("big_high", 5, 100f));
            Assert.AreEqual(1, ZoneResolver.Pick(zones));
        }

        [Test]
        public void Pick_SamePriority_SmallerAreaWins()
        {
            var zones = List(new ZoneCandidate("big", 1, 100f), new ZoneCandidate("small", 1, 4f));
            Assert.AreEqual(1, ZoneResolver.Pick(zones));
        }

        [Test]
        public void Pick_SamePriorityAndArea_SmallerIdWins_IndependentOfOrder()
        {
            var forward = List(new ZoneCandidate("zone.a", 1, 4f), new ZoneCandidate("zone.b", 1, 4f));
            var backward = List(new ZoneCandidate("zone.b", 1, 4f), new ZoneCandidate("zone.a", 1, 4f));

            Assert.AreEqual("zone.a", forward[ZoneResolver.Pick(forward)].Id);
            Assert.AreEqual("zone.a", backward[ZoneResolver.Pick(backward)].Id);
        }
    }

    public class ZoneTrackerLogicTests
    {
        private static List<ZoneCandidate> In(string id, int priority = 0) =>
            new List<ZoneCandidate> { new ZoneCandidate(id, priority, 10f) };

        private static readonly List<ZoneCandidate> Nowhere = new List<ZoneCandidate>();

        [Test]
        public void FirstZone_EntersImmediately()
        {
            var logic = new ZoneTrackerLogic();

            bool entered = logic.Update(In("town"), false, out string id);

            Assert.IsTrue(entered);
            Assert.AreEqual("town", id);
            Assert.AreEqual("town", logic.Current);
        }

        [Test]
        public void NoZoneContainsPlayer_KeepsPreviousAndEmitsNothing()
        {
            var logic = new ZoneTrackerLogic();
            logic.Update(In("town"), false, out _);

            bool entered = logic.Update(Nowhere, false, out string id);

            Assert.IsFalse(entered);
            Assert.IsNull(id);
            Assert.AreEqual("town", logic.Current, "Không vùng nào chứa → giữ vùng cũ.");
        }

        [Test]
        public void SameZone_DoesNotEmitAgain()
        {
            var logic = new ZoneTrackerLogic();
            logic.Update(In("town"), false, out _);

            Assert.IsFalse(logic.Update(In("town"), false, out _));
            Assert.IsFalse(logic.Update(In("town"), false, out _));
        }

        [Test]
        public void SwitchingZone_NeedsDwellTicks()
        {
            var logic = new ZoneTrackerLogic(2);
            logic.Update(In("town"), false, out _);

            Assert.IsFalse(logic.Update(In("farm"), false, out _), "Mới một nhịp ở vùng mới.");
            Assert.AreEqual("town", logic.Current);

            Assert.IsTrue(logic.Update(In("farm"), false, out string id), "Đủ hai nhịp → đổi.");
            Assert.AreEqual("farm", id);
            Assert.AreEqual("farm", logic.Current);
        }

        [Test]
        public void BoundaryFlicker_NeverEmits()
        {
            var logic = new ZoneTrackerLogic(2);
            logic.Update(In("town"), false, out _);

            int emitted = 0;
            for (int i = 0; i < 20; i++)
            {
                // Chớp qua lại ở biên: farm, town, farm, town...
                if (logic.Update(In(i % 2 == 0 ? "farm" : "town"), false, out _)) emitted++;
            }

            Assert.AreEqual(0, emitted);
            Assert.AreEqual("town", logic.Current);
        }

        [Test]
        public void ImmediateRequest_ReEmitsSameZone()
        {
            var logic = new ZoneTrackerLogic();
            logic.Update(In("town"), false, out _);

            bool entered = logic.Update(In("town"), true, out string id);

            Assert.IsTrue(entered, "Vừa vào tầng: phát lại để banner, nhạc, camera đúng ngay.");
            Assert.AreEqual("town", id);
        }

        [Test]
        public void ImmediateRequest_SwitchesWithoutDwell()
        {
            var logic = new ZoneTrackerLogic(5);
            logic.Update(In("town"), false, out _);

            Assert.IsTrue(logic.Update(In("farm"), true, out string id));
            Assert.AreEqual("farm", id);
        }

        [Test]
        public void Reset_ForgetsCurrent_SoNextZoneEmitsImmediately()
        {
            var logic = new ZoneTrackerLogic(3);
            logic.Update(In("town"), false, out _);
            logic.Reset();

            Assert.IsNull(logic.Current);
            Assert.IsTrue(logic.Update(In("other_floor_town"), false, out string id));
            Assert.AreEqual("other_floor_town", id);
        }

        [Test]
        public void OverlappingZones_UsesPriority()
        {
            var logic = new ZoneTrackerLogic();
            var both = new List<ZoneCandidate>
            {
                new ZoneCandidate("farm", 0, 100f),
                new ZoneCandidate("approach", 3, 10f)
            };

            logic.Update(both, false, out string id);

            Assert.AreEqual("approach", id);
        }
    }
}
