using Game.Core.Input;
using Game.Core.Stats;
using UnityEngine;

namespace Game.PvE.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private MonoBehaviour inputReaderComponent;

        [Header("Stats")]
        [SerializeField] private int startingLevel = 1;

        private Rigidbody2D _rb;
        private IInputReader _inputReader;
        private Vector2 _currentMove;
        private CharacterStats _stats;

        public CharacterStats Stats => _stats;

        /// <summary>1 = quay phải, -1 = quay trái. PlayerAttack dùng để đặt vùng đánh phía trước.</summary>
        public int Facing { get; private set; } = 1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _stats = new CharacterStats();
            _stats.SetLevel(startingLevel);

            if (inputReaderComponent is IInputReader reader)
            {
                _inputReader = reader;
            }
            else if (TryGetComponent(out IInputReader directReader))
            {
                _inputReader = directReader;
            }
        }

        private void OnEnable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnMove += HandleMove;
            }
        }

        private void OnDisable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnMove -= HandleMove;
            }
        }

        private void HandleMove(object sender, Vector2 value)
        {
            _currentMove = value;
        }

        private void FixedUpdate()
        {
            Vector2 velocity = _currentMove.normalized * moveSpeed;
            _rb.linearVelocity = velocity;

            if (spriteRenderer != null)
            {
                if (_currentMove.x > 0.01f)
                {
                    spriteRenderer.flipX = false;
                    Facing = 1;
                }
                else if (_currentMove.x < -0.01f)
                {
                    spriteRenderer.flipX = true;
                    Facing = -1;
                }
            }

            if (animator != null)
            {
                animator.SetBool("IsWalking", _currentMove.sqrMagnitude > 0.001f);
            }
        }

        public void AssignInputReader(IInputReader reader)
        {
            if (_inputReader != null)
            {
                _inputReader.OnMove -= HandleMove;
            }
            _inputReader = reader;
            if (_inputReader != null)
            {
                _inputReader.OnMove += HandleMove;
            }
        }
    }
}
