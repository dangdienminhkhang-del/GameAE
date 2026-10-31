using System.Collections;
using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;

namespace Game.PvE.Exploration
{
    /// <summary>
    /// Đèn Hiệu (cổng dịch chuyển kiểu SAO). Đặt trên cột đèn của Vùng An Toàn. Người chơi tới gần lần đầu thì đèn được thắp
    /// (lưu vào Player Data "beacons"); đèn đã thắp có thể chọn làm đích dịch chuyển nhanh trên bản đồ.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BeaconWaypoint : MonoBehaviour
    {
        [SerializeField] private string beaconId = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private string floorId = "";
        [Tooltip("Điểm người chơi xuất hiện khi dịch chuyển tới, tính từ vị trí đèn.")]
        [SerializeField] private Vector2 arrivalOffset = new Vector2(0f, -1.8f);
        [SerializeField, Min(0.5f)] private float lightRadius = 3.5f;

        private static readonly List<BeaconWaypoint> s_all = new List<BeaconWaypoint>();
        private Transform _player;

        public static IReadOnlyList<BeaconWaypoint> All => s_all;

        public string BeaconId => beaconId;
        public string DisplayName => displayName;
        public string FloorId => floorId;
        public Vector2 ArrivalPoint => (Vector2)transform.position + arrivalOffset;
        public bool IsLit => MapServices.Progress.IsLit(beaconId);

        private void OnEnable()
        {
            if (!s_all.Contains(this)) s_all.Add(this);
            StartCoroutine(Loop());
        }

        private void OnDisable()
        {
            s_all.Remove(this);
        }

        private IEnumerator Loop()
        {
            var wait = new WaitForSeconds(0.4f);
            while (true)
            {
                if (_player == null) _player = MapExplorer.FindPlayer();
                if (_player != null && !IsLit &&
                    Vector2.Distance(_player.position, transform.position) <= lightRadius &&
                    MapServices.Progress.Light(beaconId))
                {
                    GameEvents.RaiseCombatNotice($"Đã thắp Đèn Hiệu: {displayName}", new Color(1f, 0.85f, 0.35f));
                    _ = MapServices.Progress.SaveIfDirtyAsync();
                }
                yield return wait;
            }
        }
    }
}
