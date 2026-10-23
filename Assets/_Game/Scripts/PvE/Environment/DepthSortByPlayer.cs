using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Sắp xếp độ sâu theo người chơi: vật thể ở phía sau (y lớn hơn) nhân vật thì vẽ dưới,
    /// ở phía trước (y nhỏ hơn) thì vẽ đè lên. Dùng cho cây và bụi.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class DepthSortByPlayer : MonoBehaviour
    {
        [SerializeField] private float sortOffsetY = 0f;
        private SpriteRenderer _self;
        private SpriteRenderer _player;
        private float _lookup;

        private void Awake() { TryGetComponent(out _self); }

        private void LateUpdate()
        {
            if (_player == null)
            {
                _lookup -= Time.deltaTime;
                if (_lookup > 0f) return;
                _lookup = 1f;
                var pc = FindAnyObjectByType<Game.PvE.Player.PlayerController>();
                if (pc != null) _player = pc.GetComponentInChildren<SpriteRenderer>();
                if (_player == null) return;
            }
            bool inFront = _player.transform.position.y > transform.position.y + sortOffsetY;
            _self.sortingOrder = _player.sortingOrder + (inFront ? 2 : -2);
        }
    }
}
