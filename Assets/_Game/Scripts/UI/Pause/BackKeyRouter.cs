using System.Collections.Generic;

namespace Game.UI.Pause
{
    /// <summary>
    /// Panel nào có thể đóng bằng nút Back thì implement interface này và tự đăng ký vào <see cref="BackKeyRouter"/>
    /// khi bật, hủy đăng ký khi tắt. Priority cao hơn = nằm trên cùng, được đóng trước
    /// (gợi ý: Settings 40, Túi 30, Quest 20, Chọn Tầng 10).
    /// </summary>
    public interface IBackHandler
    {
        int BackPriority { get; }
        bool IsOpen { get; }
        void CloseFromBack();
    }

    /// <summary>Việc cần làm khi người chơi bấm Back. KHÔNG có giá trị nào là "thoát app": Back không bao giờ thoát app trong gameplay.</summary>
    public enum BackAction
    {
        /// <summary>Nuốt phím, không làm gì (ví dụ Death Screen đang hiện).</summary>
        Consumed = 0,
        CloseTopPanel = 1,
        ClosePause = 2,
        OpenPause = 3
    }

    /// <summary>
    /// Thuần logic ưu tiên Back: Death Screen chặn hết → panel đang mở (cao nhất trước) → đang Pause thì Tiếp tục
    /// → chưa pause thì mở Pause nếu được phép → còn lại nuốt phím.
    /// Phần danh sách handler cũng thuần (dùng interface), nên test không cần Unity.
    /// </summary>
    public sealed class BackKeyRouter
    {
        private readonly List<IBackHandler> _handlers = new List<IBackHandler>();

        public int HandlerCount => _handlers.Count;

        public void Register(IBackHandler handler)
        {
            if (handler != null && !_handlers.Contains(handler)) _handlers.Add(handler);
        }

        public void Unregister(IBackHandler handler)
        {
            if (handler != null) _handlers.Remove(handler);
        }

        /// <summary>Panel đang mở có priority cao nhất, hoặc null.</summary>
        public IBackHandler TopOpen()
        {
            IBackHandler top = null;
            for (int i = 0; i < _handlers.Count; i++)
            {
                IBackHandler h = _handlers[i];
                if (h == null || !h.IsOpen) continue;
                if (top == null || h.BackPriority > top.BackPriority) top = h;
            }
            return top;
        }

        public static BackAction Decide(bool deathScreenShowing, bool hasOpenPanel, bool isPauseOpen, bool canPause)
        {
            if (deathScreenShowing) return BackAction.Consumed;
            if (hasOpenPanel) return BackAction.CloseTopPanel;
            if (isPauseOpen) return BackAction.ClosePause;
            if (canPause) return BackAction.OpenPause;
            return BackAction.Consumed;
        }

        /// <summary>Quyết định và, nếu là đóng panel, đóng luôn panel trên cùng. Trả về việc đã chọn.</summary>
        public BackAction Handle(bool deathScreenShowing, bool isPauseOpen, bool canPause)
        {
            IBackHandler top = deathScreenShowing ? null : TopOpen();
            BackAction action = Decide(deathScreenShowing, top != null, isPauseOpen, canPause);
            if (action == BackAction.CloseTopPanel) top.CloseFromBack();
            return action;
        }
    }
}
