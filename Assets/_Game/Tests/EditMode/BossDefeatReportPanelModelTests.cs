using Game.UI.Boss;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task P8-5: máy trạng thái THUẦN của bảng "Thử lại báo boss" (Hidden → Failed → Sending → Confirmed).
    /// Không cần scene, Canvas hay BossController: model chỉ nhận tín hiệu và cho biết nên hiện gì.
    /// </summary>
    public class BossDefeatReportPanelModelTests
    {
        private BossDefeatReportPanelModel _model;
        private int _changedCount;

        [SetUp]
        public void SetUp()
        {
            _model = new BossDefeatReportPanelModel();
            _changedCount = 0;
            _model.Changed += () => _changedCount++;
        }

        // ── Trạng thái ban đầu ──────────────────────────────────────────────────

        [Test]
        public void Initial_IsHidden_NothingVisible()
        {
            Assert.AreEqual(BossDefeatReportPanelState.Hidden, _model.State);
            Assert.IsFalse(_model.IsPanelVisible);
            Assert.IsFalse(_model.IsConfirmedToastVisible);
            Assert.IsFalse(_model.IsRetryInteractable);
            Assert.IsFalse(_model.IsRetryInFlight);
        }

        // ── Hidden → Failed ─────────────────────────────────────────────────────

        [Test]
        public void ReportFailed_ShowsPanel_WithRetryEnabled()
        {
            _model.ReportFailed();

            Assert.AreEqual(BossDefeatReportPanelState.Failed, _model.State);
            Assert.IsTrue(_model.IsPanelVisible);
            Assert.IsFalse(_model.IsConfirmedToastVisible);
            Assert.IsTrue(_model.IsRetryInteractable);
            Assert.AreEqual(BossDefeatReportPanelModel.RetryLabel, _model.RetryButtonText);
        }

        // ── Failed → Sending (chặn bấm đúp) ─────────────────────────────────────

        [Test]
        public void TryBeginRetry_FromFailed_GoesToSending_LocksButton_ShowsSendingLabel()
        {
            _model.ReportFailed();

            Assert.IsTrue(_model.TryBeginRetry());

            Assert.AreEqual(BossDefeatReportPanelState.Sending, _model.State);
            Assert.IsTrue(_model.IsPanelVisible, "Đang gửi vẫn hiện bảng");
            Assert.IsFalse(_model.IsRetryInteractable, "Đang gửi thì khóa nút Thử lại");
            Assert.AreEqual("Đang gửi...", _model.RetryButtonText);
            Assert.IsTrue(_model.IsRetryInFlight);
        }

        [Test]
        public void TryBeginRetry_DoubleTap_SecondPressIsIgnored()
        {
            _model.ReportFailed();

            Assert.IsTrue(_model.TryBeginRetry(), "Lần bấm đầu đi tiếp");
            Assert.IsFalse(_model.TryBeginRetry(), "Lần bấm thứ hai (bấm đúp) phải bị chặn");
            Assert.AreEqual(BossDefeatReportPanelState.Sending, _model.State);
        }

        [Test]
        public void TryBeginRetry_WhenHidden_IsIgnored()
        {
            Assert.IsFalse(_model.TryBeginRetry());
            Assert.AreEqual(BossDefeatReportPanelState.Hidden, _model.State);
            Assert.IsFalse(_model.IsRetryInFlight);
        }

        [Test]
        public void TryBeginRetry_WhenConfirmed_IsIgnored()
        {
            _model.ReportConfirmed();

            Assert.IsFalse(_model.TryBeginRetry());
            Assert.AreEqual(BossDefeatReportPanelState.Confirmed, _model.State);
        }

        // ── Sending → Confirmed (rồi tự ẩn sau 3 giây) ──────────────────────────

        [Test]
        public void Confirmed_AfterRetry_HidesPanel_ShowsToast()
        {
            _model.ReportFailed();
            _model.TryBeginRetry();

            _model.ReportConfirmed();

            Assert.AreEqual(BossDefeatReportPanelState.Confirmed, _model.State);
            Assert.IsFalse(_model.IsPanelVisible, "Xác nhận xong thì ẩn bảng");
            Assert.IsTrue(_model.IsConfirmedToastVisible, "Hiện dòng 'Máy chủ đã ghi nhận'");
            Assert.IsFalse(_model.IsRetryInFlight);
        }

        [Test]
        public void Confirmed_ToastLastsThreeSeconds_ThenHides()
        {
            Assert.AreEqual(3f, BossDefeatReportPanelModel.ConfirmedToastSeconds, 0.0001f);

            _model.ReportConfirmed();

            _model.Tick(1.0f);
            _model.Tick(1.9f);
            Assert.AreEqual(BossDefeatReportPanelState.Confirmed, _model.State, "Mới 2.9 giây: vẫn hiện");
            Assert.IsTrue(_model.IsConfirmedToastVisible);

            _model.Tick(0.1f);
            Assert.AreEqual(BossDefeatReportPanelState.Hidden, _model.State, "Đủ 3 giây: tự ẩn");
            Assert.IsFalse(_model.IsConfirmedToastVisible);
        }

        [Test]
        public void Confirmed_WithoutPriorFailure_AlsoShowsToast()
        {
            // Lần báo đầu tiên thành công ngay (không qua Failed) cũng phát DefeatReportConfirmed.
            _model.ReportConfirmed();

            Assert.AreEqual(BossDefeatReportPanelState.Confirmed, _model.State);
            Assert.IsTrue(_model.IsConfirmedToastVisible);
        }

        [Test]
        public void Tick_IgnoredOutsideConfirmed_AndForNonPositiveDelta()
        {
            _model.ReportFailed();
            _model.Tick(10f);
            Assert.AreEqual(BossDefeatReportPanelState.Failed, _model.State, "Failed không tự ẩn theo thời gian");

            _model.ReportConfirmed();
            _model.Tick(0f);
            _model.Tick(-5f);
            Assert.AreEqual(BossDefeatReportPanelState.Confirmed, _model.State, "Delta ≤ 0 không làm thời gian trôi");
        }

        [Test]
        public void Confirmed_TimerRestartsOnEachConfirmation()
        {
            _model.ReportConfirmed();
            _model.Tick(2.5f);

            _model.ReportConfirmed(); // xác nhận mới → đếm lại 3 giây
            _model.Tick(2.5f);

            Assert.AreEqual(BossDefeatReportPanelState.Confirmed, _model.State);
        }

        // ── Sending → Failed (thử lại vẫn lỗi) ─────────────────────────────────

        [Test]
        public void RetryFails_GoesBackToFailed_AndRetryIsEnabledAgain()
        {
            _model.ReportFailed();
            _model.TryBeginRetry();

            _model.ReportFailed();

            Assert.AreEqual(BossDefeatReportPanelState.Failed, _model.State);
            Assert.IsTrue(_model.IsRetryInteractable, "Cho thử lại lần nữa");
            Assert.AreEqual(BossDefeatReportPanelModel.RetryLabel, _model.RetryButtonText);
            Assert.IsFalse(_model.IsRetryInFlight);
            Assert.IsTrue(_model.TryBeginRetry(), "Bấm lại được sau khi lỗi");
        }

        // ── Đóng: không chặn chơi tiếp ──────────────────────────────────────────

        [Test]
        public void Close_FromFailed_HidesPanel()
        {
            _model.ReportFailed();

            _model.Close();

            Assert.AreEqual(BossDefeatReportPanelState.Hidden, _model.State);
            Assert.IsFalse(_model.IsPanelVisible);
        }

        [Test]
        public void Close_WhenHidden_DoesNothing()
        {
            _model.Close();

            Assert.AreEqual(BossDefeatReportPanelState.Hidden, _model.State);
            Assert.AreEqual(0, _changedCount, "Không đổi trạng thái thì không phát Changed");
        }

        [Test]
        public void Close_WhileSending_Hides_ButBlocksNewRetryUntilResultArrives()
        {
            _model.ReportFailed();
            _model.TryBeginRetry();

            _model.Close();

            Assert.AreEqual(BossDefeatReportPanelState.Hidden, _model.State);
            Assert.IsTrue(_model.IsRetryInFlight, "Lần gửi vẫn chạy ngầm");
            Assert.IsFalse(_model.TryBeginRetry(), "Bảng đã ẩn: không có gì để bấm");
        }

        [Test]
        public void Close_WhileSending_ThenRetryFails_ShowsPanelAgain()
        {
            _model.ReportFailed();
            _model.TryBeginRetry();
            _model.Close();

            _model.ReportFailed();

            Assert.AreEqual(BossDefeatReportPanelState.Failed, _model.State);
            Assert.IsTrue(_model.IsPanelVisible);
            Assert.IsTrue(_model.IsRetryInteractable);
        }

        [Test]
        public void Close_WhileSending_ThenRetryConfirms_ShowsToast()
        {
            _model.ReportFailed();
            _model.TryBeginRetry();
            _model.Close();

            _model.ReportConfirmed();

            Assert.AreEqual(BossDefeatReportPanelState.Confirmed, _model.State);
            Assert.IsTrue(_model.IsConfirmedToastVisible);
        }

        [Test]
        public void Close_DuringConfirmedToast_HidesIt()
        {
            _model.ReportConfirmed();

            _model.Close();

            Assert.AreEqual(BossDefeatReportPanelState.Hidden, _model.State);
        }

        // ── Không để kẹt ở "Đang gửi..." ────────────────────────────────────────

        [Test]
        public void RetryReturnedWithoutResult_WhileSending_GoesToHidden_NotStuck()
        {
            _model.ReportFailed();
            _model.TryBeginRetry();

            _model.RetryReturnedWithoutResult();

            Assert.AreEqual(BossDefeatReportPanelState.Hidden, _model.State);
            Assert.IsFalse(_model.IsRetryInFlight);
        }

        [Test]
        public void RetryReturnedWithoutResult_AfterResultAlreadyArrived_ChangesNothing()
        {
            _model.ReportFailed();
            _model.TryBeginRetry();
            _model.ReportConfirmed(); // kết quả tới trước khi hàm thử lại trả về

            _model.RetryReturnedWithoutResult();

            Assert.AreEqual(BossDefeatReportPanelState.Confirmed, _model.State);
        }

        [Test]
        public void RetryReturnedWithoutResult_WithNoRetryRunning_ChangesNothing()
        {
            _model.ReportFailed();

            _model.RetryReturnedWithoutResult();

            Assert.AreEqual(BossDefeatReportPanelState.Failed, _model.State);
        }

        // ── Boss khác báo lỗi khi đang hiện xác nhận ────────────────────────────

        [Test]
        public void ReportFailed_WhileConfirmedToastShowing_SwitchesToFailed()
        {
            _model.ReportConfirmed();

            _model.ReportFailed();

            Assert.AreEqual(BossDefeatReportPanelState.Failed, _model.State);
            Assert.IsTrue(_model.IsPanelVisible);
            Assert.IsFalse(_model.IsConfirmedToastVisible);
        }

        // ── Sự kiện Changed ─────────────────────────────────────────────────────

        [Test]
        public void Changed_FiresOncePerStateChange()
        {
            _model.ReportFailed();
            Assert.AreEqual(1, _changedCount);

            _model.TryBeginRetry();
            Assert.AreEqual(2, _changedCount);

            _model.TryBeginRetry(); // bị chặn: không đổi gì
            Assert.AreEqual(2, _changedCount);

            _model.ReportConfirmed();
            Assert.AreEqual(3, _changedCount);

            _model.Tick(BossDefeatReportPanelModel.ConfirmedToastSeconds);
            Assert.AreEqual(4, _changedCount);
        }

        // ── Chữ hiển thị + kích thước nút (cảm ứng) ─────────────────────────────

        [Test]
        public void Texts_MatchTheSpecification()
        {
            Assert.AreEqual(
                "Boss đã gục nhưng chưa báo được lên máy chủ. Hãy kết nối lại, rồi bấm Thử lại.",
                BossDefeatReportPanelModel.FailedMessage);
            Assert.AreEqual("Thử lại", BossDefeatReportPanelModel.RetryLabel);
            Assert.AreEqual("Đang gửi...", BossDefeatReportPanelModel.SendingLabel);
            Assert.AreEqual("Đóng", BossDefeatReportPanelModel.CloseLabel);
            Assert.AreEqual("Máy chủ đã ghi nhận. Tầng kế đã mở.", BossDefeatReportPanelModel.ConfirmedMessage);
        }

        [Test]
        public void Buttons_AreAtLeast96PixelsTall_ForTouch()
        {
            Assert.GreaterOrEqual(BossDefeatReportPanelModel.MinButtonHeightPixels, 96f);
            Assert.GreaterOrEqual(BossDefeatReportPanelModel.ButtonHeightPixels, BossDefeatReportPanelModel.MinButtonHeightPixels,
                "Nút phải cao ≥ 96 px để chạm bằng ngón tay");
        }

        // ── Đường đi đầy đủ ─────────────────────────────────────────────────────

        [Test]
        public void FullPath_Hidden_Failed_Sending_Confirmed_Hidden()
        {
            Assert.AreEqual(BossDefeatReportPanelState.Hidden, _model.State);

            _model.ReportFailed();
            Assert.AreEqual(BossDefeatReportPanelState.Failed, _model.State);

            Assert.IsTrue(_model.TryBeginRetry());
            Assert.AreEqual(BossDefeatReportPanelState.Sending, _model.State);

            _model.ReportConfirmed();
            Assert.AreEqual(BossDefeatReportPanelState.Confirmed, _model.State);

            _model.Tick(BossDefeatReportPanelModel.ConfirmedToastSeconds);
            Assert.AreEqual(BossDefeatReportPanelState.Hidden, _model.State);
        }
    }
}
