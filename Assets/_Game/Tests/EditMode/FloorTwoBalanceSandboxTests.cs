using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Core.Combat;
using Game.Core.Floor;
using Game.Core.Stats;
using Game.PvE.Boss;
using Game.PvE.Enemy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Phase 9 (P9-3) — sandbox cân bằng riêng cho Tầng 2 (docs/07 §5.1, §6, §9).
    /// KHÔNG gõ tay số liệu: quái đọc từ asset THẬT (tìm theo ID bằng AssetDatabase), hệ số tầng lấy từ
    /// FloorScaling qua EnemyController (cùng nguồn lúc chạy game, gồm cả bước làm tròn), người chơi là
    /// CharacterStats thật, defenseWeight đọc từ CombatBalanceConfig thật.
    /// Thử ở đầu dải (Lv 6) và cuối dải (Lv 10) theo FloorDefinition_Floor02.
    ///
    /// Đơn vị mục tiêu theo docs/07 §5: quái thường đo bằng SỐ ĐÒN; Elite đo bằng GIÂY (12–20s). Giây = số đòn × cooldown
    /// Attack_PlayerBasic THẬT ÷ hiệu suất trúng (BossBalanceModel.PlayerHitEfficiency, cùng giả định với sandbox boss, docs/07 §5.2).
    /// Đây là ƯỚC LƯỢNG đua sát thương (không tính thời gian tiếp cận); số đo thật lấy từ điện thoại (docs/07 §9).
    /// </summary>
    public class FloorTwoBalanceSandboxTests
    {
        private const string CombatBalanceConfigPath = "Assets/_Game/Data/Config/CombatBalanceConfig.asset";
        private const string PlayerBasicAttackPath = "Assets/_Game/Data/Config/Attack_PlayerBasic.asset";
        private const string Floor02Path = "Assets/_Game/Content/Floors/Floor02/FloorDefinition_Floor02.asset";
        private const string EnemiesFolder = "Assets/_Game/Data/Enemies";

        // ID quái thật (đọc từ asset Tầng 2). Tìm asset theo ID, không theo đường dẫn.
        private const string GuardId = "enemy.boiler_guard";
        private const string EliteId = "enemy.pressure_warden";
        private const string SlimeId = "enemy.slime";
        private const string BatId = "enemy.bat";

        // Quái của Tầng 2 đã nằm trong một bộ test cân bằng nào đó:
        //  - Guard, Elite: file này;
        //  - Bat: BalanceSandboxTests (Tầng 1–10, gồm Tầng 2).
        // Thêm quái mới vào spawnEnemyIds của floor.02 mà quên test thì test "AllCovered" sẽ đỏ.
        private static readonly string[] CoveredEnemyIds = { GuardId, EliteId, BatId };

        // Khoảng mục tiêu (docs/07 §4.2, §5, §5.1, §9).
        private const int GuardHitsToKillMin = 4, GuardHitsToKillMax = 7;
        private const int GuardHitsToKillPlayerMin = 10, GuardHitsToKillPlayerMax = 20;
        private const int GuardPerLevelMin = 8, GuardPerLevelMax = 15;
        private const double EliteSecondsToKillMin = 12.0, EliteSecondsToKillMax = 20.0;
        private const int EliteHitsToKillPlayerMin = 5, EliteHitsToKillPlayerMax = 10;

        private const double ExpectedDefenseWeight = 0.3;

        private double _defenseWeight;
        private AttackDefinition _basicAttack;

        [SetUp]
        public void Setup()
        {
            var config = AssetDatabase.LoadAssetAtPath<CombatBalanceConfig>(CombatBalanceConfigPath);
            Assert.IsNotNull(config, $"Không tìm thấy CombatBalanceConfig tại {CombatBalanceConfigPath}");
            _defenseWeight = config.defenseWeight;
            CombatBalance.DefenseWeight = _defenseWeight;

            _basicAttack = AssetDatabase.LoadAssetAtPath<AttackDefinition>(PlayerBasicAttackPath);
            Assert.IsNotNull(_basicAttack, $"Không tìm thấy AttackDefinition tại {PlayerBasicAttackPath}");
        }

        [TearDown]
        public void TearDown()
        {
            CombatBalance.DefenseWeight = 1.0;
        }

        // ── Guard ───────────────────────────────────────────────────────────

        [Test]
        public void Guard_Floor2_AllMetricsInRange()
        {
            Assert.AreEqual(ExpectedDefenseWeight, _defenseWeight, 0.000001, "defenseWeight phải là 0.3.");
            var floor = LoadFloor2();
            EnemyDefinition guard = FindEnemyById(GuardId);

            var failures = new List<string>();
            var table = new StringBuilder();
            table.AppendLine(Header(guard, floor));

            foreach (int level in Levels(floor))
            {
                Row r = Evaluate(guard, floor.floorNumber, level);
                table.AppendLine(Format(r));

                if (r.HitsToKillEnemy < GuardHitsToKillMin || r.HitsToKillEnemy > GuardHitsToKillMax)
                    failures.Add($"Guard Lv {level}: đòn thường hạ Guard = {r.HitsToKillEnemy} (cần {GuardHitsToKillMin}–{GuardHitsToKillMax})");
                if (r.HitsToKillPlayer < GuardHitsToKillPlayerMin || r.HitsToKillPlayer > GuardHitsToKillPlayerMax)
                    failures.Add($"Guard Lv {level}: Guard hạ người đứng im = {r.HitsToKillPlayer} (cần {GuardHitsToKillPlayerMin}–{GuardHitsToKillPlayerMax})");
                if (r.EnemiesPerLevel < GuardPerLevelMin || r.EnemiesPerLevel > GuardPerLevelMax)
                    failures.Add($"Guard Lv {level}: số Guard để lên level = {r.EnemiesPerLevel} (cần {GuardPerLevelMin}–{GuardPerLevelMax})");
                CollectClampFailures("Guard", r, failures);
            }

            TestContext.WriteLine(table.ToString());
            Assert.IsEmpty(failures,
                "Guard Tầng 2 có ca ngoài khoảng mục tiêu. Đừng chỉnh mò — báo bảng kết quả:\n" + string.Join("\n", failures));
        }

        // ── Elite ───────────────────────────────────────────────────────────

        [Test]
        public void Elite_Floor2_AllMetricsInRange()
        {
            Assert.AreEqual(ExpectedDefenseWeight, _defenseWeight, 0.000001, "defenseWeight phải là 0.3.");
            var floor = LoadFloor2();
            EnemyDefinition elite = FindEnemyById(EliteId);

            var failures = new List<string>();
            var table = new StringBuilder();
            table.AppendLine(Header(elite, floor));

            foreach (int level in Levels(floor))
            {
                Row r = Evaluate(elite, floor.floorNumber, level);
                table.AppendLine(Format(r));

                if (r.SecondsToKillEnemy < EliteSecondsToKillMin || r.SecondsToKillEnemy > EliteSecondsToKillMax)
                    failures.Add($"Elite Lv {level}: hạ Elite mất ≈ {r.SecondsToKillEnemy:F1}s ({r.HitsToKillEnemy} đòn) (cần {EliteSecondsToKillMin:F0}–{EliteSecondsToKillMax:F0}s)");
                if (r.HitsToKillPlayer < EliteHitsToKillPlayerMin || r.HitsToKillPlayer > EliteHitsToKillPlayerMax)
                    failures.Add($"Elite Lv {level}: Elite hạ người đứng im = {r.HitsToKillPlayer} (cần {EliteHitsToKillPlayerMin}–{EliteHitsToKillPlayerMax})");
                // Elite KHÔNG bị ràng buộc "quái mỗi level" (docs/07 §5.1) — chỉ in để tham khảo.
                CollectClampFailures("Elite", r, failures);
            }

            TestContext.WriteLine(table.ToString());
            Assert.IsEmpty(failures,
                "Elite Tầng 2 có ca ngoài khoảng mục tiêu. Đừng chỉnh mò — báo bảng kết quả:\n" + string.Join("\n", failures));
        }

        // ── Telegraph: điểm dạy chính của Tầng 2 ───────────────────────────

        [Test]
        public void Guard_WindupIsLongerThanSlime()
        {
            float guard = WindupOf(FindEnemyById(GuardId));
            float slime = WindupOf(FindEnemyById(SlimeId));
            TestContext.WriteLine($"attackWindupSeconds: Guard {guard:F2}s | Slime {slime:F2}s");
            Assert.Greater(guard, slime, "Guard phải báo đòn lâu hơn Slime (telegraph dài là điểm dạy chính của Tầng 2).");
        }

        // ── Không quên quái mới ─────────────────────────────────────────────

        [Test]
        public void Floor2_EveryEnemyInSpawnList_HasAssetAndIsCoveredBySandbox()
        {
            var floor = LoadFloor2();
            Assert.IsNotEmpty(floor.spawnEnemyIds, "floor.02 chưa có spawnEnemyIds.");

            foreach (var id in floor.spawnEnemyIds)
            {
                Assert.IsNotNull(TryFindEnemyById(id), $"floor.02 có '{id}' nhưng không có asset EnemyDefinition nào mang ID này.");
                CollectionAssert.Contains(CoveredEnemyIds, id,
                    $"Quái '{id}' của floor.02 chưa có trong bộ test cân bằng. Thêm vào FloorTwoBalanceSandboxTests rồi mới thêm quái.");
            }
        }

        // ── Helper ──────────────────────────────────────────────────────────

        private static FloorDefinition LoadFloor2()
        {
            var def = AssetDatabase.LoadAssetAtPath<FloorDefinition>(Floor02Path);
            Assert.IsNotNull(def, $"Không tìm thấy FloorDefinition tại {Floor02Path}");
            Assert.AreEqual(2, def.floorNumber);
            return def;
        }

        private static int[] Levels(FloorDefinition floor)
        {
            return new[] { floor.recommendedLevelMin, floor.recommendedLevelMax };
        }

        private static EnemyDefinition TryFindEnemyById(string id)
        {
            return AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemiesFolder })
                .Select(g => AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(e => e != null && e.Id == id);
        }

        private static EnemyDefinition FindEnemyById(string id)
        {
            var enemy = TryFindEnemyById(id);
            Assert.IsNotNull(enemy, $"Không có asset EnemyDefinition nào mang ID '{id}'.");
            return enemy;
        }

        private static float WindupOf(EnemyDefinition enemy)
        {
            var p = new SerializedObject(enemy).FindProperty("attackWindupSeconds");
            Assert.IsNotNull(p, "EnemyDefinition không có field attackWindupSeconds.");
            return p.floatValue;
        }

        private static void CollectClampFailures(string who, Row r, List<string> failures)
        {
            if (r.HitDamage <= 1.0)
                failures.Add($"{who} Lv {r.Level}: damage người chơi lên quái = {r.HitDamage} đang chạm clamp 1");
            if (r.EnemyHitDamage <= 1.0)
                failures.Add($"{who} Lv {r.Level}: damage quái lên người chơi = {r.EnemyHitDamage} đang chạm clamp 1");
        }

        private struct Row
        {
            public int Floor;
            public int Level;
            public double EnemyHp, EnemyAtk, EnemyDef;
            public double HitDamage;
            public double EnemyHitDamage;
            public int HitsToKillEnemy;
            public double SecondsToKillEnemy;
            public int HitsToKillPlayer;
            public int EnemyExp;
            public int ExpNext;
            public int EnemiesPerLevel;
        }

        private Row Evaluate(EnemyDefinition enemy, int floor, int level)
        {
            var player = new CharacterStats(level);

            double hp, atk, def;
            int exp;
            var go = new GameObject("FloorTwoBalanceSandboxEnemy");
            go.SetActive(false); // không chạy Awake/OnEnable, chỉ đọc chỉ số
            try
            {
                var controller = go.AddComponent<EnemyController>();
                controller.SetDefinition(enemy);
                controller.SetFloor(floor);
                hp = controller.MaxHealth;
                atk = controller.Attack;
                def = controller.Defense;
                exp = controller.ExperienceReward;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            double hit = DamageCalculator.Calculate(player.Attack, def, _basicAttack.DamageMultiplier, false, player.CriticalDamage, _defenseWeight);
            double enemyHit = DamageCalculator.Calculate(atk, player.Defense, 1.0, false, 1.0, _defenseWeight);
            int expNext = CharacterStats.ExpRequiredForNextLevel(level);
            int hitsToKillEnemy = (int)Math.Ceiling(hp / hit);

            return new Row
            {
                Floor = floor,
                Level = level,
                EnemyHp = hp,
                EnemyAtk = atk,
                EnemyDef = def,
                HitDamage = hit,
                EnemyHitDamage = enemyHit,
                HitsToKillEnemy = hitsToKillEnemy,
                SecondsToKillEnemy = hitsToKillEnemy * (double)_basicAttack.CooldownSeconds / BossBalanceModel.PlayerHitEfficiency,
                HitsToKillPlayer = (int)Math.Ceiling(player.MaxHealth / enemyHit),
                EnemyExp = exp,
                ExpNext = expNext,
                EnemiesPerLevel = exp > 0 ? (int)Math.Ceiling((double)expNext / exp) : int.MaxValue
            };
        }

        private string Header(EnemyDefinition enemy, FloorDefinition floor)
        {
            return $"=== {enemy.DisplayName} ({enemy.Id}) | Tầng {floor.floorNumber} | defenseWeight {_defenseWeight} | " +
                   $"gốc HP {enemy.MaxHealth} / ATK {enemy.Attack} / DEF {enemy.Defense} / EXP {enemy.ExperienceReward} ===\n" +
                   "Lv | HP | ATK | DEF | EXP | Dame người→quái | Dame quái→người | Đòn hạ quái | Giây hạ quái (ước lượng) | Đòn hạ người đứng im | EXPNext | Quái/level";
        }

        private static string Format(Row r)
        {
            return $"{r.Level,2} | {r.EnemyHp,5:F0} | {r.EnemyAtk,3:F0} | {r.EnemyDef,3:F0} | {r.EnemyExp,3} | " +
                   $"{r.HitDamage,5:F0} | {r.EnemyHitDamage,5:F0} | {r.HitsToKillEnemy,3} | {r.SecondsToKillEnemy,5:F1} | {r.HitsToKillPlayer,3} | {r.ExpNext,4} | {r.EnemiesPerLevel,3}";
        }
    }
}
