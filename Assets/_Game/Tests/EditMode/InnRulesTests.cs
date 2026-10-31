using Game.Core.Inn;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class InnRulesTests
    {
        [Test]
        public void CheckCanRest_WhenDead_ReturnsIsDead()
        {
            var result = InnRules.CheckCanRest(playerCol: 100, cost: 50, inCombat: false, isDead: true);
            Assert.AreEqual(InnRestResult.IsDead, result);
        }

        [Test]
        public void CheckCanRest_WhenInCombat_ReturnsInCombat()
        {
            var result = InnRules.CheckCanRest(playerCol: 100, cost: 50, inCombat: true, isDead: false);
            Assert.AreEqual(InnRestResult.InCombat, result);
        }

        [Test]
        public void CheckCanRest_WhenNotEnoughCol_ReturnsNotEnoughCol()
        {
            var result = InnRules.CheckCanRest(playerCol: 30, cost: 50, inCombat: false, isDead: false);
            Assert.AreEqual(InnRestResult.NotEnoughCol, result);
        }

        [Test]
        public void CheckCanRest_WhenExactCol_ReturnsSuccess()
        {
            var result = InnRules.CheckCanRest(playerCol: 50, cost: 50, inCombat: false, isDead: false);
            Assert.AreEqual(InnRestResult.Success, result);
        }

        [Test]
        public void CheckCanRest_WhenMoreThanEnoughCol_ReturnsSuccess()
        {
            var result = InnRules.CheckCanRest(playerCol: 500, cost: 50, inCombat: false, isDead: false);
            Assert.AreEqual(InnRestResult.Success, result);
        }

        [Test]
        public void CheckCanRest_NegativeCost_ReturnsInvalidArguments()
        {
            var result = InnRules.CheckCanRest(playerCol: 50, cost: -10, inCombat: false, isDead: false);
            Assert.AreEqual(InnRestResult.InvalidArguments, result);
        }

        [Test]
        public void CalculateHealAmount_ReturnsMissingHealth()
        {
            double heal = InnRules.CalculateHealAmount(currentHealth: 45.0, maxHealth: 100.0);
            Assert.AreEqual(55.0, heal, 0.001);
        }

        [Test]
        public void CalculateHealAmount_WhenFullOrOver_ReturnsZero()
        {
            double heal = InnRules.CalculateHealAmount(currentHealth: 100.0, maxHealth: 100.0);
            Assert.AreEqual(0.0, heal, 0.001);

            heal = InnRules.CalculateHealAmount(currentHealth: 120.0, maxHealth: 100.0);
            Assert.AreEqual(0.0, heal, 0.001);
        }

        [Test]
        public void GetTargetMorningHour_ReturnsMorningHourCorrectly()
        {
            double hour = InnRules.GetTargetMorningHour(8.0);
            Assert.AreEqual(8.0, hour, 0.001);

            double wrapped = InnRules.GetTargetMorningHour(32.0);
            Assert.AreEqual(8.0, wrapped, 0.001);
        }
    }
}
