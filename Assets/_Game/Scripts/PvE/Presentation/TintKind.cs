namespace Game.PvE.Presentation
{
    /// <summary>
    /// Loại màu trạng thái do EnemyController/BossController truyền vào SpriteTint (docs/14 §2, ADR-029).
    /// Giá trị màu cụ thể vẫn do chủ sở hữu quyết (bảng màu báo đòn ở docs/14 §2); enum này chỉ cho biết MÀU ĐÓ LÀ GÌ
    /// để hệ nháy biết có nên giảm độ đậm lớp phủ trắng hay không.
    /// </summary>
    public enum TintKind
    {
        /// <summary>Màu gốc (trắng), không có trạng thái đặc biệt.</summary>
        Base = 0,

        /// <summary>Báo đòn nguy hiểm: quái windup, boss Telegraph, boss Pressure (windup và lao). Người chơi phải ĐỌC được màu này.</summary>
        DangerTelegraph = 1,

        /// <summary>Cửa sổ trừng phạt của boss: Punish, Recovery sau lao (màu xanh nhạt).</summary>
        Window = 2,

        /// <summary>Cuồng nộ (màu nền của boss khi enrage).</summary>
        Enrage = 3
    }
}
