using Game.Core.Character;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Vùng an toàn kiểm tra theo VỊ TRÍ (không dùng cờ bool) — bug cũ: 2 vùng kề nhau hoặc teleport
    /// làm cờ sai, player vô nhiễm vĩnh viễn.
    /// </summary>
    public class SafeZoneServiceTests
    {
        [Test]
        public void IsSafeZoneAt_WithNoZone_IsFalse()
        {
            var service = new SafeZoneService();

            Assert.IsFalse(service.IsSafeZoneAt(Vector2.zero));
        }

        [Test]
        public void IsSafeZoneAt_InsideRegisteredZone_IsTrue_OutsideIsFalse()
        {
            var zone = new GameObject("Zone");
            Collider2D collider = zone.AddComponent<BoxCollider2D>();
            ((BoxCollider2D)collider).size = new Vector2(4f, 4f);
            zone.transform.position = new Vector3(10f, 5f, 0f);
            Physics2D.SyncTransforms();

            var service = new SafeZoneService();
            service.Register(collider);

            try
            {
                Assert.IsTrue(service.IsSafeZoneAt(new Vector2(10f, 5f)), "trong vùng");
                Assert.IsFalse(service.IsSafeZoneAt(new Vector2(0f, 0f)), "ngoài vùng");
                Assert.AreEqual(1, service.ZoneCount);
            }
            finally
            {
                Object.DestroyImmediate(zone);
            }
        }

        [Test]
        public void Unregister_RemovesZone()
        {
            var zone = new GameObject("Zone");
            Collider2D collider = zone.AddComponent<BoxCollider2D>();
            zone.transform.position = Vector3.zero;
            Physics2D.SyncTransforms();

            var service = new SafeZoneService();
            service.Register(collider);
            service.Unregister(collider);

            try
            {
                Assert.AreEqual(0, service.ZoneCount);
                Assert.IsFalse(service.IsSafeZoneAt(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(zone);
            }
        }

        [Test]
        public void TwoAdjacentZones_LeavingOneStaysSafeInTheOther()
        {
            var first = new GameObject("ZoneA");
            Collider2D a = first.AddComponent<BoxCollider2D>();
            ((BoxCollider2D)a).size = new Vector2(4f, 4f);
            first.transform.position = new Vector3(0f, 0f, 0f);

            var second = new GameObject("ZoneB");
            Collider2D b = second.AddComponent<BoxCollider2D>();
            ((BoxCollider2D)b).size = new Vector2(4f, 4f);
            second.transform.position = new Vector3(3.5f, 0f, 0f);
            Physics2D.SyncTransforms();

            var service = new SafeZoneService();
            service.Register(a);
            service.Register(b);

            try
            {
                Assert.IsTrue(service.IsSafeZoneAt(new Vector2(3.5f, 0f)), "vẫn trong vùng B");
                Assert.IsFalse(service.IsSafeZoneAt(new Vector2(10f, 0f)), "xa cả hai vùng");
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void Reset_ClearsAllZones()
        {
            var zone = new GameObject("Zone");
            Collider2D collider = zone.AddComponent<BoxCollider2D>();
            var service = new SafeZoneService();
            service.Register(collider);

            service.Reset();

            try
            {
                Assert.AreEqual(0, service.ZoneCount);
            }
            finally
            {
                Object.DestroyImmediate(zone);
            }
        }
    }
}
