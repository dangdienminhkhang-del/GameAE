using Game.PvE.NPC;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Controls
{
    /// <summary>
    /// Nút cảm ứng cho hành động PvE (Android không có bàn phím).
    /// CHỈ gọi API public, không chứa logic gameplay (luật #3).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PvEActionButton : MonoBehaviour
    {
        public enum ActionKind { Interact = 0, QuestLog = 1, Inventory = 2 }

        [SerializeField] private ActionKind action = ActionKind.Interact;
        [SerializeField] private float interactRange = 3f;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(HandleClick);
        }

        public void HandleClick()
        {
            switch (action)
            {
                case ActionKind.QuestLog:
                    Quest.QuestPanel panel = FindAnyObjectByType<Quest.QuestPanel>();
                    if (panel != null) panel.ToggleQuestLog();
                    break;

                case ActionKind.Inventory:
                    Inventory.InventoryPanel inventory = FindAnyObjectByType<Inventory.InventoryPanel>();
                    if (inventory != null) inventory.Toggle();
                    break;

                default:
                    TalkToNearestNpc();
                    break;
            }
        }

        /// <summary>Gọi hội thoại NPC gần nhất trong tầm (nút "Nói chuyện").</summary>
        public void TalkToNearestNpc()
        {
            var player = FindAnyObjectByType<Game.PvE.Player.PlayerController>();
            Vector2 origin = player != null ? (Vector2)player.transform.position : Vector2.zero;

            NPCInteractable[] npcs = FindObjectsByType<NPCInteractable>(FindObjectsSortMode.None);
            NPCInteractable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < npcs.Length; i++)
            {
                NPCInteractable npc = npcs[i];
                if (npc == null || !npc.gameObject.activeInHierarchy) continue;

                float distance = Vector2.Distance(origin, npc.transform.position);
                if (distance <= interactRange && distance < bestDistance)
                {
                    best = npc;
                    bestDistance = distance;
                }
            }

            if (best != null) best.Talk();
            else Debug.Log("[PvEActionButton] Không có NPC trong tầm.");
        }
    }
}
