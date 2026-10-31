using Game.Core.Stats;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class StatAllocationTests
    {
        [Test]
        public void TotalPoints_Level1IsZero_ThenThreePerLevel()
        {
            Assert.AreEqual(0, StatAllocation.TotalPointsForLevel(1));
            Assert.AreEqual(0, StatAllocation.TotalPointsForLevel(0));
            Assert.AreEqual(21, StatAllocation.TotalPointsForLevel(8));
        }

        [Test]
        public void Unspent_IsTotalMinusSpent()
        {
            var a = new StatAllocation();
            Assert.AreEqual(21, a.Unspent(8));
            Assert.IsTrue(a.TryAllocate(AllocatableStat.Strength, 8, 5));
            Assert.IsTrue(a.TryAllocate(AllocatableStat.Agility, 8, 2));
            Assert.AreEqual(14, a.Unspent(8));
            Assert.AreEqual(5, a.Strength);
            Assert.AreEqual(2, a.Agility);
        }

        [Test]
        public void TryAllocate_FailsWithoutEnoughPoints_AndChangesNothing()
        {
            var a = new StatAllocation();
            Assert.IsFalse(a.TryAllocate(AllocatableStat.Strength, 1)); // level 1: 0 điểm
            Assert.IsTrue(a.TryAllocate(AllocatableStat.Strength, 2, 3));
            Assert.IsFalse(a.TryAllocate(AllocatableStat.Agility, 2)); // hết điểm
            Assert.IsFalse(a.TryAllocate(AllocatableStat.Agility, 5, 0));
            Assert.IsFalse(a.TryAllocate(AllocatableStat.Agility, 5, -1));
            Assert.AreEqual(3, a.Strength);
            Assert.AreEqual(0, a.Agility);
        }

        [Test]
        public void TryAllocate_RaisesChangedOnlyOnSuccess()
        {
            var a = new StatAllocation();
            int count = 0;
            a.Changed += () => count++;
            a.TryAllocate(AllocatableStat.Strength, 1);
            Assert.AreEqual(0, count);
            a.TryAllocate(AllocatableStat.Strength, 2);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void LoadFrom_ClampsNegativeAndOverBudget_AgilityTrimmedFirst()
        {
            var a = new StatAllocation();

            a.LoadFrom(-5, -1, 10);
            Assert.AreEqual(0, a.Strength);
            Assert.AreEqual(0, a.Agility);

            a.LoadFrom(20, 20, 8); // ngân sách 21
            Assert.AreEqual(20, a.Strength);
            Assert.AreEqual(1, a.Agility);

            a.LoadFrom(99, 5, 8);
            Assert.AreEqual(21, a.Strength);
            Assert.AreEqual(0, a.Agility);
        }

        [Test]
        public void OldSave_ZeroZero_GetsFullPointsForLevel()
        {
            var a = new StatAllocation();
            a.LoadFrom(0, 0, 8);
            Assert.AreEqual(21, a.Unspent(8));
        }

        [Test]
        public void Clear_ResetsAllocatedPoints()
        {
            var a = new StatAllocation();
            a.TryAllocate(AllocatableStat.Agility, 5, 4);
            a.Clear();
            Assert.AreEqual(0, a.Spent);
            Assert.AreEqual(12, a.Unspent(5));
        }

        [Test]
        public void Bonuses_ScaleLinearly_AndAreCapped()
        {
            Assert.AreEqual(0.0, StatAllocation.AttackBonus(0));
            Assert.AreEqual(0.0, StatAllocation.AttackBonus(-3));
            Assert.AreEqual(7.0, StatAllocation.AttackBonus(7), 1e-9);

            Assert.AreEqual(0.04, StatAllocation.MoveSpeedFractionBonus(10), 1e-9);
            Assert.AreEqual(0.03, StatAllocation.CritChanceBonus(10), 1e-9);

            Assert.AreEqual(StatAllocation.MaxMoveSpeedFraction, StatAllocation.MoveSpeedFractionBonus(100000), 1e-9);
            Assert.AreEqual(StatAllocation.MaxCritChanceBonus, StatAllocation.CritChanceBonus(100000), 1e-9);
        }

        [Test]
        public void CharacterStats_AppliesAllocationBonus_AndKeepsItAcrossLevelChange()
        {
            var stats = new CharacterStats(1);
            double baseAttack = stats.Attack;
            double baseCrit = stats.CriticalChance;
            double baseSpeed = stats.MoveSpeed;

            stats.SetAllocationBonus(10.0, 0.2, 0.05);
            Assert.AreEqual(baseAttack + 10.0, stats.Attack, 1e-9);
            Assert.AreEqual(baseCrit + 0.05, stats.CriticalChance, 1e-9);
            Assert.AreEqual(baseSpeed * 1.2, stats.MoveSpeed, 1e-9);
            Assert.AreEqual(0.2, stats.MoveSpeedBonusFraction, 1e-9);

            stats.SetLevel(5);
            Assert.AreEqual(10 + 4 * 2 + 10.0, stats.Attack, 1e-9);
            Assert.AreEqual(0.2, stats.MoveSpeedBonusFraction, 1e-9);

            stats.SetAllocationBonus(0.0, 0.0, 0.0);
            Assert.AreEqual(10 + 4 * 2, stats.Attack, 1e-9);
            Assert.AreEqual(baseSpeed, stats.MoveSpeed, 1e-9);
        }

        [Test]
        public void CharacterStats_AllocationStacksWithEquipmentBonus()
        {
            var stats = new CharacterStats(1);
            double baseAttack = stats.Attack;
            stats.SetEquipmentBonus(5.0, 0.0, 0.0);
            stats.SetAllocationBonus(3.0, 0.0, 0.0);
            Assert.AreEqual(baseAttack + 5.0 + 3.0, stats.Attack, 1e-9);
        }
    }
}
