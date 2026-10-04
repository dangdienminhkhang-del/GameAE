using System;

namespace Game.Core.Events
{
    public static class GameEvents
    {
        public static event EventHandler OnEnemyDefeated;
        public static void RaiseEnemyDefeated(object sender) => OnEnemyDefeated?.Invoke(sender, EventArgs.Empty);

        public static event EventHandler<int> OnExperienceGained;
        public static void RaiseExperienceGained(object sender, int exp) => OnExperienceGained?.Invoke(sender, exp);

        public static event EventHandler<int> OnLevelUp;
        public static void RaiseLevelUp(object sender, int newLevel) => OnLevelUp?.Invoke(sender, newLevel);

        /// <summary>UI chỉ hiển thị — mọi phép tính EXP/level nằm ở PlayerProgress.</summary>
        public static event EventHandler<ExperienceChangedArgs> OnExperienceChanged;
        public static void RaiseExperienceChanged(object sender, ExperienceChangedArgs args) => OnExperienceChanged?.Invoke(sender, args);

        public static event EventHandler<HealthChangedArgs> OnHealthChanged;
        public static void RaiseHealthChanged(object sender, HealthChangedArgs args) => OnHealthChanged?.Invoke(sender, args);

        public static event EventHandler OnItemAcquired;
        public static void RaiseItemAcquired(object sender) => OnItemAcquired?.Invoke(sender, EventArgs.Empty);

        /// <summary>Item vừa được thêm vào kho (itemId + số lượng thực tế đã thêm).</summary>
        public static event EventHandler<ItemAcquiredArgs> OnItemAcquiredWithPayload;
        public static void RaiseItemAcquired(object sender, ItemAcquiredArgs args) =>
            OnItemAcquiredWithPayload?.Invoke(sender, args);

        /// <summary>UI bấm "Giao nhiệm vụ" → NPC tương ứng xử lý (UI chỉ gửi ý định).</summary>
        public static event EventHandler<NpcDialogueArgs> OnQuestTurnInRequested;
        public static void RaiseQuestTurnInRequested(object sender, NpcDialogueArgs args) =>
            OnQuestTurnInRequested?.Invoke(sender, args);

        /// <summary>Chỉ số nhân vật đổi (lên level / mặc-tháo trang bị) → HUD cập nhật lại HP bar.</summary>
        public static event EventHandler OnStatsChanged;
        public static void RaiseStatsChanged(object sender) => OnStatsChanged?.Invoke(sender, EventArgs.Empty);

        /// <summary>IDeathService đã ghi nhận + LƯU cái chết → Death Screen hiển thị (docs/09 §1.4).</summary>
        public static event EventHandler<Core.Character.DeathInfo> OnPlayerDeathRecorded;
        public static void RaisePlayerDeathRecorded(object sender, Core.Character.DeathInfo info) =>
            OnPlayerDeathRecorded?.Invoke(sender, info);

        /// <summary>
        /// Không xác minh được tài khoản đã chết hay chưa (không đọc được Player Data sau nhiều lần thử)
        /// → fail-closed: chặn vào game, UI báo lỗi (docs/03 §12).
        /// </summary>
        public static event EventHandler OnDeathStateUnavailable;
        public static void RaiseDeathStateUnavailable(object sender) =>
            OnDeathStateUnavailable?.Invoke(sender, EventArgs.Empty);

        /// <summary>Giờ game thay đổi (mỗi tick) — HUD đồng hồ nghe.</summary>
        public static event EventHandler<TimeChangedArgs> OnTimeChanged;
        public static void RaiseTimeChanged(object sender, TimeChangedArgs args) => OnTimeChanged?.Invoke(sender, args);

        /// <summary>Đổi pha ngày/đêm (Sáng/Trưa/Chiều/Tối/Đêm) — ánh sáng, quái, NPC, quest nghe theo (docs/08).</summary>
        public static event EventHandler<TimeChangedArgs> OnTimePhaseChanged;
        public static void RaiseTimePhaseChanged(object sender, TimeChangedArgs args) => OnTimePhaseChanged?.Invoke(sender, args);

        public static event EventHandler<Core.Combat.DamageInfo> OnPlayerDied;
        public static void RaisePlayerDied(object sender, Core.Combat.DamageInfo lastHit) => OnPlayerDied?.Invoke(sender, lastHit);

        /// <summary>NPC muốn mở hội thoại (UI hiển thị, không chứa logic quest).</summary>
        public static event EventHandler<NpcDialogueArgs> OnNpcDialogue;
        public static void RaiseNpcDialogue(object sender, NpcDialogueArgs args) => OnNpcDialogue?.Invoke(sender, args);

        /// <summary>Trạng thái quest đổi (nhận/tiến độ/hoàn thành) — UI quest log nghe.</summary>
        public static event EventHandler<QuestChangedArgs> OnQuestChanged;
        public static void RaiseQuestChanged(object sender, QuestChangedArgs args) => OnQuestChanged?.Invoke(sender, args);

        /// <summary>UI bấm "Nhận nhiệm vụ" → NPC tương ứng xử lý (UI chỉ gửi ý định).</summary>
        public static event EventHandler<NpcDialogueArgs> OnQuestAcceptRequested;
        public static void RaiseQuestAcceptRequested(object sender, NpcDialogueArgs args) => OnQuestAcceptRequested?.Invoke(sender, args);
    }

    public class NpcDialogueArgs : EventArgs
    {
        public Core.NPC.NPCDefinition Npc;
        public string Line;
        public bool ShowAcceptButton;
        /// <summary>Hiện nút "Giao nhiệm vụ" khi quest đang ReadyToTurnIn.</summary>
        public bool ShowTurnInButton;
    }

    public class QuestChangedArgs : EventArgs
    {
        public Core.Quest.QuestDefinition Quest;
        public int Progress;
        public Core.Quest.QuestStatus Status;
        public string Message;
    }

    public class HealthChangedArgs : EventArgs
    {
        public double Current;
        public double Max;
        public double Delta;
    }

    public class ExperienceChangedArgs : EventArgs
    {
        public int Level;
        public int Current;
        public int Required;
    }

    public class TimeChangedArgs : EventArgs
    {
        /// <summary>Giờ game 0-24.</summary>
        public double GameHours;
        public int Day;
        public Core.Time.TimeOfDay Phase;
    }

    public class ItemAcquiredArgs : EventArgs
    {
        /// <summary>ID của item đã thêm vào kho (khớp với QuestDefinition.TargetId).</summary>
        public string ItemId;
        /// <summary>Số lượng thực tế đã thêm vào kho trong lần này.</summary>
        public int Quantity;
    }
}
