using System;
using System.Collections.Generic;
using Game.Core.Presentation;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>PlaySpeedState: pause thắng dừng hình, dừng hình luôn tự hết (không kẹt), ForceReset trả về 1.</summary>
    public class PlaySpeedStateTests
    {
        private List<float> _writes;
        private PlaySpeedState _state;

        [SetUp]
        public void SetUp()
        {
            _writes = new List<float>();
            _state = new PlaySpeedState(scale => _writes.Add(scale));
        }

        [Test]
        public void Constructor_RejectsNullApplier()
        {
            Assert.Throws<ArgumentNullException>(() => new PlaySpeedState(null));
        }

        [Test]
        public void Initially_ScaleIsOne()
        {
            _state.Tick(0.016f);
            Assert.AreEqual(1f, _state.CurrentScale);
            Assert.AreEqual(1f, _writes[_writes.Count - 1]);
        }

        [Test]
        public void HitStop_SlowsThenRestoresByRealTime()
        {
            _state.RequestHitStop(0.05f, 0.05f);
            Assert.AreEqual(0.05f, _state.CurrentScale, 1e-5f);
            Assert.IsTrue(_state.IsHitStopActive);

            _state.Tick(0.03f);
            Assert.AreEqual(0.05f, _state.CurrentScale, 1e-5f, "chưa hết 0.05s thật");

            _state.Tick(0.03f);
            Assert.AreEqual(1f, _state.CurrentScale, 1e-5f);
            Assert.IsFalse(_state.IsHitStopActive);
        }

        [Test]
        public void HitStop_NeverSticks_EvenWithHugeDelta()
        {
            _state.RequestHitStop(0.06f, 0.05f);
            _state.Tick(10f);
            Assert.AreEqual(1f, _state.CurrentScale);
            Assert.AreEqual(1f, _writes[_writes.Count - 1]);
        }

        [Test]
        public void Pause_SetsZero_AndWinsOverHitStop()
        {
            _state.RequestHitStop(0.06f, 0.05f);
            _state.PushPause();
            Assert.AreEqual(0f, _state.CurrentScale);
            Assert.IsTrue(_state.IsPaused);
            Assert.IsFalse(_state.IsHitStopActive, "đang pause thì không tính là đang dừng hình");
        }

        [Test]
        public void HitStopRequestedWhilePaused_IsIgnored()
        {
            _state.PushPause();
            _state.RequestHitStop(0.06f, 0.05f);
            _state.PopPause();
            Assert.AreEqual(1f, _state.CurrentScale, "không được còn dư dừng hình sau khi bỏ pause");
        }

        [Test]
        public void NestedPause_NeedsMatchingPops()
        {
            _state.PushPause();
            _state.PushPause();
            _state.PopPause();
            Assert.AreEqual(0f, _state.CurrentScale);
            Assert.AreEqual(1, _state.PauseDepth);

            _state.PopPause();
            Assert.AreEqual(1f, _state.CurrentScale);
        }

        [Test]
        public void ExtraPop_DoesNotGoNegative()
        {
            _state.PopPause();
            _state.PopPause();
            Assert.AreEqual(0, _state.PauseDepth);
            Assert.AreEqual(1f, _state.CurrentScale);
        }

        [Test]
        public void ForceReset_ClearsPauseAndHitStop()
        {
            _state.PushPause();
            _state.PushPause();
            _state.ForceReset();
            Assert.IsFalse(_state.IsPaused);
            Assert.AreEqual(1f, _state.CurrentScale);
            Assert.AreEqual(1f, _writes[_writes.Count - 1]);

            _state.RequestHitStop(0.06f, 0.05f);
            _state.ForceReset();
            Assert.IsFalse(_state.IsHitStopActive);
            Assert.AreEqual(1f, _state.CurrentScale);
        }

        [Test]
        public void NonPositiveSeconds_AreIgnored()
        {
            _state.RequestHitStop(0f, 0.05f);
            _state.RequestHitStop(-1f, 0.05f);
            Assert.IsFalse(_state.IsHitStopActive);
            Assert.AreEqual(1f, _state.CurrentScale);
        }

        [Test]
        public void OverlappingRequests_KeepLongerRemainingAndLowerScale()
        {
            _state.RequestHitStop(0.04f, 0.2f);
            _state.RequestHitStop(0.06f, 0.1f);
            Assert.AreEqual(0.1f, _state.CurrentScale, 1e-5f);

            _state.Tick(0.05f);
            Assert.IsTrue(_state.IsHitStopActive, "còn khoảng 0.01s");

            _state.Tick(0.02f);
            Assert.IsFalse(_state.IsHitStopActive);
            Assert.AreEqual(1f, _state.CurrentScale);
        }

        [Test]
        public void Applier_IsOnlyCalledWhenScaleChanges()
        {
            _state.RequestHitStop(0.06f, 0.05f);
            int afterRequest = _writes.Count;

            _state.Tick(0.01f);
            _state.Tick(0.01f);
            Assert.AreEqual(afterRequest, _writes.Count, "scale không đổi thì không ghi lại");

            _state.Tick(0.5f);
            Assert.AreEqual(afterRequest + 1, _writes.Count);
        }
    }
}
