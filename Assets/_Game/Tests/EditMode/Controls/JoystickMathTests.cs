using Game.Core.Facing;
using Game.Core.Items;
using Game.UI.Controls;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class JoystickMathTests
    {
        private const float Range = 70f;

        [Test]
        public void BelowDeadZone_IsZero()
        {
            Assert.AreEqual(Vector2.zero, JoystickMath.Apply(new Vector2(0.1f * Range, 0f), Range, 0.15f));
            Assert.AreEqual(Vector2.zero, JoystickMath.Apply(Vector2.zero, Range, 0.15f));
        }

        [Test]
        public void ContinuousAtThreshold_NoJump()
        {
            float dz = 0.2f;
            Vector2 justBelow = JoystickMath.Apply(new Vector2((dz - 0.001f) * Range, 0f), Range, dz);
            Vector2 justAbove = JoystickMath.Apply(new Vector2((dz + 0.001f) * Range, 0f), Range, dz);
            Assert.AreEqual(0f, justBelow.magnitude, 1e-6f);
            Assert.Less(justAbove.magnitude, 0.01f, "ngay trên ngưỡng đầu ra phải gần 0, không nhảy vọt");
        }

        [Test]
        public void MagnitudeNeverExceedsOne_AndReachesOneAtFullDrag()
        {
            Assert.AreEqual(1f, JoystickMath.Apply(new Vector2(Range, 0f), Range, 0.2f).magnitude, 1e-5f);
            Assert.AreEqual(1f, JoystickMath.Apply(new Vector2(500f, 500f), Range, 0.2f).magnitude, 1e-5f);
        }

        [Test]
        public void DirectionIsPreserved()
        {
            Vector2 delta = new Vector2(0.6f * Range, 0.4f * Range);
            Vector2 result = JoystickMath.Apply(delta, Range, 0.15f);
            Assert.Greater(result.magnitude, 0f);
            Assert.AreEqual(delta.normalized.x, result.normalized.x, 1e-5f);
            Assert.AreEqual(delta.normalized.y, result.normalized.y, 1e-5f);
        }

        [Test]
        public void Sensitivity_HigherReachesFullSpeedWithShorterDrag()
        {
            Vector2 delta = new Vector2(0.8f * Range, 0f);
            float normal = JoystickMath.Apply(delta, Range, 0.15f, 1f).magnitude;
            float sensitive = JoystickMath.Apply(delta, Range, 0.15f, 1.3f).magnitude;
            Assert.Greater(sensitive, normal);
        }

        [Test]
        public void InvalidInput_GivesZero()
        {
            Assert.AreEqual(Vector2.zero, JoystickMath.Apply(new Vector2(float.NaN, 1f), Range, 0.15f));
            Assert.AreEqual(Vector2.zero, JoystickMath.Apply(new Vector2(10f, 0f), 0f, 0.15f));
            Assert.AreEqual(Vector2.zero, JoystickMath.Apply(new Vector2(10f, 0f), float.NaN, 0.15f));
        }

        [Test]
        public void HugeDeadZone_IsCapped_NotDivideByZero()
        {
            Vector2 result = JoystickMath.Apply(new Vector2(Range, 0f), Range, 5f);
            Assert.IsFalse(float.IsNaN(result.x) || float.IsInfinity(result.x));
        }

        // ── Mục 3b: dead zone chỉ có MỘT nguồn (JoystickMath); FacingTracker chỉ lọc nhiễu số ──

        [Test]
        public void BelowDeadZone_FacingKeepsOldDirection()
        {
            var tracker = new FacingTracker(Direction8.East);
            Vector2 output = JoystickMath.Apply(new Vector2(0f, 0.1f * Range), Range, SettingsRulesMin());
            Assert.AreEqual(Vector2.zero, output);
            Assert.IsFalse(tracker.Update(output));
            Assert.AreEqual(Direction8.East, tracker.Direction);
        }

        [Test]
        public void JustAboveDeadZone_FacingChangesDirection()
        {
            var tracker = new FacingTracker(Direction8.East);
            float dz = SettingsRulesMin();
            // Kéo thẳng lên, vượt dead zone một chút: đầu ra nhỏ nhưng vẫn phải đổi hướng sang Bắc.
            Vector2 output = JoystickMath.Apply(new Vector2(0f, (dz + 0.05f) * Range), Range, dz);
            Assert.Greater(output.magnitude, FacingTracker.DefaultDeadZone, "đầu ra ngay trên dead zone phải lớn hơn ngưỡng lọc nhiễu của FacingTracker");
            Assert.IsTrue(tracker.Update(output));
            Assert.AreEqual(Direction8.North, tracker.Direction);
        }

        private static float SettingsRulesMin() => Game.Core.Settings.SettingsRules.MinDeadZone;
    }

    public class PotionButtonViewModelTests
    {
        [Test]
        public void Ok_IsBrightAndHasNoStatus()
        {
            var vm = PotionButtonViewModel.From(ConsumableUseResult.Ok, 3, 0f);
            Assert.IsTrue(vm.Available);
            Assert.AreEqual(1f, vm.Alpha);
            Assert.AreEqual(string.Empty, vm.StatusText);
            Assert.AreEqual(string.Empty, vm.ToastText);
            Assert.AreEqual("3", vm.CountText);
        }

        [Test]
        public void InCombat_IsDimWithCountdown()
        {
            var vm = PotionButtonViewModel.From(ConsumableUseResult.InCombat, 2, 3.2f);
            Assert.IsFalse(vm.Available);
            Assert.AreEqual(PotionButtonViewModel.DimAlpha, vm.Alpha);
            Assert.AreEqual("Đang giao chiến 4s", vm.StatusText);
            Assert.AreEqual(ConsumableRules.DescribeResult(ConsumableUseResult.InCombat), vm.ToastText);
        }

        [Test]
        public void OnCooldown_IsDimWithCountdown()
        {
            var vm = PotionButtonViewModel.From(ConsumableUseResult.OnCooldown, 2, 1.0f);
            Assert.IsFalse(vm.Available);
            Assert.AreEqual("Đang chờ 1s", vm.StatusText);
        }

        [Test]
        public void EveryResult_HasConsistentViewModel()
        {
            foreach (ConsumableUseResult result in System.Enum.GetValues(typeof(ConsumableUseResult)))
            {
                var vm = PotionButtonViewModel.From(result, 1, 0f);
                Assert.AreEqual(result == ConsumableUseResult.Ok, vm.Available, result.ToString());
                if (result != ConsumableUseResult.Ok)
                {
                    Assert.AreEqual(ConsumableRules.DescribeResult(result), vm.ToastText, result.ToString());
                    Assert.IsNotEmpty(vm.StatusText, result.ToString());
                    Assert.AreEqual(PotionButtonViewModel.DimAlpha, vm.Alpha, result.ToString());
                }
            }
        }

        [Test]
        public void NegativeQuantity_ShowsZero()
        {
            Assert.AreEqual("0", PotionButtonViewModel.From(ConsumableUseResult.OutOfStock, -5, 0f).CountText);
        }
    }
}
