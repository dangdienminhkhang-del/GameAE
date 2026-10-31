using System.Collections;
using Game.Core.Events;
using Game.Core.Items;
using Game.PvE.Player;
using Game.PvE.SafeZone;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PvE.Items
{
    /// <summary>
    /// Điều khiển sử dụng Tinh Thể Dịch Chuyển (Teleport Crystal theo SAO).
    /// Đứng yên niệm chú 3 giây để dịch chuyển về Vùng an toàn của thị trấn.
    /// Huỷ niệm chú nếu trúng đòn hoặc di chuyển.
    /// </summary>
    [DisallowMultipleComponent]
    public class TeleportCrystalController : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, 0.85f, 0.35f);
        private static readonly Color Warn = new Color(1f, 0.45f, 0.35f);

        private PlayerInventory _inventory;
        private HealthComponent _health;
        private Rigidbody2D _rb;

        private Coroutine _channelCoroutine;
        private bool _isChanneling;
        private Vector2 _channelStartPosition;

        public bool IsChanneling => _isChanneling;

        private void Awake()
        {
            TryGetComponent(out _inventory);
            TryGetComponent(out _health);
            TryGetComponent(out _rb);
        }

        private void OnEnable()
        {
            GameEvents.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            CancelChannel(null);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.tKey.wasPressedThisFrame)
            {
                TryUseCrystal();
            }

            if (_isChanneling)
            {
                float distanceMoved = Vector2.Distance(transform.position, _channelStartPosition);
                if (TeleportCrystalRules.IsCancelledByMovement(distanceMoved))
                {
                    CancelChannel("Dịch chuyển bị huỷ: bạn đã di chuyển!");
                }
            }
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs args)
        {
            if (!_isChanneling || args == null) return;
            if (ReferenceEquals(sender, _health) && args.Delta < 0)
            {
                CancelChannel("Dịch chuyển bị huỷ: bạn vừa bị tấn công!");
            }
        }

        /// <summary>
        /// Kích hoạt sử dụng Tinh Thể Dịch Chuyển (gọi từ phím T, UI nút hoặc test).
        /// </summary>
        public TeleportCrystalResult TryUseCrystal()
        {
            int count = _inventory != null ? _inventory.Inventory.GetStack(TeleportCrystalRules.ItemId) : 0;
            bool isDead = _health != null && _health.IsDead;

            TeleportCrystalResult result = TeleportCrystalRules.CanUse(count, isDead, _isChanneling);
            if (result != TeleportCrystalResult.Ok)
            {
                string msg = TeleportCrystalRules.DescribeResult(result);
                if (!string.IsNullOrEmpty(msg))
                {
                    GameEvents.RaiseCombatNotice(msg, Warn);
                }
                return result;
            }

            _channelCoroutine = StartCoroutine(ChannelRoutine());
            return TeleportCrystalResult.Ok;
        }

        public void CancelChannel(string reason)
        {
            if (_channelCoroutine != null)
            {
                StopCoroutine(_channelCoroutine);
                _channelCoroutine = null;
            }

            if (_isChanneling && !string.IsNullOrEmpty(reason))
            {
                GameEvents.RaiseCombatNotice(reason, Warn);
            }

            _isChanneling = false;
        }

        private IEnumerator ChannelRoutine()
        {
            _isChanneling = true;
            _channelStartPosition = transform.position;

            GameEvents.RaiseCombatNotice("Đang niệm chú: [DỊCH CHUYỂN, THỊ TRẤN!]... Đứng yên 3 giây", Gold);

            float timer = 0f;
            while (timer < TeleportCrystalRules.ChannelSeconds)
            {
                yield return null;
                timer += Time.deltaTime;
            }

            // Hoàn thành niệm chú: trừ 1 tinh thể khỏi túi đồ
            if (_inventory != null && _inventory.Inventory != null)
            {
                _inventory.Inventory.RemoveStack(TeleportCrystalRules.ItemId, 1);
            }

            // Tìm điểm đến: SafeZone gần nhất hoặc đầu tiên
            Vector3 targetPos = Vector3.zero;
            var safeZone = FindAnyObjectByType<SafeZoneTrigger>();
            if (safeZone != null)
            {
                targetPos = safeZone.transform.position;
            }

            transform.position = targetPos;
            if (_rb != null)
            {
                _rb.position = targetPos;
                _rb.linearVelocity = Vector2.zero;
            }

            _isChanneling = false;
            _channelCoroutine = null;

            GameEvents.RaiseCombatNotice("Đã dịch chuyển về thị trấn an toàn!", Color.green);
        }
    }
}
