using Game.PvE.Zones;
using UnityEngine;

namespace Game.PvE.Camera
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.12f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds;
        [SerializeField] private Vector2 maxBounds;

        private Vector3 _velocity = Vector3.zero;
        private UnityEngine.Camera _cam;

        // Camera Shake (rung chấn lực đánh)
        public static CameraFollow Instance { get; private set; }
        private float _shakeDuration;
        private float _shakeTimer;
        private float _shakeIntensity;

        // Chuyển bounds mượt giữa hai vùng (Phase 9.7a). Chạy bằng thời gian THẬT để dừng hình không làm lệch.
        private bool _transitioning;
        private Vector2 _fromMin;
        private Vector2 _fromMax;
        private Vector2 _toMin;
        private Vector2 _toMax;
        private float _transitionElapsed;
        private float _transitionDuration;

        public bool UseBounds => useBounds;
        public Vector2 MinBounds => minBounds;
        public Vector2 MaxBounds => maxBounds;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            Instance = this;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
        }

        public void Shake(float duration, float intensity)
        {
            if (intensity <= 0f || duration <= 0f) return;
            _shakeDuration = duration;
            _shakeTimer = duration;
            _shakeIntensity = intensity;
        }

        public static void RequestShake(float duration = 0.12f, float intensity = 0.15f)
        {
            if (Instance != null) Instance.Shake(duration, intensity);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            AdvanceBoundsTransition(UnityEngine.Time.unscaledDeltaTime);

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime,
                UnityEngine.Mathf.Infinity, UnityEngine.Time.deltaTime);

            if (useBounds && _cam != null)
            {
                float halfH = _cam.orthographicSize;
                float halfW = halfH * _cam.aspect;
                Vector2 clamped = CameraBoundsMath.Clamp(new Vector2(next.x, next.y), minBounds, maxBounds, halfW, halfH);
                next.x = clamped.x;
                next.y = clamped.y;
            }

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= UnityEngine.Time.unscaledDeltaTime;
                float factor = Mathf.Clamp01(_shakeTimer / Mathf.Max(0.001f, _shakeDuration));
                Vector2 shakeOffset = UnityEngine.Random.insideUnitCircle * (_shakeIntensity * factor);
                next.x += shakeOffset.x;
                next.y += shakeOffset.y;
            }

            next.z = offset.z;

            transform.position = next;
        }

        /// <summary>Đặt camera ngay lên mục tiêu (không SmoothDamp) — dùng khi người chơi bị dịch chuyển sang tầng khác.</summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            _velocity = Vector3.zero;
            Vector3 snapped = new Vector3(target.position.x, target.position.y, offset.z);

            if (useBounds && _cam != null)
            {
                float halfH = _cam.orthographicSize;
                float halfW = halfH * _cam.aspect;
                Vector2 clamped = CameraBoundsMath.Clamp(new Vector2(snapped.x, snapped.y), minBounds, maxBounds, halfW, halfH);
                snapped.x = clamped.x;
                snapped.y = clamped.y;
            }

            transform.position = snapped;
        }

        public void AssignTarget(Transform t)
        {
            target = t;
        }

        /// <summary>Đặt bounds ngay (hủy chuyển mượt đang chạy).</summary>
        public void SetBounds(Bounds worldBounds)
        {
            _transitioning = false;
            useBounds = true;
            minBounds = new Vector2(worldBounds.min.x, worldBounds.min.y);
            maxBounds = new Vector2(worldBounds.max.x, worldBounds.max.y);
        }

        /// <summary>
        /// Chuyển bounds sang <paramref name="worldBounds"/> trong <paramref name="seconds"/> giây thời gian thật.
        /// Chưa có bounds trước đó (hoặc seconds &lt;= 0) thì đặt ngay.
        /// </summary>
        public void SetBoundsSmooth(Bounds worldBounds, float seconds)
        {
            if (!useBounds || seconds <= 0f)
            {
                SetBounds(worldBounds);
                return;
            }

            _fromMin = minBounds;
            _fromMax = maxBounds;
            _toMin = new Vector2(worldBounds.min.x, worldBounds.min.y);
            _toMax = new Vector2(worldBounds.max.x, worldBounds.max.y);
            _transitionElapsed = 0f;
            _transitionDuration = seconds;
            _transitioning = true;
        }

        private void AdvanceBoundsTransition(float unscaledDelta)
        {
            if (!_transitioning) return;

            _transitionElapsed += unscaledDelta > 0f ? unscaledDelta : 0f;
            float t = _transitionDuration > 0f ? Mathf.Clamp01(_transitionElapsed / _transitionDuration) : 1f;
            CameraBoundsMath.Lerp(_fromMin, _fromMax, _toMin, _toMax, CameraBoundsMath.SmoothStep(t),
                out minBounds, out maxBounds);

            if (t >= 1f)
            {
                minBounds = _toMin;
                maxBounds = _toMax;
                _transitioning = false;
            }
        }
    }
}
