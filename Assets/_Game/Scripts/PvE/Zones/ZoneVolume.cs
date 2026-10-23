using System.Collections.Generic;
using Game.Core.Floor;
using UnityEngine;

namespace Game.PvE.Zones
{
    /// <summary>
    /// Một vùng của tầng (Thị trấn, Vùng Farm, Hành lang boss, Phòng boss...). Đặt trên object có Collider2D trigger.
    /// Chỉ là DỮ LIỆU + câu hỏi "điểm này có trong vùng không": việc theo dõi người chơi do <see cref="ZoneTracker"/> làm.
    /// Không biết tầng hay quái cụ thể; ID theo quy ước ổn định dạng zone.&lt;tầng&gt;_&lt;vùng&gt; do designer đặt (docs/03 §2).
    /// Nằm trong nội dung tầng (bị bật/tắt cùng tầng): vùng của tầng đang tắt tự rời danh sách.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ZoneVolume : MonoBehaviour, IFloorActivationListener
    {
        private static readonly List<ZoneVolume> Registered = new List<ZoneVolume>();

        /// <summary>Mọi vùng đang bật.</summary>
        public static IReadOnlyList<ZoneVolume> All => Registered;

        [SerializeField] private string zoneId = "zone.new";
        [SerializeField] private string displayName = "Vùng";
        [SerializeField] private ZoneKind kind = ZoneKind.Farm;
        [Tooltip("Vùng chồng nhau: priority CAO hơn thắng; bằng nhau thì vùng NHỎ hơn thắng.")]
        [SerializeField] private int priority;
        [Tooltip("ID nhạc phát khi vào vùng (xem AudioIds). Để trống = giữ nhạc hiện tại.")]
        [SerializeField] private string musicId;
        [Tooltip("Collider2D định bounds camera cho vùng này. Để trống = dùng chính collider của vùng.")]
        [SerializeField] private Collider2D cameraBoundsCollider;

        private Collider2D _collider;

        public string ZoneId => zoneId;
        public string DisplayName => displayName;
        public ZoneKind Kind => kind;
        public int Priority => priority;
        public string MusicId => musicId;

        /// <summary>Bounds thế giới dùng cho camera khi người chơi ở vùng này.</summary>
        public Bounds CameraBounds
        {
            get
            {
                Collider2D source = cameraBoundsCollider != null ? cameraBoundsCollider : ResolveCollider();
                return source != null ? source.bounds : default;
            }
        }

        /// <summary>Diện tích (AABB) của vùng, dùng để chọn vùng nhỏ hơn khi chồng nhau.</summary>
        public float Area
        {
            get
            {
                Collider2D c = ResolveCollider();
                if (c == null) return float.MaxValue;
                Vector3 size = c.bounds.size;
                return size.x * size.y;
            }
        }

        /// <summary>Điểm có nằm trong vùng không (dùng Collider2D.OverlapPoint, không cấp phát).</summary>
        public bool Contains(Vector2 point)
        {
            Collider2D c = ResolveCollider();
            return c != null && c.enabled && c.OverlapPoint(point);
        }

        /// <summary>Gán dữ liệu bằng code (test hoặc công cụ Editor). Scene thường gán qua Inspector.</summary>
        public void Configure(string id, string display, ZoneKind zoneKind, int zonePriority, string music)
        {
            zoneId = id;
            displayName = display;
            kind = zoneKind;
            priority = zonePriority;
            musicId = music;
        }

        /// <summary>Tìm vùng đang bật theo ID; null nếu không có.</summary>
        public static ZoneVolume Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Registered.Count; i++)
            {
                ZoneVolume v = Registered[i];
                if (v != null && string.Equals(v.zoneId, id, System.StringComparison.Ordinal)) return v;
            }
            return null;
        }

        private Collider2D ResolveCollider()
        {
            if (_collider == null) TryGetComponent(out _collider);
            return _collider;
        }

        private void OnEnable()
        {
            if (!Registered.Contains(this)) Registered.Add(this);
        }

        private void OnDisable()
        {
            Registered.Remove(this);
        }

        private void Reset()
        {
            if (TryGetComponent(out Collider2D c)) c.isTrigger = true;
        }

        // Vào (hoặc vào lại) tầng: nhờ ZoneTracker đánh giá lại NGAY ở nhịp kế tiếp (sau khi người chơi được đặt ở điểm vào)
        // để banner, nhạc và camera đúng ngay từ đầu.
        public void OnFloorActivated(FloorDefinition def)
        {
            ZoneTracker.RequestImmediateEvaluation();
        }

        public void OnFloorDeactivated() { }

        // Enter Play Mode Options có thể tắt Domain Reload: bỏ danh sách cũ khi vào Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Registered.Clear();
        }
    }
}
