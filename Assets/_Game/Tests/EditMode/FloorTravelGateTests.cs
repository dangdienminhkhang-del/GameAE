using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Character;
using Game.Core.Floor;
using Game.Core.Time;
using Game.Core.World;
using Game.PvE.Bootstrap;
using Game.PvE.Floor;
using Game.UI.Floor;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task P9-1b — cổng chuyển tầng: chỉ đổi tầng khi người chơi đứng trong Vùng An Toàn và còn sống.
    /// Phần luật thuần (FloorTravelRules) và BuildPlan có test ở FloorTravelRulesTests / FloorActivationTests.
    /// File này kiểm: cổng trong FloorLoader, hàm cổng của PvEBootstrap, và chữ UI hiện cho người chơi.
    /// Chạy trong EditMode (không đụng scene thật).
    /// </summary>
    public class FloorTravelGateTests
    {
        // Chữ người chơi thấy (loader quyết định nội dung, UI hiện nguyên văn).
        private const string NotInSafeZoneText = "Chỉ có thể chuyển tầng khi đang ở Vùng An Toàn.";
        private const string PlayerDeadText = "Nhân vật đã gục, không thể chuyển tầng.";

        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            _created.Clear();
        }

        // ── Giả lập ─────────────────────────────────────────────────────────

        /// <summary>World state giả: trả một trạng thái cố định và đếm số lần bị hỏi.</summary>
        private sealed class CountingWorldState : IWorldStateService
        {
            private readonly FloorStatus _status;
            public int FloorStatusCalls;

            public CountingWorldState(FloorStatus status) { _status = status; }

            public Task<FloorStatus> GetFloorStatusAsync(string floorId)
            {
                FloorStatusCalls++;
                return Task.FromResult(_status);
            }

            public Task<Dictionary<string, FloorStatus>> GetAllFloorStatusesAsync()
                => Task.FromResult(new Dictionary<string, FloorStatus>());

            public Task<BossStatus> GetBossStatusAsync(string bossId)
                => Task.FromResult(BossStatus.Active);

            public Task<Dictionary<string, BossStatus>> GetAllBossStatusesAsync()
                => Task.FromResult(new Dictionary<string, BossStatus>());

            public Task<WorldTime> GetWorldTimeAsync()
                => Task.FromResult(new WorldTime());

            public Task ReportBossDefeated(string bossId, string floorId)
                => Task.CompletedTask;
        }

        /// <summary>Dịch vụ chết giả, chỉ quan tâm cờ IsPlayerDead.</summary>
        private sealed class StubDeathService : IDeathService
        {
            public bool Dead;

            public bool IsPlayerDead => Dead;
            public DeathInfo CurrentDeathInfo => null;
            public event EventHandler<DeathInfo> OnPlayerDied { add { } remove { } }

            public Task<DeathInfo> LoadDeathInfoAsync() => Task.FromResult<DeathInfo>(null);
            public Task RecordDeathAsync(DeathRecord record) => Task.CompletedTask;
            public Task RetryRecordAsync() => Task.CompletedTask;

            [Obsolete("Dùng RecordDeathAsync")]
            public Task OnPlayerDiedAsync(DeathReason reason, string sourceId, int currentLevel, int highestFloor, int playTimeSeconds)
                => Task.CompletedTask;

            public Task ResetDeathStateAsync() => Task.CompletedTask;
        }

        private FloorDatabase MakeDatabase(params string[] ids)
        {
            var db = ScriptableObject.CreateInstance<FloorDatabase>();
            _created.Add(db);
            db.floors = new List<FloorDefinition>();
            for (int i = 0; i < ids.Length; i++)
            {
                var def = ScriptableObject.CreateInstance<FloorDefinition>();
                _created.Add(def);
                def.id = ids[i];
                def.floorNumber = i + 1;
                def.displayName = "Tầng " + (i + 1);
                def.sceneKey = "TestScene" + (i + 1);
                db.floors.Add(def);
            }
            return db;
        }

        private static FloorLoadResult Load(FloorLoader loader, string id)
            => loader.LoadFloorAsync(id).GetAwaiter().GetResult();

        // ── FloorLoader: cổng từ chối ───────────────────────────────────────

        [Test]
        public void Gate_NotInSafeZone_Denies_WithPlayerMessage_AndSkipsWorldState()
        {
            var world = new CountingWorldState(FloorStatus.Available);
            var loader = new FloorLoader(world, MakeDatabase("test.a"), false, () => FloorTravelRules.Result.NotInSafeZone);

            var result = Load(loader, "test.a");

            Assert.AreEqual(FloorLoadStatus.NotAllowed, result.Status);
            Assert.AreEqual(NotInSafeZoneText, result.ErrorMessage, "Chữ phải giữ nguyên văn để UI hiện trực tiếp.");
            Assert.IsNull(result.Floor);
            Assert.IsNull(loader.CurrentFloorId, "Bị từ chối thì không được đổi tầng hiện tại.");
            Assert.AreEqual(0, world.FloorStatusCalls, "Từ chối sớm: không được đọc world state vô ích.");
        }

        [Test]
        public void Gate_PlayerDead_Denies_WithDeadMessage()
        {
            var loader = new FloorLoader(new CountingWorldState(FloorStatus.Available), MakeDatabase("test.a"),
                false, () => FloorTravelRules.Result.PlayerDead);

            var result = Load(loader, "test.a");

            Assert.AreEqual(FloorLoadStatus.NotAllowed, result.Status);
            Assert.AreEqual(PlayerDeadText, result.ErrorMessage);
            Assert.IsNull(loader.CurrentFloorId);
        }

        [Test]
        public void Gate_Denied_KeepsTheCurrentFloorUnchanged()
        {
            var verdict = FloorTravelRules.Result.Ok;
            var loader = new FloorLoader(new CountingWorldState(FloorStatus.Available),
                MakeDatabase("test.a", "test.b"), false, () => verdict);

            Assert.AreEqual(FloorLoadStatus.Loaded, Load(loader, "test.a").Status);
            Assert.AreEqual("test.a", loader.CurrentFloorId);

            verdict = FloorTravelRules.Result.NotInSafeZone; // người chơi bước ra ngoài rồi bấm chọn tầng khác
            var denied = Load(loader, "test.b");

            Assert.AreEqual(FloorLoadStatus.NotAllowed, denied.Status);
            Assert.AreEqual("test.a", loader.CurrentFloorId, "Tầng cũ không được bị unload hay đổi khi bị từ chối.");
        }

        // ── FloorLoader: cổng cho phép / không có cổng ──────────────────────

        [Test]
        public void Gate_Ok_LoadsTheFloor()
        {
            var loader = new FloorLoader(new CountingWorldState(FloorStatus.Available), MakeDatabase("test.a"),
                false, () => FloorTravelRules.Result.Ok);

            var result = Load(loader, "test.a");

            Assert.AreEqual(FloorLoadStatus.Loaded, result.Status);
            Assert.AreEqual("test.a", result.Floor.id);
            Assert.AreEqual("test.a", loader.CurrentFloorId);
        }

        [Test]
        public void NoGate_BehavesExactlyAsBefore()
        {
            var db = MakeDatabase("test.a");
            var world = new CountingWorldState(FloorStatus.Available);

            var twoArgs = new FloorLoader(world, db);
            var threeArgs = new FloorLoader(world, db, false);
            var explicitNull = new FloorLoader(world, db, false, null);

            foreach (var loader in new[] { twoArgs, threeArgs, explicitNull })
            {
                var result = Load(loader, "test.a");
                Assert.AreEqual(FloorLoadStatus.Loaded, result.Status, "Không có cổng = hành vi cũ, không bao giờ NotAllowed.");
                Assert.AreEqual("test.a", loader.CurrentFloorId);
            }
        }

        [Test]
        public void Gate_IsAskedAtCallTime_NotCachedOnce()
        {
            var verdict = FloorTravelRules.Result.NotInSafeZone;
            var loader = new FloorLoader(new CountingWorldState(FloorStatus.Available), MakeDatabase("test.a"),
                false, () => verdict);

            Assert.AreEqual(FloorLoadStatus.NotAllowed, Load(loader, "test.a").Status);

            verdict = FloorTravelRules.Result.Ok; // người chơi quay lại Vùng An Toàn
            Assert.AreEqual(FloorLoadStatus.Loaded, Load(loader, "test.a").Status, "Cổng phải đọc lại mỗi lần, không chốt một lần.");
        }

        // ── FloorLoader: cổng kiểm HAI lần ──────────────────────────────────

        [Test]
        public void Gate_OkFirst_ThenLeavesSafeZoneWhileWaiting_IsDenied_AndKeepsCurrentFloor()
        {
            int calls = 0;
            var world = new CountingWorldState(FloorStatus.Available);
            // Lần 1 (đầu LoadFloorAsync) cho qua; lần 2 (sau khi đọc world state) người chơi đã bước ra khỏi Vùng An Toàn.
            var loader = new FloorLoader(world, MakeDatabase("test.a"), false,
                () => ++calls == 1 ? FloorTravelRules.Result.Ok : FloorTravelRules.Result.NotInSafeZone);

            var result = Load(loader, "test.a");

            Assert.AreEqual(FloorLoadStatus.NotAllowed, result.Status);
            Assert.AreEqual(NotInSafeZoneText, result.ErrorMessage);
            Assert.AreEqual(2, calls, "Cổng phải được kiểm hai lần: đầu hàm và ngay trước khi nạp/kích hoạt.");
            Assert.AreEqual(1, world.FloorStatusCalls, "Lần kiểm thứ hai nằm SAU khi đọc world state.");
            Assert.IsNull(loader.CurrentFloorId, "Bị từ chối ở lần kiểm thứ hai thì tầng hiện tại không được đổi.");
        }

        [Test]
        public void Gate_SecondCheckDenied_KeepsPreviousFloor_WhenAlreadyStandingElsewhere()
        {
            int calls = 0;
            bool countCalls = false; // chỉ đếm trong lần load thứ hai
            var loader = new FloorLoader(new CountingWorldState(FloorStatus.Available),
                MakeDatabase("test.a", "test.b"), false,
                () => countCalls && ++calls == 2 ? FloorTravelRules.Result.NotInSafeZone : FloorTravelRules.Result.Ok);

            Assert.AreEqual(FloorLoadStatus.Loaded, Load(loader, "test.a").Status);
            Assert.AreEqual("test.a", loader.CurrentFloorId);

            countCalls = true; // lần load b: kiểm lần 1 = Ok, kiểm lần 2 = đã bước ra ngoài
            var result = Load(loader, "test.b");

            Assert.AreEqual(FloorLoadStatus.NotAllowed, result.Status);
            Assert.AreEqual("test.a", loader.CurrentFloorId, "Từ chối ở lần kiểm thứ hai: không unload tầng đang đứng.");
        }

        // ── Thứ tự kiểm: account chết trước cổng ────────────────────────────

        [Test]
        public void AccountDead_IsCheckedBeforeTheGate()
        {
            int gateCalls = 0;
            var loader = new FloorLoader(new CountingWorldState(FloorStatus.Available), MakeDatabase("test.a"),
                true, () => { gateCalls++; return FloorTravelRules.Result.NotInSafeZone; });

            var result = Load(loader, "test.a");

            Assert.AreEqual(FloorLoadStatus.Error, result.Status, "Account đã chết giữ nguyên hành vi cũ (Error), không đổi thành NotAllowed.");
            Assert.AreEqual(0, gateCalls);
        }

        [Test]
        public void Gate_Ok_ButFloorLocked_StillReportsLocked()
        {
            var loader = new FloorLoader(new CountingWorldState(FloorStatus.Locked), MakeDatabase("test.a"),
                false, () => FloorTravelRules.Result.Ok);

            Assert.AreEqual(FloorLoadStatus.Locked, Load(loader, "test.a").Status);
            Assert.IsNull(loader.CurrentFloorId);
        }

        // ── PvEBootstrap.EvaluateTravelGate (hàm cổng thuần) ────────────────

        private SafeZoneService MakeSafeZoneAt(Vector3 position, float size)
        {
            var zone = new GameObject("TestSafeZone");
            _created.Add(zone);
            var box = zone.AddComponent<BoxCollider2D>();
            box.size = new Vector2(size, size);
            zone.transform.position = position;
            Physics2D.SyncTransforms();

            var service = new SafeZoneService();
            service.Register(box);
            return service;
        }

        private Transform MakePlayer(Vector3 position)
        {
            var player = new GameObject("TestPlayer");
            _created.Add(player);
            player.transform.position = position;
            return player.transform;
        }

        [Test]
        public void EvaluateTravelGate_InsideSafeZoneAndAlive_IsOk()
        {
            var zones = MakeSafeZoneAt(new Vector3(10f, 5f, 0f), 4f);
            var player = MakePlayer(new Vector3(10f, 5f, 0f));

            Assert.AreEqual(FloorTravelRules.Result.Ok,
                PvEBootstrap.EvaluateTravelGate(player, zones, new StubDeathService { Dead = false }));
        }

        [Test]
        public void EvaluateTravelGate_OutsideSafeZone_IsNotInSafeZone()
        {
            var zones = MakeSafeZoneAt(new Vector3(10f, 5f, 0f), 4f);
            var player = MakePlayer(Vector3.zero);

            Assert.AreEqual(FloorTravelRules.Result.NotInSafeZone,
                PvEBootstrap.EvaluateTravelGate(player, zones, new StubDeathService { Dead = false }));
        }

        [Test]
        public void EvaluateTravelGate_PlayerNull_CountsAsOutsideSafeZone()
        {
            var zones = MakeSafeZoneAt(Vector3.zero, 100f);

            Assert.AreEqual(FloorTravelRules.Result.NotInSafeZone,
                PvEBootstrap.EvaluateTravelGate(null, zones, new StubDeathService { Dead = false }),
                "Không có người chơi = đóng cổng, không được mở.");
        }

        [Test]
        public void EvaluateTravelGate_NoSafeZoneService_IsNotInSafeZone()
        {
            var player = MakePlayer(Vector3.zero);

            Assert.AreEqual(FloorTravelRules.Result.NotInSafeZone,
                PvEBootstrap.EvaluateTravelGate(player, null, new StubDeathService { Dead = false }));
        }

        [Test]
        public void EvaluateTravelGate_Dead_IsPlayerDead_EvenInsideSafeZone()
        {
            var zones = MakeSafeZoneAt(new Vector3(10f, 5f, 0f), 4f);
            var inside = MakePlayer(new Vector3(10f, 5f, 0f));
            var outside = MakePlayer(Vector3.zero);
            var dead = new StubDeathService { Dead = true };

            Assert.AreEqual(FloorTravelRules.Result.PlayerDead, PvEBootstrap.EvaluateTravelGate(inside, zones, dead));
            Assert.AreEqual(FloorTravelRules.Result.PlayerDead, PvEBootstrap.EvaluateTravelGate(outside, zones, dead),
                "Gục được ưu tiên trước 'ngoài Vùng An Toàn'.");
        }

        [Test]
        public void EvaluateTravelGate_NoDeathService_TreatedAsAlive()
        {
            var zones = MakeSafeZoneAt(new Vector3(10f, 5f, 0f), 4f);
            var player = MakePlayer(new Vector3(10f, 5f, 0f));

            Assert.AreEqual(FloorTravelRules.Result.Ok, PvEBootstrap.EvaluateTravelGate(player, zones, null));
        }

        [Test]
        public void EvaluateTravelGate_IsReadAtCallTime_PlayerWalksOut()
        {
            var zones = MakeSafeZoneAt(new Vector3(10f, 5f, 0f), 4f);
            var player = MakePlayer(new Vector3(10f, 5f, 0f));
            var alive = new StubDeathService { Dead = false };

            Assert.AreEqual(FloorTravelRules.Result.Ok, PvEBootstrap.EvaluateTravelGate(player, zones, alive));

            player.position = new Vector3(40f, 40f, 0f); // bước ra khỏi Vùng An Toàn
            Assert.AreEqual(FloorTravelRules.Result.NotInSafeZone, PvEBootstrap.EvaluateTravelGate(player, zones, alive));
        }

        // ── FloorSelectPanel: chữ hiện khi bị từ chối ───────────────────────

        [Test]
        public void Panel_DescribeFailure_NotAllowed_ShowsLoaderMessageVerbatim()
        {
            Assert.AreEqual(NotInSafeZoneText,
                FloorSelectPanel.DescribeFailure(FloorLoadResult.NotAllowedResult(NotInSafeZoneText)));
            Assert.AreEqual(PlayerDeadText,
                FloorSelectPanel.DescribeFailure(FloorLoadResult.NotAllowedResult(PlayerDeadText)));
        }

        [Test]
        public void Panel_DescribeFailure_NotAllowedWithoutReason_StillShowsSomething()
        {
            string text = FloorSelectPanel.DescribeFailure(FloorLoadResult.NotAllowedResult(null));

            Assert.IsFalse(string.IsNullOrWhiteSpace(text));
        }

        [Test]
        public void Panel_DescribeFailure_OtherStatuses_KeepTheirOldWording()
        {
            Assert.AreEqual("Tầng chưa mở.", FloorSelectPanel.DescribeFailure(FloorLoadResult.LockedResult("x")));
            Assert.AreEqual("Tầng không tồn tại.", FloorSelectPanel.DescribeFailure(FloorLoadResult.NotFoundResult("x")));
        }
    }
}
