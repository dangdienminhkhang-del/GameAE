using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Combat
{
    /// <summary>
    /// Kết quả của một nhịp DoT khi hiệu ứng trạng thái gây sát thương.
    /// </summary>
    public readonly struct StatusEffectTickResult
    {
        public readonly StatusEffectType Type;
        public readonly double Damage;
        public readonly string SourceId;
        public readonly object Source;

        public StatusEffectTickResult(StatusEffectType type, double damage, string sourceId, object source)
        {
            Type = type;
            Damage = damage;
            SourceId = sourceId;
            Source = source;
        }
    }

    /// <summary>
    /// Quy tắc quản lý các hiệu ứng trạng thái (Status Effects) chuẩn Sword Art Online (SAO).
    /// Pure logic — độc lập khỏi scene, 100% testable trong EditMode.
    /// </summary>
    public static class StatusEffectRules
    {
        // ── Presets mặc định chuẩn SAO ──
        public const float DefaultPoisonDuration = 8.0f;
        public const float DefaultPoisonTickInterval = 1.0f;
        public const double DefaultPoisonHpPercentPerTick = 0.02; // 2% Max HP mỗi giây

        public const float DefaultBleedDuration = 4.0f;
        public const float DefaultBleedTickInterval = 0.5f; // Giật mỗi 0.5s
        public const double DefaultBleedTickDamage = 8.0;

        public const float DefaultParalyzeDuration = 4.0f;
        public const float DefaultStunDuration = 1.5f;

        /// <summary>Hệ số kháng choáng của Boss (giảm 60% thời gian choáng).</summary>
        public const float BossStunDurationMultiplier = 0.40f;

        /// <summary>
        /// Áp dụng hoặc làm mới hiệu ứng trạng thái lên danh sách active.
        /// Trả về true nếu là hiệu ứng mới, false nếu làm mới (refresh) thời gian hiệu ứng cũ.
        /// </summary>
        public static bool ApplyEffect(
            List<StatusEffectInstance> activeList,
            StatusEffectType type,
            float duration,
            double damagePerTick = 0.0,
            float tickInterval = 1.0f,
            string sourceId = null,
            object source = null)
        {
            if (activeList == null || type == StatusEffectType.None || duration <= 0f)
                return false;

            // Kiểm tra hiệu ứng cùng loại đã tồn tại chưa
            for (int i = 0; i < activeList.Count; i++)
            {
                if (activeList[i].Type == type)
                {
                    // Làm mới thời gian: lấy max giữa thời gian còn lại và thời gian mới
                    activeList[i].RemainingDuration = Mathf.Max(activeList[i].RemainingDuration, duration);
                    activeList[i].TotalDuration = Mathf.Max(activeList[i].TotalDuration, duration);
                    if (damagePerTick > activeList[i].DamagePerTick)
                    {
                        activeList[i].DamagePerTick = damagePerTick;
                    }
                    return false; // Đã làm mới
                }
            }

            // Thêm hiệu ứng mới
            activeList.Add(new StatusEffectInstance(type, duration, damagePerTick, tickInterval, sourceId, source));
            return true;
        }

        /// <summary>
        /// Cập nhật thời gian trôi qua và sinh ra các tick DoT sát thương.
        /// </summary>
        public static void Tick(
            List<StatusEffectInstance> activeList,
            float deltaTime,
            List<StatusEffectTickResult> outTickResults)
        {
            if (activeList == null || activeList.Count == 0) return;

            for (int i = activeList.Count - 1; i >= 0; i--)
            {
                var effect = activeList[i];
                effect.RemainingDuration -= deltaTime;

                if (effect.IsDoT && effect.DamagePerTick > 0.0)
                {
                    effect.TimeUntilNextTick -= deltaTime;
                    if (effect.TimeUntilNextTick <= 0f)
                    {
                        outTickResults?.Add(new StatusEffectTickResult(
                            effect.Type,
                            effect.DamagePerTick,
                            effect.SourceId,
                            effect.Source));

                        effect.TimeUntilNextTick = effect.TickInterval;
                    }
                }

                // Xóa hiệu ứng khi hết hạn
                if (effect.RemainingDuration <= 0f)
                {
                    activeList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Kiểm tra xem thực thể có đang bị khóa hành động (do Tê liệt hoặc Choáng) hay không.
        /// </summary>
        public static bool HasActionLock(IReadOnlyList<StatusEffectInstance> activeList)
        {
            if (activeList == null) return false;
            for (int i = 0; i < activeList.Count; i++)
            {
                if (activeList[i].IsActionLocked) return true;
            }
            return false;
        }

        /// <summary>
        /// Kiểm tra xem thực thể có đang dính hiệu ứng cụ thể hay không.
        /// </summary>
        public static bool HasEffect(IReadOnlyList<StatusEffectInstance> activeList, StatusEffectType type)
        {
            if (activeList == null) return false;
            for (int i = 0; i < activeList.Count; i++)
            {
                if (activeList[i].Type == type) return true;
            }
            return false;
        }

        /// <summary>
        /// Hóa giải (Cure / Cleanse) hiệu ứng trạng thái cụ thể hoặc toàn bộ hiệu ứng nếu type == None.
        /// Trả về số lượng hiệu ứng đã bị hóa giải.
        /// </summary>
        public static int Cure(List<StatusEffectInstance> activeList, StatusEffectType type = StatusEffectType.None)
        {
            if (activeList == null || activeList.Count == 0) return 0;

            if (type == StatusEffectType.None)
            {
                int count = activeList.Count;
                activeList.Clear();
                return count;
            }

            int removed = 0;
            for (int i = activeList.Count - 1; i >= 0; i--)
            {
                if (activeList[i].Type == type)
                {
                    activeList.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }
    }
}
