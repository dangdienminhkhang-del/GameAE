using System;
using System.Threading.Tasks;
using Game.Core.Network;
using Game.Core.Time;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Phase 6 — Time System local rate tests (docs/08 §2.2)
    /// + Wave 2B — Authoritative Time tests (docs/08 §3): snapshot, revision, drift, stale, authoritative.
    /// </summary>
    public class TimeServiceTests
    {
        // ---------- Helpers ----------
        private static TimeConfig MakeConfig(float secondsPerDay, float startHour,
            int maxAgeSeconds = 900, float materialDrift = 0.25f)
        {
            var config = UnityEngine.ScriptableObject.CreateInstance<TimeConfig>();
            var so = new UnityEditor.SerializedObject(config);
            so.FindProperty("realSecondsPerGameDay").floatValue = secondsPerDay;
            so.FindProperty("startHour").floatValue = startHour;
            so.FindProperty("maxSnapshotAgeSeconds").intValue = maxAgeSeconds;
            so.FindProperty("materialDriftGameHours").floatValue = materialDrift;
            so.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static WorldTimeSnapshot MakeSnapshot(double gameHours, long anchorUnix,
            long revision = 1L, double rate = 1200.0, int cycleVer = 1)
        {
            return new WorldTimeSnapshot
            {
                schemaVersion = WorldTimeSnapshot.CurrentSchemaVersion,
                gameTimeHoursAtAnchor = gameHours,
                anchorUnixSeconds = anchorUnix,
                revision = revision,
                cycleConfigVersion = cycleVer,
                realSecondsPerGameDay = rate,
            };
        }

        private class FakeServerFunctions : IServerFunctionService
        {
            public WorldTimeSnapshot NextSnapshot;
            public int ReportCallCount;
            public Func<WorldTimeSnapshot> Factory;
            public bool ThrowOnGet;

            public Task ReportBossDefeatedAsync(string bossId, string floorId)
            {
                ReportCallCount++;
                return Task.CompletedTask;
            }

            public Task<WorldTimeSnapshot> GetWorldTimeSnapshotAsync()
            {
                if (ThrowOnGet) throw new InvalidOperationException("server down");
                return Task.FromResult(Factory != null ? Factory() : NextSnapshot);
            }
        }

        // ========================================================
        // Wave 2B — WorldTimeSnapshot: offset, newer/equivalent, stale
        // ========================================================

        [Test]
        public void Snapshot_PredictGameHours_UsesSnapshotRate_Not24HourClock()
        {
            long anchor = 1_700_000_000;
            var snap = MakeSnapshot(8.0, anchor, rate: 1200.0); // 1 game day = 1200 real s → 1 real s = 0.02 h
            // +300 real s = 6 game hours
            double predicted = snap.PredictGameHours(anchor + 300);
            Assert.AreEqual(14.0, predicted, 0.0001);
        }

        [Test]
        public void Snapshot_PredictGameHours_WrapsPastMidnight()
        {
            long anchor = 1_700_000_000;
            var snap = MakeSnapshot(22.0, anchor, rate: 1200.0);
            // +300s = +6h → 28h mod 24 = 4h
            double predicted = snap.PredictGameHours(anchor + 300);
            Assert.AreEqual(4.0, predicted, 0.0001);
        }

        [Test]
        public void Snapshot_PredictGameHours_BeforeAnchor_ClampsToAnchorValue()
        {
            long anchor = 1_700_000_000;
            var snap = MakeSnapshot(10.0, anchor);
            double predicted = snap.PredictGameHours(anchor - 60);
            Assert.AreEqual(10.0, predicted, 0.0001);
        }

        [Test]
        public void Snapshot_IsNewerOrEquivalent_HigherRevision_Wins()
        {
            long t = 1_700_000_000;
            var current = MakeSnapshot(8.0, t, revision: 3L);
            var newer = MakeSnapshot(9.0, t, revision: 4L);
            Assert.IsTrue(WorldTimeSnapshot.IsNewerOrEquivalent(current, newer));
        }

        [Test]
        public void Snapshot_IsNewerOrEquivalent_SameRevision_OlderAnchor_Rejected()
        {
            long t = 1_700_000_000;
            var current = MakeSnapshot(8.0, t + 300, revision: 3L);
            var stale = MakeSnapshot(7.0, t, revision: 3L); // anchor cũ hơn
            Assert.IsFalse(WorldTimeSnapshot.IsNewerOrEquivalent(current, stale));
        }

        [Test]
        public void Snapshot_IsNewerOrEquivalent_SameRevision_SameAnchor_Accepted()
        {
            long t = 1_700_000_000;
            var current = MakeSnapshot(8.0, t, revision: 3L);
            var same = MakeSnapshot(8.0, t, revision: 3L);
            Assert.IsTrue(WorldTimeSnapshot.IsNewerOrEquivalent(current, same));
        }

        [Test]
        public void Snapshot_IsStale_RespectsMaxAgeSeconds()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var snap = MakeSnapshot(8.0, now - 1000); // age ~1000s
            Assert.IsTrue(snap.IsStale(now, 900));  // 1000 > 900 → stale
            Assert.IsFalse(snap.IsStale(now, 1200)); // 1000 < 1200 → ok
        }

        [Test]
        public void Snapshot_CycleConfigVersion_HigherThanSupported_NotSupported()
        {
            var snap = MakeSnapshot(8.0, 1_700_000_000, cycleVer: 99);
            Assert.IsFalse(snap.CycleConfigSupported);
        }

        // ========================================================
        // Wave 2B — TimeService: Resync + Revision validation
        // ========================================================

        [Test]
        public async Task ResyncAsync_WithoutServerFunctions_ReturnsFalse_AndNotAuthoritative()
        {
            var config = MakeConfig(1200f, 8f);
            var service = new Game.Services.Time.TimeService(config, null);
            try
            {
                await service.InitializeAsync();
                bool ok = await service.ResyncAsync();
                Assert.IsFalse(ok);
                Assert.IsFalse(service.IsAuthoritative);
                Assert.IsNull(service.CurrentSnapshot);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task InitializeAsync_WithServerFunctions_LoadsStartupSnapshot_IsAuthoritative()
        {
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            fake.NextSnapshot = MakeSnapshot(12.0, now, revision: 5L);

            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();

                Assert.IsNotNull(service.CurrentSnapshot);
                Assert.AreEqual(5L, service.CurrentSnapshot.revision);
                // predicted = 12.0 (vì anchor = now nên elapsed ~0)
                Assert.AreEqual(12.0, service.CurrentGameTimeHours, 0.01);
                Assert.IsTrue(service.IsAuthoritative);
                Assert.AreEqual(0, service.SnapshotAgeSeconds); // mới apply, tuổi rất nhỏ
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_OldRevisionCandidate_Rejected_NoEvent()
        {
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            WorldTimeSnapshot fired = null;

            fake.NextSnapshot = MakeSnapshot(8.0, now, revision: 5L);
            var service = new Game.Services.Time.TimeService(config, fake);
            service.WorldTimeResynced += (s, snap) => fired = snap;
            try
            {
                await service.InitializeAsync();
                long firstRev = service.CurrentSnapshot.revision;
                fired = null;

                // Gửi candidate revision thấp hơn (4 < 5)
                fake.NextSnapshot = MakeSnapshot(20.0, now + 10, revision: 4L);
                bool ok = await service.ResyncAsync();

                Assert.IsFalse(ok, "rejected revision nên ResyncAsync phải return false");
                Assert.IsNull(fired, "WorldTimeResynced KHÔNG được phát khi snapshot bị reject");
                Assert.AreEqual(5L, service.CurrentSnapshot.revision, "revision không đổi sau khi reject");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_SameRevision_OlderAnchor_Rejected()
        {
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            fake.NextSnapshot = MakeSnapshot(8.0, now + 1000, revision: 3L);
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                WorldTimeSnapshot first = service.CurrentSnapshot;

                // Candidate cùng revision nhưng anchor cũ hơn
                fake.NextSnapshot = MakeSnapshot(8.0, now, revision: 3L);
                bool ok = await service.ResyncAsync();

                Assert.IsFalse(ok);
                Assert.AreSame(first, service.CurrentSnapshot);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_ServerThrows_FailsGracefully_IsAuthoritativeFalse_NoException()
        {
            var config = MakeConfig(1200f, 8f);
            var fake = new FakeServerFunctions { ThrowOnGet = true };
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                Assert.DoesNotThrowAsync(async () => await service.InitializeAsync());
                Assert.IsFalse(service.IsAuthoritative);
                Assert.IsNull(service.CurrentSnapshot);
                // Fallback về local StartHour 8.0
                Assert.AreEqual(8.0, service.CurrentGameTimeHours, 0.0001);

                bool ok = await service.ResyncAsync();
                Assert.IsFalse(ok);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_ServerReturnsNull_NoFallback_IsAuthoritativeFalse_SnapshotNull()
        {
            var config = MakeConfig(1200f, 12f);
            var fake = new FakeServerFunctions();
            fake.NextSnapshot = null;
            fake.Factory = () => null;

            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();

                Assert.IsFalse(service.IsAuthoritative,
                    "khi server trả null, IsAuthoritative phải = false (không có fallback snapshot).");
                Assert.IsNull(service.CurrentSnapshot,
                    "CurrentSnapshot phải = null (LUẬT 08 §5: không tự bịa snapshot khi lỗi).");
                // Giờ vẫn dùng local StartHour cho presentation (tick local tiếp tục, không đóng băng).
                Assert.AreEqual(12.0, service.CurrentGameTimeHours, 0.0001);

                // Resync tiếp → cũng null → cũng false, không throw.
                bool ok = await service.ResyncAsync();
                Assert.IsFalse(ok);
                Assert.IsNull(service.CurrentSnapshot);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_AfterValidSnapshot_ResyncFailsNull_YetAuthoritative_WhenSnapshotYoung()
        {
            // maxSnapshotAgeSeconds rất lớn (3600 s) → snapshot còn trẻ → IsAuthoritative vẫn true
            var config = MakeConfig(1200f, 8f, maxAgeSeconds: 3600);
            var fake = new FakeServerFunctions();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Lần 1: snapshot tốt, revision=5
            fake.NextSnapshot = MakeSnapshot(8.0, now, revision: 5L);
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                Assert.IsTrue(service.IsAuthoritative, "snapshot đã được xác nhận ban đầu");
                Assert.AreEqual(5L, service.CurrentSnapshot.revision);

                // Tạo resync thất bại (candidate null)
                fake.NextSnapshot = null;
                fake.Factory = () => null;
                bool ok = await service.ResyncAsync();

                Assert.IsFalse(ok, "resync thất bại → return false");
                // Snapshot cũ giữ nguyên:
                Assert.AreEqual(5L, service.CurrentSnapshot.revision, "snapshot cũ KHÔNG bị xóa khi resync lỗi");
                Assert.IsTrue(service.IsAuthoritative,
                    "tuổi snapshot < MaxSnapshotAgeSeconds(3600s) → IsAuthoritative vẫn true (08 §5 hàng 2)");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_AfterValidSnapshot_ResyncFailsNull_AuthoritativeFalseWhenSnapshotStale()
        {
            // maxSnapshotAgeSeconds = 1s; sau khi init,chờ 2s → snapshot "quá tuổituổi → false
            var config = MakeConfig(1200f, 8f, maxAgeSeconds: 1);
            var fake = new FakeServerFunctions();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            fake.NextSnapshot = MakeSnapshot(10.0, now, revision: 2L);
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                Assert.IsTrue(service.IsAuthoritative);

                // chờ snapshot già hơn max (tuổi > 1 giây)
                await Task.Delay(2100);

                // Resync thất bại (server trả null)
                fake.NextSnapshot = null;
                fake.Factory = () => null;
                await service.ResyncAsync();

                Assert.IsFalse(service.IsAuthoritative,
                    "snapshot đã quá 1s → IsAuthoritative phải false dù có snapshot đã xác nhận trước đó");                // Snapshot instance vẫn còn (không bị xóa):
                Assert.IsNotNull(service.CurrentSnapshot, "snapshot instance vẫn còn lưu trong bộ nhớ");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_HigherCycleConfigVersion_FallsOutOfAuthoritative_NoCrash()
        {
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            fake.NextSnapshot = MakeSnapshot(8.0, now, cycleVer: 999); // unsupported version
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                Assert.IsNotNull(service.CurrentSnapshot);
                // CycleConfigSupported=false → IsAuthoritative=false
                Assert.IsFalse(service.IsAuthoritative);
                // Nhưng giờ vẫn được predicted để UI hiển thị (không crash)
                Assert.GreaterOrEqual(service.CurrentGameTimeHours, 0.0);
                Assert.Less(service.CurrentGameTimeHours, 24.0);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        // ========================================================
        // Wave 2B — Drift correction (docs/08 §3.3 + §5)
        // ========================================================

        [Test]
        public async Task ResyncAsync_SmallDrift_AppliesHalfSmoothCorrection()
        {
            var config = MakeConfig(1200f, 0f, materialDrift: 1.0f); // material drift = 1 giờ; test drift 0.5h (nhỏ hơn)
            var fake = new FakeServerFunctions();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            fake.NextSnapshot = MakeSnapshot(10.0, now);
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                double first = service.CurrentGameTimeHours;

                // Bây giờ cho candidate anchor mới hơn 1 chút, predicted = 10.5
                // (drift = +0.5, nhỏ hơn material 1.0 → smooth correction 50%)
                fake.Factory = () => MakeSnapshot(10.5, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                // Đặt _effectiveGameHours = first = 10.0
                await service.ResyncAsync();

                double expected = first + 0.5 * 0.5; // 10.0 + 0.25 = 10.25
                Assert.AreEqual(expected, service.CurrentGameTimeHours, 0.02,
                    "drift nhỏ phải được sửa 50% (smooth correction)");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_LargeDrift_SnapsImmediately_NoSmooth()
        {
            var config = MakeConfig(1200f, 0f, materialDrift: 0.25f);
            var fake = new FakeServerFunctions();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            fake.NextSnapshot = MakeSnapshot(10.0, now);
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();

                // Drift = 3.0 giờ (lớn hơn material 0.25 → snap ngay)
                fake.Factory = () => MakeSnapshot(13.0, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                await service.ResyncAsync();

                Assert.AreEqual(13.0, service.CurrentGameTimeHours, 0.02,
                    "drift lớn phải snap ngay đến predicted server");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_WorldTimeResyncedEvent_FiresOnce_PerAcceptedSnapshot()
        {
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int count = 0;
            WorldTimeSnapshot lastSnap = null;

            fake.NextSnapshot = MakeSnapshot(8.0, now, revision: 1L);
            var service = new Game.Services.Time.TimeService(config, fake);
            service.WorldTimeResynced += (s, snap) => { count++; lastSnap = snap; };
            try
            {
                await service.InitializeAsync();
                Assert.AreEqual(1, count);

                fake.NextSnapshot = MakeSnapshot(9.0, now + 5, revision: 2L);
                await service.ResyncAsync();
                Assert.AreEqual(2, count);
                Assert.AreEqual(2L, lastSnap.revision);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        // ========================================================
        // Wave 2B — Tick uses snapshot rate
        // ========================================================

        [Test]
        public async Task Tick_AfterSnapshot_UsesSnapshotRate_NotConfigRate()
        {
            // Config rate = 1200 s/day = 0.02 h/s
            // Snapshot rate = 600 s/day = 0.04 h/s → tick phải gấp đôi
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            fake.NextSnapshot = MakeSnapshot(0.0, now, rate: 600.0);

            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                double start = service.CurrentGameTimeHours;
                service.Tick(100f); // 100s * (24/600) = 100 * 0.04 = 4 giờ
                double expected = (start + 4.0) % 24.0;
                Assert.AreEqual(expected, service.CurrentGameTimeHours, 0.02,
                    "tick phải dùng rate từ snapshot, không phải config rate");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        // ========================================================
        // Wave 2B — SetGameTime on interface (PvE không cần downcast)
        // ========================================================

        [Test]
        public void SetGameTime_ViaInterface_NormalizesNegativeAndOver24()
        {
            var config = MakeConfig(1200f, 8f);
            ITimeService svc = new Game.Services.Time.TimeService(config, null);
            try
            {
                svc.InitializeAsync().GetAwaiter().GetResult();
                svc.SetGameTime(-1.0);
                Assert.AreEqual(23.0, svc.CurrentGameTimeHours, 0.0001);
                svc.SetGameTime(25.0);
                Assert.AreEqual(1.0, svc.CurrentGameTimeHours, 0.0001);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        // ========================================================
        // Legacy local rate tests (docs/08 §2.2 — giữ nguyên 6 test)
        // ========================================================

        [Test]
        public void Initialize_UsesConfigStartHourAndPhase()
        {
            TimeConfig config = MakeConfig(1200f, 8f);
            var service = new Game.Services.Time.TimeService(config, null);

            try
            {
                service.InitializeAsync().GetAwaiter().GetResult();

                Assert.AreEqual(8.0, service.CurrentGameTimeHours, 0.0001);
                Assert.AreEqual(TimeOfDay.Morning, service.CurrentPhase);
                Assert.IsTrue(service.IsInitialized);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Tick_AdvancesByConfiguredRate()
        {
            TimeConfig config = MakeConfig(1200f, 0f);
            var service = new Game.Services.Time.TimeService(config, null);

            try
            {
                service.InitializeAsync().GetAwaiter().GetResult();
                service.Tick(60f);

                Assert.AreEqual(1.2, service.CurrentGameTimeHours, 0.0001);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Tick_WrapsAroundMidnight()
        {
            TimeConfig config = MakeConfig(1200f, 23f);
            var service = new Game.Services.Time.TimeService(config, null);

            try
            {
                service.InitializeAsync().GetAwaiter().GetResult();
                service.Tick(120f);

                Assert.AreEqual(1.4, service.CurrentGameTimeHours, 0.0001);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Legacy_SetGameTime_NormalizesNegativeAndOver24()
        {
            TimeConfig config = MakeConfig(1200f, 8f);
            var service = new Game.Services.Time.TimeService(config, null);

            try
            {
                service.InitializeAsync().GetAwaiter().GetResult();

                service.SetGameTime(-1.0);
                Assert.AreEqual(23.0, service.CurrentGameTimeHours, 0.0001);

                service.SetGameTime(25.0);
                Assert.AreEqual(1.0, service.CurrentGameTimeHours, 0.0001);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void CalculatePhase_MatchesBoundaries()
        {
            TimeConfig config = MakeConfig(1200f, 8f);
            var service = new Game.Services.Time.TimeService(config, null);

            try
            {
                Assert.AreEqual(TimeOfDay.Night, service.CalculatePhase(3.0));
                Assert.AreEqual(TimeOfDay.Dawn, service.CalculatePhase(6.0));
                Assert.AreEqual(TimeOfDay.Morning, service.CalculatePhase(9.0));
                Assert.AreEqual(TimeOfDay.Noon, service.CalculatePhase(12.0));
                Assert.AreEqual(TimeOfDay.Afternoon, service.CalculatePhase(15.0));
                Assert.AreEqual(TimeOfDay.Evening, service.CalculatePhase(18.0));
                Assert.AreEqual(TimeOfDay.Night, service.CalculatePhase(22.0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Tick_WithoutConfig_UsesDefaultTwentyMinuteDay()
        {
            var service = new Game.Services.Time.TimeService(null, null);

            service.InitializeAsync().GetAwaiter().GetResult();
            service.Tick(60f);

            Assert.AreEqual(9.2, service.CurrentGameTimeHours, 0.0001);
        }
    }
}
