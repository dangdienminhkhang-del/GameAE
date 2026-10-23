using Game.Core.Audio;
using UnityEngine;

namespace Game.Services.Audio
{
    /// <summary>
    /// Giữ AudioService đang chạy cho ServiceProvider.GetAudioService(). Static nên PHẢI reset khi vào Play
    /// (Enter Play Mode Options có thể tắt Domain Reload).
    /// </summary>
    public static class AudioServiceLocator
    {
        // Lưu kiểu cụ thể (UnityEngine.Object) để kiểm "đã bị hủy" bằng == null đúng nghĩa.
        private static AudioService s_current;

        /// <summary>Bản đang chạy, hoặc bản im lặng nếu chưa có (không bao giờ null).</summary>
        public static IAudioService Current
        {
            get
            {
                if (s_current != null) return s_current;
                return NullAudioService.Instance;
            }
        }

        /// <summary>Đăng ký <paramref name="service"/>. Trả false nếu đã có một AudioService khác còn sống (tránh nhân đôi).</summary>
        internal static bool TryClaim(AudioService service)
        {
            if (s_current != null && s_current != service) return false;
            s_current = service;
            return true;
        }

        internal static void Release(AudioService service)
        {
            if (s_current == service) s_current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            s_current = null;
        }
    }
}
