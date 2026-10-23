using UnityEngine;

namespace Game.PvE.Boss
{
    /// <summary>
    /// Phát animation sprite 8 hướng cho boss (idle / walk / attack) mà KHÔNG sửa BossController.
    /// - Hướng: theo vận tốc khi đang di chuyển; đứng yên thì nhìn theo mục tiêu.
    /// - Attack: phát một lần khi boss bắt đầu telegraph (BossController.TelegraphStarted),
    ///   kéo dài đúng bằng thời gian báo đòn để khung chém rơi gần lúc đòn trúng.
    /// - Sprite 8 hướng nên tắt flipX mà BossController đặt (LateUpdate ép về false).
    /// Thứ tự hướng: S, SE, E, NE, N, NW, W, SW (khớp bộ sprite).
    /// </summary>
    [DisallowMultipleComponent]
    public class BossDirectionalSpriteAnimator : MonoBehaviour
    {
        public const int DirectionCount = 8;
        public const int WalkFrames = 4;
        public const int AttackFrames = 6;

        [SerializeField] private BossController controller;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D body;

        [Tooltip("8 sprite, thứ tự S, SE, E, NE, N, NW, W, SW.")]
        [SerializeField] private Sprite[] idle = new Sprite[DirectionCount];
        [Tooltip("8 hướng x 4 khung, chỉ số = hướng * 4 + khung.")]
        [SerializeField] private Sprite[] walk = new Sprite[DirectionCount * WalkFrames];
        [Tooltip("8 hướng x 6 khung, chỉ số = hướng * 6 + khung.")]
        [SerializeField] private Sprite[] attack = new Sprite[DirectionCount * AttackFrames];

        [SerializeField, Min(1f)] private float walkFps = 8f;
        [SerializeField, Min(0.1f)] private float movingSpeedThreshold = 0.05f;
        [SerializeField, Min(0.1f)] private float attackMinSeconds = 0.35f;
        [SerializeField, Min(0.1f)] private float attackMaxSeconds = 1.5f;
        [SerializeField, Min(0f)] private float attackHoldSeconds = 0.15f;

        private int _direction;
        private float _walkClock;
        private float _attackTime = -1f;
        private float _attackDuration;
        private Vector2 _lastFacing = Vector2.down;

        private void Reset()
        {
            controller = GetComponent<BossController>();
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<BossController>();
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            BossController.TelegraphStarted += OnTelegraphStarted;
        }

        private void OnDisable()
        {
            BossController.TelegraphStarted -= OnTelegraphStarted;
            _attackTime = -1f;
        }

        private void OnTelegraphStarted(BossController source)
        {
            if (source != controller) return;
            float windup = source.Definition != null ? source.Definition.AttackWindupSeconds : 0.6f;
            _attackDuration = Mathf.Clamp(windup, attackMinSeconds, attackMaxSeconds);
            _attackTime = 0f;
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null) return;

            // BossController đặt flipX theo hướng ngang; bộ sprite 8 hướng đã có sẵn hướng trái/phải.
            spriteRenderer.flipX = false;

            Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
            bool moving = velocity.sqrMagnitude > movingSpeedThreshold * movingSpeedThreshold;
            bool attacking = _attackTime >= 0f;

            if (moving)
            {
                _lastFacing = velocity;
            }
            else if (controller != null && controller.Target != null)
            {
                Vector2 toTarget = (Vector2)controller.Target.position - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.0001f) _lastFacing = toTarget;
            }
            _direction = DirectionIndex(_lastFacing);

            Sprite frame;
            if (attacking)
            {
                _attackTime += Time.deltaTime;
                float t = _attackDuration > 0f ? _attackTime / _attackDuration : 1f;
                int f = Mathf.Clamp(Mathf.FloorToInt(t * AttackFrames), 0, AttackFrames - 1);
                frame = Pick(attack, _direction * AttackFrames + f);
                if (_attackTime >= _attackDuration + attackHoldSeconds) _attackTime = -1f;
            }
            else if (moving)
            {
                _walkClock += Time.deltaTime * walkFps;
                int f = Mathf.FloorToInt(_walkClock) % WalkFrames;
                frame = Pick(walk, _direction * WalkFrames + f);
            }
            else
            {
                _walkClock = 0f;
                frame = Pick(idle, _direction);
            }

            if (frame != null) spriteRenderer.sprite = frame;
        }

        /// <summary>0=S, 1=SE, 2=E, 3=NE, 4=N, 5=NW, 6=W, 7=SW.</summary>
        public static int DirectionIndex(Vector2 v)
        {
            if (v.sqrMagnitude < 1e-6f) return 0;
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; // E = 0, N = 90, S = -90
            int idx = Mathf.RoundToInt((angle + 90f) / 45f);
            return ((idx % DirectionCount) + DirectionCount) % DirectionCount;
        }

        private static Sprite Pick(Sprite[] set, int index)
        {
            if (set == null || index < 0 || index >= set.Length) return null;
            return set[index];
        }

#if UNITY_EDITOR
        /// <summary>Gán sprite từ 3 sheet đã cắt (tên: idle_dir, walk_dir_n, attack_dir_n).</summary>
        public void AssignFromSheets(Sprite[] sheetSprites)
        {
            string[] dirs = { "south", "south-east", "east", "north-east", "north", "north-west", "west", "south-west" };
            idle = new Sprite[DirectionCount];
            walk = new Sprite[DirectionCount * WalkFrames];
            attack = new Sprite[DirectionCount * AttackFrames];
            foreach (var s in sheetSprites)
            {
                if (s == null) continue;
                for (int d = 0; d < DirectionCount; d++)
                {
                    if (s.name == "idle_" + dirs[d]) idle[d] = s;
                    for (int f = 0; f < WalkFrames; f++)
                        if (s.name == "walk_" + dirs[d] + "_" + f) walk[d * WalkFrames + f] = s;
                    for (int f = 0; f < AttackFrames; f++)
                        if (s.name == "attack_" + dirs[d] + "_" + f) attack[d * AttackFrames + f] = s;
                }
            }
        }
#endif
    }
}
