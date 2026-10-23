using Game.Core.Events;
using Game.Core.Presentation;
using Game.Core.Settings;
using Game.PvE.Player;
using Game.Services.Haptics;
using Game.UI.Settings;
using UnityEngine;

namespace Game.UI.Haptics
{
    /// <summary>
    /// Nghe máu của NGƯỜI CHƠI và ra lệnh rung (P9.6): xung ngắn khi mất máu; hai xung ngắn mỗi lần HP rơi xuống dưới ngưỡng
    /// (có độ trễ: chỉ báo lại sau khi HP lên trên ngưỡng nạp lại). Không rung khi đã chết. Số liệu nằm trong CombatFeelConfig.
    /// Luật độ trễ nằm ở <see cref="LowHpWarning"/> (thuần, có test), lớp này chỉ nối dây.
    /// </summary>
    public class HapticFeedbackListener : MonoBehaviour
    {
        [Tooltip("Asset cấu hình cảm giác chiến đấu (không bắt buộc; thiếu thì dùng bản đang active hoặc mặc định).")]
        [SerializeField] private CombatFeelConfig config;

        private LowHpWarning _warning;
        private bool _dead;

        private void OnEnable()
        {
            CombatFeelConfig cfg = CombatFeelConfig.Resolve(config);
            _warning = new LowHpWarning(cfg.HapticLowHpRatio, cfg.HapticRearmRatio);
            _dead = false;

            HapticsService.ConfigureMinInterval(cfg.HapticMinIntervalSeconds);
            HapticsService.Enabled = SettingsRuntime.Current.HapticsEnabled;
            SettingsRuntime.Changed += HandleSettingsChanged;

            GameEvents.OnHealthChanged += HandleHealthChanged;
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            SettingsRuntime.Changed -= HandleSettingsChanged;
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        private void HandleSettingsChanged(PlayerSettings settings)
        {
            HapticsService.Enabled = settings.HapticsEnabled;
        }

        private void HandlePlayerDied(object sender, Game.Core.Combat.DamageInfo lastHit)
        {
            _dead = true;
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs args)
        {
            // Chỉ máu của người chơi (HealthComponent); máu quái không rung.
            if (args == null || !(sender is HealthComponent)) return;
            if (_dead || args.Current <= 0.0) return;

            CombatFeelConfig cfg = CombatFeelConfig.Resolve(config);
            bool warn = _warning.ShouldWarn(args.Current, args.Max);

            if (warn)
            {
                HapticsService.DoublePulse(cfg.HapticWarnPulseMilliseconds, cfg.HapticWarnGapMilliseconds);
            }
            else if (args.Delta < 0.0)
            {
                HapticsService.Pulse(cfg.HapticHitMilliseconds);
            }
        }
    }
}
