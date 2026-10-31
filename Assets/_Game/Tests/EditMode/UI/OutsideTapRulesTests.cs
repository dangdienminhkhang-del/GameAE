using Game.UI.Pause;
using NUnit.Framework;

namespace Game.Tests.EditMode.UI
{
    [TestFixture]
    public class OutsideTapRulesTests
    {
        [Test]
        public void TapOutsideOnEmptyArea_Closes()
        {
            Assert.IsTrue(OutsideTapRules.ShouldClose(stableTarget: true, pressedThisFrame: true, insideWindow: false, overInteractive: false));
        }

        [Test]
        public void TapInsideWindow_DoesNotClose()
        {
            Assert.IsFalse(OutsideTapRules.ShouldClose(true, true, insideWindow: true, overInteractive: false));
        }

        [Test]
        public void TapOutsideOnAnotherButton_DoesNotClose_ButtonHandlesItself()
        {
            // Ví dụ: bấm đúng nút bật/tắt panel — nếu cũng đóng ở đây thì nút sẽ mở lại ngay.
            Assert.IsFalse(OutsideTapRules.ShouldClose(true, true, insideWindow: false, overInteractive: true));
        }

        [Test]
        public void PanelJustOpenedThisFrame_DoesNotClose()
        {
            Assert.IsFalse(OutsideTapRules.ShouldClose(stableTarget: false, pressedThisFrame: true, insideWindow: false, overInteractive: false));
        }

        [Test]
        public void NoPress_DoesNotClose()
        {
            Assert.IsFalse(OutsideTapRules.ShouldClose(true, pressedThisFrame: false, insideWindow: false, overInteractive: false));
        }

        [Test]
        public void IsBackdrop_FullScreenRoot_True()
        {
            Assert.IsTrue(OutsideTapRules.IsBackdrop(2361f, 1080f, 1920f, 1080f));
            Assert.IsTrue(OutsideTapRules.IsBackdrop(1800f, 1000f, 1920f, 1080f));
        }

        [Test]
        public void IsBackdrop_WindowSizedRoot_False()
        {
            Assert.IsFalse(OutsideTapRules.IsBackdrop(860f, 680f, 1920f, 1080f));
            // Rộng đủ nhưng thấp → vẫn là cửa sổ.
            Assert.IsFalse(OutsideTapRules.IsBackdrop(1920f, 700f, 1920f, 1080f));
        }
    }
}
