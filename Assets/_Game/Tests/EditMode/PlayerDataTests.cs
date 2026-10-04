using System.Collections.Generic;
using Game.Core.Items;
using Game.Core.World;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Phase 5 — DTO save/load (docs/03 §5): round-trip JSON + ItemDatabase cộng chỉ số.</summary>
    public class PlayerDataTests
    {
        [Test]
        public void InventoryData_JsonRoundTrip_KeepsStacksAndInstances()
        {
            var data = new InventoryData();
            data.stacks.Add(new StackEntry { itemId = "item.potion", quantity = 5 });
            data.instances.Add(new InstanceEntry
            {
                instanceId = "inst_1",
                itemId = "item.sword",
                upgradeLevel = 2,
                rolledAttack = 3.5,
                rolledDefense = 1.5,
                rolledMaxHealth = 10.0
            });

            var restored = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(data));

            Assert.AreEqual(1, restored.stacks.Count);
            Assert.AreEqual("item.potion", restored.stacks[0].itemId);
            Assert.AreEqual(5, restored.stacks[0].quantity);
            Assert.AreEqual(1, restored.instances.Count);
            Assert.AreEqual("inst_1", restored.instances[0].instanceId);
            Assert.AreEqual(2, restored.instances[0].upgradeLevel);
            Assert.AreEqual(3.5, restored.instances[0].rolledAttack, 0.0001);
        }

        [Test]
        public void ProfileData_JsonRoundTrip_KeepsLevelAndExp()
        {
            var profile = new ProfileData { level = 7, exp = 250 };

            var restored = JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(profile));

            Assert.AreEqual(7, restored.level);
            Assert.AreEqual(250, restored.exp);
        }

        [Test]
        public void ItemDatabase_SumEquipment_AddsBaseAndRolledStats()
        {
            ItemDefinition sword = ScriptableObject.CreateInstance<ItemDefinition>();
            ItemDatabase database = ScriptableObject.CreateInstance<ItemDatabase>();

            try
            {
                SetPrivate(sword, "id", "item.sword");
                SetPrivate(sword, "baseStats", new ItemStats(5.0, 0.0, 0.0));
                SetPrivate(sword, "equipmentSlot", EquipmentSlot.Weapon);
                SetPrivate(database, "items", new List<ItemDefinition> { sword });

                var inventory = new Inventory();
                inventory.AddInstance(new ItemInstance("inst_1", "item.sword", new ItemStats(2.0, 1.0, 0.0)));
                var equipment = new Equipment();
                equipment.Equip(EquipmentSlot.Weapon, "inst_1");

                ItemStats total = database.SumEquipment(equipment, inventory);

                Assert.AreEqual(7.0, total.attack, 0.0001, "base 5 + rolled 2");
                Assert.AreEqual(1.0, total.defense, 0.0001);
                Assert.IsNotNull(database.Find("item.sword"));
                Assert.IsNull(database.Find("item.khong_ton_tai"));
            }
            finally
            {
                Object.DestroyImmediate(sword);
                Object.DestroyImmediate(database);
            }
        }

        /// <summary>Test ghi field private serialized (không có setter public — đúng luật balance trong SO).</summary>
        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, $"không tìm thấy field {fieldName}");
            field.SetValue(target, value);
        }
    }
}
