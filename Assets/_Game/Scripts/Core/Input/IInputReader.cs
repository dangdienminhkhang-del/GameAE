using System;
using UnityEngine;

namespace Game.Core.Input
{
    public interface IInputReader
    {
        event EventHandler<Vector2> OnMove;
        event EventHandler OnAttackStarted;
        event EventHandler OnAttackStopped;

        void SetMoveInput(Vector2 value);
        void SetAttackInput(bool isPressed);
    }
}
