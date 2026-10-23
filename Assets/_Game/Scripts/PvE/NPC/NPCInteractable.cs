using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.NPC;
using Game.Core.Quest;
using Game.Core.Shop;
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
        private bool _hasPhaseRestriction;
        private bool _lastTimeAuthoritative;
        private TimeOfDay _phase = TimeOfDay.Morning;
        private ITimeService _time;
        private float _resolveTimeTimer; // = 0f → gọi ngay frame đầu
        private Collider2D _interactionCollider;
        private int _lineIndex;

        // Danh sách NPC đang bật (Phase 9.9: mũi tên quest tra NPC theo npcId mà không phải Find). Static: reset khi vào Play.
        private static readonly List<NPCInteractable> s_registry = new List<NPCInteractable>();

        public static IReadOnlyList<NPCInteractable> Registered => s_registry;

        /// <summary>NPC đang bật có npcId này (null nếu không có). Không tìm trong scene.</summary>
        public static NPCInteractable FindByNpcId(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return null;
            for (int i = 0; i < s_registry.Count; i++)
            {
                NPCInteractable npc = s_registry[i];
                if (npc != null && npc.definition != null && npc.definition.Id == npcId) return npc;
            }
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            s_registry.Clear();
        }

        public NPCDefinition Definition => definition;
        public bool IsAvailable => _isAvailable;

        private void Awake()
        {
            if (spriteRenderer == null) TryGetComponent(out spriteRenderer);
            TryGetComponent(out _interactionCollider);
            if (questLog == null) questLog = FindAnyObjectByType<PlayerQuestLog>();
            _hasPhaseRestriction = definition != null && HasPhaseRestriction();
        }

        private void OnEnable()
        {
            if (!s_registry.Contains(this)) s_registry.Add(this);
            GameEvents.OnTimePhaseChanged     += HandlePhaseChanged;
            GameEvents.OnQuestAcceptRequested += HandleAcceptRequested;
            GameEvents.OnQuestTurnInRequested += HandleTurnInRequested;
            ApplyAvailability();
        }

        private void OnDisable()
        {
            s_registry.Remove(this);
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
            _isAvailable = !_hasPhaseRestriction ||
                           (_time != null && _time.IsAuthoritative && definition.IsAvailableAt(_phase));
            if (!_isAvailable) _playerInRange = false;
            if (spriteRenderer != null) spriteRenderer.enabled = _isAvailable;
            if (_interactionCollider != null) _interactionCollider.enabled = _isAvailable;
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
            if (_time == null)
            {
                _resolveTimeTimer -= Time.deltaTime;
                if (_resolveTimeTimer <= 0f)
                {
                    ResolveTimeService();
                    _resolveTimeTimer = 1f;
                }
            }
            bool timeAuthoritative = _time != null && _time.IsAuthoritative;
            if (timeAuthoritative != _lastTimeAuthoritative)
            {
                _lastTimeAuthoritative = timeAuthoritative;
                ApplyAvailability();
            }

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

            // NPC thương nhân: mở cửa hàng thay vì hội thoại nhiệm vụ
            if (definition.Shop != null)
            {
                ShopEvents.RaiseShopRequested(this, new ShopRequest { Npc = definition, Shop = definition.Shop });
                return;
            }

            // Phân loại trạng thái quest để chọn dialogue line
            bool readyToTurnIn = questLog != null && definition.OfferedQuest != null
                && questLog.IsReadyToTurnIn(definition.OfferedQuest);
            bool inProgress = questLog != null && definition.OfferedQuest != null
                && questLog.IsActive(definition.OfferedQuest);
            bool completed = questLog != null && definition.OfferedQuest != null
                && questLog.IsCompleted(definition.OfferedQuest)
                && !(definition.OfferedQuest.Repeatable);
            QuestAcceptResult acceptResult = questLog != null && definition.OfferedQuest != null
                ? questLog.CheckAccept(definition.OfferedQuest)
                : QuestAcceptResult.Locked;
            bool canAccept = acceptResult == QuestAcceptResult.Ok;
            bool acceptBlockedByRule = acceptResult == QuestAcceptResult.Locked ||
                                       acceptResult == QuestAcceptResult.WrongTime ||
                                       acceptResult == QuestAcceptResult.TimeUnavailable;

            string line;
            if (completed)       line = definition.QuestCompletedLine;
            else if (readyToTurnIn) line = "Ngươi đã hoàn thành rồi — hãy giao nhiệm vụ đi.";
            else if (inProgress) line = definition.QuestInProgressLine;
            else if (acceptBlockedByRule) line = PlayerQuestLog.AcceptFailureMessage(acceptResult);
            else if (definition.OfferedQuest != null) line = definition.QuestOfferLine;
            else if (definition.DialogueLines.Count > 0)
            {
                // Không có quest: mỗi lần nói chuyện đọc câu kế tiếp (xoay vòng).
                line = definition.DialogueLines[_lineIndex % definition.DialogueLines.Count];
                _lineIndex++;
            }
            else line = "...";

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

            QuestAcceptResult result = questLog.CheckAccept(definition.OfferedQuest);
            bool accepted = result == QuestAcceptResult.Ok && questLog.TryAccept(definition.OfferedQuest);
            GameEvents.RaiseNpcDialogue(this, new NpcDialogueArgs
            {
                Npc              = definition,
                Line             = accepted ? "Hãy đi đi. Ta chờ tin tốt." :
                    (result == QuestAcceptResult.Ok ? "Mục tiêu Thu thập phải là vật phẩm xếp chồng." : PlayerQuestLog.AcceptFailureMessage(result)),
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
