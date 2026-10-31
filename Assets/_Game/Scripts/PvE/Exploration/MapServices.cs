using Game.Core.World;
using Game.Services;
using UnityEngine;

namespace Game.PvE.Exploration
{
    /// <summary>
    /// Điểm truy cập dùng chung tới <see cref="ExplorationProgress"/> (bản đồ + Đèn Hiệu) trong phiên chơi.
    /// Tạo lười lần đầu cần; reset khi vào Play Mode (tắt Domain Reload vẫn đúng).
    /// </summary>
    public static class MapServices
    {
        private static ExplorationProgress s_progress;

        public static ExplorationProgress Progress
        {
            get
            {
                if (s_progress == null)
                {
                    s_progress = new ExplorationProgress(ServiceProvider.CreateSaveService());
                    _ = s_progress.LoadAsync(); // không bao giờ ném
                }
                return s_progress;
            }
        }

        /// <summary>Cho test: thay dịch vụ bằng bản dùng save giả. Truyền null để reset.</summary>
        public static void SetForTests(ExplorationProgress progress)
        {
            s_progress = progress;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            s_progress = null;
        }
    }
}
