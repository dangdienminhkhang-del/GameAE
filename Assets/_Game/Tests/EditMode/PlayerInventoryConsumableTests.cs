using System.Collections.Generic;
using System.Reflection;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Items;
using Game.Core.Stats;
using Game.PvE.Items;
using Game.PvE.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task P (phần 1) — PlayerInventory.TryUseConsumable: trừ 1, hồi không vượt max, không dùng khi giao chiến, không hồi sinh.
    /// MonoBehaviour không tự chạy Awake/OnEnable trong EditMode → test tự gọi OnEnable/OnDisable (nối/gỡ sự kiện) và gắn field private.
    /// </summary>
    public class PlayerInventoryConsumableTests
    {
        private const string PotionId = "item.health_potion";

        private GameObject _go;
        private PlayerInventory _inventory;
        private HealthComponent _health;
        private ItemDatabase _database;
        private ItemDefinition _potion;
        private ItemDefinition _sword;

        [SetUp]
        public void SetUp()
        {
            _potion = ConsumableRulesTests.MakeDefinition(PotionId, ItemType.Consumable, heal: 40.0, cooldown: 5f);
            _sword = ConsumableRulesTests.MakeDefinition("item.sword", ItemType.Weapon, heal: 0.0, cooldown: 0f);

            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            ConsumableRulesTests.SetField(_database, "items", new List<ItemDefinition> { _potion, _sword });

            _go = new GameObject("TestPlayer");
            _health = _go.AddComponent<HealthComponent>();
            _inventory = _go.AddComponent<PlayerInventory>();

            ConsumableRulesTests.SetField(_inventory, "database", _database);
            ConsumableRulesTests.SetField(_inventory, "loadOnStart", false);

            // HealthComponent.Start không chạy trong EditMode → gắn tay: max 100 (level 1), đang 60.
            ConsumableRulesTests.SetField(_health, "_stats", new CharacterStats(1));
            SetHealth(60.0);

            // Phòng trường hợp Unity đã tự gọi OnEnable: gỡ trước rồi nối lại đúng 1 lần.
            InvokeLifecycle("OnDisable");
            InvokeLifecycle("OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            InvokeLifecycle("OnDisable"); // gỡ sự kiện static GameEvents, khỏi rò sang test khác
            if (_go != null) Object.DestroyImmediate(_go);
            if (_database != null) Object.DestroyImmediate(_database);
            if (_potion != null) Object.DestroyImmediate(_potion);
            if (_sword != null) Object.DestroyImmediate(_sword);
        }

        [Test]
        public void Use_ConsumesOne_AndHealsFixedAmount()
        {
            _inventory.Inventory.AddStack(PotionId, 3, 99);
            SetHealth(30.0);

            ConsumableUseResult result = _inventory.TryUseConsumable(PotionId);

            Assert.AreEqual(ConsumableUseResult.Ok, result);
            Assert.AreEqual(70.0, _health.CurrentHealth, 0.0001, "30 + 40");
            Assert.AreEqual(2, _inventory.Inventory.GetStack(PotionId), "trừ đúng 1 bình");
        }

        [Test]
        public void Use_HealNeverExceedsMax()
        {
            _inventory.Inventory.AddStack(PotionId, 1, 99);
            SetHealth(90.0);

            Assert.AreEqual(ConsumableUseResult.Ok, _inventory.TryUseConsumable(PotionId));

            Assert.AreEqual(100.0, _health.CurrentHealth, 0.0001, "90 + 40 bị chặn ở max 100");
            Assert.AreEqual(0, _inventory.Inventory.GetStack(PotionId));
        }

        [Test]
        public void Use_FullHealth_DoesNotConsume()
        {
            _inventory.Inventory.AddStack(PotionId, 2, 99);
            SetHealth(100.0);

            Assert.AreEqual(ConsumableUseResult.FullHealth, _inventory.TryUseConsumable(PotionId));
            Assert.AreEqual(2, _inventory.Inventory.GetStack(PotionId), "máu đầy thì không phí bình");
        }

        [Test]
        public void Use_AfterBeingHit_IsBlockedAsInCombat_ThenAllowedAfterLockSeconds()
        {
            _inventory.Inventory.AddStack(PotionId, 2, 99);

            // Bị đánh: HealthComponent phát OnHealthChanged với Delta < 0.
            GameEvents.RaiseHealthChanged(_health, new HealthChangedArgs { Current = 50.0, Max = 100.0, Delta = -10.0 });

            Assert.AreEqual(ConsumableUseResult.InCombat, _inventory.TryUseConsumable(PotionId));
            Assert.AreEqual(2, _inventory.Inventory.GetStack(PotionId), "đang giao chiến thì không trừ bình");
            Assert.AreEqual(60.0, _health.CurrentHealth, 0.0001, "đang giao chiến thì không hồi");

            // Giả lập "đã qua hơn 5 giây" bằng cách đẩy mốc giao chiến về quá khứ.
            _inventory.UseClock.MarkCombat(UnityEngine.Time.time - (ConsumableRules.DefaultCombatLockSeconds + 1f));

            Assert.AreEqual(ConsumableUseResult.Ok, _inventory.TryUseConsumable(PotionId));
        }

        [Test]
        public void Healing_DoesNotCountAsCombat()
        {
            _inventory.Inventory.AddStack(PotionId, 2, 99);
            SetHealth(30.0);

            Assert.AreEqual(ConsumableUseResult.Ok, _inventory.TryUseConsumable(PotionId));

            // Lần dùng thứ hai bị chặn vì THỜI GIAN CHỜ, không phải vì "giao chiến" (sự kiện hồi máu có Delta > 0).
            Assert.AreEqual(ConsumableUseResult.OnCooldown, _inventory.GetConsumableUseState(PotionId));
        }

        [Test]
        public void Use_AfterKillingEnemy_IsBlockedAsInCombat()
        {
            _inventory.Inventory.AddStack(PotionId, 1, 99);

            GameEvents.RaiseEnemyDefeated(this);

            Assert.AreEqual(ConsumableUseResult.InCombat, _inventory.TryUseConsumable(PotionId));
            Assert.AreEqual(1, _inventory.Inventory.GetStack(PotionId));
        }

        [Test]
        public void Use_AfterHittingEnemy_IsBlockedAsInCombat()
        {
            // ACR-1: đánh trúng quái CHƯA chết cũng là giao chiến.
            _inventory.Inventory.AddStack(PotionId, 1, 99);
            var ownStats = GiveInventoryOwnStats();
            var enemy = new FakeEnemy(50.0);

            bool raised = AttackExecutor.ApplyHit(enemy, new DamageInfo(5.0) { Source = ownStats });

            Assert.IsTrue(raised);
            Assert.IsFalse(enemy.IsDead, "quái vẫn còn sống sau hit");
            Assert.AreEqual(ConsumableUseResult.InCombat, _inventory.TryUseConsumable(PotionId));
            Assert.AreEqual(1, _inventory.Inventory.GetStack(PotionId), "đang giao chiến thì không trừ bình");
        }

        [Test]
        public void Use_AfterHitOnDeadTarget_IsNotCombat()
        {
            _inventory.Inventory.AddStack(PotionId, 1, 99);
            var ownStats = GiveInventoryOwnStats();
            var deadEnemy = new FakeEnemy(50.0, startDead: true);

            bool raised = AttackExecutor.ApplyHit(deadEnemy, new DamageInfo(5.0) { Source = ownStats });

            Assert.IsFalse(raised, "mục tiêu đã chết thì không phát HitResolved");
            Assert.AreEqual(ConsumableUseResult.Ok, _inventory.TryUseConsumable(PotionId));
        }

        [Test]
        public void Use_AfterHitFromAnotherSource_IsNotCombat()
        {
            // Hit của người/nguồn khác không được đánh dấu giao chiến cho player này.
            _inventory.Inventory.AddStack(PotionId, 1, 99);
            GiveInventoryOwnStats();
            var someoneElse = new CharacterStats(1);

            Assert.IsTrue(AttackExecutor.ApplyHit(new FakeEnemy(50.0), new DamageInfo(5.0) { Source = someoneElse }));
            Assert.IsTrue(AttackExecutor.ApplyHit(new FakeEnemy(50.0), new DamageInfo(5.0) { Source = null }));

            Assert.AreEqual(ConsumableUseResult.Ok, _inventory.TryUseConsumable(PotionId));
        }

        [Test]
        public void Use_Twice_SecondIsOnCooldown_ThenAllowedAfterCooldown()
        {
            _inventory.Inventory.AddStack(PotionId, 3, 99);
            SetHealth(10.0);

            Assert.AreEqual(ConsumableUseResult.Ok, _inventory.TryUseConsumable(PotionId));
            Assert.AreEqual(ConsumableUseResult.OnCooldown, _inventory.TryUseConsumable(PotionId));
            Assert.AreEqual(2, _inventory.Inventory.GetStack(PotionId), "lần bị chặn không trừ bình");

            _inventory.UseClock.MarkUsed(PotionId, UnityEngine.Time.time - 6f); // giả lập đã qua 6 giây
            Assert.AreEqual(ConsumableUseResult.Ok, _inventory.TryUseConsumable(PotionId));
            Assert.AreEqual(1, _inventory.Inventory.GetStack(PotionId));
        }

        [Test]
        public void Use_OutOfStock()
        {
            Assert.AreEqual(ConsumableUseResult.OutOfStock, _inventory.TryUseConsumable(PotionId));
            Assert.AreEqual(60.0, _health.CurrentHealth, 0.0001);
        }

        [Test]
        public void Use_WhenDead_NeverRevives()
        {
            _inventory.Inventory.AddStack(PotionId, 2, 99);
            SetHealth(0.0);

            Assert.AreEqual(ConsumableUseResult.PlayerDead, _inventory.TryUseConsumable(PotionId));

            Assert.AreEqual(0.0, _health.CurrentHealth, 0.0001, "bình máu không được hồi sinh");
            Assert.IsTrue(_health.IsDead);
            Assert.AreEqual(2, _inventory.Inventory.GetStack(PotionId));
        }

        [Test]
        public void Use_NonConsumableOrUnknownItem_DoesNothing()
        {
            _inventory.Inventory.AddStack("item.sword", 1, 99);

            Assert.AreEqual(ConsumableUseResult.NotConsumable, _inventory.TryUseConsumable("item.sword"));
            Assert.AreEqual(ConsumableUseResult.NotConsumable, _inventory.TryUseConsumable("item.khong_ton_tai"));
            Assert.AreEqual(ConsumableUseResult.NotConsumable, _inventory.TryUseConsumable(null));
            Assert.AreEqual(1, _inventory.Inventory.GetStack("item.sword"));
            Assert.AreEqual(60.0, _health.CurrentHealth, 0.0001);
        }

        [Test]
        public void GetConsumableUseState_HasNoSideEffects()
        {
            _inventory.Inventory.AddStack(PotionId, 2, 99);

            Assert.AreEqual(ConsumableUseResult.Ok, _inventory.GetConsumableUseState(PotionId));
            Assert.AreEqual(ConsumableUseResult.Ok, _inventory.GetConsumableUseState(PotionId), "hỏi nhiều lần vẫn Ok — chưa dùng thật");
            Assert.AreEqual(2, _inventory.Inventory.GetStack(PotionId));
            Assert.AreEqual(60.0, _health.CurrentHealth, 0.0001);
        }

        // ── Helper ──

        /// <summary>PlayerInventory.ResolveStats lấy từ PlayerController (không có trong EditMode) → gắn tay CharacterStats của player.</summary>
        private CharacterStats GiveInventoryOwnStats()
        {
            var stats = new CharacterStats(1);
            ConsumableRulesTests.SetField(_inventory, "_stats", stats);
            return stats;
        }

        private sealed class FakeEnemy : IDamageable
        {
            private double _current;

            public FakeEnemy(double maxHealth, bool startDead = false)
            {
                MaxHealth = maxHealth;
                _current = startDead ? 0.0 : maxHealth;
            }

            public double CurrentHealth => _current;
            public double MaxHealth { get; }
            public bool IsDead => _current <= 0.0;

            public void TakeDamage(DamageInfo damage)
            {
                if (IsDead) return;
                _current = System.Math.Max(0.0, _current - damage.Amount);
            }

            public void Heal(double amount) { }
            public void SetMaxHealth(double maxHp, bool resetCurrent = false) { }
        }

        private void SetHealth(double value)
        {
            ConsumableRulesTests.SetField(_health, "_currentHealth", value);
        }

        private void InvokeLifecycle(string methodName)
        {
            if (_inventory == null) return;
            MethodInfo method = typeof(PlayerInventory).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"PlayerInventory.{methodName} không tồn tại");
            method.Invoke(_inventory, null);
        }
    }
}
