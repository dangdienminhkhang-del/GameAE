using System.Collections.Generic;
using Game.Core.Combat;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Phase 3 — ghép chỉ số + AttackDefinition thành DamageInfo và áp lên mục tiêu.</summary>
    public class AttackExecutorTests
    {
        private AttackDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            // Asset mặc định: multiplier 1.0, crit -1 (dùng chỉ số nhân vật)
            _definition = AttackDefinition.CreateDefault();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_definition);
        }

        [Test]
        public void ResolveDamage_NoCrit_UsesAttackerStatsAndDefenderDefense()
        {
            var attacker = new FakeStats(attack: 10.0, defense: 0.0, critChance: 0.1, critDamage: 1.5);

            DamageInfo damage = AttackExecutor.ResolveDamage(attacker, 5.0, _definition, criticalRoll: 0.9);

            Assert.IsFalse(damage.IsCritical);
            Assert.AreEqual(5.0, damage.Amount, 0.0001);
        }

        [Test]
        public void ResolveDamage_CritRollBelowChance_AppliesCriticalMultiplier()
        {
            var attacker = new FakeStats(attack: 10.0, defense: 0.0, critChance: 0.1, critDamage: 1.5);

            DamageInfo damage = AttackExecutor.ResolveDamage(attacker, 5.0, _definition, criticalRoll: 0.05);

            Assert.IsTrue(damage.IsCritical);
            Assert.AreEqual(7.5, damage.Amount, 0.0001);
            Assert.AreEqual(1.5, damage.CriticalMultiplier, 0.0001);
        }

        [Test]
        public void ResolveDamage_KeepsSourceId()
        {
            var attacker = new FakeStats(10.0, 0.0, 0.0, 1.5);

            DamageInfo damage = AttackExecutor.ResolveDamage(attacker, 0.0, _definition, 0.9, sourceId: "Player");

            Assert.AreEqual("Player", damage.SourceId);
        }

        [Test]
        public void ResolveCriticalChance_IsClampedToUnitRange()
        {
            var attacker = new FakeStats(10.0, 0.0, 2.0, 1.5);

            Assert.AreEqual(1.0, AttackExecutor.ResolveCriticalChance(attacker, _definition), 0.0001);
        }

        [Test]
        public void ResolveDefense_TargetWithoutStats_IsZero()
        {
            Assert.AreEqual(0.0, AttackExecutor.ResolveDefense(new FakeTarget(100.0)), 0.0001);
        }

        [Test]
        public void ResolveDefense_TargetWithStats_UsesDefense()
        {
            Assert.AreEqual(5.0, AttackExecutor.ResolveDefense(new FakeStatsTarget(100.0, 5.0)), 0.0001);
        }

        [Test]
        public void ApplyDamage_HitsAliveTargetsAndSkipsDeadOnes()
        {
            var alive = new FakeTarget(100.0);
            var dead = new FakeTarget(100.0, startDead: true);
            var targets = new List<IDamageable> { alive, dead, null };

            int hits = AttackExecutor.ApplyDamage(targets, new DamageInfo(7.0));

            Assert.AreEqual(1, hits);
            Assert.AreEqual(7.0, alive.LastDamage, 0.0001);
            Assert.AreEqual(0.0, dead.LastDamage, 0.0001);
        }

        private class FakeStats : ICombatStats
        {
            public FakeStats(double attack, double defense, double critChance, double critDamage)
            {
                Attack = attack;
                Defense = defense;
                CriticalChance = critChance;
                CriticalDamage = critDamage;
            }

            public double Attack { get; }
            public double Defense { get; }
            public double CriticalChance { get; }
            public double CriticalDamage { get; }
        }

        private class FakeTarget : IDamageable
        {
            private readonly double _maxHealth;
            private double _currentHealth;

            public FakeTarget(double maxHealth, bool startDead = false)
            {
                _maxHealth = maxHealth;
                _currentHealth = startDead ? 0.0 : maxHealth;
            }

            public double LastDamage { get; private set; }
            public double CurrentHealth => _currentHealth;
            public double MaxHealth => _maxHealth;
            public bool IsDead => _currentHealth <= 0.0;

            public virtual void TakeDamage(DamageInfo damage)
            {
                if (IsDead) return;
                LastDamage = damage.Amount;
                _currentHealth = System.Math.Max(0.0, _currentHealth - damage.Amount);
            }

            public void Heal(double amount)
            {
                _currentHealth = System.Math.Min(_maxHealth, _currentHealth + amount);
            }

            public void SetMaxHealth(double maxHp, bool resetCurrent = false)
            {
                if (resetCurrent) _currentHealth = maxHp;
            }
        }

        private class FakeStatsTarget : FakeTarget, ICombatStats
        {
            public FakeStatsTarget(double maxHealth, double defense) : base(maxHealth)
            {
                Defense = defense;
            }

            public double Attack => 0.0;
            public double Defense { get; }
            public double CriticalChance => 0.0;
            public double CriticalDamage => 1.0;
        }
    }
}
