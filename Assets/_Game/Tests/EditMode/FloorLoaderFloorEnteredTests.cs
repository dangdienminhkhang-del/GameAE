using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Game.Core.Floor;
using Game.Core.Time;
using Game.Core.World;
using Game.PvE.Floor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task P9-1d — callback "đã vào tầng" của FloorLoader (dùng để theo dõi tầng cao nhất).
    /// Chạy trong EditMode: FloorLoader đi nhánh EditMode (không đụng scene thật).
    /// Các nhánh chỉ có trong Play Mode (tầng nằm trong scene host, "đang đứng đúng tầng", nạp additive)
    /// cùng gọi một hàm nội bộ Entered(); chúng nằm trong danh sách thử tay ở báo cáo P9-1d.
    /// </summary>
    public class FloorLoaderFloorEnteredTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            _created.Clear();
        }

        private sealed class FixedWorldState : IWorldStateService
        {
            private readonly FloorStatus _status;
            public FixedWorldState(FloorStatus status) { _status = status; }

            public Task<FloorStatus> GetFloorStatusAsync(string floorId) => Task.FromResult(_status);
            public Task<Dictionary<string, FloorStatus>> GetAllFloorStatusesAsync()
                => Task.FromResult(new Dictionary<string, FloorStatus>());
            public Task<BossStatus> GetBossStatusAsync(string bossId) => Task.FromResult(BossStatus.Active);
            public Task<Dictionary<string, BossStatus>> GetAllBossStatusesAsync()
                => Task.FromResult(new Dictionary<string, BossStatus>());
            public Task<WorldTime> GetWorldTimeAsync() => Task.FromResult(new WorldTime());
            public Task ReportBossDefeated(string bossId, string floorId) => Task.CompletedTask;
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

        // ── Thành công → callback chạy ──────────────────────────────────────

        [Test]
        public void Loaded_RunsTheCallback_OnceWithTheEnteredFloor()
        {
            var entered = new List<FloorDefinition>();
            var loader = new FloorLoader(new FixedWorldState(FloorStatus.Available), MakeDatabase("test.a", "test.b"),
                false, null, def => entered.Add(def));

            var result = Load(loader, "test.b");

            Assert.AreEqual(FloorLoadStatus.Loaded, result.Status);
            Assert.AreEqual(1, entered.Count, "Mỗi lần vào tầng thành công gọi callback đúng một lần.");
            Assert.AreSame(result.Floor, entered[0]);
            Assert.AreEqual(2, entered[0].floorNumber);
        }

        [Test]
        public void LoadedTwice_RunsTheCallbackEachTime()
        {
            int calls = 0;
            var loader = new FloorLoader(new FixedWorldState(FloorStatus.Available), MakeDatabase("test.a", "test.b"),
                false, null, _ => calls++);

            Load(loader, "test.a");
            Load(loader, "test.b");

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void Loaded_WithTravelGateOk_RunsTheCallback()
        {
            int calls = 0;
            var loader = new FloorLoader(new FixedWorldState(FloorStatus.Available), MakeDatabase("test.a"),
                false, () => FloorTravelRules.Result.Ok, _ => calls++);

            Assert.AreEqual(FloorLoadStatus.Loaded, Load(loader, "test.a").Status);
            Assert.AreEqual(1, calls);
        }

        // ── Không thành công → callback KHÔNG chạy ──────────────────────────

        [Test]
        public void Locked_DoesNotRunTheCallback()
        {
            int calls = 0;
            var loader = new FloorLoader(new FixedWorldState(FloorStatus.Locked), MakeDatabase("test.a"),
                false, null, _ => calls++);

            Assert.AreEqual(FloorLoadStatus.Locked, Load(loader, "test.a").Status);
            Assert.AreEqual(0, calls, "Tầng chưa mở thì không được tính là đã vào.");
        }

        [Test]
        public void NotFound_DoesNotRunTheCallback()
        {
            int calls = 0;
            var loader = new FloorLoader(new FixedWorldState(FloorStatus.Available), MakeDatabase("test.a"),
                false, null, _ => calls++);

            Assert.AreEqual(FloorLoadStatus.NotFound, Load(loader, "khong.co").Status);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void NotAllowedByTravelGate_DoesNotRunTheCallback()
        {
            int calls = 0;
            var loader = new FloorLoader(new FixedWorldState(FloorStatus.Available), MakeDatabase("test.a"),
                false, () => FloorTravelRules.Result.NotInSafeZone, _ => calls++);

            Assert.AreEqual(FloorLoadStatus.NotAllowed, Load(loader, "test.a").Status);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void DeadAccount_Error_DoesNotRunTheCallback()
        {
            int calls = 0;
            var loader = new FloorLoader(new FixedWorldState(FloorStatus.Available), MakeDatabase("test.a"),
                true, null, _ => calls++);

            Assert.AreEqual(FloorLoadStatus.Error, Load(loader, "test.a").Status);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void MissingSceneKey_Error_DoesNotRunTheCallback()
        {
            int calls = 0;
            var db = MakeDatabase("test.a");
            db.floors[0].sceneKey = "";
            var loader = new FloorLoader(new FixedWorldState(FloorStatus.Available), db, false, null, _ => calls++);

            Assert.AreEqual(FloorLoadStatus.Error, Load(loader, "test.a").Status);
            Assert.AreEqual(0, calls);
        }

        // ── Callback lỗi / không có callback ────────────────────────────────

        [Test]
        public void ThrowingCallback_DoesNotBreakEnteringTheFloor()
        {
            var loader = new FloorLoader(new FixedWorldState(FloorStatus.Available), MakeDatabase("test.a"),
                false, null, _ => throw new InvalidOperationException("callback-hong"));

            LogAssert.Expect(LogType.Exception, new Regex("callback-hong"));
            var result = Load(loader, "test.a");

            Assert.AreEqual(FloorLoadStatus.Loaded, result.Status, "Callback ném lỗi không được làm hỏng kết quả.");
            Assert.AreEqual("test.a", result.Floor.id);
            Assert.AreEqual("test.a", loader.CurrentFloorId, "Vẫn phải đổi tầng hiện tại.");
        }

        [Test]
        public void NullCallback_BehavesLikeBefore()
        {
            var loader = new FloorLoader(new FixedWorldState(FloorStatus.Available), MakeDatabase("test.a"),
                false, null, null);

            var result = Load(loader, "test.a");

            Assert.AreEqual(FloorLoadStatus.Loaded, result.Status);
            Assert.AreEqual("test.a", loader.CurrentFloorId);
        }
    }
}
