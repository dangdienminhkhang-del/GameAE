using Game.UI.Map;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class MapOutsideTapTests
    {
        [Test]
        public void OutsideTap_WhenMini_CollapsesToIcon()
        {
            Assert.AreEqual(MapPanelState.Collapsed, MapPanelFlow.OnOutsideTapPressed(MapPanelState.Mini));
        }

        [Test]
        public void OutsideTap_WhenCollapsed_StaysCollapsed()
        {
            Assert.AreEqual(MapPanelState.Collapsed, MapPanelFlow.OnOutsideTapPressed(MapPanelState.Collapsed));
        }

        [Test]
        public void OutsideTap_WhenFull_CollapsesToIcon()
        {
            Assert.AreEqual(MapPanelState.Collapsed, MapPanelFlow.OnOutsideTapPressed(MapPanelState.Full));
        }

        [Test]
        public void ExistingEmptyAreaRule_IsUnchanged_MiniStaysMini()
        {
            // Luật cũ (nút phủ toàn màn hình của bản đồ lớn) không bị ảnh hưởng.
            Assert.AreEqual(MapPanelState.Mini, MapPanelFlow.OnEmptyAreaPressed(MapPanelState.Mini));
        }
    }
}
