using System;
using Game.PvE.Zones;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Zones
{
    /// <summary>Phase 9.7a — chọn điểm spawn tránh Safe Zone và tôn trọng vùng gán cho spawner.</summary>
    public class SpawnPointPickerTests
    {
        private static Func<Vector2> Sequence(params Vector2[] values)
        {
            int index = 0;
            return () => values[Math.Min(index++, values.Length - 1)];
        }

        [Test]
        public void NothingBlocked_ReturnsFirstPoint()
        {
            var result = SpawnPointPicker.Pick(new Vector2(10f, 0f), 2f, p => false, Sequence(new Vector2(1f, 0f)));

            Assert.IsFalse(result.UsedFallback);
            Assert.AreEqual(1, result.Attempts);
            Assert.AreEqual(new Vector2(12f, 0f), result.Position);
        }

        [Test]
        public void NullBlockedFunction_MeansNothingBlocked()
        {
            var result = SpawnPointPicker.Pick(Vector2.zero, 1f, null, Sequence(new Vector2(0f, 1f)));

            Assert.IsFalse(result.UsedFallback);
            Assert.AreEqual(new Vector2(0f, 1f), result.Position);
        }

        [Test]
        public void SkipsBlockedPoints_UntilAValidOne()
        {
            // Hai điểm đầu rơi vào vùng bị chặn (x < 0), điểm thứ ba hợp lệ.
            var points = Sequence(new Vector2(-1f, 0f), new Vector2(-0.5f, 0f), new Vector2(1f, 0f));

            var result = SpawnPointPicker.Pick(Vector2.zero, 4f, p => p.x < 0f, points);

            Assert.IsFalse(result.UsedFallback);
            Assert.AreEqual(3, result.Attempts);
            Assert.AreEqual(new Vector2(4f, 0f), result.Position);
        }

        [Test]
        public void AllAttemptsBlocked_FallsBackToCenter()
        {
            var result = SpawnPointPicker.Pick(new Vector2(5f, 5f), 3f, p => true, Sequence(Vector2.one), 6);

            Assert.IsTrue(result.UsedFallback);
            Assert.AreEqual(6, result.Attempts);
            Assert.AreEqual(new Vector2(5f, 5f), result.Position);
        }

        [Test]
        public void ZeroOrNegativeAttempts_StillTriesOnce()
        {
            int calls = 0;
            var result = SpawnPointPicker.Pick(Vector2.zero, 1f, p => { calls++; return true; }, Sequence(Vector2.one), 0);

            Assert.AreEqual(1, calls);
            Assert.IsTrue(result.UsedFallback);
        }

        [Test]
        public void NullRandomSource_FallsBackToCenterWithoutThrowing()
        {
            var result = SpawnPointPicker.Pick(new Vector2(2f, 3f), 1f, p => false, null);

            Assert.IsTrue(result.UsedFallback);
            Assert.AreEqual(new Vector2(2f, 3f), result.Position);
        }

        [Test]
        public void NaNOrNegativeRadius_TreatedAsZero()
        {
            var nan = SpawnPointPicker.Pick(new Vector2(1f, 1f), float.NaN, null, Sequence(new Vector2(1f, 0f)));
            var negative = SpawnPointPicker.Pick(new Vector2(1f, 1f), -5f, null, Sequence(new Vector2(1f, 0f)));

            Assert.AreEqual(new Vector2(1f, 1f), nan.Position);
            Assert.AreEqual(new Vector2(1f, 1f), negative.Position);
        }

        [Test]
        public void NeverReturnsPointInsideSafeZone_WhenValidPointsExist()
        {
            // Tâm spawn nằm sát Safe Zone (hình tròn bán kính 3 tại gốc); vòng lấy mẫu chồng một phần lên Safe Zone.
            Vector2 safeCenter = Vector2.zero;
            const float safeRadius = 3f;
            Func<Vector2, bool> inSafeZone = p => (p - safeCenter).sqrMagnitude <= safeRadius * safeRadius;

            var rng = new System.Random(12345);
            Func<Vector2> unitCircle = () =>
            {
                double angle = rng.NextDouble() * Math.PI * 2.0;
                double r = Math.Sqrt(rng.NextDouble());
                return new Vector2((float)(Math.Cos(angle) * r), (float)(Math.Sin(angle) * r));
            };

            int realPicks = 0;
            for (int i = 0; i < 500; i++)
            {
                var result = SpawnPointPicker.Pick(new Vector2(5f, 0f), 4f, inSafeZone, unitCircle, 24);
                if (result.UsedFallback) continue;
                realPicks++;
                Assert.IsFalse(inSafeZone(result.Position), $"Điểm {result.Position} nằm trong Safe Zone.");
            }

            Assert.Greater(realPicks, 450, "Phần lớn lần chọn phải ra điểm hợp lệ (fallback chỉ là phương án cuối).");
        }

        [Test]
        public void RespectsAssignedZone_PointsOutsideZoneAreBlocked()
        {
            // Vùng được gán: hình chữ nhật x 0..6, y -2..2. Vòng lấy mẫu rộng hơn vùng.
            Func<Vector2, bool> outsideZone = p => p.x < 0f || p.x > 6f || p.y < -2f || p.y > 2f;

            var rng = new System.Random(777);
            Func<Vector2> unitCircle = () =>
            {
                double angle = rng.NextDouble() * Math.PI * 2.0;
                double r = Math.Sqrt(rng.NextDouble());
                return new Vector2((float)(Math.Cos(angle) * r), (float)(Math.Sin(angle) * r));
            };

            for (int i = 0; i < 300; i++)
            {
                var result = SpawnPointPicker.Pick(new Vector2(3f, 0f), 6f, outsideZone, unitCircle, 32);
                if (result.UsedFallback)
                {
                    Assert.AreEqual(new Vector2(3f, 0f), result.Position);
                    continue;
                }
                Assert.IsFalse(outsideZone(result.Position), $"Điểm {result.Position} nằm ngoài vùng gán.");
            }
        }
    }
}
