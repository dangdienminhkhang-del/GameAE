using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Character;
using Game.Core.Events;
using Game.Core.World;
using Game.Services.Character;
using Game.Services.World;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Permadeath (docs/09 §1, docs/03 §12, ADR-021): chết chỉ ghi nhận một lần; PlayerDeathRecorded CHỈ phát sau khi
    /// nguồn chính xác nhận; ghi lỗi thì vẫn chặn chơi + ném SaveUnavailableException; không xác minh được thì FAIL-CLOSED.
    /// </summary>
    public class DeathPersistenceServiceTests
    {
        /// <summary>Giả lập nguồn chính (cloud) bị lỗi mạng khi ĐỌC.</summary>
        private class UnavailableSaveService : PlayerSaveService
        {
            public override Task<T> LoadAuthoritativeAsync<T>(string key)
            {
                return Task.FromException<T>(new SaveUnavailableException(key, null));
            }
        }

        /// <summary>Giả lập nguồn chính khi GHI: lúc lỗi, lúc được; đếm số lần gọi từng đường ghi.</summary>
        private class FlakySaveService : PlayerSaveService
        {
            public bool FailAuthoritative;
            public Exception FailWith;           // null → SaveUnavailableException
            public int AuthoritativeCalls;
            public int PlainSaveCalls;
            public Action OnAuthoritativeSave;   // chạy ngay lúc ghi (để kiểm tra thứ tự "ghi xong mới phát sự kiện")

            public override Task SaveAuthoritativeAsync<T>(string key, T value)
            {
                AuthoritativeCalls++;
                OnAuthoritativeSave?.Invoke();
                if (FailAuthoritative)
                    return Task.FromException(FailWith ?? new SaveUnavailableException(key, null));
                return base.SaveAuthoritativeAsync(key, value);
            }

            public override Task SaveAsync<T>(string key, T value)
            {
                PlainSaveCalls++;
                return base.SaveAsync(key, value);
            }
        }

        /// <summary>ISaveService "trơn" (không có IAuthoritativeSaveService) → service phải fallback sang SaveAsync.</summary>
        private class PlainSaveService : ISaveService
        {
            public int SaveCalls;
            public object LastSaved;

            public Task<T> LoadAsync<T>(string key) => Task.FromResult(default(T));
            public Task<T> LoadAuthoritativeAsync<T>(string key) => Task.FromResult(default(T));

            public Task SaveAsync<T>(string key, T value)
            {
                SaveCalls++;
                LastSaved = value;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string key) => Task.CompletedTask;
            public Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys) =>
                Task.FromResult(new Dictionary<string, object>());
            public Task SaveBatchAsync(Dictionary<string, object> keyValues) => Task.CompletedTask;
        }

        private int _recordedEvents;
        private int _diedEvents;

        [SetUp]
        public void SetUp()
        {
            _recordedEvents = 0;
            _diedEvents = 0;
            GameEvents.OnPlayerDeathRecorded += CountRecorded;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnPlayerDeathRecorded -= CountRecorded;
        }

        private void CountRecorded(object sender, DeathInfo info) => _recordedEvents++;

        private DeathPersistenceService NewService(ISaveService save)
        {
            var service = new DeathPersistenceService(save);
            service.OnPlayerDied += (s, info) => _diedEvents++;
            return service;
        }

        private static DeathRecord MakeRecord(
            DeathReason reason = DeathReason.Enemy,
            string sourceId = "enemy.slime",
            int level = 5,
            int floor = 2,
            int playSeconds = 120,
            long deathTimeUnix = 1700000000L)
        {
            return new DeathRecord(reason, sourceId, level, floor, playSeconds, deathTimeUnix);
        }

        // ── Đọc trạng thái (fail-closed) ──────────────────────────────────────

        [Test]
        public void LoadDeathInfo_WhenSourceUnavailable_Throws_NotFallbackToAlive()
        {
            var service = new DeathPersistenceService(new UnavailableSaveService());

            Assert.Throws<SaveUnavailableException>(() => service.LoadDeathInfoAsync().GetAwaiter().GetResult());
            Assert.IsFalse(service.IsPlayerDead, "Chưa biết ≠ còn sống: caller phải tự chặn, service không được tự khẳng định");
        }

        [Test]
        public void LoadDeathInfo_ReturnsSavedDeadState()
        {
            var save = new PlayerSaveService();
            save.SaveAsync(SaveKeys.Death, new DeathInfo { isDead = true, deathCause = "Enemy" }).GetAwaiter().GetResult();
            var service = new DeathPersistenceService(save);

            DeathInfo info = service.LoadDeathInfoAsync().GetAwaiter().GetResult();

            Assert.IsTrue(info.isDead);
            Assert.IsTrue(service.IsPlayerDead);
        }

        // ── RecordDeathAsync: ghi xác nhận ───────────────────────────────────

        [Test]
        public void RecordDeath_PersistsDeath()
        {
            var save = new PlayerSaveService();
            var service = NewService(save);

            service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult();

            DeathInfo stored = save.LoadAuthoritativeAsync<DeathInfo>(SaveKeys.Death).GetAwaiter().GetResult();
            Assert.IsTrue(stored.isDead);
            Assert.AreEqual("Enemy", stored.deathCause);
            Assert.AreEqual("enemy.slime", stored.deathSourceId);
            Assert.AreEqual(5, stored.finalLevel);
            Assert.AreEqual(2, stored.finalHighestFloor);
            Assert.AreEqual(120, stored.totalPlayTimeSeconds);
            Assert.AreEqual(1700000000L, stored.deathTimeUnix);
        }

        [Test]
        public void RecordDeath_CalledTwice_KeepsFirstDeath()
        {
            var save = new PlayerSaveService();
            var service = NewService(save);

            service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult();
            service.RecordDeathAsync(MakeRecord(DeathReason.Trap, "trap.spike", 9, 3, 999)).GetAwaiter().GetResult();

            Assert.AreEqual("Enemy", service.CurrentDeathInfo.deathCause);
            Assert.AreEqual("enemy.slime", service.CurrentDeathInfo.deathSourceId);
            Assert.AreEqual(5, service.CurrentDeathInfo.finalLevel);

            DeathInfo stored = save.LoadAuthoritativeAsync<DeathInfo>(SaveKeys.Death).GetAwaiter().GetResult();
            Assert.AreEqual("enemy.slime", stored.deathSourceId, "Lần gọi thứ hai không được ghi đè");
            Assert.AreEqual(1, _recordedEvents, "PlayerDeathRecorded chỉ phát một lần");
            Assert.AreEqual(1, _diedEvents, "OnPlayerDied chỉ phát một lần");
        }

        [Test]
        public void RecordDeath_Confirmed_RaisesPlayerDeathRecordedExactlyOnce_AfterWrite()
        {
            var save = new FlakySaveService();
            int recordedWhenSaving = -1;
            int diedWhenSaving = -1;
            save.OnAuthoritativeSave = () =>
            {
                recordedWhenSaving = _recordedEvents;
                diedWhenSaving = _diedEvents;
            };
            var service = NewService(save);

            service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult();

            Assert.AreEqual(0, recordedWhenSaving, "Lúc đang ghi chưa được phát PlayerDeathRecorded");
            Assert.AreEqual(0, diedWhenSaving, "Lúc đang ghi chưa được phát OnPlayerDied");
            Assert.AreEqual(1, _recordedEvents);
            Assert.AreEqual(1, _diedEvents);
            Assert.IsFalse(service.IsAwaitingConfirmation);
        }

        [Test]
        public void RecordDeath_UsesAuthoritativeSave_WhenSupported()
        {
            var save = new FlakySaveService();
            var service = NewService(save);

            service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult();

            Assert.AreEqual(1, save.AuthoritativeCalls);
            Assert.AreEqual(0, save.PlainSaveCalls, "Không được dùng SaveAsync (nuốt lỗi cloud) cho cái chết");
        }

        [Test]
        public void RecordDeath_FallsBackToSaveAsync_WhenSaveServiceIsNotAuthoritative()
        {
            var save = new PlainSaveService();
            var service = NewService(save);

            service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult();

            Assert.AreEqual(1, save.SaveCalls);
            Assert.IsTrue(((DeathInfo)save.LastSaved).isDead);
            Assert.AreEqual(1, _recordedEvents);
        }

        // ── RecordDeathAsync: ghi lỗi ────────────────────────────────────────

        [Test]
        public void RecordDeath_SaveUnavailable_Throws_NoEvent_AndStillBlocksPlay()
        {
            var save = new FlakySaveService { FailAuthoritative = true };
            var service = NewService(save);

            Assert.Throws<SaveUnavailableException>(() => service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult());

            Assert.AreEqual(0, _recordedEvents, "Chưa xác nhận → KHÔNG được phát PlayerDeathRecorded");
            Assert.AreEqual(0, _diedEvents, "Chưa xác nhận → KHÔNG được phát OnPlayerDied");
            Assert.IsTrue(service.IsPlayerDead, "Vẫn phải coi là đã chết (chặn chơi) dù chưa lưu được");
            Assert.IsTrue(service.IsAwaitingConfirmation);
            Assert.AreEqual("enemy.slime", service.CurrentDeathInfo.deathSourceId);
        }

        [Test]
        public void RecordDeath_UnexpectedSaveError_IsWrappedAsSaveUnavailable()
        {
            var save = new FlakySaveService { FailAuthoritative = true, FailWith = new InvalidOperationException("boom") };
            var service = NewService(save);

            var thrown = Assert.Throws<SaveUnavailableException>(() => service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult());

            Assert.IsInstanceOf<InvalidOperationException>(thrown.InnerException);
            Assert.AreEqual(0, _recordedEvents);
        }

        [Test]
        public void RecordDeath_CalledAgainAfterFailedWrite_IsIgnored_NoSecondWrite()
        {
            var save = new FlakySaveService { FailAuthoritative = true };
            var service = NewService(save);
            Assert.Throws<SaveUnavailableException>(() => service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult());

            Assert.DoesNotThrow(() => service.RecordDeathAsync(MakeRecord(DeathReason.Boss, "boss.floor_01", 9, 3, 999)).GetAwaiter().GetResult());

            Assert.AreEqual(1, save.AuthoritativeCalls, "Đã chết rồi thì bỏ qua — thử lại phải đi qua RetryRecordAsync");
            Assert.AreEqual("enemy.slime", service.CurrentDeathInfo.deathSourceId, "Giữ cái chết đầu tiên");
        }

        [Test]
        public void LoadDeathInfo_WhileAwaitingConfirmation_KeepsDeadState()
        {
            var save = new FlakySaveService { FailAuthoritative = true };
            var service = NewService(save);
            Assert.Throws<SaveUnavailableException>(() => service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult());

            DeathInfo info = service.LoadDeathInfoAsync().GetAwaiter().GetResult();

            Assert.IsTrue(info.isDead, "Bản cũ ('sống') trên nguồn chính không được đè lên cái chết chưa ghi được");
            Assert.IsTrue(service.IsPlayerDead);
        }

        // ── RetryRecordAsync ─────────────────────────────────────────────────

        [Test]
        public void Retry_AfterFailure_Succeeds_RaisesEventsExactlyOnce()
        {
            var save = new FlakySaveService { FailAuthoritative = true };
            var service = NewService(save);
            Assert.Throws<SaveUnavailableException>(() => service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult());
            Assert.AreEqual(0, _recordedEvents);

            save.FailAuthoritative = false; // bật mạng lại
            service.RetryRecordAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, _recordedEvents);
            Assert.AreEqual(1, _diedEvents);
            Assert.IsFalse(service.IsAwaitingConfirmation);

            DeathInfo stored = save.LoadAuthoritativeAsync<DeathInfo>(SaveKeys.Death).GetAwaiter().GetResult();
            Assert.IsTrue(stored.isDead);
            Assert.AreEqual("enemy.slime", stored.deathSourceId);

            // Bấm Thử lại thêm lần nữa khi đã xác nhận → không làm gì, không phát thêm.
            service.RetryRecordAsync().GetAwaiter().GetResult();
            Assert.AreEqual(1, _recordedEvents);
            Assert.AreEqual(1, save.AuthoritativeCalls - 1, "Chỉ có đúng một lần ghi thành công sau lần lỗi");
        }

        [Test]
        public void Retry_StillUnavailable_Throws_NoEvent_ThenLaterSucceeds()
        {
            var save = new FlakySaveService { FailAuthoritative = true };
            var service = NewService(save);
            Assert.Throws<SaveUnavailableException>(() => service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult());

            Assert.Throws<SaveUnavailableException>(() => service.RetryRecordAsync().GetAwaiter().GetResult());
            Assert.AreEqual(0, _recordedEvents);
            Assert.IsTrue(service.IsAwaitingConfirmation, "Vẫn còn bản chờ");

            save.FailAuthoritative = false;
            service.RetryRecordAsync().GetAwaiter().GetResult();
            Assert.AreEqual(1, _recordedEvents);
        }

        [Test]
        public void Retry_WithoutPendingRecord_DoesNothing()
        {
            var save = new FlakySaveService();
            var service = NewService(save);

            Assert.DoesNotThrow(() => service.RetryRecordAsync().GetAwaiter().GetResult());

            Assert.AreEqual(0, save.AuthoritativeCalls);
            Assert.AreEqual(0, _recordedEvents);
            Assert.IsFalse(service.IsPlayerDead);
        }

        [Test]
        public void ResetDeathState_ClearsPendingConfirmation()
        {
            var save = new FlakySaveService { FailAuthoritative = true };
            var service = NewService(save);
            Assert.Throws<SaveUnavailableException>(() => service.RecordDeathAsync(MakeRecord()).GetAwaiter().GetResult());

            service.ResetDeathStateAsync().GetAwaiter().GetResult();

            Assert.IsFalse(service.IsPlayerDead);
            Assert.IsFalse(service.IsAwaitingConfirmation);
        }

        // ── API cũ ───────────────────────────────────────────────────────────

#pragma warning disable CS0618 // OnPlayerDiedAsync là API cũ [Obsolete] — test này kiểm tra nó vẫn chạy
        [Test]
        public void LegacyOnPlayerDiedAsync_StillWorks_AndGoesThroughRecordDeath()
        {
            var save = new FlakySaveService();
            var service = NewService(save);

            service.OnPlayerDiedAsync(DeathReason.Enemy, "enemy.slime", 5, 1, 120).GetAwaiter().GetResult();

            DeathInfo stored = save.LoadAuthoritativeAsync<DeathInfo>(SaveKeys.Death).GetAwaiter().GetResult();
            Assert.IsTrue(stored.isDead);
            Assert.AreEqual("Enemy", stored.deathCause);
            Assert.AreEqual("enemy.slime", stored.deathSourceId);
            Assert.AreEqual(5, stored.finalLevel);
            Assert.AreEqual(1, stored.finalHighestFloor);
            Assert.AreEqual(120, stored.totalPlayTimeSeconds);
            Assert.Greater(stored.deathTimeUnix, 0L, "API cũ phải tự đóng dấu thời điểm chết");
            Assert.AreEqual(1, save.AuthoritativeCalls, "API cũ phải đi qua RecordDeathAsync (ghi xác nhận)");
            Assert.AreEqual(1, _recordedEvents);
        }

        [Test]
        public void LegacyOnPlayerDiedAsync_SaveUnavailable_Throws()
        {
            var save = new FlakySaveService { FailAuthoritative = true };
            var service = NewService(save);

            Assert.Throws<SaveUnavailableException>(() =>
                service.OnPlayerDiedAsync(DeathReason.Enemy, "enemy.slime", 5, 1, 120).GetAwaiter().GetResult());
            Assert.AreEqual(0, _recordedEvents);
        }
#pragma warning restore CS0618
    }
}
