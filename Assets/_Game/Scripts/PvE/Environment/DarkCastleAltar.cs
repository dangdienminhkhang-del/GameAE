using Game.Core.Events;
using Game.PvE.Camera;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Bàn Thờ Bóng Tối (Dark Castle Altar) trong Lâu Đài Bóng Tối.
    /// Kích hoạt sức mạnh bóng tối: ban tặng 50 Adrenaline và 5 giây Bất Tử (Invulnerable).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DarkCastleAltar : MonoBehaviour
    {
        [SerializeField] private float invulnerableDuration = 5.0f;
        [SerializeField] private float adrenalineBonus = 50.0f;
        [SerializeField] private float cooldown = 45.0f;

        private float _nextAvailableTime = 0f;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && UnityEngine.Time.time >= _nextAvailableTime)
            {
                ActivateAltar(other.gameObject);
            }
        }

        private void ActivateAltar(GameObject player)
        {
            _nextAvailableTime = UnityEngine.Time.time + cooldown;

            if (player.TryGetComponent<AdrenalineController>(out var adrenaline))
            {
                adrenaline.AddAdrenaline(adrenalineBonus);
            }

            if (player.TryGetComponent<HealthComponent>(out var health))
            {
                health.IsInvulnerable = true;
                CancelInvoke(nameof(EndInvulnerability));
                Invoke(nameof(EndInvulnerability), invulnerableDuration);
                _cachedHealth = health;
            }

            CameraFollow.RequestShake(0.22f, 0.25f);
            GameEvents.RaiseCombatNotice("Phong Ấn Bóng Tối (Bất Tử 5s)!", new Color(0.7f, 0.4f, 1.0f));
        }

        private HealthComponent _cachedHealth;

        private void EndInvulnerability()
        {
            if (_cachedHealth != null)
            {
                _cachedHealth.IsInvulnerable = false;
                GameEvents.RaiseCombatNotice("Hết Bất Tử", new Color(0.7f, 0.7f, 0.7f));
            }
        }

        private void OnDisable()
        {
            EndInvulnerability();
        }
    }
}
