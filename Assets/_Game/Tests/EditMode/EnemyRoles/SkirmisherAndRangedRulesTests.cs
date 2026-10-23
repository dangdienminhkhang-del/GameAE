using Game.PvE.Enemy.Behaviors;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Chu kỳ Skirmisher: Approach → Attack → Retreat → Approach (Phase 9.8). Có trễ, không dao động, không kẹt ở pha lùi.</summary>
    public class SkirmisherCycleRulesTests
    {
        private const float AttackRange = 1.2f;
        private const float RetreatRange = 2.5f;
        private const float RetreatSeconds = 0.8f;
        private const float Dt = 0.05f;

        private SkirmisherCycleRules _rules;

        [SetUp]
        public void SetUp()
        {
            _rules = new SkirmisherCycleRules();
            _rules.Reset();
        }

        [Test]
        public void StartsInApproach()
        {
            Assert.AreEqual(SkirmisherPhase.Approach, _rules.Phase);
            Assert.AreEqual(0f, _rules.RetreatRemaining);
        }

        [Test]
        public void Approach_EntersAttack_WhenInsideAttackRange()
        {
            Assert.AreEqual(SkirmisherPhase.Approach, _rules.Advance(3f, Dt, AttackRange, RetreatRange, false));
            Assert.AreEqual(SkirmisherPhase.Attack, _rules.Advance(1.0f, Dt, AttackRange, RetreatRange, false));
        }

        [Test]
        public void Attack_HasHysteresis_StaysUntilTargetClearlyLeaves()
        {
            _rules.Advance(1.0f, Dt, AttackRange, RetreatRange, false);
            Assert.AreEqual(SkirmisherPhase.Attack, _rules.Phase);

            // Vừa quá attackRange một chút (trong biên trễ): vẫn Attack, không nhấp nháy.
            Assert.AreEqual(SkirmisherPhase.Attack, _rules.Advance(AttackRange + SkirmisherCycleRules.AttackExitMargin * 0.5f, Dt, AttackRange, RetreatRange, false));
            // Ra hẳn ngoài biên trễ: quay lại Approach.
            Assert.AreEqual(SkirmisherPhase.Approach, _rules.Advance(AttackRange + SkirmisherCycleRules.AttackExitMargin + 0.1f, Dt, AttackRange, RetreatRange, false));
        }

        [Test]
        public void OnStrike_StartsRetreat_WithTheConfiguredSeconds()
        {
            _rules.OnStrike(RetreatSeconds);
            Assert.AreEqual(SkirmisherPhase.Retreat, _rules.Phase);
            Assert.AreEqual(RetreatSeconds, _rules.RetreatRemaining, 1e-5f);
        }

        [Test]
        public void Retreat_BlocksEngageAndForcesDisengage()
        {
            _rules.OnStrike(RetreatSeconds);
            Assert.IsFalse(_rules.ShouldEngage(0.5f, AttackRange), "đang lùi thì không vào Attack dù trong tầm");
            Assert.IsTrue(_rules.ShouldDisengage(0.5f, AttackRange));
        }

        [Test]
        public void Engage_OutsideRetreat_FollowsAttackRange()
        {
            Assert.IsTrue(_rules.ShouldEngage(AttackRange, AttackRange));
            Assert.IsFalse(_rules.ShouldEngage(AttackRange + 0.01f, AttackRange));
            Assert.IsFalse(_rules.ShouldDisengage(AttackRange, AttackRange));
            Assert.IsTrue(_rules.ShouldDisengage(AttackRange + 0.01f, AttackRange));
        }

        [Test]
        public void Retreat_EndsWhenFarEnough()
        {
            _rules.OnStrike(RetreatSeconds);
            Assert.AreEqual(SkirmisherPhase.Retreat, _rules.Advance(1.5f, Dt, AttackRange, RetreatRange, false));
            Assert.AreEqual(SkirmisherPhase.Approach, _rules.Advance(RetreatRange + 0.1f, Dt, AttackRange, RetreatRange, false));
            Assert.AreEqual(0f, _rules.RetreatRemaining);
        }

        [Test]
        public void Retreat_EndsByTime_EvenIfTargetStaysClose()
        {
            _rules.OnStrike(RetreatSeconds);
            // Bị chặn đường nên khoảng cách không tăng: hết retreatSeconds vẫn sang Approach.
            SkirmisherPhase phase = SkirmisherPhase.Retreat;
            float elapsed = 0f;
            while (phase == SkirmisherPhase.Retreat && elapsed < RetreatSeconds + 1f)
            {
                phase = _rules.Advance(1.0f, Dt, AttackRange, RetreatRange, false);
                elapsed += Dt;
            }
            Assert.AreEqual(SkirmisherPhase.Approach, phase);
            Assert.LessOrEqual(elapsed, RetreatSeconds + Dt * 2f, "thoát đúng retreatSeconds, không kéo dài");
            Assert.GreaterOrEqual(elapsed, RetreatSeconds - Dt * 2f, "không thoát sớm khi chưa bị kẹt");
        }

        [Test]
        public void Retreat_ExitsImmediately_WhenStuck()
        {
            _rules.OnStrike(RetreatSeconds);
            Assert.AreEqual(SkirmisherPhase.Approach, _rules.Advance(1.0f, 0.001f, AttackRange, RetreatRange, true));
        }

        [Test]
        public void Retreat_NeverTargetsInsideAttackRange()
        {
            // retreatRange cấu hình nhỏ hơn tầm đánh: vẫn phải lùi ra ngoài tầm đánh một đoạn trước khi thôi.
            _rules.OnStrike(RetreatSeconds);
            Assert.AreEqual(SkirmisherPhase.Retreat, _rules.Advance(AttackRange, Dt, AttackRange, 0.5f, false));
        }

        [Test]
        public void NegativeDelta_IsTreatedAsZero()
        {
            _rules.OnStrike(RetreatSeconds);
            _rules.Advance(1.0f, -3f, AttackRange, RetreatRange, false);
            Assert.AreEqual(SkirmisherPhase.Retreat, _rules.Phase);
            Assert.AreEqual(RetreatSeconds, _rules.RetreatRemaining, 1e-5f);
        }

        [Test]
        public void NegativeRetreatSeconds_DoesNotLockTheCycle()
        {
            _rules.OnStrike(-5f);
            Assert.AreEqual(0f, _rules.RetreatRemaining);
            Assert.AreEqual(SkirmisherPhase.Approach, _rules.Advance(1.0f, Dt, AttackRange, RetreatRange, false));
        }

        [Test]
        public void Reset_ReturnsToApproach()
        {
            _rules.OnStrike(RetreatSeconds);
            _rules.Reset();
            Assert.AreEqual(SkirmisherPhase.Approach, _rules.Phase);
            Assert.AreEqual(0f, _rules.RetreatRemaining);
        }

        [Test]
        public void FullCycle_RunsRepeatedly_WithoutOscillationOrStalling()
        {
            // Mô phỏng: áp sát 2.4 u/s, lùi 3 u/s. Mỗi lần vào Attack thì ra đòn ngay (OnStrike).
            float distance = 5f;
            int strikes = 0;
            float retreatClock = 0f;
            float longestRetreat = 0f;
            int phaseFlips = 0;
            SkirmisherPhase previous = _rules.Phase;

            for (int step = 0; step < 400; step++) // 20 giây
            {
                SkirmisherPhase phase = _rules.Advance(distance, Dt, AttackRange, RetreatRange, false);
                if (phase != previous) phaseFlips++;
                previous = phase;

                if (phase == SkirmisherPhase.Attack)
                {
                    _rules.OnStrike(RetreatSeconds);
                    strikes++;
                    previous = _rules.Phase;
                    phaseFlips++;
                }
                else if (phase == SkirmisherPhase.Approach)
                {
                    distance = Mathf.Max(0.2f, distance - 2.4f * Dt);
                    retreatClock = 0f;
                }
                else
                {
                    distance += 3f * Dt;
                    retreatClock += Dt;
                    longestRetreat = Mathf.Max(longestRetreat, retreatClock);
                }
            }

            Assert.GreaterOrEqual(strikes, 8, "chu kỳ phải lặp lại nhiều lần trong 20 giây");
            Assert.LessOrEqual(strikes, 30, "không đánh dồn dập (dao động)");
            Assert.LessOrEqual(longestRetreat, RetreatSeconds + Dt * 2f, "không có lần lùi nào dài hơn retreatSeconds");
            Assert.LessOrEqual(phaseFlips, strikes * 4, "mỗi chu kỳ chỉ đổi pha vài lần");
        }

        [Test]
        public void BlockedWholeTime_StillCyclesAndNeverGetsStuckInRetreat()
        {
            // Khoảng cách không đổi (bị chặn tứ phía): các lần lùi vẫn hết giờ, quái vẫn quay lại Approach/Attack.
            int approaches = 0;
            for (int step = 0; step < 200; step++)
            {
                SkirmisherPhase phase = _rules.Advance(1.0f, Dt, AttackRange, RetreatRange, false);
                if (phase == SkirmisherPhase.Attack) _rules.OnStrike(RetreatSeconds);
                if (phase == SkirmisherPhase.Approach) approaches++;
            }
            Assert.Greater(approaches, 5);
        }
    }

    /// <summary>Luật bắn của quái Ranged (Phase 9.8): điều kiện vào/ra Attack có trễ, không bắn vào Safe Zone, hướng khóa không NaN.</summary>
    public class RangedAttackRulesTests
    {
        private const float Shoot = 6f;
        private const float Retreat = 2.5f;

        // ── Vào / ra Attack ──────────────────────────────────────────────────

        [Test]
        public void ShouldEngage_InsideShootRangeAndNotTooClose()
        {
            Assert.IsTrue(RangedAttackRules.ShouldEngage(4.5f, Shoot, Retreat, false));
            Assert.IsTrue(RangedAttackRules.ShouldEngage(Shoot, Shoot, Retreat, false));
            Assert.IsTrue(RangedAttackRules.ShouldEngage(Retreat, Shoot, Retreat, false));
        }

        [Test]
        public void ShouldEngage_FalseWhenTooFar()
        {
            Assert.IsFalse(RangedAttackRules.ShouldEngage(Shoot + 0.01f, Shoot, Retreat, false));
        }

        [Test]
        public void ShouldEngage_TooClose_OnlyWhenCornered()
        {
            Assert.IsFalse(RangedAttackRules.ShouldEngage(1f, Shoot, Retreat, false), "quá gần: ưu tiên lùi");
            Assert.IsTrue(RangedAttackRules.ShouldEngage(1f, Shoot, Retreat, true), "bị dồn góc: vẫn đánh, không đứng im vô tận");
        }

        [Test]
        public void ShouldDisengage_HasHysteresisOnShootRange()
        {
            Assert.IsFalse(RangedAttackRules.ShouldDisengage(Shoot * 1.05f, Shoot, Retreat, false), "vừa ra khỏi tầm bắn: chưa bỏ");
            Assert.IsTrue(RangedAttackRules.ShouldDisengage(Shoot * RangedAttackRules.DisengageRangeFactor + 0.05f, Shoot, Retreat, false));
        }

        [Test]
        public void ShouldDisengage_WhenTooCloseAndCanRetreat_ButNotWhenCornered()
        {
            float close = Retreat * RangedAttackRules.DisengageCloseFactor - 0.1f;
            Assert.IsTrue(RangedAttackRules.ShouldDisengage(close, Shoot, Retreat, false));
            Assert.IsFalse(RangedAttackRules.ShouldDisengage(close, Shoot, Retreat, true));
        }

        [Test]
        public void EngageDisengage_DoNotFlickerAtTheShootRangeEdge()
        {
            // Đang Attack, mục tiêu dao động quanh biên tầm bắn: không được bỏ rồi vào liên tục.
            bool attacking = true;
            int toggles = 0;
            float[] distances = { 5.9f, 6.1f, 5.95f, 6.2f, 6.05f, 6.3f };
            foreach (float d in distances)
            {
                bool next = attacking
                    ? !RangedAttackRules.ShouldDisengage(d, Shoot, Retreat, false)
                    : RangedAttackRules.ShouldEngage(d, Shoot, Retreat, false);
                if (next != attacking) toggles++;
                attacking = next;
            }
            Assert.AreEqual(0, toggles);
        }

        // ── Safe Zone ────────────────────────────────────────────────────────

        [Test]
        public void CanFire_NeverIntoSafeZone_AndNeedsATarget()
        {
            Assert.IsTrue(RangedAttackRules.CanFire(true, false));
            Assert.IsFalse(RangedAttackRules.CanFire(true, true));
            Assert.IsFalse(RangedAttackRules.CanFire(false, false));
            Assert.IsFalse(RangedAttackRules.CanFire(false, true));
        }

        // ── Hướng khóa ───────────────────────────────────────────────────────

        [Test]
        public void AimDirection_IsUnitVectorTowardTargetAtFireTime()
        {
            Vector2 d = RangedAttackRules.AimDirection(Vector2.zero, new Vector2(3f, 4f), Vector2.right);
            Assert.AreEqual(1f, d.magnitude, 1e-5f);
            Assert.AreEqual(0.6f, d.x, 1e-5f);
            Assert.AreEqual(0.8f, d.y, 1e-5f);
        }

        [Test]
        public void AimDirection_IsFixedOnceComputed_TargetMovingLaterDoesNotChangeIt()
        {
            // Hướng là một giá trị Vector2 đã tính: dời mục tiêu sau đó không làm nó đổi (đó là điều làm đạn né được).
            Vector2 target = new Vector2(5f, 0f);
            Vector2 locked = RangedAttackRules.AimDirection(Vector2.zero, target, Vector2.right);
            target = new Vector2(0f, 5f);
            Assert.AreEqual(Vector2.right, locked);
        }

        [Test]
        public void AimDirection_ZeroDistance_UsesFallback_NoNaN()
        {
            Vector2 d = RangedAttackRules.AimDirection(Vector2.one, Vector2.one, new Vector2(0f, 3f));
            Assert.IsFalse(float.IsNaN(d.x) || float.IsNaN(d.y));
            Assert.AreEqual(0f, d.x, 1e-5f);
            Assert.AreEqual(1f, d.y, 1e-5f);
        }

        [Test]
        public void AimDirection_ZeroDistanceAndZeroFallback_FallsBackToRight()
        {
            Vector2 d = RangedAttackRules.AimDirection(Vector2.one, Vector2.one, Vector2.zero);
            Assert.AreEqual(Vector2.right, d);
        }
    }
}
