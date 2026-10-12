using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Game.Core.Account;
using Game.Core.Character;
using Game.Core.World;
using Game.Services.Character;
using Game.Services.World;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// D2 + D3 — Recovery record khi ghi cái chết lỗi (docs/09 §1.3, ADR-021).
    /// D2: cloud không ghi được lúc chết rồi người chơi tắt app → lần mở sau cloud nói "còn sống" → sống lại.
    ///     Cách chữa: ghi lỗi → file local {playerId, DeathInfo}; load lại đúng tài khoản → coi là ĐÃ CHẾT.
    /// D3: trước đây chỉ có MỘT file death.pending.json cho cả máy → hai tài khoản cùng chết lúc cloud lỗi thì người sau
    ///     đè người trước → tài khoản trước sống lại. Giờ mỗi tài khoản một file death.pending.{playerId}.json.
    /// Mọi test dùng THƯ MỤC TẠM thật: file recovery do CloudPlayerSaveService thật ghi/đọc/xóa;
    /// chỉ phần "cloud" (ghi lỗi/được, đọc "sống") là giả.
    /// </summary>
    public class DeathRecoveryRecordTests
    {
        private const string PlayerA = "player_A";
        private const string PlayerB = "player_B";
        private const string LegacyFileName = "death.pending.json";

        /// <summary>Tài khoản giả: PlayerId đổi được; null = chưa đăng nhập / UGS chưa khởi tạo.</summary>
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
        /// "Cloud" giả: ghi (authoritative) lúc lỗi lúc được, đọc luôn trả bản đang lưu ("sống" lúc đầu).
        /// Phần file recovery ủy quyền cho CloudPlayerSaveService THẬT trỏ vào thư mục tạm.
        /// </summary>
        private class FakeCloudWithRealFiles : ISaveService, IAuthoritativeSaveService, ILocalRecoveryStore
        {
            private readonly CloudPlayerSaveService _files;

            public bool FailWrite;
            public bool FailRead;
            public int WriteCalls;
            public DeathInfo CloudDeath = new DeathInfo(); // trạng thái "death" đang nằm trên cloud: còn sống

            public FakeCloudWithRealFiles(string directory)
            {
                _files = new CloudPlayerSaveService(directory);
            }

            public Task<T> LoadAsync<T>(string key) => Task.FromResult(default(T));

            public Task<T> LoadAuthoritativeAsync<T>(string key)
            {
                if (FailRead) return Task.FromException<T>(new SaveUnavailableException(key, null));
                if (key == SaveKeys.Death && typeof(T) == typeof(DeathInfo)) return Task.FromResult((T)(object)CloudDeath);
                return Task.FromResult(default(T));
            }

            public Task SaveAsync<T>(string key, T value)
            {
                if (key == SaveKeys.Death && (object)value is DeathInfo info) CloudDeath = info;
                return Task.CompletedTask;
            }

            public Task SaveAuthoritativeAsync<T>(string key, T value)
            {
                WriteCalls++;
                if (FailWrite) return Task.FromException(new SaveUnavailableException(key, null));
                if (key == SaveKeys.Death && (object)value is DeathInfo info) CloudDeath = info;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string key) => Task.CompletedTask;
            public Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys) =>
                Task.FromResult(new Dictionary<string, object>());
            public Task SaveBatchAsync(Dictionary<string, object> keyValues) => Task.CompletedTask;

            public bool TryWriteRecovery<T>(string name, T value) => _files.TryWriteRecovery(name, value);
            public T ReadRecovery<T>(string name) => _files.ReadRecovery<T>(name);
            public bool DeleteRecovery(string name) => _files.DeleteRecovery(name);
        }

        private string _dir;

        /// <summary>Đường dẫn file pending RIÊNG của một tài khoản: death.pending.{playerId đã làm sạch}.json.</summary>
        private string PendingPathFor(string playerId) =>
            Path.Combine(_dir, DeathPersistenceService.PendingRecoveryNameFor(playerId) + ".json");

        /// <summary>File chung cũ (trước D3).</summary>
        private string LegacyPendingPath => Path.Combine(_dir, LegacyFileName);

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "GameAE_DeathRecovery_" + Guid.NewGuid().ToString("N"));
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

        private static DeathPersistenceService NewService(ISaveService save, string playerId) =>
            new DeathPersistenceService(save, new StubAccount { Id = playerId });

        private static DeathRecord MakeRecord(
            DeathReason reason = DeathReason.Enemy,
            string sourceId = "enemy.slime",
            int level = 5,
            int floor = 2,
            int playSeconds = 120) =>
            new DeathRecord(reason, sourceId, level, floor, playSeconds, 1700000000L);

        private static void Record(DeathPersistenceService service, DeathRecord record = null) =>
            service.RecordDeathAsync(record ?? MakeRecord()).GetAwaiter().GetResult();

        private static DeathInfo Load(DeathPersistenceService service) =>
            service.LoadDeathInfoAsync().GetAwaiter().GetResult();

        private PendingDeathRecord ReadPendingFile(string playerId) =>
            JsonUtility.FromJson<PendingDeathRecord>(File.ReadAllText(PendingPathFor(playerId)));

        private string[] PendingFilesInDir() =>
            Directory.GetFiles(_dir, "death.pending*").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        /// <summary>Chết lúc cloud ghi lỗi (người chơi tắt app ngay sau đó): trả cloud giả để test "mở lại app".</summary>
        private FakeCloudWithRealFiles DieWhileCloudDown(string playerId, DeathRecord record = null)
        {
            var cloud = new FakeCloudWithRealFiles(_dir) { FailWrite = true };
            Assert.Throws<SaveUnavailableException>(() => Record(NewService(cloud, playerId), record));
            return cloud;
        }

        /// <summary>Tạo file chung CŨ (trước D3) bằng đúng định dạng cũ: death.pending.json = {playerId, DeathInfo}.</summary>
        private void WriteLegacyFile(string playerId, string sourceId = "legacy.slime", bool isDead = true)
        {
            var legacy = new PendingDeathRecord
            {
                playerId = playerId,
                deathInfo = new DeathInfo { isDead = isDead, deathCause = "Enemy", deathSourceId = sourceId, finalLevel = 4 }
            };
            Assert.IsTrue(new CloudPlayerSaveService(_dir).TryWriteRecovery(DeathPersistenceService.PendingRecoveryName, legacy));
            Assert.IsTrue(File.Exists(LegacyPendingPath), "Tiền đề: file chung cũ phải tồn tại");
        }

        // ── 1. Ghi lỗi → có file pending RIÊNG của tài khoản ─────────────────

        [Test]
        public void RecordDeath_CloudWriteFails_WritesPerAccountPendingFile_WithPlayerIdAndDeathInfo()
        {
            DieWhileCloudDown(PlayerA);

            Assert.IsTrue(File.Exists(Path.Combine(_dir, "death.pending.player_A.json")), "Tên file phải là death.pending.<playerId>.json");
            PendingDeathRecord pending = ReadPendingFile(PlayerA);
            Assert.AreEqual(PlayerA, pending.playerId);
            Assert.IsTrue(pending.deathInfo.isDead);
            Assert.AreEqual("Enemy", pending.deathInfo.deathCause);
            Assert.AreEqual("enemy.slime", pending.deathInfo.deathSourceId);
            Assert.AreEqual(5, pending.deathInfo.finalLevel);
            Assert.AreEqual(2, pending.deathInfo.finalHighestFloor);
            Assert.AreEqual(120, pending.deathInfo.totalPlayTimeSeconds);
            Assert.AreEqual(1700000000L, pending.deathInfo.deathTimeUnix);
        }

        [Test]
        public void RecordDeath_CloudWriteFails_DoesNotWriteTheSharedLegacyFile()
        {
            DieWhileCloudDown(PlayerA);

            Assert.IsFalse(File.Exists(LegacyPendingPath), "Luồng mới không còn ghi file chung death.pending.json");
            CollectionAssert.AreEqual(new[] { "death.pending.player_A.json" }, PendingFilesInDir());
        }

        [Test]
        public void RecordDeath_CloudWriteSucceeds_WritesNoPendingFile()
        {
            var cloud = new FakeCloudWithRealFiles(_dir);

            Record(NewService(cloud, PlayerA));

            Assert.IsEmpty(PendingFilesInDir(), "Cloud đã nhận thì không cần recovery record");
            Assert.IsTrue(cloud.CloudDeath.isDead);
        }

        [Test]
        public void RecordDeath_PlayerIdUnknown_StillThrows_ButCreatesNoOrphanPendingFile()
        {
            var cloud = new FakeCloudWithRealFiles(_dir) { FailWrite = true };
            var service = NewService(cloud, playerId: null);

            Assert.Throws<SaveUnavailableException>(() => Record(service));

            Assert.IsEmpty(PendingFilesInDir(), "Không biết file của ai thì không tạo (sẽ không bao giờ đối chiếu được)");
            Assert.IsTrue(service.IsPlayerDead, "Vẫn chặn chơi trong phiên này");
        }

        [Test]
        public void RecordDeath_CloudFails_ThenRetrySucceeds_DeletesOwnPendingFile()
        {
            var cloud = new FakeCloudWithRealFiles(_dir) { FailWrite = true };
            var service = NewService(cloud, PlayerA);
            Assert.Throws<SaveUnavailableException>(() => Record(service));
            Assert.IsTrue(File.Exists(PendingPathFor(PlayerA)));

            cloud.FailWrite = false; // có mạng lại, người chơi bấm Thử lại
            service.RetryRecordAsync().GetAwaiter().GetResult();

            Assert.IsFalse(File.Exists(PendingPathFor(PlayerA)), "Cloud đã nhận → xóa recovery record");
            Assert.IsTrue(cloud.CloudDeath.isDead);
        }

        // ── 2. Load lại: playerId khớp → ĐÃ CHẾT ─────────────────────────────

        [Test]
        public void Load_AfterRestart_PendingMatchesPlayer_IsDead_EvenThoughCloudSaysAlive()
        {
            FakeCloudWithRealFiles cloud = DieWhileCloudDown(PlayerA);
            Assert.IsFalse(cloud.CloudDeath.isDead, "Tiền đề: cloud vẫn là bản cũ 'còn sống'");

            // "Tắt app, mở lại": service mới, cloud vẫn chưa ghi được.
            var reopened = NewService(cloud, PlayerA);
            DeathInfo info = Load(reopened);

            Assert.IsTrue(info.isDead, "Có file pending của đúng tài khoản → phải coi là đã chết (không sống lại)");
            Assert.IsTrue(reopened.IsPlayerDead);
            Assert.AreEqual("enemy.slime", info.deathSourceId, "Dùng DeathInfo trong file");
            Assert.IsTrue(File.Exists(PendingPathFor(PlayerA)), "Chưa đẩy được lên cloud → GIỮ file");
            Assert.IsTrue(reopened.IsAwaitingConfirmation, "Vẫn là bản chờ xác nhận → Retry dùng được");
        }

        [Test]
        public void Load_PendingMatches_CloudReadAlsoFails_StillDead_DoesNotThrow()
        {
            FakeCloudWithRealFiles cloud = DieWhileCloudDown(PlayerA);
            cloud.FailRead = true;

            var reopened = NewService(cloud, PlayerA);
            DeathInfo info = null;

            Assert.DoesNotThrow(() => info = Load(reopened), "Đã chắc chắn là chết thì không cần (và không được) đòi cloud");
            Assert.IsTrue(info.isDead);
        }

        [Test]
        public void Load_PendingMatches_PushSucceeds_DeletesPendingFile_AndCloudBecomesDead()
        {
            FakeCloudWithRealFiles cloud = DieWhileCloudDown(PlayerA);
            cloud.FailWrite = false; // mở lại app khi đã có mạng

            var reopened = NewService(cloud, PlayerA);
            DeathInfo info = Load(reopened);

            Assert.IsTrue(info.isDead);
            Assert.IsTrue(cloud.CloudDeath.isDead, "Phải đẩy cái chết lên cloud");
            Assert.AreEqual("enemy.slime", cloud.CloudDeath.deathSourceId);
            Assert.IsFalse(File.Exists(PendingPathFor(PlayerA)), "Đẩy thành công → xóa file pending");
            Assert.IsFalse(reopened.IsAwaitingConfirmation);

            // Lần mở sau nữa: không còn file, cloud là nguồn chính và đã chết.
            Assert.IsTrue(Load(NewService(cloud, PlayerA)).isDead);
        }

        // ── 2b. Tài khoản khác / không xác định → không ảnh hưởng ────────────

        [Test]
        public void Load_PendingBelongsToOtherPlayer_IsIgnored_AndFileKept()
        {
            FakeCloudWithRealFiles cloud = DieWhileCloudDown(PlayerA);
            string before = File.ReadAllText(PendingPathFor(PlayerA));
            cloud.FailWrite = false;
            int writesBefore = cloud.WriteCalls;

            var otherAccount = NewService(cloud, PlayerB); // tài khoản khác trên cùng máy
            DeathInfo info = Load(otherAccount);

            Assert.IsFalse(info.isDead, "Cái chết của tài khoản A không được áp lên tài khoản B");
            Assert.IsFalse(otherAccount.IsPlayerDead);
            Assert.AreEqual(before, File.ReadAllText(PendingPathFor(PlayerA)), "File của A phải giữ nguyên nội dung");
            Assert.AreEqual(writesBefore, cloud.WriteCalls, "Không được đẩy cái chết của A lên cloud của B");

            // Tài khoản A quay lại: vẫn được coi là đã chết.
            Assert.IsTrue(Load(NewService(cloud, PlayerA)).isDead);
        }

        [Test]
        public void Load_PlayerIdUnknown_IgnoresPending_KeepsFile()
        {
            FakeCloudWithRealFiles cloud = DieWhileCloudDown(PlayerA);
            cloud.FailWrite = false;

            var notSignedIn = NewService(cloud, playerId: null);
            DeathInfo info = Load(notSignedIn);

            Assert.IsFalse(info.isDead, "Không biết tài khoản nào → không áp file");
            Assert.IsTrue(File.Exists(PendingPathFor(PlayerA)), "Không xóa file khi chưa đối chiếu được");
        }

        [Test]
        public void Load_PendingFileWithOtherPlayerIdInside_IsIgnored_FileKept()
        {
            // File của A bị chép/đổi tên nhầm thành file của B: tên file khớp B nhưng playerId bên trong là A.
            DieWhileCloudDown(PlayerA);
            File.Copy(PendingPathFor(PlayerA), PendingPathFor(PlayerB));
            var cloud = new FakeCloudWithRealFiles(_dir);

            DeathInfo info = Load(NewService(cloud, PlayerB));

            Assert.IsFalse(info.isDead, "playerId bên trong khác tài khoản hiện tại → không áp");
            Assert.IsTrue(File.Exists(PendingPathFor(PlayerB)), "Không xóa file khi chưa đối chiếu được");
        }

        [Test]
        public void Load_CorruptPendingFile_IsIgnored_NoCrash_FileKept()
        {
            File.WriteAllText(PendingPathFor(PlayerA), "{{ không phải json");
            var cloud = new FakeCloudWithRealFiles(_dir);

            var service = NewService(cloud, PlayerA);
            DeathInfo info = null;

            Assert.DoesNotThrow(() => info = Load(service));
            Assert.IsFalse(info.isDead);
            Assert.IsTrue(File.Exists(PendingPathFor(PlayerA)));
        }

        [Test]
        public void Load_NoPendingFile_BehavesAsBefore_ReadsCloud()
        {
            var cloud = new FakeCloudWithRealFiles(_dir) { CloudDeath = new DeathInfo { isDead = true, deathCause = "Boss" } };

            DeathInfo info = Load(NewService(cloud, PlayerA));

            Assert.IsTrue(info.isDead);
            Assert.AreEqual("Boss", info.deathCause);
        }

        // ── D3: hai tài khoản cùng chết lúc cloud lỗi ────────────────────────

        [Test]
        public void TwoAccounts_BothDieWhileCloudDown_GetSeparateFiles_AndBothStayDead()
        {
            var cloud = new FakeCloudWithRealFiles(_dir) { FailWrite = true };
            var recordA = MakeRecord(DeathReason.Enemy, "enemy.slime", level: 5, floor: 1, playSeconds: 100);
            var recordB = MakeRecord(DeathReason.Boss, "boss.floor_01", level: 9, floor: 3, playSeconds: 900);

            Assert.Throws<SaveUnavailableException>(() => Record(NewService(cloud, PlayerA), recordA));
            Assert.Throws<SaveUnavailableException>(() => Record(NewService(cloud, PlayerB), recordB));

            // Hai file riêng, không đè nhau.
            CollectionAssert.AreEqual(
                new[] { "death.pending.player_A.json", "death.pending.player_B.json" },
                PendingFilesInDir());
            Assert.AreEqual("enemy.slime", ReadPendingFile(PlayerA).deathInfo.deathSourceId);
            Assert.AreEqual("boss.floor_01", ReadPendingFile(PlayerB).deathInfo.deathSourceId);
            Assert.IsFalse(File.Exists(LegacyPendingPath));

            // "Mở lại app" khi cloud vẫn lỗi: cả hai vẫn chết, đúng dữ liệu của mình (A không bị B đè).
            DeathInfo loadedA = Load(NewService(cloud, PlayerA));
            DeathInfo loadedB = Load(NewService(cloud, PlayerB));
            Assert.IsTrue(loadedA.isDead, "Tài khoản A (chết trước) không được sống lại");
            Assert.AreEqual("enemy.slime", loadedA.deathSourceId);
            Assert.AreEqual(5, loadedA.finalLevel);
            Assert.IsTrue(loadedB.isDead);
            Assert.AreEqual("boss.floor_01", loadedB.deathSourceId);
            Assert.AreEqual(9, loadedB.finalLevel);
        }

        [Test]
        public void TwoAccounts_CloudComesBack_EachPushesOnlyItsOwnDeath_AndKeepsTheOthersFile()
        {
            var cloud = new FakeCloudWithRealFiles(_dir) { FailWrite = true };
            Assert.Throws<SaveUnavailableException>(() => Record(NewService(cloud, PlayerA), MakeRecord(sourceId: "enemy.slime")));
            Assert.Throws<SaveUnavailableException>(() => Record(NewService(cloud, PlayerB), MakeRecord(DeathReason.Boss, "boss.floor_01")));
            cloud.FailWrite = false;

            // A mở app có mạng: đẩy bản của A, xóa file A; file B còn nguyên.
            Assert.IsTrue(Load(NewService(cloud, PlayerA)).isDead);
            Assert.AreEqual("enemy.slime", cloud.CloudDeath.deathSourceId, "Chỉ đẩy cái chết của A");
            Assert.IsFalse(File.Exists(PendingPathFor(PlayerA)));
            Assert.IsTrue(File.Exists(PendingPathFor(PlayerB)), "A xong không được xóa file của B");

            // B mở app sau đó: vẫn là đã chết, đẩy bản của B.
            Assert.IsTrue(Load(NewService(cloud, PlayerB)).isDead);
            Assert.AreEqual("boss.floor_01", cloud.CloudDeath.deathSourceId);
            Assert.IsEmpty(PendingFilesInDir());
        }

        [Test]
        public void ResetDeathState_DeletesOwnPendingFile_SoNextLoadIsNotForcedDead()
        {
            FakeCloudWithRealFiles cloud = DieWhileCloudDown(PlayerA);
            var service = NewService(cloud, PlayerA);

            service.ResetDeathStateAsync().GetAwaiter().GetResult();

            Assert.IsFalse(File.Exists(PendingPathFor(PlayerA)));
            Assert.IsFalse(Load(NewService(cloud, PlayerA)).isDead);
        }

        [Test]
        public void ResetDeathState_OfAccountA_KeepsPendingFileOfAccountB()
        {
            var cloud = new FakeCloudWithRealFiles(_dir) { FailWrite = true };
            Assert.Throws<SaveUnavailableException>(() => Record(NewService(cloud, PlayerA)));
            Assert.Throws<SaveUnavailableException>(() => Record(NewService(cloud, PlayerB)));

            NewService(cloud, PlayerA).ResetDeathStateAsync().GetAwaiter().GetResult();

            Assert.IsFalse(File.Exists(PendingPathFor(PlayerA)));
            Assert.IsTrue(File.Exists(PendingPathFor(PlayerB)), "Reset A không được xóa file của B");
            Assert.IsTrue(Load(NewService(cloud, PlayerB)).isDead, "B vẫn chết");
        }

        // ── D3: di chuyển file chung cũ death.pending.json ───────────────────

        [Test]
        public void Legacy_MatchesCurrentPlayer_CloudUp_IsPushed_AndLegacyFileDeleted()
        {
            WriteLegacyFile(PlayerA, sourceId: "legacy.slime");
            var cloud = new FakeCloudWithRealFiles(_dir);

            DeathInfo info = Load(NewService(cloud, PlayerA));

            Assert.IsTrue(info.isDead, "File cũ của đúng tài khoản → coi là bản chờ → đã chết");
            Assert.AreEqual("legacy.slime", info.deathSourceId);
            Assert.IsTrue(cloud.CloudDeath.isDead, "Phải đẩy lên cloud");
            Assert.AreEqual("legacy.slime", cloud.CloudDeath.deathSourceId);
            Assert.IsFalse(File.Exists(LegacyPendingPath), "Di chuyển xong → xóa file chung cũ");
            Assert.IsEmpty(PendingFilesInDir(), "Đẩy được rồi thì cũng không còn file riêng");
        }

        [Test]
        public void Legacy_MatchesCurrentPlayer_CloudDown_IsMovedToOwnFile_StillDead_ThenRetryPushes()
        {
            WriteLegacyFile(PlayerA, sourceId: "legacy.slime");
            var cloud = new FakeCloudWithRealFiles(_dir) { FailWrite = true };

            var service = NewService(cloud, PlayerA);
            DeathInfo info = Load(service);

            Assert.IsTrue(info.isDead, "Cloud chưa nhận được vẫn phải là đã chết");
            Assert.IsTrue(service.IsAwaitingConfirmation);
            Assert.IsFalse(File.Exists(LegacyPendingPath), "File chung cũ đã được dọn");
            Assert.IsTrue(File.Exists(PendingPathFor(PlayerA)), "Bản chờ phải nằm an toàn ở file riêng của tài khoản (không mất cái chết)");
            PendingDeathRecord moved = ReadPendingFile(PlayerA);
            Assert.AreEqual(PlayerA, moved.playerId);
            Assert.AreEqual("legacy.slime", moved.deathInfo.deathSourceId);
            Assert.AreEqual(4, moved.deathInfo.finalLevel);

            // Có mạng lại: mở app (service mới) đẩy được, file riêng bị xóa.
            cloud.FailWrite = false;
            Assert.IsTrue(Load(NewService(cloud, PlayerA)).isDead);
            Assert.AreEqual("legacy.slime", cloud.CloudDeath.deathSourceId);
            Assert.IsEmpty(PendingFilesInDir());
        }

        [Test]
        public void Legacy_BelongsToOtherPlayer_IsKeptUntouched_AndNotAppliedToCurrentPlayer()
        {
            WriteLegacyFile(PlayerA, sourceId: "legacy.slime");
            string before = File.ReadAllText(LegacyPendingPath);
            var cloud = new FakeCloudWithRealFiles(_dir);

            DeathInfo infoB = Load(NewService(cloud, PlayerB));

            Assert.IsFalse(infoB.isDead, "File chung cũ của A không được áp lên B");
            Assert.AreEqual(before, File.ReadAllText(LegacyPendingPath), "playerId khác → giữ nguyên file cũ");
            Assert.AreEqual(0, cloud.WriteCalls, "Không được đẩy cái chết của A lên cloud của B");
            Assert.IsFalse(File.Exists(PendingPathFor(PlayerB)));

            // Khi A đăng nhập, cái chết của A vẫn còn đó và được di chuyển.
            Assert.IsTrue(Load(NewService(cloud, PlayerA)).isDead);
            Assert.IsFalse(File.Exists(LegacyPendingPath));
        }

        [Test]
        public void Legacy_PlayerIdUnknown_IsUntouched()
        {
            WriteLegacyFile(PlayerA);
            string before = File.ReadAllText(LegacyPendingPath);
            var cloud = new FakeCloudWithRealFiles(_dir);

            DeathInfo info = Load(NewService(cloud, playerId: null));

            Assert.IsFalse(info.isDead);
            Assert.AreEqual(before, File.ReadAllText(LegacyPendingPath));
            Assert.AreEqual(0, cloud.WriteCalls);
        }

        [Test]
        public void Legacy_AndOwnFileBothExist_OwnFileWins_LegacyDeleted()
        {
            FakeCloudWithRealFiles cloud = DieWhileCloudDown(PlayerA, MakeRecord(sourceId: "enemy.slime"));
            WriteLegacyFile(PlayerA, sourceId: "legacy.slime");

            DeathInfo info = Load(NewService(cloud, PlayerA)); // cloud vẫn lỗi

            Assert.IsTrue(info.isDead);
            Assert.AreEqual("enemy.slime", info.deathSourceId, "File riêng (luồng mới) thắng file cũ");
            Assert.AreEqual("enemy.slime", ReadPendingFile(PlayerA).deathInfo.deathSourceId, "Không bị file cũ đè");
            Assert.IsFalse(File.Exists(LegacyPendingPath));
        }

        [Test]
        public void Legacy_MatchesButHasNoDeath_IsLeftAlone_AndPlayerIsAlive()
        {
            WriteLegacyFile(PlayerA, isDead: false);
            var cloud = new FakeCloudWithRealFiles(_dir);

            DeathInfo info = Load(NewService(cloud, PlayerA));

            Assert.IsFalse(info.isDead, "File cũ không chứa cái chết thì không được ép chết");
            Assert.IsEmpty(PendingFilesInDir().Where(n => n != LegacyFileName));
        }

        // ── D3: tên file hợp lệ với mọi PlayerId ─────────────────────────────

        [Test]
        public void PendingRecoveryNameFor_SafeId_IsReadable()
        {
            Assert.AreEqual("death.pending.player_A", DeathPersistenceService.PendingRecoveryNameFor("player_A"));
            Assert.AreEqual("death.pending.Abc-123_xyz", DeathPersistenceService.PendingRecoveryNameFor("Abc-123_xyz"));
        }

        [Test]
        public void PendingRecoveryNameFor_UnknownPlayerId_ReturnsNull_SoNoFileIsCreated()
        {
            Assert.IsNull(DeathPersistenceService.PendingRecoveryNameFor(null));
            Assert.IsNull(DeathPersistenceService.PendingRecoveryNameFor(string.Empty));
        }

        [Test]
        public void PendingRecoveryNameFor_StrangeCharacters_KeepsSafePart_AndAppendsStableHash()
        {
            // Mã băm FNV-1a 64-bit của PlayerId GỐC — khóa cứng để tên file không đổi giữa các lần chạy/nền tảng.
            Assert.AreEqual("death.pending.ab~e620c3190468cf61", DeathPersistenceService.PendingRecoveryNameFor("a/b"));
            Assert.AreEqual("death.pending.ab~e661911904a01160", DeathPersistenceService.PendingRecoveryNameFor("a:b"));
        }

        [TestCase("a/b")]
        [TestCase("..\\..\\evil")]
        [TestCase("x:y*z?\"<>|")]
        [TestCase("id with spaces")]
        [TestCase("tên có dấu")]
        [TestCase("con\u0001trol")]
        [TestCase("../../../etc/passwd")]
        [TestCase(" ")]
        [TestCase("....")]
        public void PendingRecoveryNameFor_StrangePlayerId_GivesValidFileName(string playerId)
        {
            string name = DeathPersistenceService.PendingRecoveryNameFor(playerId);
            string fileName = name + ".json";

            Assert.IsNotNull(name);
            StringAssert.StartsWith("death.pending.", name);
            Assert.AreEqual(-1, fileName.IndexOfAny(Path.GetInvalidFileNameChars()), $"Tên '{fileName}' còn ký tự không hợp lệ");
            Assert.AreEqual(fileName, Path.GetFileName(fileName), "Không được chứa dấu phân cách thư mục");
            StringAssert.DoesNotContain("..", name.Substring("death.pending.".Length), "Không được có '..' để thoát thư mục");
            Assert.Less(name.Length, 120);
            Assert.AreEqual(name, DeathPersistenceService.PendingRecoveryNameFor(playerId), "Cùng PlayerId phải ra cùng tên");
        }

        [Test]
        public void PendingRecoveryNameFor_DifferentPlayerIds_NeverShareAName()
        {
            string longA = new string('x', 64) + "AAA";
            string longB = new string('x', 64) + "BBB"; // cùng 64 ký tự đầu → chỉ khác nhau ở phần bị cắt
            var ids = new[] { "a/b", "a:b", "ab", "a_b", "a b", "a-b", longA, longB, "A", "player_A", "player_A!" };

            var names = ids.Select(DeathPersistenceService.PendingRecoveryNameFor).ToList();

            Assert.AreEqual(ids.Length, new HashSet<string>(names).Count, "Hai PlayerId khác nhau không được trùng tên file:\n" + string.Join("\n", names));
        }

        [Test]
        public void StrangePlayerId_EndToEnd_PendingFileIsCreatedInTheFolder_AndLoadsBackAsDead()
        {
            const string weird = "weird/id:with*chars?";
            var cloud = new FakeCloudWithRealFiles(_dir) { FailWrite = true };
            Assert.Throws<SaveUnavailableException>(() => Record(NewService(cloud, weird)));

            string path = PendingPathFor(weird);
            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual(_dir, Path.GetDirectoryName(path), "File phải nằm trong thư mục local, không thoát ra ngoài");
            Assert.AreEqual("death.pending.weirdidwithchars~54a70e1d7bd2e760.json", Path.GetFileName(path));
            Assert.AreEqual(weird, ReadPendingFile(weird).playerId, "playerId GỐC được giữ nguyên trong nội dung file");

            Assert.IsTrue(Load(NewService(cloud, weird)).isDead, "Mở lại app vẫn chết");
        }

        // ── CloudPlayerSaveService: kho recovery thật ────────────────────────

        [Test]
        public void CloudPlayerSaveService_Recovery_RoundTrip_UsesPerAccountName_AndDeleteIsIdempotent()
        {
            var files = new CloudPlayerSaveService(_dir);
            string name = DeathPersistenceService.PendingRecoveryNameFor(PlayerA);
            var record = new PendingDeathRecord
            {
                playerId = PlayerA,
                deathInfo = new DeathInfo { isDead = true, deathSourceId = "enemy.bat", finalLevel = 7 }
            };

            Assert.IsTrue(files.TryWriteRecovery(name, record));
            Assert.IsTrue(File.Exists(Path.Combine(_dir, "death.pending.player_A.json")), "Tên recovery phải ra đúng file death.pending.<playerId>.json");
            Assert.IsFalse(File.Exists(LegacyPendingPath));

            PendingDeathRecord read = files.ReadRecovery<PendingDeathRecord>(name);
            Assert.AreEqual(PlayerA, read.playerId);
            Assert.AreEqual("enemy.bat", read.deathInfo.deathSourceId);
            Assert.AreEqual(7, read.deathInfo.finalLevel);

            Assert.IsTrue(files.DeleteRecovery(name));
            Assert.IsFalse(File.Exists(PendingPathFor(PlayerA)));
            Assert.IsTrue(files.DeleteRecovery(name), "Xóa file không tồn tại vẫn là thành công");
            Assert.IsNull(files.ReadRecovery<PendingDeathRecord>(name));
        }
    }
}
