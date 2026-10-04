using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Character
{
    /// <summary>
    /// Vùng an toàn (Trụ Đèn) — kiểm tra theo VỊ TRÍ tại thời điểm bị đánh, không dùng cờ bool.
    /// Lý do: cờ bool sai khi 2 vùng kề nhau, hoặc khi object bị teleport/tắt (mất sự kiện exit).
    /// Dùng AABB (bounds.Contains) để chạy được cả trong EditMode test, không cần physics world.
    /// </summary>
    public class SafeZoneService
    {
        private readonly List<Collider2D> _zones = new List<Collider2D>();

        public int ZoneCount => _zones.Count;

        public void Register(Collider2D zone)
        {
            if (zone == null) return;
            if (!_zones.Contains(zone)) _zones.Add(zone);
        }

        public void Unregister(Collider2D zone)
        {
            if (zone == null) return;
            _zones.Remove(zone);
        }

        /// <summary>Điểm có nằm trong vùng an toàn nào không?</summary>
        public bool IsSafeZoneAt(Vector2 point)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                Collider2D zone = _zones[i];
                if (zone == null || !zone.enabled) continue;
                if (zone.bounds.Contains(new Vector3(point.x, point.y, zone.bounds.center.z))) return true;
            }
            return false;
        }

        public void Reset()
        {
            _zones.Clear();
        }
    }
}
