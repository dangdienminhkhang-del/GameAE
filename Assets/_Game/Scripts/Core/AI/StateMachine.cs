using System;
using System.Collections.Generic;

namespace Game.Core.AI
{
    public interface IState
    {
        void Enter();
        void Tick();
        void Exit();
    }

    public abstract class StateBase : IState
    {
        public abstract void Enter();
        public abstract void Tick();
        public abstract void Exit();
    }

    public class StateMachine
    {
        private readonly Dictionary<Type, IState> _states = new Dictionary<Type, IState>();
        public IState Current { get; private set; }
        public Type CurrentStateType => Current?.GetType();

        public void Register<TState>(TState state) where TState : class, IState
        {
            _states[typeof(TState)] = state;
        }

        public void ChangeTo<TState>() where TState : class, IState
        {
            var type = typeof(TState);
            if (!_states.TryGetValue(type, out var next))
            {
                throw new InvalidOperationException($"[StateMachine] State {type.Name} not registered");
            }
            if (Current == next) return;
            Current?.Exit();
            Current = next;
            Current.Enter();
        }

        public void Tick()
        {
            Current?.Tick();
        }
    }
}
