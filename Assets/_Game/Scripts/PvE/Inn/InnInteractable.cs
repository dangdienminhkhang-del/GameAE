using Game.Core.Events;
using Game.Core.Inn;
using Game.Core.Time;
using Game.PvE.Items;
using Game.PvE.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PvE.Inn
{
    /// <summary>
    /// NPC hoặc điểm tương tác Quán trọ (Inn) trong thị trấn (docs/16 & prompt).
    /// Người chơi đến gần và bấm E hoặc tương tác để mở giao dịch nghỉ ngơi.
    /// Dùng lại mẫu NPCInteractable có sẵn, phát sự kiện qua GameEvents để UI hiển thị.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InnInteractable : MonoBehaviour
    {
        [Header("Thông tin Quán trọ")]
        [SerializeField] private string innName = "Quán trọ Bồ Câu Trắng";
        [SerializeField] private int restCost = InnRules.DefaultRestCost;
        [SerializeField] private float interactionRadius = 2.5f;

        private bool _playerInRange;
        private Collider2D _triggerCollider;

        public string InnName => innName;
        public int RestCost => restCost;
        public float InteractionRadius => interactionRadius;
        public bool PlayerInRange => _playerInRange;

        private void Awake()
        {
            TryGetComponent(out _triggerCollider);
            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInRange = true;
                GameEvents.RaiseCombatNotice($"[E] Nói chuyện với Chủ {innName}", Color.cyan);
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

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                Interact();
            }
        }

        /// <summary>
        /// Mở bảng đối thoại quán trọ khi tương tác (phát qua GameEvents để UI bắt).
        /// </summary>
        public void Interact()
        {
            GameEvents.RaiseInnRequested(new InnRequestArgs
            {
                InnName = innName,
                RestCost = restCost,
                OnExecuteRest = ExecuteRest,
                Position = transform.position
            });
        }

        /// <summary>
        /// Thực hiện logic nghỉ trọ (gọi từ UI hoặc test).
        /// </summary>
        public InnRestResult ExecuteRest()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return InnRestResult.InvalidArguments;

            var wallet = player.GetComponent<PlayerWallet>();
            var health = player.GetComponent<HealthComponent>();
            var inventory = player.GetComponent<PlayerInventory>();

            int currentCol = wallet != null ? wallet.Col : 0;
            bool inCombat = inventory != null && inventory.IsInCombat;
            bool isDead = health != null && health.IsDead;

            InnRestResult check = InnRules.CheckCanRest(currentCol, restCost, inCombat, isDead);
            if (check != InnRestResult.Success)
            {
                GameEvents.RaiseCombatNotice(InnRules.GetResultMessage(check, restCost), new Color(1f, 0.4f, 0.4f));
                return check;
            }

            // 1. Trừ tiền Col
            if (wallet != null)
            {
                wallet.TrySpend(restCost);
            }

            // 2. Hồi phục toàn bộ máu
            if (health != null)
            {
                health.Heal(health.MaxHealth);
            }

            // 3. Tua thời gian sang buổi sáng
            AdvanceTimeToMorning();

            // 4. Thông báo thành công
            GameEvents.RaiseCombatNotice(InnRules.GetResultMessage(InnRestResult.Success, restCost), Color.green);
            return InnRestResult.Success;
        }

        private void AdvanceTimeToMorning()
        {
            var clock = FindAnyObjectByType<Game.PvE.Clock.WorldClock>();
            ITimeService time = clock != null ? clock.Clock : null;
            if (time != null)
            {
                time.SetGameTime(InnRules.DefaultMorningHour);
            }
        }
    }
}
