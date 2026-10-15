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

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime,
                UnityEngine.Mathf.Infinity, UnityEngine.Time.deltaTime);

            if (useBounds && _cam != null)
            {
                float halfH = _cam.orthographicSize;
                float halfW = halfH * _cam.aspect;
                float minX = minBounds.x + halfW;
                float maxX = maxBounds.x - halfW;
                float minY = minBounds.y + halfH;
                float maxY = maxBounds.y - halfH;
                next.x = UnityEngine.Mathf.Clamp(next.x, minX, maxX);
                next.y = UnityEngine.Mathf.Clamp(next.y, minY, maxY);
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
                snapped.x = UnityEngine.Mathf.Clamp(snapped.x, minBounds.x + halfW, maxBounds.x - halfW);
                snapped.y = UnityEngine.Mathf.Clamp(snapped.y, minBounds.y + halfH, maxBounds.y - halfH);
            }

            transform.position = snapped;
        }

        public void AssignTarget(Transform t)
        {
            target = t;
        }

        public void SetBounds(Bounds worldBounds)
        {
            useBounds = true;
            minBounds = new Vector2(worldBounds.min.x, worldBounds.min.y);
            maxBounds = new Vector2(worldBounds.max.x, worldBounds.max.y);
        }
    }
}
