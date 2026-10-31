using System;
using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Events;
using Game.PvE.Boss;
using Game.PvE.Enemy;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Combat
{
    /// <summary>
    /// Quản lý runtime các hiệu ứng trạng thái (Độc, Chảy Máu, Tê Liệt, Choáng) chuẩn SAO.
    /// Tự động gắn runtime lên Player, Enemy, và Boss.
    /// Xử lý sát thương DoT định kỳ (qua DamageInfo.IsPeriodic để không cướp Last Attack Bonus),
    /// khóa hành động khi bị Tê Liệt / Choáng, và hiển thị tint màu trạng thái.
    /// </summary>
    public class StatusEffectController : MonoBehaviour
    {
        private readonly List<StatusEffectInstance> _activeEffects = new List<StatusEffectInstance>();
        private readonly List<StatusEffectTickResult> _tickResultsBuffer = new List<StatusEffectTickResult>();

        private IDamageable _damageable;
        private PlayerController _playerController;
        private EnemyController _enemyController;
        private BossController _bossController;
        private SpriteRenderer _spriteRenderer;
        private Color _baseColor = Color.white;
        private bool _colorCaptured;

        public IReadOnlyList<StatusEffectInstance> ActiveEffects => _activeEffects;
        public event Action<StatusEffectType, bool> StatusEffectChanged;

        private void Awake()
        {
            _damageable = GetComponent<IDamageable>();
            TryGetComponent(out _playerController);
            TryGetComponent(out _enemyController);
            TryGetComponent(out _bossController);

            CaptureRenderer();
        }

        private void CaptureRenderer()
        {
            if (_colorCaptured) return;
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _baseColor = _spriteRenderer.color;
                _colorCaptured = true;
            }
        }

        /// <summary>
        /// Áp dụng một hiệu ứng trạng thái lên thực thể.
        /// </summary>
        public bool ApplyEffect(
            StatusEffectType type,
            float duration,
            double damagePerTick = 0.0,
            float tickInterval = 1.0f,
            string sourceId = null,
            object source = null)
        {
            if (type == StatusEffectType.None || duration <= 0f) return false;

            // Boss có sức kháng Choáng tự nhiên (giảm thời gian choáng 60%)
            if (_bossController != null && type == StatusEffectType.Stun)
            {
                duration *= StatusEffectRules.BossStunDurationMultiplier;
            }

            bool isNew = StatusEffectRules.ApplyEffect(
                _activeEffects, type, duration, damagePerTick, tickInterval, sourceId, source);

            // Báo thông báo nổi cho Player nếu là người chơi bị dính hiệu ứng
            if (_playerController != null && isNew)
            {
                RaisePlayerNotice(type);
            }

            SyncActionLock();
            UpdateVisualTint();
            StatusEffectChanged?.Invoke(type, true);
            return isNew;
        }

        public bool ApplyPoison(float duration, double damagePerTick, string sourceId = null, object source = null)
        {
            return ApplyEffect(StatusEffectType.Poison, duration, damagePerTick, StatusEffectRules.DefaultPoisonTickInterval, sourceId, source);
        }

        public bool ApplyBleed(float duration, double damagePerTick, string sourceId = null, object source = null)
        {
            return ApplyEffect(StatusEffectType.Bleed, duration, damagePerTick, StatusEffectRules.DefaultBleedTickInterval, sourceId, source);
        }

        public bool ApplyParalyze(float duration, string sourceId = null, object source = null)
        {
            return ApplyEffect(StatusEffectType.Paralyze, duration, 0.0, 1.0f, sourceId, source);
        }

        public bool ApplyStun(float duration, string sourceId = null, object source = null)
        {
            return ApplyEffect(StatusEffectType.Stun, duration, 0.0, 1.0f, sourceId, source);
        }

        /// <summary>
        /// Hóa giải hiệu ứng trạng thái cụ thể hoặc toàn bộ nếu type == None (dùng cho bình Antidote).
        /// </summary>
        public int Cure(StatusEffectType type = StatusEffectType.None)
        {
            int cured = StatusEffectRules.Cure(_activeEffects, type);
            if (cured > 0)
            {
                SyncActionLock();
                UpdateVisualTint();
                StatusEffectChanged?.Invoke(type, false);
            }
            return cured;
        }

        public bool HasEffect(StatusEffectType type)
        {
            return StatusEffectRules.HasEffect(_activeEffects, type);
        }

        private void Update()
        {
            if (_activeEffects.Count == 0) return;

            _tickResultsBuffer.Clear();
            int countBefore = _activeEffects.Count;

            StatusEffectRules.Tick(_activeEffects, Time.deltaTime, _tickResultsBuffer);

            // Xử lý sát thương theo chu kỳ (DoT)
            if (_tickResultsBuffer.Count > 0 && _damageable != null && !_damageable.IsDead)
            {
                for (int i = 0; i < _tickResultsBuffer.Count; i++)
                {
                    var tick = _tickResultsBuffer[i];
                    var dmg = new DamageInfo(tick.Damage)
                    {
                        IsPeriodic = true, // Cờ quan trọng: DoT không cướp Last Attack Bonus
                        SourceId = tick.SourceId,
                        Source = tick.Source
                    };
                    _damageable.TakeDamage(dmg);
                }
            }

            // Nếu số lượng hiệu ứng thay đổi (hết hạn), cập nhật trạng thái
            if (_activeEffects.Count != countBefore)
            {
                SyncActionLock();
                UpdateVisualTint();
            }
        }

        private void SyncActionLock()
        {
            bool locked = StatusEffectRules.HasActionLock(_activeEffects);

            if (_playerController == null) TryGetComponent(out _playerController);
            if (_playerController != null)
            {
                // Chỉ set ActionLocked nếu không có hiệu ứng khác (hoặc bật nếu có)
                if (locked) _playerController.IsActionLocked = true;
                else
                {
                    // Nếu không còn hiệu ứng gây khóa, kiểm tra xem có đang bị khựng chiêu hay không
                    if (TryGetComponent<Skills.PostSkillDelayController>(out var delayCtrl))
                    {
                        if (!delayCtrl.IsFrozen) _playerController.IsActionLocked = false;
                    }
                    else
                    {
                        _playerController.IsActionLocked = false;
                    }
                }
            }

            if (_enemyController != null)
            {
                _enemyController.IsActionLocked = locked;
            }

            if (_bossController != null)
            {
                _bossController.IsActionLocked = locked;
            }
        }

        private void UpdateVisualTint()
        {
            CaptureRenderer();
            if (_spriteRenderer == null) return;

            if (HasEffect(StatusEffectType.Paralyze))
            {
                _spriteRenderer.color = new Color(1f, 1f, 0.45f, 1f); // Vàng điện tê liệt
            }
            else if (HasEffect(StatusEffectType.Stun))
            {
                _spriteRenderer.color = new Color(0.85f, 0.85f, 0.85f, 1f); // Xám choáng
            }
            else if (HasEffect(StatusEffectType.Poison))
            {
                _spriteRenderer.color = new Color(0.55f, 1f, 0.55f, 1f); // Xanh lá độc
            }
            else if (HasEffect(StatusEffectType.Bleed))
            {
                _spriteRenderer.color = new Color(1f, 0.45f, 0.45f, 1f); // Đỏ sẫm chảy máu
            }
            else
            {
                _spriteRenderer.color = _baseColor;
            }
        }

        private void RaisePlayerNotice(StatusEffectType type)
        {
            switch (type)
            {
                case StatusEffectType.Poison:
                    GameEvents.RaiseCombatNotice("☣ BỊ NHIỄM ĐỘC! ☣", new Color(0.4f, 1f, 0.4f));
                    break;
                case StatusEffectType.Bleed:
                    GameEvents.RaiseCombatNotice("🩸 BỊ CHẢY MÁU! 🩸", new Color(1f, 0.3f, 0.3f));
                    break;
                case StatusEffectType.Paralyze:
                    GameEvents.RaiseCombatNotice("⚡ BỊ TÊ LIỆT! ⚡", new Color(1f, 1f, 0.3f));
                    break;
                case StatusEffectType.Stun:
                    GameEvents.RaiseCombatNotice("💫 BỊ CHOÁNG! 💫", new Color(1f, 0.85f, 0.4f));
                    break;
            }
        }

        private void OnDisable()
        {
            _activeEffects.Clear();
            SyncActionLock();
            UpdateVisualTint();
        }
    }
}
