using Game.Core.Events;
using Game.PvE.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PvE.LifeSkills
{
    /// <summary>
    /// Điểm tương tác Bếp Nấu Ăn (Cooking Pot theo phong cách SAO).
    /// Người chơi đến gần và bấm E để mở bảng Nấu Ăn.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CookingPotInteractable : MonoBehaviour
    {
        [SerializeField] private string potName = "Bếp Lửa Dã Ngoại";

        private bool _playerInRange;

        private void Awake()
        {
            if (TryGetComponent(out Collider2D col))
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInRange = true;
                GameEvents.RaiseCombatNotice($"[E] Nấu ăn tại {potName}", Color.cyan);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInRange = false;
            }
        }

        private void Update()
        {
            if (!_playerInRange) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
            {
                Interact();
            }
        }

        public void Interact()
        {
            GameEvents.RaiseCookingRequested();
        }
    }
}
