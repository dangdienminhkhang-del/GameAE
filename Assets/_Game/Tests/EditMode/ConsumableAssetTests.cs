using System.Collections.Generic;
using Game.Core.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task P Phần 1 — đọc tài sản THẬT từ đĩa.
    /// AssetDatabase: API của Unity Editor để tải file asset (không cần chạy game).
    /// ConsumableRules.CanUse: hàm kiểm tra xem có được dùng Bình Máu không.
    /// </summary>
    public class ConsumableAssetTests
    {
        private const string PotionPath = "Assets/_Game/Data/Items/Item_HealthPotion.asset";

        // ── Test 1: đọc trường số từ asset thật ──────────────────────────

        [Test]
        public void HealthPotion_Asset_HasCorrectValues()
        {
            var potion = AssetDatabase.LoadAssetAtPath<ItemDefinition>(PotionPath);
            Assert.IsNotNull(potion, $"Không tìm thấy asset tại {PotionPath}");

            Assert.AreEqual("item.health_potion", potion.Id,
                "Id phải khớp — dùng để tra trong kho đồ");
            Assert.AreEqual(ItemType.Consumable, potion.Type,
                "Type = Consumable (2) — chỉ loại này mới dùng được");
            Assert.AreEqual(40.0, potion.HealAmount, 0.0001,
                "HealAmount = 40 — lượng HP hồi mỗi lần dùng");
            Assert.AreEqual(5f, potion.UseCooldownSeconds, 0.0001f,
                "UseCooldownSeconds = 5 — thời gian chờ giữa 2 lần dùng");
        }

        // ── Test 2: ConsumableRules.CanUse trả Ok với asset thật ─────────

        [Test]
        public void HealthPotion_CanUse_ReturnsOk_WithRealAsset()
        {
            var potion = AssetDatabase.LoadAssetAtPath<ItemDefinition>(PotionPath);
            Assert.IsNotNull(potion, $"Không tìm thấy asset tại {PotionPath}");

            // Điều kiện: máu 60/100, đã yên tĩnh 100 giây (ngoài giao chiến),
            // chưa dùng lần nào (Infinity), còn 1 bình trong kho.
            // Tham số đúng: secondsSinceCombat (không phải secondsSinceLastCombat)
            ConsumableUseResult result = ConsumableRules.CanUse(
                potion,
                currentHp: 60f,
                maxHp: 100f,
                secondsSinceCombat: 100f,
                secondsSinceLastUse: float.PositiveInfinity,
                quantity: 1);

            Assert.AreEqual(ConsumableUseResult.Ok, result,
                "Phải trả Ok: máu thiếu, ngoài giao chiến, không cooldown, còn bình");
        }

        // ── Test 3: Bình Máu thật nằm trong ItemDatabase nào đó ─────────

        [Test]
        public void HealthPotion_ExistsInSomeItemDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemDatabase");

            if (guids.Length == 0)
            {
                Assert.Fail("Không tìm thấy asset ItemDatabase nào trong project. " +
                            "Tạo một ItemDatabase và thêm Bình Máu vào danh sách.");
            }

            var found = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
                if (db == null) continue;

                ItemDefinition potion = db.Find("item.health_potion");
                if (potion != null)
                    found.Add(path);
            }

            Assert.IsTrue(found.Count > 0,
                "Không có ItemDatabase nào chứa item.health_potion. " +
                "Mở ItemDatabase.asset → kéo Item_HealthPotion vào danh sách Items.");
        }
    }
}
