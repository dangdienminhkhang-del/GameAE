using System.Collections;
using Game.Core.World;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Exploration
{
    /// <summary>
    /// Vẽ bản đồ khi người chơi đi (Sổ Hải Đồ). Mỗi ~0.4 giây vẽ các khối trong bán kính <see cref="revealRadius"/> quanh người chơi,
    /// lưu định kỳ và khi tắt tầng / tạm dừng ứng dụng. Đặt cạnh <see cref="FloorMapInfo"/> trên Content của tầng.
    /// </summary>
    [RequireComponent(typeof(FloorMapInfo))]
    [DisallowMultipleComponent]
    public sealed class MapExplorer : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float revealRadius = 12f;
        [SerializeField, Min(0.1f)] private float intervalSeconds = 0.4f;
        [SerializeField, Min(1f)] private float saveIntervalSeconds = 10f;

        private FloorMapInfo _info;
        private Transform _player;
        private float _sinceSave;

        public MapRevealGrid Grid { get; private set; }
        public FloorMapInfo Info => _info;

        public static MapExplorer Active { get; private set; }

        public static Transform FindPlayer()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            return pc != null ? pc.transform : null;
        }

        private void OnEnable()
        {
            _info = GetComponent<FloorMapInfo>();
            var b = _info.WorldBounds;
            Grid = MapServices.Progress.GetGrid(_info.FloorId, b.xMin, b.yMin, b.width, b.height);
            Active = this;
            _sinceSave = 0f;
            StartCoroutine(Loop());
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
            _ = MapServices.Progress.SaveIfDirtyAsync();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) _ = MapServices.Progress.SaveIfDirtyAsync();
        }

        private IEnumerator Loop()
        {
            var wait = new WaitForSeconds(intervalSeconds);
            while (true)
            {
                if (_player == null) _player = FindPlayer();
                if (_player != null)
                {
                    var p = _player.position;
                    MapServices.Progress.Reveal(_info.FloorId, p.x, p.y, revealRadius);
                }

                _sinceSave += intervalSeconds;
                if (_sinceSave >= saveIntervalSeconds)
                {
                    _sinceSave = 0f;
                    _ = MapServices.Progress.SaveIfDirtyAsync();
                }
                yield return wait;
            }
        }
    }
}
