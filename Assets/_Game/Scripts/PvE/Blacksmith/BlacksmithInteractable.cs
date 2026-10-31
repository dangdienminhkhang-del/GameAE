using System;
using Game.Core.Events;
using Game.PvE.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PvE.Blacksmith
{
    /// <summary>
    /// Thợ rèn trong thị trấn (SAO: thợ rèn sửa và cường hóa vũ khí). Người chơi đến gần và bấm E
    /// (hoặc nút nổi ở giữa màn hình trên điện thoại) để mở bảng. Phát sự kiện tĩnh cho UI (ForgePanel) nên
    /// không phải sửa GameEvents dùng chung. Logic sửa/cường hóa nằm ở PlayerInventory (luật ở Core/Items).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BlacksmithInteractable : MonoBehaviour
    {
        /// <summary>Người chơi muốn mở bảng thợ rèn (UI nghe sự kiện này).</summary>
        public static event Action<BlacksmithInteractable> Requested;

        /// <summary>Người chơi vào/ra vùng tương tác (UI dùng để hiện nút nổi cho điện thoại).</summary>
        public static event Action<BlacksmithInteractable, bool> InRangeChanged;

        [SerializeField] private string smithName = "Thợ rèn";

        private bool _playerInRange;
        private Collider2D _trigger;

        public string SmithName => smithName;
        public bool PlayerInRange => _playerInRange;

        private void Awake()
        {
            if (TryGetComponent(out _trigger)) _trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_playerInRange || other == null || other.GetComponentInParent<PlayerController>() == null) return;
            _playerInRange = true;
            InRangeChanged?.Invoke(this, true);
            GameEvents.RaiseCombatNotice($"[E] Ghé {smithName}: sửa & cường hóa vũ khí", new Color(1f, 0.7f, 0.3f));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_playerInRange || other == null || other.GetComponentInParent<PlayerController>() == null) return;
            _playerInRange = false;
            InRangeChanged?.Invoke(this, false);
        }

        private void OnDisable()
        {
            if (_playerInRange)
            {
                _playerInRange = false;
                InRangeChanged?.Invoke(this, false);
            }
        }

        private void Update()
        {
            if (!_playerInRange) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame) Interact();
        }

        public void Interact()
        {
            Requested?.Invoke(this);
        }
    }
}
