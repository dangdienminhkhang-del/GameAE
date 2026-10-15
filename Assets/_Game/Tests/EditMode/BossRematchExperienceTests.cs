using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Character;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Items;
using Game.Core.Time;
using Game.Core.World;
using Game.PvE.Boss;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// P9-1c — Boss đánh lại (rematch) KHÔNG cho EXP (docs/02 §24).
    /// "Rematch" = World State báo boss đã Defeated (cùng cờ đang quyết định đồ hiếm × rematchRareMultiplier).
    /// BossController đọc cờ này MỘT lần rồi dùng chung cho EXP, đồ hiếm và báo cáo.
    ///
    /// Ghi chú hành vi: đọc World State lỗi → coi là KHÔNG-rematch (hành vi sẵn có của dự án) → vẫn có EXP và vẫn báo server.
    /// </summary>
    public class BossRematchExperienceTests
    {
        private const int BossExp = 77;

        // ── Fake service (mẫu từ BossWorldIntegrationTests) ─────────────────────

        private sealed class StrictSaveService : ISaveService, IAuthoritativeSaveService
        {
            public Task<T> LoadAsync<T>(string key) => Task.FromResult(default(T));
            public Task SaveAsync<T>(string key, T value) => Task.CompletedTask;
            public Task DeleteAsync(string key) => Task.CompletedTask;
            public Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys) =>
                Task.FromResult(new Dictionary<string, object>());
            public Task SaveBatchAsync(Dictionary<string, object> keyValues) => Task.CompletedTask;
            public Task<T> LoadAuthoritativeAsync<T>(string key) => Task.FromResult(default(T));
            public Task SaveAuthoritativeAsync<T>(string key, T value) => Task.CompletedTask;
        }

        private sealed class FakeWorldStateService : IWorldStateService
        {
            public BossStatus CurrentBossStatus = BossStatus.Active;
            public int ReportCalls;
            public int GetBossStatusCalls;
            public bool ThrowOnRead;

            public Task<Dictionary<string, FloorStatus>> GetAllFloorStatusesAsync() =>
                Task.FromResult(new Dictionary<string, FloorStatus>());
            public Task<FloorStatus> GetFloorStatusAsync(string floorId) => Task.FromResult(FloorStatus.Locked);
            public Task<Dictionary<string, BossStatus>> GetAllBossStatusesAsync() =>
                Task.FromResult(new Dictionary<string, BossStatus>());
            public Task<WorldTime> GetWorldTimeAsync() => Task.FromResult(new WorldTime());

            public Task<BossStatus> GetBossStatusAsync(string bossId)
            {
                GetBossStatusCalls++;
                if (ThrowOnRead) throw new Exception("world read failed");
                return Task.FromResult(CurrentBossStatus);
            }

            public Task ReportBossDefeated(string bossId, string floorId)
            {
                ReportCalls++;
                CurrentBossStatus = BossStatus.Defeated;
                return Task.CompletedTask;
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static ItemDefinition MakeItem(string id)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            SetPrivate(item, "id", id);
            SetPrivate(item, "displayName", id);
            return item;
        }

        private static BossDefinition MakeBoss(ItemDefinition main)
        {
            var boss = ScriptableObject.CreateInstance<BossDefinition>();
            SetPrivate(boss, "id", "boss.floor_01");
            SetPrivate(boss, "floorId", "floor.01");
            SetPrivate(boss, "experienceReward", BossExp);
            SetPrivate(boss, "firstClearRewardItem", (ItemDefinition)null);
            SetPrivate(boss, "rematchRareMultiplier", 0.05f);
            SetPrivate(boss, "drops", new List<BossDefinition.BossDrop>
            {
                new BossDefinition.BossDrop { item = main, chance = 1f, minCount = 1, maxCount = 1 }
            });
            SetPrivate(boss, "rareDrops", new List<BossDefinition.BossDrop>());
            return boss;
        }

        private static void SetPrivate<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, "Thiếu field test: " + fieldName);
            field.SetValue(target, value);
        }

        /// <summary>Kết quả theo dõi các sự kiện lúc boss gục.</summary>
        private sealed class Observed
        {
            public readonly List<string> Order = new List<string>();
            public readonly List<int> ExpValues = new List<int>();
        }

        /// <summary>Chạy boss gục qua BossController thật, ghi lại thứ tự sự kiện.</summary>
        private static async Task<Observed> DefeatBossAsync(FakeWorldStateService world)
        {
            ItemDefinition main = MakeItem("item.main");
            BossDefinition boss = MakeBoss(main);
            var gameObject = new GameObject("BossRematchExpTest");
            gameObject.AddComponent<Rigidbody2D>();
            var controller = gameObject.AddComponent<BossController>();

            var seen = new Observed();
            EventHandler enemyHandler = (s, e) => seen.Order.Add("enemy");
            EventHandler<BossDefeatedArgs> bossHandler = (s, a) => seen.Order.Add("boss");
            EventHandler<int> expHandler = (s, exp) => { seen.Order.Add("exp"); seen.ExpValues.Add(exp); };
            Action<BossController> diedHandler = _ => seen.Order.Add("died");

            GameEvents.OnEnemyDefeated += enemyHandler;
            GameEvents.OnBossDefeated += bossHandler;
            GameEvents.OnExperienceGained += expHandler;
            controller.Died += diedHandler;
            try
            {
                controller.SetDefinition(boss);
                controller.SetServices(world, new StrictSaveService());
                await controller.HandleDefeatAsync();
            }
            finally
            {
                GameEvents.OnEnemyDefeated -= enemyHandler;
                GameEvents.OnBossDefeated -= bossHandler;
                GameEvents.OnExperienceGained -= expHandler;
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(main);
                Object.DestroyImmediate(boss);
            }
            return seen;
        }

        // ── BossController: EXP theo rematch ────────────────────────────────────

        [Test]
        public async Task Controller_Rematch_GivesNoExp_NoWorldReport_ButDefeatEventsStillFire()
        {
            var world = new FakeWorldStateService { CurrentBossStatus = BossStatus.Defeated };

            Observed seen = await DefeatBossAsync(world);

            Assert.AreEqual(0, seen.ExpValues.Count, "Rematch không được cho EXP.");
            Assert.AreEqual(0, world.ReportCalls, "Rematch không báo World State.");
            Assert.AreEqual(1, world.GetBossStatusCalls, "Chỉ đọc World State một lần.");
            CollectionAssert.AreEqual(new[] { "enemy", "boss", "died" }, seen.Order,
                "EnemyDefeated, BossDefeated, Died vẫn phát đủ.");
        }

        [Test]
        public async Task Controller_FirstKill_GivesExpExactlyOnce_AndReports()
        {
            var world = new FakeWorldStateService { CurrentBossStatus = BossStatus.Active };

            Observed seen = await DefeatBossAsync(world);

            CollectionAssert.AreEqual(new[] { BossExp }, seen.ExpValues, "Đúng một lần, đúng definition.ExperienceReward.");
            Assert.AreEqual(1, world.ReportCalls, "Lần hạ đầu phải báo World State.");
            Assert.AreEqual(1, world.GetBossStatusCalls,
                "EXP, đồ hiếm và báo cáo dùng CÙNG một lần đọc — không đọc hai lần.");
            CollectionAssert.AreEqual(new[] { "enemy", "boss", "died", "exp" }, seen.Order,
                "EnemyDefeated/BossDefeated/Died phát ngay; EXP phát sau khi biết rematch.");
        }

        [Test]
        public async Task Controller_WorldStateReadFails_IsTreatedAsNotRematch_ExpStillGiven()
        {
            // Hành vi sẵn có: đọc lỗi → không suy ra rematch → có EXP và vẫn báo (server idempotent quyết định).
            var world = new FakeWorldStateService { ThrowOnRead = true };

            Observed seen = await DefeatBossAsync(world);

            CollectionAssert.AreEqual(new[] { BossExp }, seen.ExpValues);
            Assert.AreEqual(1, world.ReportCalls);
            Assert.AreEqual(1, world.GetBossStatusCalls);
        }

        // ── BossDefeatFlow: đồ rơi và cờ knownRematch ───────────────────────────

        [Test]
        public async Task Flow_Rematch_MainDropStillFalls_AndNoWorldReport()
        {
            ItemDefinition main = MakeItem("item.main");
            BossDefinition boss = MakeBoss(main);
            try
            {
                var world = new FakeWorldStateService { CurrentBossStatus = BossStatus.Defeated };
                var flow = new BossDefeatFlow(world, new StrictSaveService());
                int granted = 0;

                // Đúng trình tự BossController: hỏi rematch trước, rồi truyền xuống luồng.
                bool isRematch = await flow.ResolveIsRematchAsync(boss.Id);
                BossDefeatFlowResult result = await flow.HandleDefeatAsync(
                    boss, isPlayerDead: false,
                    (item, quantity) => { granted += quantity; return quantity; },
                    knownRematch: isRematch);

                Assert.IsTrue(result.IsRematch);
                Assert.AreEqual(BossDefeatReportStatus.NotRequired, result.ReportStatus);
                Assert.AreEqual(0, world.ReportCalls, "Rematch không báo World State.");
                Assert.GreaterOrEqual(granted, 1, "Đồ chính vẫn rơi khi rematch.");
                Assert.AreEqual(1, world.GetBossStatusCalls, "Chỉ một lần đọc cho cả hai bước.");
            }
            finally
            {
                Object.DestroyImmediate(main);
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public async Task Flow_KnownRematchTrue_DoesNotReadWorldState_AndTrustsTheGivenValue()
        {
            ItemDefinition main = MakeItem("item.main");
            BossDefinition boss = MakeBoss(main);
            try
            {
                // World State nói "Active" nhưng cờ truyền vào là true → phải tin cờ truyền vào.
                var world = new FakeWorldStateService { CurrentBossStatus = BossStatus.Active };
                var flow = new BossDefeatFlow(world, new StrictSaveService());

                BossDefeatFlowResult result = await flow.HandleDefeatAsync(
                    boss, isPlayerDead: false, (item, quantity) => quantity, knownRematch: true);

                Assert.AreEqual(0, world.GetBossStatusCalls, "Có knownRematch thì không đọc World State.");
                Assert.IsTrue(result.IsRematch);
                Assert.AreEqual(0, world.ReportCalls);
            }
            finally
            {
                Object.DestroyImmediate(main);
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public async Task Flow_KnownRematchFalse_DoesNotReadWorldState_AndReports()
        {
            ItemDefinition main = MakeItem("item.main");
            BossDefinition boss = MakeBoss(main);
            try
            {
                // World State nói "Defeated" nhưng cờ truyền vào là false → phải tin cờ truyền vào.
                var world = new FakeWorldStateService { CurrentBossStatus = BossStatus.Defeated };
                var flow = new BossDefeatFlow(world, new StrictSaveService());

                BossDefeatFlowResult result = await flow.HandleDefeatAsync(
                    boss, isPlayerDead: false, (item, quantity) => quantity, knownRematch: false);

                Assert.AreEqual(0, world.GetBossStatusCalls);
                Assert.IsFalse(result.IsRematch);
                Assert.AreEqual(1, world.ReportCalls);
            }
            finally
            {
                Object.DestroyImmediate(main);
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public async Task Flow_KnownRematchNull_BehavesAsBefore_ReadsWorldStateOnce()
        {
            ItemDefinition main = MakeItem("item.main");
            BossDefinition boss = MakeBoss(main);
            try
            {
                var world = new FakeWorldStateService { CurrentBossStatus = BossStatus.Defeated };
                var flow = new BossDefeatFlow(world, new StrictSaveService());

                BossDefeatFlowResult result = await flow.HandleDefeatAsync(
                    boss, isPlayerDead: false, (item, quantity) => quantity, knownRematch: null);

                Assert.AreEqual(1, world.GetBossStatusCalls);
                Assert.IsTrue(result.IsRematch);
                Assert.AreEqual(0, world.ReportCalls);

                // Gọi kiểu cũ (3 tham số) cũng như vậy.
                var world2 = new FakeWorldStateService { CurrentBossStatus = BossStatus.Active };
                var flow2 = new BossDefeatFlow(world2, new StrictSaveService());
                BossDefeatFlowResult legacy = await flow2.HandleDefeatAsync(boss, false, (item, quantity) => quantity);

                Assert.AreEqual(1, world2.GetBossStatusCalls);
                Assert.IsFalse(legacy.IsRematch);
                Assert.AreEqual(1, world2.ReportCalls);
            }
            finally
            {
                Object.DestroyImmediate(main);
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public async Task ResolveIsRematchAsync_DefeatedTrue_ActiveFalse_ReadErrorFalse()
        {
            var flow = new BossDefeatFlow(new FakeWorldStateService { CurrentBossStatus = BossStatus.Defeated }, new StrictSaveService());
            Assert.IsTrue(await flow.ResolveIsRematchAsync("boss.floor_01"));

            flow = new BossDefeatFlow(new FakeWorldStateService { CurrentBossStatus = BossStatus.Active }, new StrictSaveService());
            Assert.IsFalse(await flow.ResolveIsRematchAsync("boss.floor_01"));

            flow = new BossDefeatFlow(new FakeWorldStateService { ThrowOnRead = true }, new StrictSaveService());
            Assert.IsFalse(await flow.ResolveIsRematchAsync("boss.floor_01"), "Đọc lỗi → không-rematch.");

            flow = new BossDefeatFlow(null, new StrictSaveService());
            Assert.IsFalse(await flow.ResolveIsRematchAsync("boss.floor_01"), "Không có service → không-rematch.");
        }
    }
}
