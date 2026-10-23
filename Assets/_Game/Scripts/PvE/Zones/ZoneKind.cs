namespace Game.PvE.Zones
{
    /// <summary>
    /// Loại vùng trong một tầng (Phase 9.7). Chỉ là nhãn dữ liệu: code gameplay KHÔNG rẽ nhánh theo tầng,
    /// chỉ dùng loại vùng khi cần (ví dụ P9.7b: BossRoom kích hoạt boss).
    /// Giá trị số được serialize trong scene nên KHÔNG đổi thứ tự hay số; chỉ được THÊM ở cuối.
    /// </summary>
    public enum ZoneKind
    {
        Town = 0,
        Farm = 1,
        BossApproach = 2,
        BossRoom = 3,

        /// <summary>Trạm nghỉ ngoài Thị trấn (Tầng 2 mở rộng): vùng an toàn nhỏ, không bị validator đòi spawner.</summary>
        Refuge = 4
    }
}
