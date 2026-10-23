using UnityEngine;

namespace Game.UI.Pause
{
    /// <summary>
    /// Bộ định tuyến Back dùng chung cho cả game (P9.6): các panel đăng ký vào đây khi bật, PauseController bấm Back qua đây.
    /// Static nên PHẢI reset khi vào Play (Enter Play Mode Options có thể tắt Domain Reload).
    /// </summary>
    public static class BackKeyRouterHost
    {
        public static BackKeyRouter Router { get; private set; } = new BackKeyRouter();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Router = new BackKeyRouter();
        }
    }
}
