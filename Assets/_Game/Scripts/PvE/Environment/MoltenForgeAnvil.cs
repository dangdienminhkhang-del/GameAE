using Game.Core.Events;
using Game.PvE.Camera;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Đe Rèn Dung Nham (Molten Forge Anvil) trong Hầm Mỏ Dung Nham.
    /// Cho phép người chơi tôi luyện vũ khí với lửa dung nham (+35% sát thương trong 30 giây).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MoltenForgeAnvil : MonoBehaviour
    {
        [SerializeField] private float buffDuration = 30f;
        [SerializeField] private float damageBonus = 1.35f;

        private float _buffTimer = 0f;
        private bool _isBuffActive = false;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                ApplyMoltenBuff();
            }
        }

        private void ApplyMoltenBuff()
        {
            _buffTimer = buffDuration;
            if (!_isBuffActive)
            {
                _isBuffActive = true;
                PlayerAttack.ExternalDamageMultiplier = damageBonus;
                CameraFollow.RequestShake(0.18f, 0.2f);
                GameEvents.RaiseCombatNotice("Lửa Dung Nham! (+35% Dmg)", new Color(1f, 0.5f, 0.1f));
            }
        }

        private void Update()
        {
            if (!_isBuffActive) return;

            _buffTimer -= UnityEngine.Time.deltaTime;
            if (_buffTimer <= 0f)
            {
                _isBuffActive = false;
                PlayerAttack.ExternalDamageMultiplier = 1.0f;
                GameEvents.RaiseCombatNotice("Hết hiệu lực Lửa Dung Nham", new Color(0.8f, 0.8f, 0.8f));
            }
        }

        private void OnDisable()
        {
            if (_isBuffActive)
            {
                _isBuffActive = false;
                PlayerAttack.ExternalDamageMultiplier = 1.0f;
            }
        }
    }
}
