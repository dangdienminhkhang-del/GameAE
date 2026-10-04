using Game.Core.Floor;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Phase 6/7 — hệ số theo tầng (docs/07 §5): 1.15^(f-1) sức mạnh, 1.20^(f-1) EXP.</summary>
    public class FloorScalingTests
    {
        [TestCase(1, 1.00f, 1.00f)]
        [TestCase(5, 1.75f, 2.07f)]
        [TestCase(10, 3.52f, 5.16f)]
        public void Multipliers_MatchDesignTable(int floor, float power, float exp)
        {
            Assert.AreEqual(power, FloorScaling.EnemyPowerMultiplier(floor), 0.005f);
            Assert.AreEqual(exp, FloorScaling.ExpMultiplier(floor), 0.005f);
        }

        [Test]
        public void FloorBelowOne_ClampsToFloorOne()
        {
            Assert.AreEqual(1.0f, FloorScaling.EnemyPowerMultiplier(0), 0.0001f);
            Assert.AreEqual(1.0f, FloorScaling.ExpMultiplier(-3), 0.0001f);
        }

        [Test]
        public void ExpScalesFasterThanEnemyPower()
        {
            Assert.Greater(FloorScaling.ExpMultiplier(10), FloorScaling.EnemyPowerMultiplier(10));
        }

        [Test]
        public void BaseStats_MatchDocsTableForFloorOne()
        {
            // docs/07 §5: quái thường base HP30/ATK5 → tầng 1 giữ nguyên.
            const double baseHp = 30.0;
            const double baseAtk = 5.0;
            float multiplier = FloorScaling.EnemyPowerMultiplier(1);

            Assert.AreEqual(30.0, baseHp * multiplier, 0.01);
            Assert.AreEqual(5.0, baseAtk * multiplier, 0.01);
        }
    }
}
