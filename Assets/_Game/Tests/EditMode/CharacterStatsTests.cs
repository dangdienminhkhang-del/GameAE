using Game.Core.Combat;
using Game.Core.Stats;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Phase 3 — bảng chỉ số và đường cong EXP theo docs/07 §1.</summary>
    public class CharacterStatsTests
    {
        [TestCase(1, 100.0, 10.0, 5.0)]
        [TestCase(2, 115.0, 12.0, 6.0)]
        [TestCase(5, 160.0, 18.0, 9.0)]
        [TestCase(10, 235.0, 28.0, 14.0)]
        public void SetLevel_MatchesDesignTable(int level, double hp, double atk, double def)
        {
            var stats = new CharacterStats(level);

            Assert.AreEqual(hp, stats.MaxHealth, 0.0001);
            Assert.AreEqual(atk, stats.Attack, 0.0001);
            Assert.AreEqual(def, stats.Defense, 0.0001);
        }

        [Test]
        public void SetLevel_ClampsToMinimumOne()
        {
            var stats = new CharacterStats(0);

            Assert.AreEqual(1, stats.Level);
            Assert.AreEqual(100.0, stats.MaxHealth, 0.0001);
        }

        // EXPNext(level) = 100 + 20 × (level − 1)  (docs/07 §1, chốt ở Task B2)
        [TestCase(1, 100)]
        [TestCase(2, 120)]
        [TestCase(3, 140)]
        [TestCase(4, 160)]
        [TestCase(5, 180)]
        [TestCase(6, 200)]
        [TestCase(7, 220)]
        [TestCase(8, 240)]
        [TestCase(9, 260)]
        [TestCase(10, 280)]
        [TestCase(11, 300)]
        [TestCase(50, 1080)]
        public void ExpRequiredForNextLevel_MatchesExactValues(int level, int expected)
        {
            Assert.AreEqual(expected, CharacterStats.ExpRequiredForNextLevel(level));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void ExpRequiredForNextLevel_LevelBelowOne_TreatedAsLevelOne(int level)
        {
            Assert.AreEqual(100, CharacterStats.ExpRequiredForNextLevel(level));
        }

        [Test]
        public void ExpRequiredForNextLevel_EachLevelNeedsExactly20MoreThanThePreviousOne()
        {
            for (int level = 1; level < 100; level++)
            {
                int step = CharacterStats.ExpRequiredForNextLevel(level + 1) - CharacterStats.ExpRequiredForNextLevel(level);
                Assert.AreEqual(20, step, $"Level {level} → {level + 1}: mỗi level phải cần hơn đúng 20 EXP");
            }
        }

        [Test]
        public void CharacterStats_ImplementsICombatStats()
        {
            var stats = new CharacterStats(3);

            Assert.IsInstanceOf<ICombatStats>(stats);
            Assert.AreEqual(0.10, ((ICombatStats)stats).CriticalChance, 0.0001);
            Assert.AreEqual(1.5, ((ICombatStats)stats).CriticalDamage, 0.0001);
        }

        [Test]
        public void SetMaxHealthOverride_ChangesMaxHealthOnly()
        {
            var stats = new CharacterStats(1);
            stats.SetMaxHealthOverride(250.0);

            Assert.AreEqual(250.0, stats.MaxHealth, 0.0001);
            Assert.AreEqual(10.0, stats.Attack, 0.0001);
        }
    }
}
