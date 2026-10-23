using System;
using System.Collections.Generic;
using Game.Core.Guidance;
using Game.Services;
using Game.Services.Guidance;
using UnityEngine;

namespace Game.UI.Guidance
{
    /// <summary>
    /// Điểm nối tĩnh của hướng dẫn (Phase 9.9): giữ kho cờ, công tắc và "Xem lại hướng dẫn". Settings (Main Menu và Pause) và
    /// <see cref="TutorialTriggerRouter"/> dùng chung nên công tắc có hiệu lực ngay. Chỉ chạm cờ LOCAL, không gọi server.
    /// Static: phải reset khi vào Play (Enter Play Mode Options có thể tắt Domain Reload).
    /// </summary>
    public static class TutorialRuntime
    {
        private static ITutorialStore s_store;
        private static Func<string> s_playerIdProvider;
        private static IReadOnlyList<string> s_knownHintIds;

        /// <summary>Công tắc "Gợi ý hướng dẫn" vừa đổi.</summary>
        public static event Action EnabledChanged;

        /// <summary>"Xem lại hướng dẫn" vừa xóa cờ "đã xem" của người chơi hiện tại.</summary>
        public static event Action SeenReset;

        public static ITutorialStore Store
        {
            get
            {
                if (s_store == null) s_store = new PlayerPrefsTutorialStore();
                return s_store;
            }
        }

        /// <summary>Thay kho (test). Truyền null để quay về PlayerPrefs.</summary>
        public static void UseStore(ITutorialStore store)
        {
            s_store = store;
        }

        /// <summary>Thay nguồn playerId (test). Truyền null để quay về dịch vụ tài khoản.</summary>
        public static void UsePlayerIdProvider(Func<string> provider)
        {
            s_playerIdProvider = provider;
        }

        /// <summary>Router đăng ký danh mục hint để xóa cờ chắc chắn. Có thể để trống (kho vẫn tự xóa theo chỉ mục).</summary>
        public static void RegisterKnownHintIds(IReadOnlyList<string> ids)
        {
            s_knownHintIds = ids;
        }

        public static string CurrentPlayerId
        {
            get
            {
                try
                {
                    if (s_playerIdProvider != null) return s_playerIdProvider();
                    return ServiceProvider.GetAccountService().PlayerId;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static bool Enabled => Store.GetEnabled();

        /// <summary>Bật/tắt gợi ý. Có hiệu lực ngay: router nghe <see cref="EnabledChanged"/> và ẩn toast đang hiện.</summary>
        public static void SetEnabled(bool enabled)
        {
            Store.SetEnabled(enabled);
            EnabledChanged?.Invoke();
        }

        /// <summary>"Xem lại hướng dẫn": CHỈ xóa cờ gợi ý local của playerId hiện tại, không đụng khóa nào khác.</summary>
        public static void ResetSeen()
        {
            Store.ClearAll(CurrentPlayerId, s_knownHintIds);
            SeenReset?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            s_store = null;
            s_playerIdProvider = null;
            s_knownHintIds = null;
            EnabledChanged = null;
            SeenReset = null;
        }
    }
}
