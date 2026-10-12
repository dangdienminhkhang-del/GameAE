using System;

namespace Game.Core.World
{
    /// <summary>
    /// Server function (Cloud Code) KHÔNG xác nhận được yêu cầu: lỗi mạng/dịch vụ, SDK chưa sẵn sàng,
    /// hoặc response sai hợp đồng (ok != true, thiếu field, ID không khớp...).
    /// Caller phải coi là "CHƯA xác nhận": không đổi World State và cho người chơi thử lại
    /// (docs/12 §7.2 — client không được tự mở tầng kế; docs/03 §12).
    /// </summary>
    public class ServerFunctionUnavailableException : Exception
    {
        /// <summary>Tên script/hàm server, ví dụ <c>ReportBossDefeated</c>.</summary>
        public string FunctionName { get; }

        public ServerFunctionUnavailableException(string functionName, string reason, Exception inner = null)
            : base($"Server function '{functionName}' không xác nhận được: {reason}", inner)
        {
            FunctionName = functionName;
        }
    }
}
