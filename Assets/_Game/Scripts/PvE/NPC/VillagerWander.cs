using UnityEngine;

namespace Game.PvE.NPC
{
    /// <summary>
    /// Cho NPC dân làng đi dạo ngẫu nhiên quanh vị trí gốc.
    /// Đứng yên khi người chơi lại gần (để nói chuyện) và né các vật cản đặc.
    /// Dùng Rigidbody2D kinematic để trigger của NPCInteractable vẫn hoạt động khi di chuyển.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class VillagerWander : MonoBehaviour
    {
        [SerializeField] private float radiusX = 3f;
        [SerializeField] private float radiusY = 1f;
        [SerializeField] private float speed = 0.9f;
        [SerializeField] private Vector2 pauseRange = new Vector2(1.5f, 4f);
        [SerializeField] private float yieldDistance = 2.5f;
        [SerializeField] private float obstacleRadius = 0.35f;

        private readonly Collider2D[] _hits = new Collider2D[8];
        private Collider2D _ownCollider;
        private Rigidbody2D _rb;
        private SpriteRenderer _sprite;
        private Transform _player;
        private Vector2 _home;
        private Vector2 _target;
        private float _pause;
        private float _playerLookupTimer;
        private bool _moving;

        private void Awake()
        {
            _home = transform.position;
            TryGetComponent(out _ownCollider);
            TryGetComponent(out _sprite);
            if (!TryGetComponent(out _rb)) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _pause = Random.Range(0f, pauseRange.y);
        }

        private void FixedUpdate()
        {
            ResolvePlayer();
            if (_player != null && Vector2.Distance(_player.position, _rb.position) < yieldDistance)
            {
                _moving = false;
                return;
            }

            if (!_moving)
            {
                _pause -= Time.fixedDeltaTime;
                if (_pause <= 0f) PickTarget();
                return;
            }

            Vector2 pos = _rb.position;
            Vector2 to = _target - pos;
            float step = speed * Time.fixedDeltaTime;
            if (to.magnitude <= step)
            {
                _rb.MovePosition(_target);
                _moving = false;
                _pause = Random.Range(pauseRange.x, pauseRange.y);
                return;
            }

            Vector2 dir = to.normalized;
            _rb.MovePosition(pos + dir * step);
            if (_sprite != null && Mathf.Abs(dir.x) > 0.1f) _sprite.flipX = dir.x < 0f;
        }

        private void ResolvePlayer()
        {
            if (_player != null) return;
            _playerLookupTimer -= Time.fixedDeltaTime;
            if (_playerLookupTimer > 0f) return;
            _playerLookupTimer = 1f;
            var pc = FindAnyObjectByType<Game.PvE.Player.PlayerController>();
            if (pc != null) _player = pc.transform;
        }

        private void PickTarget()
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 candidate = _home + new Vector2(
                    Random.Range(-radiusX, radiusX), Random.Range(-radiusY, radiusY));
                if (IsBlocked(candidate)) continue;
                _target = candidate;
                _moving = true;
                return;
            }
            _pause = 1f;
        }

        private bool IsBlocked(Vector2 point)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            int n = Physics2D.OverlapCircle(point, obstacleRadius, filter, _hits);
            for (int i = 0; i < n; i++)
            {
                if (_hits[i] != null && _hits[i] != _ownCollider) return true;
            }
            return false;
        }
    }
}
