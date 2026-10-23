using System.Collections.Generic;
using Game.Core.Presentation;
using Game.UI.Pause;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class PauseRulesTests
    {
        [Test]
        public void CanPause_OnlyWhenNotDeadAndNotLoading()
        {
            Assert.IsTrue(PauseRules.CanPause(false, false));
            Assert.IsFalse(PauseRules.CanPause(true, false));
            Assert.IsFalse(PauseRules.CanPause(false, true));
            Assert.IsFalse(PauseRules.CanPause(true, true));
        }

        [Test]
        public void Gate_DeathFlowIsLatched_NeverAllowsPauseAgain()
        {
            var gate = new PauseGate();
            Assert.IsTrue(gate.CanPause);
            gate.MarkDeathFlow();
            Assert.IsFalse(gate.CanPause);
            gate.IsLoading = false;
            Assert.IsFalse(gate.CanPause, "đã chết thì không bao giờ pause lại trong scene này");
        }

        [Test]
        public void Gate_LoadingBlocksOnlyWhileLoading()
        {
            var gate = new PauseGate { IsLoading = true };
            Assert.IsFalse(gate.CanPause);
            gate.IsLoading = false;
            Assert.IsTrue(gate.CanPause);
        }

        [Test]
        public void Gate_ResetClearsEverything()
        {
            var gate = new PauseGate();
            gate.MarkDeathFlow();
            gate.IsLoading = true;
            gate.Reset();
            Assert.IsTrue(gate.CanPause);
        }
    }

    public class MainMenuQuitGateTests
    {
        [Test]
        public void InCombat_QuitIsBlockedWithLabel()
        {
            Assert.IsFalse(MainMenuQuitGate.CanQuit(true));
            Assert.AreEqual(MainMenuQuitGate.LockedLabel, MainMenuQuitGate.LabelFor(true));
            StringAssert.Contains("giao chiến", MainMenuQuitGate.LockedLabel);
        }

        [Test]
        public void OutOfCombat_QuitAllowedNoLabel()
        {
            Assert.IsTrue(MainMenuQuitGate.CanQuit(false));
            Assert.AreEqual(string.Empty, MainMenuQuitGate.LabelFor(false));
        }
    }

    public class BackKeyRouterTests
    {
        private sealed class FakePanel : IBackHandler
        {
            public int BackPriority { get; set; }
            public bool IsOpen { get; set; }
            public int CloseCalls;
            public void CloseFromBack() { CloseCalls++; IsOpen = false; }
        }

        [Test]
        public void DeathScreen_BlocksEverything_AndNeverExits()
        {
            var router = new BackKeyRouter();
            var panel = new FakePanel { BackPriority = 30, IsOpen = true };
            router.Register(panel);
            Assert.AreEqual(BackAction.Consumed, router.Handle(true, false, true));
            Assert.AreEqual(0, panel.CloseCalls);
            Assert.AreEqual(BackAction.Consumed, BackKeyRouter.Decide(true, false, false, true));
        }

        [Test]
        public void ClosesTopmostPanelFirst_ByPriority()
        {
            var router = new BackKeyRouter();
            var floor = new FakePanel { BackPriority = 10, IsOpen = true };
            var inventory = new FakePanel { BackPriority = 30, IsOpen = true };
            var settings = new FakePanel { BackPriority = 40, IsOpen = true };
            router.Register(floor);
            router.Register(inventory);
            router.Register(settings);

            Assert.AreEqual(BackAction.CloseTopPanel, router.Handle(false, true, true));
            Assert.AreEqual(1, settings.CloseCalls);
            Assert.AreEqual(0, inventory.CloseCalls);

            Assert.AreEqual(BackAction.CloseTopPanel, router.Handle(false, true, true));
            Assert.AreEqual(1, inventory.CloseCalls);
            Assert.AreEqual(BackAction.CloseTopPanel, router.Handle(false, true, true));
            Assert.AreEqual(1, floor.CloseCalls);
        }

        [Test]
        public void NoPanel_PauseOpen_ResumesGame()
        {
            Assert.AreEqual(BackAction.ClosePause, BackKeyRouter.Decide(false, false, true, true));
        }

        [Test]
        public void NoPanel_NotPaused_OpensPauseWhenAllowed()
        {
            Assert.AreEqual(BackAction.OpenPause, BackKeyRouter.Decide(false, false, false, true));
        }

        [Test]
        public void NoPanel_NotPaused_CannotPause_ConsumesKey()
        {
            Assert.AreEqual(BackAction.Consumed, BackKeyRouter.Decide(false, false, false, false));
        }

        [Test]
        public void NoOutcomeExitsTheApp()
        {
            // Mọi tổ hợp đầu vào đều ra một trong bốn hành động, không có "thoát app".
            foreach (bool death in new[] { false, true })
            foreach (bool panel in new[] { false, true })
            foreach (bool paused in new[] { false, true })
            foreach (bool can in new[] { false, true })
            {
                BackAction a = BackKeyRouter.Decide(death, panel, paused, can);
                Assert.That(System.Enum.IsDefined(typeof(BackAction), a));
            }
            Assert.AreEqual(4, System.Enum.GetValues(typeof(BackAction)).Length);
        }

        [Test]
        public void ClosedPanelsAndUnregistered_AreIgnored()
        {
            var router = new BackKeyRouter();
            var closed = new FakePanel { BackPriority = 50, IsOpen = false };
            var open = new FakePanel { BackPriority = 10, IsOpen = true };
            router.Register(closed);
            router.Register(open);
            Assert.AreSame(open, router.TopOpen());

            router.Unregister(open);
            Assert.IsNull(router.TopOpen());
            Assert.AreEqual(BackAction.OpenPause, router.Handle(false, false, true));
        }

        [Test]
        public void Register_DoesNotDuplicate()
        {
            var router = new BackKeyRouter();
            var p = new FakePanel { IsOpen = true };
            router.Register(p);
            router.Register(p);
            Assert.AreEqual(1, router.HandlerCount);
        }
    }

    public class PauseTimeScaleTests
    {
        [Test]
        public void NestedPause_ReturnsToOneOnlyAfterAllPops()
        {
            var applied = new List<float>();
            var state = new PlaySpeedState(applied.Add);

            state.PushPause();
            state.PushPause();
            Assert.AreEqual(0f, state.CurrentScale);
            state.PopPause();
            Assert.AreEqual(0f, state.CurrentScale, "còn một lớp pause");
            state.PopPause();
            Assert.AreEqual(1f, state.CurrentScale);
            Assert.AreEqual(1f, applied[applied.Count - 1]);
        }

        [Test]
        public void ExtraPop_NeverGoesNegative()
        {
            var state = new PlaySpeedState(_ => { });
            state.PopPause();
            state.PopPause();
            Assert.AreEqual(0, state.PauseDepth);
            Assert.AreEqual(1f, state.CurrentScale);
        }

        [Test]
        public void ForceReset_AsWhenLeavingScene_RestoresSpeedOne()
        {
            float last = -1f;
            var state = new PlaySpeedState(v => last = v);
            state.PushPause();
            state.PushPause();
            state.ForceReset();
            Assert.AreEqual(1f, last);
            Assert.IsFalse(state.IsPaused);
        }
    }
}
