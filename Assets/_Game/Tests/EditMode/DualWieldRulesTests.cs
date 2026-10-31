using System.Collections.Generic;
using System.Reflection;
using Game.Core.Combat;
using Game.Core.Items;
using Game.PvE.Items;
using Game.PvE.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class DualWieldRulesTests
    {
        [Test]
        public void IsDualWielding_RequiresMainAndOffHandWeapons()
        {
            Assert.IsTrue(DualWieldRules.IsDualWielding(hasMainHandWeapon: true, hasOffHandWeapon: true, isSkillUnlocked: true));
            Assert.IsFalse(DualWieldRules.IsDualWielding(hasMainHandWeapon: true, hasOffHandWeapon: false, isSkillUnlocked: true));
            Assert.IsFalse(DualWieldRules.IsDualWielding(hasMainHandWeapon: false, hasOffHandWeapon: true, isSkillUnlocked: true));
            Assert.IsFalse(DualWieldRules.IsDualWielding(hasMainHandWeapon: true, hasOffHandWeapon: true, isSkillUnlocked: false));
        }

        [Test]
        public void CalculateTotalAttack_WithoutDualWield_OnlyMainHandContributes()
        {
            double total = DualWieldRules.CalculateTotalAttack(
                baseAttack: 10.0,
                mainWeaponAttack: 25.0,
                offWeaponAttack: 20.0,
                isDualWielding: false);

            Assert.AreEqual(35.0, total, 1e-4, "Chưa bật Nhị Đao Lưu thì chỉ tính kiếm chính.");
        }

        [Test]
        public void CalculateTotalAttack_WithDualWield_CombinesMainAndOffHand()
        {
            double total = DualWieldRules.CalculateTotalAttack(
                baseAttack: 10.0,
                mainWeaponAttack: 25.0,
                offWeaponAttack: 20.0,
                isDualWielding: true);

            // 10 + 25 + (20 * 0.60) = 47.0
            Assert.AreEqual(47.0, total, 1e-4, "Nhị Đao Lưu phải cộng thêm 60% sát thương vũ khí phụ.");
        }

        [Test]
        public void CalculateAttackCooldown_DualWielding_ReducesCooldownBy25Percent()
        {
            float normal = DualWieldRules.CalculateAttackCooldown(0.40f, isDualWielding: false);
            Assert.AreEqual(0.40f, normal, 1e-4f);

            float fast = DualWieldRules.CalculateAttackCooldown(0.40f, isDualWielding: true);
            Assert.AreEqual(0.30f, fast, 1e-4f, "Cầm song kiếm phải giảm 25% thời gian hồi đòn (từ 0.40s xuống 0.30s).");
        }

        [Test]
        public void GetComboMultiplier_DualWielding_HasHigherScaling()
        {
            double step0 = DualWieldRules.GetComboMultiplier(0, isDualWielding: true);
            double step1 = DualWieldRules.GetComboMultiplier(1, isDualWielding: true);
            double step2 = DualWieldRules.GetComboMultiplier(2, isDualWielding: true);

            Assert.AreEqual(1.15, step0, 1e-4);
            Assert.AreEqual(1.45, step1, 1e-4);
            Assert.AreEqual(2.20, step2, 1e-4);

            Assert.Greater(step2, step1);
            Assert.Greater(step1, step0);
        }

        [Test]
        public void PlayerAttack_IsDualWielding_CanBeToggled()
        {
            var go = new GameObject("PlayerAttackTest", typeof(PlayerController), typeof(PlayerAttack));
            var attack = go.GetComponent<PlayerAttack>();

            try
            {
                Assert.IsFalse(attack.IsDualWielding);
                attack.IsDualWielding = true;
                Assert.IsTrue(attack.IsDualWielding);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ItemDatabase_SumEquipment_OffHandContributes60PercentAttack()
        {
            var mainSword = ScriptableObject.CreateInstance<ItemDefinition>();
            var offSword = ScriptableObject.CreateInstance<ItemDefinition>();
            var database = ScriptableObject.CreateInstance<ItemDatabase>();

            try
            {
                SetPrivate(mainSword, "id", "item.main");
                SetPrivate(mainSword, "baseStats", new ItemStats(10.0, 0.0, 0.0));
                SetPrivate(mainSword, "equipmentSlot", EquipmentSlot.Weapon);

                SetPrivate(offSword, "id", "item.off");
                SetPrivate(offSword, "baseStats", new ItemStats(10.0, 2.0, 20.0));
                SetPrivate(offSword, "equipmentSlot", EquipmentSlot.OffHand);

                SetPrivate(database, "items", new List<ItemDefinition> { mainSword, offSword });

                var inventory = new Inventory();
                inventory.AddInstance(new ItemInstance("inst_main", "item.main", default));
                inventory.AddInstance(new ItemInstance("inst_off", "item.off", default));

                var equipment = new Equipment();
                equipment.Equip(EquipmentSlot.Weapon, "inst_main");
                equipment.Equip(EquipmentSlot.OffHand, "inst_off");

                ItemStats total = database.SumEquipment(equipment, inventory);
                // Main hand: 10 atk. Off hand: 10 * 0.6 = 6 atk. Total = 16.0 atk
                Assert.AreEqual(16.0, total.attack, 1e-4, "Tay phụ chỉ đóng góp 60% sát thương công kích.");
                Assert.AreEqual(2.0, total.defense, 1e-4, "Chỉ số thủ của tay phụ giữ nguyên 100%.");
                Assert.AreEqual(20.0, total.maxHealth, 1e-4, "Máu cộng thêm từ tay phụ giữ nguyên 100%.");
            }
            finally
            {
                Object.DestroyImmediate(mainSword);
                Object.DestroyImmediate(offSword);
                Object.DestroyImmediate(database);
            }
        }

        [Test]
        public void PlayerInventory_EquippingBothHands_EnablesDualWieldOnPlayerAttack()
        {
            var go = new GameObject("Player_DualWieldTest", typeof(PlayerController), typeof(PlayerAttack), typeof(PlayerInventory));
            var attack = go.GetComponent<PlayerAttack>();
            var inv = go.GetComponent<PlayerInventory>();

            try
            {
                inv.Equipment.Equip(EquipmentSlot.Weapon, "sword1");
                inv.UpdateDualWieldState();
                Assert.IsFalse(attack.IsDualWielding, "Chỉ có 1 kiếm chưa thể kích hoạt Nhị Đao Lưu");

                inv.Equipment.Equip(EquipmentSlot.OffHand, "sword2");
                inv.UpdateDualWieldState();
                Assert.IsTrue(attack.IsDualWielding, "Cầm 2 kiếm trên 2 tay phải kích hoạt Nhị Đao Lưu!");

                inv.Equipment.Unequip(EquipmentSlot.OffHand);
                inv.UpdateDualWieldState();
                Assert.IsFalse(attack.IsDualWielding, "Tháo kiếm tay phụ phải hủy trạng thái Nhị Đao Lưu");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void SetPrivate<T>(object target, string fieldName, T value)
        {
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi != null) fi.SetValue(target, value);
        }
    }
}
