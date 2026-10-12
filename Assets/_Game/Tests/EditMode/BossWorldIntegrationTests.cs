using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Character;
using Game.Core.Combat;
using Game.Core.Items;
using Game.Core.Time;
using Game.Core.World;
using Game.PvE.Boss;
using Game.PvE.Bootstrap;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Kiểm thử Phase 8 / Wave 3 (Boss & Shared World State) — docs/02 §21–24, docs/03 §6, docs/13 §5:
    /// 1. BossDefinition validation (ID chuẩn, telegraph, punish window).
    /// 2. First-Clear Reward Claim chỉ nhận đúng 1 lần cho mỗi player save (không duplicate).
    /// 3. Permadeath khi chết trong trận boss: ghi nhận đúng reason=Boss.
    ///
    /// World State do server xác nhận (WorldStateService + UGSServerFunctionService, hai account,
    /// retry, FloorLoader) được kiểm ở <see cref="WorldStateServerConfirmationTests"/> (task P8-2).
    /// </summary>
    public class BossWorldIntegrationTests
    {
        private class InMemorySaveService : ISaveService, IAuthoritativeSaveService
        {
            private readonly Dictionary<string, object> _storage = new Dictionary<string, object>();

            public Task<T> LoadAsync<T>(string key)
            {
                if (_storage.TryGetValue(key, out var val) && val is T typed)
                    return Task.FromResult(typed);
                return Task.FromResult(default(T));
            }

            public Task<T> LoadAuthoritativeAsync<T>(string key)
            {
                return LoadAsync<T>(key);
            }

            public Task SaveAsync<T>(string key, T value)
            {
                _storage[key] = value;
                return Task.CompletedTask;
            }

            public Task SaveAuthoritativeAsync<T>(string key, T value)
            {
                _storage[key] = value;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string key)
            {
                _storage.Remove(key);
                return Task.CompletedTask;
            }

            public Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys)
            {
                var dict = new Dictionary<string, object>();
                foreach (var k in keys)
                {
                    if (_storage.TryGetValue(k, out var v)) dict[k] = v;
                }
                return Task.FromResult(dict);
            }

            public Task SaveBatchAsync(Dictionary<string, object> keyValues)
            {
                foreach (var kv in keyValues) _storage[kv.Key] = kv.Value;
                return Task.CompletedTask;
            }
        }

        // ── 1. BossDefinition Validation ────────────────────────────────────────

        [Test]
        public void BossDefinition_HasValidId_EnforcesCorrectNamingFormat()
        {
            var boss = ScriptableObject.CreateInstance<BossDefinition>();
            try
            {
                // Format chuẩn boss.floor_NN
                typeof(BossDefinition)
                    .GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(boss, "boss.floor_01");
                Assert.IsTrue(boss.HasValidId(), "boss.floor_01 phải hợp lệ");

                typeof(BossDefinition)
                    .GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(boss, "boss.floor_02");
                Assert.IsTrue(boss.HasValidId(), "boss.floor_02 phải hợp lệ");

                // Sai định dạng
                typeof(BossDefinition)
                    .GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(boss, "boss.1");
                Assert.IsFalse(boss.HasValidId(), "boss.1 không đủ 2 số phải không hợp lệ");

                typeof(BossDefinition)
                    .GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(boss, "TheGleamEyes");
                Assert.IsFalse(boss.HasValidId(), "Tên không theo chuẩn boss.floor_NN phải không hợp lệ");
            }
            finally
            {
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public void BossDefinition_HasTelegraphAndPunishWindow()
        {
            var boss = ScriptableObject.CreateInstance<BossDefinition>();
            try
            {
                Assert.Greater(boss.AttackWindupSeconds, 0f, "Phải có Telegraph windup > 0");
                Assert.Greater(boss.PunishWindowSeconds, 0f, "Phải có Punish window > 0");
                Assert.Greater(boss.MaxHealth, 0.0, "MaxHealth phải > 0");
                Assert.Greater(boss.Attack, 0.0, "Attack phải > 0");
            }
            finally
            {
                Object.DestroyImmediate(boss);
            }
        }

        // ── 2. First-Clear Reward Claim ─────────────────────────────────────────

        [Test]
        public async Task BossRewardHandler_FirstClearClaim_CanOnlyBeClaimedOnce()
        {
            var saveService = new InMemorySaveService();
            var rewardItem = MakeItem("item.boss_weapon", "Kiếm Bến Cạn");

            try
            {
                // Lần 1: Nhận thành công
                int granted = 0;
                FirstClearClaimResult firstClaim = await BossRewardHandler.ClaimFirstClearRewardAsync(
                    "boss.floor_01", rewardItem, 1, saveService,
                    (item, quantity) => { granted += quantity; return quantity; });
                Assert.AreEqual(FirstClearClaimResult.Claimed, firstClaim,
                    "Lần đầu hạ boss phải claim được First-Clear reward");
                Assert.AreEqual(1, granted, "Claim xác nhận xong mới cấp item.");

                // Kiểm tra data đã lưu
                var savedClaims = await saveService.LoadAsync<RewardClaimsData>(SaveKeys.RewardClaims);
                Assert.IsNotNull(savedClaims);
                Assert.IsTrue(savedClaims.HasClaim(BossRewardHandler.FirstClearType, "boss.floor_01"));

                // Lần 2 (đánh lại boss / rematch): Không được nhận lại bonus
                FirstClearClaimResult secondClaim = await BossRewardHandler.ClaimFirstClearRewardAsync(
                    "boss.floor_01", rewardItem, 1, saveService,
                    (item, quantity) => { granted += quantity; return quantity; });
                Assert.AreEqual(FirstClearClaimResult.AlreadyClaimed, secondClaim,
                    "Lần thứ 2 (rematch) không được nhận duplicate First-Clear reward");
                Assert.AreEqual(1, granted);
            }
            finally
            {
                Object.DestroyImmediate(rewardItem);
            }
        }

        private static ItemDefinition MakeItem(string id, string displayName)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            typeof(ItemDefinition)
                .GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(item, id);
            typeof(ItemDefinition)
                .GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(item, displayName);
            return item;
        }

        // ── 3. Permadeath Integration ───────────────────────────────────────────

        [Test]
        public void Permadeath_BossDamage_CreatesValidDeathRecord()
        {
            // Kiểm tra struct DeathRecord khi chết do Boss
            var record = new DeathRecord(
                DeathReason.Boss,
                "boss.floor_01",
                finalLevel: 5,
                finalHighestFloor: 1,
                totalPlayTimeSeconds: 120,
                deathTimeUnix: System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            Assert.AreEqual(DeathReason.Boss, record.Reason, "Lý do chết phải là Boss");
            Assert.AreEqual("boss.floor_01", record.SourceId, "SourceId phải là ID chuẩn của Boss");
            Assert.AreEqual(5, record.FinalLevel);
            Assert.AreEqual(1, record.FinalHighestFloor);
        }

        [TestCase("boss.floor_01", DeathReason.Boss)]
        [TestCase("boss.floor_123", DeathReason.Boss)]
        [TestCase("enemy.slime", DeathReason.Enemy)]
        [TestCase("boss.floor_xx", DeathReason.Enemy)]
        [TestCase(null, DeathReason.Trap)]
        public void ResolveDeathReason_ClassifiesBossEnemyAndTrap(string sourceId, DeathReason expected)
        {
            DamageInfo hit = sourceId == null ? null : new DamageInfo(1) { SourceId = sourceId };
            Assert.AreEqual(expected, PvEBootstrap.ResolveDeathReason(hit));
        }

        // ── 4. P8-3 authoritative flow ──────────────────────────────────────────

        private sealed class StrictSaveService : ISaveService, IAuthoritativeSaveService
        {
            public RewardClaimsData StoredClaims;
            public bool ThrowOnAuthoritativeLoad;
            public bool ThrowOnAuthoritativeSave;
            public int AuthoritativeLoadCount;
            public int AuthoritativeSaveCount;
            public readonly List<string> Calls = new List<string>();

            public Task<T> LoadAsync<T>(string key) => Task.FromResult(default(T));
            public Task SaveAsync<T>(string key, T value) => Task.CompletedTask;
            public Task DeleteAsync(string key) => Task.CompletedTask;
            public Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys) =>
                Task.FromResult(new Dictionary<string, object>());
            public Task SaveBatchAsync(Dictionary<string, object> keyValues) => Task.CompletedTask;

            public Task<T> LoadAuthoritativeAsync<T>(string key)
            {
                AuthoritativeLoadCount++;
                Calls.Add("load");
                if (ThrowOnAuthoritativeLoad) throw new SaveUnavailableException(key, new Exception("cloud read failed"));
                if (key == SaveKeys.RewardClaims && StoredClaims != null)
                    return Task.FromResult((T)(object)StoredClaims);
                return Task.FromResult(default(T));
            }

            public Task SaveAuthoritativeAsync<T>(string key, T value)
            {
                AuthoritativeSaveCount++;
                Calls.Add("save");
                if (ThrowOnAuthoritativeSave) throw new SaveUnavailableException(key, new Exception("cloud write failed"));
                if (key == SaveKeys.RewardClaims) StoredClaims = value as RewardClaimsData;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeWorldStateService : IWorldStateService
        {
            public BossStatus CurrentBossStatus = BossStatus.Active;
            public int ReportCalls;
            public int ReportFailuresRemaining;
            public bool ThrowOnRead;

            public Task<Dictionary<string, FloorStatus>> GetAllFloorStatusesAsync() =>
                Task.FromResult(new Dictionary<string, FloorStatus>());
            public Task<FloorStatus> GetFloorStatusAsync(string floorId) => Task.FromResult(FloorStatus.Locked);
            public Task<Dictionary<string, BossStatus>> GetAllBossStatusesAsync() =>
                Task.FromResult(new Dictionary<string, BossStatus>());
            public Task<WorldTime> GetWorldTimeAsync() => Task.FromResult(new WorldTime());

            public Task<BossStatus> GetBossStatusAsync(string bossId)
            {
                if (ThrowOnRead) throw new Exception("world read failed");
                return Task.FromResult(CurrentBossStatus);
            }

            public Task ReportBossDefeated(string bossId, string floorId)
            {
                ReportCalls++;
                if (ReportFailuresRemaining > 0)
                {
                    ReportFailuresRemaining--;
                    throw new Exception("server unavailable");
                }
                CurrentBossStatus = BossStatus.Defeated;
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task FirstClear_AuthoritativeClaimIsSavedBeforeGrant_AndLoadFailureDoesNotOverwrite()
        {
            ItemDefinition reward = MakeItem("item.first_clear", "Phần thưởng đầu");
            try
            {
                var save = new StrictSaveService();
                var order = save.Calls;
                int granted = 0;

                FirstClearClaimResult result = await BossRewardHandler.ClaimFirstClearRewardAsync(
                    "boss.floor_01", reward, 2, save,
                    (item, quantity) =>
                    {
                        order.Add("grant");
                        granted += quantity;
                        return quantity;
                    });

                Assert.AreEqual(FirstClearClaimResult.Claimed, result);
                CollectionAssert.AreEqual(new[] { "load", "save", "grant" }, order,
                    "Claim phải được source chính xác nhận trước khi cấp item.");
                Assert.AreEqual(2, granted);

                save.StoredClaims = new RewardClaimsData();
                save.StoredClaims.AddClaim("Other", "boss.floor_99", 123);
                save.ThrowOnAuthoritativeLoad = true;
                int saveCountBefore = save.AuthoritativeSaveCount;
                int grantsBefore = granted;

                result = await BossRewardHandler.ClaimFirstClearRewardAsync(
                    "boss.floor_01", reward, 1, save,
                    (item, quantity) => { granted += quantity; return quantity; });

                Assert.AreEqual(FirstClearClaimResult.Unconfirmed, result);
                Assert.AreEqual(saveCountBefore, save.AuthoritativeSaveCount,
                    "Đọc claim lỗi thì không được ghi object rỗng đè claim cũ.");
                Assert.IsTrue(save.StoredClaims.HasClaim("Other", "boss.floor_99"));
                Assert.IsFalse(save.StoredClaims.HasClaim(BossRewardHandler.FirstClearType, "boss.floor_01"));
                Assert.AreEqual(grantsBefore, granted, "Không cấp item khi claim chưa xác nhận.");
            }
            finally
            {
                Object.DestroyImmediate(reward);
            }
        }

        [Test]
        public async Task Rematch_DoesNotReportWorldBoss_MainDropsRemain_RareChanceUsesPointZeroFive()
        {
            ItemDefinition main = MakeItem("item.main", "Main");
            ItemDefinition rare = MakeItem("item.rare", "Rare");
            BossDefinition boss = MakeBoss(main, rare);
            try
            {
                var world = new FakeWorldStateService { CurrentBossStatus = BossStatus.Defeated };
                var flow = new BossDefeatFlow(world, new StrictSaveService());
                int granted = 0;

                BossDefeatFlowResult result = await flow.HandleDefeatAsync(
                    boss, isPlayerDead: false,
                    (item, quantity) => { granted += quantity; return quantity; });

                Assert.IsTrue(result.IsRematch);
                Assert.AreEqual(BossDefeatReportStatus.NotRequired, result.ReportStatus);
                Assert.AreEqual(0, world.ReportCalls, "Rematch không được gọi ReportBossDefeated.");
                Assert.GreaterOrEqual(granted, 1, "Main drop vẫn rơi khi rematch.");
                Assert.AreEqual(1, BossRewardHandler.RollRareDrops(boss, isRematch: true, randomRoll: 0.04f).Count);
                Assert.AreEqual(0, BossRewardHandler.RollRareDrops(boss, isRematch: true, randomRoll: 0.06f).Count,
                    "Rare chance 1.0 × rematch multiplier mặc định 0.05 phải trượt ở roll 0.06.");
            }
            finally
            {
                Object.DestroyImmediate(main);
                Object.DestroyImmediate(rare);
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public async Task ReportFailure_LeavesRewardsIndependent_AndRetryConfirmsWithoutRepeatingFlow()
        {
            ItemDefinition main = MakeItem("item.main", "Main");
            BossDefinition boss = MakeBoss(main, rare: null);
            try
            {
                var world = new FakeWorldStateService { ReportFailuresRemaining = 1 };
                var flow = new BossDefeatFlow(world, new StrictSaveService());
                int granted = 0;

                BossDefeatFlowResult first = await flow.HandleDefeatAsync(
                    boss, isPlayerDead: false,
                    (item, quantity) => { granted += quantity; return quantity; });

                Assert.AreEqual(BossDefeatReportStatus.Failed, first.ReportStatus);
                Assert.AreEqual(1, world.ReportCalls);
                Assert.GreaterOrEqual(granted, 1, "Report lỗi không làm mất main drop.");
                int grantsAfterDefeat = granted;

                var retry = await flow.RetryDefeatReportAsync(boss);
                Assert.AreEqual(BossDefeatReportStatus.Confirmed, retry.status);
                Assert.AreEqual(2, world.ReportCalls);
                Assert.AreEqual(grantsAfterDefeat, granted, "Retry chỉ report, không cấp/roll item lần nữa.");
            }
            finally
            {
                Object.DestroyImmediate(main);
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public async Task BossController_ReportFailureRaisesRetryEvents_WithoutRepeatingRewards()
        {
            ItemDefinition main = MakeItem("item.main", "Main");
            BossDefinition boss = MakeBoss(main, rare: null);
            var gameObject = new GameObject("BossControllerReportTest");
            gameObject.AddComponent<Rigidbody2D>();
            var controller = gameObject.AddComponent<BossController>();
            try
            {
                var world = new FakeWorldStateService { ReportFailuresRemaining = 1 };
                controller.SetDefinition(boss);
                controller.SetServices(world, new StrictSaveService());

                int failureEvents = 0;
                int confirmedEvents = 0;
                controller.DefeatReportFailed += _ => failureEvents++;
                controller.DefeatReportConfirmed += () => confirmedEvents++;

                await controller.HandleDefeatAsync();
                Assert.AreEqual(1, failureEvents);
                Assert.AreEqual(0, confirmedEvents);
                Assert.AreEqual(1, world.ReportCalls);

                await controller.RetryDefeatReportAsync();
                Assert.AreEqual(1, failureEvents, "Retry thành công không phát lỗi cũ lần nữa.");
                Assert.AreEqual(1, confirmedEvents);
                Assert.AreEqual(2, world.ReportCalls);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(main);
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public async Task PlayerAlreadyDead_WhenBossFalls_ReportsWorldButDoesNotClaimOrGrant()
        {
            ItemDefinition main = MakeItem("item.main", "Main");
            ItemDefinition reward = MakeItem("item.first", "First");
            BossDefinition boss = MakeBoss(main, rare: null, firstClear: reward);
            try
            {
                var world = new FakeWorldStateService();
                var save = new StrictSaveService();
                var flow = new BossDefeatFlow(world, save);
                int granted = 0;

                BossDefeatFlowResult result = await flow.HandleDefeatAsync(
                    boss, isPlayerDead: true,
                    (item, quantity) => { granted += quantity; return quantity; });

                Assert.AreEqual(BossDefeatReportStatus.Confirmed, result.ReportStatus);
                Assert.AreEqual(1, world.ReportCalls, "Boss thật sự gục vẫn phải báo World State.");
                Assert.AreEqual(0, save.AuthoritativeLoadCount, "Account đã chết không được xét First-Clear.");
                Assert.AreEqual(0, granted, "Account đã chết không được nhận bonus/drop.");
            }
            finally
            {
                Object.DestroyImmediate(main);
                Object.DestroyImmediate(reward);
                Object.DestroyImmediate(boss);
            }
        }

        private static BossDefinition MakeBoss(ItemDefinition main, ItemDefinition rare, ItemDefinition firstClear = null)
        {
            var boss = ScriptableObject.CreateInstance<BossDefinition>();
            SetPrivate(boss, "id", "boss.floor_01");
            SetPrivate(boss, "floorId", "floor.01");
            SetPrivate(boss, "firstClearRewardItem", firstClear);
            SetPrivate(boss, "firstClearRewardQuantity", 1);
            SetPrivate(boss, "rematchRareMultiplier", 0.05f);
            SetPrivate(boss, "drops", main == null
                ? new List<BossDefinition.BossDrop>()
                : new List<BossDefinition.BossDrop>
                {
                    new BossDefinition.BossDrop { item = main, chance = 1f, minCount = 1, maxCount = 1 }
                });
            SetPrivate(boss, "rareDrops", rare == null
                ? new List<BossDefinition.BossDrop>()
                : new List<BossDefinition.BossDrop>
                {
                    new BossDefinition.BossDrop { item = rare, chance = 1f, minCount = 1, maxCount = 1 }
                });
            return boss;
        }

        private static void SetPrivate<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, "Thiếu field test: " + fieldName);
            field.SetValue(target, value);
        }
    }
}
