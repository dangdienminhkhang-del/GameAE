using Game.UI.Pause;
using NUnit.Framework;

namespace Game.Tests.EditMode.UI
{
    [TestFixture]
    public class ExclusivePanelGroupTests
    {
        private class FakeExclusivePanel : IExclusivePanel
        {
            public int BackPriority { get; }
            public bool IsOpen { get; set; }
            public bool ClosedFromBackCalled { get; private set; }
            public bool CloseExclusiveCalled { get; private set; }

            public FakeExclusivePanel(int priority, bool isOpen = true)
            {
                BackPriority = priority;
                IsOpen = isOpen;
            }

            public void CloseFromBack()
            {
                ClosedFromBackCalled = true;
                IsOpen = false;
            }

            public void CloseExclusive()
            {
                CloseExclusiveCalled = true;
                IsOpen = false;
            }
        }

        private class FakeStandardBackHandler : IBackHandler
        {
            public int BackPriority { get; }
            public bool IsOpen { get; set; }
            public bool ClosedFromBackCalled { get; private set; }

            public FakeStandardBackHandler(int priority, bool isOpen = true)
            {
                BackPriority = priority;
                IsOpen = isOpen;
            }

            public void CloseFromBack()
            {
                ClosedFromBackCalled = true;
                IsOpen = false;
            }
        }

        [Test]
        public void OpeningPanel_ClosesOtherExclusivePanels()
        {
            var router = new BackKeyRouter();
            var inventory = new FakeExclusivePanel(30, isOpen: true);
            var quest = new FakeExclusivePanel(20, isOpen: true);
            var floor = new FakeExclusivePanel(10, isOpen: false);
            var character = new FakeExclusivePanel(25, isOpen: false);

            router.Register(inventory);
            router.Register(quest);
            router.Register(floor);
            router.Register(character);

            // Now character opens
            character.IsOpen = true;
            router.CloseExclusiveOthers(character);

            Assert.IsFalse(inventory.IsOpen, "Inventory should be closed.");
            Assert.IsTrue(inventory.CloseExclusiveCalled);

            Assert.IsFalse(quest.IsOpen, "Quest should be closed.");
            Assert.IsTrue(quest.CloseExclusiveCalled);

            Assert.IsFalse(floor.IsOpen, "Floor was already closed.");
            Assert.IsFalse(floor.CloseExclusiveCalled);

            Assert.IsTrue(character.IsOpen, "Opening panel stays open.");
            Assert.IsFalse(character.CloseExclusiveCalled);
        }

        [Test]
        public void OpeningExclusivePanel_DoesNotCloseSettingsOrPause()
        {
            var router = new BackKeyRouter();
            var settings = new FakeStandardBackHandler(40, isOpen: true);
            var inventory = new FakeExclusivePanel(30, isOpen: true);

            router.Register(settings);
            router.Register(inventory);

            var quest = new FakeExclusivePanel(20, isOpen: true);
            router.Register(quest);

            router.CloseExclusiveOthers(quest);

            Assert.IsTrue(settings.IsOpen, "Settings is above group and must not be closed by exclusive group.");
            Assert.IsFalse(inventory.IsOpen, "Inventory should be closed.");
        }

        [Test]
        public void BackKeyOrder_PreservesPriorities()
        {
            var router = new BackKeyRouter();
            var settings = new FakeStandardBackHandler(40, isOpen: true);
            var shop = new FakeExclusivePanel(35, isOpen: true);
            var inventory = new FakeExclusivePanel(30, isOpen: true);
            var character = new FakeExclusivePanel(25, isOpen: true);
            var quest = new FakeExclusivePanel(20, isOpen: true);
            var floor = new FakeExclusivePanel(10, isOpen: true);

            router.Register(settings);
            router.Register(shop);
            router.Register(inventory);
            router.Register(character);
            router.Register(quest);
            router.Register(floor);

            // Top open should be Settings (40)
            Assert.AreEqual(settings, router.TopOpen());

            // Handle Back closes Settings first
            BackAction act = router.Handle(deathScreenShowing: false, isPauseOpen: false, canPause: true);
            Assert.AreEqual(BackAction.CloseTopPanel, act);
            Assert.IsTrue(settings.ClosedFromBackCalled);

            // Next top open should be Shop (35)
            Assert.AreEqual(shop, router.TopOpen());
        }
    }
}
