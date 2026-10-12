using System.Collections.Generic;
using System.Reflection;
using Game.Core.Items;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Tests cho ItemDatabase.EnsureLookup — đặc biệt kiểm tra trạng thái lệch
    /// xảy ra khi Enter Play Mode với DisableDomainReload (_built=true, _lookup=null).
    /// </summary>
    public class ItemDatabaseTests
    {
        // ── Helpers ────────────────────────────────────────────────────────

        private static ItemDatabase CreateDatabase(params (string id, string displayName)[] items)
        {
            var db = ScriptableObject.CreateInstance<ItemDatabase>();
            var itemsField = typeof(ItemDatabase).GetField("items",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(itemsField, "ItemDatabase: field 'items' không tìm thấy");

            var list = new List<ItemDefinition>();
            foreach (var (id, displayName) in items)
            {
                var def = ScriptableObject.CreateInstance<ItemDefinition>();
                SetDefField(def, "id", id);
                SetDefField(def, "displayName", displayName);
                SetDefField(def, "equipmentSlot", EquipmentSlot.None);
                list.Add(def);
            }
            itemsField.SetValue(db, list);
            return db;
        }

        private static void SetDefField<T>(ItemDefinition def, string name, T value)
        {
            FieldInfo f = typeof(ItemDefinition).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"ItemDefinition: field '{name}' không tìm thấy");
            f.SetValue(def, value);
        }

        private static void ForceStaleState(ItemDatabase db)
        {
            // Mô phỏng DisableDomainReload: _built=true nhưng _lookup=null
            var builtField  = typeof(ItemDatabase).GetField("_built",  BindingFlags.Instance | BindingFlags.NonPublic);
            var lookupField = typeof(ItemDatabase).GetField("_lookup", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(builtField,  "ItemDatabase: field '_built'  không tìm thấy");
            Assert.IsNotNull(lookupField, "ItemDatabase: field '_lookup' không tìm thấy");
            builtField.SetValue(db,  true);
            lookupField.SetValue(db, null);
        }

        // ── Tests ──────────────────────────────────────────────────────────

        /// <summary>
        /// (a) Ép _built=true, _lookup=null rồi Find vẫn trả đúng item
        ///     — kiểm tra EnsureLookup xử lý trạng thái lệch (Enter Play Mode / DisableDomainReload).
        /// </summary>
        [Test]
        public void Find_RebuildsLookup_WhenBuiltTrueButLookupNull()
        {
            ItemDatabase db = CreateDatabase(("sword.iron", "Kiếm Sắt"), ("armor.leather", "Giáp Da"));

            // Gọi Find lần đầu để build lookup bình thường
            Assert.IsNotNull(db.Find("sword.iron"), "Precondition: Find lần đầu phải trả đúng item");

            // Ép vào trạng thái lệch (_built=true, _lookup=null)
            ForceStaleState(db);

            // Find phải tự rebuild và vẫn trả đúng item — không NRE
            ItemDefinition result = db.Find("sword.iron");
            Assert.IsNotNull(result, "Find phải rebuild lookup khi _built=true nhưng _lookup=null");
            Assert.AreEqual("Kiếm Sắt", result.DisplayName);

            Object.DestroyImmediate(db);
        }

        /// <summary>
        /// (b) itemId lạ → null, không ném exception.
        /// </summary>
        [Test]
        public void Find_UnknownId_ReturnsNull_NoException()
        {
            ItemDatabase db = CreateDatabase(("sword.iron", "Kiếm Sắt"));

            ItemDefinition result = null;
            Assert.DoesNotThrow(() => result = db.Find("item.does.not.exist"),
                "Find với id không tồn tại không được ném exception");
            Assert.IsNull(result, "Find với id không tồn tại phải trả null");

            Object.DestroyImmediate(db);
        }

        /// <summary>
        /// (c) Find trả đúng cho cả 3 item trong database test.
        /// </summary>
        [Test]
        public void Find_ReturnsCorrectDefinition_ForAllItems()
        {
            ItemDatabase db = CreateDatabase(
                ("sword.iron",    "Kiếm Sắt"),
                ("armor.leather", "Giáp Da"),
                ("potion.hp",     "Bình Máu"));

            Assert.AreEqual("Kiếm Sắt",   db.Find("sword.iron")?.DisplayName,    "sword.iron");
            Assert.AreEqual("Giáp Da",    db.Find("armor.leather")?.DisplayName, "armor.leather");
            Assert.AreEqual("Bình Máu",   db.Find("potion.hp")?.DisplayName,     "potion.hp");
            Assert.IsNull(db.Find(null),  "null id phải trả null");
            Assert.IsNull(db.Find(""),    "empty id phải trả null");

            Object.DestroyImmediate(db);
        }

        /// <summary>
        /// (d) Stale state + Find id lạ → null, không NRE (kết hợp case a + b).
        /// </summary>
        [Test]
        public void Find_StaleState_UnknownId_ReturnsNull_NoException()
        {
            ItemDatabase db = CreateDatabase(("sword.iron", "Kiếm Sắt"));
            ForceStaleState(db);

            ItemDefinition result = null;
            Assert.DoesNotThrow(() => result = db.Find("unknown.id"),
                "Find với stale state + id lạ không được ném exception");
            Assert.IsNull(result);

            Object.DestroyImmediate(db);
        }
    }
}
