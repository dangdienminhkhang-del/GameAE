using System.Collections.Generic;
using Game.Core.Items;
using Game.Core.Stats;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Phase 5 — kho đồ, trang bị, và cộng chỉ số (Base + Equipment, docs/02 §6).</summary>
    public class InventoryTests
    {
        [Test]
        public void AddStack_OverflowsIntoNewStack_WithoutLosingItems()
        {
            var inventory = new Inventory();

            int acceptedFirst = inventory.AddStack("potion", 5, 9);
            int acceptedSecond = inventory.AddStack("potion", 9, 9);

            Assert.AreEqual(5, acceptedFirst);
            Assert.AreEqual(9, acceptedSecond, "phải nhận đủ 9 — không được nuốt phần vượt maxStack");
            Assert.AreEqual(14, inventory.GetStack("potion"), "tổng không đổi: 5 + 9");
            Assert.AreEqual(2, inventory.Stacks.Count, "tràn maxStack thì mở stack mới");
            Assert.LessOrEqual(inventory.Stacks[0].quantity, 9);
        }

        [Test]
        public void RemoveStack_FailsWhenNotEnough()
        {
            var inventory = new Inventory();
            inventory.AddStack("potion", 3, 99);

            Assert.IsFalse(inventory.RemoveStack("potion", 5));
            Assert.AreEqual(3, inventory.GetStack("potion"));
            Assert.IsTrue(inventory.RemoveStack("potion", 3));
            Assert.AreEqual(0, inventory.GetStack("potion"));
        }

        [Test]
        public void Instances_AddFindRemove()
        {
            var inventory = new Inventory();
            inventory.AddInstance(new ItemInstance("inst_1", "sword", default));

            Assert.IsNotNull(inventory.FindInstance("inst_1"));
            Assert.IsTrue(inventory.RemoveInstance("inst_1"));
            Assert.IsNull(inventory.FindInstance("inst_1"));
            Assert.IsFalse(inventory.RemoveInstance("inst_1"));
        }

        [Test]
        public void Equipment_EquipReplacesSlotAndReturnsPrevious()
        {
            var equipment = new Equipment();

            Assert.IsNull(equipment.Equip(EquipmentSlot.Weapon, "inst_1"));
            Assert.AreEqual("inst_1", equipment.GetInstanceId(EquipmentSlot.Weapon));
            Assert.AreEqual("inst_1", equipment.Equip(EquipmentSlot.Weapon, "inst_2"));
            Assert.AreEqual("inst_2", equipment.GetInstanceId(EquipmentSlot.Weapon));
            Assert.IsTrue(equipment.IsEquipped("inst_2"));
            Assert.IsFalse(equipment.IsEquipped("inst_1"));
        }

        [Test]
        public void Equipment_UnequipClearsSlot()
        {
            var equipment = new Equipment();
            equipment.Equip(EquipmentSlot.Armor, "inst_9");

            Assert.AreEqual("inst_9", equipment.Unequip(EquipmentSlot.Armor));
            Assert.IsNull(equipment.GetInstanceId(EquipmentSlot.Armor));
        }

        [Test]
        public void CharacterStats_EquipmentBonusAddsOnTopOfLevel()
        {
            var stats = new CharacterStats(2);

            stats.SetEquipmentBonus(attack: 5, defense: 3, maxHealth: 10);

            Assert.AreEqual(17.0, stats.Attack, 0.0001, "ATK level 2 = 12, +5 = 17");
            Assert.AreEqual(9.0, stats.Defense, 0.0001, "DEF level 2 = 6, +3 = 9");
            Assert.AreEqual(125.0, stats.MaxHealth, 0.0001, "HP level 2 = 115, +10 = 125");
        }

        [Test]
        public void CharacterStats_MaxHealthOverrideStillWins()
        {
            var stats = new CharacterStats(1);
            stats.SetEquipmentBonus(0.0, 0.0, 50.0);

            stats.SetMaxHealthOverride(200.0);
            Assert.AreEqual(200.0, stats.MaxHealth, 0.0001);

            stats.SetLevel(3);
            Assert.AreEqual(200.0, stats.MaxHealth, 0.0001, "override vẫn thắng sau khi đổi level");
        }
    }
}
