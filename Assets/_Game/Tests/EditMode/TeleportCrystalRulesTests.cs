using Game.Core.Items;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class TeleportCrystalRulesTests
    {
        [Test]
        public void CanUse_WhenNoCrystal_ReturnsOutOfStock()
        {
            var result = TeleportCrystalRules.CanUse(crystalCount: 0, isDead: false, isChanneling: false);
            Assert.AreEqual(TeleportCrystalResult.OutOfStock, result);
        }

        [Test]
        public void CanUse_WhenDead_ReturnsPlayerDead()
        {
            var result = TeleportCrystalRules.CanUse(crystalCount: 5, isDead: true, isChanneling: false);
            Assert.AreEqual(TeleportCrystalResult.PlayerDead, result);
        }

        [Test]
        public void CanUse_WhenAlreadyChanneling_ReturnsAlreadyChanneling()
        {
            var result = TeleportCrystalRules.CanUse(crystalCount: 5, isDead: false, isChanneling: true);
            Assert.AreEqual(TeleportCrystalResult.AlreadyChanneling, result);
        }

        [Test]
        public void CanUse_WhenValid_ReturnsOk()
        {
            var result = TeleportCrystalRules.CanUse(crystalCount: 2, isDead: false, isChanneling: false);
            Assert.AreEqual(TeleportCrystalResult.Ok, result);
        }

        [Test]
        public void IsCancelledByMovement_OverThreshold_ReturnsTrue()
        {
            Assert.IsTrue(TeleportCrystalRules.IsCancelledByMovement(0.6f));
        }

        [Test]
        public void IsCancelledByMovement_UnderThreshold_ReturnsFalse()
        {
            Assert.IsFalse(TeleportCrystalRules.IsCancelledByMovement(0.3f));
            Assert.IsFalse(TeleportCrystalRules.IsCancelledByMovement(0.5f));
        }

        [Test]
        public void IsCancelledByDamage_WhenDamagePositive_ReturnsTrue()
        {
            Assert.IsTrue(TeleportCrystalRules.IsCancelledByDamage(10.0));
            Assert.IsTrue(TeleportCrystalRules.IsCancelledByDamage(0.1));
        }

        [Test]
        public void IsCancelledByDamage_WhenZeroOrNegative_ReturnsFalse()
        {
            Assert.IsFalse(TeleportCrystalRules.IsCancelledByDamage(0.0));
            Assert.IsFalse(TeleportCrystalRules.IsCancelledByDamage(-5.0));
        }
    }
}
