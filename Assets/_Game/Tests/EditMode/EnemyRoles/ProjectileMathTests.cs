using Game.PvE.Enemy.Projectile;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Toán đạn quái (Phase 9.8): bước đi, va chạm theo đoạn (không xuyên khi dt lớn), hết giờ, thời gian bay tối thiểu 0.5 s.</summary>
    public class ProjectileMathTests
    {
        // ── Bước đi ──────────────────────────────────────────────────────────

        [Test]
        public void Step_MovesStraightBySpeedTimesDelta()
        {
            Vector2 p = ProjectileMath.Step(new Vector2(1f, 2f), Vector2.right, 8f, 0.5f);
            Assert.AreEqual(5f, p.x, 1e-5f);
            Assert.AreEqual(2f, p.y, 1e-5f);
        }

        [Test]
        public void Step_NegativeDeltaOrSpeed_DoesNotMoveBackwards()
        {
            Assert.AreEqual(new Vector2(1f, 1f), ProjectileMath.Step(new Vector2(1f, 1f), Vector2.right, 8f, -1f));
            Assert.AreEqual(new Vector2(1f, 1f), ProjectileMath.Step(new Vector2(1f, 1f), Vector2.right, -8f, 1f));
        }

        [Test]
        public void Step_ZeroDelta_StaysPut()
        {
            Assert.AreEqual(Vector2.zero, ProjectileMath.Step(Vector2.zero, Vector2.up, 8f, 0f));
        }

        // ── Va chạm đoạn–tròn ────────────────────────────────────────────────

        [Test]
        public void SegmentHitsCircle_HitsWhenPathPassesThroughCircle()
        {
            Assert.IsTrue(ProjectileMath.SegmentHitsCircle(new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(1f, 0.2f), 0.5f));
        }

        [Test]
        public void SegmentHitsCircle_MissesWhenPathPassesOutside()
        {
            Assert.IsFalse(ProjectileMath.SegmentHitsCircle(new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(1f, 2f), 0.5f));
        }

        [Test]
        public void SegmentHitsCircle_DoesNotTunnelWhenStepIsHuge()
        {
            // dt lớn (lag/dừng hình rồi nhả): bước dài 20 đơn vị đi xuyên người chơi ở giữa đoạn. Kiểm đoạn vẫn bắt được.
            Vector2 start = Vector2.zero;
            Vector2 end = ProjectileMath.Step(start, Vector2.right, 10f, 2f);
            Assert.AreEqual(20f, end.x, 1e-4f);
            Assert.IsTrue(ProjectileMath.SegmentHitsCircle(start, end, new Vector2(5f, 0f), 0.4f), "chỉ kiểm điểm cuối sẽ bỏ lỡ");
            Assert.IsFalse(Vector2.Distance(end, new Vector2(5f, 0f)) <= 0.4f, "điều kiện của test: điểm cuối cách xa người chơi");
        }

        [Test]
        public void SegmentHitsCircle_DoesNotHitCircleBehindTheStart()
        {
            Assert.IsFalse(ProjectileMath.SegmentHitsCircle(new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(-3f, 0f), 0.5f));
        }

        [Test]
        public void SegmentHitsCircle_DoesNotHitCircleBeyondTheEnd()
        {
            Assert.IsFalse(ProjectileMath.SegmentHitsCircle(new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(5f, 0f), 0.5f));
        }

        [Test]
        public void SegmentHitsCircle_ZeroLengthSegment_ActsAsPointTest()
        {
            Assert.IsTrue(ProjectileMath.SegmentHitsCircle(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1.2f, 1f), 0.5f));
            Assert.IsFalse(ProjectileMath.SegmentHitsCircle(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(4f, 1f), 0.5f));
        }

        [Test]
        public void SegmentHitsCircle_NegativeRadius_IsTreatedAsZero()
        {
            Assert.IsFalse(ProjectileMath.SegmentHitsCircle(Vector2.zero, new Vector2(2f, 0f), new Vector2(1f, 0.1f), -5f));
            Assert.IsTrue(ProjectileMath.SegmentHitsCircle(Vector2.zero, new Vector2(2f, 0f), new Vector2(1f, 0f), -5f), "tâm nằm đúng trên đoạn");
        }

        // ── Hết thời gian sống ───────────────────────────────────────────────

        [Test]
        public void IsExpired_AtOrAfterLifetime()
        {
            Assert.IsFalse(ProjectileMath.IsExpired(1.49f, 1.5f));
            Assert.IsTrue(ProjectileMath.IsExpired(1.5f, 1.5f));
            Assert.IsTrue(ProjectileMath.IsExpired(9f, 1.5f));
        }

        // ── Né được trên điện thoại ──────────────────────────────────────────

        [Test]
        public void MinDodgeFlightSeconds_IsHalfASecond()
        {
            Assert.AreEqual(0.5f, ProjectileMath.MinDodgeFlightSeconds, 1e-6f);
        }

        [Test]
        public void FlightSeconds_IsDistanceOverSpeed()
        {
            Assert.AreEqual(0.75f, ProjectileMath.FlightSeconds(6f, 8f), 1e-5f);
            Assert.AreEqual(0f, ProjectileMath.FlightSeconds(-3f, 8f));
            Assert.IsTrue(float.IsPositiveInfinity(ProjectileMath.FlightSeconds(6f, 0f)));
            Assert.IsTrue(float.IsPositiveInfinity(ProjectileMath.FlightSeconds(6f, -2f)));
        }

        [Test]
        public void IsDodgeable_TrueOnlyWhenFlightAtLeastMinimum()
        {
            Assert.IsTrue(ProjectileMath.IsDodgeable(6f, 8f), "0.75 s");
            Assert.IsTrue(ProjectileMath.IsDodgeable(6f, 12f), "đúng 0.5 s");
            Assert.IsFalse(ProjectileMath.IsDodgeable(6f, 13f), "0.46 s: quá nhanh để né");
            Assert.IsFalse(ProjectileMath.IsDodgeable(6f, 30f));
        }

        // ── Damage ───────────────────────────────────────────────────────────

        [Test]
        public void ScaleDamage_ScaleOne_KeepsAmount()
        {
            Assert.AreEqual(12.0, ProjectileMath.ScaleDamage(12.0, 1f));
        }

        [Test]
        public void ScaleDamage_RoundsLikeScaleStat_AndNeverDropsBelowOneForPositiveScale()
        {
            Assert.AreEqual(10.0, ProjectileMath.ScaleDamage(12.0, 0.8f), "9.6 → 10");
            Assert.AreEqual(1.0, ProjectileMath.ScaleDamage(1.0, 0.1f), "dương nhưng nhỏ: tối thiểu 1 để đạn không vô hình");
        }

        [Test]
        public void ScaleDamage_NonPositiveScaleOrAmount_IsZero()
        {
            Assert.AreEqual(0.0, ProjectileMath.ScaleDamage(12.0, 0f));
            Assert.AreEqual(0.0, ProjectileMath.ScaleDamage(12.0, -1f));
            Assert.AreEqual(0.0, ProjectileMath.ScaleDamage(0.0, 1f));
            Assert.AreEqual(0.0, ProjectileMath.ScaleDamage(double.NaN, 1f));
        }
    }
}
