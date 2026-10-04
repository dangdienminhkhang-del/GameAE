using Game.Core.Combat;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Phase 3 — công thức 07 §4: Damage = ATK × multiplier − DEF, tối thiểu 1, crit nhân sau.</summary>
    public class DamageCalculatorTests
    {
        [Test]
        public void Calculate_SubtractsDefense()
        {
            Assert.AreEqual(5.0, DamageCalculator.Calculate(10.0, 5.0), 0.0001);
        }

        [Test]
        public void Calculate_ClampsToMinimumOne()
        {
            Assert.AreEqual(1.0, DamageCalculator.Calculate(10.0, 100.0), 0.0001);
        }

        [Test]
        public void Calculate_AppliesSkillMultiplierBeforeDefense()
        {
            // 10 × 1.5 = 15; 15 − 5 = 10
            Assert.AreEqual(10.0, DamageCalculator.Calculate(10.0, 5.0, 1.5), 0.0001);
        }

        [Test]
        public void Calculate_CriticalMultipliesAfterDefense()
        {
            // (10 − 5) × 1.5 = 7.5
            Assert.AreEqual(7.5, DamageCalculator.Calculate(10.0, 5.0, 1.0, true, 1.5), 0.0001);
        }

        [Test]
        public void Calculate_NeverReturnsZeroOrNegative()
        {
            Assert.GreaterOrEqual(DamageCalculator.Calculate(0.0, 0.0), 1.0);
            Assert.GreaterOrEqual(DamageCalculator.Calculate(1.0, 50.0), 1.0);
        }
    }
}
