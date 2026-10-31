using Game.Core.Skills;
using Game.PvE.Enemy;
using Game.PvE.Player;
using Game.PvE.Skills;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class SearchingHidingTests
    {
        [Test]
        public void CalculateSearchingRadius_ScalesWithSkillLevel()
        {
            float baseRadius = SearchingHidingRules.CalculateSearchingRadius(0);
            Assert.AreEqual(SearchingHidingRules.DefaultSearchingRadius, baseRadius, 1e-4f);

            float level50 = SearchingHidingRules.CalculateSearchingRadius(50);
            Assert.Greater(level50, baseRadius, "Level 50 Searching phải có tầm quét xa hơn level 0.");
            Assert.AreEqual(SearchingHidingRules.DefaultSearchingRadius + 5.0f, level50, 1e-4f);

            float maxLevel = SearchingHidingRules.CalculateSearchingRadius(200);
            Assert.AreEqual(SearchingHidingRules.MaxSearchingRadius, maxLevel, 1e-4f, "Phải clamp ở mức bán kính tối đa.");
        }

        [Test]
        public void IsWithinSearchingRange_AccuratelyEvaluatesDistance()
        {
            Vector2 player = new Vector2(0f, 0f);
            Vector2 monsterNear = new Vector2(3f, 4f); // dist = 5m
            Vector2 monsterFar = new Vector2(8f, 6f);  // dist = 10m

            float radius = 7.5f;
            Assert.IsTrue(SearchingHidingRules.IsWithinSearchingRange(player, monsterNear, radius));
            Assert.IsFalse(SearchingHidingRules.IsWithinSearchingRange(player, monsterFar, radius));
        }

        [Test]
        public void CalculateEffectiveDetectionRange_StandingHiding_ReducesEnemyDetection()
        {
            float baseRange = 10.0f;

            // Không ẩn nấp
            float normal = SearchingHidingRules.CalculateEffectiveDetectionRange(baseRange, 0f);
            Assert.AreEqual(10.0f, normal, 1e-4f);

            // Đứng yên ẩn nấp (giảm 60%)
            float standingHiding = SearchingHidingRules.CalculateEffectiveDetectionRange(baseRange, SearchingHidingRules.HidingReductionStanding);
            Assert.AreEqual(4.0f, standingHiding, 1e-4f, "Tầm phát hiện 10m khi giảm 60% phải còn 4m.");

            // Tầm tối thiểu không được nhỏ hơn 1.0m
            float tinyRange = SearchingHidingRules.CalculateEffectiveDetectionRange(1.5f, 0.9f);
            Assert.AreEqual(1.0f, tinyRange, 1e-4f);
        }

        [Test]
        public void ShouldBreakHiding_TriggersOnAttackOrDamageOrSkill()
        {
            Assert.IsTrue(SearchingHidingRules.ShouldBreakHiding(didAttack: true, didTakeDamage: false, isUsingSkill: false));
            Assert.IsTrue(SearchingHidingRules.ShouldBreakHiding(didAttack: false, didTakeDamage: true, isUsingSkill: false));
            Assert.IsTrue(SearchingHidingRules.ShouldBreakHiding(didAttack: false, didTakeDamage: false, isUsingSkill: true));
            Assert.IsFalse(SearchingHidingRules.ShouldBreakHiding(didAttack: false, didTakeDamage: false, isUsingSkill: false));
        }

        [Test]
        public void EvaluateHidingFactor_StandingVsMoving()
        {
            // Đứng yên chưa đủ thời gian (< 1.2s)
            float notYet = SearchingHidingRules.EvaluateHidingFactor(0.8f, isMoving: false, isStealthBroken: false);
            Assert.AreEqual(0f, notYet);

            // Đứng yên đủ thời gian (>= 1.2s) -> HidingReductionStanding (0.60)
            float standing = SearchingHidingRules.EvaluateHidingFactor(1.5f, isMoving: false, isStealthBroken: false);
            Assert.AreEqual(SearchingHidingRules.HidingReductionStanding, standing, 1e-4f);

            // Di chuyển chậm -> HidingReductionMoving (0.30)
            float moving = SearchingHidingRules.EvaluateHidingFactor(1.5f, isMoving: true, isStealthBroken: false);
            Assert.AreEqual(SearchingHidingRules.HidingReductionMoving, moving, 1e-4f);

            // Nếu đã bị lộ (stealth broken) -> 0
            float broken = SearchingHidingRules.EvaluateHidingFactor(2.0f, isMoving: false, isStealthBroken: true);
            Assert.AreEqual(0f, broken);
        }

        [Test]
        public void SearchingHidingController_EnterAndBreakHiding_TogglesState()
        {
            var go = new GameObject("PlayerStealthTest", typeof(PlayerController), typeof(SearchingHidingController));
            var ctrl = go.GetComponent<SearchingHidingController>();

            try
            {
                Assert.IsFalse(ctrl.IsHiding);

                ctrl.EnterHiding();
                Assert.IsTrue(ctrl.IsHiding);
                Assert.Greater(SearchingHidingController.CurrentPlayerHidingFactor, 0f);

                ctrl.BreakHiding(silent: true);
                Assert.IsFalse(ctrl.IsHiding);
                Assert.AreEqual(0f, SearchingHidingController.CurrentPlayerHidingFactor);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
