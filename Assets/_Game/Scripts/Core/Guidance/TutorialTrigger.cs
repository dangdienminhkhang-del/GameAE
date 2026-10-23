namespace Game.Core.Guidance
{
    /// <summary>
    /// Tín hiệu CHUNG làm một gợi ý hướng dẫn được xếp hàng (Phase 9.9). Chỉ mô tả "điều gì vừa xảy ra" ở mức cơ chế,
    /// KHÔNG chứa tên content cụ thể (tầng, quái, vật phẩm). Giá trị số được serialize trong asset: KHÔNG đổi thứ tự hay số.
    /// </summary>
    public enum TutorialTrigger
    {
        None = 0,
        /// <summary>Người chơi đã sẵn sàng (có nhân vật và playerId).</summary>
        PlayerReady = 1,
        /// <summary>Lần đầu người chơi RA KHỎI Safe Zone (bắt cạnh).</summary>
        LeftSafeZone = 2,
        /// <summary>Một quái đang nhắm người chơi bắt đầu báo đòn.</summary>
        EnemyWindup = 3,
        /// <summary>Một kỹ thuật vũ khí vừa được mở khóa (đủ mastery, có vũ khí).</summary>
        TechniqueUnlocked = 4,
        /// <summary>Máu người chơi xuống dưới ngưỡng thấp (có trễ để không bắn lặp).</summary>
        LowHealth = 5,
        /// <summary>Lần đầu quay VÀO Safe Zone sau khi đã ra ngoài (bắt cạnh).</summary>
        EnteredSafeZone = 6,
        /// <summary>Có vật phẩm vừa được thêm vào kho.</summary>
        ItemAcquired = 7,
        /// <summary>Có vật phẩm mặc được vừa được thêm vào kho.</summary>
        EquippableAcquired = 8,
        /// <summary>Boss vừa bị hạ, máy chủ đã xác nhận và bảng báo boss đã đóng (tầng kế đã mở).</summary>
        FloorUnlocked = 9
    }

    /// <summary>
    /// Việc người chơi làm xong thì gợi ý tự ẩn sớm. Giá trị số được serialize trong asset: KHÔNG đổi thứ tự hay số.
    /// </summary>
    public enum TutorialComplete
    {
        None = 0,
        /// <summary>Di chuyển liên tục một lúc.</summary>
        Moved = 1,
        /// <summary>Đòn của người chơi đã trúng mục tiêu.</summary>
        HitLanded = 2,
        /// <summary>Người chơi đã né (Dodge).</summary>
        Dodged = 3,
        /// <summary>Người chơi đã dùng kỹ thuật vũ khí.</summary>
        TechniqueUsed = 4,
        /// <summary>Người chơi đã dùng vật phẩm tiêu hao (bình).</summary>
        ConsumableUsed = 5,
        /// <summary>Người chơi đã mặc một trang bị.</summary>
        Equipped = 6
    }
}
