using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Game.Core.Floor;
using Game.Core.Network;
using Game.Core.World;
using Game.Services.Network;
using Game.Services.World;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task P8-2 — "Boss đã bị hạ" phải được SERVER xác nhận (docs/02 §22, docs/03 §6, docs/12 §7.2).
    ///
    /// Mô hình test: MỘT kho cloud giả dùng chung (<see cref="FakeCloud"/> = Game Data "world")
    /// + mỗi account có <see cref="WorldStateService"/> RIÊNG và một server giả riêng, cùng ghi/đọc kho đó.
    /// Nhờ vậy account B chỉ thấy thay đổi của A khi dữ liệu thật sự đi qua "cloud".
    ///
    /// Không test nào gọi SDK/Cloud Code thật: seam <c>LoadWorldStateFromCloudAsync</c> (WorldStateService)
    /// và <c>InvokeReportBossScriptAsync</c> (UGSServerFunctionService) được ghi đè.
    /// </summary>
    public class WorldStateServerConfirmationTests
    {
        private const string Boss1 = "boss.floor_01";
        private const string Floor1 = "floor.01";
        private const string Floor2 = "floor.02";

        // ── Kho cloud giả dùng chung (Game Data, customId "world") ─────────────────────────

        private sealed class FakeCloud
        {
            public readonly Dictionary<string, string> Floors = new Dictionary<string, string>();
            public readonly Dictionary<string, string> Bosses = new Dictionary<string, string>();

            public WorldStateService.CloudWorldData Snapshot()
            {
                return new WorldStateService.CloudWorldData
                {
                    Floors = new Dictionary<string, string>(Floors),
                    Bosses = new Dictionary<string, string>(Bosses)
                };
            }
        }

        // ── Server giả: mô phỏng đúng hợp đồng ReportBossDefeated.js (P8-1) ─────────────────

        private sealed class FakeServer : IServerFunctionService
        {
            private readonly FakeCloud _cloud;

            public int CallCount;
            public int FailuresRemaining;                 // N lần gọi đầu sẽ lỗi (mất mạng)
            public string NextFloorOverride;              // ép server trả tầng kế khác "+1"
            public Func<string, string, BossDefeatResult> ResultFactory; // ép trả kết quả lạ (hoặc null)
            public BossDefeatResult LastResult;

            public FakeServer(FakeCloud cloud) { _cloud = cloud; }

            public Task<BossDefeatResult> ReportBossDefeatedAsync(string bossId, string floorId)
            {
                CallCount++;
                if (FailuresRemaining > 0)
                {
                    FailuresRemaining--;
                    return Task.FromException<BossDefeatResult>(
                        new ServerFunctionUnavailableException("ReportBossDefeated", "giả lập mất mạng"));
                }

                if (ResultFactory != null)
                {
                    LastResult = ResultFactory(bossId, floorId);
                    return Task.FromResult(LastResult);
                }

                bool already = _cloud.Bosses.TryGetValue(bossId, out var bs) && bs == "DEFEATED";
                string next = NextFloorOverride ?? NextFloorOf(floorId);

                _cloud.Bosses[bossId] = "DEFEATED";
                _cloud.Floors[floorId] = "CLEARED";
                if (!(_cloud.Floors.TryGetValue(next, out var ns) && ns == "CLEARED"))
                    _cloud.Floors[next] = "AVAILABLE";

                LastResult = new BossDefeatResult(already, bossId, floorId, next);
                return Task.FromResult(LastResult);
            }

            public Task<Game.Core.Time.WorldTimeSnapshot> GetWorldTimeSnapshotAsync()
                => Task.FromResult<Game.Core.Time.WorldTimeSnapshot>(null);
        }

        // ── WorldStateService test: đọc từ FakeCloud thay cho Cloud Save ───────────────────

        private sealed class TestWorldStateService : WorldStateService
        {
            private readonly FakeCloud _cloud;
            public bool CloudReadable = true;   // false: như chưa đọc được → trả null, chỉ dùng cache
            public bool CloudReadThrows;        // true: đọc cloud ném lỗi

            public TestWorldStateService(FakeCloud cloud, IServerFunctionService server) : base(server)
            {
                _cloud = cloud;
            }

            protected override Task<CloudWorldData> LoadWorldStateFromCloudAsync()
            {
                if (CloudReadThrows)
                    return Task.FromException<CloudWorldData>(new InvalidOperationException("cloud down"));
                if (!CloudReadable)
                    return Task.FromResult<CloudWorldData>(null);
                return Task.FromResult(_cloud.Snapshot());
            }
        }

        private sealed class Account
        {
            public readonly FakeServer Server;
            public readonly TestWorldStateService World;

            public Account(FakeCloud cloud)
            {
                Server = new FakeServer(cloud);
                World = new TestWorldStateService(cloud, Server);
            }
        }

        // ── FloorDatabase tối thiểu: Floor 1 mở sẵn, Floor 2 khóa bởi boss.floor_01 ───────────

        private sealed class FloorFixture : IDisposable
        {
            public readonly FloorDatabase Database;
            private readonly FloorDefinition _f1;
            private readonly FloorDefinition _f2;

            public FloorFixture()
            {
                Database = ScriptableObject.CreateInstance<FloorDatabase>();
                _f1 = ScriptableObject.CreateInstance<FloorDefinition>();
                _f1.id = Floor1;
                _f1.floorNumber = 1;
                _f1.sceneKey = "Floor01_Scene";
                _f1.requiredBossDefeatedId = "";

                _f2 = ScriptableObject.CreateInstance<FloorDefinition>();
                _f2.id = Floor2;
                _f2.floorNumber = 2;
                _f2.sceneKey = "Floor02_Scene";
                _f2.requiredBossDefeatedId = Boss1;

                Database.floors = new List<FloorDefinition> { _f1, _f2 };
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(_f1);
                UnityEngine.Object.DestroyImmediate(_f2);
                UnityEngine.Object.DestroyImmediate(Database);
            }
        }

        private static string NextFloorOf(string floorId)
        {
            var m = Regex.Match(floorId, @"^floor\.(\d{2,})$");
            int n = int.Parse(m.Groups[1].Value) + 1;
            return "floor." + n.ToString("D" + Math.Max(2, m.Groups[1].Value.Length));
        }

        private static async Task AssertWorldIsPristine(WorldStateService world, string because)
        {
            var floors = await world.GetAllFloorStatusesAsync();
            var bosses = await world.GetAllBossStatusesAsync();
            Assert.AreEqual(1, floors.Count, because + " — chỉ có tầng mở mặc định.");
            Assert.AreEqual(FloorStatus.Available, floors[Floor1], because);
            Assert.AreEqual(FloorStatus.Locked, await world.GetFloorStatusAsync(Floor2), because);
            Assert.AreEqual(BossStatus.Active, bosses[Boss1], because);
            Assert.AreEqual(BossStatus.Active, await world.GetBossStatusAsync(Boss1), because);
        }

        // ══════════════════════════════════════════════════════════════════════════════════
        // (a) Hai account: A báo → B (instance mới) thấy kết quả qua "cloud"
        // ══════════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task TwoAccounts_AReportsConfirmed_NewInstanceOfBSeesBossDefeated_Floor01Cleared_Floor02Available()
        {
            var cloud = new FakeCloud();
            var a = new Account(cloud);
            var bBefore = new Account(cloud);

            // Trước khi A báo: B thấy tầng 2 khóa, boss còn Active.
            Assert.AreEqual(FloorStatus.Locked, await bBefore.World.GetFloorStatusAsync(Floor2));
            Assert.AreEqual(BossStatus.Active, await bBefore.World.GetBossStatusAsync(Boss1));

            await a.World.ReportBossDefeated(Boss1, Floor1);

            Assert.AreEqual(1, a.Server.CallCount, "Phải hỏi server đúng 1 lần.");
            Assert.IsFalse(a.Server.LastResult.AlreadyDefeated, "Lần đầu boss chưa Defeated.");

            // B là instance MỚI (cache sạch) → chỉ có thể biết qua dữ liệu trong cloud.
            var bAfter = new Account(cloud);
            Assert.AreEqual(BossStatus.Defeated, await bAfter.World.GetBossStatusAsync(Boss1));
            Assert.AreEqual(FloorStatus.Cleared, await bAfter.World.GetFloorStatusAsync(Floor1));
            Assert.AreEqual(FloorStatus.Available, await bAfter.World.GetFloorStatusAsync(Floor2));
            Assert.AreEqual(0, bAfter.Server.CallCount, "B không gọi server ghi — chỉ đọc World State.");

            // Instance B cũ (đã tồn tại trước đó) cũng thấy sau lần refresh kế tiếp.
            Assert.AreEqual(FloorStatus.Available, await bBefore.World.GetFloorStatusAsync(Floor2));

            // Chính A cũng thấy đúng.
            Assert.AreEqual(BossStatus.Defeated, await a.World.GetBossStatusAsync(Boss1));
            Assert.AreEqual(FloorStatus.Cleared, await a.World.GetFloorStatusAsync(Floor1));
            Assert.AreEqual(FloorStatus.Available, await a.World.GetFloorStatusAsync(Floor2));
        }

        [Test]
        public async Task Report_Success_UpdatesCacheFromServerResponse_EvenWhenCloudNotReadable()
        {
            var cloud = new FakeCloud();
            var a = new Account(cloud);
            a.World.CloudReadable = false; // chỉ còn cache → kiểm đúng cache được cập nhật từ response

            await a.World.ReportBossDefeated(Boss1, Floor1);

            Assert.AreEqual(BossStatus.Defeated, await a.World.GetBossStatusAsync(Boss1));
            Assert.AreEqual(FloorStatus.Cleared, await a.World.GetFloorStatusAsync(Floor1));
            Assert.AreEqual(FloorStatus.Available, await a.World.GetFloorStatusAsync(Floor2));
        }

        [Test]
        public async Task Report_UsesNextFloorIdFromServer_NotAClientGuess()
        {
            var cloud = new FakeCloud();
            var a = new Account(cloud);
            a.World.CloudReadable = false;
            a.Server.NextFloorOverride = "floor.07"; // server (data) quyết định tầng kế, không phải "+1"

            await a.World.ReportBossDefeated(Boss1, Floor1);

            Assert.AreEqual(FloorStatus.Available, await a.World.GetFloorStatusAsync("floor.07"));
            Assert.AreEqual(FloorStatus.Locked, await a.World.GetFloorStatusAsync(Floor2),
                "Client không được tự đoán floor.02 = floor.01 + 1.");
        }

        // ══════════════════════════════════════════════════════════════════════════════════
        // (b) Server lỗi → A ném, cache A không đổi, B vẫn thấy floor.02 Locked
        // ══════════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ServerFails_AThrows_CacheOfAUnchanged_BStillSeesFloor02Locked()
        {
            var cloud = new FakeCloud();
            var a = new Account(cloud);
            a.Server.FailuresRemaining = 1;

            var ex = Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await a.World.ReportBossDefeated(Boss1, Floor1));
            Assert.AreEqual("ReportBossDefeated", ex.FunctionName);
            Assert.AreEqual(1, a.Server.CallCount);

            // Tắt đọc cloud để chỉ kiểm CACHE của A: không được có dấu vết "đã hạ" nào.
            a.World.CloudReadable = false;
            await AssertWorldIsPristine(a.World, "Cache của A sau khi server lỗi");

            // Cloud không bị ghi → B (instance mới) vẫn thấy tầng 2 khóa.
            Assert.AreEqual(0, cloud.Bosses.Count);
            Assert.AreEqual(0, cloud.Floors.Count);
            var b = new Account(cloud);
            await AssertWorldIsPristine(b.World, "B sau khi server của A lỗi");
        }

        [Test]
        public async Task ServerThrowsOtherException_IsRethrown_CacheUnchanged()
        {
            var cloud = new FakeCloud();
            var a = new Account(cloud);
            a.World.CloudReadable = false;
            a.Server.ResultFactory = (b, f) => throw new TimeoutException("hết giờ");

            Assert.ThrowsAsync<TimeoutException>(async () => await a.World.ReportBossDefeated(Boss1, Floor1));
            await AssertWorldIsPristine(a.World, "Cache sau exception bất kỳ từ server");
        }

        [Test]
        public async Task NoServerFunctionService_Throws_AndNeverReportsFakeSuccess()
        {
            var world = new WorldStateService(); // không có IServerFunctionService

            var ex = Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await world.ReportBossDefeated(Boss1, Floor1));
            Assert.AreEqual("ReportBossDefeated", ex.FunctionName);

            Assert.AreEqual(FloorStatus.Locked, await world.GetFloorStatusAsync(Floor2));
            Assert.AreEqual(BossStatus.Active, await world.GetBossStatusAsync(Boss1));
            Assert.AreEqual(FloorStatus.Available, await world.GetFloorStatusAsync(Floor1));
        }

        [Test]
        public async Task ServerReturnsNullOrMismatchedResult_Throws_CacheUnchanged()
        {
            var cloud = new FakeCloud();
            var a = new Account(cloud);
            a.World.CloudReadable = false;

            a.Server.ResultFactory = (b, f) => null;
            Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await a.World.ReportBossDefeated(Boss1, Floor1), "kết quả null");

            a.Server.ResultFactory = (b, f) => new BossDefeatResult(false, "boss.floor_09", f, Floor2);
            Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await a.World.ReportBossDefeated(Boss1, Floor1), "bossId không khớp");

            a.Server.ResultFactory = (b, f) => new BossDefeatResult(false, b, "floor.09", Floor2);
            Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await a.World.ReportBossDefeated(Boss1, Floor1), "floorId không khớp");

            a.Server.ResultFactory = (b, f) => new BossDefeatResult(false, b, f, "");
            Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await a.World.ReportBossDefeated(Boss1, Floor1), "thiếu nextFloorId");

            await AssertWorldIsPristine(a.World, "Cache sau các kết quả không hợp lệ");
        }

        [Test]
        public void EmptyIds_ThrowArgumentException_AndNeverReachServer()
        {
            var cloud = new FakeCloud();
            var a = new Account(cloud);

            Assert.ThrowsAsync<ArgumentException>(async () => await a.World.ReportBossDefeated("", Floor1));
            Assert.ThrowsAsync<ArgumentException>(async () => await a.World.ReportBossDefeated(Boss1, null));
            Assert.AreEqual(0, a.Server.CallCount);
        }

        // ══════════════════════════════════════════════════════════════════════════════════
        // (c) Retry sau lỗi thì thành công
        // ══════════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task RetryAfterFailure_Succeeds_AndBSeesTheResult()
        {
            var cloud = new FakeCloud();
            var a = new Account(cloud);
            a.Server.FailuresRemaining = 1;

            Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await a.World.ReportBossDefeated(Boss1, Floor1));

            await a.World.ReportBossDefeated(Boss1, Floor1); // thử lại → thành công

            Assert.AreEqual(2, a.Server.CallCount);
            Assert.AreEqual(BossStatus.Defeated, await a.World.GetBossStatusAsync(Boss1));
            Assert.AreEqual(FloorStatus.Available, await a.World.GetFloorStatusAsync(Floor2));

            var b = new Account(cloud);
            Assert.AreEqual(BossStatus.Defeated, await b.World.GetBossStatusAsync(Boss1));
            Assert.AreEqual(FloorStatus.Cleared, await b.World.GetFloorStatusAsync(Floor1));
            Assert.AreEqual(FloorStatus.Available, await b.World.GetFloorStatusAsync(Floor2));
        }

        // ══════════════════════════════════════════════════════════════════════════════════
        // (d) Báo hai lần: không lỗi, kết quả như nhau, vẫn hỏi server
        // ══════════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task ReportTwice_NoError_SameOutcome_ServerAskedBothTimes()
        {
            var cloud = new FakeCloud();
            var a = new Account(cloud);

            await a.World.ReportBossDefeated(Boss1, Floor1);
            var floorsAfterFirst = await a.World.GetAllFloorStatusesAsync();
            Assert.IsFalse(a.Server.LastResult.AlreadyDefeated);

            Assert.DoesNotThrowAsync(async () => await a.World.ReportBossDefeated(Boss1, Floor1));

            Assert.AreEqual(2, a.Server.CallCount, "Khi đã Defeated vẫn hỏi server để server tự sửa trạng thái lệch.");
            Assert.IsTrue(a.Server.LastResult.AlreadyDefeated, "Lần hai server báo alreadyDefeated.");

            var floorsAfterSecond = await a.World.GetAllFloorStatusesAsync();
            CollectionAssert.AreEquivalent(floorsAfterFirst, floorsAfterSecond);
            Assert.AreEqual(BossStatus.Defeated, await a.World.GetBossStatusAsync(Boss1));
            Assert.AreEqual(FloorStatus.Cleared, await a.World.GetFloorStatusAsync(Floor1));
            Assert.AreEqual(FloorStatus.Available, await a.World.GetFloorStatusAsync(Floor2));
        }

        [Test]
        public async Task Report_NeverDowngradesAClearedNextFloor()
        {
            var cloud = new FakeCloud();
            cloud.Floors[Floor2] = "CLEARED"; // tầng kế đã Cleared từ trước
            var a = new Account(cloud);
            await a.World.GetAllFloorStatusesAsync(); // nạp cache từ cloud
            Assert.AreEqual(FloorStatus.Cleared, await a.World.GetFloorStatusAsync(Floor2));

            a.World.CloudReadable = false; // chỉ kiểm logic cập nhật cache
            await a.World.ReportBossDefeated(Boss1, Floor1);

            Assert.AreEqual(FloorStatus.Cleared, await a.World.GetFloorStatusAsync(Floor2),
                "Cleared không bao giờ bị hạ xuống Available.");
        }

        // ══════════════════════════════════════════════════════════════════════════════════
        // (e) FloorLoader của B: load được floor.02 sau (a), bị Locked sau (b)
        // ══════════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task FloorLoader_AccountB_LoadsFloor02_AfterAConfirmed()
        {
            using var floors = new FloorFixture();
            var cloud = new FakeCloud();

            var bBefore = new Account(cloud);
            var loaderBefore = new Game.PvE.Floor.FloorLoader(bBefore.World, floors.Database, accountIsDead: false);
            Assert.AreEqual(FloorLoadStatus.Locked, (await loaderBefore.LoadFloorAsync(Floor2)).Status,
                "Floor 2 phải khóa trước khi boss bị hạ.");

            var a = new Account(cloud);
            await a.World.ReportBossDefeated(Boss1, Floor1);

            var bAfter = new Account(cloud); // account B, instance mới
            var loaderAfter = new Game.PvE.Floor.FloorLoader(bAfter.World, floors.Database, accountIsDead: false);
            var result = await loaderAfter.LoadFloorAsync(Floor2);

            Assert.AreEqual(FloorLoadStatus.Loaded, result.Status);
            Assert.AreEqual(Floor2, loaderAfter.CurrentFloorId);
        }

        [Test]
        public async Task FloorLoader_AccountB_Floor02StaysLocked_WhenAServerFailed()
        {
            using var floors = new FloorFixture();
            var cloud = new FakeCloud();

            var a = new Account(cloud);
            a.Server.FailuresRemaining = 1;
            Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await a.World.ReportBossDefeated(Boss1, Floor1));

            var b = new Account(cloud);
            var loader = new Game.PvE.Floor.FloorLoader(b.World, floors.Database, accountIsDead: false);
            var result = await loader.LoadFloorAsync(Floor2);

            Assert.AreEqual(FloorLoadStatus.Locked, result.Status);
            Assert.IsNull(loader.CurrentFloorId);
        }

        // ══════════════════════════════════════════════════════════════════════════════════
        // Đọc World State: lỗi đọc cloud vẫn fail-closed
        // ══════════════════════════════════════════════════════════════════════════════════

        [Test]
        public async Task CloudReadFails_StaysFailClosed_NextFloorLocked_DefaultFloorStillAvailable()
        {
            var cloud = new FakeCloud();
            cloud.Bosses[Boss1] = "DEFEATED";
            cloud.Floors[Floor1] = "CLEARED";
            cloud.Floors[Floor2] = "AVAILABLE";

            var b = new Account(cloud);
            b.World.CloudReadThrows = true; // đọc lỗi → không được tự mở tầng nào

            Assert.AreEqual(FloorStatus.Locked, await b.World.GetFloorStatusAsync(Floor2));
            Assert.AreEqual(BossStatus.Active, await b.World.GetBossStatusAsync(Boss1));
            Assert.AreEqual(FloorStatus.Available, await b.World.GetFloorStatusAsync(Floor1));

            // Cloud hồi lại → thấy đúng.
            b.World.CloudReadThrows = false;
            Assert.AreEqual(FloorStatus.Available, await b.World.GetFloorStatusAsync(Floor2));
        }

        [Test]
        public async Task CloudReadFailsLater_KeepsLastConfirmedCache()
        {
            var cloud = new FakeCloud();
            var a = new Account(cloud);
            await a.World.ReportBossDefeated(Boss1, Floor1);

            a.World.CloudReadThrows = true;
            Assert.AreEqual(FloorStatus.Available, await a.World.GetFloorStatusAsync(Floor2));
            Assert.AreEqual(BossStatus.Defeated, await a.World.GetBossStatusAsync(Boss1));
        }

        // ══════════════════════════════════════════════════════════════════════════════════
        // (f) UGSServerFunctionService: response sai hợp đồng / lỗi gọi → NÉM (không nuốt, không "thành công giả")
        // ══════════════════════════════════════════════════════════════════════════════════

        private sealed class ScriptedUgsServerFunctionService : UGSServerFunctionService
        {
            public Func<Task<Dictionary<string, object>>> Script;
            public int InvokeCount;

            protected override Task<Dictionary<string, object>> InvokeReportBossScriptAsync(string bossId, string floorId)
            {
                InvokeCount++;
                return Script();
            }
        }

        private static Dictionary<string, object> ValidResponse(bool alreadyDefeated = false)
        {
            return new Dictionary<string, object>
            {
                { "ok", true },
                { "alreadyDefeated", alreadyDefeated },
                { "bossId", Boss1 },
                { "floorId", Floor1 },
                { "bossStatus", "DEFEATED" },
                { "floorStatus", "CLEARED" },
                { "nextFloorId", Floor2 }
            };
        }

        private static Dictionary<string, object> Without(string key)
        {
            var d = ValidResponse();
            d.Remove(key);
            return d;
        }

        private static Dictionary<string, object> With(string key, object value)
        {
            var d = ValidResponse();
            d[key] = value;
            return d;
        }

        [Test]
        public async Task Ugs_ValidResponse_ReturnsConfirmedResult()
        {
            var svc = new ScriptedUgsServerFunctionService { Script = () => Task.FromResult(ValidResponse(alreadyDefeated: true)) };

            var result = await svc.ReportBossDefeatedAsync(Boss1, Floor1);

            Assert.IsTrue(result.AlreadyDefeated);
            Assert.AreEqual(Boss1, result.BossId);
            Assert.AreEqual(Floor1, result.FloorId);
            Assert.AreEqual(Floor2, result.NextFloorId);
            Assert.AreEqual(1, svc.InvokeCount);
        }

        [Test]
        public void Ugs_InvalidResponses_AllThrow_ServerFunctionUnavailable()
        {
            var cases = new List<KeyValuePair<string, Dictionary<string, object>>>
            {
                new KeyValuePair<string, Dictionary<string, object>>("response null", null),
                new KeyValuePair<string, Dictionary<string, object>>("ok=false", With("ok", false)),
                new KeyValuePair<string, Dictionary<string, object>>("thiếu ok", Without("ok")),
                new KeyValuePair<string, Dictionary<string, object>>("ok là chuỗi 'true'", With("ok", "true")),
                new KeyValuePair<string, Dictionary<string, object>>("ok = null", With("ok", null)),
                new KeyValuePair<string, Dictionary<string, object>>("thiếu alreadyDefeated", Without("alreadyDefeated")),
                new KeyValuePair<string, Dictionary<string, object>>("thiếu bossId", Without("bossId")),
                new KeyValuePair<string, Dictionary<string, object>>("thiếu floorId", Without("floorId")),
                new KeyValuePair<string, Dictionary<string, object>>("thiếu bossStatus", Without("bossStatus")),
                new KeyValuePair<string, Dictionary<string, object>>("thiếu floorStatus", Without("floorStatus")),
                new KeyValuePair<string, Dictionary<string, object>>("thiếu nextFloorId", Without("nextFloorId")),
                new KeyValuePair<string, Dictionary<string, object>>("bossId không khớp", With("bossId", "boss.floor_09")),
                new KeyValuePair<string, Dictionary<string, object>>("floorId không khớp", With("floorId", "floor.09")),
                new KeyValuePair<string, Dictionary<string, object>>("bossStatus != DEFEATED", With("bossStatus", "ACTIVE")),
                new KeyValuePair<string, Dictionary<string, object>>("floorStatus != CLEARED", With("floorStatus", "AVAILABLE")),
                new KeyValuePair<string, Dictionary<string, object>>("nextFloorId sai dạng", With("nextFloorId", "floor.2")),
                new KeyValuePair<string, Dictionary<string, object>>("nextFloorId rỗng", With("nextFloorId", "")),
                new KeyValuePair<string, Dictionary<string, object>>("response rỗng {}", new Dictionary<string, object>()),
            };

            foreach (var c in cases)
            {
                var response = c.Value;
                var svc = new ScriptedUgsServerFunctionService { Script = () => Task.FromResult(response) };

                Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                    async () => await svc.ReportBossDefeatedAsync(Boss1, Floor1), "Phải ném khi: " + c.Key);
                Assert.AreEqual(1, svc.InvokeCount, c.Key);
            }
        }

        [Test]
        public void Ugs_ScriptCallFails_IsWrapped_NotSwallowed_KeepsInnerException()
        {
            var inner = new InvalidOperationException("mất mạng");
            var asyncFail = new ScriptedUgsServerFunctionService { Script = () => Task.FromException<Dictionary<string, object>>(inner) };
            var syncFail = new ScriptedUgsServerFunctionService { Script = () => throw inner };

            var ex1 = Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await asyncFail.ReportBossDefeatedAsync(Boss1, Floor1));
            var ex2 = Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await syncFail.ReportBossDefeatedAsync(Boss1, Floor1));

            Assert.AreSame(inner, ex1.InnerException);
            Assert.AreSame(inner, ex2.InnerException);
            Assert.AreEqual("ReportBossDefeated", ex1.FunctionName);
        }

        [Test]
        public void Ugs_ServerFunctionUnavailableFromSeam_PassesThroughUnchanged()
        {
            var original = new ServerFunctionUnavailableException("ReportBossDefeated", "CloudCodeService null");
            var svc = new ScriptedUgsServerFunctionService { Script = () => throw original };

            var ex = Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await svc.ReportBossDefeatedAsync(Boss1, Floor1));

            Assert.AreSame(original, ex);
        }

        // ── Đường đi đầy đủ: WorldStateService ← UGSServerFunctionService (parser thật) ───────

        [Test]
        public async Task EndToEnd_WorldStateService_WithUgsParser_AppliesOnlyValidatedResponse()
        {
            var cloud = new FakeCloud();

            // Response hợp lệ → cache cập nhật theo response.
            var okSvc = new ScriptedUgsServerFunctionService { Script = () => Task.FromResult(ValidResponse()) };
            var worldOk = new TestWorldStateService(cloud, okSvc) { CloudReadable = false };
            await worldOk.ReportBossDefeated(Boss1, Floor1);
            Assert.AreEqual(BossStatus.Defeated, await worldOk.GetBossStatusAsync(Boss1));
            Assert.AreEqual(FloorStatus.Cleared, await worldOk.GetFloorStatusAsync(Floor1));
            Assert.AreEqual(FloorStatus.Available, await worldOk.GetFloorStatusAsync(Floor2));

            // Response sai (ok=false) → ném và cache nguyên vẹn.
            var badSvc = new ScriptedUgsServerFunctionService { Script = () => Task.FromResult(With("ok", false)) };
            var worldBad = new TestWorldStateService(cloud, badSvc) { CloudReadable = false };
            Assert.ThrowsAsync<ServerFunctionUnavailableException>(
                async () => await worldBad.ReportBossDefeated(Boss1, Floor1));
            await AssertWorldIsPristine(worldBad, "Cache sau response ok=false");
        }
    }
}
