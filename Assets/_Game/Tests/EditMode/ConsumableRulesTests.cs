using System.Reflection;
using Game.Core.Items;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Task P (phần 1) — luật dùng bình máu: docs/07 §2 (ngoài giao chiến, không hồi sinh, có thời gian chờ).</summary>
    public class ConsumableRulesTests
    {
        private const float Quiet = 100f;               // đã yên tĩnh rất lâu
        private const float NeverUsed = float.PositiveInfinity;

        private ItemDefinition _potion;

        [SetUp]
        public void SetUp()
        {
            _potion = MakeDefinition("item.health_potion", ItemType.Consumable, heal: 40.0, cooldown: 5f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_potion != null) Object.DestroyImmediate(_potion);
        }

        // ── Đủ 6 kết quả (+ PlayerDead cho luật "không hồi sinh") ──

        [Test]
        public void CanUse_Ok_WhenHurt_Quiet_NotOnCooldown_InStock()
        {
            Assert.AreEqual(ConsumableUseResult.Ok,
                ConsumableRules.CanUse(_potion, 60, 100, Quiet, NeverUsed, 3));
        }

        [Test]
        public void CanUse_NotConsumable_ForNullWeaponOrZeroHeal()
        {
            ItemDefinition sword = MakeDefinition("item.sword", ItemType.Weapon, heal: 40.0, cooldown: 5f);
            ItemDefinition noHeal = MakeDefinition("item.stew", ItemType.Consumable, heal: 0.0, cooldown: 5f);
            try
            {
                Assert.AreEqual(ConsumableUseResult.NotConsumable, ConsumableRules.CanUse(null, 60, 100, Quiet, NeverUsed, 3));
                Assert.AreEqual(ConsumableUseResult.NotConsumable, ConsumableRules.CanUse(sword, 60, 100, Quiet, NeverUsed, 3),
                    "chỉ ItemType.Consumable mới dùng được, dù có điền healAmount");
                Assert.AreEqual(ConsumableUseResult.NotConsumable, ConsumableRules.CanUse(noHeal, 60, 100, Quiet, NeverUsed, 3),
                    "Consumable không có healAmount thì chưa có tác dụng");
            }
            finally
            {
                Object.DestroyImmediate(sword);
                Object.DestroyImmediate(noHeal);
            }
        }

        [Test]
        public void CanUse_FullHealth_WhenAtMax()
        {
            Assert.AreEqual(ConsumableUseResult.FullHealth, ConsumableRules.CanUse(_potion, 100, 100, Quiet, NeverUsed, 3));
        }

        [Test]
        public void CanUse_InCombat_WhenLessThanLockSeconds()
        {
            Assert.AreEqual(ConsumableUseResult.InCombat, ConsumableRules.CanUse(_potion, 60, 100, 2f, NeverUsed, 3));
        }

        [Test]
        public void CanUse_OnCooldown_WhenUsedRecently()
        {
            Assert.AreEqual(ConsumableUseResult.OnCooldown, ConsumableRules.CanUse(_potion, 60, 100, Quiet, 2f, 3));
        }

        [Test]
        public void CanUse_OutOfStock_WhenQuantityZero()
        {
            Assert.AreEqual(ConsumableUseResult.OutOfStock, ConsumableRules.CanUse(_potion, 60, 100, Quiet, NeverUsed, 0));
        }

        [Test]
        public void CanUse_PlayerDead_NeverRevives()
        {
            Assert.AreEqual(ConsumableUseResult.PlayerDead, ConsumableRules.CanUse(_potion, 0, 100, Quiet, NeverUsed, 3));
            Assert.AreEqual(ConsumableUseResult.PlayerDead, ConsumableRules.CanUse(_potion, -5, 100, Quiet, NeverUsed, 3));
        }

        // ── Ranh giới + tham số ──

        [Test]
        public void CanUse_ExactlyAtLockSeconds_IsAllowed()
        {
            Assert.AreEqual(ConsumableUseResult.Ok,
                ConsumableRules.CanUse(_potion, 60, 100, ConsumableRules.DefaultCombatLockSeconds, NeverUsed, 3),
                "đủ 5 giây thì được dùng (< 5 mới là đang giao chiến)");
            Assert.AreEqual(ConsumableUseResult.InCombat,
                ConsumableRules.CanUse(_potion, 60, 100, ConsumableRules.DefaultCombatLockSeconds - 0.01f, NeverUsed, 3));
        }

        [Test]
        public void CanUse_ExactlyAtCooldown_IsAllowed()
        {
            Assert.AreEqual(ConsumableUseResult.Ok, ConsumableRules.CanUse(_potion, 60, 100, Quiet, 5f, 3));
            Assert.AreEqual(ConsumableUseResult.OnCooldown, ConsumableRules.CanUse(_potion, 60, 100, Quiet, 4.99f, 3));
        }

        [Test]
        public void CanUse_CombatLockSeconds_IsAParameter()
        {
            Assert.AreEqual(ConsumableUseResult.Ok, ConsumableRules.CanUse(_potion, 60, 100, 2f, NeverUsed, 3, combatLockSeconds: 1f));
            Assert.AreEqual(ConsumableUseResult.InCombat, ConsumableRules.CanUse(_potion, 60, 100, 8f, NeverUsed, 3, combatLockSeconds: 10f));
        }

        [Test]
        public void CanUse_FullHealthWinsOverCombat_SoReasonIsTruthful()
        {
            Assert.AreEqual(ConsumableUseResult.FullHealth, ConsumableRules.CanUse(_potion, 100, 100, 1f, 1f, 3));
        }

        [Test]
        public void ComputeHeal_NeverExceedsMax()
        {
            Assert.AreEqual(40.0, ConsumableRules.ComputeHeal(_potion, 30, 100), 0.0001);
            Assert.AreEqual(10.0, ConsumableRules.ComputeHeal(_potion, 90, 100), 0.0001, "chỉ hồi phần còn thiếu");
            Assert.AreEqual(0.0, ConsumableRules.ComputeHeal(_potion, 100, 100), 0.0001);
            Assert.AreEqual(0.0, ConsumableRules.ComputeHeal(null, 50, 100), 0.0001);
        }

        [Test]
        public void DescribeResult_OkIsEmpty_OthersHaveText()
        {
            Assert.IsEmpty(ConsumableRules.DescribeResult(ConsumableUseResult.Ok));
            foreach (ConsumableUseResult result in System.Enum.GetValues(typeof(ConsumableUseResult)))
            {
                if (result == ConsumableUseResult.Ok) continue;
                Assert.IsNotEmpty(ConsumableRules.DescribeResult(result), $"{result} phải có chữ lý do cho UI");
            }
            Assert.AreEqual("Đang giao chiến", ConsumableRules.DescribeResult(ConsumableUseResult.InCombat));
        }

        // ── ItemDefinition ──

        [Test]
        public void ItemDefinition_NegativeValuesAreClampedToZero()
        {
            ItemDefinition bad = MakeDefinition("item.bad", ItemType.Consumable, heal: -10.0, cooldown: -3f);
            try
            {
                Assert.AreEqual(0.0, bad.HealAmount, 0.0001);
                Assert.AreEqual(0f, bad.UseCooldownSeconds, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(bad);
            }
        }

        // ── ConsumableUseClock ──

        [Test]
        public void Clock_NeverMarked_IsInfinity()
        {
            var clock = new ConsumableUseClock();
            Assert.IsTrue(float.IsPositiveInfinity(clock.SecondsSinceCombat(10f)));
            Assert.IsTrue(float.IsPositiveInfinity(clock.SecondsSinceUse("item.health_potion", 10f)));
        }

        [Test]
        public void Clock_TracksCombatAndPerItemUse()
        {
            var clock = new ConsumableUseClock();
            clock.MarkCombat(10f);
            clock.MarkUsed("item.a", 11f);

            Assert.AreEqual(2f, clock.SecondsSinceCombat(12f), 0.0001f);
            Assert.AreEqual(1f, clock.SecondsSinceUse("item.a", 12f), 0.0001f);
            Assert.IsTrue(float.IsPositiveInfinity(clock.SecondsSinceUse("item.b", 12f)), "thời gian chờ tính RIÊNG từng loại");
        }

        [Test]
        public void Clock_Reset_ForgetsEverything()
        {
            var clock = new ConsumableUseClock();
            clock.MarkCombat(10f);
            clock.MarkUsed("item.a", 10f);

            clock.Reset();

            Assert.IsTrue(float.IsPositiveInfinity(clock.SecondsSinceCombat(11f)));
            Assert.IsTrue(float.IsPositiveInfinity(clock.SecondsSinceUse("item.a", 11f)));
        }

        // ── Helper ──

        internal static ItemDefinition MakeDefinition(string id, ItemType type, double heal, float cooldown)
        {
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            SetField(definition, "id", id);
            SetField(definition, "displayName", id);
            SetField(definition, "type", type);
            SetField(definition, "healAmount", heal);
            SetField(definition, "useCooldownSeconds", cooldown);
            return definition;
        }

        internal static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"không tìm thấy field '{name}' trên {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}
