using System.Collections;
using Game.Core.Events;
using Game.Core.LifeSkills;
using Game.PvE.Items;
using Game.PvE.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PvE.LifeSkills
{
    /// <summary>
    /// Điểm câu cá trong thế giới (bờ hồ, vịnh biển theo phong cách SAO tầng 22).
    /// Mini-game một nút: Thả cần -> chờ cá cắn -> bấm đúng cửa sổ phản xạ để bắt cá.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FishingSpotInteractable : MonoBehaviour
    {
        private enum FishingState { Idle, WaitingBite, FishBiting }

        [Header("Cấu hình điểm câu")]
        [SerializeField] private string spotName = "Hồ Nước Trong";
        [SerializeField] private float minBiteDelay = 2.0f;
        [SerializeField] private float maxBiteDelay = 4.5f;

        private bool _playerInRange;
        private FishingState _state = FishingState.Idle;
        private Coroutine _fishingRoutine;
        private float _biteStartTime;
        private float _currentAllowedWindow;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInRange = true;
                if (_state == FishingState.Idle)
                {
                    GameEvents.RaiseCombatNotice($"[E] Thả cần câu cá tại {spotName}", Color.cyan);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInRange = false;
                CancelFishing("Đã rời khỏi điểm câu cá.");
            }
        }

        private void Update()
        {
            if (!_playerInRange) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
            {
                OnActionPressed();
            }
        }

        public void OnActionPressed()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;

            var lifeSkills = player.GetComponent<PlayerLifeSkills>();
            var inventory = player.GetComponent<PlayerInventory>();

            switch (_state)
            {
                case FishingState.Idle:
                    StartFishing(lifeSkills);
                    break;

                case FishingState.WaitingBite:
                    // Bấm quá sớm khi cá chưa cắn
                    CancelFishing("Bạn giật cần quá sớm! Cá sợ chạy mất rồi.");
                    break;

                case FishingState.FishBiting:
                    // Bấm khi cá đang cắn!
                    ResolveCatch(lifeSkills, inventory);
                    break;
            }
        }

        private void StartFishing(PlayerLifeSkills lifeSkills)
        {
            _state = FishingState.WaitingBite;
            GameEvents.RaiseCombatNotice("Đã thả cần câu... Đang chờ cá cắn...", Color.yellow);
            _fishingRoutine = StartCoroutine(FishingSequence(lifeSkills));
        }

        private IEnumerator FishingSequence(PlayerLifeSkills lifeSkills)
        {
            float delay = Random.Range(minBiteDelay, maxBiteDelay);
            yield return new WaitForSeconds(delay);

            if (_state != FishingState.WaitingBite) yield break;

            _state = FishingState.FishBiting;
            _biteStartTime = Time.time;

            int prof = lifeSkills != null ? lifeSkills.FishingProficiency : 0;
            _currentAllowedWindow = LifeSkillRules.CalculateReactionWindow(prof);

            GameEvents.RaiseCombatNotice("💥 CÁ CẮN CÂU! NHẤN [E] NGAY! 💥", new Color(1f, 0.3f, 0.2f));

            yield return new WaitForSeconds(_currentAllowedWindow);

            if (_state == FishingState.FishBiting)
            {
                // Hết giờ mà chưa bấm
                CancelFishing("Quá chậm! Cá đã đớp mồi rồi chuồn mất.");
            }
        }

        private void ResolveCatch(PlayerLifeSkills lifeSkills, PlayerInventory inventory)
        {
            float elapsed = Time.time - _biteStartTime;
            if (_fishingRoutine != null)
            {
                StopCoroutine(_fishingRoutine);
                _fishingRoutine = null;
            }

            _state = FishingState.Idle;

            if (LifeSkillRules.CheckFishingSuccess(elapsed, _currentAllowedWindow))
            {
                // Thêm cá vào túi đồ
                if (inventory != null)
                {
                    var def = inventory.Database != null ? inventory.Database.Find(LifeSkillRules.FishItemId) : null;
                    if (def != null)
                    {
                        inventory.TryAddItem(def, 1);
                    }
                    else if (inventory.Inventory != null)
                    {
                        inventory.Inventory.AddStack(LifeSkillRules.FishItemId, 1, 99);
                    }
                }

                if (lifeSkills != null)
                {
                    lifeSkills.AddFishingExp(LifeSkillRules.ExpPerFishingSuccess);
                }

                GameEvents.RaiseCombatNotice($"🎉 BẮT ĐƯỢC 1 CÁ TƯƠI! (+{LifeSkillRules.ExpPerFishingSuccess} EXP Câu cá)", Color.green);
            }
            else
            {
                GameEvents.RaiseCombatNotice("Cá đã vùng thoát!", Color.gray);
            }
        }

        private void CancelFishing(string reason)
        {
            if (_fishingRoutine != null)
            {
                StopCoroutine(_fishingRoutine);
                _fishingRoutine = null;
            }

            if (_state != FishingState.Idle && !string.IsNullOrEmpty(reason))
            {
                GameEvents.RaiseCombatNotice(reason, Color.gray);
            }

            _state = FishingState.Idle;
        }
    }
}
