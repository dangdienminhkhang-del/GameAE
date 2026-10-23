using Game.Core.Facing;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Toán hướng nhìn 8 hướng (docs/13 Wave 9.5a, docs/14 §4.1).</summary>
    public class FacingMathTests
    {
        private static readonly Direction8[] All =
        {
            Direction8.East, Direction8.NorthEast, Direction8.North, Direction8.NorthWest,
            Direction8.West, Direction8.SouthWest, Direction8.South, Direction8.SouthEast
        };

        [Test]
        public void FromVector_EightAxesAndDiagonals_MapToExpectedDirection()
        {
            Assert.AreEqual(Direction8.East, FacingMath.FromVector(new Vector2(1, 0), Direction8.West));
            Assert.AreEqual(Direction8.NorthEast, FacingMath.FromVector(new Vector2(1, 1), Direction8.West));
            Assert.AreEqual(Direction8.North, FacingMath.FromVector(new Vector2(0, 1), Direction8.West));
            Assert.AreEqual(Direction8.NorthWest, FacingMath.FromVector(new Vector2(-1, 1), Direction8.East));
            Assert.AreEqual(Direction8.West, FacingMath.FromVector(new Vector2(-1, 0), Direction8.East));
            Assert.AreEqual(Direction8.SouthWest, FacingMath.FromVector(new Vector2(-1, -1), Direction8.East));
            Assert.AreEqual(Direction8.South, FacingMath.FromVector(new Vector2(0, -1), Direction8.East));
            Assert.AreEqual(Direction8.SouthEast, FacingMath.FromVector(new Vector2(1, -1), Direction8.East));
        }

        [Test]
        public void FromVector_NearBoundaries_RoundsToNearestDirection()
        {
            // Biên ở 22.5°: 22° còn là Đông, 23° đã là Đông-Bắc.
            Assert.AreEqual(Direction8.East, FacingMath.FromVector(Polar(22f), Direction8.West));
            Assert.AreEqual(Direction8.NorthEast, FacingMath.FromVector(Polar(23f), Direction8.West));
            Assert.AreEqual(Direction8.NorthEast, FacingMath.FromVector(Polar(67f), Direction8.West));
            Assert.AreEqual(Direction8.North, FacingMath.FromVector(Polar(68f), Direction8.West));
            // Vòng qua 360°: 338° còn là Đông-Nam, 338° + 1° đã là Đông.
            Assert.AreEqual(Direction8.SouthEast, FacingMath.FromVector(Polar(337f), Direction8.West));
            Assert.AreEqual(Direction8.East, FacingMath.FromVector(Polar(338f), Direction8.West));
            Assert.AreEqual(Direction8.East, FacingMath.FromVector(Polar(359.9f), Direction8.West));
        }

        [Test]
        public void FromVector_ZeroOrInvalid_ReturnsFallback()
        {
            Assert.AreEqual(Direction8.South, FacingMath.FromVector(Vector2.zero, Direction8.South));
            Assert.AreEqual(Direction8.South, FacingMath.FromVector(new Vector2(float.NaN, 1f), Direction8.South));
            Assert.AreEqual(Direction8.South, FacingMath.FromVector(new Vector2(float.PositiveInfinity, 0f), Direction8.South));
            Assert.AreEqual(Direction8.South, FacingMath.FromVector(new Vector2(1e-9f, 0f), Direction8.South));
        }

        [Test]
        public void ToVector_IsUnitLength_AndRoundTripsThroughFromVector()
        {
            foreach (Direction8 d in All)
            {
                Vector2 v = FacingMath.ToVector(d);
                Assert.AreEqual(1f, v.magnitude, 1e-4f, d.ToString());
                Assert.AreEqual(d, FacingMath.FromVector(v, Direction8.East), d.ToString());
            }
        }

        [Test]
        public void ToVector_AxesAreExact()
        {
            Assert.AreEqual(new Vector2(1f, 0f), FacingMath.ToVector(Direction8.East));
            Assert.AreEqual(new Vector2(-1f, 0f), FacingMath.ToVector(Direction8.West));
            Assert.AreEqual(new Vector2(0f, 1f), FacingMath.ToVector(Direction8.North));
            Assert.AreEqual(new Vector2(0f, -1f), FacingMath.ToVector(Direction8.South));
        }

        [Test]
        public void ToArtSlot_EightDirections_MapToFiveSlotsWithFlip()
        {
            AssertSlot(Direction8.South, ArtSlot.South, false);
            AssertSlot(Direction8.SouthEast, ArtSlot.SouthEast, false);
            AssertSlot(Direction8.East, ArtSlot.East, false);
            AssertSlot(Direction8.NorthEast, ArtSlot.NorthEast, false);
            AssertSlot(Direction8.North, ArtSlot.North, false);
            AssertSlot(Direction8.SouthWest, ArtSlot.SouthEast, true);
            AssertSlot(Direction8.West, ArtSlot.East, true);
            AssertSlot(Direction8.NorthWest, ArtSlot.NorthEast, true);
        }

        [Test]
        public void ToAnimatorParams_FaceXIsAbsolute_FaceYKeepsSign()
        {
            FacingMath.ToAnimatorParams(Direction8.West, out float x, out float y);
            Assert.AreEqual(1f, x, 1e-5f);
            Assert.AreEqual(0f, y, 1e-5f);

            FacingMath.ToAnimatorParams(Direction8.SouthWest, out x, out y);
            Assert.AreEqual(0.7071f, x, 1e-3f);
            Assert.AreEqual(-0.7071f, y, 1e-3f);

            FacingMath.ToAnimatorParams(Direction8.North, out x, out y);
            Assert.AreEqual(0f, x, 1e-5f);
            Assert.AreEqual(1f, y, 1e-5f);

            FacingMath.ToAnimatorParams(Direction8.South, out x, out y);
            Assert.AreEqual(0f, x, 1e-5f);
            Assert.AreEqual(-1f, y, 1e-5f);
        }

        [Test]
        public void AngularDistance_WrapsAround()
        {
            Assert.AreEqual(45f, FacingMath.AngularDistance(350f, 35f), 1e-3f);
            Assert.AreEqual(180f, FacingMath.AngularDistance(0f, 180f), 1e-3f);
            Assert.AreEqual(0f, FacingMath.AngularDistance(90f, 90f), 1e-3f);
        }

        private static Vector2 Polar(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private static void AssertSlot(Direction8 d, ArtSlot slot, bool flip)
        {
            ArtFacing a = FacingMath.ToArtSlot(d);
            Assert.AreEqual(slot, a.Slot, d.ToString());
            Assert.AreEqual(flip, a.FlipX, d.ToString());
        }
    }
}
