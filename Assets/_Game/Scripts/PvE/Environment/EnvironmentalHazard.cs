using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Events;
using Game.PvE.Camera;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Environment
{
    public enum HazardType
    {
        MagmaLava,      // Dung nham nóng bỏng
        ThornBramble,   // Bụi gai rừng rậm
        VoidAbyss       // Vực thẳm bóng tối
    }

    /// <summary>
    /// Hiệu ứng cạm bẫy / môi trường cho các map thế giới (Rừng Cổ, Mỏ Dung Nham, Lâu Đài).
    /// Hỗ trợ gây sát thương thiêu đốt, làm chậm (slow), và đẩy lùi (knockback).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnvironmentalHazard : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [SerializeField] private HazardType hazardType = HazardType.MagmaLava;
        [SerializeField] private double damagePerSecond = 10.0;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private float slowFactor = 0.5f; // Giảm 50% tốc độ cho ThornBramble
        [SerializeField] private bool affectsEnemies = true;

        private readonly HashSet<IDamageable> _targetsInside = new HashSet<IDamageable>();
        private readonly Dictionary<IDamageable, float> _nextTickTimes = new Dictionary<IDamageable, float>();
        private readonly HashSet<PlayerController> _slowedPlayers = new HashSet<PlayerController>();

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                if (affectsEnemies || other.CompareTag("Player"))
                {
                    _targetsInside.Add(damageable);
                    _nextTickTimes[damageable] = UnityEngine.Time.time; // tick ngay khi bước vào
                }
            }

            if (hazardType == HazardType.ThornBramble && other.TryGetComponent<PlayerController>(out var player))
            {
                player.SpeedMultiplier = slowFactor;
                _slowedPlayers.Add(player);
                GameEvents.RaiseCombatNotice("Bụi Gai! (Làm Chậm 50%)", new Color(0.4f, 0.9f, 0.4f));
            }
            else if (hazardType == HazardType.VoidAbyss && other.CompareTag("Player"))
            {
                CameraFollow.RequestShake(0.2f, 0.25f);
                GameEvents.RaiseCombatNotice("Vực Thẳm Hư Vô!", new Color(0.7f, 0.3f, 1f));
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                _targetsInside.Remove(damageable);
                _nextTickTimes.Remove(damageable);
            }

            if (hazardType == HazardType.ThornBramble && other.TryGetComponent<PlayerController>(out var player))
            {
                if (_slowedPlayers.Remove(player))
                {
                    player.SpeedMultiplier = 1.0f;
                }
            }
        }

        private void Update()
        {
            if (_targetsInside.Count == 0) return;

            float now = UnityEngine.Time.time;
            var toRemove = new List<IDamageable>();

            foreach (var target in _targetsInside)
            {
                if (target == null || target.IsDead)
                {
                    toRemove.Add(target);
                    continue;
                }

                if (_nextTickTimes.TryGetValue(target, out float nextTime) && now >= nextTime)
                {
                    _nextTickTimes[target] = now + tickInterval;
                    ApplyHazardDamage(target);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                _targetsInside.Remove(toRemove[i]);
                _nextTickTimes.Remove(toRemove[i]);
            }
        }

        private void ApplyHazardDamage(IDamageable target)
        {
            double dmg = damagePerSecond * tickInterval;
            var dmgInfo = new DamageInfo(dmg)
            {
                SourceId = "hazard." + hazardType.ToString().ToLowerInvariant(),
                Source = this
            };

            target.TakeDamage(dmgInfo);

            if (target is HealthComponent)
            {
                if (hazardType == HazardType.MagmaLava)
                {
                    CameraFollow.RequestShake(0.12f, 0.1f);
                    GameEvents.RaiseCombatNotice("Bỏng Lửa!", new Color(1f, 0.4f, 0.1f));
                }
            }
        }

        private void OnDisable()
        {
            foreach (var p in _slowedPlayers)
            {
                if (p != null) p.SpeedMultiplier = 1.0f;
            }
            _slowedPlayers.Clear();
            _targetsInside.Clear();
            _nextTickTimes.Clear();
        }
    }
}
