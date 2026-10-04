using System;
using Game.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PvE.Player
{
    public class PlayerInputReader : MonoBehaviour, IInputReader
    {
        public event EventHandler<Vector2> OnMove;
        public event EventHandler OnAttackStarted;
        public event EventHandler OnAttackStopped;

        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference attackAction;

        private Vector2 _lastMoveInput;

        private void Awake()
        {
            _lastMoveInput = Vector2.zero;
        }

        private void OnEnable()
        {
            if (moveAction != null)
            {
                moveAction.action.performed += HandleMovePerformed;
                moveAction.action.canceled += HandleMoveCanceled;
                moveAction.action.Enable();
            }
            if (attackAction != null)
            {
                attackAction.action.performed += HandleAttackPerformed;
                attackAction.action.canceled += HandleAttackCanceled;
                attackAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.action.performed -= HandleMovePerformed;
                moveAction.action.canceled -= HandleMoveCanceled;
                moveAction.action.Disable();
            }
            if (attackAction != null)
            {
                attackAction.action.performed -= HandleAttackPerformed;
                attackAction.action.canceled -= HandleAttackCanceled;
                attackAction.action.Disable();
            }
        }

        private void HandleMovePerformed(InputAction.CallbackContext ctx)
        {
            Vector2 v = ctx.ReadValue<Vector2>();
            SetMoveInput(v);
        }

        private void HandleMoveCanceled(InputAction.CallbackContext ctx)
        {
            SetMoveInput(Vector2.zero);
        }

        private void HandleAttackPerformed(InputAction.CallbackContext ctx)
        {
            SetAttackInput(true);
        }

        private void HandleAttackCanceled(InputAction.CallbackContext ctx)
        {
            SetAttackInput(false);
        }

        public void SetMoveInput(Vector2 value)
        {
            _lastMoveInput = value;
            OnMove?.Invoke(this, value);
        }

        public void SetAttackInput(bool isPressed)
        {
            if (isPressed)
                OnAttackStarted?.Invoke(this, EventArgs.Empty);
            else
                OnAttackStopped?.Invoke(this, EventArgs.Empty);
        }
    }
}
