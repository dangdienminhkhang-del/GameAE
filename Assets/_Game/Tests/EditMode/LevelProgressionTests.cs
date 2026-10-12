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

        [TestCase(1, 100)]
        [TestCase(2, 120)]
        [TestCase(5, 180)]
        [TestCase(10, 280)]
        public void RequiredExp_FollowsNewFormula(int level, int expected)
        {
            var progression = new LevelProgression(level, 0);

            Assert.AreEqual(expected, progression.RequiredExp, "RequiredExp phải = 100 + 20 × (level − 1)");
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

        // ── Save cũ (Task B2, mục 3): EXP đang lưu ≥ EXPNext theo công thức mới → lên level bằng EXP dư ──

        [Test]
        public void OldSave_ExpAboveNewThreshold_LevelsUpWithRemainder()
        {
            // Level 5 cần 180 theo công thức mới; save cũ đang giữ 200.
            var viaConstructor = new LevelProgression(5, 200);
            Assert.AreEqual(6, viaConstructor.Level);
            Assert.AreEqual(20, viaConstructor.CurrentExp, "EXP dư phải được giữ, không mất tiến độ");

            var viaResetTo = new LevelProgression();
            viaResetTo.ResetTo(5, 200);
            Assert.AreEqual(6, viaResetTo.Level);
            Assert.AreEqual(20, viaResetTo.CurrentExp);
        }

        [Test]
        public void OldSave_BelowOldThresholdButAboveNew_StillLevelsUp()
        {
            // Level 2: ngưỡng cũ 283, ngưỡng mới 120. EXP 150 phải lên level 3, dư 30 (Level 3 cần 140).
            var progression = new LevelProgression(2, 150);

            Assert.AreEqual(3, progression.Level);
            Assert.AreEqual(30, progression.CurrentExp);
            Assert.AreEqual(CharacterStats.ExpRequiredForNextLevel(3), progression.RequiredExp);
        }

        [Test]
        public void OldSave_ExpExactlyAtThreshold_LevelsUpWithZeroRemainder()
        {
            var progression = new LevelProgression(1, 100);

            Assert.AreEqual(2, progression.Level);
            Assert.AreEqual(0, progression.CurrentExp);
        }

        [Test]
        public void OldSave_ExpJustBelowThreshold_NoLevelUp()
        {
            var progression = new LevelProgression(5, 179);

            Assert.AreEqual(5, progression.Level);
            Assert.AreEqual(179, progression.CurrentExp);
        }

        [Test]
        public void OldSave_ExpEnoughForSeveralLevels_LevelsUpRepeatedly()
        {
            // 100 (L1) + 120 (L2) + 140 (L3) = 360; 500 − 360 = 140 < 160 (L4) → dừng ở level 4.
            var progression = new LevelProgression(1, 500);

            Assert.AreEqual(4, progression.Level);
            Assert.AreEqual(140, progression.CurrentExp);
        }

        [Test]
        public void OldSave_HugeExp_IsNotClamped_AndTotalExpIsConserved()
        {
            const int savedExp = 1000000;
            var progression = new LevelProgression(1, savedExp);

            Assert.Less(progression.CurrentExp, progression.RequiredExp, "Sau khi nạp, EXP phải nhỏ hơn EXPNext");

            long spent = 0;
            for (int level = 1; level < progression.Level; level++)
                spent += CharacterStats.ExpRequiredForNextLevel(level);

            Assert.AreEqual(savedExp, spent + progression.CurrentExp, "Tổng EXP phải bảo toàn (không clamp, không mất)");
        }
    }
}
