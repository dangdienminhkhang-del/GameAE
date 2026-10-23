using System.Collections.Generic;
using Game.Core.Facing;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class FacingTrackerTests
    {
        [Test]
        public void StartsFacingEast_WithUnitVector()
        {
            var t = new FacingTracker();
            Assert.AreEqual(Direction8.East, t.Direction);
            Assert.AreEqual(new Vector2(1f, 0f), t.Vector);
        }

        [Test]
        public void InputAboveDeadZone_ChangesDirection()
        {
            var t = new FacingTracker();
            Assert.IsTrue(t.Update(new Vector2(0f, 1f)));
            Assert.AreEqual(Direction8.North, t.Direction);
            Assert.IsTrue(t.Update(new Vector2(-1f, 0f)));
            Assert.AreEqual(Direction8.West, t.Direction);
        }

        [Test]
        public void InputBelowDeadZone_IsIgnored()
        {
            var t = new FacingTracker(Direction8.East, 0.2f);
            Assert.IsFalse(t.Update(new Vector2(0f, 0.15f)));
            Assert.AreEqual(Direction8.East, t.Direction);
            Assert.IsTrue(t.Update(new Vector2(0f, 0.25f)));
            Assert.AreEqual(Direction8.North, t.Direction);
        }

        [Test]
        public void ReleasingJoystick_KeepsLastDirection()
        {
            var t = new FacingTracker();
            t.Update(new Vector2(-0.7f, -0.7f));
            Assert.AreEqual(Direction8.SouthWest, t.Direction);

            Assert.IsFalse(t.Update(Vector2.zero));
            Assert.AreEqual(Direction8.SouthWest, t.Direction);
        }

        [Test]
        public void InvalidInput_IsIgnored()
        {
            var t = new FacingTracker(Direction8.North);
            Assert.IsFalse(t.Update(new Vector2(float.NaN, 1f)));
            Assert.IsFalse(t.Update(new Vector2(float.PositiveInfinity, 0f)));
            Assert.AreEqual(Direction8.North, t.Direction);
        }

        [Test]
        public void Vector_IsAlwaysNormalized_ForAnyInputMagnitude()
        {
            var t = new FacingTracker();
            foreach (Vector2 input in new[] { new Vector2(0.3f, 0.3f), new Vector2(5f, 2f), new Vector2(-0.1f, 0.9f), new Vector2(0f, -40f) })
            {
                t.Update(input);
                Assert.AreEqual(1f, t.Vector.magnitude, 1e-4f, input.ToString());
            }
        }

        [Test]
        public void Hysteresis_JitterNearBoundaryDoesNotFlip()
        {
            var t = new FacingTracker(Direction8.East); // trễ mặc định 5°: đổi hướng khi lệch quá 27.5°
            Assert.IsFalse(t.Update(Polar(24f)), "24° lệch Đông 24° < 27.5°: giữ Đông dù gần Đông-Bắc hơn");
            Assert.AreEqual(Direction8.East, t.Direction);

            Assert.IsTrue(t.Update(Polar(30f)), "30° > 27.5°: đổi sang Đông-Bắc");
            Assert.AreEqual(Direction8.NorthEast, t.Direction);

            Assert.IsFalse(t.Update(Polar(20f)), "20° lệch Đông-Bắc 25° < 27.5°: giữ Đông-Bắc");
            Assert.AreEqual(Direction8.NorthEast, t.Direction);
        }

        [Test]
        public void Set_ChangesDirectionWithoutDeadZone_AndIgnoresInvalid()
        {
            var t = new FacingTracker(Direction8.East, 0.5f);
            Assert.IsTrue(t.Set(new Vector2(0f, -0.01f)));
            Assert.AreEqual(Direction8.South, t.Direction);
            Assert.IsFalse(t.Set(Vector2.zero));
            Assert.IsFalse(t.Set(new Vector2(float.NaN, 0f)));
            Assert.AreEqual(Direction8.South, t.Direction);
        }

        [Test]
        public void DeadZone_IsSanitized()
        {
            Assert.AreEqual(0f, new FacingTracker(Direction8.East, -1f).DeadZone);
            Assert.AreEqual(0f, new FacingTracker(Direction8.East, float.NaN).DeadZone);
            Assert.LessOrEqual(new FacingTracker(Direction8.East, 5f).DeadZone, 0.9f);
        }

        private static Vector2 Polar(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }

    public class HitboxMathTests
    {
        [Test]
        public void HorizontalFacing_MatchesLegacyFormulaExactly()
        {
            Vector2[] offsets = { new Vector2(0.9f, 0f), new Vector2(1.25f, 0.3f), new Vector2(0.6f, -0.2f), new Vector2(0f, 0.5f) };
            Vector2[] positions = { new Vector2(3.5f, -2f), Vector2.zero, new Vector2(-7.25f, 11.1f) };

            foreach (Vector2 pos in positions)
            {
                foreach (Vector2 offset in offsets)
                {
                    foreach (int facing in new[] { 1, -1 })
                    {
                        Vector2 legacy = new Vector2(pos.x + offset.x * facing, pos.y + offset.y);
                        Vector2 now = HitboxMath.Center(pos, new Vector2(facing, 0f), offset);
                        Assert.AreEqual(legacy.x, now.x, $"x pos={pos} off={offset} f={facing}");
                        Assert.AreEqual(legacy.y, now.y, $"y pos={pos} off={offset} f={facing}");
                    }
                }
            }
        }

        [Test]
        public void AllEightDirections_KeepForwardDistanceEqualToOffsetX()
        {
            Vector2 offset = new Vector2(1.2f, 0f);
            Vector2 pos = new Vector2(2f, 3f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 dir = FacingMath.ToVector((Direction8)i);
                Vector2 c = HitboxMath.Center(pos, dir, offset);
                Assert.AreEqual(offset.x, (c - pos).magnitude, 1e-4f, ((Direction8)i).ToString());
            }
        }

        [Test]
        public void NorthAndSouth_PlaceTheBoxAboveAndBelow()
        {
            Vector2 offset = new Vector2(1f, 0f);
            Vector2 up = HitboxMath.Center(Vector2.zero, FacingMath.ToVector(Direction8.North), offset);
            Vector2 down = HitboxMath.Center(Vector2.zero, FacingMath.ToVector(Direction8.South), offset);
            Assert.AreEqual(0f, up.x, 1e-5f);
            Assert.AreEqual(1f, up.y, 1e-5f);
            Assert.AreEqual(-1f, down.y, 1e-5f);
        }

        [Test]
        public void OffsetY_StaysOnWorldYAxis()
        {
            Vector2 c = HitboxMath.Center(Vector2.zero, FacingMath.ToVector(Direction8.West), new Vector2(1f, 0.4f));
            Assert.AreEqual(-1f, c.x, 1e-5f);
            Assert.AreEqual(0.4f, c.y, 1e-5f);
        }
    }

    public class AimAssistTests
    {
        private static AimCandidate C(float x, float y, bool valid = true) => new AimCandidate(new Vector2(x, y), valid);

        [Test]
        public void PicksNearestInsideRadius()
        {
            var list = new List<AimCandidate> { C(2f, 0f), C(1f, 0f), C(0f, 1.5f) };
            Assert.AreEqual(1, AimAssist.PickNearest(Vector2.zero, list, 2.5f));
        }

        [Test]
        public void NobodyInsideRadius_ReturnsMinusOne()
        {
            var list = new List<AimCandidate> { C(3f, 0f), C(0f, -4f) };
            Assert.AreEqual(-1, AimAssist.PickNearest(Vector2.zero, list, 2.5f));
        }

        [Test]
        public void ExactlyOnRadius_IsIncluded()
        {
            var list = new List<AimCandidate> { C(2.5f, 0f) };
            Assert.AreEqual(0, AimAssist.PickNearest(Vector2.zero, list, 2.5f));
        }

        [Test]
        public void SkipsInvalidAndNonFiniteCandidates()
        {
            var list = new List<AimCandidate> { C(0.5f, 0f, false), C(float.NaN, 0f), C(float.PositiveInfinity, 0f), C(2f, 0f) };
            Assert.AreEqual(3, AimAssist.PickNearest(Vector2.zero, list, 2.5f));
        }

        [Test]
        public void EqualDistance_PicksEarlierInList()
        {
            var list = new List<AimCandidate> { C(1f, 0f), C(0f, 1f), C(-1f, 0f) };
            Assert.AreEqual(0, AimAssist.PickNearest(Vector2.zero, list, 2f));
            Assert.AreEqual(0, AimAssist.PickNearest(Vector2.zero, list, 2f), "gọi lại cho cùng kết quả (ổn định)");
        }

        [Test]
        public void NullListOrBadRadius_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, AimAssist.PickNearest(Vector2.zero, null, 2f));
            var list = new List<AimCandidate> { C(0.5f, 0f) };
            Assert.AreEqual(-1, AimAssist.PickNearest(Vector2.zero, list, 0f));
            Assert.AreEqual(-1, AimAssist.PickNearest(Vector2.zero, list, -1f));
            Assert.AreEqual(-1, AimAssist.PickNearest(Vector2.zero, list, float.NaN));
        }

        [Test]
        public void TryDirection_NormalizedAndRejectsSamePoint()
        {
            Assert.IsTrue(AimAssist.TryDirection(new Vector2(1f, 1f), new Vector2(1f, 5f), out Vector2 d));
            Assert.AreEqual(new Vector2(0f, 1f), d);
            Assert.IsFalse(AimAssist.TryDirection(new Vector2(2f, 2f), new Vector2(2f, 2f), out _));
        }
    }
}
