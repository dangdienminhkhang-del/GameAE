using Game.Core.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Số sát thương nổi: làm tròn, phân loại, cache chuỗi, chống chồng chữ.</summary>
    public class DamageNumberRulesTests
    {
        [Test]
        public void ToDisplayValue_RoundsAwayFromZero()
        {
            Assert.AreEqual(13, DamageNumberRules.ToDisplayValue(12.5));
            Assert.AreEqual(12, DamageNumberRules.ToDisplayValue(12.49));
            Assert.AreEqual(100, DamageNumberRules.ToDisplayValue(100.0));
        }

        [Test]
        public void ToDisplayValue_IsAtLeastOne_ForAnyHit()
        {
            Assert.AreEqual(1, DamageNumberRules.ToDisplayValue(0.0));
            Assert.AreEqual(1, DamageNumberRules.ToDisplayValue(0.4));
            Assert.AreEqual(1, DamageNumberRules.ToDisplayValue(-5.0));
            Assert.AreEqual(1, DamageNumberRules.ToDisplayValue(double.NaN));
        }

        [Test]
        public void ToDisplayValue_HugeValue_DoesNotOverflow()
        {
            Assert.AreEqual(int.MaxValue, DamageNumberRules.ToDisplayValue(1e18));
            Assert.AreEqual(int.MaxValue, DamageNumberRules.ToDisplayValue(double.PositiveInfinity));
        }

        [Test]
        public void Classify_PicksColorGroup()
        {
            Assert.AreEqual(DamageNumberKind.Received, DamageNumberRules.Classify(true, false));
            Assert.AreEqual(DamageNumberKind.Received, DamageNumberRules.Classify(true, true), "nhận vào thì chí mạng không đổi nhóm");
            Assert.AreEqual(DamageNumberKind.DealtCritical, DamageNumberRules.Classify(false, true));
            Assert.AreEqual(DamageNumberKind.Dealt, DamageNumberRules.Classify(false, false));
        }

        [Test]
        public void GetText_IsCachedForCommonValues_AndCorrectBeyondCache()
        {
            Assert.AreSame(DamageNumberRules.GetText(37), DamageNumberRules.GetText(37), "cùng chuỗi, không cấp phát lại");
            Assert.AreEqual("0", DamageNumberRules.GetText(0));
            Assert.AreEqual("999", DamageNumberRules.GetText(999));
            Assert.AreEqual("1000", DamageNumberRules.GetText(1000));
            Assert.AreEqual("123456", DamageNumberRules.GetText(123456));
        }

        [Test]
        public void CountNear_CountsOnlyPointsWithinRadius()
        {
            var points = new[] { new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(100f, 0f), new Vector2(5f, 5f) };
            Assert.AreEqual(3, DamageNumberRules.CountNear(points, 4, Vector2.zero, 20f));
            Assert.AreEqual(2, DamageNumberRules.CountNear(points, 2, Vector2.zero, 20f), "chỉ xét 'count' phần tử đầu");
        }

        [Test]
        public void CountNear_HandlesEmptyAndNull()
        {
            Assert.AreEqual(0, DamageNumberRules.CountNear(null, 5, Vector2.zero, 20f));
            Assert.AreEqual(0, DamageNumberRules.CountNear(new Vector2[0], 0, Vector2.zero, 20f));
            Assert.AreEqual(0, DamageNumberRules.CountNear(new[] { Vector2.zero }, 5, new Vector2(500f, 0f), 20f));
        }

        [Test]
        public void StackOffset_MovesUpByIndexTimesStep()
        {
            Assert.AreEqual(Vector2.zero, DamageNumberRules.StackOffset(0, 28f));
            Assert.AreEqual(Vector2.zero, DamageNumberRules.StackOffset(-3, 28f));
            Assert.AreEqual(new Vector2(0f, 56f), DamageNumberRules.StackOffset(2, 28f));
        }
    }
}
