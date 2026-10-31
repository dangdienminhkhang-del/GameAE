using System;

namespace Game.Core.Events
{
    public static class GameEvents
    {
        public static event EventHandler OnEnemyDefeated;
        public static void RaiseEnemyDefeated(object sender) => OnEnemyDefeated?.Invoke(sender, EventArgs.Empty);

        /// <summary>Boss bị đánh bại (Wave 3 — BossDefeatedArgs mang bossId và floorId).</summary>
        public static event EventHandler<BossDefeatedArgs> OnBossDefeated;
        public static void RaiseBossDefeated(object sender, BossDefeatedArgs args) => OnBossDefeated?.Invoke(sender, args);

        public static event EventHandler<int> OnExperienceGained;
        public static void RaiseExperienceGained(object sender, int exp) => OnExperienceGained?.Invoke(sender, exp);

        public static event EventHandler<int> OnLevelUp;
        public static void RaiseLevelUp(object sender, int newLevel) => OnLevelUp?.Invoke(sender, newLevel);

        /// <summary>UI chỉ hiển thị — mọi phép tính EXP/level nằm ở PlayerProgress.</summary>
        public static event EventHandler<ExperienceChangedArgs> OnExperienceChanged;
        public static void RaiseExperienceChanged(object sender, ExperienceChangedArgs args) => OnExperienceChanged?.Invoke(sender, args);

        public static event EventHandler<HealthChangedArgs> OnHealthChanged;
        public static void RaiseHealthChanged(object sender, HealthChangedArgs args) => OnHealthChanged?.Invoke(sender, args);

        /// <summary>
        /// HitResolved (docs/12 §11): một hit của NGƯỜI CHƠI đã thật sự áp damage lên mục tiêu còn sống.
        /// Không phát khi mục tiêu đã chết hoặc từ chối damage (Safe Zone...); quái đánh người chơi dùng OnHealthChanged.
        /// Chỉ AttackExecutor.ApplyHit được phát sự kiện này.
        /// </summary>
        public static event EventHandler<HitResolvedArgs> OnHitResolved;
        public static void RaiseHitResolved(object sender, HitResolvedArgs args) => OnHitResolved?.Invoke(sender, args);

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

        /// <summary>Thông báo combat nổi giữa màn hình (UI lắng nghe sự kiện này).</summary>
        public static event Action<string, UnityEngine.Color> OnCombatNotice;
        public static void RaiseCombatNotice(string text, UnityEngine.Color color) => OnCombatNotice?.Invoke(text, color);

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

        /// <summary>
        /// Người chơi đã chết nhưng nguồn chính CHƯA xác nhận đã ghi (docs/09 §1.3). Vẫn chặn chơi;
        /// Death Screen hiện "CHƯA LƯU ĐƯỢC CÁI CHẾT" với Thử lại / Thoát. KHÔNG phải PlayerDeathRecorded.
        /// </summary>
        public static event EventHandler<Core.Character.DeathInfo> OnDeathRecordUnconfirmed;
        public static void RaiseDeathRecordUnconfirmed(object sender, Core.Character.DeathInfo info) =>
            OnDeathRecordUnconfirmed?.Invoke(sender, info);

        /// <summary>UI bấm "Thử lại" → Bootstrap gọi IDeathService.RetryRecordAsync (UI chỉ gửi ý định).</summary>
        public static event EventHandler OnDeathRetryRequested;
        public static void RaiseDeathRetryRequested(object sender) =>
            OnDeathRetryRequested?.Invoke(sender, EventArgs.Empty);

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

        /// <summary>
        /// ZoneEntered (docs/12 §11, Phase 9.7a): người chơi vừa vào một vùng của tầng (Thị trấn, Vùng Farm...).
        /// Banner, nhạc, camera và quest Reach nghe sự kiện này. Chỉ ZoneTracker phát.
        /// </summary>
        public static event EventHandler<ZoneEnteredArgs> OnZoneEntered;
        public static void RaiseZoneEntered(object sender, ZoneEnteredArgs args) => OnZoneEntered?.Invoke(sender, args);

        /// <summary>Yêu cầu mở giao diện Quán trọ (Inn) khi người chơi tương tác (UI lắng nghe).</summary>
        public static event Action<InnRequestArgs> OnInnRequested;
        public static void RaiseInnRequested(InnRequestArgs args) => OnInnRequested?.Invoke(args);

        /// <summary>Yêu cầu mở giao diện Nấu Ăn (Cooking) khi tương tác (UI lắng nghe).</summary>
        public static event Action OnCookingRequested;
        public static void RaiseCookingRequested() => OnCookingRequested?.Invoke();

        /// <summary>Yêu cầu mở bảng chọn chế độ Tỉ Thí Duel (UI nghe).</summary>
        public static event Action OnDuelRequested;
        public static void RaiseDuelRequested() => OnDuelRequested?.Invoke();

        /// <summary>UI bấm bắt đầu Duel với chế độ đã chọn (Arena nghe).</summary>
        public static event Action<Game.Core.PvP.DuelMode> OnDuelStartRequested;
        public static void RaiseDuelStartRequested(Game.Core.PvP.DuelMode mode) => OnDuelStartRequested?.Invoke(mode);

        /// <summary>Yêu cầu mở bảng Tổ Đội / Bang Hội / Bạn Bè (Social Panel - UI nghe).</summary>
        public static event Action OnSocialPanelRequested;
        public static void RaiseSocialPanelRequested() => OnSocialPanelRequested?.Invoke();
    }

    public class ZoneEnteredArgs : EventArgs
    {
        /// <summary>ID ổn định của vùng (ví dụ zone.floor_NN_town); khớp QuestDefinition.TargetId của objective Reach.</summary>
        public string ZoneId;
        /// <summary>Tên hiển thị cho banner. Phần mở rộng so với docs/12 §11 (chỉ có ZoneId).</summary>
        public string DisplayName;
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

    public class HitResolvedArgs : EventArgs
    {
        /// <summary>Bên đánh (DamageInfo.Source — chỉ số của người chơi). Dùng để biết hit có phải của mình không.</summary>
        public object Source;
        public Core.Combat.IDamageable Target;
        public Core.Combat.DamageInfo Damage;
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

    public class BossDefeatedArgs : EventArgs
    {
        public string BossId;
        public string FloorId;
        public bool IsLastAttack;
        public string LastAttackItemId;
        public string LastAttackItemName;
    }

    public class InnRequestArgs : EventArgs
    {
        public string InnName;
        public int RestCost;
        public Func<Game.Core.Inn.InnRestResult> OnExecuteRest;
        public UnityEngine.Vector3 Position;
    }
}
