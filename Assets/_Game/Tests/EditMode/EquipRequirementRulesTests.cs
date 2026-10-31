using Game.Core.Items;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class EquipRequirementRulesTests
    {
        [Test]
        public void NoRequirement_AlwaysMeets()
        {
            Assert.IsTrue(EquipRequirementRules.MeetsStrength(0, 0));
            Assert.IsTrue(EquipRequirementRules.MeetsStrength(-5, 0));
        }

        [Test]
        public void Requirement_NeedsEnoughStrength()
        {
            Assert.IsFalse(EquipRequirementRules.MeetsStrength(10, 9));
            Assert.IsTrue(EquipRequirementRules.MeetsStrength(10, 10));
            Assert.IsTrue(EquipRequirementRules.MeetsStrength(10, 25));
            Assert.IsFalse(EquipRequirementRules.MeetsStrength(1, -3));
        }

        [Test]
        public void MissingMessage_EmptyWhenMet_AndNamesRequirementWhenNot()
        {
            Assert.AreEqual(string.Empty, EquipRequirementRules.MissingMessage(5, 5));
            string msg = EquipRequirementRules.MissingMessage(12, 4);
            StringAssert.Contains("12", msg);
            StringAssert.Contains("4", msg);
        }

        [Test]
        public void RequirementLabel_EmptyWithoutRequirement()
        {
            Assert.AreEqual(string.Empty, EquipRequirementRules.RequirementLabel(0));
            StringAssert.Contains("8", EquipRequirementRules.RequirementLabel(8));
        }
    }
}
