namespace Game.UI.Map
{
    /// <summary>Ba trạng thái của giao diện bản đồ: chỉ icon → minimap → bản đồ lớn.</summary>
    public enum MapPanelState
    {
        /// <summary>Chỉ hiện icon bản đồ.</summary>
        Collapsed = 0,

        /// <summary>Icon + minimap nhỏ.</summary>
        Mini = 1,

        /// <summary>Bản đồ toàn màn hình.</summary>
        Full = 2
    }

    /// <summary>
    /// Luồng chuyển trạng thái (hàm thuần, test được không cần scene):
    /// bấm icon: Collapsed ⇄ Mini · bấm minimap: Mini → Full · bản đồ lớn: chạm vùng trống / phím M → về icon.
    /// Mọi đường ĐÓNG bản đồ lớn đều về icon (không dừng ở minimap) để người chơi chỉ cần một thao tác.
    /// </summary>
    public static class MapPanelFlow
    {
        public static MapPanelState OnIconPressed(MapPanelState s)
        {
            switch (s)
            {
                case MapPanelState.Collapsed: return MapPanelState.Mini;
                case MapPanelState.Mini: return MapPanelState.Collapsed;
                default: return MapPanelState.Collapsed; // đang mở bản đồ lớn: thu hết về icon
            }
        }

        public static MapPanelState OnMiniPressed(MapPanelState s)
        {
            return s == MapPanelState.Mini ? MapPanelState.Full : s;
        }

        /// <summary>Chạm vào vùng trống của bản đồ lớn (không phải Đèn Hiệu): về icon.</summary>
        public static MapPanelState OnEmptyAreaPressed(MapPanelState s)
        {
            return s == MapPanelState.Full ? MapPanelState.Collapsed : s;
        }

        /// <summary>
        /// Chạm ra ngoài (vùng trống bên ngoài minimap/bản đồ): thu về icon.
        /// Bản đồ lớn vốn đã tự thu qua nút phủ toàn màn hình; luật này chủ yếu cho minimap.
        /// </summary>
        public static MapPanelState OnOutsideTapPressed(MapPanelState s)
        {
            return s == MapPanelState.Collapsed ? s : MapPanelState.Collapsed;
        }

        /// <summary>Phím M: mở bản đồ lớn; đang mở thì đóng về icon.</summary>
        public static MapPanelState OnMapKeyPressed(MapPanelState s)
        {
            return s == MapPanelState.Full ? MapPanelState.Collapsed : MapPanelState.Full;
        }

        /// <summary>Bắt đầu dịch chuyển nhanh: dọn bản đồ khỏi màn hình trong lúc đứng kênh.</summary>
        public static MapPanelState OnTravelStarted(MapPanelState s)
        {
            return MapPanelState.Collapsed;
        }

        /// <summary>Trạng thái khi bật lại UI (đổi tầng, vào lại): nhớ Collapsed/Mini, nhưng không tự mở bản đồ lớn.</summary>
        public static MapPanelState Restore(MapPanelState remembered)
        {
            return remembered == MapPanelState.Full ? MapPanelState.Mini : remembered;
        }
    }
}
