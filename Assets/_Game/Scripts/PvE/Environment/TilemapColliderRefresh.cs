using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Làm mới collider của Tilemap chặn đường khi vào màn.
    /// CompositeCollider2D có thể giữ hình dạng cũ (map trước khi vẽ lại) sau khi tải scene,
    /// khiến chỗ đi được bị chặn còn chỗ nước/rừng lại hở. Bật tắt lại TilemapCollider2D
    /// và dựng lại geometry để khớp 100% với các ô trên Tilemap.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class TilemapColliderRefresh : MonoBehaviour
    {
        private void Awake() { Refresh(); }
        private void Start() { Refresh(); }

        [ContextMenu("Refresh Collider")]
        public void Refresh()
        {
            if (!TryGetComponent(out TilemapCollider2D tc)) return;
            tc.enabled = false;
            tc.enabled = true;
            tc.ProcessTilemapChanges();
            if (TryGetComponent(out CompositeCollider2D comp)) comp.GenerateGeometry();
            Physics2D.SyncTransforms();
        }
    }
}
