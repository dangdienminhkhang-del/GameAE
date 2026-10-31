using System;

namespace Game.Core.Combat
{
    /// <summary>
    /// Thể hiện một hiệu ứng trạng thái đang tác động lên thực thể.
    /// Pure C# class — dễ test trong EditMode.
    /// </summary>
    [Serializable]
    public class StatusEffectInstance
    {
        public StatusEffectType Type { get; set; }
        public float RemainingDuration { get; set; }
        public float TotalDuration { get; set; }
        public float TickInterval { get; set; }
        public float TimeUntilNextTick { get; set; }
        public double DamagePerTick { get; set; }
        public string SourceId { get; set; }
        public object Source { get; set; }

        public bool IsActionLocked => Type == StatusEffectType.Paralyze || Type == StatusEffectType.Stun;
        public bool IsDoT => Type == StatusEffectType.Poison || Type == StatusEffectType.Bleed;

        public StatusEffectInstance(
            StatusEffectType type,
            float duration,
            double damagePerTick = 0.0,
            float tickInterval = 1.0f,
            string sourceId = null,
            object source = null)
        {
            Type = type;
            TotalDuration = duration;
            RemainingDuration = duration;
            DamagePerTick = damagePerTick;
            TickInterval = tickInterval > 0f ? tickInterval : 1.0f;
            TimeUntilNextTick = TickInterval;
            SourceId = sourceId;
            Source = source;
        }
    }
}
