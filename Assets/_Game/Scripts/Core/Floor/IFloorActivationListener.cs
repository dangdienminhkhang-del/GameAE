namespace Game.Core.Floor
{
    /// <summary>
    /// Thành phần nằm trong nội dung một tầng muốn biết khi tầng đó được bật/tắt
    /// (ví dụ: spawner hồi sinh quái, boss reset trận đấu).
    /// Core không biết ai implement — PvE implement, FloorSceneBinder thông báo.
    /// </summary>
    public interface IFloorActivationListener
    {
        /// <summary>Tầng vừa được bật (người chơi vừa vào tầng này).</summary>
        void OnFloorActivated(FloorDefinition def);

        /// <summary>Tầng sắp bị tắt (người chơi sang tầng khác).</summary>
        void OnFloorDeactivated();
    }
}
