using Game.Core.Combat;
using Game.Core.Stats;
using Game.PvE.Boss;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Cân bằng boss Tầng 2 (Bộ Điều Áp Cổ) theo đúng mô hình và ngưỡng của boss Tầng 1 (BossBalanceSandboxTests),
    /// nhưng dải level chuẩn của Tầng 2 là 6–10: level chuẩn = 10 (cuối dải), level thấp = 9.
    /// Số liệu đọc từ asset thật; đây là ước lượng, số đo thật phải lấy từ thiết bị.
    /// </summary>
    public class BossFloor02BalanceTests
    {
        private const string BossPath = "Assets/_Game/Data/Enemies/Boss_Floor02.asset";
        private const string CombatBalanceConfigPath = "Assets/_Game/Data/Config/CombatBalanceConfig.asset";
        private const string PlayerBasicAttackPath = "Assets/_Game/Data/Config/Attack_PlayerBasic.asset";
        private const int StandardLevel = 10;
        private const int LowLevel = 9;

        private BossDefinition _boss;
        private CombatBalanceConfig _config;
        private AttackDefinition _attack;

        [SetUp]
        public void Setup()
        {
            _boss = AssetDatabase.LoadAssetAtPath<BossDefinition>(BossPath);
            _config = AssetDatabase.LoadAssetAtPath<CombatBalanceConfig>(CombatBalanceConfigPath);
            _attack = AssetDatabase.LoadAssetAtPath<AttackDefinition>(PlayerBasicAttackPath);
            Assert.IsNotNull(_boss, "Thiếu Boss_Floor02.asset");
            Assert.IsNotNull(_config, "Thiếu CombatBalanceConfig.asset");
            Assert.IsNotNull(_attack, "Thiếu Attack_PlayerBasic.asset");
        }

        private BossBalanceModel.Result Evaluate(int level)
        {
            return BossBalanceModel.Evaluate(_boss, _config.defenseWeight, new CharacterStats(level), _attack);
        }

        [Test]
        public void FirstClear_StandardLevel_TakesBetween90And150Seconds()
        {
            var r = Evaluate(StandardLevel);
            Assert.That(r.TimeToKillSeconds, Is.InRange(90.0, 150.0), $"Lv {StandardLevel}: hạ boss mất {r.TimeToKillSeconds:F1}s (cần 90–150s).");
        }

        [Test]
        public void FirstClear_LowLevel_NotLongerThan190Seconds_AndNotFasterThanStandard()
        {
            var low = Evaluate(LowLevel);
            Assert.LessOrEqual(low.TimeToKillSeconds, 190.0, $"Lv {LowLevel}: hạ boss mất {low.TimeToKillSeconds:F1}s (cần ≤ 190s).");
            Assert.GreaterOrEqual(low.TimeToKillSeconds, Evaluate(StandardLevel).TimeToKillSeconds - 0.0001, "Level thấp hơn không được hạ boss nhanh hơn level chuẩn.");
        }

        [Test]
        public void BossKillsStandingPlayer_StandardLevel_In5To8NormalHits()
        {
            var r = Evaluate(StandardLevel);
            Assert.That(r.NormalHitsToKillPlayer, Is.InRange(5, 8), $"Lv {StandardLevel}: boss hạ người đứng yên trong {r.NormalHitsToKillPlayer} đòn thường (cần 5–8).");
        }

        [Test]
        public void PressureHit_IsStrongerThanNormalHit_ButNeverAOneShot()
        {
            foreach (int level in new[] { StandardLevel, LowLevel })
            {
                var r = Evaluate(level);
                Assert.Greater(r.BossPressureHitDamage, r.BossNormalHitDamage, $"Lv {level}: đòn lao phải mạnh hơn đòn thường.");
                Assert.Greater(r.PressureHitsToKillPlayer, 1, $"Lv {level}: một đòn lao không được hạ người từ đầy máu.");
            }
        }

        [Test]
        public void Damage_NeverHitsTheClampOfOne()
        {
            foreach (int level in new[] { StandardLevel, LowLevel })
            {
                var r = Evaluate(level);
                Assert.Greater(r.PlayerHitDamage, 1.0, $"Lv {level}: damage người chơi lên boss chạm clamp 1.");
                Assert.Greater(r.BossNormalHitDamage, 1.0, $"Lv {level}: damage đòn thường boss chạm clamp 1.");
                Assert.Greater(r.BossPressureHitDamage, 1.0, $"Lv {level}: damage đòn lao boss chạm clamp 1.");
            }
        }

        [Test]
        public void Floor02Boss_IsTougherThanFloor01Boss()
        {
            var floor1 = AssetDatabase.LoadAssetAtPath<BossDefinition>("Assets/_Game/Data/Enemies/Boss_Floor01.asset");
            Assert.IsNotNull(floor1);
            Assert.Greater(_boss.Attack, floor1.Attack, "Boss Tầng 2 phải đánh mạnh hơn boss Tầng 1.");
            Assert.Greater(_boss.Defense, floor1.Defense, "Boss Tầng 2 phải có giáp cao hơn boss Tầng 1.");
            Assert.Greater(_boss.ExperienceReward, floor1.ExperienceReward, "Thưởng kinh nghiệm boss Tầng 2 phải cao hơn Tầng 1.");
        }
    }
}
