using Game.Core.Facing;
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

        [Header("Facing (8 hướng, docs/13 Wave 9.5a)")]
        [Tooltip("Ngưỡng chết của hướng nhìn: chỉ lọc nhiễu nhỏ, KHÔNG phải dead zone joystick (dead zone là việc của P9.6).")]
        [SerializeField] private float facingDeadZone = FacingTracker.DefaultDeadZone;

        [Header("Stats")]
        [SerializeField] private int startingLevel = 1;

        private Rigidbody2D _rb;
        private IInputReader _inputReader;
        private Vector2 _currentMove;
        private CharacterStats _stats;
        private FacingTracker _facingTracker;
        private float _speedMultiplier = 1f;

        public float SpeedMultiplier
        {
            get => _speedMultiplier;
            set => _speedMultiplier = Mathf.Clamp(value, 0.1f, 3.0f);
        }

        // Tham số Animator hướng nhìn: kiểm MỘT lần xem controller có đủ FaceX/FaceY (Float) hay không.
        private static readonly int FaceXHash = Animator.StringToHash("FaceX");
        private static readonly int FaceYHash = Animator.StringToHash("FaceY");
        private bool _faceParamsChecked;
        private bool _hasFaceParams;

        public CharacterStats Stats => _stats;

        /// <summary>1 = quay phải, -1 = quay trái. PlayerAttack dùng để đặt vùng đánh phía trước.</summary>
        public int Facing { get; private set; } = 1;

        private FacingTracker Tracker => _facingTracker ?? (_facingTracker = new FacingTracker(Direction8.East, facingDeadZone));

        /// <summary>Hướng nhìn hiện tại, vector đơn vị theo 8 hướng, mặc định (1, 0). Đòn đánh, Né và Kỹ thuật dùng hướng này.</summary>
        public Vector2 FacingDirection => Tracker.Vector;

        /// <summary>Hướng nhìn hiện tại dưới dạng một trong 8 hướng.</summary>
        public Direction8 Facing8 => Tracker.Direction;

        /// <summary>
        /// Đặt hướng nhìn (ví dụ tự nhắm quay về phía quái). Vector gần 0 hoặc NaN thì bỏ qua.
        /// Nếu hướng mới có thành phần ngang thì Facing (±1) và flipX được cập nhật cùng, để hình khớp hướng đánh.
        /// </summary>
        public void SetFacingDirection(Vector2 direction)
        {
            if (!FacingMath.IsUsable(direction)) return;

            Tracker.Set(direction);
            // Quyết định lật hình theo hướng đã làm tròn (Bắc/Nam thì giữ nguyên), không theo vector thô có thể lệch nhẹ.
            Vector2 snapped = Tracker.Vector;
            if (snapped.x > 0.01f) ApplyHorizontalFacing(1);
            else if (snapped.x < -0.01f) ApplyHorizontalFacing(-1);
        }

        private void ApplyHorizontalFacing(int facing)
        {
            Facing = facing;
            if (spriteRenderer != null) spriteRenderer.flipX = facing < 0;
        }

        private void EnsureAdvancedCombatComponents()
        {
            if (!TryGetComponent<ComboTracker>(out _)) gameObject.AddComponent<ComboTracker>();
            if (!TryGetComponent<Dodge.PerfectDodgeDetector>(out _)) gameObject.AddComponent<Dodge.PerfectDodgeDetector>();
            if (!TryGetComponent<AdrenalineController>(out _)) gameObject.AddComponent<AdrenalineController>();
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _stats = new CharacterStats();
            _stats.SetLevel(startingLevel);

            EnsureAdvancedCombatComponents();

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
            Vector2 velocity = _currentMove.normalized * (moveSpeed * _speedMultiplier);
            _rb.linearVelocity = velocity;

            // Hướng nhìn 8 hướng: thả joystick (vector gần 0) thì giữ hướng cũ.
            Tracker.Update(_currentMove);

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
                UpdateFaceParams();
            }
        }

        private void UpdateFaceParams()
        {
            if (!_faceParamsChecked) CheckFaceParams();
            if (!_hasFaceParams) return;

            FacingMath.ToAnimatorParams(Tracker.Direction, out float faceX, out float faceY);
            animator.SetFloat(FaceXHash, faceX);
            animator.SetFloat(FaceYHash, faceY);
        }

        // Kiểm một lần: chỉ ghi FaceX/FaceY khi controller có đủ hai tham số Float (tránh cảnh báo lặp mỗi khung hình).
        private void CheckFaceParams()
        {
            if (animator.runtimeAnimatorController == null) return; // chưa có controller: kiểm lại ở lần sau

            _faceParamsChecked = true;
            bool hasX = false;
            bool hasY = false;
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type != AnimatorControllerParameterType.Float) continue;
                if (parameters[i].nameHash == FaceXHash) hasX = true;
                else if (parameters[i].nameHash == FaceYHash) hasY = true;
            }
            _hasFaceParams = hasX && hasY;
        }

        private void OnValidate()
        {
            facingDeadZone = Mathf.Clamp(facingDeadZone, 0f, 0.5f);
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
