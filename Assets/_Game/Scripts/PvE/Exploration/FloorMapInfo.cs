using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.PvE.Exploration
{
    /// <summary>
    /// Mô tả bản đồ của MỘT tầng: id tầng, giới hạn thế giới (để chia khối sương) và hai tilemap dùng vẽ bản đồ.
    /// Đặt trên Content của tầng. <see cref="Current"/> là tầng đang bật (Content chỉ bật khi đang ở tầng đó).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloorMapInfo : MonoBehaviour
    {
        [SerializeField] private string floorId = "";
        [SerializeField] private Rect worldBounds = new Rect(0, 0, 64, 64);
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap wallsTilemap;
        [Tooltip("BẬT cho tầng có đất là hình vẽ sẵn (không có tilemap sàn): ô KHÔNG có tile ở wallsTilemap (khối chặn) được coi là đi được.")]
        [SerializeField] private bool groundIsOpen;

        public static FloorMapInfo Current { get; private set; }

        public string FloorId => floorId;
        public Rect WorldBounds => worldBounds;
        public Tilemap FloorTilemap => floorTilemap;
        public Tilemap WallsTilemap => wallsTilemap;
        public bool GroundIsOpen => groundIsOpen;

        private void OnEnable() { Current = this; }

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }
    }
}
