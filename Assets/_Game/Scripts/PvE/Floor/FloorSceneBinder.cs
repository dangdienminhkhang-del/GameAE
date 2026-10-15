using System;
using System.Collections.Generic;
using Game.Core.Floor;
using UnityEngine;

namespace Game.PvE.Floor
{
    /// <summary>
    /// Gắn nội dung một tầng (quái, NPC, boss, Safe Zone...) vào khung chuyển tầng chung.
    ///
    /// Đặt component này trên một object LUÔN BẬT; nội dung tầng nằm dưới <see cref="contentRoot"/>
    /// (object con được bật/tắt). Binder tự đăng ký vào danh sách static trong Awake và gỡ trong OnDestroy,
    /// nên vẫn tìm được tầng dù contentRoot đang tắt.
    ///
    /// Không có logic theo số tầng cụ thể: binder chỉ biết floorId (dữ liệu) của chính nó.
    /// </summary>
    public class FloorSceneBinder : MonoBehaviour
    {
        private static readonly List<FloorSceneBinder> Registered = new List<FloorSceneBinder>();

        [Tooltip("ID của FloorDefinition mà binder này phục vụ (trùng FloorDefinition.id).")]
        [SerializeField] private string floorId;

        [Tooltip("Object con chứa toàn bộ nội dung tầng — sẽ được bật/tắt. Không được là object chứa binder này.")]
        [SerializeField] private GameObject contentRoot;

        [Tooltip("Điểm đặt người chơi khi vào tầng.")]
        [SerializeField] private Transform entryPoint;

        [Tooltip("Tùy chọn: registrar đọc FloorDefinition và nạp danh sách quái cho spawner.")]
        [SerializeField] private FloorSpawnRegistrar floorSpawnRegistrar;

        public string FloorId => floorId;
        public GameObject ContentRoot => contentRoot;
        public Transform EntryPoint => entryPoint;

        /// <summary>Nội dung tầng đang bật.</summary>
        public bool IsActive => contentRoot != null && contentRoot.activeSelf;

        /// <summary>Mọi binder đang đăng ký (cả tầng đang tắt).</summary>
        public static IReadOnlyList<FloorSceneBinder> All => Registered;

        // ── Danh sách static ────────────────────────────────────────────────

        public static void Register(FloorSceneBinder binder)
        {
            if (binder == null || Registered.Contains(binder)) return;
            Registered.Add(binder);
        }

        public static void Unregister(FloorSceneBinder binder)
        {
            Registered.Remove(binder);
        }

        // Domain Reload có thể tắt (Enter Play Mode Options) → xóa danh sách cũ khi vào Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Registered.Clear();
        }

        private void Awake()
        {
            Register(this);
        }

        private void OnDestroy()
        {
            Unregister(this);
        }

        /// <summary>Gán dữ liệu bằng code (test hoặc bootstrap). Scene thường gán qua Inspector.</summary>
        public void Configure(string id, GameObject root, Transform entry, FloorSpawnRegistrar registrar)
        {
            floorId = id;
            contentRoot = root;
            entryPoint = entry;
            floorSpawnRegistrar = registrar;
        }

        // ── Bật / tắt tầng ──────────────────────────────────────────────────

        /// <summary>Bật nội dung → nạp dữ liệu spawn (nếu có registrar) → thông báo listener trong nội dung.</summary>
        public void Activate(FloorDefinition def)
        {
            if (contentRoot != null && !contentRoot.activeSelf) contentRoot.SetActive(true);
            if (floorSpawnRegistrar != null) floorSpawnRegistrar.Register(def);
            NotifyActivated(CollectListeners(), def);
        }

        /// <summary>Thông báo listener rồi tắt nội dung.</summary>
        public void Deactivate()
        {
            if (contentRoot == null) return;
            NotifyDeactivated(CollectListeners());
            contentRoot.SetActive(false);
        }

        /// <summary>Mọi listener trong nội dung tầng, kể cả object đang tắt (boss đã gục, spawner tắt riêng).</summary>
        private IFloorActivationListener[] CollectListeners()
        {
            if (contentRoot == null) return Array.Empty<IFloorActivationListener>();
            return contentRoot.GetComponentsInChildren<IFloorActivationListener>(true);
        }

        // ── Thông báo (tách riêng để test được bằng listener thường) ────────

        /// <summary>Thông báo OnFloorActivated cho từng listener; một listener lỗi không chặn các listener còn lại.</summary>
        public static void NotifyActivated(IEnumerable<IFloorActivationListener> listeners, FloorDefinition def)
        {
            NotifyEach(listeners, listener => listener.OnFloorActivated(def));
        }

        /// <summary>Thông báo OnFloorDeactivated cho từng listener; một listener lỗi không chặn các listener còn lại.</summary>
        public static void NotifyDeactivated(IEnumerable<IFloorActivationListener> listeners)
        {
            NotifyEach(listeners, listener => listener.OnFloorDeactivated());
        }

        private static void NotifyEach(IEnumerable<IFloorActivationListener> listeners, Action<IFloorActivationListener> call)
        {
            if (listeners == null) return;

            foreach (var listener in listeners)
            {
                if (listener == null) continue;
                try
                {
                    call(listener);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
