using System;
using System.Collections.Generic;
using System.Linq;
using Game.PvE.Boss;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Phase 8 — chốt việc nối dây boss vào scene/asset (docs/05 Phase 8).
    /// Các test này đọc asset và scene thật, để lỗi "quên đặt boss / quên đăng ký item thưởng /
    /// boss rơi xuống do trọng lực" không quay lại. Không chạy gameplay.
    /// </summary>
    public class BossSceneWiringTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/PvE_Gameplay.unity";
        private const string BossAssetPath = "Assets/_Game/Data/Enemies/Boss_Floor01.asset";
        private const string BossPrefabPath = "Assets/_Game/Prefabs/Boss_Floor01.prefab";
        private const string ItemDatabasePath = "Assets/_Game/Data/Items/ItemDatabase.asset";
        private const string Floor01Path = "Assets/_Game/Content/Floors/Floor01/FloorDefinition_Floor01.asset";
        private const string Floor02Path = "Assets/_Game/Content/Floors/Floor02/FloorDefinition_Floor02.asset";

        private static T WithScene<T>(Func<Scene, T> body)
        {
            Scene existing = SceneManager.GetSceneByPath(ScenePath);
            bool wasLoaded = existing.IsValid() && existing.isLoaded;
            Scene scene = wasLoaded
                ? existing
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                return body(scene);
            }
            finally
            {
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static BossController[] FindBosses(Scene scene)
        {
            var result = new List<BossController>();
            foreach (var root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<BossController>(true));
            return result.ToArray();
        }

        private static BossDefinition LoadBossDefinition()
        {
            var def = AssetDatabase.LoadAssetAtPath<BossDefinition>(BossAssetPath);
            Assert.IsNotNull(def, "Thiếu " + BossAssetPath);
            return def;
        }

        [Test]
        public void PvEGameplayScene_HasExactlyOneBoss_UsingBossFloor01()
        {
            var def = LoadBossDefinition();
            int count = WithScene(scene =>
            {
                var bosses = FindBosses(scene);
                if (bosses.Length == 1)
                {
                    Assert.AreSame(def, bosses[0].Definition,
                        "BossController trong scene phải dùng Boss_Floor01.asset.");
                }
                return bosses.Length;
            });
            Assert.AreEqual(1, count, "Scene PvE_Gameplay phải có đúng một BossController.");
        }

        [Test]
        public void PvEGameplayScene_Boss_PhysicsAndLayer_AreSafe()
        {
            WithScene(scene =>
            {
                var bosses = FindBosses(scene);
                Assert.AreEqual(1, bosses.Length);
                var go = bosses[0].gameObject;

                Assert.AreEqual("Enemy", LayerMask.LayerToName(go.layer),
                    "Boss phải ở layer Enemy để đòn đánh của người chơi trúng được.");

                var rb = go.GetComponent<Rigidbody2D>();
                Assert.IsNotNull(rb);
                Assert.AreEqual(RigidbodyType2D.Dynamic, rb.bodyType);
                Assert.AreEqual(0f, rb.gravityScale, 0.0001f,
                    "Dự án có trọng lực mặc định (0,-9.81); boss phải có Gravity Scale = 0 kẻo rơi khỏi màn hình.");
                Assert.AreEqual(0f, rb.linearDamping, 0.0001f,
                    "Linear Damping phải = 0 để cú lao đi đủ quãng pressureDashDistance.");
                Assert.IsTrue((rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0);

                var box = go.GetComponent<BoxCollider2D>();
                Assert.IsNotNull(box, "Boss cần collider để người chơi đánh trúng.");
                Assert.IsFalse(box.isTrigger);

                var sr = go.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr);
                Assert.IsNotNull(sr.sprite, "Boss chưa có hình (sprite).");
                return true;
            });
        }

        [Test]
        public void BossPrefab_Exists_AndMatchesSceneSetup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            Assert.IsNotNull(prefab, "Thiếu " + BossPrefabPath);
            var boss = prefab.GetComponent<BossController>();
            Assert.IsNotNull(boss);
            Assert.AreSame(LoadBossDefinition(), boss.Definition);
            Assert.AreEqual("Enemy", LayerMask.LayerToName(prefab.layer));
            var rb = prefab.GetComponent<Rigidbody2D>();
            Assert.AreEqual(0f, rb.gravityScale, 0.0001f);
            Assert.AreEqual(0f, rb.linearDamping, 0.0001f);
        }

        [Test]
        public void BossRewards_AreConfigured_AndRegisteredInItemDatabase()
        {
            var def = LoadBossDefinition();
            Assert.IsNotNull(def.FirstClearRewardItem, "Boss chưa có vật phẩm first-clear.");
            Assert.Greater(def.FirstClearRewardQuantity, 0);
            Assert.Greater(def.MainDrops.Count, 0, "Boss chưa có main drop cho rematch.");
            Assert.Greater(def.RareDrops.Count, 0, "Boss chưa có rare drop.");

            var rewardItems = new List<UnityEngine.Object> { def.FirstClearRewardItem };
            rewardItems.AddRange(def.MainDrops.Select(d => (UnityEngine.Object)d.item));
            rewardItems.AddRange(def.RareDrops.Select(d => (UnityEngine.Object)d.item));
            foreach (var item in rewardItems)
                Assert.IsNotNull(item, "Có một drop chưa gán vật phẩm.");

            var db = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ItemDatabasePath);
            Assert.IsNotNull(db);
            var list = new SerializedObject(db).FindProperty("items");
            var registered = new HashSet<UnityEngine.Object>();
            for (int i = 0; i < list.arraySize; i++)
                registered.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);

            foreach (var item in rewardItems.Distinct())
                Assert.IsTrue(registered.Contains(item),
                    $"Item thưởng '{item.name}' chưa có trong ItemDatabase: lưu xong sẽ không nạp lại được.");

            foreach (var d in def.MainDrops.Concat(def.RareDrops))
            {
                Assert.That(d.chance, Is.InRange(0f, 1f));
                Assert.GreaterOrEqual(d.minCount, 1);
                Assert.GreaterOrEqual(d.maxCount, d.minCount);
            }
        }

        [Test]
        public void Floors_AreLinkedToTheBoss()
        {
            var def = LoadBossDefinition();
            var floor1 = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Floor01Path));
            var floor2 = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Floor02Path));

            Assert.AreEqual(def.FloorId, floor1.FindProperty("id").stringValue,
                "BossDefinition.floorId phải trùng id của Tầng 1.");
            Assert.AreEqual(def.Id, floor1.FindProperty("bossId").stringValue,
                "Tầng 1 phải khai báo đúng boss của nó.");
            Assert.AreEqual(def.Id, floor2.FindProperty("requiredBossDefeatedId").stringValue,
                "Tầng 2 chỉ mở khi boss Tầng 1 bị hạ.");
        }
    }
}
