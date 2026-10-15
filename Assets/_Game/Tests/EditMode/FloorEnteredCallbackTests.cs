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
    /// P9-1d — callback "đã vào tầng" của FloorLoader: chạy khi vào tầng thành công,
    /// KHÔNG chạy khi Locked/NotFound/NotAllowed/Error, và callback ném lỗi không làm hỏng việc vào tầng.
    /// Chỉ kiểm được nhánh EditMode của loader (các nhánh Play Mode cần scene thật — kiểm tay trong Editor).
    /// </summary>
    public class FloorEnteredCallbackTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            _created.Clear();
        }

        private sealed class FakeWorldState : IWorldStateService
        {
            private readonly FloorStatus _status;
            public FakeWorldState(FloorStatus status) { _status = status; }

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

        private static FloorLoader MakeLoader(
            FloorDatabase db,
            FloorStatus status,
            Action<FloorDefinition> onEntered,
            bool accountDead = false,
            Func<FloorTravelRules.Result> gate = null)
            => new FloorLoader(new FakeWorldState(status), db, accountDead, gate, onEntered);

        // ── Chạy khi vào tầng thành công ────────────────────────────────────

        [Test]
        public void Callback_RunsOnSuccess_WithTheFloorDefinition()
        {
            var db = MakeDatabase("test.a", "test.b");
            var entered = new List<FloorDefinition>();
            var loader = MakeLoader(db, FloorStatus.Available, entered.Add);

            var result = Load(loader, "test.b");

            Assert.AreEqual(FloorLoadStatus.Loaded, result.Status);
            Assert.AreEqual(1, entered.Count);
            Assert.AreSame(db.GetFloor("test.b"), entered[0]);
            Assert.AreEqual(2, entered[0].floorNumber);
        }

        [Test]
        public void Callback_RunsOnEveryEntry_IncludingReEnteringTheSameFloor()
        {
            var db = MakeDatabase("test.a", "test.b");
            var ids = new List<string>();
            var loader = MakeLoader(db, FloorStatus.Available, def => ids.Add(def.id));

            Load(loader, "test.a");
            Load(loader, "test.b");
            Load(loader, "test.b");

            CollectionAssert.AreEqual(new[] { "test.a", "test.b", "test.b" }, ids);
        }

        [Test]
        public void Callback_WithOpenGate_StillRuns()
        {
            var db = MakeDatabase("test.a");
            int calls = 0;
            var loader = MakeLoader(db, FloorStatus.Available, _ => calls++, gate: () => FloorTravelRules.Result.Ok);

            Assert.AreEqual(FloorLoadStatus.Loaded, Load(loader, "test.a").Status);
            Assert.AreEqual(1, calls);
        }

        // ── KHÔNG chạy khi không vào được tầng ──────────────────────────────

        [Test]
        public void Callback_NotCalled_WhenLocked()
        {
            int calls = 0;
            var loader = MakeLoader(MakeDatabase("test.a"), FloorStatus.Locked, _ => calls++);

            Assert.AreEqual(FloorLoadStatus.Locked, Load(loader, "test.a").Status);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Callback_NotCalled_WhenNotFound()
        {
            int calls = 0;
            var loader = MakeLoader(MakeDatabase("test.a"), FloorStatus.Available, _ => calls++);

            Assert.AreEqual(FloorLoadStatus.NotFound, Load(loader, "test.khong_co").Status);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Callback_NotCalled_WhenTravelGateDenies()
        {
            int calls = 0;
            var loader = MakeLoader(MakeDatabase("test.a"), FloorStatus.Available, _ => calls++,
                gate: () => FloorTravelRules.Result.NotInSafeZone);

            Assert.AreEqual(FloorLoadStatus.NotAllowed, Load(loader, "test.a").Status);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Callback_NotCalled_WhenAccountDead()
        {
            int calls = 0;
            var loader = MakeLoader(MakeDatabase("test.a"), FloorStatus.Available, _ => calls++, accountDead: true);

            Assert.AreEqual(FloorLoadStatus.Error, Load(loader, "test.a").Status);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Callback_NotCalled_WhenSceneKeyMissing()
        {
            var db = MakeDatabase("test.a");
            db.GetFloor("test.a").sceneKey = "";
            int calls = 0;
            var loader = MakeLoader(db, FloorStatus.Available, _ => calls++);

            Assert.AreEqual(FloorLoadStatus.Error, Load(loader, "test.a").Status);
            Assert.AreEqual(0, calls);
        }

        // ── Callback lỗi / không có callback ────────────────────────────────

        [Test]
        public void Callback_Throws_DoesNotBreakEnteringTheFloor()
        {
            var db = MakeDatabase("test.a");
            var loader = MakeLoader(db, FloorStatus.Available,
                _ => throw new InvalidOperationException("callback hỏng"));

            LogAssert.Expect(LogType.Exception, new Regex("callback hỏng"));
            FloorLoadResult result = null;
            Assert.DoesNotThrow(() => result = Load(loader, "test.a"));

            Assert.AreEqual(FloorLoadStatus.Loaded, result.Status);
            Assert.AreSame(db.GetFloor("test.a"), result.Floor);
            Assert.AreEqual("test.a", loader.CurrentFloorId, "Vẫn phải đổi tầng hiện tại dù callback lỗi.");
        }

        [Test]
        public void NoCallback_BehavesAsBefore()
        {
            var db = MakeDatabase("test.a");
            var world = new FakeWorldState(FloorStatus.Available);

            var withNullCallback = new FloorLoader(world, db, false, null, null);
            var fourArgs = new FloorLoader(world, db, false, null);

            foreach (var loader in new[] { withNullCallback, fourArgs })
            {
                Assert.AreEqual(FloorLoadStatus.Loaded, Load(loader, "test.a").Status);
                Assert.AreEqual("test.a", loader.CurrentFloorId);
            }
        }
    }
}
