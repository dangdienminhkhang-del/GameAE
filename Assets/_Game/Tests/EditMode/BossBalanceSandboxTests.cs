using System.IO;
using System.Text;
using Game.Core.Combat;
using Game.Core.Stats;
using Game.PvE.Boss;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task P8-4 — Sandbox cân bằng Boss Tầng 1 (docs/07 §5, §9; docs/12 §10). KHÔNG gõ tay số liệu:
    /// - boss: đọc asset THẬT <c>Boss_Floor01.asset</c> bằng AssetDatabase;
    /// - defenseWeight: đọc từ <c>CombatBalanceConfig.asset</c> THẬT (cùng nguồn PvEBootstrap nạp lúc chạy game);
    /// - người chơi: <see cref="CharacterStats"/> THẬT theo level (Lv 5 = chuẩn, Lv 4 = cận);
    /// - đòn người chơi: <c>Attack_PlayerBasic.asset</c> THẬT (cooldown/hệ số lấy từ asset).
    ///
    /// ĐÂY CHỈ LÀ ƯỚC LƯỢNG (xem giả định ở <see cref="BossBalanceModel"/> và docs/07 §5.2). Số đo thật phải lấy từ
    /// điện thoại (docs/07 §9 — Device PvE; docs/12 §10). Test đỏ nghĩa là số boss lệch mục tiêu —
    /// chỉnh số trong asset (maxHealth 1200–2000, attack 23–36) rồi chạy lại, đừng nới khoảng kiểm.
    /// </summary>
    public class BossBalanceSandboxTests
    {
        private const string BossPath = "Assets/_Game/Data/Enemies/Boss_Floor01.asset";
        private const string CombatBalanceConfigPath = "Assets/_Game/Data/Config/CombatBalanceConfig.asset";
        private const string PlayerBasicAttackPath = "Assets/_Game/Data/Config/Attack_PlayerBasic.asset";

        private const double ExpectedDefenseWeight = 0.3;

        // Level người chơi: cuối dải recommended của Tầng 1 (5N, docs/07 §1) và thấp hơn một level.
        private const int StandardLevel = 5;
        private const int LowLevel = 4;

        // Mục tiêu (docs/12 §10; yêu cầu P8-4).
        private const double MinFirstClearSeconds = 90.0;
        private const double MaxFirstClearSeconds = 150.0;
        private const double MaxLowLevelFirstClearSeconds = 190.0;
        private const int MinNormalHitsToKillStandard = 5;
        private const int MaxNormalHitsToKillStandard = 8;
        private const int MinNormalHitsToKillLow = 4;

        // Các field mới của BossDefinition phải có dòng YAML tường minh trong asset.
        private static readonly string[] PressureYamlFields =
        {
            "attackHitRadius", "pressureEveryNAttacks", "pressureWindupSeconds", "pressureDashDistance",
            "pressureDashSeconds", "pressureHitRadius", "pressureDamageMultiplier", "pressureRecoverySeconds"
        };

        private BossDefinition _boss;
        private AttackDefinition _basicAttack;
        private double _defenseWeight;

        [SetUp]
        public void Setup()
        {
            _boss = AssetDatabase.LoadAssetAtPath<BossDefinition>(BossPath);
            Assert.IsNotNull(_boss, $"Không tìm thấy BossDefinition tại {BossPath}");

            var config = AssetDatabase.LoadAssetAtPath<CombatBalanceConfig>(CombatBalanceConfigPath);
            Assert.IsNotNull(config, $"Không tìm thấy CombatBalanceConfig tại {CombatBalanceConfigPath}");
            _defenseWeight = config.defenseWeight;

            _basicAttack = AssetDatabase.LoadAssetAtPath<AttackDefinition>(PlayerBasicAttackPath);
            Assert.IsNotNull(_basicAttack, $"Không tìm thấy AttackDefinition tại {PlayerBasicAttackPath}");

            Assert.AreEqual(ExpectedDefenseWeight, _defenseWeight, 0.000001,
                "CombatBalanceConfig.defenseWeight phải là 0.3 thì bảng sandbox boss này mới có nghĩa");
        }

        // ── (a) Thời gian hạ boss lần đầu ────────────────────────────────────────

        [Test]
        public void FirstClear_StandardLevel5_TakesBetween90And150Seconds()
        {
            var result = Evaluate(StandardLevel);
            PrintTable();

            Assert.GreaterOrEqual(result.TimeToKillSeconds, MinFirstClearSeconds,
                $"Lv {StandardLevel}: hạ boss quá nhanh ({result.TimeToKillSeconds:F1}s < {MinFirstClearSeconds}s). Tăng maxHealth (≤ 2000) hoặc giảm defense nếu cần.");
            Assert.LessOrEqual(result.TimeToKillSeconds, MaxFirstClearSeconds,
                $"Lv {StandardLevel}: hạ boss quá lâu ({result.TimeToKillSeconds:F1}s > {MaxFirstClearSeconds}s). Giảm maxHealth (≥ 1200) trước khi đụng tới chỉ số người chơi.");
        }

        [Test]
        public void FirstClear_LowLevel4_NotLongerThan190Seconds()
        {
            var result = Evaluate(LowLevel);
            PrintTable();

            Assert.LessOrEqual(result.TimeToKillSeconds, MaxLowLevelFirstClearSeconds,
                $"Lv {LowLevel}: hạ boss quá lâu ({result.TimeToKillSeconds:F1}s > {MaxLowLevelFirstClearSeconds}s).");
            Assert.Greater(result.TimeToKillSeconds, Evaluate(StandardLevel).TimeToKillSeconds - 0.0001,
                "Level thấp hơn không được hạ boss nhanh hơn level chuẩn.");
        }

        // ── (b) Boss hạ người chơi đứng yên ──────────────────────────────────────

        [Test]
        public void BossKillsStandingPlayer_Level5_In5To8NormalHits()
        {
            var result = Evaluate(StandardLevel);
            PrintTable();

            Assert.GreaterOrEqual(result.NormalHitsToKillPlayer, MinNormalHitsToKillStandard,
                $"Lv {StandardLevel}: boss hạ người đứng yên chỉ trong {result.NormalHitsToKillPlayer} đòn thường (cần ≥ {MinNormalHitsToKillStandard}).");
            Assert.LessOrEqual(result.NormalHitsToKillPlayer, MaxNormalHitsToKillStandard,
                $"Lv {StandardLevel}: boss cần {result.NormalHitsToKillPlayer} đòn thường để hạ người đứng yên (cần ≤ {MaxNormalHitsToKillStandard}).");
        }

        [Test]
        public void BossKillsStandingPlayer_Level4_InNoFewerThan4NormalHits()
        {
            var result = Evaluate(LowLevel);
            PrintTable();

            Assert.GreaterOrEqual(result.NormalHitsToKillPlayer, MinNormalHitsToKillLow,
                $"Lv {LowLevel}: boss hạ người đứng yên chỉ trong {result.NormalHitsToKillPlayer} đòn thường (cần ≥ {MinNormalHitsToKillLow}).");
        }

        [Test]
        public void PressureHit_IsStrongerThanNormalHit_ButNeverAOneShot()
        {
            foreach (int level in new[] { StandardLevel, LowLevel })
            {
                var result = Evaluate(level);
                Assert.Greater(result.BossPressureHitDamage, result.BossNormalHitDamage,
                    $"Lv {level}: đòn lao phải mạnh hơn đòn thường (hệ số {_boss.PressureDamageMultiplier}).");
                Assert.Greater(result.PressureHitsToKillPlayer, 1,
                    $"Lv {level}: một đòn lao ({result.BossPressureHitDamage}) không được hạ người từ đầy máu ({result.PlayerMaxHealth}).");
            }
        }

        // ── (c) Damage không chạm clamp 1 ────────────────────────────────────────

        [Test]
        public void Damage_NeverHitsTheClampOfOne()
        {
            foreach (int level in new[] { StandardLevel, LowLevel })
            {
                var result = Evaluate(level);
                Assert.Greater(result.PlayerHitDamage, 1.0, $"Lv {level}: damage người chơi lên boss = {result.PlayerHitDamage} đang chạm clamp 1");
                Assert.Greater(result.BossNormalHitDamage, 1.0, $"Lv {level}: damage đòn thường boss = {result.BossNormalHitDamage} đang chạm clamp 1");
                Assert.Greater(result.BossPressureHitDamage, 1.0, $"Lv {level}: damage đòn lao boss = {result.BossPressureHitDamage} đang chạm clamp 1");
            }
        }

        // ── Số liệu boss trong khoảng cho phép + asset khai báo đủ field ─────────

        [Test]
        public void BossAsset_NumbersStayInsideTheAllowedTuningRange()
        {
            Assert.IsTrue(_boss.HasValidId(), $"ID boss '{_boss.Id}' phải theo chuẩn boss.floor_NN");
            Assert.That(_boss.MaxHealth, Is.InRange(1200.0, 2000.0), "maxHealth chỉ chỉnh trong 1200–2000 (P8-4 #6)");
            Assert.That(_boss.Attack, Is.InRange(23.0, 36.0), "attack chỉ chỉnh trong 23–36 (P8-4 #6)");
        }

        [Test]
        public void BossAsset_YamlDeclaresEveryNewField()
        {
            string yaml = File.ReadAllText(Path.GetFullPath(BossPath));
            foreach (string field in PressureYamlFields)
            {
                StringAssert.Contains($"  {field}:", yaml, $"Boss_Floor01.asset thiếu dòng YAML cho field '{field}'");
            }
        }

        // ── Kiểm chính mô hình (không phụ thuộc asset) ───────────────────────────

        [Test]
        public void Model_CycleLengthComesFromDefinitionDurations()
        {
            var result = Evaluate(StandardLevel);

            double observe = BossBalanceModel.ObserveSeconds(_boss);
            double normalCycle = observe + _boss.AttackWindupSeconds + _boss.PunishWindowSeconds;
            double pressureCycle = observe + _boss.PressureWindupSeconds + _boss.PressureDashSeconds + _boss.PressureRecoverySeconds;
            double superCycle = _boss.PressureEveryNAttacks * normalCycle + pressureCycle;

            Assert.AreEqual(normalCycle, result.NormalCycleSeconds, 0.0001);
            Assert.AreEqual(pressureCycle, result.PressureCycleSeconds, 0.0001);
            Assert.AreEqual(superCycle, result.SuperCycleSeconds, 0.0001);
        }

        [Test]
        public void Model_OnlyCountsPlayerHitsInsidePunishAndRecoveryWindows()
        {
            var result = Evaluate(StandardLevel);

            int expectedHits = _boss.PressureEveryNAttacks * BossBalanceModel.HitsPerWindow(_boss.PunishWindowSeconds, _basicAttack.CooldownSeconds)
                               + BossBalanceModel.HitsPerWindow(_boss.PressureRecoverySeconds, _basicAttack.CooldownSeconds);
            Assert.AreEqual(expectedHits, result.HitsPerSuperCycle);

            double expectedDamage = expectedHits * result.PlayerHitDamage * BossBalanceModel.PlayerHitEfficiency;
            Assert.AreEqual(expectedDamage, result.ExpectedDamagePerSuperCycle, 0.0001);
            Assert.AreEqual(_boss.MaxHealth / expectedDamage * result.SuperCycleSeconds, result.TimeToKillSeconds, 0.0001);
        }

        [TestCase(1.2, 0.45, 3)]
        [TestCase(1.6, 0.45, 4)]
        [TestCase(1.35, 0.45, 4)] // 1.35/0.45 ≈ 2.9999999 do số thực: không được mất một đòn oan
        [TestCase(0.4, 0.45, 1)]  // window ngắn hơn cooldown vẫn kịp vung đòn đầu
        [TestCase(0.0, 0.45, 0)]
        [TestCase(1.2, 0.0, 0)]
        public void Model_HitsPerWindow(double windowSeconds, double cooldownSeconds, int expected)
        {
            Assert.AreEqual(expected, BossBalanceModel.HitsPerWindow(windowSeconds, cooldownSeconds));
        }

        // ── Helper ───────────────────────────────────────────────────────────────

        private BossBalanceModel.Result Evaluate(int playerLevel)
        {
            return BossBalanceModel.Evaluate(_boss, _defenseWeight, new CharacterStats(playerLevel), _basicAttack);
        }

        /// <summary>In bảng TRƯỚC khi assert để bảng luôn hiện, kể cả khi có ca đỏ.</summary>
        private void PrintTable()
        {
            var table = new StringBuilder();
            table.AppendLine($"=== {_boss.DisplayName} ({_boss.Id}) | defenseWeight {_defenseWeight} | HP {_boss.MaxHealth} / ATK {_boss.Attack} / DEF {_boss.Defense} ===");
            table.AppendLine($"Đòn người chơi: ×{_basicAttack.DamageMultiplier} mỗi {_basicAttack.CooldownSeconds}s | hiệu suất trúng {BossBalanceModel.PlayerHitEfficiency:P0} | tiếp cận {BossBalanceModel.ApproachSecondsPerCycle}s/chu kỳ");
            table.AppendLine($"Cửa sổ: Punish {_boss.PunishWindowSeconds}s | Recovery sau lao {_boss.PressureRecoverySeconds}s | cứ {_boss.PressureEveryNAttacks} đòn thường → 1 Pressure (telegraph {_boss.PressureWindupSeconds}s, lao {_boss.PressureDashSeconds}s)");
            table.AppendLine("Lv | HP người | Đòn người→boss | Đòn thường boss | Đòn lao boss | Đòn thường hạ người | Đòn lao hạ người | Đòn/Punish | Đòn/Recovery | Chu kỳ (s) | Dmg/chu kỳ | Hạ boss (s)");
            foreach (int level in new[] { StandardLevel, LowLevel })
            {
                var r = Evaluate(level);
                table.AppendLine(
                    $"{r.PlayerLevel,2} | {r.PlayerMaxHealth,8:F0} | {r.PlayerHitDamage,14:F0} | {r.BossNormalHitDamage,15:F0} | {r.BossPressureHitDamage,12:F0} | " +
                    $"{r.NormalHitsToKillPlayer,19} | {r.PressureHitsToKillPlayer,16} | {r.HitsPerNormalWindow,10} | {r.HitsPerPressureWindow,12} | " +
                    $"{r.SuperCycleSeconds,10:F2} | {r.ExpectedDamagePerSuperCycle,10:F1} | {r.TimeToKillSeconds,11:F1}");
            }
            TestContext.WriteLine(table.ToString());
        }
    }
}
