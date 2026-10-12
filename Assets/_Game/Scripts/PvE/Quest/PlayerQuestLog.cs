using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game.Core.Events;
using Game.Core.Quest;
using Game.Core.Time;
using Game.Core.World;
using Game.PvE.Clock;
using Game.PvE.Enemy;
using Game.PvE.Items;
using Game.Services;
using UnityEngine;

namespace Game.PvE.Quest
{
    /// <summary>
    /// Sổ nhiệm vụ của người chơi.
    ///
    /// Flow Wave 1A (docs/13 §5, docs/02 §51.1, docs/03 §16.1):
    ///   Active → objective đủ → ReadyToTurnIn → NPC turn-in → Completed + reward
    ///
    /// Collect: đếm TUYỆT ĐỐI theo itemId từ event có payload (không cộng dồn mỗi lần nhặt).
    /// Kill / Talk / Reach: đối chiếu targetId; event không đúng target bị bỏ qua.
    /// Save: serial (chỉ một write tại một thời điểm) để response cũ không ghi đè state mới.
    /// </summary>
    public class PlayerQuestLog : MonoBehaviour
    {
        [SerializeField] private List<QuestDefinition> knownQuests = new List<QuestDefinition>();
        [SerializeField] private PlayerInventory playerInventory;

        private readonly QuestLog _log = new QuestLog();
        private ISaveService _save;

        // ── Serial save ────────────────────────────────────────────────────
        private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);
        private bool _dirty;
        private bool _saveWriterRunning;
        private Task _saveWriterTask;
        private ITimeService _time;
        private bool _loaded;
        private bool _saveLocked;
        // Thưởng chỉ được cấp SAU KHI lần lưu chứa trạng thái Completed thành công (chống double-reward).
        private readonly QuestRewardLedger _rewards = new QuestRewardLedger();
        private bool _destroyed;
        private readonly CancellationTokenSource _loadRetryCancellation = new CancellationTokenSource();

        public QuestLog Log => _log;
        public IReadOnlyList<QuestDefinition> KnownQuests => knownQuests;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (playerInventory == null) TryGetComponent(out playerInventory);
            _save = ServiceProvider.CreateSaveService();
            ResolveTimeService();
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyDefeated  += HandleEnemyDefeated;
            GameEvents.OnItemAcquiredWithPayload += HandleItemAcquiredWithPayload;
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnPlayerDeathRecorded += HandleDeathRecorded;
            // Fallback cho OnItemAcquired cũ (không có payload) — không dùng để advance nữa
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyDefeated  -= HandleEnemyDefeated;
            GameEvents.OnItemAcquiredWithPayload -= HandleItemAcquiredWithPayload;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnPlayerDeathRecorded -= HandleDeathRecorded;
        }

        private async void Start()
        {
            await LoadWithRetryAsync();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && CanSave()) EnqueueSave();
        }

        private void OnApplicationQuit()
        {
            if (CanSave()) EnqueueSave();
        }

        private void OnDestroy()
        {
            _destroyed = true;
            _rewards.Clear();
            _loadRetryCancellation.Cancel();
        }

        private void HandlePlayerDied(object sender, Game.Core.Combat.DamageInfo lastHit) => LockSaving();
        private void HandleDeathRecorded(object sender, Game.Core.Character.DeathInfo death) => LockSaving();

        private void LockSaving()
        {
            _saveLocked = true;
            _dirty = false;
            _rewards.Clear(); // đã chết / khóa lưu → bỏ thưởng đang chờ
            _loadRetryCancellation.Cancel();
        }

        // ── Public query ───────────────────────────────────────────────────

        public QuestState GetState(QuestDefinition quest) =>
            quest != null ? _log.Get(quest.Id) : null;

        public bool IsActive(QuestDefinition quest) =>
            quest != null && _log.IsActive(quest.Id);

        public bool IsReadyToTurnIn(QuestDefinition quest) =>
            quest != null && _log.IsReadyToTurnIn(quest.Id);

        public bool IsCompleted(QuestDefinition quest) =>
            quest != null && _log.IsCompleted(quest.Id);

        public QuestAcceptResult CheckAccept(QuestDefinition quest)
        {
            ITimeService time = ResolveTimeService();
            TimeOfDay phase = time != null ? time.CurrentPhase : TimeOfDay.Morning;
            return QuestRules.CanAccept(quest, _log, phase, time != null && time.IsAuthoritative);
        }

        // ── Accept ─────────────────────────────────────────────────────────

        /// <summary>
        /// Nhận quest từ NPC. Trả true nếu thành công.
        /// Điều kiện: chưa active, chưa completed (hoặc repeatable).
        /// </summary>
        public bool TryAccept(QuestDefinition quest)
        {
            if (quest == null) return false;

            QuestAcceptResult result = CheckAccept(quest);
            if (result != QuestAcceptResult.Ok)
            {
                Publish(quest, AcceptFailureMessage(result));
                return false;
            }

            if (quest.ObjectiveType == QuestObjectiveType.Collect && !HasStackableCollectTarget(quest))
            {
                Debug.LogWarning($"[PlayerQuestLog] Collect quest '{quest.Id}' requires a stackable target item.", this);
                return false;
            }

            QuestState state = _log.GetOrCreate(quest.Id);
            state.status = QuestStatus.Active;
            state.progress = quest.ObjectiveType == QuestObjectiveType.Collect
                ? QuestRules.SeedCollectProgress(quest, GetOwnedStackCount(quest.TargetId))
                : 0;
            if (state.progress >= quest.RequiredCount) state.status = QuestStatus.ReadyToTurnIn;

            Publish(quest, state.IsReadyToTurnIn ? "Đã đủ vật phẩm — hãy giao nhiệm vụ." : "Đã nhận nhiệm vụ");
            EnqueueSave();
            return true;
        }

        // ── Turn-in ────────────────────────────────────────────────────────

        /// <summary>
        /// Player nói chuyện NPC có quest đang ReadyToTurnIn → đánh dấu Completed NGAY trong bộ nhớ
        /// (UI thấy ngay), còn thưởng chỉ được cấp SAU KHI lần lưu chứa Completed thành công.
        /// Trả true nếu turn-in được ghi nhận. UI chỉ gọi sau khi NPC xác nhận intent.
        /// </summary>
        public bool TryTurnIn(QuestDefinition quest, PlayerInventory inventory = null)
        {
            if (quest == null) return false;
            if (!_log.IsReadyToTurnIn(quest.Id)) return false;

            // Với quest Collect: kiểm tra player thực sự còn đủ item (consume intent)
            if (quest.ObjectiveType == QuestObjectiveType.Collect)
            {
                PlayerInventory inv = inventory ?? playerInventory;
                int owned = inv != null ? inv.Inventory.GetStack(quest.TargetId) : 0;
                if (owned < quest.RequiredCount)
                {
                    // Không đủ item → rollback về Active
                    QuestState rollback = _log.Get(quest.Id);
                    if (rollback != null)
                    {
                        rollback.status = QuestStatus.Active;
                        rollback.progress = Mathf.Min(owned, quest.RequiredCount);
                    }
                    Publish(quest, "Không đủ vật phẩm để giao nhiệm vụ.");
                    EnqueueSave();
                    return false;
                }
            }

            bool ok = _log.TurnIn(quest.Id);
            if (!ok) return false;

            // Chưa cấp thưởng ở đây: nếu app bị đóng giữa lúc item/EXP đã lưu mà quest chưa lưu,
            // lần mở sau quest vẫn ReadyToTurnIn và người chơi nhận thưởng lần hai.
            _rewards.Enqueue(quest);
            Publish(quest, "Hoàn thành nhiệm vụ!");
            EnqueueSave();
            return true;
        }

        // ── Notify từ NPC/Zone ─────────────────────────────────────────────

        public void NotifyTalk(string npcId)  => Advance(QuestObjectiveType.Talk,  npcId);
        public void NotifyReach(string zoneId) => Advance(QuestObjectiveType.Reach, zoneId);

        // ── Event handlers ─────────────────────────────────────────────────

        private void HandleEnemyDefeated(object sender, EventArgs e)
        {
            if (sender is EnemyController enemy && enemy.Definition != null)
                Advance(QuestObjectiveType.Kill, enemy.Definition.Id);
        }

        private void HandleItemAcquiredWithPayload(object sender, ItemAcquiredArgs args)
        {
            if (args == null) return;
            AdvanceCollect(args.ItemId);
        }

        // ── Advance logic ──────────────────────────────────────────────────

        private void Advance(QuestObjectiveType type, string targetId)
        {
            ITimeService time = ResolveTimeService();
            TimeOfDay phase = time != null ? time.CurrentPhase : TimeOfDay.Morning;
            bool timeAuthoritative = time != null && time.IsAuthoritative;

            for (int i = 0; i < knownQuests.Count; i++)
            {
                QuestDefinition quest = knownQuests[i];
                if (!QuestRules.CanAdvance(quest, _log, type, targetId, phase, timeAuthoritative)) continue;

                bool ready = _log.AddProgress(quest.Id, quest.RequiredCount);
                Publish(quest, null);
                EnqueueSave();

                if (ready) Publish(quest, "Đến NPC để giao nhiệm vụ!");
            }
        }

        private void AdvanceCollect(string itemId)
        {
            ITimeService time = ResolveTimeService();
            TimeOfDay phase = time != null ? time.CurrentPhase : TimeOfDay.Morning;
            bool timeAuthoritative = time != null && time.IsAuthoritative;

            for (int i = 0; i < knownQuests.Count; i++)
            {
                QuestDefinition quest = knownQuests[i];
                if (!QuestRules.CanAdvance(quest, _log, QuestObjectiveType.Collect, itemId, phase, timeAuthoritative)) continue;

                // Đếm tuyệt đối từ kho — tránh double-count khi nhặt nhiều lần
                int owned = GetOwnedStackCount(itemId);

                QuestState state = _log.Get(quest.Id);
                if (state == null) continue;

                int newProgress = Mathf.Min(owned, quest.RequiredCount);
                if (newProgress <= state.progress) continue;

                bool ready = _log.AddProgress(quest.Id, quest.RequiredCount, newProgress - state.progress);
                Publish(quest, null);
                EnqueueSave();

                if (ready) Publish(quest, "Đến NPC để giao nhiệm vụ!");
            }
        }

        // ── Reward ────────────────────────────────────────────────────────

        private void GrantReward(QuestDefinition quest)
        {
            if (quest.ExperienceReward > 0)
                GameEvents.RaiseExperienceGained(this, quest.ExperienceReward);

            if (quest.ItemReward != null && playerInventory != null)
                playerInventory.TryAddItem(quest.ItemReward, quest.ItemRewardCount);

            Debug.Log($"[Quest] Hoàn thành '{quest.Title}' → +{quest.ExperienceReward} EXP", this);
        }

        private ITimeService ResolveTimeService()
        {
            if (_time != null) return _time;

            WorldClock worldClock = FindAnyObjectByType<WorldClock>();
            _time = worldClock != null ? worldClock.Clock : null;
            return _time;
        }

        private int GetOwnedStackCount(string itemId)
        {
            return playerInventory != null ? playerInventory.Inventory.GetStack(itemId) : 0;
        }

        private bool HasStackableCollectTarget(QuestDefinition quest)
        {
            return playerInventory != null &&
                   playerInventory.Database != null &&
                   playerInventory.Database.Find(quest.TargetId) != null &&
                   playerInventory.Database.Find(quest.TargetId).Stackable;
        }

        public static string AcceptFailureMessage(QuestAcceptResult result)
        {
            switch (result)
            {
                case QuestAcceptResult.Locked: return "Bạn chưa hoàn thành nhiệm vụ tiên quyết.";
                case QuestAcceptResult.WrongTime: return "Nhiệm vụ này chưa đúng thời điểm.";
                case QuestAcceptResult.TimeUnavailable: return "Chưa xác minh được thời gian thế giới.";
                case QuestAcceptResult.AlreadyActive: return "Bạn đã nhận nhiệm vụ này rồi.";
                case QuestAcceptResult.AlreadyCompleted: return "Bạn đã hoàn thành nhiệm vụ này rồi.";
                default: return null;
            }
        }

        // ── Publish ───────────────────────────────────────────────────────

        private void Publish(QuestDefinition quest, string message)
        {
            QuestState state = _log.Get(quest.Id);
            GameEvents.RaiseQuestChanged(this, new QuestChangedArgs
            {
                Quest    = quest,
                Progress = state != null ? state.progress : 0,
                Status   = state != null ? state.status  : QuestStatus.Available,
                Message  = message
            });
        }

        // ── Serial Save ───────────────────────────────────────────────────
        // Một writer duy nhất chụp snapshot SAU KHI giữ lock. Nếu gameplay đổi state
        // trong lúc write, dirty vẫn true và vòng lặp ghi snapshot mới kế tiếp.

        private void EnqueueSave()
        {
            if (!CanSave()) return;
            _dirty = true;
            if (_saveWriterRunning) return;

            _saveWriterRunning = true;
            _saveWriterTask = RunSaveWriterAsync();
        }

        private bool CanSave() => _loaded && _save != null && !_saveLocked && !_destroyed;

        private async Task RunSaveWriterAsync()
        {
            await _saveLock.WaitAsync();
            try
            {
                while (_dirty && _loaded && _save != null && !_saveLocked && !_destroyed)
                {
                    _dirty = false;
                    QuestData snapshot = BuildSnapshot();
                    // Thưởng đang chờ lúc này đã nằm trong snapshot vừa chụp.
                    _rewards.BeginSave();
                    try
                    {
                        await _save.SaveAsync(SaveKeys.Quests, snapshot);
                    }
                    catch (Exception ex)
                    {
                        // Lưu lỗi: thưởng quay lại hàng chờ, không cấp, không mất.
                        _rewards.RollbackSave();
                        Debug.LogError($"[PlayerQuestLog] Save thất bại, sẽ thử lại: {ex.Message}", this);
                        _dirty = true;
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5), _loadRetryCancellation.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        continue;
                    }

                    GrantCommittedRewards();
                }
            }
            finally
            {
                _saveLock.Release();
                _saveWriterRunning = false;
                if (_dirty) EnqueueSave();
            }
        }

        /// <summary>Lần lưu vừa thành công: cấp thưởng của các quest nằm trong snapshot đó (đúng một lần).</summary>
        private void GrantCommittedRewards()
        {
            IReadOnlyList<QuestDefinition> committed = _rewards.CommitSave();
            if (committed.Count == 0) return;

            // Đã chết / khóa lưu / bị hủy trong lúc lưu: không cấp thưởng.
            if (_saveLocked || _destroyed) return;

            for (int i = 0; i < committed.Count; i++)
            {
                try
                {
                    GrantReward(committed[i]);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PlayerQuestLog] Cấp thưởng '{committed[i]?.Id}' thất bại: {ex.Message}", this);
                }
            }
        }

        private QuestData BuildSnapshot()
        {
            var data = new QuestData();
            foreach (var pair in _log.States)
            {
                data.quests.Add(new QuestEntryData
                {
                    questId  = pair.Key,
                    status   = (int)pair.Value.status,
                    progress = pair.Value.progress
                });
            }
            return data;
        }

        public Task SaveAsync()
        {
            EnqueueSave();
            return _saveWriterTask ?? Task.CompletedTask;
        }

        private async Task LoadWithRetryAsync()
        {
            while (!_loaded && !_saveLocked && !_destroyed)
            {
                if (await LoadAsync()) return;

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), _loadRetryCancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        public async Task<bool> LoadAsync()
        {
            if (_save == null || _saveLocked || _destroyed) return false;

            try
            {
                QuestData data = await _save.LoadAsync<QuestData>(SaveKeys.Quests);
                if (_saveLocked || _destroyed) return false;

                var states = new List<QuestState>();
                if (data != null && data.quests != null)
                {
                    foreach (QuestEntryData entry in data.quests)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.questId)) continue;
                        states.Add(new QuestState(entry.questId, (QuestStatus)entry.status, entry.progress));
                    }
                }
                _log.LoadFrom(states);
                _loaded = true;

                for (int i = 0; i < knownQuests.Count; i++)
                {
                    if (knownQuests[i] != null && _log.Get(knownQuests[i].Id) != null)
                        Publish(knownQuests[i], null);
                }
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError($"[PlayerQuestLog] Nạp quest thất bại, sẽ thử lại: {error.Message}", this);
                return false;
            }
        }

        public void LoadFromData(QuestData data)
        {
            if (data == null || data.quests == null) return;
            var states = new List<QuestState>();
            foreach (QuestEntryData entry in data.quests)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.questId))
                    states.Add(new QuestState(entry.questId, (QuestStatus)entry.status, entry.progress));
            }
            _log.LoadFrom(states);
            _loaded = true;
        }
    }
}
