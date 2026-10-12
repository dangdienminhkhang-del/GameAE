using System;

namespace Game.UI.Boss
{
    /// <summary>Trạng thái của bảng "Thử lại báo boss".</summary>
    public enum BossDefeatReportPanelState
    {
        /// <summary>Không hiện gì.</summary>
        Hidden = 0,

        /// <summary>Boss đã gục nhưng máy chủ chưa xác nhận: hiện bảng với nút Thử lại + Đóng.</summary>
        Failed = 1,

        /// <summary>Đang gửi lại: nút Thử lại bị khóa, hiện "Đang gửi...".</summary>
        Sending = 2,

        /// <summary>Máy chủ đã xác nhận: ẩn bảng, hiện dòng thông báo ngắn trong vài giây.</summary>
        Confirmed = 3
    }

    /// <summary>
    /// Máy trạng thái THUẦN của bảng báo boss (docs/02 §22, docs/12 §7.2). Không biết Unity, không biết BossController,
    /// không có logic thưởng hay World State: chỉ nhận "báo thất bại / báo thành công / bấm Thử lại / bấm Đóng / thời gian trôi"
    /// và cho biết lúc này nên hiện gì. <see cref="BossDefeatReportPanel"/> chỉ vẽ theo model này nên test được không cần cảnh.
    ///
    /// Đường đi: Hidden → Failed → Sending → Confirmed → Hidden (sau <see cref="ConfirmedToastSeconds"/> giây).
    /// Sending → Failed khi thử lại vẫn thất bại. Đóng ở bất kỳ lúc nào đưa về Hidden mà không chặn người chơi.
    /// </summary>
    public sealed class BossDefeatReportPanelModel
    {
        // ── Chữ hiển thị (đúng chữ yêu cầu P8-5) ──────────────────────────────────

        public const string FailedMessage = "Boss đã gục nhưng chưa báo được lên máy chủ. Hãy kết nối lại, rồi bấm Thử lại.";
        public const string RetryLabel = "Thử lại";
        public const string SendingLabel = "Đang gửi...";
        public const string CloseLabel = "Đóng";
        public const string ConfirmedMessage = "Máy chủ đã ghi nhận. Tầng kế đã mở.";

        // ── Số liệu bố cục / thời gian (hằng có tên, không magic number) ─────────────

        /// <summary>Dòng "Máy chủ đã ghi nhận" hiện bao lâu (giây, thời gian thực).</summary>
        public const float ConfirmedToastSeconds = 3f;

        /// <summary>Chiều cao tối thiểu của nút để chạm bằng ngón tay (pixel theo độ phân giải tham chiếu 1920x1080).</summary>
        public const float MinButtonHeightPixels = 96f;

        /// <summary>Chiều cao nút thực tế, luôn ≥ <see cref="MinButtonHeightPixels"/>.</summary>
        public const float ButtonHeightPixels = 112f;

        private bool _retryInFlight;
        private float _confirmedElapsed;

        public BossDefeatReportPanelState State { get; private set; } = BossDefeatReportPanelState.Hidden;

        /// <summary>Phát ra mỗi khi State đổi, để UI vẽ lại.</summary>
        public event Action Changed;

        /// <summary>Bảng (thông báo + nút) đang hiện.</summary>
        public bool IsPanelVisible => State == BossDefeatReportPanelState.Failed || State == BossDefeatReportPanelState.Sending;

        /// <summary>Dòng "Máy chủ đã ghi nhận" đang hiện.</summary>
        public bool IsConfirmedToastVisible => State == BossDefeatReportPanelState.Confirmed;

        /// <summary>Chỉ bấm được Thử lại khi đang Failed: đang gửi thì khóa để chặn bấm đúp.</summary>
        public bool IsRetryInteractable => State == BossDefeatReportPanelState.Failed;

        public string RetryButtonText => State == BossDefeatReportPanelState.Sending ? SendingLabel : RetryLabel;

        /// <summary>Có một lần thử lại đang chạy (kể cả khi người chơi đã bấm Đóng giữa chừng).</summary>
        public bool IsRetryInFlight => _retryInFlight;

        /// <summary>Boss báo "chưa báo được lên máy chủ" (lần đầu hoặc thử lại vẫn lỗi) → hiện bảng, cho bấm Thử lại.</summary>
        public void ReportFailed()
        {
            _retryInFlight = false;
            SetState(BossDefeatReportPanelState.Failed);
        }

        /// <summary>
        /// Máy chủ đã xác nhận (lần báo đầu tiên hoặc sau Thử lại) → ẩn bảng, hiện dòng xác nhận trong
        /// <see cref="ConfirmedToastSeconds"/> giây.
        /// </summary>
        public void ReportConfirmed()
        {
            _retryInFlight = false;
            _confirmedElapsed = 0f;
            SetState(BossDefeatReportPanelState.Confirmed);
        }

        /// <summary>
        /// Người chơi bấm Thử lại. Trả true (và chuyển Sending) CHỈ khi đang Failed và chưa có lần gửi nào đang chạy.
        /// Trả false = bỏ qua lần bấm này (bấm đúp, hoặc bảng không ở trạng thái Failed).
        /// </summary>
        public bool TryBeginRetry()
        {
            if (State != BossDefeatReportPanelState.Failed || _retryInFlight) return false;

            _retryInFlight = true;
            SetState(BossDefeatReportPanelState.Sending);
            return true;
        }

        /// <summary>
        /// Lần thử lại đã chạy xong mà boss không phát tín hiệu Failed/Confirmed nào (ví dụ boss không còn báo cáo chờ
        /// hoặc đã bị hủy). Gỡ trạng thái Sending để bảng không bị kẹt: về Hidden. Không làm gì nếu kết quả đã tới.
        /// </summary>
        public void RetryReturnedWithoutResult()
        {
            if (!_retryInFlight) return;

            _retryInFlight = false;
            if (State == BossDefeatReportPanelState.Sending) SetState(BossDefeatReportPanelState.Hidden);
        }

        /// <summary>
        /// Nút Đóng: ẩn bảng, không chặn chơi tiếp. Nếu đang gửi thì lần gửi vẫn chạy ngầm; kết quả tới sau
        /// (Failed → hiện lại bảng, Confirmed → hiện dòng xác nhận).
        /// </summary>
        public void Close()
        {
            if (State == BossDefeatReportPanelState.Hidden) return;
            SetState(BossDefeatReportPanelState.Hidden);
        }

        /// <summary>Cho thời gian trôi (giây thực). Hết <see cref="ConfirmedToastSeconds"/> thì dòng xác nhận tự ẩn.</summary>
        public void Tick(float deltaSeconds)
        {
            if (State != BossDefeatReportPanelState.Confirmed || deltaSeconds <= 0f) return;

            _confirmedElapsed += deltaSeconds;
            if (_confirmedElapsed >= ConfirmedToastSeconds) SetState(BossDefeatReportPanelState.Hidden);
        }

        private void SetState(BossDefeatReportPanelState next)
        {
            State = next;
            Changed?.Invoke();
        }
    }
}
