using System.Collections.Generic;
using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Băng tải: mọi vật thể có Rigidbody2D động đứng trên vùng trigger bị đẩy dọc theo hướng băng tải.
    /// Dịch vị trí trực tiếp mỗi bước vật lý nên cộng thêm vào chuyển động tự điều khiển (không bị PlayerController ghi đè vận tốc).
    /// Dịch nhẹ (mặc định 1,4 ô/giây, chậm hơn nhiều so với tốc độ chạy) để người chơi vẫn đi ngược được.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ConveyorBelt : MonoBehaviour
    {
        [SerializeField] private Vector2 direction = Vector2.right;
        [SerializeField, Min(0f)] private float speed = 1.4f;

        private readonly HashSet<Rigidbody2D> _bodies = new HashSet<Rigidbody2D>();
        private readonly List<Rigidbody2D> _scratch = new List<Rigidbody2D>();

        public Vector2 Direction => direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        private void Reset()
        {
            var box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
        }

        private void OnDisable()
        {
            _bodies.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic) _bodies.Add(body);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (body != null) _bodies.Remove(body);
        }

        private void FixedUpdate()
        {
            if (_bodies.Count == 0 || speed <= 0f) return;
            Vector2 delta = Direction * (speed * Time.fixedDeltaTime);
            _scratch.Clear();
            _scratch.AddRange(_bodies);
            for (int i = 0; i < _scratch.Count; i++)
            {
                var body = _scratch[i];
                if (body == null || !body.gameObject.activeInHierarchy) { _bodies.Remove(body); continue; }
                body.position += delta;
            }
        }
    }
}
