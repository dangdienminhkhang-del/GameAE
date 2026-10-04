using UnityEngine;

namespace Game.PvE.SafeZone
{
    /// <summary>
    /// Vùng an toàn quanh "Trụ Đèn" (docs/09 §1.3, docs/11 §2): player đứng trong vùng thì không nhận damage.
    /// Đăng ký collider của mình vào SafeZoneService; việc kiểm tra diễn ra lúc bị đánh (theo vị trí).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SafeZoneTrigger : MonoBehaviour
    {
        [SerializeField] private bool debugLog = false;

        private Collider2D _zone;

        private void Awake()
        {
            _zone = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            SafeZoneProvider.GetOrCreate().Register(_zone);
            if (debugLog) Debug.Log("[SafeZone] đăng ký vùng an toàn", this);
        }

        private void OnDisable()
        {
            SafeZoneProvider.GetOrCreate().Unregister(_zone);
        }
    }
}
