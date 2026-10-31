using Game.UI.Map;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class MapPanelFlowTests
    {
        private const MapPanelState C = MapPanelState.Collapsed;
        private const MapPanelState M = MapPanelState.Mini;
        private const MapPanelState F = MapPanelState.Full;

        [Test]
        public void Icon_TogglesBetweenCollapsedAndMini()
        {
            Assert.AreEqual(M, MapPanelFlow.OnIconPressed(C), "Bấm icon khi chỉ có icon phải ra minimap.");
            Assert.AreEqual(C, MapPanelFlow.OnIconPressed(M), "Bấm icon khi đang có minimap phải thu về icon.");
        }

        [Test]
        public void Icon_WhileFullMapOpen_CollapsesToIcon()
        {
            Assert.AreEqual(C, MapPanelFlow.OnIconPressed(F));
        }

        [Test]
        public void Minimap_OpensFullMap_OnlyFromMini()
        {
            Assert.AreEqual(F, MapPanelFlow.OnMiniPressed(M));
            Assert.AreEqual(C, MapPanelFlow.OnMiniPressed(C), "Minimap đang ẩn thì không thể bấm vào.");
            Assert.AreEqual(F, MapPanelFlow.OnMiniPressed(F));
        }

        [Test]
        public void EmptyArea_OnFullMap_GoesStraightBackToIcon()
        {
            Assert.AreEqual(C, MapPanelFlow.OnEmptyAreaPressed(F), "Chạm vùng trống ở bản đồ lớn phải về icon, không dừng ở minimap.");
        }

        [Test]
        public void EmptyArea_WhenNotFull_ChangesNothing()
        {
            Assert.AreEqual(M, MapPanelFlow.OnEmptyAreaPressed(M));
            Assert.AreEqual(C, MapPanelFlow.OnEmptyAreaPressed(C));
        }

        [Test]
        public void MapKey_OpensFullMapFromAnywhere_AndClosesItToIcon()
        {
            Assert.AreEqual(F, MapPanelFlow.OnMapKeyPressed(C));
            Assert.AreEqual(F, MapPanelFlow.OnMapKeyPressed(M));
            Assert.AreEqual(C, MapPanelFlow.OnMapKeyPressed(F));
        }

        [Test]
        public void TravelStarted_ClearsTheMapFromScreen()
        {
            Assert.AreEqual(C, MapPanelFlow.OnTravelStarted(F));
            Assert.AreEqual(C, MapPanelFlow.OnTravelStarted(M));
        }

        [Test]
        public void Restore_NeverReopensFullMap()
        {
            Assert.AreEqual(C, MapPanelFlow.Restore(C));
            Assert.AreEqual(M, MapPanelFlow.Restore(M));
            Assert.AreEqual(M, MapPanelFlow.Restore(F), "Đổi tầng/vào lại không được tự bật bản đồ toàn màn hình.");
        }

        [Test]
        public void FullPath_IconThenMiniThenEmptyTap_ReturnsToIcon()
        {
            var s = C;
            s = MapPanelFlow.OnIconPressed(s);       Assert.AreEqual(M, s);
            s = MapPanelFlow.OnMiniPressed(s);       Assert.AreEqual(F, s);
            s = MapPanelFlow.OnEmptyAreaPressed(s);  Assert.AreEqual(C, s);
            s = MapPanelFlow.OnIconPressed(s);       Assert.AreEqual(M, s, "Sau khi về icon, bấm icon phải ra lại minimap.");
        }
    }
}
