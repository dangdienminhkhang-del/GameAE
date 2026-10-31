using Game.Core.Items;
using Game.Core.World;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class EquipmentSlotRulesTests
    {
        [Test]
        public void NonAccessorySlots_ResolveToThemselves()
        {
            var eq = new Equipment();
            Assert.AreEqual(EquipmentSlot.Weapon, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Weapon, eq, "a"));
            Assert.AreEqual(EquipmentSlot.Armor, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Armor, eq, "a"));
            Assert.AreEqual(EquipmentSlot.OffHand, EquipmentSlotRules.ResolveTarget(EquipmentSlot.OffHand, eq, "a"));
            Assert.AreEqual(EquipmentSlot.None, EquipmentSlotRules.ResolveTarget(EquipmentSlot.None, eq, "a"));
        }

        [Test]
        public void Weapon_EquipsToMainHandFirst_ThenOffHandForDualWield()
        {
            var eq = new Equipment();
            Assert.AreEqual(EquipmentSlot.Weapon, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Weapon, eq, "sword1"));
            eq.Equip(EquipmentSlot.Weapon, "sword1");

            // Khi tay chính đã có vũ khí, vũ khí thứ 2 tự vào tay phụ để kích hoạt Nhị Đao Lưu
            Assert.AreEqual(EquipmentSlot.OffHand, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Weapon, eq, "sword2"));
            eq.Equip(EquipmentSlot.OffHand, "sword2");

            // Cả hai tay đều có vũ khí → vũ khí thứ 3 thay vào tay chính
            Assert.AreEqual(EquipmentSlot.Weapon, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Weapon, eq, "sword3"));
        }

        [Test]
        public void Weapon_AlreadyInOffHand_KeepsOffHandSlot()
        {
            var eq = new Equipment();
            eq.Equip(EquipmentSlot.Weapon, "sword1");
            eq.Equip(EquipmentSlot.OffHand, "sword2");

            Assert.AreEqual(EquipmentSlot.OffHand, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Weapon, eq, "sword2"));
            Assert.AreEqual(EquipmentSlot.Weapon, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Weapon, eq, "sword1"));
        }

        [Test]
        public void OffHandSlot_ExplicitDefinition_AlwaysResolvesToOffHand()
        {
            var eq = new Equipment();
            Assert.AreEqual(EquipmentSlot.OffHand, EquipmentSlotRules.ResolveTarget(EquipmentSlot.OffHand, eq, "dagger1"));
            eq.Equip(EquipmentSlot.OffHand, "dagger1");
            Assert.AreEqual(EquipmentSlot.OffHand, EquipmentSlotRules.ResolveTarget(EquipmentSlot.OffHand, eq, "dagger2"));
        }

        [Test]
        public void Accessory_FillsFirstFreeSlot_ThenSecond_ThenReplacesFirst()
        {
            var eq = new Equipment();

            Assert.AreEqual(EquipmentSlot.Accessory, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Accessory, eq, "ring1"));
            eq.Equip(EquipmentSlot.Accessory, "ring1");

            Assert.AreEqual(EquipmentSlot.Accessory2, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Accessory, eq, "ring2"));
            eq.Equip(EquipmentSlot.Accessory2, "ring2");

            Assert.AreEqual(EquipmentSlot.Accessory, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Accessory, eq, "ring3"));
        }

        [Test]
        public void Accessory_AlreadyEquipped_KeepsItsCurrentSlot()
        {
            var eq = new Equipment();
            eq.Equip(EquipmentSlot.Accessory, "ring1");
            eq.Equip(EquipmentSlot.Accessory2, "ring2");

            Assert.AreEqual(EquipmentSlot.Accessory2, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Accessory, eq, "ring2"));
            Assert.AreEqual(EquipmentSlot.Accessory, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Accessory, eq, "ring1"));
        }

        [Test]
        public void Accessory_FreesFirstSlotAfterUnequip()
        {
            var eq = new Equipment();
            eq.Equip(EquipmentSlot.Accessory, "ring1");
            eq.Equip(EquipmentSlot.Accessory2, "ring2");
            eq.Unequip(EquipmentSlot.Accessory);

            Assert.AreEqual(EquipmentSlot.Accessory, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Accessory, eq, "ring3"));
        }

        [Test]
        public void NullEquipment_FallsBackToFirstAccessorySlot()
        {
            Assert.AreEqual(EquipmentSlot.Accessory, EquipmentSlotRules.ResolveTarget(EquipmentSlot.Accessory, null, "x"));
        }

        [Test]
        public void AllSlots_AreUnique_AndHaveDisplayNames()
        {
            var seen = new System.Collections.Generic.HashSet<EquipmentSlot>();
            foreach (EquipmentSlot slot in EquipmentSlotRules.AllSlots)
            {
                Assert.AreNotEqual(EquipmentSlot.None, slot);
                Assert.IsTrue(seen.Add(slot), "Slot bị lặp: " + slot);
                Assert.IsNotEmpty(EquipmentSlotRules.DisplayName(slot));
            }
            Assert.AreEqual(5, seen.Count);
        }

        [Test]
        public void EnumValues_AreStable_ForSerializedAssets()
        {
            Assert.AreEqual(1, (int)EquipmentSlot.Weapon);
            Assert.AreEqual(2, (int)EquipmentSlot.Armor);
            Assert.AreEqual(3, (int)EquipmentSlot.OffHand);
            Assert.AreEqual(4, (int)EquipmentSlot.Accessory);
            Assert.AreEqual(5, (int)EquipmentSlot.Accessory2);
            Assert.AreEqual(3, (int)ItemType.Material);
            Assert.AreEqual(4, (int)ItemType.Accessory);
        }

        [Test]
        public void EquipmentData_OldSaveWithoutNewSlots_LoadsAsEmpty()
        {
            // Save cũ chỉ có weapon + armor: các ô mới phải là null (trống), không lỗi.
            var data = JsonUtility.FromJson<EquipmentData>("{\"weapon\":\"w1\",\"armor\":\"a1\"}");
            Assert.AreEqual("w1", data.weapon);
            Assert.AreEqual("a1", data.armor);
            Assert.IsTrue(string.IsNullOrEmpty(data.offHand));
            Assert.IsTrue(string.IsNullOrEmpty(data.accessory));
            Assert.IsTrue(string.IsNullOrEmpty(data.accessory2));
        }

        [Test]
        public void EquipmentData_NewSlots_RoundTripThroughJson()
        {
            var data = new EquipmentData { weapon = "w", offHand = "o", accessory = "r1", accessory2 = "r2" };
            var back = JsonUtility.FromJson<EquipmentData>(JsonUtility.ToJson(data));
            Assert.AreEqual("o", back.offHand);
            Assert.AreEqual("r1", back.accessory);
            Assert.AreEqual("r2", back.accessory2);
        }
    }
}
