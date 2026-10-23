using Game.Core.Events;
using Game.Core.Presentation;
using Game.PvE.Boss;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Dừng hình ngắn khi đòn của NGƯỜI CHƠI trúng (docs/05 Phase 9.5). Chỉ nghe GameEvents.OnHitResolved
    /// (đòn quái đánh người chơi không đi qua sự kiện này nên không bao giờ dừng hình) và không tự đụng timeScale:
    /// mọi thay đổi tốc độ đi qua <see cref="PlaySpeedState"/>. Quyết định nằm ở <see cref="HitStopRules"/>.
    /// </summary>
    public sealed class HitStopController : MonoBehaviour
    {
        [Tooltip("Máu người chơi — để biết người chơi đã chết thì không dừng hình. Để trống sẽ tự tìm lúc Start.")]
        [SerializeField] private HealthComponent playerHealth;
        [Tooltip("Để trống = dùng CombatFeelConfig đang active (PlaySpeedTicker đăng ký).")]
        [SerializeField] private CombatFeelConfig config;

        private double _lastHitStopAt = HitStopRules.NeverStopped;

        private void OnEnable()
        {
            GameEvents.OnHitResolved += HandleHitResolved;
        }

        private void Start()
        {
            if (playerHealth == null) playerHealth = FindFirstObjectByType<HealthComponent>();
        }

        private void OnDisable()
        {
            GameEvents.OnHitResolved -= HandleHitResolved;
            _lastHitStopAt = HitStopRules.NeverStopped;
        }

        private void HandleHitResolved(object sender, HitResolvedArgs args)
        {
            if (args == null || args.Target == null) return;

            PlaySpeedState speed = PlaySpeedState.Shared;
            double now = UnityEngine.Time.unscaledTimeAsDouble;

            var context = new HitStopContext(
                isPlayerHit: args.Source != null,
                isPaused: speed.IsPaused,
                playerIsDead: playerHealth != null && playerHealth.IsDead,
                targetKilled: args.Target.IsDead,
                targetIsBoss: args.Target is BossController,
                now: now,
                lastHitStopAt: _lastHitStopAt);

            HitStopSettings settings = CombatFeelConfig.Resolve(config).HitStop;
            HitStopDecision decision = HitStopRules.Evaluate(context, settings);
            if (!decision.Apply) return;

            _lastHitStopAt = now;
            speed.RequestHitStop(decision.Seconds, decision.TimeScale);
        }
    }
}
