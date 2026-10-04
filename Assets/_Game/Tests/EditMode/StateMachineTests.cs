using System;
using Game.Core.AI;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Core/AI/StateMachine — nền cho EnemyController ở Phase 4.</summary>
    public class StateMachineTests
    {
        [Test]
        public void ChangeTo_EntersStateAndTicksIt()
        {
            var machine = new StateMachine();
            var idle = new CountingState();
            machine.Register(idle);

            machine.ChangeTo<CountingState>();
            machine.Tick();
            machine.Tick();

            Assert.AreSame(idle, machine.Current);
            Assert.AreEqual(1, idle.Enters);
            Assert.AreEqual(2, idle.Ticks);
        }

        [Test]
        public void ChangeTo_ExitsPreviousState()
        {
            var machine = new StateMachine();
            var idle = new IdleState();
            var chase = new ChaseState();
            machine.Register(idle);
            machine.Register(chase);

            machine.ChangeTo<IdleState>();
            machine.ChangeTo<ChaseState>();

            Assert.AreEqual(1, idle.Exits);
            Assert.AreEqual(1, chase.Enters);
            Assert.AreEqual(typeof(ChaseState), machine.CurrentStateType);
        }

        [Test]
        public void ChangeTo_SameStateTwice_DoesNotReenter()
        {
            var machine = new StateMachine();
            var idle = new CountingState();
            machine.Register(idle);

            machine.ChangeTo<CountingState>();
            machine.ChangeTo<CountingState>();

            Assert.AreEqual(1, idle.Enters);
            Assert.AreEqual(0, idle.Exits);
        }

        [Test]
        public void ChangeTo_UnregisteredState_Throws()
        {
            var machine = new StateMachine();

            Assert.Throws<InvalidOperationException>(() => machine.ChangeTo<CountingState>());
        }

        [Test]
        public void Tick_WithoutState_DoesNothing()
        {
            var machine = new StateMachine();

            Assert.DoesNotThrow(() => machine.Tick());
            Assert.IsNull(machine.Current);
        }

        private class CountingState : StateBase
        {
            public int Enters;
            public int Exits;
            public int Ticks;

            public override void Enter() => Enters++;
            public override void Tick() => Ticks++;
            public override void Exit() => Exits++;
        }

        private class IdleState : StateBase
        {
            public int Enters;
            public int Exits;

            public override void Enter() => Enters++;
            public override void Tick() { }
            public override void Exit() => Exits++;
        }

        /// <summary>
        /// Lưu ý API: StateMachine.Register&lt;TState&gt; đăng ký theo KIỂU TĨNH của tham số,
        /// nên hai state phải là hai class khác nhau — không thể có 2 state cùng class.
        /// </summary>
        private class ChaseState : StateBase
        {
            public int Enters;

            public override void Enter() => Enters++;
            public override void Tick() { }
            public override void Exit() { }
        }
    }
}
