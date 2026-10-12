using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Game.Core.Account;
using Game.Core.World;
using Game.Services.Account;
using Game.Services.World;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// S1 — Không mất tiến độ chơi offline.
    /// Vấn đề: SaveAsync ghi local rồi cloud nhưng nuốt lỗi cloud; LoadAsync luôn ưu tiên cloud → tiến độ chơi lúc mất mạng
    /// bị bản cloud cũ đè khi mở lại có mạng.
    /// Cách chữa: cloud lỗi → file dấu chờ "sync.pending.{key}.json" = { playerId }; load đúng tài khoản có dấu → đọc LOCAL rồi đẩy lên cloud.
    /// Mọi test dùng THƯ MỤC TẠM thật: file local/dấu do CloudPlayerSaveService THẬT ghi/đọc/xóa;
    /// chỉ phần "cloud" (ICloudSavePort) và "tài khoản" (IAccountService) là giả.
    /// </summary>
    public class CloudPlayerSaveServiceSyncPendingTests
    {
        private const string PlayerA = "player_A";
        private const string PlayerB = "player_B";
        private const string Key = SaveKeys.Inventory;

        [Serializable]
        public class Payload
        {
            public int value;
        }

        /// <summary>Tài khoản giả: PlayerId đổi được; null = chưa đăng nhập / mất mạng nên chưa biết PlayerId.</summary>
        private class StubAccount : IAccountService
        {
            public string Id;

            public bool IsSignedIn => Id != null;
            public string PlayerId => Id;
            public AccountType AccountType => AccountType.Guest;
            public LinkProvider LinkedProvider => default;
            public bool IsLinked => false;

            public Task InitializeAsync() => Task.CompletedTask;
            public Task<bool> SignInAnonymouslyAsync() => Task.FromResult(true);
            public Task SignOutAsync() => Task.CompletedTask;
        }

        /// <summary>
        /// "Cloud" giả: sẵn sàng/chưa sẵn sàng, ghi lỗi/được, hoặc ghi theo kịch bản (treo bằng TaskCompletionSource, lỗi...).
        /// <see cref="Stored"/> là bản đang nằm trên cloud.
        /// </summary>
        private sealed class FakeCloud : ICloudSavePort
        {
            public bool Ready = true;
            public bool FailSaves;
            public int SaveCalls;
            public readonly Dictionary<string, object> Stored = new Dictionary<string, object>();

            /// <summary>Mỗi lần ghi lấy một bước ở đây (nếu còn) thay cho FailSaves — để mô phỏng lần ghi treo/lỗi riêng.</summary>
            public readonly Queue<Func<Task>> SaveScript = new Queue<Func<Task>>();

            public bool IsReady => Ready;

            public Task<T> LoadAsync<T>(string key) =>
                Task.FromResult(Stored.TryGetValue(key, out object value) ? (T)value : default(T));

            public async Task SaveAsync<T>(string key, T value)
            {
                SaveCalls++;

                if (SaveScript.Count > 0) await SaveScript.Dequeue()();
                else if (FailSaves) throw new InvalidOperationException("cloud down");

                Stored[key] = value;
            }

            public int CloudValue(string key) => ((Payload)Stored[key]).value;
        }

        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "GameAE_SyncPending_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            }
            catch (IOException)
            {
                // Thư mục tạm bị khóa tạm thời — không làm hỏng test.
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private CloudPlayerSaveService NewService(FakeCloud cloud, string playerId) =>
            new CloudPlayerSaveService(_dir, new StubAccount { Id = playerId }, cloud);

        /// <summary>Đường dẫn viết cứng (không dùng PendingMarkerName) để test chốt luôn tên file thật.</summary>
        private string MarkerPath(string key) => Path.Combine(_dir, "sync.pending." + key + ".json");

        private string LocalPath(string key) => Path.Combine(_dir, key + ".json");

        private string LastPlayerPath => Path.Combine(_dir, "lastPlayerId.json");

        private static void Save(CloudPlayerSaveService service, string key, int value) =>
            service.SaveAsync(key, new Payload { value = value }).GetAwaiter().GetResult();

        private static Payload Load(CloudPlayerSaveService service, string key) =>
            service.LoadAsync<Payload>(key).GetAwaiter().GetResult();

        private string MarkerOwner(string key) =>
            JsonUtility.FromJson<CloudPlayerSaveService.SyncPendingRecord>(File.ReadAllText(MarkerPath(key))).playerId;

        private int LocalValue(string key) =>
            JsonUtility.FromJson<Payload>(File.ReadAllText(LocalPath(key))).value;

        /// <summary>Chơi lúc mất mạng: lưu khi cloud lỗi → bản local mới hơn cloud, có dấu chờ của <paramref name="playerId"/>.</summary>
        private void PlayOffline(string playerId, string key, int value)
        {
            var cloud = new FakeCloud { FailSaves = true };
            Save(NewService(cloud, playerId), key, value);
            Assert.IsTrue(File.Exists(MarkerPath(key)), "Tiền đề: chơi lúc cloud lỗi phải có dấu chờ");
        }

        /// <summary>
        /// Chạy <paramref name="start"/> với SynchronizationContext = null để các <c>await</c> bên trong KHÔNG bị đẩy
        /// vào hàng đợi của Unity (test chặn luồng chính nên hàng đợi đó không bao giờ chạy) mà chạy thẳng khi nhả cổng.
        /// </summary>
        private static Task RunWithoutSyncContext(Func<Task> start)
        {
            SynchronizationContext previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                return start();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        // ── 1. Lưu: cloud lỗi → có dấu ───────────────────────────────────────

        [Test]
        public void Save_CloudFails_WritesMarker_WithPlayerId_AndKeepsLocalFile()
        {
            var cloud = new FakeCloud { FailSaves = true };

            Save(NewService(cloud, PlayerA), Key, 5);

            Assert.IsTrue(File.Exists(MarkerPath(Key)), "Cloud ghi lỗi → phải có sync.pending.{key}.json");
            Assert.AreEqual(PlayerA, MarkerOwner(Key));
            Assert.AreEqual(5, LocalValue(Key), "Bản local vẫn được ghi");
        }

        [Test]
        public void Save_CloudNotReady_WritesMarker()
        {
            var cloud = new FakeCloud { Ready = false };

            Save(NewService(cloud, PlayerA), Key, 5);

            Assert.IsTrue(File.Exists(MarkerPath(Key)), "Cloud chưa sẵn sàng (mất mạng/chưa đăng nhập) → cũng có dấu");
            Assert.AreEqual(PlayerA, MarkerOwner(Key));
            Assert.AreEqual(0, cloud.SaveCalls, "Cloud chưa sẵn sàng thì không được gọi ghi");
        }

        [Test]
        public void Save_CloudSucceeds_WritesNoMarker()
        {
            var cloud = new FakeCloud();

            Save(NewService(cloud, PlayerA), Key, 5);

            Assert.IsFalse(File.Exists(MarkerPath(Key)), "Cloud đã nhận thì không cần dấu");
            Assert.AreEqual(5, cloud.CloudValue(Key));
        }

        [Test]
        public void Save_CloudSucceeds_AfterEarlierFailure_DeletesMarker()
        {
            PlayOffline(PlayerA, Key, 5);
            var cloud = new FakeCloud(); // có mạng lại

            Save(NewService(cloud, PlayerA), Key, 6);

            Assert.IsFalse(File.Exists(MarkerPath(Key)), "Lần lưu mới nhất lên cloud được → xóa dấu");
            Assert.AreEqual(6, cloud.CloudValue(Key));
        }

        [Test]
        public void Save_MarkerIsPerKey_OtherKeysAreUntouched()
        {
            PlayOffline(PlayerA, SaveKeys.Inventory, 5);
            var cloud = new FakeCloud();

            Save(NewService(cloud, PlayerA), SaveKeys.Quests, 1);

            Assert.IsTrue(File.Exists(MarkerPath(SaveKeys.Inventory)), "Lưu key khác thành công không được xóa dấu của key này");
            Assert.IsFalse(File.Exists(MarkerPath(SaveKeys.Quests)));
        }

        [Test]
        public void Save_PlayerIdUnknown_StillSavesLocal_ButCreatesNoOrphanMarker()
        {
            var cloud = new FakeCloud { FailSaves = true };

            Assert.DoesNotThrow(() => Save(NewService(cloud, playerId: null), Key, 5));

            Assert.AreEqual(5, LocalValue(Key));
            Assert.IsFalse(File.Exists(MarkerPath(Key)), "Không biết file của ai thì không tạo dấu mồ côi");
        }

        [Test]
        public void Save_WritesLastPlayerId_WheneverPlayerIdIsKnown()
        {
            Save(NewService(new FakeCloud(), PlayerA), Key, 1); // cloud ghi được, không có dấu nào

            Assert.IsTrue(File.Exists(LastPlayerPath), "Phải ghi lastPlayerId.json mỗi khi biết PlayerId");
            Assert.AreEqual(PlayerA,
                JsonUtility.FromJson<CloudPlayerSaveService.LastPlayerRecord>(File.ReadAllText(LastPlayerPath)).playerId);

            Save(NewService(new FakeCloud(), PlayerB), Key, 2); // đổi tài khoản → ghi lại
            Assert.AreEqual(PlayerB,
                JsonUtility.FromJson<CloudPlayerSaveService.LastPlayerRecord>(File.ReadAllText(LastPlayerPath)).playerId);
        }

        [Test]
        public void Save_PlayerIdUnknown_UsesLastPlayerId_ForTheMarker()
        {
            Save(NewService(new FakeCloud(), PlayerA), Key, 1); // lần mở trước có mạng, ghi lastPlayerId = A

            // Mở lại lúc mất mạng: chưa đăng nhập được nên chưa biết PlayerId.
            Save(NewService(new FakeCloud { Ready = false }, playerId: null), Key, 5);

            Assert.IsTrue(File.Exists(MarkerPath(Key)));
            Assert.AreEqual(PlayerA, MarkerOwner(Key), "Dùng PlayerId đăng nhập gần nhất");
        }

        // ── 2. Load: dấu khớp → đọc local và đẩy lên cloud ───────────────────

        [Test]
        public void Load_OwnMarker_ReadsLocal_NotTheStaleCloud_AndPushesItToCloud()
        {
            PlayOffline(PlayerA, Key, 5);
            var cloud = new FakeCloud();
            cloud.Stored[Key] = new Payload { value = 1 }; // bản cloud CŨ

            Payload loaded = Load(NewService(cloud, PlayerA), Key);

            Assert.AreEqual(5, loaded.value, "Phải đọc bản local (mới hơn), không phải bản cloud cũ");
            Assert.AreEqual(5, cloud.CloudValue(Key), "Phải đẩy bản local lên cloud");
            Assert.AreEqual(1, cloud.SaveCalls);
        }

        [Test]
        public void Load_OwnMarker_PushSucceeds_DeletesMarker()
        {
            PlayOffline(PlayerA, Key, 5);
            var cloud = new FakeCloud();

            Load(NewService(cloud, PlayerA), Key);

            Assert.IsFalse(File.Exists(MarkerPath(Key)), "Đẩy thành công → xóa dấu");

            // Lần mở sau nữa: không còn dấu, cloud là nguồn chính và đã là bản mới.
            Assert.AreEqual(5, Load(NewService(cloud, PlayerA), Key).value);
            Assert.AreEqual(1, cloud.SaveCalls, "Không còn dấu thì không đẩy lại");
        }

        [Test]
        public void Load_OwnMarker_PushFails_KeepsMarker_AndStillReturnsLocal()
        {
            PlayOffline(PlayerA, Key, 5);
            var cloud = new FakeCloud { FailSaves = true };
            cloud.Stored[Key] = new Payload { value = 1 };

            Payload loaded = null;
            Assert.DoesNotThrow(() => loaded = Load(NewService(cloud, PlayerA), Key));

            Assert.AreEqual(5, loaded.value);
            Assert.IsTrue(File.Exists(MarkerPath(Key)), "Đẩy lỗi → GIỮ dấu để lần mở sau thử lại");
            Assert.AreEqual(1, cloud.CloudValue(Key), "Cloud vẫn là bản cũ");
        }

        [Test]
        public void Load_OwnMarker_CloudNotReady_ReturnsLocal_KeepsMarker()
        {
            PlayOffline(PlayerA, Key, 5);
            var cloud = new FakeCloud { Ready = false };

            Payload loaded = Load(NewService(cloud, PlayerA), Key);

            Assert.AreEqual(5, loaded.value);
            Assert.IsTrue(File.Exists(MarkerPath(Key)));
            Assert.AreEqual(0, cloud.SaveCalls);
        }

        [Test]
        public void Load_OfflineStart_PlayerIdUnknown_UsesLastPlayerId_ToMatchTheMarker()
        {
            Save(NewService(new FakeCloud(), PlayerA), Key, 1);                               // phiên 1: có mạng
            Save(NewService(new FakeCloud { Ready = false }, playerId: null), Key, 5);        // phiên 2: mất mạng, chơi tiếp

            // Phiên 3: vẫn mất mạng, chưa biết PlayerId → dùng lastPlayerId (A) khớp dấu của A → đọc local.
            var offline = new FakeCloud { Ready = false };
            Payload loaded = Load(NewService(offline, playerId: null), Key);
            Assert.AreEqual(5, loaded.value);
            Assert.IsTrue(File.Exists(MarkerPath(Key)));

            // Phiên 4: có mạng, đăng nhập A; cloud còn bản cũ → local thắng rồi đẩy lên.
            var online = new FakeCloud();
            online.Stored[Key] = new Payload { value = 1 };
            Assert.AreEqual(5, Load(NewService(online, PlayerA), Key).value);
            Assert.AreEqual(5, online.CloudValue(Key));
            Assert.IsFalse(File.Exists(MarkerPath(Key)));
        }

        // ── 2b. Dấu của tài khoản khác / không có dấu → như cũ ───────────────

        [Test]
        public void Load_MarkerBelongsToOtherPlayer_IsIgnored_ReadsCloud_AndFileKept()
        {
            PlayOffline(PlayerA, Key, 5);
            string markerBefore = File.ReadAllText(MarkerPath(Key));

            var cloud = new FakeCloud();
            cloud.Stored[Key] = new Payload { value = 100 }; // dữ liệu của tài khoản B trên cloud

            Payload loaded = Load(NewService(cloud, PlayerB), Key); // tài khoản khác trên cùng máy

            Assert.AreEqual(100, loaded.value, "Dấu của A không được làm B đọc bản local của A");
            Assert.AreEqual(0, cloud.SaveCalls, "Không được đẩy dữ liệu của A lên cloud của B");
            Assert.IsTrue(File.Exists(MarkerPath(Key)), "playerId khác → KHÔNG xóa file dấu");
            Assert.AreEqual(markerBefore, File.ReadAllText(MarkerPath(Key)), "File dấu phải giữ nguyên nội dung");
            Assert.AreEqual(PlayerA, MarkerOwner(Key));
        }

        [Test]
        public void Load_NoMarker_ReadsCloudFirst_AsBefore()
        {
            Save(NewService(new FakeCloud(), PlayerA), Key, 5); // local = 5, không có dấu
            var cloud = new FakeCloud();
            cloud.Stored[Key] = new Payload { value = 9 };

            Payload loaded = Load(NewService(cloud, PlayerA), Key);

            Assert.AreEqual(9, loaded.value, "Không có dấu → cloud trước như cũ");
            Assert.AreEqual(0, cloud.SaveCalls);
        }

        [Test]
        public void Load_NoMarker_CloudFails_FallsBackToLocal_AsBefore()
        {
            Save(NewService(new FakeCloud(), PlayerA), Key, 5);
            var cloud = new FakeCloud { Ready = false };

            Assert.AreEqual(5, Load(NewService(cloud, PlayerA), Key).value);
        }

        [Test]
        public void Load_OwnMarker_ButLocalFileMissing_FallsBackToCloud()
        {
            // Dấu mồ côi (file local đã mất): không có gì mới hơn cloud để giữ.
            var files = new CloudPlayerSaveService(_dir);
            Assert.IsTrue(files.TryWriteRecovery(CloudPlayerSaveService.PendingMarkerName(Key),
                new CloudPlayerSaveService.SyncPendingRecord { playerId = PlayerA }));
            var cloud = new FakeCloud();
            cloud.Stored[Key] = new Payload { value = 7 };

            Payload loaded = Load(NewService(cloud, PlayerA), Key);

            Assert.AreEqual(7, loaded.value);
            Assert.AreEqual(0, cloud.SaveCalls, "Không có bản local thì không đẩy gì lên");
        }

        // ── 3. Lần lưu cũ hoàn tất muộn không xóa dấu của lần lưu mới ────────

        [Test]
        public void Save_OlderSaveFinishesLate_DoesNotDeleteMarkerOfNewerSave()
        {
            var cloud = new FakeCloud();
            var gate = new TaskCompletionSource<bool>();
            cloud.SaveScript.Enqueue(() => gate.Task);                                                         // lần lưu 1: treo
            cloud.SaveScript.Enqueue(() => Task.FromException(new InvalidOperationException("cloud down"))); // lần lưu 2: lỗi
            CloudPlayerSaveService service = NewService(cloud, PlayerA);

            Task first = RunWithoutSyncContext(() => service.SaveAsync(Key, new Payload { value = 1 }));
            Assert.IsFalse(first.IsCompleted, "Tiền đề: lần lưu 1 đang treo chờ cloud");

            Save(service, Key, 2); // lần lưu 2 (mới hơn) lỗi → có dấu
            Assert.IsTrue(File.Exists(MarkerPath(Key)), "Tiền đề: lần lưu mới nhất lỗi → có dấu");

            // Lần lưu 1 (cũ) hoàn tất muộn và THÀNH CÔNG.
            RunWithoutSyncContext(() =>
            {
                gate.SetResult(true);
                return Task.CompletedTask;
            });
            first.GetAwaiter().GetResult();

            Assert.IsTrue(File.Exists(MarkerPath(Key)), "Lần lưu cũ xong muộn không được xóa dấu của lần lưu mới hơn");
            Assert.AreEqual(2, LocalValue(Key), "File local là bản của lần lưu mới nhất");
        }

        // ── 4. Key "death" không có dấu ──────────────────────────────────────

        [Test]
        public void Save_DeathKey_NeverCreatesMarker_EvenWhenCloudFails()
        {
            var cloud = new FakeCloud { FailSaves = true };
            CloudPlayerSaveService service = NewService(cloud, PlayerA);

            Save(service, SaveKeys.Death, 1);
            Save(service, Key, 1); // đối chứng: key thường cùng điều kiện thì CÓ dấu

            Assert.IsFalse(File.Exists(MarkerPath(SaveKeys.Death)), "Key death có cơ chế riêng → không có dấu");
            Assert.IsTrue(File.Exists(MarkerPath(Key)));
        }

        [Test]
        public void Load_DeathKey_IgnoresAnyMarker_ReadsCloudFirst()
        {
            var files = new CloudPlayerSaveService(_dir);
            Save(NewService(new FakeCloud(), PlayerA), SaveKeys.Death, 9);
            Assert.IsTrue(files.TryWriteRecovery(CloudPlayerSaveService.PendingMarkerName(SaveKeys.Death),
                new CloudPlayerSaveService.SyncPendingRecord { playerId = PlayerA }));
            var cloud = new FakeCloud();
            cloud.Stored[SaveKeys.Death] = new Payload { value = 1 };

            Payload loaded = Load(NewService(cloud, PlayerA), SaveKeys.Death);

            Assert.AreEqual(1, loaded.value, "Key death không dùng dấu chờ: cloud trước");
            Assert.AreEqual(0, cloud.SaveCalls);
        }

        // ── Xóa key / reset tài khoản xóa dấu ────────────────────────────────

        [Test]
        public void Delete_RemovesMarker_AndLocalFile()
        {
            PlayOffline(PlayerA, Key, 5);

            NewService(new FakeCloud { Ready = false }, PlayerA).DeleteAsync(Key).GetAwaiter().GetResult();

            Assert.IsFalse(File.Exists(MarkerPath(Key)), "Xóa key phải xóa dấu của key đó");
            Assert.IsFalse(File.Exists(LocalPath(Key)));
        }

        [Test]
        public void Delete_OneKey_KeepsMarkersOfOtherKeys()
        {
            PlayOffline(PlayerA, SaveKeys.Inventory, 5);
            PlayOffline(PlayerA, SaveKeys.Quests, 3);

            NewService(new FakeCloud(), PlayerA).DeleteAsync(SaveKeys.Inventory).GetAwaiter().GetResult();

            Assert.IsFalse(File.Exists(MarkerPath(SaveKeys.Inventory)));
            Assert.IsTrue(File.Exists(MarkerPath(SaveKeys.Quests)));
        }

        [Test]
        public void ResetGuest_RemovesMarkersOfEveryKey_OfTheOldAccount()
        {
            PlayOffline(PlayerA, SaveKeys.Inventory, 5);
            PlayOffline(PlayerA, SaveKeys.Quests, 3);
            PlayOffline(PlayerA, SaveKeys.Progress, 2);
            var files = NewService(new FakeCloud { Ready = false }, PlayerB); // tài khoản mới sau khi reset
            var reset = new AccountResetService(new StubAccount { Id = PlayerB }, files);

            bool ok = reset.ResetGuestAccountAsync().GetAwaiter().GetResult();

            Assert.IsTrue(ok);
            foreach (string key in SaveKeys.All)
            {
                Assert.IsFalse(File.Exists(MarkerPath(key)), $"Reset phải xóa dấu chờ của '{key}'");
                Assert.IsFalse(File.Exists(LocalPath(key)), $"Reset phải xóa file local '{key}'");
            }
        }

        [Test]
        public void PendingMarkerName_MatchesTheFileName()
        {
            Assert.AreEqual("sync.pending.inventory", CloudPlayerSaveService.PendingMarkerName(SaveKeys.Inventory));
        }
    }
}
