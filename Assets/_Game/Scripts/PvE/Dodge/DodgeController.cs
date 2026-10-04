using System;
using Game.Core.Combat;
using Game.Core.Skills;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Dodge
{
    /// <summary>
    /// Dodge là hành động chung, không phải Technique — không cần mastery (ADR-025, docs/07 §4.4).
    /// Không i-frames. Không xuyên tường: Rigidbody2D.Cast → MovePosition trong FixedUpdate.
    /// Wave 2A: từ chối khi TechniqueController đang chạy (cùng GameObject, GetComponent).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class DodgeController : MonoBehaviour
    {
        [Header("Refs (tự tìm nếu bỏ trống)")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private DodgeDefinition definition;

        [Header("Presentation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string dodgeTriggerName = "";

        [Header("Debug")]
        [SerializeField] private bool logDodge = false;

        private CooldownSystem _cooldown;
        private Rigidbody2D _rb;
        private Game.PvE.Skills.TechniqueController _techniqueController;

        private enum DodgeState { Idle, Dodging, Recovery }
        private DodgeState _state = DodgeState.Idle;
        private float _stateEnterTime;
        private Vector2 _dodgeDir;
        private float _dodgeSafeDistance;
        private float _dodgeTravelled;

        public event EventHandler OnDodgeStarted;
        public event EventHandler OnDodgeFinished;
        public event EventHandler OnDodgeBlocked;

        public bool IsDodging          => _state == DodgeState.Dodging || _state == DodgeState.Recovery;
        public bool IsReady            => _state == DodgeState.Idle && _cooldown != null && _cooldown.IsReady;
        public float CooldownRemaining => _cooldown != null ? _cooldown.Remaining : 0f;
        public float CooldownTotal     => definition != null ? definition.CooldownSeconds : 0f;

        private void Awake()
        {
            if (playerController == null) TryGetComponent(out playerController);
            TryGetComponent(out _rb);
            TryGetComponent(out _techniqueController);
            _cooldown = new CooldownSystem(this);
            if (definition == null) definition = DodgeDefinition.CreateDefault();
        }

        /// <summary>UI button gọi — né theo hướng mặt hiện tại. Trả true nếu bắt đầu né.</summary>
        public bool TryDodge()
        {
            if (!IsReady)                    { OnDodgeBlocked?.Invoke(this, EventArgs.Empty); return false; }
            if (definition == null)          { OnDodgeBlocked?.Invoke(this, EventArgs.Empty); return false; }

            // Từ chối khi Technique đang chạy
            if (_techniqueController != null &&
                _techniqueController.State != Game.Core.Skills.TechniqueExecutionState.Idle)
            {
                OnDodgeBlocked?.Invoke(this, EventArgs.Empty);
                return false;
            }

            int facing     = playerController != null ? playerController.Facing : 1;
            _dodgeDir      = new Vector2(facing, 0f);
            float wantDist = definition.DashDistance;

            // Cắt khoảng cách tới vật cản gần nhất bằng Rigidbody2D.Cast
            _dodgeSafeDistance = wantDist;
            if (_rb != null)
            {
                var hits = new RaycastHit2D[4];
                var filter = new ContactFilter2D { useTriggers = false };
                int hitCount = _rb.Cast(_dodgeDir, filter, hits, wantDist);
                if (hitCount > 0 && hits[0].collider != null)
                    _dodgeSafeDistance = UnityEngine.Mathf.Max(0f, hits[0].distance - 0.05f);
            }
            _dodgeTravelled = 0f;

            _state = DodgeState.Dodging;
            _stateEnterTime = UnityEngine.Time.time;
            _cooldown.StartCooldown(definition.CooldownSeconds);

            OnDodgeStarted?.Invoke(this, EventArgs.Empty);
            if (animator != null && !string.IsNullOrEmpty(dodgeTriggerName))
                animator.SetTrigger(dodgeTriggerName);

            if (logDodge) Debug.Log($"[DodgeController] Né {_dodgeSafeDistance:F2}u dir={facing}");
            return true;
        }

        // ── Update ─────────────────────────────────────────────────

        private void Update()
        {
            switch (_state)
            {
                case DodgeState.Dodging:  TickDodging();  break;
                case DodgeState.Recovery: TickRecovery(); break;
            }
        }

        // Di chuyển thực tế trong FixedUpdate để Rigidbody2D xử lý collision đúng
        private void FixedUpdate()
        {
            if (_state != DodgeState.Dodging || _rb == null) return;
            if (definition == null) { EndIdle(); return; }

            float duration = Mathf.Max(0.001f, definition.DodgeDurationSeconds);
            float elapsed  = UnityEngine.Time.time - _stateEnterTime;
            float t        = Mathf.Clamp01(elapsed / duration);

            float targetTravelled = _dodgeSafeDistance * t;
            float step = targetTravelled - _dodgeTravelled;
            if (step > 0.0001f)
            {
                _dodgeTravelled = targetTravelled;
                _rb.MovePosition(_rb.position + _dodgeDir * step);
            }
        }

        private void TickDodging()
        {
            if (definition == null) { EndIdle(); return; }
            if (UnityEngine.Time.time - _stateEnterTime >= Mathf.Max(0.001f, definition.DodgeDurationSeconds))
            {
                _state = DodgeState.Recovery;
                _stateEnterTime = UnityEngine.Time.time;
                if (logDodge) Debug.Log($"[DodgeController] Recovery {definition.RecoverySeconds:F2}s");
            }
        }

        private void TickRecovery()
        {
            if (definition == null) { EndIdle(); return; }
            if (UnityEngine.Time.time - _stateEnterTime >= definition.RecoverySeconds)
            {
                EndIdle();
                OnDodgeFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        private void EndIdle() => _state = DodgeState.Idle;
    }
}
