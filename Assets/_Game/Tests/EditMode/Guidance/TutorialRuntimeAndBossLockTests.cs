using Game.Services.Guidance;
using Game.UI.Boss;
using Game.UI.Guidance;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Điểm nối Settings ↔ hướng dẫn (Phase 9.9): công tắc "Gợi ý hướng dẫn" có hiệu lực ngay và "Xem lại hướng dẫn" CHỈ xóa cờ gợi ý
    /// local của người chơi hiện tại (không chạm khóa nào khác như settings.audio.*, không chạm người chơi khác, không đổi công tắc).
    /// TutorialRuntime là static: test luôn trả về mặc định và gỡ người nghe.
    /// </summary>
    public class TutorialRuntimeSettingsTests
    {
        private MemoryTutorialBackend _backend;
        private PlayerPrefsTutorialStore _store;
        private int _enabledChanged;
        private int _seenReset;

        private void OnEnabledChanged() => _enabledChanged++;
        private void OnSeenReset() => _seenReset++;

        [SetUp]
        public void SetUp()
        {
            _backend = new MemoryTutorialBackend();
            _store = new PlayerPrefsTutorialStore(_backend);
            _enabledChanged = 0;
            _seenReset = 0;

            TutorialRuntime.UseStore(_store);
            TutorialRuntime.UsePlayerIdProvider(() => "p1");
            TutorialRuntime.EnabledChanged += OnEnabledChanged;
            TutorialRuntime.SeenReset += OnSeenReset;
        }

        [TearDown]
        public void TearDown()
        {
            TutorialRuntime.EnabledChanged -= OnEnabledChanged;
            TutorialRuntime.SeenReset -= OnSeenReset;
            TutorialRuntime.RegisterKnownHintIds(null);
            TutorialRuntime.UsePlayerIdProvider(null);
            TutorialRuntime.UseStore(null);
        }

        // ── Công tắc ─────────────────────────────────────────────────────────

        [Test]
        public void Switch_DefaultsToOn()
        {
            Assert.IsTrue(TutorialRuntime.Enabled);
        }

        [Test]
        public void Switch_TakesEffectImmediately_AndNotifiesListeners()
        {
            TutorialRuntime.SetEnabled(false);

            Assert.IsFalse(TutorialRuntime.Enabled, "đọc lại ngay thấy đã tắt");
            Assert.AreEqual(1, _enabledChanged, "router nghe sự kiện này để ẩn toast đang hiện");

            TutorialRuntime.SetEnabled(true);
            Assert.IsTrue(TutorialRuntime.Enabled);
            Assert.AreEqual(2, _enabledChanged);
        }

        [Test]
        public void Switch_IsStoredLocally_UnderItsOwnKey()
        {
            TutorialRuntime.SetEnabled(false);
            Assert.AreEqual(0, _backend.Ints[PlayerPrefsTutorialStore.EnabledKey]);
        }

        // ── Xem lại hướng dẫn ────────────────────────────────────────────────

        [Test]
        public void ReplayTutorial_ClearsOnlyTheCurrentPlayersTutorialFlags()
        {
            _store.MarkSeen("p1", "hint.move");
            _store.MarkSeen("p1", "hint.attack");
            _store.MarkSeen("p2", "hint.move");
            _backend.SetInt("settings.audio.music_volume", 7);
            _backend.SetInt("account.keep_me", 1);

            TutorialRuntime.ResetSeen();

            Assert.IsFalse(_store.HasSeen("p1", "hint.move"));
            Assert.IsFalse(_store.HasSeen("p1", "hint.attack"));
            Assert.IsTrue(_store.HasSeen("p2", "hint.move"), "người chơi khác không bị ảnh hưởng");
            Assert.AreEqual(7, _backend.Ints["settings.audio.music_volume"], "không xóa khóa âm thanh");
            Assert.AreEqual(1, _backend.Ints["account.keep_me"], "không xóa khóa không liên quan");
            Assert.AreEqual(1, _seenReset);
        }

        [Test]
        public void ReplayTutorial_DoesNotChangeTheSwitch()
        {
            TutorialRuntime.SetEnabled(false);
            TutorialRuntime.ResetSeen();
            Assert.IsFalse(TutorialRuntime.Enabled);
        }

        [Test]
        public void ReplayTutorial_UsesRegisteredHintIds_EvenIfTheIndexWasLost()
        {
            _store.MarkSeen("p1", "hint.move");
            _backend.Delete(PlayerPrefsTutorialStore.IndexKey("p1"));
            TutorialRuntime.RegisterKnownHintIds(new[] { "hint.move" });

            TutorialRuntime.ResetSeen();

            Assert.IsFalse(new PlayerPrefsTutorialStore(_backend).HasSeen("p1", "hint.move"));
        }

        [Test]
        public void ReplayTutorial_WithoutAPlayerId_DoesNotThrow_AndClearsNothing()
        {
            _store.MarkSeen("p1", "hint.move");
            TutorialRuntime.UsePlayerIdProvider(() => null);

            Assert.DoesNotThrow(() => TutorialRuntime.ResetSeen());

            Assert.IsTrue(_store.HasSeen("p1", "hint.move"));
        }

        [Test]
        public void ReplayTutorial_WhenThePlayerIdProviderThrows_DoesNotThrow()
        {
            TutorialRuntime.UsePlayerIdProvider(() => throw new System.InvalidOperationException("chưa đăng nhập"));

            Assert.IsNull(TutorialRuntime.CurrentPlayerId);
            Assert.DoesNotThrow(() => TutorialRuntime.ResetSeen());
        }

        [Test]
        public void ReplayTutorial_ThenTheHintsCanBeSeenAgain()
        {
            _store.MarkSeen("p1", "hint.move");
            TutorialRuntime.ResetSeen();
            Assert.IsFalse(_store.HasSeen("p1", "hint.move"), "người chơi sẽ thấy lại gợi ý");
        }
    }

    /// <summary>Khóa chữ bảng báo boss P8-5: gói hướng dẫn tuyệt đối không đổi chuỗi này.</summary>
    public class BossReportPanelStringLockTests
    {
        [Test]
        public void ConfirmedMessage_IsLocked()
        {
            Assert.AreEqual("Máy chủ đã ghi nhận. Tầng kế đã mở.", BossDefeatReportPanelModel.ConfirmedMessage);
        }

        [Test]
        public void OtherPanelStrings_AreLockedToo()
        {
            Assert.AreEqual("Boss đã gục nhưng chưa báo được lên máy chủ. Hãy kết nối lại, rồi bấm Thử lại.", BossDefeatReportPanelModel.FailedMessage);
            Assert.AreEqual("Thử lại", BossDefeatReportPanelModel.RetryLabel);
            Assert.AreEqual("Đang gửi...", BossDefeatReportPanelModel.SendingLabel);
            Assert.AreEqual("Đóng", BossDefeatReportPanelModel.CloseLabel);
        }

        [Test]
        public void ReadingTheState_DoesNotChangeTheFlow_HiddenToFailedToSendingToConfirmedToHidden()
        {
            // Gói hướng dẫn chỉ ĐỌC trạng thái này (đã đóng / đã xác nhận). Đường đi của máy trạng thái không được đổi.
            var model = new BossDefeatReportPanelModel();
            Assert.AreEqual(BossDefeatReportPanelState.Hidden, model.State);

            model.ReportFailed();
            Assert.AreEqual(BossDefeatReportPanelState.Failed, model.State);
            Assert.IsTrue(model.IsPanelVisible);

            Assert.IsTrue(model.TryBeginRetry());
            Assert.AreEqual(BossDefeatReportPanelState.Sending, model.State);

            model.ReportConfirmed();
            Assert.AreEqual(BossDefeatReportPanelState.Confirmed, model.State);
            Assert.IsFalse(model.IsPanelVisible, "bảng đã đóng khi đã xác nhận (điều kiện của hint.floor_select)");

            model.Tick(BossDefeatReportPanelModel.ConfirmedToastSeconds);
            Assert.AreEqual(BossDefeatReportPanelState.Hidden, model.State);
        }
    }
}
