using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Game.Core.Floor;
using Game.Core.Time;
using Game.Core.World;
using Game.PvE.Floor;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// EditMode tests cho Wave 1B — Floor Framework.
    /// Dùng mock IWorldStateService inline — không cần framework mock nào.
    /// </summary>
    public class FloorFrameworkTests
    {
        // ─── Mock IWorldStateService ──────────────────────────────────────────────

        /// <summary>
        /// Mock đơn giản: trả cùng một FloorStatus cho mọi ID.
        /// </summary>
        private class MockWorldStateService : IWorldStateService
        {
            private readonly FloorStatus _defaultStatus;

            // Dictionary cho phép set status khác nhau theo ID
            private readonly Dictionary<string, FloorStatus> _overrides;

            public MockWorldStateService(FloorStatus defaultStatus,
                                         Dictionary<string, FloorStatus> overrides = null)
            {
                _defaultStatus = defaultStatus;
                _overrides = overrides ?? new Dictionary<string, FloorStatus>();
            }

            public Task<FloorStatus> GetFloorStatusAsync(string floorId)
            {
                if (_overrides.TryGetValue(floorId, out var status))
                    return Task.FromResult(status);
                return Task.FromResult(_defaultStatus);
            }

            public Task<Dictionary<string, FloorStatus>> GetAllFloorStatusesAsync()
            {
                return Task.FromResult(new Dictionary<string, FloorStatus>(_overrides));
            }

            public Task<BossStatus> GetBossStatusAsync(string bossId)
                => Task.FromResult(BossStatus.Active);

            public Task<Dictionary<string, BossStatus>> GetAllBossStatusesAsync()
                => Task.FromResult(new Dictionary<string, BossStatus>());

            public Task<WorldTime> GetWorldTimeAsync()
                => Task.FromResult(new WorldTime());

            public Task ReportBossDefeated(string bossId, string floorId)
                => Task.CompletedTask;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>Tạo FloorDefinition bằng ScriptableObject.CreateInstance (không cần asset).</summary>
        private static FloorDefinition MakeFloor(string id, int number, string displayName = null)
        {
            var def = UnityEngine.ScriptableObject.CreateInstance<FloorDefinition>();
            def.id = id;
            def.floorNumber = number;
            def.displayName = displayName ?? $"Tầng {number}";
            def.sceneKey = $"PvE_Floor{number:D2}";
            return def;
        }

        /// <summary>Tạo FloorDatabase và đăng ký các floor đã truyền vào.</summary>
        private static FloorDatabase MakeDatabase(params FloorDefinition[] floors)
        {
            var db = UnityEngine.ScriptableObject.CreateInstance<FloorDatabase>();
            db.floors = new System.Collections.Generic.List<FloorDefinition>(floors);
            return db;
        }

        // ─── Test 1: Unknown ID → NotFound ────────────────────────────────────────

        [Test]
        public void FloorLoader_Returns_NotFound_ForUnknownId()
        {
            // Arrange: database rỗng
            var db = MakeDatabase();
            var worldState = new MockWorldStateService(FloorStatus.Available);
            var loader = new FloorLoader(worldState, db);

            // Act
            var task = loader.LoadFloorAsync("floor.99");
            task.Wait();
            var result = task.Result;

            // Assert
            Assert.AreEqual(FloorLoadStatus.NotFound, result.Status,
                "ID không có trong database phải trả NotFound");
            Assert.IsNull(result.Floor,
                "Floor phải null khi NotFound");
            Assert.IsNotEmpty(result.ErrorMessage,
                "ErrorMessage phải có nội dung khi NotFound");
        }

        // ─── Test 2: Locked floor → Locked result ─────────────────────────────────

        [Test]
        public void FloorLoader_Returns_Locked_WhenWorldStateLocked()
        {
            // Arrange
            var floor2 = MakeFloor("floor.02", 2, "Tầng 2");
            var db = MakeDatabase(floor2);

            // Mock trả Locked cho mọi ID
            var worldState = new MockWorldStateService(FloorStatus.Locked);
            var loader = new FloorLoader(worldState, db);

            // Act
            var task = loader.LoadFloorAsync("floor.02");
            task.Wait();
            var result = task.Result;

            // Assert
            Assert.AreEqual(FloorLoadStatus.Locked, result.Status,
                "World state Locked phải trả FloorLoadStatus.Locked");
            Assert.IsNull(result.Floor,
                "Floor phải null khi Locked");
        }

        // ─── Test 3: Floor 1 loads successfully ───────────────────────────────────

        [Test]
        public void FloorLoader_Returns_Loaded_ForFloor01()
        {
            // Arrange
            var floor1 = MakeFloor("floor.01", 1, "Tầng Mở Đầu");
            var db = MakeDatabase(floor1);

            // Mock trả Available
            var worldState = new MockWorldStateService(FloorStatus.Available);
            var loader = new FloorLoader(worldState, db);

            // Act
            var task = loader.LoadFloorAsync("floor.01");
            task.Wait();
            var result = task.Result;

            // Assert
            Assert.AreEqual(FloorLoadStatus.Loaded, result.Status,
                "Floor 01 với Available phải trả Loaded");
            Assert.IsNotNull(result.Floor,
                "Floor không được null khi Loaded");
            Assert.AreEqual("floor.01", result.Floor.id,
                "Floor ID phải khớp");
            Assert.AreEqual("Tầng Mở Đầu", result.Floor.displayName,
                "displayName phải khớp");
        }

        // ─── Test 4: Floor 2 added via data — không cần sửa code ─────────────────

        [Test]
        public void FloorDatabase_Floor2_LoadsWithoutCodeChange()
        {
            // Arrange: tạo 2 FloorDefinition, register vào 1 database
            var floor1 = MakeFloor("floor.01", 1, "Tầng 1");
            var floor2 = MakeFloor("floor.02", 2, "Tầng 2");
            var db = MakeDatabase(floor1, floor2);

            var worldState = new MockWorldStateService(FloorStatus.Available);
            var loader = new FloorLoader(worldState, db);

            // Kiểm tra floor2 accessible từ database
            Assert.IsNotNull(db.GetFloor("floor.01"), "Floor.01 phải có trong database");
            Assert.IsNotNull(db.GetFloor("floor.02"), "Floor.02 phải có trong database");

            // Sort theo floorNumber — floor.01 phải đứng trước floor.02
            var all = db.GetAllFloors();
            Assert.AreEqual(2, all.Count, "Database phải có 2 floor");
            Assert.AreEqual(1, all[0].floorNumber, "Floor đầu tiên phải là floor 1");
            Assert.AreEqual(2, all[1].floorNumber, "Floor thứ hai phải là floor 2");

            // Load floor 2 thành công không cần code change
            var task = loader.LoadFloorAsync("floor.02");
            task.Wait();
            var result = task.Result;

            Assert.AreEqual(FloorLoadStatus.Loaded, result.Status,
                "Floor 02 (data-only) phải load được mà không sửa Core/PvE code");
            Assert.AreEqual("floor.02", result.Floor.id, "ID phải là floor.02");
        }

        // ─── Test 5: ID validation ────────────────────────────────────────────────

        [Test]
        public void FloorDefinition_ValidatesIdFormat()
        {
            // Regex từ contract §4.6
            const string pattern = @"^floor\.\d{2,}$";

            // Valid
            Assert.IsTrue(Regex.IsMatch("floor.01", pattern), "floor.01 phải valid");
            Assert.IsTrue(Regex.IsMatch("floor.10", pattern), "floor.10 phải valid");
            Assert.IsTrue(Regex.IsMatch("floor.99", pattern), "floor.99 phải valid");
            Assert.IsTrue(Regex.IsMatch("floor.100", pattern), "floor.100 (3 chữ số) phải valid");

            // Invalid
            Assert.IsFalse(Regex.IsMatch("Floor01", pattern),  "Floor01 (hoa, không có dấu chấm) phải invalid");
            Assert.IsFalse(Regex.IsMatch("floor.1", pattern),  "floor.1 (1 chữ số) phải invalid");
            Assert.IsFalse(Regex.IsMatch("1", pattern),        "chỉ số phải invalid");
            Assert.IsFalse(Regex.IsMatch("floor01", pattern),  "không có dấu chấm phải invalid");
            Assert.IsFalse(Regex.IsMatch("floor.", pattern),   "không có số phải invalid");
            Assert.IsFalse(Regex.IsMatch("floor.0a", pattern), "có chữ trong phần số phải invalid");
            Assert.IsFalse(Regex.IsMatch("", pattern),         "rỗng phải invalid");
        }

        // ─── Test 6: Cleared floor → Loaded (bonus) ───────────────────────────────

        [Test]
        public void FloorLoader_Returns_Loaded_WhenWorldStateCleared()
        {
            // Arrange: tầng đã Cleared vẫn có thể vào lại
            var floor1 = MakeFloor("floor.01", 1);
            var db = MakeDatabase(floor1);
            var worldState = new MockWorldStateService(FloorStatus.Cleared);
            var loader = new FloorLoader(worldState, db);

            // Act
            var task = loader.LoadFloorAsync("floor.01");
            task.Wait();
            var result = task.Result;

            // Assert
            Assert.AreEqual(FloorLoadStatus.Loaded, result.Status,
                "Tầng Cleared phải load được (người chơi có thể farm lại)");
        }
    }
}
