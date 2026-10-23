using Game.Core.Combat;
using Game.Core.Events;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Suối Sinh Mệnh Cổ Đại (Ancient Life Spring) trong Rừng Rậm Cổ Đại.
    /// Giúp hồi phục Sinh Lực và Năng Lượng khi đứng gần.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AncientLifeSpring : MonoBehaviour
    {
        [SerializeField] private double healPerSecond = 12.0;
        [SerializeField] private float adrenalinePerSecond = 15.0f;
        [SerializeField] private float tickInterval = 0.5f;

        private float _nextTick;
        private IDamageable _playerTarget;
        private AdrenalineController _playerAdrenaline;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                other.TryGetComponent(out _playerTarget);
                other.TryGetComponent(out _playerAdrenaline);
                GameEvents.RaiseCombatNotice("Hấp Thụ Suối Sinh Mệnh!", new Color(0.3f, 1f, 0.5f));
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerTarget = null;
                _playerAdrenaline = null;
            }
        }

        private void Update()
        {
            if (_playerTarget == null) return;

            if (UnityEngine.Time.time >= _nextTick)
            {
                _nextTick = UnityEngine.Time.time + tickInterval;
                double healAmount = healPerSecond * tickInterval;
                _playerTarget.Heal(healAmount);

                if (_playerAdrenaline != null)
                {
                    _playerAdrenaline.AddAdrenaline(adrenalinePerSecond * tickInterval);
                }
            }
        }
    }
}
