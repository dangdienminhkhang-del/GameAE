using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Gắn vào NPC/quái để được xếp theo trục Y cùng thế giới (xem <see cref="WorldYSort"/>).
    /// Phải nằm trong file riêng cùng tên: Unity chỉ gắn được MonoBehaviour khi class nằm trong file trùng tên class,
    /// nếu để chung file với WorldYSort thì component trên prefab bị "script missing".
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class YSortAgent : MonoBehaviour
    {
        private SpriteRenderer _sr;

        private void OnEnable()
        {
            TryGetComponent(out _sr);
            WorldYSort.Register(_sr);
        }

        private void OnDisable()
        {
            WorldYSort.Unregister(_sr);
        }
    }
}
