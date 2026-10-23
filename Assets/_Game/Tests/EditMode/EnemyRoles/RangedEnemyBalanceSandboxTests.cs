using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Core.Combat;
using Game.Core.Floor;
using Game.Core.Stats;
using Game.PvE.Boss;
using Game.PvE.Enemy;
using Game.PvE.Enemy.Projectile;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// P9.8 — sandbox cân bằng cho hai quái vai trò mới (docs/07 §5, §5.1).
    ///
    /// Số liệu của hai quái nằm ở <see cref="RoleEnemyNumbers"/> (nguồn duy nhất cho bước Unity):
    /// test dựng EnemyDefinition trong bộ nhớ từ các số này (không cần asset), tính bằng đúng nguồn lúc chạy game
    /// (CharacterStats, DamageCalculator, FloorScaling qua EnemyController, ProjectileMath) và so với khoảng mục tiêu.
    /// Khi asset thật đã được tạo (bước U4), test cuối cùng kiểm asset khớp RoleEnemyNumbers để hai nơi không lệch nhau.
    ///
    /// Mô hình giây giống FloorTwoBalanceSandboxTests: giây = số đòn × cooldown Attack_PlayerBasic ÷ BossBalanceModel.PlayerHitEfficiency
    /// (0.45s ÷ 70%). Đây là ƯỚC LƯỢNG đua sát thương; số đo thật lấy từ điện thoại (docs/07 §9).
    ///
    /// Mức người chơi đo:
    ///  - Ranged Tầng 1 (phần sâu Vùng Farm): Lv 3 và Lv 5. Dải đầy đủ Lv 1–5 không thể đồng thời đạt 8–14 giây ở cả hai đầu
    ///    (damage người chơi tăng 10 → 18, tỉ lệ 1.8 lớn hơn tỉ lệ 21/13 của khoảng mục tiêu), nên quái này đặt ở phần sâu của Vùng Farm
    ///    và đo từ Lv 3. Ghi vào Handoff.
    ///  - Ranged và Skirmisher Tầng 2: đầu và cuối dải recommended level của floor.02 (Lv 6 và Lv 10).
    /// </summary>
    public class RangedEnemyBalanceSandboxTests
    {
        private const string CombatBalanceConfigPath = "Assets/_Game/Data/Config/CombatBalanceConfig.asset";
        private const string PlayerBasicAttackPath = "Assets/_Game/Data/Config/Attack_PlayerBasic.asset";
        private const string Floor02Path = "Assets/_Game/Content/Floors/Floor02/FloorDefinition_Floor02.asset";
        private const string EnemiesFolder = "Assets/_Game/Data/Enemies";

        // Khoảng mục tiêu (docs/07 §5, §5.1).
        private const double SecondsToKillMin = 8.0, SecondsToKillMax = 14.0;
        private const int HitsToKillPlayerMin = 10, HitsToKillPlayerMax = 20;
        private const int PerLevelMin = 8, PerLevelMax = 15;
        private const double ExpectedDefenseWeight = 0.3;

        private static readonly int[] RangedFloor1Levels = { 3, 5 };

        private double _defenseWeight;
        private AttackDefinition _basicAttack;
        private readonly List<UnityEngine.Object> _temp = new List<UnityEngine.Object>();

        // ── Số liệu hai quái mới (nguồn cho bước Unity) ─────────────────────

        public sealed class RoleEnemyNumbers
        {
            public string Id, DisplayName;
            public CombatRole Role;
            public double MaxHealth, Attack, Defense;
            public int Exp;
            public float MoveSpeed, DetectionRange, AttackRange, AttackCooldown, AttackWindup, HurtSeconds, DeathSeconds;
            public float PreferredRange, RetreatRange, RetreatSeconds, ShootRange;
            public float ProjectileSpeed, ProjectileLifetime, ProjectileRadius, ProjectileDamageScale;

            // Ranged: đứng xa, báo đòn 0.6s rồi bắn một viên đạn né được (bay từ tầm bắn tới người chơi 0.75s).
            public static readonly RoleEnemyNumbers PipeSpitter = new RoleEnemyNumbers
            {
                Id = "enemy.pipe_spitter", DisplayName = "Ống Phun", Role = CombatRole.Ranged,
                MaxHealth = 240, Attack = 14, Defense = 0, Exp = 14,
                MoveSpeed = 1.6f, DetectionRange = 7f, AttackRange = 1.1f,
                AttackCooldown = 2.2f, AttackWindup = 0.6f, HurtSeconds = 0.25f, DeathSeconds = 0.6f,
                PreferredRange = 4.5f, RetreatRange = 2.5f, RetreatSeconds = 0.8f, ShootRange = 6f,
                ProjectileSpeed = 8f, ProjectileLifetime = 1.5f, ProjectileRadius = 0.25f, ProjectileDamageScale = 0.9f
            };

            // Skirmisher: áp sát, đánh cận chiến, lùi 0.8s rồi áp sát lại. preferredRange/shootRange chỉ để thỏa thứ tự kẹp.
            public static readonly RoleEnemyNumbers WireStalker = new RoleEnemyNumbers
            {
                Id = "enemy.wire_stalker", DisplayName = "Dây Rình", Role = CombatRole.Skirmisher,
                MaxHealth = 230, Attack = 11, Defense = 0, Exp = 14,
                MoveSpeed = 2.4f, DetectionRange = 6f, AttackRange = 1.1f,
                AttackCooldown = 1.6f, AttackWindup = 0.4f, HurtSeconds = 0.25f, DeathSeconds = 0.6f,
                PreferredRange = 3.5f, RetreatRange = 2.5f, RetreatSeconds = 0.8f, ShootRange = 4f,
                ProjectileSpeed = 8f, ProjectileLifetime = 1.5f, ProjectileRadius = 0.25f, ProjectileDamageScale = 1f
            };
        }

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
            foreach (var o in _temp) if (o != null) UnityEngine.Object.DestroyImmediate(o);
            _temp.Clear();
        }

        // ── Đo cân bằng ─────────────────────────────────────────────────────

        [Test]
        public void PipeSpitter_Floor1DeepFarm_AllMetricsInRange()
        {
            RunMetrics(RoleEnemyNumbers.PipeSpitter, 1, RangedFloor1Levels);
        }

        [Test]
        public void PipeSpitter_Floor2_AllMetricsInRange()
        {
            FloorDefinition floor = LoadFloor2();
            RunMetrics(RoleEnemyNumbers.PipeSpitter, floor.floorNumber, new[] { floor.recommendedLevelMin, floor.recommendedLevelMax });
        }

        [Test]
        public void WireStalker_Floor2_AllMetricsInRange()
        {
            FloorDefinition floor = LoadFloor2();
            RunMetrics(RoleEnemyNumbers.WireStalker, floor.floorNumber, new[] { floor.recommendedLevelMin, floor.recommendedLevelMax });
        }

        private void RunMetrics(RoleEnemyNumbers numbers, int floorNumber, int[] levels)
        {
            Assert.AreEqual(ExpectedDefenseWeight, _defenseWeight, 0.000001, "defenseWeight phải là 0.3.");
            EnemyDefinition definition = Build(numbers);

            var failures = new List<string>();
            var table = new StringBuilder();
            table.AppendLine($"=== {numbers.DisplayName} ({numbers.Id}, {numbers.Role}) | Tầng {floorNumber} | defenseWeight {_defenseWeight} ===");
            table.AppendLine("Lv | HP | ATK | DEF | EXP | Dame người→quái | Đòn hạ quái | Giây hạ quái (ước lượng) | Dame mỗi đòn/đạn quái→người | Đòn hạ người đứng im | Quái/level");

            foreach (int level in levels)
            {
                Row r = Evaluate(definition, numbers, floorNumber, level);
                table.AppendLine($"{r.Level,2} | {r.EnemyHp,4:F0} | {r.EnemyAtk,3:F0} | {r.EnemyDef,3:F0} | {r.EnemyExp,3} | {r.HitDamage,4:F0} | {r.HitsToKillEnemy,3} | " +
                                 $"{r.SecondsToKillEnemy,5:F1} | {r.EnemyHitDamage,4:F0} | {r.HitsToKillPlayer,3} | {r.EnemiesPerLevel,3}");

                if (r.SecondsToKillEnemy < SecondsToKillMin || r.SecondsToKillEnemy > SecondsToKillMax)
                    failures.Add($"{numbers.DisplayName} Tầng {floorNumber} Lv {level}: hạ mất ≈ {r.SecondsToKillEnemy:F1}s ({r.HitsToKillEnemy} đòn) (cần {SecondsToKillMin:F0}–{SecondsToKillMax:F0}s)");
                if (r.HitsToKillPlayer < HitsToKillPlayerMin || r.HitsToKillPlayer > HitsToKillPlayerMax)
                    failures.Add($"{numbers.DisplayName} Tầng {floorNumber} Lv {level}: hạ người đứng im = {r.HitsToKillPlayer} đòn (cần {HitsToKillPlayerMin}–{HitsToKillPlayerMax})");
                if (r.EnemiesPerLevel < PerLevelMin || r.EnemiesPerLevel > PerLevelMax)
                    failures.Add($"{numbers.DisplayName} Tầng {floorNumber} Lv {level}: số quái để lên level = {r.EnemiesPerLevel} (cần {PerLevelMin}–{PerLevelMax})");
                if (r.HitDamage <= 1.0)
                    failures.Add($"{numbers.DisplayName} Tầng {floorNumber} Lv {level}: damage người chơi lên quái = {r.HitDamage} đang chạm clamp 1");
                if (r.EnemyHitDamage <= 1.0)
                    failures.Add($"{numbers.DisplayName} Tầng {floorNumber} Lv {level}: damage quái lên người chơi = {r.EnemyHitDamage} đang chạm clamp 1");
            }

            TestContext.WriteLine(table.ToString());
            Assert.IsEmpty(failures,
                $"{numbers.DisplayName} có ca ngoài khoảng mục tiêu. Đừng chỉnh mò — báo bảng kết quả:\n" + string.Join("\n", failures));
        }

        // ── Đạn né được và thứ tự tầm ───────────────────────────────────────

        [Test]
        public void PipeSpitter_ProjectileIsDodgeable_AndReachesPlayerFromShootRange()
        {
            RoleEnemyNumbers n = RoleEnemyNumbers.PipeSpitter;
            float flight = ProjectileMath.FlightSeconds(n.ShootRange, n.ProjectileSpeed);
            TestContext.WriteLine($"Đạn bay từ tầm bắn {n.ShootRange} với tốc độ {n.ProjectileSpeed}: {flight:F2}s (tối thiểu {ProjectileMath.MinDodgeFlightSeconds}s)");

            Assert.IsTrue(ProjectileMath.IsDodgeable(n.ShootRange, n.ProjectileSpeed), "Đạn phải đủ chậm để né được (>= 0.5s bay từ tầm bắn).");
            Assert.GreaterOrEqual(n.ProjectileLifetime, flight, "Đạn phải sống đủ lâu để bay hết tầm bắn.");
        }

        [Test]
        public void BothDefinitions_RangeOrderingHolds_AfterClamp()
        {
            foreach (RoleEnemyNumbers n in new[] { RoleEnemyNumbers.PipeSpitter, RoleEnemyNumbers.WireStalker })
            {
                EnemyDefinition d = Build(n);
                d.ClampRoleFields();
                Assert.Less(d.RetreatRange, d.PreferredRange, n.Id);
                Assert.LessOrEqual(d.PreferredRange, d.ShootRange, n.Id);
                Assert.LessOrEqual(d.ShootRange, d.DetectionRange, n.Id);
                // Kẹp không được làm đổi số đã chọn (số đã thỏa thứ tự).
                Assert.AreEqual(n.RetreatRange, d.RetreatRange, 1e-6f, n.Id + " retreatRange");
                Assert.AreEqual(n.PreferredRange, d.PreferredRange, 1e-6f, n.Id + " preferredRange");
                Assert.AreEqual(n.ShootRange, d.ShootRange, 1e-6f, n.Id + " shootRange");
            }
        }

        [Test]
        public void PipeSpitter_WindupIsLongerThanSlime_SoTelegraphIsReadable()
        {
            // Slime dùng windup mặc định 0.35s (asset không ghi field này). Quái bắn xa phải báo đòn lâu hơn.
            Assert.Greater(RoleEnemyNumbers.PipeSpitter.AttackWindup, 0.35f);
        }

        // ── Asset thật (sau bước U4) phải khớp số ở trên ────────────────────

        [Test]
        public void Assets_WhenCreated_MatchTheNumbersInThisFile()
        {
            bool anyChecked = false;
            foreach (RoleEnemyNumbers n in new[] { RoleEnemyNumbers.PipeSpitter, RoleEnemyNumbers.WireStalker })
            {
                EnemyDefinition asset = TryFindEnemyById(n.Id);
                if (asset == null) continue;
                anyChecked = true;

                Assert.AreEqual(n.Role, asset.Role, n.Id + " role");
                Assert.AreEqual(n.MaxHealth, asset.MaxHealth, 1e-6, n.Id + " maxHealth");
                Assert.AreEqual(n.Attack, asset.Attack, 1e-6, n.Id + " attack");
                Assert.AreEqual(n.Defense, asset.Defense, 1e-6, n.Id + " defense");
                Assert.AreEqual(n.Exp, asset.ExperienceReward, n.Id + " exp");
                Assert.AreEqual(n.MoveSpeed, asset.MoveSpeed, 1e-6f, n.Id + " moveSpeed");
                Assert.AreEqual(n.DetectionRange, asset.DetectionRange, 1e-6f, n.Id + " detectionRange");
                Assert.AreEqual(n.AttackRange, asset.AttackRange, 1e-6f, n.Id + " attackRange");
                Assert.AreEqual(n.AttackCooldown, asset.AttackCooldownSeconds, 1e-6f, n.Id + " cooldown");
                Assert.AreEqual(n.AttackWindup, asset.AttackWindupSeconds, 1e-6f, n.Id + " windup");
                Assert.AreEqual(n.PreferredRange, asset.PreferredRange, 1e-6f, n.Id + " preferredRange");
                Assert.AreEqual(n.RetreatRange, asset.RetreatRange, 1e-6f, n.Id + " retreatRange");
                Assert.AreEqual(n.RetreatSeconds, asset.RetreatSeconds, 1e-6f, n.Id + " retreatSeconds");
                Assert.AreEqual(n.ShootRange, asset.ShootRange, 1e-6f, n.Id + " shootRange");
                Assert.AreEqual(n.ProjectileSpeed, asset.ProjectileSpeed, 1e-6f, n.Id + " projectileSpeed");
                Assert.AreEqual(n.ProjectileLifetime, asset.ProjectileLifetime, 1e-6f, n.Id + " projectileLifetime");
                Assert.AreEqual(n.ProjectileRadius, asset.ProjectileRadius, 1e-6f, n.Id + " projectileRadius");
                Assert.AreEqual(n.ProjectileDamageScale, asset.ProjectileDamageScale, 1e-6f, n.Id + " projectileDamageScale");
            }

            if (!anyChecked) Assert.Ignore("Chưa có asset enemy.pipe_spitter / enemy.wire_stalker (bước Unity U4 sẽ tạo).");
        }

        // ── Helper ──────────────────────────────────────────────────────────

        private static FloorDefinition LoadFloor2()
        {
            var def = AssetDatabase.LoadAssetAtPath<FloorDefinition>(Floor02Path);
            Assert.IsNotNull(def, $"Không tìm thấy FloorDefinition tại {Floor02Path}");
            Assert.AreEqual(2, def.floorNumber);
            return def;
        }

        private static EnemyDefinition TryFindEnemyById(string id)
        {
            return AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemiesFolder })
                .Select(g => AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(e => e != null && e.Id == id);
        }

        private EnemyDefinition Build(RoleEnemyNumbers n)
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            _temp.Add(def);
            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue = n.Id;
            so.FindProperty("displayName").stringValue = n.DisplayName;
            so.FindProperty("maxHealth").doubleValue = n.MaxHealth;
            so.FindProperty("attack").doubleValue = n.Attack;
            so.FindProperty("defense").doubleValue = n.Defense;
            so.FindProperty("experienceReward").intValue = n.Exp;
            so.FindProperty("moveSpeed").floatValue = n.MoveSpeed;
            so.FindProperty("detectionRange").floatValue = n.DetectionRange;
            so.FindProperty("attackRange").floatValue = n.AttackRange;
            so.FindProperty("attackCooldownSeconds").floatValue = n.AttackCooldown;
            so.FindProperty("attackWindupSeconds").floatValue = n.AttackWindup;
            so.FindProperty("hurtSeconds").floatValue = n.HurtSeconds;
            so.FindProperty("deathSeconds").floatValue = n.DeathSeconds;
            so.FindProperty("role").enumValueIndex = (int)n.Role;
            so.FindProperty("preferredRange").floatValue = n.PreferredRange;
            so.FindProperty("retreatRange").floatValue = n.RetreatRange;
            so.FindProperty("retreatSeconds").floatValue = n.RetreatSeconds;
            so.FindProperty("shootRange").floatValue = n.ShootRange;
            so.FindProperty("projectileSpeed").floatValue = n.ProjectileSpeed;
            so.FindProperty("projectileLifetime").floatValue = n.ProjectileLifetime;
            so.FindProperty("projectileRadius").floatValue = n.ProjectileRadius;
            so.FindProperty("projectileDamageScale").floatValue = n.ProjectileDamageScale;
            so.ApplyModifiedPropertiesWithoutUndo();
            return def;
        }

        private struct Row
        {
            public int Level;
            public double EnemyHp, EnemyAtk, EnemyDef;
            public int EnemyExp;
            public double HitDamage;
            public int HitsToKillEnemy;
            public double SecondsToKillEnemy;
            public double EnemyHitDamage;
            public int HitsToKillPlayer;
            public int EnemiesPerLevel;
        }

        private Row Evaluate(EnemyDefinition definition, RoleEnemyNumbers numbers, int floor, int level)
        {
            var player = new CharacterStats(level);

            double hp, atk, def;
            int exp;
            var go = new GameObject("RangedEnemyBalanceSandboxEnemy");
            go.SetActive(false); // không chạy Awake/OnEnable, chỉ đọc chỉ số
            try
            {
                var controller = go.AddComponent<EnemyController>();
                controller.SetDefinition(definition);
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

            // Đúng đường lúc chạy: damage tính như đòn cận chiến rồi nhân hệ số đạn (EnemyController.CreateAttackDamage).
            double enemyHit = DamageCalculator.Calculate(atk, player.Defense, 1.0, false, 1.0, _defenseWeight);
            if (numbers.Role == CombatRole.Ranged) enemyHit = ProjectileMath.ScaleDamage(enemyHit, numbers.ProjectileDamageScale);

            int hitsToKillEnemy = (int)Math.Ceiling(hp / hit);
            int expNext = CharacterStats.ExpRequiredForNextLevel(level);

            return new Row
            {
                Level = level,
                EnemyHp = hp,
                EnemyAtk = atk,
                EnemyDef = def,
                EnemyExp = exp,
                HitDamage = hit,
                HitsToKillEnemy = hitsToKillEnemy,
                SecondsToKillEnemy = hitsToKillEnemy * (double)_basicAttack.CooldownSeconds / BossBalanceModel.PlayerHitEfficiency,
                EnemyHitDamage = enemyHit,
                HitsToKillPlayer = (int)Math.Ceiling(player.MaxHealth / enemyHit),
                EnemiesPerLevel = exp > 0 ? (int)Math.Ceiling((double)expNext / exp) : int.MaxValue
            };
        }
    }
}
