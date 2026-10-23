using Game.Core.Stats;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class HealthRegenRulesTests
    {
        [Test]
        public void OutOfCombat_RegensSlowly()
        {
            double heal = HealthRegenRules.ComputeHeal(100, 200, 10, inCombat: false, inSafeZone: false, isDead: false);
            Assert.AreEqual(200 * HealthRegenRules.OutOfCombatFractionPerSecond * 10, heal, 1e-9);
        }

        [Test]
        public void InCombat_OutsideSafeZone_NoRegen()
        {
            Assert.AreEqual(0.0, HealthRegenRules.ComputeHeal(100, 200, 10, true, false, false));
        }

        [Test]
        public void SafeZone_RegensFasterThanOutOfCombat()
        {
            double safe = HealthRegenRules.ComputeHeal(10, 1000, 1, false, true, false);
            double outside = HealthRegenRules.ComputeHeal(10, 1000, 1, false, false, false);
            Assert.Greater(safe, outside);
        }

        [Test]
        public void SafeZone_RegensEvenWhileCombatLocked()
        {
            Assert.Greater(HealthRegenRules.ComputeHeal(10, 1000, 1, true, true, false), 0.0);
        }

        [Test]
        public void NeverExceedsMaxHealth()
        {
            double heal = HealthRegenRules.ComputeHeal(199.5, 200, 1000, false, true, false);
            Assert.AreEqual(0.5, heal, 1e-9);
        }

        [Test]
        public void Dead_FullHealth_OrInvalidInput_NoRegen()
        {
            Assert.AreEqual(0.0, HealthRegenRules.ComputeHeal(0, 200, 5, false, true, true));
            Assert.AreEqual(0.0, HealthRegenRules.ComputeHeal(200, 200, 5, false, true, false));
            Assert.AreEqual(0.0, HealthRegenRules.ComputeHeal(50, 0, 5, false, true, false));
            Assert.AreEqual(0.0, HealthRegenRules.ComputeHeal(50, 200, 0, false, true, false));
            Assert.AreEqual(0.0, HealthRegenRules.ComputeHeal(50, 200, -3, false, true, false));
        }
    }
}
