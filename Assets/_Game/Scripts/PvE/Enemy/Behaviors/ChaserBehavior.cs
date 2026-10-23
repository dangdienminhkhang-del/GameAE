using UnityEngine;

namespace Game.PvE.Enemy.Behaviors
{
    /// <summary>
    /// Hành vi cũ: đuổi, vào Attack khi trong tầm đánh, cận chiến. Dùng cho Chaser, và tạm cho Guard/Elite.
    /// PHẢI cho cùng quyết định như code trước Phase 9.8 (có test hồi quy).
    /// </summary>
    public sealed class ChaserBehavior : IEnemyBehavior
    {
        private readonly float _attackRange;

        public ChaserBehavior(CombatRole role, float attackRange)
        {
            Role = role;
            _attackRange = attackRange;
        }

        public CombatRole Role { get; }

        public void Reset() { }

        public Vector2 ChaseDirection(in BehaviorInput input)
        {
            return EnemyPositioningRules.Toward(input.Self, input.Target);
        }

        public bool ShouldEngage(in BehaviorInput input) => input.Distance <= _attackRange;

        public bool ShouldDisengage(in BehaviorInput input) => input.Distance > _attackRange;

        public bool Strike(EnemyController owner) => owner.TryAttackTarget();
    }
}
