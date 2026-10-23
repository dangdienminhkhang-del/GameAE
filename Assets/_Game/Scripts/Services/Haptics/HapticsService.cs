using UnityEngine;

namespace Game.Services.Haptics
{
    /// <summary>
    /// Rung điện thoại (P9.6). Android: Vibrator qua androidjni, dùng VibrationEffect khi API 26 trở lên,
    /// lỗi thì lùi về Handheld.Vibrate. Editor và nền tảng khác: không làm gì.
    /// Tôn trọng cài đặt Rung (<see cref="Enabled"/>, do lớp UI nối từ Settings) và giãn cách tối thiểu qua <see cref="HapticThrottle"/>.
    /// Quyền VIBRATE trong AndroidManifest do Unity AI kiểm sau khi build (code này không sửa manifest).
    /// </summary>
    public static class HapticsService
    {
        private static HapticThrottle s_throttle = new HapticThrottle();
        private static bool s_enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject s_vibrator;
        private static int s_sdkInt = -1;
        private static bool s_failed;
#endif

        /// <summary>Bật/tắt rung theo Settings. Tắt thì mọi lệnh rung bị bỏ qua.</summary>
        public static bool Enabled
        {
            get => s_enabled;
            set => s_enabled = value;
        }

        /// <summary>Đặt giãn cách tối thiểu giữa hai xung (giây thời gian thật). Gọi khi có cấu hình mới.</summary>
        public static void ConfigureMinInterval(float seconds)
        {
            s_throttle = new HapticThrottle(seconds);
        }

        /// <summary>Một xung ngắn <paramref name="milliseconds"/> mili-giây.</summary>
        public static void Pulse(int milliseconds)
        {
            if (!CanVibrate() || milliseconds <= 0) return;
            VibrateOneShot(milliseconds);
        }

        /// <summary>Hai xung ngắn cách nhau một khoảng nghỉ (cảnh báo HP thấp).</summary>
        public static void DoublePulse(int pulseMilliseconds, int gapMilliseconds)
        {
            if (!CanVibrate() || pulseMilliseconds <= 0) return;
            if (gapMilliseconds < 0) gapMilliseconds = 0;
            VibrateWaveform(new long[] { 0, pulseMilliseconds, gapMilliseconds, pulseMilliseconds });
        }

        private static bool CanVibrate()
        {
            if (!s_enabled) return false;
            return s_throttle.TryAcquire(UnityEngine.Time.realtimeSinceStartup);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void VibrateOneShot(int milliseconds)
        {
            try
            {
                if (!EnsureVibrator()) { Handheld.Vibrate(); return; }
                if (s_sdkInt >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, -1))
                    {
                        s_vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    s_vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (System.Exception)
            {
                FallbackVibrate();
            }
        }

        private static void VibrateWaveform(long[] timings)
        {
            try
            {
                if (!EnsureVibrator()) { Handheld.Vibrate(); return; }
                if (s_sdkInt >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, -1))
                    {
                        s_vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    s_vibrator.Call("vibrate", timings, -1);
                }
            }
            catch (System.Exception)
            {
                FallbackVibrate();
            }
        }

        private static bool EnsureVibrator()
        {
            if (s_failed) return false;
            if (s_vibrator != null) return true;

            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                s_vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                s_sdkInt = version.GetStatic<int>("SDK_INT");
            }
            if (s_vibrator == null) s_failed = true;
            return s_vibrator != null;
        }

        private static void FallbackVibrate()
        {
            s_failed = true;
            try { Handheld.Vibrate(); } catch (System.Exception) { }
        }
#else
        // Editor và nền tảng khác: không rung.
        private static void VibrateOneShot(int milliseconds) { }
        private static void VibrateWaveform(long[] timings) { }
#endif

        // Static: reset khi vào Play (Enter Play Mode Options có thể tắt Domain Reload).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            s_throttle = new HapticThrottle();
            s_enabled = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            s_vibrator = null;
            s_sdkInt = -1;
            s_failed = false;
#endif
        }
    }
}
