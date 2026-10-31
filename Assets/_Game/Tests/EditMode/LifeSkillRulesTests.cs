using Game.Core.LifeSkills;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class LifeSkillRulesTests
    {
        [Test]
        public void GetSkillLevel_ZeroOrNegative_ReturnsOne()
        {
            Assert.AreEqual(1, LifeSkillRules.GetSkillLevel(0));
            Assert.AreEqual(1, LifeSkillRules.GetSkillLevel(-50));
        }

        [Test]
        public void GetSkillLevel_CalculatesCorrectly()
        {
            Assert.AreEqual(1, LifeSkillRules.GetSkillLevel(99));
            Assert.AreEqual(2, LifeSkillRules.GetSkillLevel(100));
            Assert.AreEqual(2, LifeSkillRules.GetSkillLevel(150));
            Assert.AreEqual(4, LifeSkillRules.GetSkillLevel(320));
        }

        [Test]
        public void CalculateReactionWindow_IncreasesWithProficiency()
        {
            float windowLv1 = LifeSkillRules.CalculateReactionWindow(0);
            float windowLv5 = LifeSkillRules.CalculateReactionWindow(400);

            Assert.AreEqual(LifeSkillRules.BaseReactionWindowSeconds, windowLv1, 0.001f);
            Assert.Greater(windowLv5, windowLv1);
            Assert.LessOrEqual(windowLv5, LifeSkillRules.MaxReactionWindowSeconds);
        }

        [Test]
        public void CheckFishingSuccess_WithinWindow_ReturnsTrue()
        {
            float window = 1.5f;
            Assert.IsTrue(LifeSkillRules.CheckFishingSuccess(0.8f, window));
            Assert.IsTrue(LifeSkillRules.CheckFishingSuccess(1.5f, window));
        }

        [Test]
        public void CheckFishingSuccess_OutsideWindow_ReturnsFalse()
        {
            float window = 1.5f;
            Assert.IsFalse(LifeSkillRules.CheckFishingSuccess(1.6f, window));
            Assert.IsFalse(LifeSkillRules.CheckFishingSuccess(-0.1f, window));
        }

        [Test]
        public void CanCook_WhenLackingIngredient_ReturnsFalse()
        {
            var recipes = LifeSkillRules.GetDefaultRecipes();
            var recipe = recipes[0]; // Cần 1 cá

            Assert.IsFalse(LifeSkillRules.CanCook(recipe, cookingProficiency: 100, ownedIngredientCount: 0));
        }

        [Test]
        public void CanCook_WhenLevelTooLow_ReturnsFalse()
        {
            var recipes = LifeSkillRules.GetDefaultRecipes();
            var recipeLv2 = recipes[1]; // Cần level 2 (>= 100 exp)

            Assert.IsFalse(LifeSkillRules.CanCook(recipeLv2, cookingProficiency: 50, ownedIngredientCount: 5));
        }

        [Test]
        public void CanCook_WhenValid_ReturnsTrue()
        {
            var recipes = LifeSkillRules.GetDefaultRecipes();
            var recipeLv1 = recipes[0];

            Assert.IsTrue(LifeSkillRules.CanCook(recipeLv1, cookingProficiency: 0, ownedIngredientCount: 1));
        }

        [Test]
        public void CookingBuffInstance_TicksAndExpires()
        {
            var buff = new CookingBuffInstance("Cá Nướng", attackBonus: 5.0, durationSeconds: 10f);
            Assert.IsFalse(buff.IsExpired);

            buff.Tick(6f);
            Assert.AreEqual(4f, buff.remainingSeconds, 0.01f);
            Assert.IsFalse(buff.IsExpired);

            buff.Tick(5f);
            Assert.IsTrue(buff.IsExpired);
            Assert.AreEqual(0f, buff.remainingSeconds, 0.01f);
        }
    }
}
