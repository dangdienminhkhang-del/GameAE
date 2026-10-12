using System;
using System.Collections.Generic;
using System.Text;
using Game.Core.Combat;
using Game.Core.Floor;
using Game.Core.Stats;
using Game.PvE.Bootstrap;
using Game.PvE.Enemy;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Sandbox kiểm tra balance (docs/07 §1, §4.2, §5.1, §6, §9). KHÔNG gõ tay số liệu:
    /// - quái: đọc từ asset THẬT (Enemy_Slime, Enemy_Bat) bằng AssetDatabase;
    /// - hệ số theo tầng: gọi hàm FloorScaling cho mọi tầng (nguồn thật mà EnemyController dùng);
    /// - người chơi: CharacterStats THẬT theo level, EXPNext THẬT từ CharacterStats;
    /// - defenseWeight: đọc từ CombatBalanceConfig THẬT (cùng nguồn PvEBootstrap nạp lúc chạy game).
    ///
    /// Cách tính quái "thật": dựng một EnemyController thật (SetDefinition + SetFloor) rồi đọc thẳng
    /// MaxHealth / Attack / Defense / ExperienceReward — gồm cả bước làm tròn số nguyên (docs/07 §6).
    /// Mỗi tầng N thử ở đầu dải level khuyến nghị (5N − 4) và cuối dải (5N).
    /// </summary>
    public class BalanceSandboxTests
    {
        private const string SlimePath = "Assets/_Game/Data/Enemies/Enemy_Slime.asset";
        private const string BatPath = "Assets/_Game/Data/Enemies/Enemy_Bat.asset";
        private const string CombatBalanceConfigPath = "Assets/_Game/Data/Config/CombatBalanceConfig.asset";
        private const string Floor01Path = "Assets/_Game/Content/Floors/Floor01/FloorDefinition_Floor01.asset";
        private const string Floor02Path = "Assets/_Game/Content/Floors/Floor02/FloorDefinition_Floor02.asset";
        private const string GameplayScenePath = "Assets/_Game/Scenes/PvE_Gameplay.unity";

        private const int FirstFloor = 1;
        private const int LastFloor = 10;

        // Khoảng mục tiêu (docs/07 §4.2, §9).
        private const int MinHitsToKillEnemy = 3;
        private const int MaxHitsToKillEnemy = 5;
        private const int MinHitsToKillPlayer = 10;
        private const int MaxHitsToKillPlayer = 20;
        private const int MinEnemiesPerLevel = 8;
        private const int MaxEnemiesPerLevel = 15;

        private const double ExpectedDefenseWeight = 0.3;

        private double _defenseWeight;

        [SetUp]
        public void Setup()
        {
            var config = AssetDatabase.LoadAssetAtPath<CombatBalanceConfig>(CombatBalanceConfigPath);
            Assert.IsNotNull(config, $"Không tìm thấy CombatBalanceConfig tại {CombatBalanceConfigPath}");

            _defenseWeight = config.defenseWeight;
            CombatBalance.DefenseWeight = _defenseWeight;
        }

        [TearDown]
        public void TearDown()
        {
            // CombatBalance.DefenseWeight là biến static: trả về mặc định 1.0 để test khác không bị ảnh hưởng.
            CombatBalance.DefenseWeight = 1.0;
        }

        // ── Bảng balance Tầng 1–10 ──

        [TestCase(SlimePath, TestName = "Slime_Floors1To10_AllMetricsInRange")]
        [TestCase(BatPath, TestName = "Bat_Floors1To10_AllMetricsInRange")]
        public void Enemy_Floors1To10_AllMetricsInRange(string enemyPath)
        {
            Assert.AreEqual(ExpectedDefenseWeight, _defenseWeight, 0.000001,
                "CombatBalanceConfig.defenseWeight phải là 0.3 thì bảng này mới có nghĩa");

            EnemyDefinition enemy = LoadEnemy(enemyPath);
            var failures = new List<string>();
            var table = new StringBuilder();

            table.AppendLine($"=== {enemy.DisplayName} | defenseWeight {_defenseWeight} | gốc HP {enemy.MaxHealth} / ATK {enemy.Attack} / DEF {enemy.Defense} / EXP {enemy.ExperienceReward} ===");
            table.AppendLine("Tầng | Lv | HP quái | ATK quái | Đòn hạ quái [3-5] | Đòn quái hạ người [10-20] | EXP quái | EXPNext | Quái/level [8-15]");

            for (int floor = FirstFloor; floor <= LastFloor; floor++)
            {
                foreach (int level in new[] { RecommendedLevelMin(floor), RecommendedLevelMax(floor) })
                {
                    Row row = Evaluate(enemy, floor, level);
                    table.AppendLine(FormatRow(row));
                    CollectFailures(enemy, row, failures);
                }
            }

            // In bảng TRƯỚC khi assert để bảng luôn hiện, kể cả khi có ca đỏ.
            TestContext.WriteLine(table.ToString());

            Assert.IsEmpty(failures,
                $"Có ca nằm ngoài khoảng mục tiêu ({enemy.DisplayName}). Đừng chỉnh mò — báo bảng kết quả:\n" + string.Join("\n", failures));
        }

        // ── Asset FloorDefinition khớp hàm FloorScaling ──

        [TestCase(Floor01Path, 1, TestName = "FloorDefinition_Floor01_MatchesFloorScaling")]
        [TestCase(Floor02Path, 2, TestName = "FloorDefinition_Floor02_MatchesFloorScaling")]
        public void FloorDefinitionAsset_ResolvedMultipliers_MatchFloorScaling(string path, int expectedFloorNumber)
        {
            var def = AssetDatabase.LoadAssetAtPath<FloorDefinition>(path);
            Assert.IsNotNull(def, $"Không tìm thấy FloorDefinition tại {path}");
            Assert.AreEqual(expectedFloorNumber, def.floorNumber, "floorNumber của asset lệch tầng");

            // scalingOverride = 0 nghĩa là lấy từ hàm FloorScaling. Nếu ai đó điền số vào thì số đó phải khớp hàm.
            float power = def.scalingOverride != null
                ? def.scalingOverride.ResolveEnemyPower(def.floorNumber)
                : FloorScaling.EnemyPowerMultiplier(def.floorNumber);
            float exp = def.scalingOverride != null
                ? def.scalingOverride.ResolveExp(def.floorNumber)
                : FloorScaling.ExpMultiplier(def.floorNumber);

            Assert.AreEqual(FloorScaling.EnemyPowerMultiplier(def.floorNumber), power, 0.0001f,
                $"{def.id}: hệ số sức mạnh của asset lệch hàm FloorScaling");
            Assert.AreEqual(FloorScaling.ExpMultiplier(def.floorNumber), exp, 0.0001f,
                $"{def.id}: hệ số EXP của asset lệch hàm FloorScaling");

            // Dải level khuyến nghị của tầng N = 5N − 4 … 5N (docs/07 §1).
            Assert.AreEqual(RecommendedLevelMin(def.floorNumber), def.recommendedLevelMin,
                $"{def.id}: recommendedLevelMin phải = 5N − 4");
            Assert.AreEqual(RecommendedLevelMax(def.floorNumber), def.recommendedLevelMax,
                $"{def.id}: recommendedLevelMax phải = 5N");
        }

        // ── Scene PvE_Gameplay nạp đúng CombatBalanceConfig (defenseWeight 0.3) ──

        [Test]
        public void PvEGameplayScene_Bootstrap_HasCombatBalanceConfig_WithDefenseWeightPointThree()
        {
            var scene = EditorSceneManager.GetSceneByPath(GameplayScenePath);
            bool openedByTest = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
                openedByTest = true;
            }

            try
            {
                PvEBootstrap bootstrap = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    bootstrap = root.GetComponentInChildren<PvEBootstrap>(true);
                    if (bootstrap != null) break;
                }

                Assert.IsNotNull(bootstrap, "Thiếu PvEBootstrap trong scene PvE_Gameplay");

                var so = new SerializedObject(bootstrap);
                var config = so.FindProperty("combatBalanceConfig").objectReferenceValue as CombatBalanceConfig;
                Assert.IsNotNull(config, "PvEBootstrap.combatBalanceConfig chưa gán → game chạy với defenseWeight 1.0");
                Assert.AreEqual(CombatBalanceConfigPath, AssetDatabase.GetAssetPath(config),
                    "PvEBootstrap đang gán sai asset CombatBalanceConfig");
                Assert.AreEqual(ExpectedDefenseWeight, config.defenseWeight, 0.000001,
                    "defenseWeight của CombatBalanceConfig phải là 0.3");
            }
            finally
            {
                if (openedByTest) EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ── Helper ──

        private static int RecommendedLevelMin(int floor) => 5 * floor - 4;
        private static int RecommendedLevelMax(int floor) => 5 * floor;

        private static EnemyDefinition LoadEnemy(string path)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            Assert.IsNotNull(enemy, $"Không tìm thấy asset quái tại {path}");
            return enemy;
        }

        private struct Row
        {
            public int Floor;
            public int Level;
            public double EnemyHp;
            public double EnemyAtk;
            public double HitDamage;          // đòn thường người chơi gây lên quái
            public double EnemyHitDamage;     // đòn quái gây lên người chơi
            public int HitsToKillEnemy;
            public int HitsToKillPlayer;
            public int EnemyExp;
            public int ExpNext;
            public int EnemiesPerLevel;
        }

        private Row Evaluate(EnemyDefinition enemy, int floor, int level)
        {
            var player = new CharacterStats(level);

            // Quái thật: EnemyController áp hệ số tầng + làm tròn số nguyên giống hệt lúc chạy game.
            double enemyHp, enemyAtk, enemyDef;
            int enemyExp;
            var go = new GameObject("BalanceSandboxEnemy");
            go.SetActive(false); // không chạy Awake/OnEnable, chỉ cần đọc chỉ số
            try
            {
                var controller = go.AddComponent<EnemyController>();
                controller.SetDefinition(enemy);
                controller.SetFloor(floor);
                enemyHp = controller.MaxHealth;
                enemyAtk = controller.Attack;
                enemyDef = controller.Defense;
                enemyExp = controller.ExperienceReward;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            // Đòn thường người chơi: hệ số kỹ năng 1.0, không chí mạng.
            double hitDamage = DamageCalculator.Calculate(
                player.Attack, enemyDef, 1.0, false, player.CriticalDamage, _defenseWeight);
            // Người chơi đứng im, quái đánh thường (không chí mạng).
            double enemyHitDamage = DamageCalculator.Calculate(
                enemyAtk, player.Defense, 1.0, false, 1.0, _defenseWeight);

            int expNext = CharacterStats.ExpRequiredForNextLevel(level);

            return new Row
            {
                Floor = floor,
                Level = level,
                EnemyHp = enemyHp,
                EnemyAtk = enemyAtk,
                HitDamage = hitDamage,
                EnemyHitDamage = enemyHitDamage,
                HitsToKillEnemy = (int)Math.Ceiling(enemyHp / hitDamage),
                HitsToKillPlayer = (int)Math.Ceiling(player.MaxHealth / enemyHitDamage),
                EnemyExp = enemyExp,
                ExpNext = expNext,
                EnemiesPerLevel = enemyExp > 0 ? (int)Math.Ceiling((double)expNext / enemyExp) : int.MaxValue
            };
        }

        private static string FormatRow(Row r)
        {
            return $"{r.Floor,4} | {r.Level,2} | {r.EnemyHp,7:F1} | {r.EnemyAtk,8:F1} | {r.HitsToKillEnemy,17} | {r.HitsToKillPlayer,25} | {r.EnemyExp,8} | {r.ExpNext,7} | {r.EnemiesPerLevel,17}";
        }

        private static void CollectFailures(EnemyDefinition enemy, Row r, List<string> failures)
        {
            string who = $"{enemy.DisplayName} Tầng {r.Floor} Lv {r.Level}";

            if (r.HitsToKillEnemy < MinHitsToKillEnemy || r.HitsToKillEnemy > MaxHitsToKillEnemy)
                failures.Add($"{who}: đòn hạ quái = {r.HitsToKillEnemy} (cần {MinHitsToKillEnemy}–{MaxHitsToKillEnemy})");
            if (r.HitsToKillPlayer < MinHitsToKillPlayer || r.HitsToKillPlayer > MaxHitsToKillPlayer)
                failures.Add($"{who}: đòn quái hạ người đứng im = {r.HitsToKillPlayer} (cần {MinHitsToKillPlayer}–{MaxHitsToKillPlayer})");
            if (r.EnemiesPerLevel < MinEnemiesPerLevel || r.EnemiesPerLevel > MaxEnemiesPerLevel)
                failures.Add($"{who}: số quái để lên level = {r.EnemiesPerLevel} (cần {MinEnemiesPerLevel}–{MaxEnemiesPerLevel})");
            if (r.HitDamage <= 1.0)
                failures.Add($"{who}: damage người chơi lên quái = {r.HitDamage} đang chạm clamp 1");
            if (r.EnemyHitDamage <= 1.0)
                failures.Add($"{who}: damage quái lên người chơi = {r.EnemyHitDamage} đang chạm clamp 1");
        }
    }
}
