using Game.Core.Events;
using Game.Core.NPC;
using Game.Core.Quest;
using Game.Core.Time;
using Game.PvE.Quest;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PvE.NPC
{
    /// <summary>
    /// NPC tương tác: player đứng trong vùng + bấm E (hoặc nút cảm ứng) để nói chuyện.
    /// NPC có thể chỉ xuất hiện ở một pha thời gian (docs/08 §3.3).
    /// UI chỉ hiển thị — logic quest nằm ở PlayerQuestLog.
    ///
    /// Turn-in flow (Wave 1A):
    ///   1. Player Talk() → NPC phát hiện quest ReadyToTurnIn → ShowTurnIn = true
    ///   2. UI hiển thị nút "Giao nhiệm vụ" → UI raise OnQuestTurnInRequested
    ///   3. HandleTurnInRequested → PlayerQuestLog.TryTurnIn → reward
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NPCInteractable : MonoBehaviour
    {
        [SerializeField] private NPCDefinition definition;
        [SerializeField] private PlayerQuestLog questLog;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private bool _playerInRange;
        private bool _isAvailable;
        private TimeOfDay _phase = TimeOfDay.Morning;
        private ITimeService _time;

        public NPCDefinition Definition => definition;
        public bool IsAvailable => _isAvailable;

        private void Awake()
        {
            if (spriteRenderer == null) TryGetComponent(out spriteRenderer);
            if (questLog == null) questLog = FindAnyObjectByType<PlayerQuestLog>();
        }

        private void OnEnable()
        {
            GameEvents.OnTimeChanged          += HandleTimeChanged;
            GameEvents.OnTimePhaseChanged     += HandlePhaseChanged;
            GameEvents.OnQuestAcceptRequested += HandleAcceptRequested;
            GameEvents.OnQuestTurnInRequested += HandleTurnInRequested;
            ApplyAvailability();
        }

        private void OnDisable()
        {
            GameEvents.OnTimeChanged          -= HandleTimeChanged;
            GameEvents.OnTimePhaseChanged     -= HandlePhaseChanged;
            GameEvents.OnQuestAcceptRequested -= HandleAcceptRequested;
            GameEvents.OnQuestTurnInRequested -= HandleTurnInRequested;
        }

        private void Start()
        {
            ResolveTimeService();
            ApplyAvailability();
        }

        // ── Availability ───────────────────────────────────────────────────

        private void HandlePhaseChanged(object sender, TimeChangedArgs args)
        {
            _phase = args.Phase;
            ResolveTimeService();
            ApplyAvailability();
        }

        private void HandleTimeChanged(object sender, TimeChangedArgs args)
        {
            _phase = args.Phase;
            ResolveTimeService();
            ApplyAvailability();
        }

        private void ResolveTimeService()
        {
            if (_time != null) return;
            var clock = FindAnyObjectByType<Game.PvE.Clock.WorldClock>();
            _time = clock != null ? clock.Clock : null;
            if (_time != null) _phase = _time.CurrentPhase;
        }

        private void ApplyAvailability()
        {
            if (definition == null) return;

            // Không truy cập OnlyAtSpecificTime: contract NPCDefinition chỉ công khai
            // IsAvailableAt. Nếu definition giới hạn bất kỳ phase nào, phải chờ giờ trusted.
            bool hasPhaseRestriction = HasPhaseRestriction();
            _isAvailable = !hasPhaseRestriction ||
                           (_time != null && _time.IsAuthoritative && definition.IsAvailableAt(_phase));
            if (spriteRenderer != null) spriteRenderer.enabled = _isAvailable;
        }

        private bool HasPhaseRestriction()
        {
            foreach (TimeOfDay phase in System.Enum.GetValues(typeof(TimeOfDay)))
            {
                if (!definition.IsAvailableAt(phase)) return true;
            }
            return false;
        }

        // ── Trigger ────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<Game.PvE.Player.PlayerController>() != null)
                _playerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<Game.PvE.Player.PlayerController>() != null)
                _playerInRange = false;
        }

        // ── Input ──────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_playerInRange || definition == null || !_isAvailable) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame) Talk();
        }

        // ── Talk ───────────────────────────────────────────────────────────

        /// <summary>Mở hội thoại — gọi từ input, touch button, hoặc test.</summary>
        public void Talk()
        {
            if (definition == null || !_isAvailable) return;

            // Báo "nói chuyện" cho objective Talk (mọi NPC)
            if (questLog != null) questLog.NotifyTalk(definition.Id);

            // Phân loại trạng thái quest để chọn dialogue line
            bool readyToTurnIn = questLog != null && definition.OfferedQuest != null
                && questLog.IsReadyToTurnIn(definition.OfferedQuest);
            bool inProgress = questLog != null && definition.OfferedQuest != null
                && questLog.IsActive(definition.OfferedQuest);
            bool completed = questLog != null && definition.OfferedQuest != null
                && questLog.IsCompleted(definition.OfferedQuest)
                && !(definition.OfferedQuest.Repeatable);
            bool canAccept = questLog != null && definition.OfferedQuest != null
                && !questLog.IsActive(definition.OfferedQuest)
                && !questLog.IsReadyToTurnIn(definition.OfferedQuest)
                && (!questLog.IsCompleted(definition.OfferedQuest) || definition.OfferedQuest.Repeatable);

            string line;
            if (completed)       line = definition.QuestCompletedLine;
            else if (readyToTurnIn) line = "Ngươi đã hoàn thành rồi — hãy giao nhiệm vụ đi.";
            else if (inProgress) line = definition.QuestInProgressLine;
            else if (definition.OfferedQuest != null) line = definition.QuestOfferLine;
            else line = definition.DialogueLines.Count > 0 ? definition.DialogueLines[0] : "...";

            GameEvents.RaiseNpcDialogue(this, new NpcDialogueArgs
            {
                Npc             = definition,
                Line            = line,
                ShowAcceptButton   = canAccept,
                ShowTurnInButton   = readyToTurnIn
            });
        }

        // ── Event handlers ─────────────────────────────────────────────────

        private void HandleAcceptRequested(object sender, NpcDialogueArgs args)
        {
            if (args == null || args.Npc != definition) return;
            if (!_isAvailable || questLog == null || definition.OfferedQuest == null) return;

            bool accepted = questLog.TryAccept(definition.OfferedQuest);
            GameEvents.RaiseNpcDialogue(this, new NpcDialogueArgs
            {
                Npc              = definition,
                Line             = accepted ? "Hãy đi đi. Ta chờ tin tốt." : "Ngươi đã nhận việc này rồi.",
                ShowAcceptButton  = false,
                ShowTurnInButton  = false
            });
        }

        private void HandleTurnInRequested(object sender, NpcDialogueArgs args)
        {
            if (args == null || args.Npc != definition) return;
            if (!_isAvailable || questLog == null || definition.OfferedQuest == null) return;

            bool ok = questLog.TryTurnIn(definition.OfferedQuest);
            string reply = ok
                ? $"Ta nhận lấy. Đây là phần thưởng của ngươi. (+{definition.OfferedQuest.ExperienceReward} EXP)"
                : "Ngươi chưa hoàn thành nhiệm vụ.";

            GameEvents.RaiseNpcDialogue(this, new NpcDialogueArgs
            {
                Npc              = definition,
                Line             = reply,
                ShowAcceptButton  = false,
                ShowTurnInButton  = false
            });
        }
    }
}
