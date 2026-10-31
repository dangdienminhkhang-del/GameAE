using System.Collections.Generic;
using Game.Core.Social;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class SocialRulesTests
    {
        [Test]
        public void CanAddMember_WhenPartyFull_ReturnsFalse()
        {
            var members = new List<PartyMember>();
            for (int i = 0; i < PartyRules.MaxPartySize; i++)
            {
                members.Add(new PartyMember($"p{i}", $"Player{i}", 10, 100));
            }

            bool canAdd = PartyRules.CanAddMember(members.Count, "p_new", members);
            Assert.IsFalse(canAdd);
        }

        [Test]
        public void CanAddMember_WhenAlreadyInParty_ReturnsFalse()
        {
            var members = new List<PartyMember>
            {
                new PartyMember("kirito", "Kirito", 50, 500),
                new PartyMember("asuna", "Asuna", 48, 450)
            };

            bool canAdd = PartyRules.CanAddMember(members.Count, "asuna", members);
            Assert.IsFalse(canAdd);
        }

        [Test]
        public void CanAddMember_WhenValid_ReturnsTrue()
        {
            var members = new List<PartyMember>
            {
                new PartyMember("kirito", "Kirito", 50, 500)
            };

            bool canAdd = PartyRules.CanAddMember(members.Count, "klein", members);
            Assert.IsTrue(canAdd);
        }

        [Test]
        public void GetPartyExpMultiplier_IncreasesWithMembers()
        {
            Assert.AreEqual(1.0, PartyRules.GetPartyExpMultiplier(1));
            Assert.AreEqual(1.1, PartyRules.GetPartyExpMultiplier(2), 0.001);
            Assert.AreEqual(1.5, PartyRules.GetPartyExpMultiplier(6), 0.001);
            // Vượt quá 6 người vẫn clamp ở 1.5
            Assert.AreEqual(1.5, PartyRules.GetPartyExpMultiplier(10), 0.001);
        }

        [Test]
        public void CalculateExpPerMember_DistributesFairlyWithBonus()
        {
            // Quái cho 100 EXP cơ bản
            // 1 người: 100 EXP
            Assert.AreEqual(100, PartyRules.CalculateExpPerMember(100, 1));

            // 2 người: 100 * 1.1 = 110 / 2 = 55 EXP mỗi người
            Assert.AreEqual(55, PartyRules.CalculateExpPerMember(100, 2));

            // 0 người: 0 EXP
            Assert.AreEqual(0, PartyRules.CalculateExpPerMember(100, 0));
        }

        [Test]
        public void IsValidRaid_RangeCheck()
        {
            Assert.IsFalse(PartyRules.IsValidRaid(1));
            Assert.IsTrue(PartyRules.IsValidRaid(2));
            Assert.IsTrue(PartyRules.IsValidRaid(PartyRules.MaxRaidParties));
            Assert.IsFalse(PartyRules.IsValidRaid(PartyRules.MaxRaidParties + 1));
        }
    }
}
