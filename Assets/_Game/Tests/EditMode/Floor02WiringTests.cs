using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Floor;
using Game.PvE.Boss;
using Game.PvE.Enemy;
using Game.PvE.Floor;
using Game.PvE.NPC;
using Game.PvE.Player;
using Game.PvE.SafeZone;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Phase 9 (P9-2) — chốt việc nối dây Tầng 2 bằng dữ liệu/scene.
    /// Đọc asset và scene thật; ID lấy từ asset, không viết cứng trong code gameplay.
    /// Không chạy gameplay, không vào Play Mode.
    /// </summary>
    public class Floor02WiringTests
    {
        private const string HostScenePath = "Assets/_Game/Scenes/PvE_Gameplay.unity";
        private const string Floor02ScenePath = "Assets/_Game/Scenes/Floor02.unity";
        private const string Floor01Path = "Assets/_Game/Content/Floors/Floor01/FloorDefinition_Floor01.asset";
        private const string Floor02Path = "Assets/_Game/Content/Floors/Floor02/FloorDefinition_Floor02.asset";
        private const string BossAssetPath = "Assets/_Game/Data/Enemies/Boss_Floor01.asset";
        private const string EnemiesFolder = "Assets/_Game/Data/Enemies";
        private const string GuardPath = "Assets/_Game/Data/Enemies/Enemy_BoilerGuard.asset";
        private const string WardenPath = "Assets/_Game/Data/Enemies/Enemy_PressureWarden.asset";
        private const string SlimePath = "Assets/_Game/Data/Enemies/Enemy_Slime.asset";
        private const string QuestPath = "Assets/_Game/Data/Quests/Quest_Floor2ValveCheck.asset";
        private const string NpcPath = "Assets/_Game/Data/NPCs/NPC_Cartographer.asset";

        // Các object GỐC được phép có trong scene host (nội dung Tầng 1 chỉ được nằm dưới Floor01).
        private static readonly HashSet<string> HostRootAllowList = new HashSet<string>
        {
            "Grid", "WorldClock", "FloorSpawnRegistrar", "PvEBootstrap", "Global Light 2D",
            "Main Camera", "EventSystem", "Player"
        };

        // ── Hỗ trợ ──────────────────────────────────────────────────────────

        private static T WithScene<T>(string path, Func<Scene, T> body)
        {
            Scene existing = SceneManager.GetSceneByPath(path);
            bool wasLoaded = existing.IsValid() && existing.isLoaded;
            Scene scene = wasLoaded ? existing : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                return body(scene);
            }
            finally
            {
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static List<T> Find<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }

        private static UnityEngine.Object Load(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            Assert.IsNotNull(asset, "Thiếu asset " + path);
            return asset;
        }

        private static FloorDefinition LoadFloor(string path)
        {
            var def = AssetDatabase.LoadAssetAtPath<FloorDefinition>(path);
            Assert.IsNotNull(def, "Thiếu FloorDefinition " + path);
            return def;
        }

        private static string StringOf(UnityEngine.Object asset, string field)
        {
            var p = new SerializedObject(asset).FindProperty(field);
            Assert.IsNotNull(p, $"{asset.name} không có field '{field}'.");
            return p.stringValue;
        }

        private static float FloatOf(UnityEngine.Object asset, string field)
        {
            var p = new SerializedObject(asset).FindProperty(field);
            Assert.IsNotNull(p, $"{asset.name} không có field '{field}'.");
            return p.floatValue;
        }

        private static FloorSceneBinder BinderFor(Scene scene, string floorId)
        {
            var matching = Find<FloorSceneBinder>(scene).Where(b => b.FloorId == floorId).ToList();
            Assert.AreEqual(1, matching.Count,
                $"Scene '{scene.name}' phải có đúng 1 FloorSceneBinder cho '{floorId}' (đang có {matching.Count}).");
            return matching[0];
        }

        /// <summary>Hình chữ nhật thế giới của BoxCollider2D (không dùng collider.bounds vì bằng 0 khi object đang tắt).</summary>
        private static Rect WorldRect(BoxCollider2D box)
        {
            Transform t = box.transform;
            Vector3 center = t.TransformPoint(box.offset);
            Vector3 s = t.lossyScale;
            Vector2 half = new Vector2(Mathf.Abs(box.size.x * s.x), Mathf.Abs(box.size.y * s.y)) * 0.5f;
            return new Rect(center.x - half.x, center.y - half.y, half.x * 2f, half.y * 2f);
        }

        private static List<Rect> SafeZoneRects(GameObject content)
        {
            return content.GetComponentsInChildren<SafeZoneTrigger>(true)
                .Select(z => z.GetComponent<BoxCollider2D>())
                .Where(b => b != null)
                .Select(WorldRect)
                .ToList();
        }

        private static HashSet<string> EntryIds(EnemySpawner spawner)
        {
            var entries = new SerializedObject(spawner).FindProperty("entries");
            var ids = new HashSet<string>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                var def = entries.GetArrayElementAtIndex(i).FindPropertyRelative("definition").objectReferenceValue;
                Assert.IsNotNull(def, $"Spawner '{spawner.name}' có entry {i} chưa gán EnemyDefinition.");
                ids.Add(StringOf(def, "id"));
            }
            return ids;
        }

        private static EnemySpawner SingleSpawnerIn(GameObject content)
        {
            var spawners = content.GetComponentsInChildren<EnemySpawner>(true);
            Assert.AreEqual(1, spawners.Length, $"'{content.transform.parent.name}/Content' phải có đúng 1 EnemySpawner.");
            return spawners[0];
        }

        // ── 1. Build Settings + binder Tầng 2 ───────────────────────────────

        [Test]
        public void Floor02Scene_IsInBuildSettings_RightAfterHostScene()
        {
            var def = LoadFloor(Floor02Path);
            string expectedPath = "Assets/_Game/Scenes/" + def.sceneKey + ".unity";
            Assert.AreEqual(Floor02ScenePath, expectedPath, "sceneKey của Tầng 2 phải trùng tên file scene.");

            var scenes = EditorBuildSettings.scenes.Select(s => s.path).ToList();
            int host = scenes.IndexOf(HostScenePath);
            int floor2 = scenes.IndexOf(expectedPath);
            Assert.GreaterOrEqual(host, 0, "PvE_Gameplay không có trong Build Settings.");
            Assert.GreaterOrEqual(floor2, 0, "Floor02 chưa được thêm vào Build Settings.");
            Assert.AreEqual(host + 1, floor2, "Floor02 phải đứng ngay sau PvE_Gameplay trong Build Settings.");
            Assert.IsTrue(EditorBuildSettings.scenes[floor2].enabled, "Floor02 đang bị tắt trong Build Settings.");
        }

        [Test]
        public void Floor02Scene_HasOneBinderMatchingDefinition_ContentOffAndBinderOn()
        {
            var def = LoadFloor(Floor02Path);
            WithScene(Floor02ScenePath, scene =>
            {
                var binder = BinderFor(scene, def.id);
                Assert.IsTrue(binder.gameObject.activeSelf, "Object chứa binder Tầng 2 phải luôn BẬT.");
                Assert.IsNotNull(binder.ContentRoot, "Binder Tầng 2 chưa gán contentRoot.");
                Assert.IsFalse(binder.ContentRoot.activeSelf,
                    "Content Tầng 2 phải được LƯU Ở TRẠNG THÁI TẮT, kẻo hai tầng chạy cùng lúc trước khi kích hoạt.");
                Assert.IsNotNull(binder.EntryPoint, "Binder Tầng 2 chưa gán entryPoint.");
                return true;
            });
        }

        [Test]
        public void HostScene_HasBinderMatchingFloor01_ContentOn()
        {
            var def = LoadFloor(Floor01Path);
            WithScene(HostScenePath, scene =>
            {
                var binder = BinderFor(scene, def.id);
                Assert.IsTrue(binder.gameObject.activeSelf, "Object chứa binder Tầng 1 phải luôn BẬT.");
                Assert.IsNotNull(binder.ContentRoot);
                Assert.IsTrue(binder.ContentRoot.activeSelf, "Content Tầng 1 phải đang BẬT khi lưu scene.");
                Assert.IsNotNull(binder.EntryPoint);
                return true;
            });
        }

        // ── 2. Khoảng cách hai điểm vào ─────────────────────────────────────

        [Test]
        public void EntryPoints_AreAtLeast50UnitsApart()
        {
            Vector3 e1 = WithScene(HostScenePath, s => BinderFor(s, LoadFloor(Floor01Path).id).EntryPoint.position);
            Vector3 e2 = WithScene(Floor02ScenePath, s => BinderFor(s, LoadFloor(Floor02Path).id).EntryPoint.position);
            Assert.GreaterOrEqual(Vector2.Distance(e1, e2), 50f,
                "Hai tầng nằm cùng một thế giới nên phải cách xa nhau để không nhìn thấy nhau.");
        }

        // ── 3. Điểm vào nằm trong Safe Zone của chính tầng ──────────────────

        [TestCase(HostScenePath, Floor01Path)]
        [TestCase(Floor02ScenePath, Floor02Path)]
        public void EntryPoint_IsInsideASafeZone_OfItsOwnFloor(string scenePath, string floorPath)
        {
            var def = LoadFloor(floorPath);
            WithScene(scenePath, scene =>
            {
                var binder = BinderFor(scene, def.id);
                var rects = SafeZoneRects(binder.ContentRoot);
                Assert.IsNotEmpty(rects, $"Content của '{def.id}' không có Safe Zone nào.");
                Vector2 p = binder.EntryPoint.position;
                Assert.IsTrue(rects.Any(r => r.Contains(p)),
                    $"entryPoint của '{def.id}' ({p}) không nằm trong Safe Zone nào của tầng này — người chơi sẽ hạ cánh ngoài vùng an toàn và bị kẹt.");
                return true;
            });
        }

        // ── 4. Spawner Tầng 2 ───────────────────────────────────────────────

        [Test]
        public void Floor02Spawner_IsConsistentWithDefinition_AndSafeZone()
        {
            var def = LoadFloor(Floor02Path);
            WithScene(Floor02ScenePath, scene =>
            {
                var binder = BinderFor(scene, def.id);
                var spawner = SingleSpawnerIn(binder.ContentRoot);
                var so = new SerializedObject(spawner);

                Assert.IsFalse(so.FindProperty("spawnOnStart").boolValue,
                    "Spawner Tầng 2 phải có spawnOnStart = TẮT (registrar sẽ gọi SpawnAll khi vào tầng).");

                // ID quái: entries ⊆ spawnEnemyIds và ngược lại.
                var entryIds = EntryIds(spawner);
                var allowed = new HashSet<string>(def.spawnEnemyIds);
                CollectionAssert.AreEquivalent(allowed, entryIds,
                    "Quái trong entries của spawner Tầng 2 phải đúng bằng spawnEnemyIds của FloorDefinition_Floor02.");

                // Mỗi ID trong spawnEnemyIds phải có asset EnemyDefinition.
                var known = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemiesFolder })
                    .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(g)))
                    .Select(a => StringOf(a, "id"))
                    .ToList();
                foreach (var id in def.spawnEnemyIds)
                    Assert.Contains(id, known, $"spawnEnemyIds của Tầng 2 có '{id}' nhưng không có asset EnemyDefinition nào mang ID này.");

                // Prefab quái dùng chung với Tầng 1.
                // Lấy prefab ngay trong scope scene host (scene sẽ đóng sau đó, component sẽ bị hủy).
                var hostPrefab = WithScene(HostScenePath, hs =>
                    new SerializedObject(SingleSpawnerIn(BinderFor(hs, LoadFloor(Floor01Path).id).ContentRoot))
                        .FindProperty("enemyPrefab").objectReferenceValue);
                Assert.AreEqual(
                    hostPrefab,
                    so.FindProperty("enemyPrefab").objectReferenceValue,
                    "Spawner Tầng 2 phải dùng chung prefab quái với Tầng 1.");

                // Pool đủ cho số lượng lớn nhất.
                var entries = so.FindProperty("entries");
                int maxCount = 0;
                float maxRadius = 0f;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var e = entries.GetArrayElementAtIndex(i);
                    maxCount = Mathf.Max(maxCount, e.FindPropertyRelative("count").intValue);
                    maxRadius = Mathf.Max(maxRadius, e.FindPropertyRelative("radius").floatValue);
                }
                Assert.GreaterOrEqual(so.FindProperty("poolSizePerDefinition").intValue, maxCount,
                    "poolSizePerDefinition nhỏ hơn số quái lớn nhất của một entry.");

                // Vùng spawn không giao Safe Zone.
                var centerT = so.FindProperty("spawnCenter").objectReferenceValue as Transform;
                Assert.IsNotNull(centerT, "Spawner Tầng 2 chưa gán spawnCenter.");
                Rect spawnRect = new Rect(
                    centerT.position.x - maxRadius, centerT.position.y - maxRadius, maxRadius * 2f, maxRadius * 2f);
                foreach (var zone in SafeZoneRects(binder.ContentRoot))
                    Assert.IsFalse(spawnRect.Overlaps(zone),
                        $"Vùng spawn {spawnRect} giao Safe Zone {zone}: quái sẽ xuất hiện ngay trong vùng an toàn.");
                return true;
            });
        }

        [Test]
        public void Floor02Registrar_IsSeparateFromContent_AndPointsToFloor02Spawner()
        {
            var def = LoadFloor(Floor02Path);
            WithScene(Floor02ScenePath, scene =>
            {
                var binder = BinderFor(scene, def.id);
                var registrar = new SerializedObject(binder).FindProperty("floorSpawnRegistrar").objectReferenceValue
                    as FloorSpawnRegistrar;
                Assert.IsNotNull(registrar, "Binder Tầng 2 chưa gán floorSpawnRegistrar.");
                Assert.IsFalse(registrar.transform.IsChildOf(binder.ContentRoot.transform),
                    "FloorSpawnRegistrar phải nằm NGOÀI Content (Content đang tắt thì registrar vẫn phải gọi được).");

                var target = new SerializedObject(registrar).FindProperty("enemySpawner").objectReferenceValue
                    as EnemySpawner;
                Assert.IsNotNull(target, "FloorSpawnRegistrar Tầng 2 chưa gán enemySpawner.");
                Assert.AreSame(SingleSpawnerIn(binder.ContentRoot), target,
                    "FloorSpawnRegistrar Tầng 2 phải trỏ tới spawner của Tầng 2.");
                return true;
            });
        }

        // ── 5. Scene host: nội dung Tầng 1 nằm trong Floor01 ────────────────

        [Test]
        public void HostScene_Floor01Content_HoldsSpawnerBossNpcAndSafeZone()
        {
            var def = LoadFloor(Floor01Path);
            WithScene(HostScenePath, scene =>
            {
                var content = BinderFor(scene, def.id).ContentRoot;
                var spawner = SingleSpawnerIn(content);
                Assert.IsNotEmpty(content.GetComponentsInChildren<BossController>(true), "Content Tầng 1 thiếu Boss.");
                Assert.IsNotEmpty(content.GetComponentsInChildren<NPCInteractable>(true), "Content Tầng 1 thiếu NPC.");
                Assert.IsNotEmpty(content.GetComponentsInChildren<SafeZoneTrigger>(true), "Content Tầng 1 thiếu Safe Zone.");

                CollectionAssert.AreEquivalent(new HashSet<string>(def.spawnEnemyIds), EntryIds(spawner),
                    "spawnEnemyIds của Tầng 1 phải đúng bằng tập ID quái trong entries của spawner Tầng 1.");
                return true;
            });
        }

        // ── 6. Không còn nội dung Tầng 1 lọt ngoài Floor01 ──────────────────

        [Test]
        public void HostScene_RootObjects_AreOnlyTheAllowedOnes()
        {
            var floor1Name = WithScene(HostScenePath, s => BinderFor(s, LoadFloor(Floor01Path).id).gameObject.name);
            WithScene(HostScenePath, scene =>
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    bool allowed = HostRootAllowList.Contains(root.name)
                                   || root.name == floor1Name
                                   || root.GetComponent<Canvas>() != null;
                    Assert.IsTrue(allowed,
                        $"Object gốc '{root.name}' trong scene host không thuộc danh sách cho phép: nội dung Tầng 1 phải nằm dưới '{floor1Name}/Content'.");
                }
                return true;
            });
        }

        // ── 7. Player / Camera / Canvas không nằm trong nội dung tầng ───────

        [TestCase(HostScenePath)]
        [TestCase(Floor02ScenePath)]
        public void Binders_DoNotContainPlayerOrCamera_AndAreNotInsideTheirOwnContent(string scenePath)
        {
            WithScene(scenePath, scene =>
            {
                foreach (var binder in Find<FloorSceneBinder>(scene))
                {
                    var content = binder.ContentRoot;
                    Assert.IsNotNull(content, $"Binder '{binder.name}' chưa có contentRoot.");
                    Assert.IsFalse(binder.transform.IsChildOf(content.transform),
                        $"Binder '{binder.name}' nằm trong contentRoot của chính nó — tắt Content sẽ tắt luôn binder.");
                    Assert.IsEmpty(content.GetComponentsInChildren<PlayerController>(true),
                        $"Player nằm dưới contentRoot của '{binder.name}'.");
                    Assert.IsEmpty(content.GetComponentsInChildren<Camera>(true),
                        $"Camera nằm dưới contentRoot của '{binder.name}'.");
                }
                return true;
            });
        }

        [Test]
        public void Floor02Scene_HasNoPlayerCameraCanvasOrEventSystem()
        {
            WithScene(Floor02ScenePath, scene =>
            {
                Assert.IsEmpty(Find<PlayerController>(scene), "Scene Floor02 không được có Player.");
                Assert.IsEmpty(Find<Camera>(scene), "Scene Floor02 không được có Camera.");
                Assert.IsEmpty(Find<Canvas>(scene), "Scene Floor02 không được có Canvas.");
                Assert.IsEmpty(Find<Component>(scene).Where(c => c != null && c.GetType().Name == "EventSystem").ToList(),
                    "Scene Floor02 không được có EventSystem.");
                return true;
            });
        }

        // ── 8. Quest / NPC nối đúng vào dữ liệu tầng ────────────────────────

        [Test]
        public void Floor02_QuestAndNpc_AreListedAndConsistent()
        {
            var def = LoadFloor(Floor02Path);
            var quest = Load(QuestPath);
            var npc = Load(NpcPath);

            string target = StringOf(quest, "targetId");
            CollectionAssert.Contains(def.spawnEnemyIds, target,
                "Quest Tầng 2 nhắm tới quái không có trong spawnEnemyIds của Tầng 2.");
            CollectionAssert.Contains(def.questIds, StringOf(quest, "id"), "questIds của Tầng 2 thiếu quest này.");
            CollectionAssert.Contains(def.npcIds, StringOf(npc, "id"), "npcIds của Tầng 2 thiếu NPC này.");
            Assert.AreSame(quest, new SerializedObject(npc).FindProperty("offeredQuest").objectReferenceValue,
                "NPC Người Vẽ Hải Đồ phải giao đúng quest của Tầng 2.");
            Assert.IsFalse(new SerializedObject(quest).FindProperty("requiresSpecificTime").boolValue,
                "Quest Tầng 2 không yêu cầu giờ.");
            Assert.IsFalse(new SerializedObject(quest).FindProperty("repeatable").boolValue,
                "Quest Tầng 2 phải làm một lần (repeatable = false), nếu không sẽ farm 60 EXP + 2 Bình Máu vô hạn.");
        }

        [Test]
        public void Floor02Scene_NpcUsesTheCartographerDefinition()
        {
            var npc = Load(NpcPath);
            WithScene(Floor02ScenePath, scene =>
            {
                var binder = BinderFor(scene, LoadFloor(Floor02Path).id);
                var npcs = binder.ContentRoot.GetComponentsInChildren<NPCInteractable>(true);
                Assert.AreEqual(1, npcs.Length, "Content Tầng 2 phải có đúng 1 NPC.");
                Assert.AreSame(npc, new SerializedObject(npcs[0]).FindProperty("definition").objectReferenceValue);
                return true;
            });
        }

        // ── 9. attackWindupSeconds không rơi về mặc định ────────────────────

        [Test]
        public void NewEnemies_HaveTheirOwnWindupTimes()
        {
            float slime = FloatOf(Load(SlimePath), "attackWindupSeconds");
            float guard = FloatOf(Load(GuardPath), "attackWindupSeconds");
            float warden = FloatOf(Load(WardenPath), "attackWindupSeconds");
            Assert.Greater(guard, slime, "Lính Gác phải báo đòn lâu hơn Slime.");
            Assert.GreaterOrEqual(guard, 0.6f, "Lính Gác bị rơi về windup mặc định?");
            Assert.GreaterOrEqual(warden, 0.5f, "Quản Áp bị rơi về windup mặc định?");
        }

        [Test]
        public void EnemyIds_AreUnique()
        {
            var ids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemiesFolder })
                .Select(g => StringOf(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(g)), "id"))
                .ToList();
            var dup = ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.IsEmpty(dup, "Có ID quái bị trùng: " + string.Join(", ", dup));
        }

        // ── 10. FloorDefinition_Floor02 giữ nguyên ──────────────────────────

        [Test]
        public void Floor02Definition_KeepsSceneKeyBossLockAndLevelRange()
        {
            var def = LoadFloor(Floor02Path);
            var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>(BossAssetPath);
            Assert.IsNotNull(boss);
            Assert.AreEqual("Floor02", def.sceneKey);
            Assert.AreEqual(boss.Id, def.requiredBossDefeatedId, "Tầng 2 phải mở khi boss Tầng 1 bị hạ.");
            Assert.AreEqual(6, def.recommendedLevelMin);
            Assert.AreEqual(10, def.recommendedLevelMax);
            Assert.AreEqual(2, def.floorNumber);
            Assert.IsTrue(string.IsNullOrEmpty(def.bossId), "Tầng 2 chưa có boss riêng.");
        }
    }
}
