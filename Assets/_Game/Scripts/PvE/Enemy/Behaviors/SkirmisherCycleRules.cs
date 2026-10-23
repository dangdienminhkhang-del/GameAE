using UnityEngine;

namespace Game.PvE.Enemy.Behaviors
{
    public enum SkirmisherPhase
    {
        Approach = 0,
        Attack = 1,
        Retreat = 2
    }

    /// <summary>
    /// Chu kỳ của Skirmisher: Approach (đuổi tới tầm đánh) → Attack (báo đòn rồi đánh) → Retreat (lùi) → Approach.
    /// Thuần logic. Chống dao động bằng trễ ở biên tầm đánh; không kẹt ở pha lùi: hết retreatSeconds, hoặc bị kẹt,
    /// hoặc đã lùi đủ xa là về Approach.
    /// </summary>
    public sealed class SkirmisherCycleRules
    {
        /// <summary>Đang Attack thì chỉ rời khi khoảng cách vượt attackRange + chừng này (trễ, tránh nhấp nháy ở biên).</summary>
        public const float AttackExitMargin = 0.3f;

        public SkirmisherPhase Phase { get; private set; }

        /// <summary>Số giây lùi còn lại (0 khi không đang lùi).</summary>
        public float RetreatRemaining { get; private set; }

        public void Reset()
        {
            Phase = SkirmisherPhase.Approach;
            RetreatRemaining = 0f;
        }

        /// <summary>Chase → Attack: không đang lùi và trong tầm đánh.</summary>
        public bool ShouldEngage(float distance, float attackRange)
        {
            return Phase != SkirmisherPhase.Retreat && distance <= attackRange;
        }

        /// <summary>
        /// Attack → Chase: đang lùi thì luôn rời; đang Attack thì rời khi ra ngoài tầm cộng biên trễ;
        /// còn lại rời khi ra ngoài tầm đánh.
        /// </summary>
        public bool ShouldDisengage(float distance, float attackRange)
        {
            if (Phase == SkirmisherPhase.Retreat) return true;
            if (Phase == SkirmisherPhase.Attack) return distance > attackRange + AttackExitMargin;
            return distance > attackRange;
        }

        /// <summary>Vừa đánh xong: bắt đầu lùi <paramref name="retreatSeconds"/> giây (số âm coi như 0).</summary>
        public void OnStrike(float retreatSeconds)
        {
            Phase = SkirmisherPhase.Retreat;
            RetreatRemaining = retreatSeconds > 0f ? retreatSeconds : 0f;
        }

        /// <summary>
        /// Tiến chu kỳ một bước và trả pha mới.
        /// Retreat kết thúc khi: hết giờ, hoặc bị kẹt (<paramref name="stuck"/>), hoặc đã lùi xa hơn
        /// max(retreatRange, attackRange + AttackExitMargin) (luôn lùi ra ngoài tầm đánh trước khi thôi).
        /// </summary>
        public SkirmisherPhase Advance(float distance, float deltaTime, float attackRange, float retreatRange, bool stuck)
        {
            float dt = deltaTime > 0f ? deltaTime : 0f;

            switch (Phase)
            {
                case SkirmisherPhase.Approach:
                    if (distance <= attackRange) Phase = SkirmisherPhase.Attack;
                    break;

                case SkirmisherPhase.Attack:
                    if (distance > attackRange + AttackExitMargin) Phase = SkirmisherPhase.Approach;
                    break;

                case SkirmisherPhase.Retreat:
                    RetreatRemaining = Mathf.Max(0f, RetreatRemaining - dt);
                    float farEnough = Mathf.Max(retreatRange, attackRange + AttackExitMargin);
                    if (stuck || RetreatRemaining <= 0f || distance >= farEnough)
                    {
                        Phase = SkirmisherPhase.Approach;
                        RetreatRemaining = 0f;
                    }
                    break;
            }
            return Phase;
        }
    }
}
