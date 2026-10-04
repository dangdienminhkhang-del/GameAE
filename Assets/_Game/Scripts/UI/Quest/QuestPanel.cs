using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.NPC;
using Game.Core.Quest;
using Game.PvE.Quest;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI.Quest
{
    /// <summary>
    /// UI hội thoại NPC + sổ nhiệm vụ (Wave 1A).
    /// Chỉ hiển thị và gửi ý định — không chứa logic quest.
    ///
    /// Nút "Nhận nhiệm vụ"  → RaiseQuestAcceptRequested
    /// Nút "Giao nhiệm vụ"  → RaiseQuestTurnInRequested  (Wave 1A)
    /// Nút đóng / phím Q    → đóng panel
    /// Phím E               → mở hội thoại NPC gần nhất (keyboard Editor support)
    ///
    /// Touch path: các Button trong prefab có thể gán OnClick → HandleAccept / HandleTurnIn / Close.
    /// </summary>
    public class QuestPanel : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PlayerQuestLog questLog;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;

        [Header("Buttons (gán trong Inspector)")]
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button turnInButton;   // Wave 1A — nút "Giao nhiệm vụ"
        [SerializeField] private Button closeButton;

        private NPCDefinition _currentNpc;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (questLog == null) questLog = FindAnyObjectByType<PlayerQuestLog>();

            if (panelRoot != null) panelRoot.SetActive(false);
            if (acceptButton  != null) acceptButton.onClick.AddListener(HandleAccept);
            if (turnInButton  != null) turnInButton.onClick.AddListener(HandleTurnIn);
            if (closeButton   != null) closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            GameEvents.OnNpcDialogue   += HandleDialogue;
            GameEvents.OnQuestChanged  += HandleQuestChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnNpcDialogue   -= HandleDialogue;
            GameEvents.OnQuestChanged  -= HandleQuestChanged;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.qKey.wasPressedThisFrame) ToggleQuestLog();
        }

        // ── Event handlers ─────────────────────────────────────────────────

        private void HandleDialogue(object sender, NpcDialogueArgs args)
        {
            if (args == null || args.Npc == null) return;
            _currentNpc = args.Npc;
            Show(args.Npc.DisplayName, args.Line,
                showAccept:  args.ShowAcceptButton,
                showTurnIn:  args.ShowTurnInButton);
        }

        private void HandleQuestChanged(object sender, QuestChangedArgs args)
        {
            if (args == null || args.Quest == null) return;
            if (!string.IsNullOrEmpty(args.Message))
                Show(args.Quest.Title, args.Message, showAccept: false, showTurnIn: false);
        }

        // ── Show / Close ───────────────────────────────────────────────────

        public void Show(string title, string body, bool showAccept, bool showTurnIn)
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (bodyText  != null) bodyText.text  = body;
            if (acceptButton != null) acceptButton.gameObject.SetActive(showAccept);
            if (turnInButton != null) turnInButton.gameObject.SetActive(showTurnIn);
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Button handlers ────────────────────────────────────────────────

        /// <summary>Nút "Nhận nhiệm vụ" — UI chỉ gửi ý định.</summary>
        public void HandleAccept()
        {
            if (_currentNpc == null) return;
            GameEvents.RaiseQuestAcceptRequested(this, new NpcDialogueArgs { Npc = _currentNpc });
        }

        /// <summary>Nút "Giao nhiệm vụ" — UI chỉ gửi ý định (Wave 1A).</summary>
        public void HandleTurnIn()
        {
            if (_currentNpc == null) return;
            GameEvents.RaiseQuestTurnInRequested(this, new NpcDialogueArgs { Npc = _currentNpc });
        }

        // ── Quest log (Q key) ──────────────────────────────────────────────

        public void ToggleQuestLog()
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
                Close();
                return;
            }
            Show("Sổ nhiệm vụ", BuildLogText(), showAccept: false, showTurnIn: false);
        }

        /// <summary>Nội dung sổ nhiệm vụ — chỉ đọc dữ liệu từ PlayerQuestLog.</summary>
        public string BuildLogText()
        {
            if (questLog == null) return "(chưa có sổ nhiệm vụ)";

            var lines = new List<string>();
            IReadOnlyList<QuestDefinition> known = questLog.KnownQuests;

            for (int i = 0; i < known.Count; i++)
            {
                QuestDefinition quest = known[i];
                if (quest == null) continue;

                QuestState state = questLog.GetState(quest);
                if (state == null)
                {
                    lines.Add($"• {quest.Title} — chưa nhận");
                    continue;
                }

                switch (state.status)
                {
                    case QuestStatus.Completed:
                        lines.Add($"✔ {quest.Title} — hoàn thành");
                        break;
                    case QuestStatus.ReadyToTurnIn:
                        lines.Add($"★ {quest.Title} — đến NPC giao nhiệm vụ!");
                        break;
                    case QuestStatus.Active:
                        lines.Add($"• {quest.Title} — {quest.ObjectiveText(state.progress)}");
                        break;
                    default:
                        lines.Add($"• {quest.Title} — chưa nhận");
                        break;
                }
            }

            if (lines.Count == 0) lines.Add("(chưa có nhiệm vụ nào)");
            lines.Add("");
            lines.Add("Q: đóng/mở sổ  |  E: nói chuyện NPC");
            return string.Join("\n", lines);
        }

        // ── Helper cho test / AI ───────────────────────────────────────────

        public void AcceptForNpc(NPCDefinition npc)
        {
            _currentNpc = npc;
            HandleAccept();
        }

        public void TurnInForNpc(NPCDefinition npc)
        {
            _currentNpc = npc;
            HandleTurnIn();
        }
    }
}
