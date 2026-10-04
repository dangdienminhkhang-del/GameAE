using Game.Core.Stats;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Phase 4 — tiến trình level (docs/07 §1): EXP dư giữ lại, lên nhiều level một lần.</summary>
    public class LevelProgressionTests
    {
        [Test]
        public void NewProgression_StartsAtLevelOne()
        {
            var progression = new LevelProgression();

            Assert.AreEqual(1, progression.Level);
            Assert.AreEqual(0, progression.CurrentExp);
            Assert.AreEqual(100, progression.RequiredExp);
        }

        [Test]
        public void AddExperience_BelowThreshold_NoLevelUp()
        {
            var progression = new LevelProgression();

            int levelUps = progression.AddExperience(60);

            Assert.AreEqual(0, levelUps);
            Assert.AreEqual(1, progression.Level);
            Assert.AreEqual(60, progression.CurrentExp);
        }

        [Test]
        public void AddExperience_ReachingThreshold_LevelsUpAndKeepsRemainder()
        {
            var progression = new LevelProgression();

            int levelUps = progression.AddExperience(150);

            Assert.AreEqual(1, levelUps);
            Assert.AreEqual(2, progression.Level);
            Assert.AreEqual(50, progression.CurrentExp, "EXP dư phải giữ lại cho level kế");
            Assert.AreEqual(CharacterStats.ExpRequiredForNextLevel(2), progression.RequiredExp);
        }

        [Test]
        public void AddExperience_EnoughForMultipleLevels_LevelsUpMultipleTimes()
        {
            var progression = new LevelProgression();
            int needLevel2 = CharacterStats.ExpRequiredForNextLevel(1);
            int needLevel3 = CharacterStats.ExpRequiredForNextLevel(2);

            int levelUps = progression.AddExperience(needLevel2 + needLevel3 + 18);

            Assert.AreEqual(2, levelUps);
            Assert.AreEqual(3, progression.Level);
            Assert.AreEqual(18, progression.CurrentExp);
        }

        [Test]
        public void AddExperience_IgnoresNonPositiveAmounts()
        {
            var progression = new LevelProgression();

            Assert.AreEqual(0, progression.AddExperience(0));
            Assert.AreEqual(0, progression.AddExperience(-50));
            Assert.AreEqual(0, progression.CurrentExp);
        }

        [Test]
        public void ResetTo_SetsLevelAndClearsExp()
        {
            var progression = new LevelProgression(5, 200);
            progression.ResetTo(1);

            Assert.AreEqual(1, progression.Level);
            Assert.AreEqual(0, progression.CurrentExp);
        }
    }
}
