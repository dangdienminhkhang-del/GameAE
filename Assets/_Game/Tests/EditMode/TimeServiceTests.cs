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

            public Task<Game.Core.World.BossDefeatResult> ReportBossDefeatedAsync(string bossId, string floorId)
            {
                ReportCallCount++;
                return Task.FromResult(new Game.Core.World.BossDefeatResult(false, bossId, floorId, "floor.02"));
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
            // anchorUnix không còn ảnh hưởng tới CurrentGameTimeHours (ApplySnapshot dùng gameTimeHoursAtAnchor trực tiếp)
            fake.NextSnapshot = MakeSnapshot(12.0, anchorUnix: 1_700_000_000L, revision: 5L);

            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();

                Assert.IsNotNull(service.CurrentSnapshot);
                Assert.AreEqual(5L, service.CurrentSnapshot.revision);
                // Server đã re-anchor → CurrentGameTimeHours == gameTimeHoursAtAnchor (không dùng giờ thiết bị)
                Assert.AreEqual(12.0, service.CurrentGameTimeHours, 0.05,
                    "ApplySnapshot phải dùng gameTimeHoursAtAnchor trực tiếp, không PredictGameHours(UtcNow)");
                Assert.IsTrue(service.IsAuthoritative);
                Assert.Less(service.SnapshotAgeSeconds, 5, "SnapshotAgeSeconds phải nhỏ (Stopwatch, không giờ máy)");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_OldRevisionCandidate_Rejected_NoEvent()
        {
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            // Anchor cố định — không dùng giờ thiết bị
            const long anchor = 1_700_000_000L;
            WorldTimeSnapshot fired = null;

            fake.NextSnapshot = MakeSnapshot(8.0, anchor, revision: 5L);
            var service = new Game.Services.Time.TimeService(config, fake);
            service.WorldTimeResynced += (s, snap) => fired = snap;
            try
            {
                await service.InitializeAsync();
                fired = null;

                // Candidate revision thấp hơn (4 < 5) → bị reject
                fake.NextSnapshot = MakeSnapshot(20.0, anchor + 10, revision: 4L);
                bool ok = await service.ResyncAsync();

                Assert.IsFalse(ok, "rejected revision → return false");
                Assert.IsNull(fired, "WorldTimeResynced KHÔNG phát khi snapshot bị reject");
                Assert.AreEqual(5L, service.CurrentSnapshot.revision, "revision không đổi");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_SameRevision_OlderAnchor_Rejected()
        {
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            const long anchor = 1_700_001_000L;

            fake.NextSnapshot = MakeSnapshot(8.0, anchor, revision: 3L);
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                WorldTimeSnapshot first = service.CurrentSnapshot;

                // Candidate cùng revision nhưng anchor cũ hơn
                fake.NextSnapshot = MakeSnapshot(8.0, anchor - 1000, revision: 3L);
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

                Assert.IsFalse(service.IsAuthoritative);
                Assert.IsNull(service.CurrentSnapshot);
                // Giờ dùng local StartHour (12.0)
                Assert.AreEqual(12.0, service.CurrentGameTimeHours, 0.0001);

                bool ok = await service.ResyncAsync();
                Assert.IsFalse(ok);
                Assert.IsNull(service.CurrentSnapshot);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        // ── Test clock-skew: anchorUnixSeconds lệch ±600s nhưng CurrentGameTimeHours vẫn đúng ──

        [Test]
        public async Task ApplySnapshot_ClockSkew_Plus600s_UsesGameTimeHoursAtAnchorDirectly()
        {
            // anchorUnixSeconds = now + 600 (máy chậm hơn server 10 phút)
            // PredictGameHours(UtcNow) sẽ cho giờ âm (~-0.2h) → SAI
            // ApplySnapshot mới dùng gameTimeHoursAtAnchor trực tiếp → đúng
            var config = MakeConfig(1200f, 0f, materialDrift: 0.01f); // materialDrift nhỏ → snap ngay
            var fake = new FakeServerFunctions();
            long nowDevice = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Server "đi trước" thiết bị 600s
            fake.NextSnapshot = MakeSnapshot(14.0, nowDevice + 600, revision: 1L);

            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                Assert.AreEqual(14.0, service.CurrentGameTimeHours, 0.1,
                    "anchorUnixSeconds > UtcNow: phải dùng gameTimeHoursAtAnchor, không PredictGameHours(UtcNow)");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ApplySnapshot_ClockSkew_Minus600s_UsesGameTimeHoursAtAnchorDirectly()
        {
            // anchorUnixSeconds = now - 600 (server anchor cũ 10 phút)
            // PredictGameHours(UtcNow) sẽ cho giờ tăng thêm ~0.2h → lệch
            // ApplySnapshot mới dùng gameTimeHoursAtAnchor trực tiếp → đúng
            var config = MakeConfig(1200f, 0f, materialDrift: 0.01f);
            var fake = new FakeServerFunctions();
            long nowDevice = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Server đã re-anchor tại thời điểm trả lời nhưng anchor cũ hơn thiết bị 600s
            fake.NextSnapshot = MakeSnapshot(14.0, nowDevice - 600, revision: 1L);

            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                Assert.AreEqual(14.0, service.CurrentGameTimeHours, 0.1,
                    "anchorUnixSeconds < UtcNow: phải dùng gameTimeHoursAtAnchor, không PredictGameHours(UtcNow)");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_AfterValidSnapshot_ResyncFailsNull_YetAuthoritative_WhenSnapshotYoung()
        {
            // maxSnapshotAgeSeconds rất lớn → snapshot còn trẻ → IsAuthoritative vẫn true
            var config = MakeConfig(1200f, 8f, maxAgeSeconds: 3600);
            var fake = new FakeServerFunctions();
            // Anchor cố định, không phụ thuộc giờ thiết bị
            fake.NextSnapshot = MakeSnapshot(8.0, 1_700_000_000L, revision: 5L);
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                Assert.IsTrue(service.IsAuthoritative, "snapshot đã được xác nhận ban đầu");

                // Resync thất bại (null)
                fake.NextSnapshot = null;
                fake.Factory = () => null;
                bool ok = await service.ResyncAsync();

                Assert.IsFalse(ok, "resync thất bại → return false");
                Assert.AreEqual(5L, service.CurrentSnapshot.revision, "snapshot cũ KHÔNG bị xóa");
                Assert.IsTrue(service.IsAuthoritative,
                    "tuổi snapshot < 3600s (Stopwatch) → IsAuthoritative vẫn true");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_AfterValidSnapshot_ResyncFailsNull_AuthoritativeFalseWhenSnapshotStale()
        {
            // maxSnapshotAgeSeconds = 1s; chờ 2s → Stopwatch age > 1s → IsAuthoritative false
            var config = MakeConfig(1200f, 8f, maxAgeSeconds: 1);
            var fake = new FakeServerFunctions();
            // Anchor cố định, không dùng giờ thiết bị
            fake.NextSnapshot = MakeSnapshot(10.0, 1_700_000_000L, revision: 2L);
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                Assert.IsTrue(service.IsAuthoritative);

                // Chờ Stopwatch age > maxSnapshotAgeSeconds (1s)
                await Task.Delay(2100);

                // Resync thất bại (server trả null)
                fake.NextSnapshot = null;
                fake.Factory = () => null;
                await service.ResyncAsync();

                Assert.IsFalse(service.IsAuthoritative,
                    "Stopwatch age > 1s → IsAuthoritative phải false dù có snapshot đã xác nhận trước đó");
                Assert.IsNotNull(service.CurrentSnapshot, "snapshot instance vẫn còn lưu trong bộ nhớ");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_HigherCycleConfigVersion_FallsOutOfAuthoritative_NoCrash()
        {
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            fake.NextSnapshot = MakeSnapshot(8.0, 1_700_000_000L, cycleVer: 999);
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
            var config = MakeConfig(1200f, 0f, materialDrift: 1.0f);
            var fake = new FakeServerFunctions();
            const long anchor = 1_700_000_000L;

            // Init với gameTimeHours = 10.0
            fake.NextSnapshot = MakeSnapshot(10.0, anchor);
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                double first = service.CurrentGameTimeHours; // = 10.0

                // Resync với predicted = 10.5 (drift = 0.5 < materialDrift 1.0 → smooth 50%)
                fake.Factory = () => MakeSnapshot(10.5, anchor + 1, revision: 2L);
                await service.ResyncAsync();

                double expected = first + 0.5 * 0.5; // 10.0 + 0.25 = 10.25
                Assert.AreEqual(expected, service.CurrentGameTimeHours, 0.05,
                    "drift nhỏ phải được sửa 50% (smooth correction)");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_LargeDrift_SnapsImmediately_NoSmooth()
        {
            var config = MakeConfig(1200f, 0f, materialDrift: 0.25f);
            var fake = new FakeServerFunctions();
            const long anchor = 1_700_000_000L;

            fake.NextSnapshot = MakeSnapshot(10.0, anchor);
            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();

                // Drift = 3.0 giờ (lớn hơn material 0.25 → snap ngay)
                fake.Factory = () => MakeSnapshot(13.0, anchor + 1, revision: 2L);
                await service.ResyncAsync();

                Assert.AreEqual(13.0, service.CurrentGameTimeHours, 0.05,
                    "drift lớn phải snap ngay đến gameTimeHoursAtAnchor server");
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public async Task ResyncAsync_WorldTimeResyncedEvent_FiresOnce_PerAcceptedSnapshot()
        {
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            const long anchor = 1_700_000_000L;
            int count = 0;
            WorldTimeSnapshot lastSnap = null;

            fake.NextSnapshot = MakeSnapshot(8.0, anchor, revision: 1L);
            var service = new Game.Services.Time.TimeService(config, fake);
            service.WorldTimeResynced += (s, snap) => { count++; lastSnap = snap; };
            try
            {
                await service.InitializeAsync();
                Assert.AreEqual(1, count);

                fake.NextSnapshot = MakeSnapshot(9.0, anchor + 5, revision: 2L);
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
            var config = MakeConfig(1200f, 0f);
            var fake = new FakeServerFunctions();
            // Snapshot rate = 600 s/day = 0.04 h/s; config rate = 1200 = 0.02 h/s
            fake.NextSnapshot = MakeSnapshot(0.0, 1_700_000_000L, rate: 600.0);

            var service = new Game.Services.Time.TimeService(config, fake);
            try
            {
                await service.InitializeAsync();
                double start = service.CurrentGameTimeHours; // = 0.0
                service.Tick(100f); // 100s × (24/600) = 4h
                Assert.AreEqual((start + 4.0) % 24.0, service.CurrentGameTimeHours, 0.02,
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
