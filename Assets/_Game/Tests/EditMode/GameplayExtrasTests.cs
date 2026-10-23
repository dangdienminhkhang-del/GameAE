using System;
using System.Linq;
using Game.Core.Stats;
using Game.PvE.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Ba tính năng mới: cảnh báo máu thấp, bảng Nhân vật, rương kho báu.
    /// Logic thuần test trực tiếp; nối dây scene kiểm tra bằng SerializedObject (không vào Play Mode).
    /// Đọc component UI theo TÊN KIỂU để không phụ thuộc assembly UI.
    /// </summary>
    public class GameplayExtrasTests
    {
        private const string HostScenePath = "Assets/_Game/Scenes/PvE_Gameplay.unity";

        // ── Cảnh báo máu thấp ──────────────────────────────────────────────────────────

        [TestCase(1f)]
        [TestCase(0.5f)]
        [TestCase(0.31f)]
        [TestCase(0f)]
        [TestCase(-0.5f)]
        [TestCase(float.NaN)]
        public void LowHealth_NoWarning_WhenHealthyDeadOrInvalid(float ratio)
        {
            Assert.IsFalse(LowHealthWarningRules.IsLow(ratio));
            Assert.AreEqual(0f, LowHealthWarningRules.Intensity(ratio, 0.37f));
        }

        [Test]
        public void LowHealth_Warns_AtThreshold_AndBelow()
        {
            Assert.IsTrue(LowHealthWarningRules.IsLow(LowHealthWarningRules.WarnRatio));
            Assert.Greater(LowHealthWarningRules.BaseIntensity(LowHealthWarningRules.WarnRatio), 0f);
            Assert.IsTrue(LowHealthWarningRules.IsLow(0.01f));
        }

        [Test]
        public void LowHealth_BaseIntensity_GrowsAsHealthDrops()
        {
            Assert.Less(LowHealthWarningRules.BaseIntensity(0.25f), LowHealthWarningRules.BaseIntensity(0.15f));
            Assert.Less(LowHealthWarningRules.BaseIntensity(0.15f), LowHealthWarningRules.BaseIntensity(0.05f));
        }

        [Test]
        public void LowHealth_Intensity_StaysWithinZeroAndMax()
        {
            for (float r = 0.01f; r <= 0.30f; r += 0.01f)
            {
                for (float t = 0f; t <= 5f; t += 0.01f)
                {
                    float v = LowHealthWarningRules.Intensity(r, t);
                    Assert.GreaterOrEqual(v, 0f);
                    Assert.LessOrEqual(v, LowHealthWarningRules.MaxIntensity + 1e-5f);
                }
            }
        }

        [Test]
        public void LowHealth_Pulses_OverTime()
        {
            float min = float.MaxValue, max = float.MinValue;
            for (float t = 0f; t <= 1f; t += 0.01f)
            {
                float v = LowHealthWarningRules.Intensity(0.1f, t);
                min = Mathf.Min(min, v);
                max = Mathf.Max(max, v);
            }
            Assert.Greater(max - min, 0.05f, "Viền phải nhấp nháy theo thời gian, không đứng yên.");
        }

        [Test]
        public void LowHealth_PulseRate_IsBelowPhotosensitivityLimit_AndCriticalIsFaster()
        {
            Assert.Less(LowHealthWarningRules.WarnPulseHz, 3f);
            Assert.Less(LowHealthWarningRules.CriticalPulseHz, 3f);
            Assert.Greater(LowHealthWarningRules.CriticalPulseHz, LowHealthWarningRules.WarnPulseHz);
        }

        // ── Bảng Nhân vật ─────────────────────────────────────────────────────────────

        [Test]
        public void CharacterSheet_BuildsOneLinePerStat_WithExpectedValues()
        {
            string[] lines = CharacterSheet.BuildLines(3, 50, 140, 130, 14, 7, 0.1, 1.5, 4);

            Assert.AreEqual(7, lines.Length);
            StringAssert.Contains("3", lines[0]);
            Assert.AreEqual("EXP 50/140 (35%)", lines[1]);
            StringAssert.Contains("130", lines[2]);
            StringAssert.Contains("14", lines[3]);
            StringAssert.Contains("7", lines[4]);
            StringAssert.Contains("10%", lines[5]);
            StringAssert.Contains("1.5", lines[5]);
            StringAssert.Contains("4", lines[6]);
        }

        [Test]
        public void CharacterSheet_DoesNotThrow_WhenExpRequiredIsZeroOrExpOverflows()
        {
            Assert.DoesNotThrow(() => CharacterSheet.BuildLines(1, 0, 0, 100, 10, 5, 0, 1.5, 4));
            string[] lines = CharacterSheet.BuildLines(1, 500, 100, 100, 10, 5, 0, 1.5, 4);
            Assert.AreEqual("EXP 500/100 (100%)", lines[1], "Phần trăm phải bị kẹp tối đa 100%.");
        }

        // ── Rương kho báu ──────────────────────────────────────────────────────────

        [Test]
        public void ChestRegistry_OpensEachIdOnlyOnce()
        {
            TreasureChest.ResetOpenedForTests();
            try
            {
                Assert.IsFalse(TreasureChest.IsOpened("test.a"));
                Assert.IsTrue(TreasureChest.TryMarkOpened("test.a"));
                Assert.IsFalse(TreasureChest.TryMarkOpened("test.a"), "Rương đã mở không được mở lần hai.");
                Assert.IsTrue(TreasureChest.IsOpened("test.a"));
                Assert.IsFalse(TreasureChest.IsOpened("test.b"));
            }
            finally
            {
                TreasureChest.ResetOpenedForTests();
            }
        }

        [TestCase(null)]
        [TestCase("")]
        public void ChestRegistry_RejectsEmptyId(string id)
        {
            Assert.IsFalse(TreasureChest.TryMarkOpened(id));
            Assert.IsFalse(TreasureChest.IsOpened(id));
        }

        // ── Nối dây scene ──────────────────────────────────────────────────────────

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

        [Test]
        public void HostScene_WiresCharacterPanel_LowHealthWarning_AndChests()
        {
            WithScene(HostScenePath, scene =>
            {
                var all = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Component>(true))
                    .Where(c => c != null)
                    .ToList();

                var panels = all.Where(c => c.GetType().Name == "CharacterPanel").ToList();
                Assert.AreEqual(1, panels.Count, "Scene phải có đúng 1 CharacterPanel.");
                var panelSo = new SerializedObject(panels[0]);
                Assert.IsNotNull(panelSo.FindProperty("playerProgress").objectReferenceValue, "CharacterPanel chưa gán playerProgress.");
                Assert.IsNotNull(panelSo.FindProperty("openButton").objectReferenceValue, "CharacterPanel chưa gán openButton.");

                var huds = all.Where(c => c.GetType().Name == "PlayerHUD").ToList();
                Assert.AreEqual(1, huds.Count, "Scene phải có đúng 1 PlayerHUD.");
                Assert.IsNotNull(new SerializedObject(huds[0]).FindProperty("lowHealthWarning").objectReferenceValue,
                    "PlayerHUD chưa gán lowHealthWarning nên sẽ không có viền cảnh báo.");

                var chests = all.OfType<TreasureChest>().ToList();
                Assert.IsNotEmpty(chests, "Scene chưa có rương nào.");
                var ids = chests.Select(c => c.ChestId).ToList();
                Assert.IsFalse(ids.Any(string.IsNullOrWhiteSpace), "Rương thiếu chestId.");
                CollectionAssert.AllItemsAreUnique(ids, "chestId phải duy nhất giữa các rương.");

                foreach (var chest in chests)
                {
                    Assert.IsTrue(chest.RewardCount > 0 || chest.ColReward > 0, $"Rương '{chest.ChestId}' không có phần thưởng nào.");
                    var rewards = new SerializedObject(chest).FindProperty("rewards");
                    for (int i = 0; i < rewards.arraySize; i++)
                    {
                        Assert.IsNotNull(rewards.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue,
                            $"Rương '{chest.ChestId}' có phần thưởng {i} chưa gán vật phẩm.");
                    }
                    var col = chest.GetComponent<Collider2D>();
                    Assert.IsNotNull(col);
                    Assert.IsTrue(col.isTrigger, $"Collider của rương '{chest.ChestId}' phải là trigger.");
                }
                return true;
            });
        }
    }
}
