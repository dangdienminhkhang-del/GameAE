using Game.Core.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Kiểm thử bộ quy tắc nhiều thanh máu Boss theo phong cách SAO (BossHealthBarRules).
    /// </summary>
    public class BossHealthBarRulesTests
    {
        [Test]
        public void DefaultValues_MatchSAOSpecification()
        {
            Assert.AreEqual(4, BossHealthBarRules.DefaultBarCount, "Mặc định Boss SAO có 4 thanh máu.");
            Assert.AreEqual(10.0, BossHealthBarRules.DefaultBossHealthMultiplier, 1e-6, "Mặc định máu Boss nhân 10.");
            Assert.Greater(BossHealthBarRules.DefaultEnrageAttackMultiplier, 1.0, "Cuồng nộ phải tăng công.");
            Assert.Greater(BossHealthBarRules.DefaultEnrageSpeedMultiplier, 1.0, "Cuồng nộ phải tăng tốc.");
            Assert.Less(BossHealthBarRules.DefaultEnrageCooldownMultiplier, 1.0, "Cuồng nộ phải giảm cooldown (đánh nhanh hơn).");
        }

        [Test]
        public void Calculate_FullHealth_ReturnsTotalBarsAndFullFill()
        {
            var state = BossHealthBarRules.Calculate(1000.0, 1000.0, 4);

            Assert.AreEqual(4, state.RemainingBars);
            Assert.AreEqual(1.0f, state.CurrentBarFill, 1e-4f);
            Assert.AreEqual(4, state.TotalBars);
            Assert.IsFalse(state.IsFinalBar);
        }

        [Test]
        public void Calculate_ZeroOrNegativeHealth_ReturnsZeroBarsAndZeroFill()
        {
            var stateZero = BossHealthBarRules.Calculate(0.0, 1000.0, 4);
            Assert.AreEqual(0, stateZero.RemainingBars);
            Assert.AreEqual(0.0f, stateZero.CurrentBarFill, 1e-4f);
            Assert.IsFalse(stateZero.IsFinalBar);

            var stateNegative = BossHealthBarRules.Calculate(-50.0, 1000.0, 4);
            Assert.AreEqual(0, stateNegative.RemainingBars);
            Assert.AreEqual(0.0f, stateNegative.CurrentBarFill, 1e-4f);
            Assert.IsFalse(stateNegative.IsFinalBar);
        }

        [Test]
        public void Calculate_MultiBarProgression_CorrectSlicesAndFills()
        {
            // Max = 1000, 4 thanh -> mỗi thanh 250 HP:
            // Bar 4: (750, 1000]
            // Bar 3: (500, 750]
            // Bar 2: (250, 500]
            // Bar 1: (0, 250] - Final bar

            // 1. Khi ở 875 HP (giữa thanh 4)
            var state875 = BossHealthBarRules.Calculate(875.0, 1000.0, 4);
            Assert.AreEqual(4, state875.RemainingBars);
            Assert.AreEqual(0.5f, state875.CurrentBarFill, 1e-3f);
            Assert.IsFalse(state875.IsFinalBar);

            // 2. Khi vừa hết thanh 4, còn đúng 750 HP -> chuyển sang thanh 3 đầy 100%
            var state750 = BossHealthBarRules.Calculate(750.0, 1000.0, 4);
            Assert.AreEqual(3, state750.RemainingBars);
            Assert.AreEqual(1.0f, state750.CurrentBarFill, 1e-3f);
            Assert.IsFalse(state750.IsFinalBar);

            // 3. Khi ở 625 HP (giữa thanh 3)
            var state625 = BossHealthBarRules.Calculate(625.0, 1000.0, 4);
            Assert.AreEqual(3, state625.RemainingBars);
            Assert.AreEqual(0.5f, state625.CurrentBarFill, 1e-3f);
            Assert.IsFalse(state625.IsFinalBar);

            // 4. Khi vừa hết thanh 3, còn đúng 500 HP -> chuyển sang thanh 2 đầy 100%
            var state500 = BossHealthBarRules.Calculate(500.0, 1000.0, 4);
            Assert.AreEqual(2, state500.RemainingBars);
            Assert.AreEqual(1.0f, state500.CurrentBarFill, 1e-3f);
            Assert.IsFalse(state500.IsFinalBar);

            // 5. Khi vừa hết thanh 2, còn đúng 250 HP -> vào thanh 1 (thanh cuối) đầy 100%
            var state250 = BossHealthBarRules.Calculate(250.0, 1000.0, 4);
            Assert.AreEqual(1, state250.RemainingBars);
            Assert.AreEqual(1.0f, state250.CurrentBarFill, 1e-3f);
            Assert.IsTrue(state250.IsFinalBar, "250 HP phải là thanh cuối.");

            // 6. Khi ở 125 HP (giữa thanh cuối)
            var state125 = BossHealthBarRules.Calculate(125.0, 1000.0, 4);
            Assert.AreEqual(1, state125.RemainingBars);
            Assert.AreEqual(0.5f, state125.CurrentBarFill, 1e-3f);
            Assert.IsTrue(state125.IsFinalBar, "125 HP phải là thanh cuối.");

            // 7. Khi ở 1 HP (gần chết)
            var state1 = BossHealthBarRules.Calculate(1.0, 1000.0, 4);
            Assert.AreEqual(1, state1.RemainingBars);
            Assert.AreEqual(1.0f / 250.0f, state1.CurrentBarFill, 1e-3f);
            Assert.IsTrue(state1.IsFinalBar);
        }

        [Test]
        public void Calculate_SingleBarBoss_AlwaysFinalBarUntilDead()
        {
            var stateFull = BossHealthBarRules.Calculate(500.0, 500.0, 1);
            Assert.AreEqual(1, stateFull.RemainingBars);
            Assert.AreEqual(1.0f, stateFull.CurrentBarFill, 1e-3f);
            Assert.IsTrue(stateFull.IsFinalBar);

            var stateHalf = BossHealthBarRules.Calculate(250.0, 500.0, 1);
            Assert.AreEqual(1, stateHalf.RemainingBars);
            Assert.AreEqual(0.5f, stateHalf.CurrentBarFill, 1e-3f);
            Assert.IsTrue(stateHalf.IsFinalBar);
        }

        [Test]
        public void Calculate_InvalidArguments_HandledGracefully()
        {
            var stateNegativeMax = BossHealthBarRules.Calculate(100.0, -100.0, 4);
            Assert.AreEqual(0, stateNegativeMax.RemainingBars);

            var stateZeroBars = BossHealthBarRules.Calculate(50.0, 100.0, 0);
            Assert.AreEqual(1, stateZeroBars.TotalBars, "Số thanh tối thiểu phải là 1");
            Assert.AreEqual(1, stateZeroBars.RemainingBars);
            Assert.AreEqual(0.5f, stateZeroBars.CurrentBarFill, 1e-3f);
        }

        [Test]
        public void GetBarColor_FinalBarIsRed_OtherBarsDistinct()
        {
            Color bar4 = BossHealthBarRules.GetBarColor(4);
            Color bar3 = BossHealthBarRules.GetBarColor(3);
            Color bar2 = BossHealthBarRules.GetBarColor(2);
            Color bar1 = BossHealthBarRules.GetBarColor(1);

            // Thanh 1 (cuối cùng) phải có sắc đỏ chiếm ưu thế
            Assert.Greater(bar1.r, bar1.g);
            Assert.Greater(bar1.r, bar1.b);

            // 4 màu phải khác nhau
            Assert.AreNotEqual(bar4, bar3);
            Assert.AreNotEqual(bar3, bar2);
            Assert.AreNotEqual(bar2, bar1);
        }

        [Test]
        public void GetUnderlyingBarColor_FinalBarReturnsDarkBackground()
        {
            Color underFinal = BossHealthBarRules.GetUnderlyingBarColor(1);
            Color underBar2 = BossHealthBarRules.GetUnderlyingBarColor(2);

            Assert.AreEqual(BossHealthBarRules.GetBarColor(1), underBar2, "Dưới thanh 2 phải là màu thanh 1.");
            Assert.Less(underFinal.r, 0.3f, "Nền dưới thanh cuối phải là nền xám tối.");
        }

        [Test]
        public void Multiplier_CanBeTemporarilyChangedAndRestored()
        {
            double original = BossHealthBarRules.BossHealthMultiplier;
            try
            {
                BossHealthBarRules.BossHealthMultiplier = 100.0;
                Assert.AreEqual(100.0, BossHealthBarRules.BossHealthMultiplier, 1e-6);
            }
            finally
            {
                BossHealthBarRules.BossHealthMultiplier = original;
            }
        }

        [Test]
        public void BossController_RuntimeMaxHealth_AppliesBossHealthMultiplier()
        {
            var go = new GameObject("TestBoss", typeof(Rigidbody2D), typeof(Game.PvE.Boss.BossController));
            var boss = go.GetComponent<Game.PvE.Boss.BossController>();
            var def = ScriptableObject.CreateInstance<Game.PvE.Boss.BossDefinition>();

            typeof(Game.PvE.Boss.BossDefinition).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, 1500.0);
            typeof(Game.PvE.Boss.BossController).GetField("definition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(boss, def);

            try
            {
                Assert.AreEqual(1500.0 * BossHealthBarRules.BossHealthMultiplier, boss.MaxHealth, 1e-4);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void BossController_EnteringFinalBar_TriggersEnrage()
        {
            var go = new GameObject("TestBoss", typeof(Rigidbody2D), typeof(Game.PvE.Boss.BossController));
            var boss = go.GetComponent<Game.PvE.Boss.BossController>();
            var def = ScriptableObject.CreateInstance<Game.PvE.Boss.BossDefinition>();

            typeof(Game.PvE.Boss.BossDefinition).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, 100.0);
            typeof(Game.PvE.Boss.BossDefinition).GetField("healthBarCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, 4);
            typeof(Game.PvE.Boss.BossController).GetField("definition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(boss, def);

            // Gọi ResetHealth để khởi tạo máu đầy
            typeof(Game.PvE.Boss.BossController).GetMethod("ResetHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(boss, null);

            try
            {
                Assert.IsFalse(boss.IsEnraged, "Máu đầy chưa được enrage.");

                // MaxHealth = 100 * 10 = 1000. 4 bars -> mỗi bar 250 HP.
                // Đánh 700 dmg -> còn 300 HP (vẫn ở bar 2, chưa vào bar 1 cuối)
                boss.TakeDamage(new Game.Core.Combat.DamageInfo(700.0));
                Assert.IsFalse(boss.IsEnraged, "Còn 300/1000 HP (bar 2) chưa vào bar cuối.");

                // Đánh thêm 60 dmg -> còn 240 HP (đã vào bar 1 cuối: <= 250 HP)
                boss.TakeDamage(new Game.Core.Combat.DamageInfo(60.0));
                Assert.IsTrue(boss.IsEnraged, "Vào thanh máu cuối phải kích hoạt Cuồng nộ (Enrage)!");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(def);
            }
        }
    }
}
