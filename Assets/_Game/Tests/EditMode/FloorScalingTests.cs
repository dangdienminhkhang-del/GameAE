using Game.Core.Floor;
using Game.PvE.Enemy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Phase 6/7 — hệ số theo tầng (docs/07 §6, chốt ở Task B2), tăng đều (tuyến tính):
    ///   enemyPowerMultiplier(N) = 1 + 0.6  × (N − 1)
    ///   expMultiplier(N)        = 1 + 0.75 × (N − 1)
    /// </summary>
    public class FloorScalingTests
    {
        private const string SlimePath = "Assets/_Game/Data/Enemies/Enemy_Slime.asset";
        private const string BatPath = "Assets/_Game/Data/Enemies/Enemy_Bat.asset";

        [TestCase(1, 1.0f, 1.0f)]
        [TestCase(2, 1.6f, 1.75f)]
        [TestCase(3, 2.2f, 2.5f)]
        [TestCase(5, 3.4f, 4.0f)]
        [TestCase(10, 6.4f, 7.75f)]
        public void Multipliers_MatchDesignTable(int floor, float power, float exp)
        {
            Assert.AreEqual(power, FloorScaling.EnemyPowerMultiplier(floor), 0.0001f);
            Assert.AreEqual(exp, FloorScaling.ExpMultiplier(floor), 0.0001f);
        }

        [Test]
        public void FloorBelowOne_ClampsToFloorOne()
        {
            Assert.AreEqual(1.0f, FloorScaling.EnemyPowerMultiplier(0), 0.0001f);
            Assert.AreEqual(1.0f, FloorScaling.ExpMultiplier(-3), 0.0001f);
        }

        [Test]
        public void ExpScalesFasterThanEnemyPower_FromFloorTwoOn()
        {
            for (int floor = 2; floor <= 10; floor++)
                Assert.Greater(FloorScaling.ExpMultiplier(floor), FloorScaling.EnemyPowerMultiplier(floor),
                    $"Tầng {floor}: hệ số EXP phải lớn hơn hệ số sức mạnh quái");
        }

        [Test]
        public void Multipliers_GrowByAFixedStepPerFloor()
        {
            // Tuyến tính: mỗi tầng cộng thêm đúng 0.6 (sức mạnh) và 0.75 (EXP).
            for (int floor = 1; floor < 50; floor++)
            {
                Assert.AreEqual(0.6f, FloorScaling.EnemyPowerMultiplier(floor + 1) - FloorScaling.EnemyPowerMultiplier(floor), 0.001f);
                Assert.AreEqual(0.75f, FloorScaling.ExpMultiplier(floor + 1) - FloorScaling.ExpMultiplier(floor), 0.001f);
            }
        }

        [Test]
        public void BaseStats_MatchDocsTableForFloorOne()
        {
            // docs/07 §1: Slime Xanh = HP 50 / ATK 11 / EXP 12. Đọc asset THẬT, không gõ tay số quái.
            var slime = LoadSlime();
            Assert.AreEqual(50.0, slime.MaxHealth, 0.0001, "Slime HP trong asset lệch bảng docs/07");
            Assert.AreEqual(11.0, slime.Attack, 0.0001, "Slime ATK trong asset lệch bảng docs/07");
            Assert.AreEqual(12, slime.ExperienceReward, "Slime EXP trong asset lệch bảng docs/07");

            // Tầng 1 giữ nguyên chỉ số gốc.
            float power = FloorScaling.EnemyPowerMultiplier(1);
            float exp = FloorScaling.ExpMultiplier(1);
            Assert.AreEqual(slime.MaxHealth, slime.MaxHealth * power, 0.01);
            Assert.AreEqual(slime.Attack, slime.Attack * power, 0.01);
            Assert.AreEqual(slime.ExperienceReward, Mathf.RoundToInt(slime.ExperienceReward * exp));
        }

        [Test]
        public void BaseStats_ScaledForFloorTwo_FromRealSlimeAsset()
        {
            // Tầng 2 = 1.6 (sức mạnh) / 1.75 (EXP): HP 50→80, ATK 11→17.6, EXP 12→21.
            var slime = LoadSlime();
            float power = FloorScaling.EnemyPowerMultiplier(2);
            float exp = FloorScaling.ExpMultiplier(2);

            Assert.AreEqual(80.0, slime.MaxHealth * power, 0.01);
            Assert.AreEqual(17.6, slime.Attack * power, 0.01);
            Assert.AreEqual(0.0, slime.Defense * power, 0.01);
            Assert.AreEqual(21, Mathf.RoundToInt(slime.ExperienceReward * exp));
        }

        private static EnemyDefinition LoadSlime()
        {
            var slime = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(SlimePath);
            Assert.IsNotNull(slime, $"Không tìm thấy asset quái tại {SlimePath}");
            return slime;
        }

        // ── EnemyController làm tròn chỉ số sau khi nhân hệ số tầng (docs/07 §6) ──

        [Test]
        public void EnemyController_SlimeFloorTwo_HasExactRoundedStats()
        {
            // Trước đây 50 × 1.6f = 80.0000012 làm quái sống dư đòn thứ 4. Giờ phải đúng 80.0.
            var enemy = MakeEnemy(LoadSlime(), floor: 2);
            try
            {
                Assert.AreEqual(80.0, enemy.MaxHealth, "Slime Tầng 2: HP phải đúng 80.0 (không phải 80.0000012)");
                Assert.AreEqual(18.0, enemy.Attack, "Slime Tầng 2: ATK 11 × 1.6 = 17.6 làm tròn thành 18");
                Assert.AreEqual(0.0, enemy.Defense);
                Assert.AreEqual(21, enemy.ExperienceReward, "Slime Tầng 2: EXP 12 × 1.75 = 21");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            }
        }

        [Test]
        public void EnemyController_BatFloorTwo_ExpRoundsHalfAwayFromZero()
        {
            // 14 × 1.75 = 24.5 → 25 (AwayFromZero). RoundToInt cũ sẽ ra 24 (làm tròn về số chẵn).
            var bat = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(BatPath);
            Assert.IsNotNull(bat, $"Không tìm thấy asset quái tại {BatPath}");

            var enemy = MakeEnemy(bat, floor: 2);
            try
            {
                Assert.AreEqual(14, bat.ExperienceReward, "EXP gốc của Bat lệch docs/07");
                Assert.AreEqual(25, enemy.ExperienceReward);
                Assert.AreEqual(64.0, enemy.MaxHealth, "Bat Tầng 2: HP 40 × 1.6 = 64");
                Assert.AreEqual(19.0, enemy.Attack, "Bat Tầng 2: ATK 12 × 1.6 = 19.2 làm tròn thành 19");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            }
        }

        [TestCase(14, 1.75f, 25)]    // 24.5  → 25
        [TestCase(14, 3.25f, 46)]    // 45.5  → 46
        [TestCase(14, 4.75f, 67)]    // 66.5  → 67 (RoundToInt cũ ra 66)
        [TestCase(14, 7.75f, 109)]   // 108.5 → 109 (RoundToInt cũ ra 108)
        [TestCase(12, 1.75f, 21)]    // 21.0  → 21
        [TestCase(12, 1.0f, 12)]
        public void ScaleExperience_RoundsHalfAwayFromZero(int baseExp, float multiplier, int expected)
        {
            Assert.AreEqual(expected, EnemyController.ScaleExperience(baseExp, multiplier));
        }

        [TestCase(SlimePath)]
        [TestCase(BatPath)]
        public void EnemyController_Floors1To10_StatsAreWholeNumbers(string path)
        {
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            Assert.IsNotNull(def, $"Không tìm thấy asset quái tại {path}");

            for (int floor = 1; floor <= 10; floor++)
            {
                var enemy = MakeEnemy(def, floor);
                try
                {
                    Assert.AreEqual(System.Math.Round(enemy.MaxHealth), enemy.MaxHealth, $"{def.DisplayName} Tầng {floor}: HP không là số nguyên ({enemy.MaxHealth:R})");
                    Assert.AreEqual(System.Math.Round(enemy.Attack), enemy.Attack, $"{def.DisplayName} Tầng {floor}: ATK không là số nguyên ({enemy.Attack:R})");
                    Assert.AreEqual(System.Math.Round(enemy.Defense), enemy.Defense, $"{def.DisplayName} Tầng {floor}: DEF không là số nguyên ({enemy.Defense:R})");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                }
            }
        }

        /// <summary>Dựng EnemyController ở trạng thái tắt (không chạy Awake/OnEnable) để đọc chỉ số sau hệ số tầng.</summary>
        private static EnemyController MakeEnemy(EnemyDefinition definition, int floor)
        {
            var go = new GameObject("FloorScalingTestEnemy");
            go.SetActive(false);
            var enemy = go.AddComponent<EnemyController>();
            enemy.SetDefinition(definition);
            enemy.SetFloor(floor);
            return enemy;
        }
    }
}
