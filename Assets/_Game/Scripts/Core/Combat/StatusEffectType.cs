namespace Game.Core.Combat
{
    /// <summary>
    /// Các trạng thái bất lợi (Debuff / Status Effects) theo chuẩn Sword Art Online (SAO).
    /// </summary>
    public enum StatusEffectType
    {
        None = 0,
        /// <summary>Độc: rút máu theo chu kỳ (DoT), kéo dài lâu.</summary>
        Poison = 1,
        /// <summary>Chảy máu: vết thương hở rút máu nhanh, thời gian ngắn.</summary>
        Bleed = 2,
        /// <summary>Tê liệt: bị giật điện / độc tê liệt (SAO Kuradeel), không thể di chuyển hoặc dùng kỹ năng.</summary>
        Paralyze = 3,
        /// <summary>Choáng: đòn chí mạng hoặc va đập mạnh làm bất tỉnh tạm thời.</summary>
        Stun = 4
    }
}
