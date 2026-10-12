using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Game.Core.Floor;
using Game.Core.Time;
using Game.Core.World;
using Game.PvE.Bootstrap;
using Game.PvE.Floor;
using Game.UI.Controls;
using Game.UI.Floor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// EditMode tests cho Phase 7 — nối bảng Chọn Tầng (Wave 1B):
    /// logic scene host, FloorSelector, và các "chốt chặn" tài sản/scene
    /// (prefab ref null, canvas inactive, nút mở bị gắn nhầm script...).
    /// </summary>
    public class FloorSelectorTests
    {
        // ─── Mock IWorldStateService ──────────────────────────────────────────

        private class StubWorldState : IWorldStateService
        {
            private readonly Dictionary<string, FloorStatus> _statuses;
            private readonly bool _throwOnAll;

            public StubWorldState(Dictionary<string, FloorStatus> statuses = null, bool throwOnAll = false)
            {
                _statuses = statuses ?? new Dictionary<string, FloorStatus>();
                _throwOnAll = throwOnAll;
            }

            public Task<FloorStatus> GetFloorStatusAsync(string floorId)
                => Task.FromResult(_statuses.TryGetValue(floorId, out var s) ? s : FloorStatus.Locked);

            public Task<Dictionary<string, FloorStatus>> GetAllFloorStatusesAsync()
            {
                if (_throwOnAll)
                    return Task.FromException<Dictionary<string, FloorStatus>>(new System.Exception("mất mạng giả lập"));
                return Task.FromResult(new Dictionary<string, FloorStatus>(_statuses));
            }

            public Task<BossStatus> GetBossStatusAsync(string bossId) => Task.FromResult(BossStatus.Active);
            public Task<Dictionary<string, BossStatus>> GetAllBossStatusesAsync() => Task.FromResult(new Dictionary<string, BossStatus>());
            public Task<WorldTime> GetWorldTimeAsync() => Task.FromResult(new WorldTime());
            public Task ReportBossDefeated(string bossId, string floorId) => Task.CompletedTask;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static FloorDefinition MakeFloor(string id, int number, string requiredBoss = null)
        {
            var def = ScriptableObject.CreateInstance<FloorDefinition>();
            def.id = id;
            def.floorNumber = number;
            def.displayName = $"Tầng {number}";
            def.sceneKey = $"PvE_Floor{number:D2}";
            def.requiredBossDefeatedId = requiredBoss ?? string.Empty;
            return def;
        }

        private static FloorDatabase MakeDatabase(params FloorDefinition[] floors)
        {
            var db = ScriptableObject.CreateInstance<FloorDatabase>();
            db.floors = new List<FloorDefinition>(floors);
            return db;
        }

        private static List<FloorSelectEntry> Select(FloorDatabase db, IWorldStateService ws)
        {
            var task = new FloorSelector(db, ws).GetSelectableFloorsAsync();
            task.Wait();
            return task.Result;
        }

        // ─── IsSceneAlreadyHosted (lỗi nhân đôi scene host) ───────────────────

        [Test]
        public void IsSceneAlreadyHosted_True_WhenSceneNameIsLoaded()
        {
            var loaded = new[] { "Boot", "PvE_Gameplay" };
            Assert.IsTrue(FloorLoader.IsSceneAlreadyHosted("PvE_Gameplay", loaded),
                "Scene đã nạp phải được coi là host — không được load lại");
        }

        [Test]
        public void IsSceneAlreadyHosted_False_WhenSceneNotLoaded()
        {
            var loaded = new[] { "Boot", "PvE_Gameplay" };
            Assert.IsFalse(FloorLoader.IsSceneAlreadyHosted("Floor02", loaded));
        }

        [Test]
        public void IsSceneAlreadyHosted_IsCaseSensitiveOrdinal()
        {
            var loaded = new[] { "PvE_Gameplay" };
            Assert.IsFalse(FloorLoader.IsSceneAlreadyHosted("pve_gameplay", loaded));
        }

        [TestCase(null)]
        [TestCase("")]
        public void IsSceneAlreadyHosted_False_WhenSceneKeyEmpty(string sceneKey)
        {
            Assert.IsFalse(FloorLoader.IsSceneAlreadyHosted(sceneKey, new[] { "PvE_Gameplay" }));
        }

        [Test]
        public void IsSceneAlreadyHosted_False_WhenListNull()
        {
            Assert.IsFalse(FloorLoader.IsSceneAlreadyHosted("PvE_Gameplay", null));
        }

        // ─── FloorSelector ────────────────────────────────────────────────────

        [Test]
        public void FloorSelector_Floor1Available_Floor2Locked_SortedByNumber()
        {
            // Đăng ký ngược thứ tự để kiểm tra sort
            var db = MakeDatabase(MakeFloor("floor.02", 2, "boss.floor_01"), MakeFloor("floor.01", 1));
            var entries = Select(db, new StubWorldState());   // world state không có entry nào

            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("floor.01", entries[0].Definition.id, "Sort tăng dần theo floorNumber");
            Assert.AreEqual("floor.02", entries[1].Definition.id);
            Assert.AreEqual(FloorStatus.Available, entries[0].Status, "Tầng không yêu cầu boss mở mặc định");
            Assert.IsTrue(entries[0].IsPlayable);
            Assert.AreEqual(FloorStatus.Locked, entries[1].Status, "Tầng cần boss + không có entry → Locked");
            Assert.IsFalse(entries[1].IsPlayable);
        }

        [Test]
        public void FloorSelector_Floor2Playable_WhenWorldStateSaysAvailable()
        {
            var db = MakeDatabase(MakeFloor("floor.01", 1), MakeFloor("floor.02", 2, "boss.floor_01"));
            var ws = new StubWorldState(new Dictionary<string, FloorStatus> { { "floor.02", FloorStatus.Available } });

            var entries = Select(db, ws);

            Assert.IsTrue(entries[1].IsPlayable, "Server báo Available thì hàng phải chọn được");
        }

        [Test]
        public void FloorSelector_WorldStateThrows_FailSafe_Floor1Available_OthersLocked()
        {
            LogAssert.Expect(LogType.Error, new Regex("FloorSelector"));
            var db = MakeDatabase(MakeFloor("floor.01", 1), MakeFloor("floor.02", 2, "boss.floor_01"));

            var entries = Select(db, new StubWorldState(throwOnAll: true));

            Assert.AreEqual(FloorStatus.Available, entries[0].Status, "Mất mạng vẫn chơi được Tầng 1");
            Assert.AreEqual(FloorStatus.Locked, entries[1].Status);
        }

        // ─── Chốt chặn tài sản ────────────────────────────────────────────────

        [Test]
        public void FloorDatabaseAsset_IsValid_AndHasFloor1AndLockedFloor2()
        {
            var db = AssetDatabase.LoadAssetAtPath<FloorDatabase>("Assets/_Game/Data/Databases/FloorDatabase.asset");
            Assert.IsNotNull(db, "Thiếu FloorDatabase.asset");

            var errors = db.Validate();
            Assert.IsEmpty(errors, "FloorDatabase.Validate() phải sạch: " + string.Join(" | ", errors));

            foreach (var f in db.floors)
                StringAssert.IsMatch(@"^floor\.\d{2,}$", f.id, "id sai định dạng: " + f.id);

            var f1 = db.GetFloor("floor.01");
            var f2 = db.GetFloor("floor.02");
            Assert.IsNotNull(f1, "Thiếu floor.01");
            Assert.IsNotNull(f2, "Thiếu floor.02 (placeholder khóa)");
            Assert.IsTrue(string.IsNullOrEmpty(f1.requiredBossDefeatedId), "Tầng 1 mở mặc định");
            Assert.IsFalse(string.IsNullOrEmpty(f2.requiredBossDefeatedId), "Tầng 2 phải yêu cầu boss");
        }

        [Test]
        public void FloorSelectRowPrefab_HasAllReferencesAssigned()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/FloorSelectRow.prefab");
            Assert.IsNotNull(prefab, "Thiếu prefab FloorSelectRow");
            var row = prefab.GetComponent<FloorSelectRow>();
            Assert.IsNotNull(row, "Prefab thiếu component FloorSelectRow");

            var so = new SerializedObject(row);
            foreach (var field in new[] { "floorNameLabel", "statusLabel", "levelRangeLabel", "selectButton", "lockedOverlay" })
                Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, $"FloorSelectRow.{field} đang null");

            var overlay = prefab.transform.Find("LockedOverlay");
            Assert.IsNotNull(overlay);
            var le = overlay.GetComponent<LayoutElement>();
            Assert.IsTrue(le != null && le.ignoreLayout,
                "LockedOverlay phải ignoreLayout, nếu không sẽ bị HorizontalLayoutGroup xếp thành 1 ô");
        }

        [Test]
        public void PvEGameplayScene_FloorUi_IsWired()
        {
            const string scenePath = "Assets/_Game/Scenes/PvE_Gameplay.unity";
            var scene = EditorSceneManager.GetSceneByPath(scenePath);
            bool openedByTest = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedByTest = true;
            }

            try
            {
                FloorSelectPanel panel = null;
                PvEBootstrap bootstrap = null;
                var floorButtons = new List<GameObject>();
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (panel == null) panel = root.GetComponentInChildren<FloorSelectPanel>(true);
                    if (bootstrap == null) bootstrap = root.GetComponentInChildren<PvEBootstrap>(true);
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        if (t.name == "FloorButton") floorButtons.Add(t.gameObject);
                }

                Assert.IsNotNull(panel, "Thiếu FloorSelectPanel trong scene");
                Assert.IsNotNull(bootstrap, "Thiếu PvEBootstrap trong scene");

                // Canvas ACTIVE (FindAnyObjectByType không thấy object inactive); chỉ PanelRoot ẩn
                Assert.IsTrue(panel.gameObject.activeSelf, "GameObject chứa FloorSelectPanel phải ACTIVE");
                var so = new SerializedObject(panel);
                var panelRoot = so.FindProperty("panelRoot").objectReferenceValue as GameObject;
                Assert.IsNotNull(panelRoot, "panelRoot chưa gán");
                Assert.IsFalse(panelRoot.activeSelf, "PanelRoot phải ẩn mặc định");

                foreach (var field in new[] { "listContainer", "rowPrefab", "closeButton", "loadingIndicator", "errorPanel", "errorLabel" })
                    Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, $"FloorSelectPanel.{field} chưa gán");

                // Canvas scaler khớp Canvas chính (ngang 1920x1080)
                var scaler = panel.GetComponent<CanvasScaler>();
                Assert.AreEqual(new Vector2(1920, 1080), scaler.referenceResolution, "CanvasScaler phải 1920x1080 (game nằm ngang)");
                Assert.IsTrue(scaler.enabled, "CanvasScaler đang bị tắt → canvas scale = 1, lệch hẳn Canvas chính");
                Assert.IsTrue(panel.GetComponent<GraphicRaycaster>().enabled, "GraphicRaycaster của FloorSelectCanvas đang bị tắt → không bấm được");

                // Bootstrap có FloorDatabase
                var bso = new SerializedObject(bootstrap);
                Assert.IsNotNull(bso.FindProperty("floorDatabase").objectReferenceValue, "PvEBootstrap.floorDatabase chưa gán");

                // Nút mở: đúng 1 nút, có FloorToggleButton, KHÔNG còn PvEActionButton (nhân bản từ QuestButton)
                Assert.AreEqual(1, floorButtons.Count, "Phải có đúng 1 FloorButton");
                Assert.IsNotNull(floorButtons[0].GetComponent<FloorToggleButton>(), "FloorButton thiếu FloorToggleButton");
                Assert.IsNull(floorButtons[0].GetComponent<PvEActionButton>(),
                    "FloorButton còn PvEActionButton → sẽ mở nhật ký quest thay vì bảng tầng");
            }
            finally
            {
                if (openedByTest) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
