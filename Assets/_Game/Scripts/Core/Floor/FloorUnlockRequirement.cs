using System;

namespace Game.Core.Floor
{
    /// <summary>
    /// Loại điều kiện mở khóa tầng.
    /// </summary>
    public enum UnlockRequirementType
    {
        /// <summary>Tầng mở từ đầu, không cần điều kiện gì.</summary>
        OpenFromStart = 0,

        /// <summary>Tầng yêu cầu boss trên server đã bị đánh bại trước đó.</summary>
        WorldBossDefeated = 1
    }

    /// <summary>
    /// Điều kiện mở khóa một tầng. Plain C# struct để embed vào FloorDefinition.
    /// Core KHÔNG gọi server trực tiếp — FloorLoader đọc trạng thái qua IWorldStateService.
    /// </summary>
    [Serializable]
    public struct FloorUnlockRequirement
    {
        /// <summary>Loại điều kiện mở khóa.</summary>
        public UnlockRequirementType type;

        /// <summary>
        /// ID của boss cần bị đánh bại (chỉ dùng khi type == WorldBossDefeated).
        /// Format: "boss.floor_NN" theo contract §4.6.
        /// </summary>
        public string requiredBossId;

        /// <summary>Factory: tầng mở từ đầu.</summary>
        public static FloorUnlockRequirement FromStart() =>
            new FloorUnlockRequirement { type = UnlockRequirementType.OpenFromStart, requiredBossId = null };

        /// <summary>Factory: tầng yêu cầu boss đã bị đánh bại.</summary>
        public static FloorUnlockRequirement RequireBoss(string bossId) =>
            new FloorUnlockRequirement { type = UnlockRequirementType.WorldBossDefeated, requiredBossId = bossId };
    }
}
