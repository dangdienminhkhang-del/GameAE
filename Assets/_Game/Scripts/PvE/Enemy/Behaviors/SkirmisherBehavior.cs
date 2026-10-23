using System;
using UnityEngine;

namespace Game.PvE.Enemy.Behaviors
{
    /// <summary>
    /// Skirmisher: áp sát, đánh cận chiến bằng đúng cơ chế và lấy đà sẵn có, rồi lùi ra, rồi áp sát lại.
    /// Chu kỳ nằm ở <see cref="SkirmisherCycleRules"/> (thuần logic). Hướng lùi tránh vùng bị chặn (Safe Zone, vật cản).
    /// </summary>
    public sealed class SkirmisherBehavior : IEnemyBehavior
    {
        private readonly EnemyRoleConfig _config;
        private readonly Func<Vector2, bool> _isBlockedPoint;
        private readonly SkirmisherCycleRules _cycle = new SkirmisherCycleRules();
        private readonly StuckDetector _stuck = new StuckDetector();

        public SkirmisherBehavior(in EnemyRoleConfig config, Func<Vector2, bool> isBlockedPoint)
        {
            _config = config;
            _isBlockedPoint = isBlockedPoint;
        }

        public CombatRole Role => CombatRole.Skirmisher;

        public SkirmisherPhase Phase => _cycle.Phase;

        public void Reset()
        {
            _cycle.Reset();
            _stuck.Reset();
        }

        public Vector2 ChaseDirection(in BehaviorInput input)
        {
            SkirmisherPhase phase = _cycle.Advance(input.Distance, input.DeltaTime, _config.AttackRange,
                _config.RetreatRange, _stuck.IsStuck);

            if (phase != SkirmisherPhase.Retreat)
            {
                _stuck.Tick(input.DeltaTime, input.Self, false);
                return EnemyPositioningRules.Toward(input.Self, input.Target);
            }

            _stuck.Tick(input.DeltaTime, input.Self, true);
            // Bị kẹt (tường) thì né sang bên; chu kỳ tự về Approach khi hết giờ hoặc kẹt lâu.
            return EnemyPositioningRules.RetreatDirection(input.Self, input.Target, _isBlockedPoint,
                EnemyPositioningRules.RetreatLookAhead, _stuck.IsStuck ? 1 : 0);
        }

        public bool ShouldEngage(in BehaviorInput input)
        {
            if (!_cycle.ShouldEngage(input.Distance, _config.AttackRange)) return false;
            // Đánh dấu pha Attack (Approach + trong tầm → Attack); dt = 0 nên không tiến giờ lùi.
            _cycle.Advance(input.Distance, 0f, _config.AttackRange, _config.RetreatRange, false);
            return true;
        }

        public bool ShouldDisengage(in BehaviorInput input)
        {
            if (!_cycle.ShouldDisengage(input.Distance, _config.AttackRange)) return false;
            // Mất tầm (không phải đang lùi) thì Attack → Approach.
            if (_cycle.Phase != SkirmisherPhase.Retreat)
            {
                _cycle.Advance(input.Distance, 0f, _config.AttackRange, _config.RetreatRange, false);
            }
            return true;
        }

        public bool Strike(EnemyController owner)
        {
            bool struck = owner.TryAttackTarget();
            if (struck) _cycle.OnStrike(_config.RetreatSeconds);
            return struck;
        }
    }
}
