using System.Collections;
using UnityEngine;

namespace Game.PvE.Zones
{
    /// <summary>
    /// Bật/tắt theo khoảng cách các nhóm nội dung của một tầng lớn (spawner, quái, đồ trang trí, hazard).
    /// Mỗi nhóm gắn với một hình chữ nhật thế giới (thường là AABB của khu). Khi người chơi (camera chính)
    /// đứng cách hình chữ nhật đó quá <see cref="deactivateDistance"/> thì nhóm bị tắt; vào trong
    /// <see cref="activateDistance"/> thì bật lại. Có hysteresis để không nhấp nháy ở biên.
    ///
    /// Chỉ là tối ưu hiệu năng: không đổi luật chơi. Quái trong nhóm bị tắt đứng yên (không AI, không render)
    /// và tiếp tục khi nhóm bật lại. Zone, Safe Zone, NPC, boss KHÔNG nên nằm trong nhóm được stream.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloorZoneStreamer : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Group
        {
            public string name;
            public GameObject root;
            public Rect area;
        }

        [SerializeField] private Group[] groups = new Group[0];
        [SerializeField, Min(1f)] private float activateDistance = 45f;
        [SerializeField, Min(1f)] private float deactivateDistance = 58f;
        [SerializeField, Min(0.1f)] private float intervalSeconds = 0.4f;
        [Tooltip("Chờ sau khi tầng bật trước lần đánh giá đầu, để FloorActivation kịp đặt người chơi vào điểm vào.")]
        [SerializeField, Min(0f)] private float firstEvaluationDelay = 0.3f;

        private Coroutine _loop;

        public int GroupCount => groups != null ? groups.Length : 0;

        private void OnEnable()
        {
            _loop = StartCoroutine(Loop());
        }

        private void OnDisable()
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }
        }

        private IEnumerator Loop()
        {
            yield return new WaitForSecondsRealtime(firstEvaluationDelay);
            var wait = new WaitForSecondsRealtime(intervalSeconds);
            while (true)
            {
                Evaluate();
                yield return wait;
            }
        }

        /// <summary>Đánh giá ngay một lần (public để test/EditMode gọi được).</summary>
        public void Evaluate()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null || groups == null) return;
            Vector2 p = cam.transform.position;
            Evaluate(p);
        }

        public void Evaluate(Vector2 point)
        {
            if (groups == null) return;
            for (int i = 0; i < groups.Length; i++)
            {
                var g = groups[i];
                if (g == null || g.root == null) continue;
                float d = DistanceToRect(point, g.area);
                bool active = g.root.activeSelf;
                if (active && d > deactivateDistance) g.root.SetActive(false);
                else if (!active && d <= activateDistance) g.root.SetActive(true);
            }
        }

        public static float DistanceToRect(Vector2 p, Rect r)
        {
            float dx = Mathf.Max(Mathf.Max(r.xMin - p.x, 0f), p.x - r.xMax);
            float dy = Mathf.Max(Mathf.Max(r.yMin - p.y, 0f), p.y - r.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
