using Game.Core.Items;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class DurabilityUpgradeRulesTests
    {
        // ── Độ bền ──

        [Test]
        public void Resolve_UnsetMeansFull_AndNoWearItemsAreZero()
        {
            Assert.AreEqual(100, DurabilityRules.Resolve(DurabilityRules.UnsetDurability, 100));
            Assert.AreEqual(40, DurabilityRules.Resolve(40, 100));
            Assert.AreEqual(100, DurabilityRules.Resolve(500, 100));
            Assert.AreEqual(0, DurabilityRules.Resolve(-1, 0));
        }

        [Test]
        public void ApplyWear_NeverBelowZero_AndKeepsNoWearItems()
        {
            Assert.AreEqual(99, DurabilityRules.ApplyWear(-1, 100, 1));
            Assert.AreEqual(0, DurabilityRules.ApplyWear(1, 100, 5));
            Assert.AreEqual(7, DurabilityRules.ApplyWear(7, 100, 0));
            Assert.AreEqual(-1, DurabilityRules.ApplyWear(-1, 0, 1));
        }

        [Test]
        public void BrokenAndLow_FlagsAreCorrect()
        {
            Assert.IsFalse(DurabilityRules.IsBroken(-1, 100));
            Assert.IsTrue(DurabilityRules.IsBroken(0, 100));
            Assert.IsFalse(DurabilityRules.IsBroken(0, 0)); // món không hao mòn không bao giờ hỏng

            Assert.IsFalse(DurabilityRules.IsLow(50, 100));
            Assert.IsTrue(DurabilityRules.IsLow(20, 100));
            Assert.IsTrue(DurabilityRules.IsLow(1, 100));
            Assert.IsFalse(DurabilityRules.IsLow(0, 100)); // hỏng thì không tính "sắp hỏng"
        }

        [Test]
        public void RepairCost_ScalesWithMissingAndUpgrade_WithMinimums()
        {
            Assert.AreEqual(0, DurabilityRules.RepairCost(100, 100, 200, 0)); // đầy bền → miễn phí
            Assert.AreEqual(0, DurabilityRules.RepairCost(0, 0, 200, 0));

            int full = DurabilityRules.FullRepairCost(200, 0);
            Assert.AreEqual(100, full); // 200 × 0.5
            Assert.AreEqual(full, DurabilityRules.RepairCost(0, 100, 200, 0));
            Assert.AreEqual(50, DurabilityRules.RepairCost(50, 100, 200, 0));

            Assert.Greater(DurabilityRules.FullRepairCost(200, 5), DurabilityRules.FullRepairCost(200, 0));
            Assert.AreEqual(DurabilityRules.MinFullRepairCost, DurabilityRules.FullRepairCost(1, 0));
            Assert.AreEqual(1, DurabilityRules.RepairCost(99, 100, 1, 0)); // thiếu 1 điểm vẫn tốn ít nhất 1 Col
        }

        [Test]
        public void Label_ShowsCurrentOverMax()
        {
            Assert.AreEqual("100/100", DurabilityRules.Label(-1, 100));
            Assert.AreEqual("37/100", DurabilityRules.Label(37, 100));
            Assert.AreEqual(string.Empty, DurabilityRules.Label(10, 0));
        }

        [Test]
        public void ItemInstance_DefaultDurabilityIsUnset()
        {
            var instance = new ItemInstance("i1", "item.sword", default, 0);
            Assert.AreEqual(DurabilityRules.UnsetDurability, instance.durability);
        }

        // ── Cường hóa ──

        [Test]
        public void SuccessChance_DecreasesWithLevel_AndZeroAtMax()
        {
            Assert.AreEqual(1.0, UpgradeRules.SuccessChance(0), 1e-9);
            double previous = 1.0;
            for (int level = 1; level < UpgradeRules.MaxLevel; level++)
            {
                double chance = UpgradeRules.SuccessChance(level);
                Assert.LessOrEqual(chance, previous, "Tỉ lệ phải không tăng ở level " + level);
                Assert.Greater(chance, 0.0);
                previous = chance;
            }
            Assert.AreEqual(0.0, UpgradeRules.SuccessChance(UpgradeRules.MaxLevel));
            Assert.AreEqual(0.0, UpgradeRules.SuccessChance(-1));
        }

        [Test]
        public void Cost_ScalesWithTierAndLevel_AndIsZeroAtMax()
        {
            Assert.AreEqual(50, UpgradeRules.Cost(1, 0));
            Assert.AreEqual(100, UpgradeRules.Cost(1, 1));
            Assert.AreEqual(150, UpgradeRules.Cost(3, 0));
            Assert.AreEqual(50, UpgradeRules.Cost(0, 0)); // tier < 1 tính là 1
            Assert.AreEqual(0, UpgradeRules.Cost(2, UpgradeRules.MaxLevel));
        }

        [Test]
        public void BonusStats_ArePercentOfBase_AndZeroAtLevelZero()
        {
            var baseStats = new ItemStats(10, 5, 100);
            Assert.IsTrue(UpgradeRules.BonusStats(baseStats, 0).IsZero);

            ItemStats bonus = UpgradeRules.BonusStats(baseStats, 5); // 5 × 8% = +40%
            Assert.AreEqual(4.0, bonus.attack, 1e-9);
            Assert.AreEqual(2.0, bonus.defense, 1e-9);
            Assert.AreEqual(40.0, bonus.maxHealth, 1e-9);

            ItemStats capped = UpgradeRules.BonusStats(baseStats, 999); // cắt ở MaxLevel
            Assert.AreEqual(10 * UpgradeRules.BonusPerLevel * UpgradeRules.MaxLevel, capped.attack, 1e-9);
        }

        [Test]
        public void Resolve_SuccessWhenRollBelowChance()
        {
            UpgradeOutcome outcome = UpgradeRules.Resolve(2, 0.5, out int newLevel); // chance 0.90
            Assert.AreEqual(UpgradeOutcome.Success, outcome);
            Assert.AreEqual(3, newLevel);
        }

        [Test]
        public void Resolve_LowLevelFailureKeepsLevel()
        {
            UpgradeOutcome outcome = UpgradeRules.Resolve(3, 0.95, out int newLevel); // chance 0.80
            Assert.AreEqual(UpgradeOutcome.Failed, outcome);
            Assert.AreEqual(3, newLevel);
        }

        [Test]
        public void Resolve_HighLevelFailureDowngradesOne()
        {
            UpgradeOutcome outcome = UpgradeRules.Resolve(UpgradeRules.DowngradeFromLevel, 0.99, out int newLevel);
            Assert.AreEqual(UpgradeOutcome.FailedDowngraded, outcome);
            Assert.AreEqual(UpgradeRules.DowngradeFromLevel - 1, newLevel);
        }

        [Test]
        public void Resolve_AtMaxLevel_DoesNothing()
        {
            UpgradeOutcome outcome = UpgradeRules.Resolve(UpgradeRules.MaxLevel, 0.0, out int newLevel);
            Assert.AreEqual(UpgradeOutcome.Failed, outcome);
            Assert.AreEqual(UpgradeRules.MaxLevel, newLevel);
        }

        [Test]
        public void Resolve_FirstUpgradeAlwaysSucceeds()
        {
            // Chance cấp 0 = 100% → mọi roll trong [0,1) đều thành công.
            Assert.AreEqual(UpgradeOutcome.Success, UpgradeRules.Resolve(0, 0.999999, out int level));
            Assert.AreEqual(1, level);
        }

        [Test]
        public void Label_ShowsPlusLevelOnlyWhenUpgraded()
        {
            Assert.AreEqual(string.Empty, UpgradeRules.Label(0));
            Assert.AreEqual("+4", UpgradeRules.Label(4));
        }
    }
}
