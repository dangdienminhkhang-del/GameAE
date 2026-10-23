using Game.Core.Events;
using UnityEngine;
using CameraFollow = Game.PvE.Camera.CameraFollow;

namespace Game.PvE.Zones
{
    /// <summary>
    /// Đổi bounds của camera theo vùng người chơi vừa vào (Phase 9.7a). Chuyển mượt khoảng 0.4 giây (thời gian thật,
    /// nên dừng hình không làm lệch); khi vừa sang tầng khác (bounds rời nhau) thì đặt ngay, không quét qua thế giới.
    /// Toán nằm ở <see cref="CameraBoundsMath"/>; việc kẹp thật sự do CameraFollow làm.
    /// </summary>
    public sealed class ZoneCameraController : MonoBehaviour
    {
        [Tooltip("Camera cần đổi bounds. Để trống = tự tìm.")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private float transitionSeconds = 0.4f;

        private bool _hasPrevious;
        private Vector2 _previousMin;
        private Vector2 _previousMax;

        private void OnEnable()
        {
            GameEvents.OnZoneEntered += HandleZoneEntered;
        }

        private void OnDisable()
        {
            GameEvents.OnZoneEntered -= HandleZoneEntered;
        }

        private void HandleZoneEntered(object sender, ZoneEnteredArgs args)
        {
            if (args == null) return;
            ZoneVolume zone = ZoneVolume.Find(args.ZoneId);
            if (zone == null) return;

            if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow>();
            if (cameraFollow == null) return;

            Bounds bounds = zone.CameraBounds;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f) return;

            Vector2 min = new Vector2(bounds.min.x, bounds.min.y);
            Vector2 max = new Vector2(bounds.max.x, bounds.max.y);

            if (CameraBoundsMath.ShouldSnap(_hasPrevious, _previousMin, _previousMax, min, max))
            {
                cameraFollow.SetBounds(bounds);
                cameraFollow.SnapToTarget();
            }
            else
            {
                cameraFollow.SetBoundsSmooth(bounds, transitionSeconds);
            }

            _hasPrevious = true;
            _previousMin = min;
            _previousMax = max;
        }
    }
}
