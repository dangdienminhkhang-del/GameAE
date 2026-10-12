using System.Reflection;
using System.Text.RegularExpressions;
using Game.Core.Combat;
using Game.PvE.Boss;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task P8-4: đòn Boss phải né được (docs/12 §4, docs/07 §5).
    /// Phần 1 kiểm luật hình học thuần (<see cref="BossHitRules"/>, <see cref="BossDashHitGate"/>).
    /// Phần 2 kiểm BossController thật (không cần scene): đòn thường chỉ trúng nếu mục tiêu còn trong vùng đánh
    /// TẠI THỜI ĐIỂM đánh; mỗi lần lao chỉ gây damage một lần.
    /// Phần 3 kiểm field data mới của BossDefinition (mặc định, validate &gt; 0, asset cũ thiếu field).
    /// </summary>
    public class BossHitRulesTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private const double TestDefenseWeight = 0.3;
        private const float Tolerance = 0.0001f;

        private double _previousDefenseWeight;

        [SetUp]
        public void SetUp()
        {
            _previousDefenseWeight = CombatBalance.DefenseWeight;
            CombatBalance.DefenseWeight = TestDefenseWeight;
        }

        [TearDown]
        public void TearDown()
        {
            CombatBalance.DefenseWeight = _previousDefenseWeight;
        }

        // ── 1. Vùng đánh thường ─────────────────────────────────────────────────

        [Test]
        public void IsInsideHitArea_TargetInsideRadius_Hits()
        {
            Assert.IsTrue(BossHitRules.IsInsideHitArea(Vector2.zero, new Vector2(1.0f, 0f), BossDefinition.DefaultAttackHitRadius));
            Assert.IsTrue(BossHitRules.IsInsideHitArea(new Vector2(5f, 5f), new Vector2(5f, 6.5f), BossDefinition.DefaultAttackHitRadius));
        }

        [Test]
        public void IsInsideHitArea_TargetExactlyOnBoundary_StillHits()
        {
            Assert.IsTrue(BossHitRules.IsInsideHitArea(Vector2.zero, new Vector2(1.8f, 0f), 1.8f));
        }

        [Test]
        public void IsInsideHitArea_TargetBeyondRadius_Misses()
        {
            Assert.IsFalse(BossHitRules.IsInsideHitArea(Vector2.zero, new Vector2(2.5f, 0f), BossDefinition.DefaultAttackHitRadius));
            Assert.IsFalse(BossHitRules.IsInsideHitArea(Vector2.zero, new Vector2(1.5f, 1.5f), BossDefinition.DefaultAttackHitRadius),
                "Chéo (1.5, 1.5) cách 2.12 > 1.8 phải trượt");
        }

        [Test]
        public void IsInsideHitArea_NegativeRadius_NeverHits()
        {
            Assert.IsFalse(BossHitRules.IsInsideHitArea(Vector2.zero, Vector2.zero, -1f));
        }

        [Test]
        public void PlayerRetreatsBeforeTheHit_TelegraphStartedInRange_ButHitMisses()
        {
            // Telegraph chỉ bắt đầu khi mục tiêu trong attackRange (1.6) — luôn nằm trong vùng đánh (1.8)...
            var definition = ScriptableObject.CreateInstance<BossDefinition>();
            try
            {
                Vector2 boss = Vector2.zero;
                Vector2 targetWhenTelegraphStarts = new Vector2(definition.AttackRange - 0.1f, 0f);
                Assert.IsTrue(BossHitRules.IsInsideHitArea(boss, targetWhenTelegraphStarts, definition.AttackHitRadius),
                    "Lúc bắt đầu telegraph mục tiêu phải đang trong vùng đánh");

                // ...nhưng nếu người chơi lùi ra ngoài vùng trước khi đòn rơi thì trượt.
                Vector2 targetWhenHitLands = new Vector2(definition.AttackHitRadius + 0.5f, 0f);
                Assert.IsFalse(BossHitRules.IsInsideHitArea(boss, targetWhenHitLands, definition.AttackHitRadius),
                    "Đã lùi ra ngoài vùng đánh trong lúc telegraph thì không được dính đòn");
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        // ── 2. Hành lang lao ────────────────────────────────────────────────────

        [Test]
        public void DashCorridor_TargetOutsideCorridor_Misses()
        {
            Vector2 start = Vector2.zero;
            Vector2 end = new Vector2(4f, 0f);
            float radius = BossDefinition.DefaultPressureHitRadius;

            Assert.IsFalse(BossHitRules.IsInsideDashCorridor(start, end, new Vector2(2f, 2.0f), radius), "Lệch ngang 2.0 > 1.4");
            Assert.IsFalse(BossHitRules.IsInsideDashCorridor(start, end, new Vector2(-2f, 0f), radius), "Sau điểm xuất phát 2.0 > 1.4");
            Assert.IsFalse(BossHitRules.IsInsideDashCorridor(start, end, new Vector2(6f, 0f), radius), "Quá điểm cuối 2.0 > 1.4");
        }

        [Test]
        public void DashCorridor_TargetInsideCorridor_Hits()
        {
            Vector2 start = Vector2.zero;
            Vector2 end = new Vector2(4f, 0f);
            float radius = BossDefinition.DefaultPressureHitRadius;

            Assert.IsTrue(BossHitRules.IsInsideDashCorridor(start, end, new Vector2(2f, 0f), radius), "Ngay trên đường lao");
            Assert.IsTrue(BossHitRules.IsInsideDashCorridor(start, end, new Vector2(2f, 1.0f), radius), "Lệch ngang 1.0 ≤ 1.4");
            Assert.IsTrue(BossHitRules.IsInsideDashCorridor(start, end, new Vector2(2f, 1.4f), radius), "Đúng biên 1.4 vẫn trúng");
            Assert.IsTrue(BossHitRules.IsInsideDashCorridor(start, end, new Vector2(-1f, 0f), radius), "Sát sau điểm xuất phát (đầu lao)");
            Assert.IsTrue(BossHitRules.IsInsideDashCorridor(start, end, new Vector2(5f, 0f), radius), "Sát sau điểm cuối (đuôi lao)");
        }

        [Test]
        public void DashCorridor_WorksForDiagonalDash()
        {
            Vector2 start = new Vector2(1f, 1f);
            Vector2 direction = new Vector2(1f, 1f).normalized;
            Vector2 end = BossHitRules.ResolveDashEnd(start, direction, 4f);
            float radius = BossDefinition.DefaultPressureHitRadius;

            Vector2 onPath = start + direction * 2f;
            Vector2 sideStep = onPath + new Vector2(-direction.y, direction.x) * 2.0f; // vuông góc 2.0

            Assert.IsTrue(BossHitRules.IsInsideDashCorridor(start, end, onPath, radius));
            Assert.IsFalse(BossHitRules.IsInsideDashCorridor(start, end, sideStep, radius));
        }

        [Test]
        public void DistanceToSegment_DegenerateSegment_IsDistanceToPoint()
        {
            Assert.AreEqual(5f, BossHitRules.DistanceToSegment(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(4f, 5f)), Tolerance);
        }

        [Test]
        public void ResolveDashEnd_IsStartPlusDirectionTimesDistance()
        {
            Vector2 end = BossHitRules.ResolveDashEnd(new Vector2(1f, 2f), Vector2.up, BossDefinition.DefaultPressureDashDistance);
            Assert.AreEqual(1f, end.x, Tolerance);
            Assert.AreEqual(2f + BossDefinition.DefaultPressureDashDistance, end.y, Tolerance);
        }

        [Test]
        public void ResolveDashDirection_IsNormalized_AndFallsBackWhenOverlapping()
        {
            Vector2 toTarget = BossHitRules.ResolveDashDirection(Vector2.zero, new Vector2(3f, 4f), Vector2.left);
            Assert.AreEqual(1f, toTarget.magnitude, Tolerance);
            Assert.AreEqual(0.6f, toTarget.x, Tolerance);
            Assert.AreEqual(0.8f, toTarget.y, Tolerance);

            Vector2 overlapped = BossHitRules.ResolveDashDirection(new Vector2(2f, 2f), new Vector2(2f, 2f), new Vector2(-5f, 0f));
            Assert.AreEqual(-1f, overlapped.x, Tolerance, "Trùng vị trí thì dùng hướng dự phòng (đã chuẩn hóa)");
            Assert.AreEqual(0f, overlapped.y, Tolerance);

            Vector2 noFallback = BossHitRules.ResolveDashDirection(Vector2.zero, Vector2.zero, Vector2.zero);
            Assert.AreEqual(Vector2.right, noFallback);
        }

        // ── 3. Mỗi lần lao chỉ gây damage một lần (cổng thuần) ──────────────────

        [Test]
        public void DashHitGate_TargetInsideCorridor_HitsExactlyOncePerDash()
        {
            var gate = new BossDashHitGate();
            Vector2 start = Vector2.zero;
            Vector2 target = new Vector2(2f, 0.5f);
            float radius = BossDefinition.DefaultPressureHitRadius;

            Assert.IsTrue(gate.TryHit(start, new Vector2(2f, 0f), target, radius), "Lần đầu lọt vào hành lang → trúng");
            Assert.IsTrue(gate.HasHit);
            Assert.IsFalse(gate.TryHit(start, new Vector2(3f, 0f), target, radius), "Cùng lần lao: không trúng lần hai");
            Assert.IsFalse(gate.TryHit(start, new Vector2(4f, 0f), target, radius), "Cùng lần lao: không trúng lần ba");
        }

        [Test]
        public void DashHitGate_Reset_AllowsNextDashToHitAgain()
        {
            var gate = new BossDashHitGate();
            float radius = BossDefinition.DefaultPressureHitRadius;
            Vector2 target = new Vector2(1f, 0f);

            Assert.IsTrue(gate.TryHit(Vector2.zero, new Vector2(1f, 0f), target, radius));
            gate.Reset();
            Assert.IsFalse(gate.HasHit);
            Assert.IsTrue(gate.TryHit(Vector2.zero, new Vector2(1f, 0f), target, radius), "Lần lao mới được trúng một lần nữa");
        }

        [Test]
        public void DashHitGate_TargetOutsideCorridor_NeverHits_AndStaysArmed()
        {
            var gate = new BossDashHitGate();
            float radius = BossDefinition.DefaultPressureHitRadius;
            Vector2 target = new Vector2(2f, 3f);

            for (float x = 0f; x <= 4f; x += 0.5f)
            {
                Assert.IsFalse(gate.TryHit(Vector2.zero, new Vector2(x, 0f), target, radius), $"Đứng ngoài hành lang, boss tới x={x} vẫn phải trượt");
            }
            Assert.IsFalse(gate.HasHit, "Trượt thì cổng chưa bị dùng");
        }

        [Test]
        public void DashHitGate_CorridorIsSweptPath_TargetAheadOfBossIsHitOnlyWhenReached()
        {
            var gate = new BossDashHitGate();
            float radius = BossDefinition.DefaultPressureHitRadius;
            Vector2 target = new Vector2(3.5f, 0f);

            Assert.IsFalse(gate.TryHit(Vector2.zero, new Vector2(1.0f, 0f), target, radius), "Boss mới lao 1.0, mục tiêu còn cách 2.5");
            Assert.IsTrue(gate.TryHit(Vector2.zero, new Vector2(2.5f, 0f), target, radius), "Boss tới gần (cách 1.0) → trúng");
        }

        // ── 4. Damage đòn lao ───────────────────────────────────────────────────

        [Test]
        public void ResolvePressureDamage_ScalesAndRoundsLikeDamageCalculator()
        {
            // 1.3f là 1.29999995 khi ép sang double: 25 × 1.3 phải ra 33 (32.5 làm tròn xa 0), không phải 32.
            Assert.AreEqual(33.0, BossHitRules.ResolvePressureDamage(25.0, BossDefinition.DefaultPressureDamageMultiplier), 0.0001);
            Assert.AreEqual(34.0, BossHitRules.ResolvePressureDamage(26.0, BossDefinition.DefaultPressureDamageMultiplier), 0.0001);
            Assert.AreEqual(1.0, BossHitRules.ResolvePressureDamage(1.0, 0.1), 0.0001, "Không dưới 1");
        }

        // ── 5. BossController thật: đòn thường chỉ trúng khi còn trong vùng ─────

        [Test]
        public void Controller_PerformAttack_TargetInsideHitArea_DamagesOnce_WithBossSourceId()
        {
            using (var rig = new Rig())
            {
                rig.TargetObject.transform.position = new Vector3(1.0f, 0f, 0f);

                rig.Boss.PerformAttack();

                Assert.AreEqual(1, rig.Player.HitCount, "Trong vùng đánh phải trúng đúng một lần");
                Assert.AreEqual(rig.Definition.Id, rig.Player.LastSourceId, "Phải truyền sourceId = definition.Id để permadeath ghi đúng lý do");
                double expected = DamageCalculator.Calculate(rig.Definition.Attack, rig.Player.Defense, 1.0, false, 1.0, TestDefenseWeight);
                Assert.AreEqual(expected, rig.Player.LastAmount, 0.0001);
            }
        }

        [Test]
        public void Controller_PerformAttack_PlayerRetreatedBeforeTheHitLands_Misses()
        {
            using (var rig = new Rig())
            {
                // Lúc telegraph bắt đầu người chơi đứng sát...
                rig.TargetObject.transform.position = new Vector3(1.0f, 0f, 0f);
                // ...rồi lùi ra ngoài vùng đánh trước khi đòn rơi.
                rig.TargetObject.transform.position = new Vector3(rig.Definition.AttackHitRadius + 0.5f, 0f, 0f);

                rig.Boss.PerformAttack();

                Assert.AreEqual(0, rig.Player.HitCount, "Ngoài vùng đánh tại thời điểm đánh thì phải trượt");
            }
        }

        [Test]
        public void Controller_PerformAttack_Miss_StillCountsTowardsPressure()
        {
            using (var rig = new Rig())
            {
                rig.TargetObject.transform.position = new Vector3(rig.Definition.AttackHitRadius + 1f, 0f, 0f);

                for (int i = 0; i < rig.Definition.PressureEveryNAttacks - 1; i++)
                {
                    rig.Boss.PerformAttack();
                    Assert.IsFalse(rig.Boss.IsPressureDue, "Chưa đủ số đòn thường");
                }

                rig.Boss.PerformAttack();
                Assert.IsTrue(rig.Boss.IsPressureDue, "Đủ pressureEveryNAttacks đòn (kể cả đòn trượt) thì đến lượt Pressure");
                Assert.AreEqual(0, rig.Player.HitCount);
            }
        }

        // ── 6. BossController thật: mỗi lần lao chỉ một lần damage ──────────────

        [Test]
        public void Controller_PressureDash_TargetInsideCorridor_DamagesExactlyOncePerDash()
        {
            using (var rig = new Rig())
            {
                rig.TargetObject.transform.position = new Vector3(2f, 0.5f, 0f);

                rig.Invoke("BeginPressureDash");
                rig.Boss.transform.position = new Vector3(2f, 0f, 0f); // boss đã quét tới gần mục tiêu
                rig.Invoke("ResolvePressureHit");
                rig.Boss.transform.position = new Vector3(3f, 0f, 0f);
                rig.Invoke("ResolvePressureHit");
                rig.Invoke("ResolvePressureHit");

                Assert.AreEqual(1, rig.Player.HitCount, "Một lần lao chỉ gây damage một lần");
                double normal = DamageCalculator.Calculate(rig.Definition.Attack, rig.Player.Defense, 1.0, false, 1.0, TestDefenseWeight);
                double expected = BossHitRules.ResolvePressureDamage(normal, rig.Definition.PressureDamageMultiplier);
                Assert.AreEqual(expected, rig.Player.LastAmount, 0.0001, "Damage đòn lao = đòn thường × pressureDamageMultiplier");
                Assert.AreEqual(rig.Definition.Id, rig.Player.LastSourceId);

                rig.Invoke("BeginPressureDash"); // lần lao mới
                rig.Invoke("ResolvePressureHit");
                Assert.AreEqual(2, rig.Player.HitCount, "Lần lao mới được gây damage một lần nữa");
            }
        }

        [Test]
        public void Controller_PressureDash_TargetOutsideCorridor_Misses()
        {
            using (var rig = new Rig())
            {
                rig.TargetObject.transform.position = new Vector3(2f, 3f, 0f);

                rig.Invoke("BeginPressureDash");
                for (float x = 0f; x <= 4f; x += 1f)
                {
                    rig.Boss.transform.position = new Vector3(x, 0f, 0f);
                    rig.Invoke("ResolvePressureHit");
                }

                Assert.AreEqual(0, rig.Player.HitCount, "Đứng ngoài hành lang lao thì không dính đòn");
            }
        }

        // ── 7. Field data mới của BossDefinition ────────────────────────────────

        [Test]
        public void BossDefinition_NewFields_DefaultToSpecValues()
        {
            var definition = ScriptableObject.CreateInstance<BossDefinition>();
            try
            {
                Assert.AreEqual(1.8f, definition.AttackHitRadius, Tolerance);
                Assert.AreEqual(2, definition.PressureEveryNAttacks);
                Assert.AreEqual(0.9f, definition.PressureWindupSeconds, Tolerance);
                Assert.AreEqual(4.0f, definition.PressureDashDistance, Tolerance);
                Assert.AreEqual(0.35f, definition.PressureDashSeconds, Tolerance);
                Assert.AreEqual(1.4f, definition.PressureHitRadius, Tolerance);
                Assert.AreEqual(1.3f, definition.PressureDamageMultiplier, Tolerance);
                Assert.AreEqual(1.6f, definition.PressureRecoverySeconds, Tolerance);
                Assert.AreEqual(definition.PressureDashDistance + definition.PressureHitRadius, definition.PressureTriggerRange, Tolerance);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void BossDefinition_OldAssetMissingNewFields_StillLoadsWithDefaults()
        {
            // Asset cũ: YAML chỉ có field cũ. Unity giữ giá trị khởi tạo cho field vắng mặt — mô phỏng bằng JSON thiếu field.
            var definition = ScriptableObject.CreateInstance<BossDefinition>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"maxHealth\":250,\"attack\":18,\"defense\":4}", definition);

                Assert.AreEqual(250.0, definition.MaxHealth, 0.0001, "Field cũ vẫn đọc được");
                Assert.AreEqual(BossDefinition.DefaultAttackHitRadius, definition.AttackHitRadius, Tolerance);
                Assert.AreEqual(BossDefinition.DefaultPressureEveryNAttacks, definition.PressureEveryNAttacks);
                Assert.AreEqual(BossDefinition.DefaultPressureWindupSeconds, definition.PressureWindupSeconds, Tolerance);
                Assert.AreEqual(BossDefinition.DefaultPressureDashDistance, definition.PressureDashDistance, Tolerance);
                Assert.AreEqual(BossDefinition.DefaultPressureDashSeconds, definition.PressureDashSeconds, Tolerance);
                Assert.AreEqual(BossDefinition.DefaultPressureHitRadius, definition.PressureHitRadius, Tolerance);
                Assert.AreEqual(BossDefinition.DefaultPressureDamageMultiplier, definition.PressureDamageMultiplier, Tolerance);
                Assert.AreEqual(BossDefinition.DefaultPressureRecoverySeconds, definition.PressureRecoverySeconds, Tolerance);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [TestCase("attackHitRadius")]
        [TestCase("pressureEveryNAttacks")]
        [TestCase("pressureWindupSeconds")]
        [TestCase("pressureDashDistance")]
        [TestCase("pressureDashSeconds")]
        [TestCase("pressureHitRadius")]
        [TestCase("pressureDamageMultiplier")]
        [TestCase("pressureRecoverySeconds")]
        public void BossDefinition_NonPositiveField_GetterFallsBack_AndOnValidateRestoresDefault(string fieldName)
        {
            var definition = ScriptableObject.CreateInstance<BossDefinition>();
            try
            {
                FieldInfo field = typeof(BossDefinition).GetField(fieldName, Private);
                Assert.IsNotNull(field, $"Thiếu field {fieldName}");
                object defaultValue = field.GetValue(definition);
                field.SetValue(definition, field.FieldType == typeof(int) ? (object)0 : 0f);

                Assert.AreEqual(defaultValue, GetterValue(definition, fieldName), $"{fieldName} = 0 thì getter phải lùi về mặc định");

                LogAssert.Expect(LogType.Warning, new Regex(fieldName));
                typeof(BossDefinition).GetMethod("OnValidate", Private).Invoke(definition, null);

                Assert.AreEqual(defaultValue, field.GetValue(definition), $"OnValidate phải đặt lại {fieldName} về mặc định");
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        private static object GetterValue(BossDefinition definition, string fieldName)
        {
            string property = char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
            return typeof(BossDefinition).GetProperty(property).GetValue(definition);
        }

        // ── Dàn dựng BossController thật, không cần scene ───────────────────────

        private sealed class TestPlayer : MonoBehaviour, IDamageable, ICombatStats
        {
            public int HitCount;
            public double LastAmount;
            public string LastSourceId;

            public double CurrentHealth { get; private set; } = 1000.0;
            public double MaxHealth => 1000.0;
            public bool IsDead => CurrentHealth <= 0.0;

            public double Attack => 0.0;
            public double Defense => 9.0;
            public double CriticalChance => 0.0;
            public double CriticalDamage => 1.0;

            public void TakeDamage(DamageInfo damage)
            {
                HitCount++;
                LastAmount = damage.Amount;
                LastSourceId = damage.SourceId;
                CurrentHealth -= damage.Amount;
            }

            public void Heal(double amount) { }
            public void SetMaxHealth(double maxHp, bool resetCurrent = false) { }
        }

        private sealed class Rig : System.IDisposable
        {
            public readonly GameObject BossObject;
            public readonly GameObject TargetObject;
            public readonly BossController Boss;
            public readonly TestPlayer Player;
            public readonly BossDefinition Definition;

            public Rig()
            {
                Definition = ScriptableObject.CreateInstance<BossDefinition>();

                // Tạo ở trạng thái tắt để Awake/Start không chạy (không service, không tìm Player tag).
                BossObject = new GameObject("BossHitRulesTests_Boss");
                BossObject.SetActive(false);
                Boss = BossObject.AddComponent<BossController>();
                Boss.SetDefinition(Definition);

                TargetObject = new GameObject("BossHitRulesTests_Player");
                Player = TargetObject.AddComponent<TestPlayer>();

                typeof(BossController).GetField("_target", Private).SetValue(Boss, TargetObject.transform);
            }

            public void Invoke(string methodName)
            {
                typeof(BossController).GetMethod(methodName, Private).Invoke(Boss, null);
            }

            public void Dispose()
            {
                Object.DestroyImmediate(BossObject);
                Object.DestroyImmediate(TargetObject);
                Object.DestroyImmediate(Definition);
            }
        }
    }
}
