using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Combat
{
    /// <summary>
    /// Truy vấn collider quanh điểm đánh rồi lọc ra IDamageable.
    /// Dùng buffer tái sử dụng (không cấp phát mỗi đòn đánh — tránh GC trên Android).
    /// </summary>
    public class HitDetection
    {
        private readonly List<Collider2D> _colliders = new List<Collider2D>(16);
        private readonly List<IDamageable> _targets = new List<IDamageable>(8);
        private ContactFilter2D _filter;
        private bool _filterReady;

        public IReadOnlyList<IDamageable> FindTargets(Vector2 center, float radius, LayerMask layerMask)
        {
            _targets.Clear();
            if (radius <= 0f) return _targets;

            if (!_filterReady)
            {
                _filter = new ContactFilter2D { useTriggers = true };
                _filterReady = true;
            }
            _filter.SetLayerMask(layerMask);

            _colliders.Clear();
            int count = Physics2D.OverlapCircle(center, radius, _filter, _colliders);
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = _colliders[i];
                if (collider == null) continue;

                IDamageable target = collider.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead) continue;
                if (!_targets.Contains(target)) _targets.Add(target);
            }
            return _targets;
        }
    }
}
