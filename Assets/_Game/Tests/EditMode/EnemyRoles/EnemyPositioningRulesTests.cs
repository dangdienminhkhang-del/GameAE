using Game.PvE.Enemy.Behaviors;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Luật giữ khoảng cách của quái Ranged (Phase 9.8): lùi khi quá gần, giữ trong dải, áp sát khi quá xa, không NaN.</summary>
    public class EnemyPositioningRulesTests
    {
        private const float Retreat = 2.5f;
        private const float Preferred = 4.5f;

        private static bool Never(Vector2 p) => false;

        private static void AssertNotNaN(Vector2 v)
        {
            Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y), "vector có NaN: " + v);
        }

        // ── Phân dải ──────────────────────────────────────────────────────────

        [Test]
        public void Classify_SplitsIntoTooCloseInBandTooFar()
        {
            Assert.AreEqual(EnemyPositioningRules.RangeBand.TooClose, EnemyPositioningRules.Classify(1f, Retreat, Preferred));
            Assert.AreEqual(EnemyPositioningRules.RangeBand.InBand, EnemyPositioningRules.Classify(3.5f, Retreat, Preferred));
            Assert.AreEqual(EnemyPositioningRules.RangeBand.TooFar, EnemyPositioningRules.Classify(5.5f, Retreat, Preferred));
        }

        [Test]
        public void Classify_BoundariesBelongToTheBand()
        {
            Assert.AreEqual(EnemyPositioningRules.RangeBand.InBand, EnemyPositioningRules.Classify(Retreat, Retreat, Preferred));
            Assert.AreEqual(EnemyPositioningRules.RangeBand.InBand, EnemyPositioningRules.Classify(Preferred, Retreat, Preferred));
        }

        // ── Hướng di chuyển ──────────────────────────────────────────────────

        [Test]
        public void RangedMove_TooClose_RetreatsAwayFromTarget()
        {
            Vector2 move = EnemyPositioningRules.RangedMove(new Vector2(1f, 0f), Vector2.zero, Retreat, Preferred, Never, false);
            Assert.AreEqual(1f, move.x, 1e-4f);
            Assert.AreEqual(0f, move.y, 1e-4f);
        }

        [Test]
        public void RangedMove_InBand_StandsStill()
        {
            Vector2 move = EnemyPositioningRules.RangedMove(new Vector2(3.5f, 0f), Vector2.zero, Retreat, Preferred, Never, false);
            Assert.AreEqual(Vector2.zero, move);
        }

        [Test]
        public void RangedMove_TooFar_ClosesInOnTarget()
        {
            Vector2 move = EnemyPositioningRules.RangedMove(new Vector2(8f, 0f), Vector2.zero, Retreat, Preferred, Never, false);
            Assert.AreEqual(-1f, move.x, 1e-4f);
            Assert.AreEqual(0f, move.y, 1e-4f);
        }

        [Test]
        public void RangedMove_ZeroDistance_IsNotNaN_AndStillRetreats()
        {
            Vector2 move = EnemyPositioningRules.RangedMove(Vector2.zero, Vector2.zero, Retreat, Preferred, Never, false);
            AssertNotNaN(move);
            Assert.AreEqual(1f, move.magnitude, 1e-4f, "trùng vị trí vẫn có hướng lùi xác định");
        }

        [Test]
        public void RangedMove_Cornered_DoesNotRetreat()
        {
            Vector2 move = EnemyPositioningRules.RangedMove(new Vector2(1f, 0f), Vector2.zero, Retreat, Preferred, Never, true);
            Assert.AreEqual(Vector2.zero, move, "bị dồn góc thì đứng lại (để vẫn đánh được), không đẩy vào tường");
        }

        [Test]
        public void RangedMove_TooFar_IgnoresCornerFlag()
        {
            Vector2 move = EnemyPositioningRules.RangedMove(new Vector2(8f, 0f), Vector2.zero, Retreat, Preferred, Never, true);
            Assert.AreEqual(-1f, move.x, 1e-4f);
        }

        // ── Hướng lùi không chui vào vùng chặn ───────────────────────────────

        [Test]
        public void RetreatDirection_StraightAwayWhenFree()
        {
            Vector2 d = EnemyPositioningRules.RetreatDirection(new Vector2(1f, 0f), Vector2.zero, Never);
            Assert.AreEqual(1f, d.x, 1e-4f);
            Assert.AreEqual(0f, d.y, 1e-4f);
        }

        [Test]
        public void RetreatDirection_NullBlocker_StillReturnsAway()
        {
            Vector2 d = EnemyPositioningRules.RetreatDirection(new Vector2(1f, 0f), Vector2.zero, null);
            Assert.AreEqual(1f, d.x, 1e-4f);
        }

        [Test]
        public void RetreatDirection_WallBehind_SlidesSideways()
        {
            // Tường ở bên phải (x > 1.2): hướng thẳng và hai hướng chéo 45° đều bị chặn, còn lại hướng 90°.
            Vector2 d = EnemyPositioningRules.RetreatDirection(new Vector2(1f, 0f), Vector2.zero, p => p.x > 1.2f);
            AssertNotNaN(d);
            Assert.AreEqual(0f, d.x, 1e-3f);
            Assert.AreEqual(1f, Mathf.Abs(d.y), 1e-3f);
        }

        [Test]
        public void RetreatDirection_NeverEntersTheBlockedArea()
        {
            // Safe Zone phía sau lưng: bất kể hướng nào trả về, điểm nhìn trước không được nằm trong vùng chặn.
            System.Func<Vector2, bool> safeZone = p => p.x > 1.2f;
            Vector2 self = new Vector2(1f, 0f);
            Vector2 d = EnemyPositioningRules.RetreatDirection(self, Vector2.zero, safeZone);
            if (d != Vector2.zero) Assert.IsFalse(safeZone(self + d * EnemyPositioningRules.RetreatLookAhead));
        }

        [Test]
        public void RetreatDirection_EveryDirectionBlocked_ReturnsZero()
        {
            Vector2 d = EnemyPositioningRules.RetreatDirection(new Vector2(1f, 0f), Vector2.zero, p => true);
            Assert.AreEqual(Vector2.zero, d, "bị dồn góc: không có hướng lùi nào");
        }

        [Test]
        public void RetreatDirection_SidestepWhenStuck_PrefersTheSide()
        {
            Vector2 d = EnemyPositioningRules.RetreatDirection(new Vector2(1f, 0f), Vector2.zero, Never, EnemyPositioningRules.RetreatLookAhead, 1);
            Assert.AreEqual(0f, d.x, 1e-3f);
            Assert.AreEqual(1f, d.y, 1e-3f);
        }

        // ── Away / Toward / Rotate ────────────────────────────────────────────

        [Test]
        public void AwayAndToward_CoincidentPoints_AreDefinedAndFinite()
        {
            Vector2 away = EnemyPositioningRules.Away(Vector2.one, Vector2.one);
            Vector2 toward = EnemyPositioningRules.Toward(Vector2.one, Vector2.one);
            AssertNotNaN(away);
            AssertNotNaN(toward);
            Assert.AreEqual(Vector2.right, away);
            Assert.AreEqual(Vector2.zero, toward, "khớp code cũ: trùng điểm thì không di chuyển");
        }

        [Test]
        public void Rotate_NinetyDegrees()
        {
            Vector2 r = EnemyPositioningRules.Rotate(Vector2.right, 90f);
            Assert.AreEqual(0f, r.x, 1e-5f);
            Assert.AreEqual(1f, r.y, 1e-5f);
        }

        // ── StuckDetector ─────────────────────────────────────────────────────

        [Test]
        public void StuckDetector_NotMovingWhileTrying_BecomesStuck()
        {
            var detector = new StuckDetector();
            detector.Tick(0.016f, Vector2.zero, true);
            Assert.IsFalse(detector.IsStuck);
            detector.Tick(0.5f, Vector2.zero, true);
            Assert.IsTrue(detector.IsStuck);
        }

        [Test]
        public void StuckDetector_MovingWhileTrying_IsNotStuck()
        {
            var detector = new StuckDetector();
            detector.Tick(0.016f, Vector2.zero, true);
            detector.Tick(0.5f, new Vector2(1f, 0f), true);
            Assert.IsFalse(detector.IsStuck);
        }

        [Test]
        public void StuckDetector_FlagExpiresAfterHoldSeconds()
        {
            var detector = new StuckDetector();
            detector.ForceStuck();
            Assert.IsTrue(detector.IsStuck);
            detector.Tick(StuckDetector.DefaultHoldSeconds + 0.1f, Vector2.zero, false);
            Assert.IsFalse(detector.IsStuck, "cờ kẹt hết hạn, quái không đứng im vô tận");
        }

        [Test]
        public void StuckDetector_Reset_ClearsEverything()
        {
            var detector = new StuckDetector();
            detector.ForceStuck();
            detector.Reset();
            Assert.IsFalse(detector.IsStuck);
        }

        [Test]
        public void StuckDetector_NegativeDelta_IsTreatedAsZero()
        {
            var detector = new StuckDetector();
            detector.ForceStuck();
            detector.Tick(-5f, Vector2.zero, false);
            Assert.IsTrue(detector.IsStuck);
        }
    }
}
